import io
import json
from threading import Thread
from fastapi import FastAPI, UploadFile, File, Form
from fastapi.responses import StreamingResponse
from PIL import Image
import torch
from transformers import AutoProcessor, AutoModelForVision2Seq, TextIteratorStreamer

app = FastAPI()

MODEL_ID = "google/medgemma-4b-it"   # <-- VERIFY EXACT HF MODEL ID
DEVICE = "cuda" if torch.cuda.is_available() else "cpu"

# Optionally use 4-bit quantization to reduce VRAM usage
load_in_4bit = True if DEVICE == "cuda" else False
model_kwargs = {"torch_dtype": torch.float16, "device_map": "auto"}
if load_in_4bit:
    model_kwargs["load_in_4bit"] = True
    model_kwargs["bnb_4bit_compute_dtype"] = torch.float16

processor = AutoProcessor.from_pretrained(MODEL_ID)
model = AutoModelForVision2Seq.from_pretrained(
    MODEL_ID,
    **model_kwargs
)

def stream_generate(prompt: str, images: list[Image.Image]):
    # Prepare inputs
    inputs = processor(
        text=prompt,
        images=images if images else None,
        return_tensors="pt"
    )
    # Move to appropriate device
    if not load_in_4bit:
        inputs = inputs.to(DEVICE)

    # Create streamer
    streamer = TextIteratorStreamer(processor, skip_special_tokens=True)
    generation_kwargs = {
        **inputs,
        "max_new_tokens": 1024,
        "temperature": 0.1,
        "do_sample": False,   # greedy for medical accuracy
        "streamer": streamer,
    }

    # Generate in a separate thread
    thread = Thread(target=model.generate, kwargs=generation_kwargs)
    thread.start()

    for new_token in streamer:
        # Send each token as a JSON line
        yield json.dumps({"token": new_token}) + "\n"

@app.post("/generate")
async def generate(
    prompt: str = Form(...),
    images: list[UploadFile] = File(default=[]),
):
    pil_images = []
    for img in images:
        content = await img.read()
        pil_images.append(Image.open(io.BytesIO(content)).convert("RGB"))

    return StreamingResponse(
        stream_generate(prompt, pil_images),
        media_type="application/x-ndjson"
    )

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)