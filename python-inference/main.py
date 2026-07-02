import io
import json
import os
import traceback
import asyncio
from threading import Thread
from queue import Queue, Empty

from fastapi import FastAPI, UploadFile, File, Form
from fastapi.responses import StreamingResponse, JSONResponse
from PIL import Image
import torch
from transformers import AutoProcessor, AutoModelForImageTextToText, TextIteratorStreamer
from huggingface_hub import login

app = FastAPI()

hf_token = os.getenv("HF_TOKEN")
if hf_token:
    login(token=hf_token)

MODEL_ID = "google/medgemma-4b-it"
DEVICE = "cuda" if torch.cuda.is_available() else "cpu"
DTYPE = torch.bfloat16 if torch.cuda.is_available() and torch.cuda.is_bf16_supported() else torch.float32

print(f"Loading processor and model onto {DEVICE}...")
processor = AutoProcessor.from_pretrained(MODEL_ID)
model = AutoModelForImageTextToText.from_pretrained(
    MODEL_ID,
    torch_dtype=DTYPE,
    device_map="auto"
)
print("Model loaded successfully.")


def enforce_alternation(messages: list[dict]) -> list[dict]:
    merged = []
    for msg in messages:
        role = msg.get("role", "user")
        content = msg.get("content", "")

        if merged and merged[-1]["role"] == role:
            if isinstance(merged[-1]["content"], str) and isinstance(content, str):
                merged[-1]["content"] += "\n" + content
            else:
                # Keep safest behavior for mixed content
                merged.append({"role": role, "content": content})
        else:
            merged.append({"role": role, "content": content})
    return merged


def chunk_for_smooth_stream(text: str):
    """
    Converts model chunk into smaller chunks for ChatGPT-like progressive rendering.
    Strategy:
      - preserve spaces/newlines
      - split mostly by words/punctuation
    """
    if not text:
        return []

    pieces = []
    buf = []

    def flush():
        nonlocal buf
        if buf:
            pieces.append("".join(buf))
            buf = []

    for ch in text:
        buf.append(ch)
        # flush on whitespace/punctuation to make output feel incremental
        if ch in {" ", "\n", "\t", ".", ",", ";", ":", "!", "?", ")", "]", "}"}:
            flush()

    flush()
    return [p for p in pieces if p]


@app.post("/generate")
async def generate(
    images: list[UploadFile] = File(default=[]),
    messages: str = Form(default="[]")
):
    try:
        try:
            history = json.loads(messages)
            if not isinstance(history, list):
                return JSONResponse(status_code=422, content={"error": "messages must be a JSON array"})
        except json.JSONDecodeError as e:
            return JSONResponse(status_code=422, content={"error": f"Invalid JSON format: {str(e)}"})

        # Load images
        pil_images = []
        for img in images:
            content = await img.read()
            pil_images.append(Image.open(io.BytesIO(content)).convert("RGB"))

        alternated = enforce_alternation(history)

        # Build multimodal chat structure
        structured_msgs = []
        for i, msg in enumerate(alternated):
            content = msg.get("content", "")
            role = msg.get("role", "user")

            if isinstance(content, list):
                parts = content
            else:
                parts = [{"type": "text", "text": content}]

            # Add uploaded images to latest user turn
            if role == "user" and i == len(alternated) - 1 and pil_images:
                for pimg in pil_images:
                    parts.append({"type": "image", "image": pimg})

            structured_msgs.append({"role": role, "content": parts})

        inputs = processor.apply_chat_template(
            structured_msgs,
            add_generation_prompt=True,
            tokenize=True,
            return_dict=True,
            return_tensors="pt",
        )
        inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

        # Use streamer + background generation thread
        streamer = TextIteratorStreamer(
            tokenizer=processor.tokenizer,
            skip_prompt=True,
            skip_special_tokens=True,
            timeout=60.0,
        )

        generation_kwargs = dict(
            **inputs,
            streamer=streamer,
            max_new_tokens=512,
            do_sample=False,
        )

        generation_error_q: Queue = Queue(maxsize=1)

        def run_generation():
            try:
                model.generate(**generation_kwargs)
            except Exception as gen_ex:
                generation_error_q.put(gen_ex)

        thread = Thread(target=run_generation, daemon=True)
        thread.start()

        async def token_stream():
            """
            NDJSON streaming:
              {"token":"..."} per chunk
              {"done":true} final marker
              {"error":"..."} on failure
            """
            try:
                while True:
                    # bubble generation thread exception quickly
                    try:
                        gen_ex = generation_error_q.get_nowait()
                        yield json.dumps({"error": f"Generation failed: {str(gen_ex)}"}) + "\n"
                        return
                    except Empty:
                        pass

                    # Try reading next model chunk
                    try:
                        new_text = next(streamer)
                    except StopIteration:
                        break
                    except Exception as stream_read_ex:
                        yield json.dumps({"error": f"Stream read failed: {str(stream_read_ex)}"}) + "\n"
                        return

                    if not new_text:
                        await asyncio.sleep(0.005)
                        continue

                    # Split to smaller chunks so frontend renders progressively
                    for piece in chunk_for_smooth_stream(new_text):
                        yield json.dumps({"token": piece}) + "\n"
                        # Tiny delay gives natural typing cadence and avoids burst flush
                        await asyncio.sleep(0.008)

                # Final done event
                yield json.dumps({"done": True}) + "\n"

            except Exception as stream_ex:
                print(f"Stream loop error: {str(stream_ex)}")
                yield json.dumps({"error": f"Stream interrupted: {str(stream_ex)}"}) + "\n"

        return StreamingResponse(token_stream(), media_type="application/x-ndjson")

    except Exception as global_ex:
        traceback.print_exc()
        return JSONResponse(
            status_code=500,
            content={"error": f"Internal Inference Core Engine Failure: {str(global_ex)}"},
        )


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)