import io
import os
import re
import csv
import json
import traceback
import asyncio
from threading import Thread
from queue import Queue, Empty
from typing import List, Tuple

from fastapi import FastAPI, UploadFile, File, Form
from fastapi.responses import StreamingResponse, JSONResponse
from PIL import Image
import torch
from transformers import AutoProcessor, AutoModelForImageTextToText, TextIteratorStreamer
from huggingface_hub import login

# Optional deps for document extraction
# pip install pypdf python-docx
try:
    from pypdf import PdfReader
except Exception:
    PdfReader = None

try:
    import docx  # python-docx
except Exception:
    docx = None

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

MAX_DOC_CHARS_PER_FILE = 12000
MAX_DOC_TOTAL_CHARS = 50000


def enforce_alternation(messages: list[dict]) -> list[dict]:
    merged = []
    for msg in messages:
        role = msg.get("role", "user")
        content = msg.get("content", "")

        if merged and merged[-1]["role"] == role:
            if isinstance(merged[-1]["content"], str) and isinstance(content, str):
                merged[-1]["content"] += "\n" + content
            else:
                merged.append({"role": role, "content": content})
        else:
            merged.append({"role": role, "content": content})
    return merged


def normalize_text(text: str) -> str:
    text = text.replace("\x00", " ")
    text = re.sub(r"[ \t]+", " ", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def safe_decode_bytes(data: bytes) -> str:
    for enc in ("utf-8", "utf-16", "latin-1"):
        try:
            return data.decode(enc)
        except Exception:
            pass
    return data.decode("latin-1", errors="ignore")


def truncate_text(text: str, max_chars: int) -> str:
    if len(text) <= max_chars:
        return text
    return text[:max_chars] + "\n...[TRUNCATED]"


def extract_text_from_txt(file_bytes: bytes) -> str:
    return normalize_text(safe_decode_bytes(file_bytes))


def extract_text_from_csv(file_bytes: bytes) -> str:
    raw = safe_decode_bytes(file_bytes)
    lines = []
    reader = csv.reader(io.StringIO(raw))
    for row in reader:
        lines.append(" | ".join(cell.strip() for cell in row))
    return normalize_text("\n".join(lines))


def extract_text_from_pdf(file_bytes: bytes) -> str:
    if PdfReader is None:
        return "[PDF text extraction unavailable: install pypdf]"

    text_chunks = []
    try:
        reader = PdfReader(io.BytesIO(file_bytes))
        for page in reader.pages:
            t = page.extract_text() or ""
            if t.strip():
                text_chunks.append(t)
    except Exception as ex:
        return f"[PDF parsing failed: {str(ex)}]"

    if not text_chunks:
        return "[No extractable text found in PDF. It may be a scanned PDF.]"

    return normalize_text("\n\n".join(text_chunks))


def extract_text_from_docx(file_bytes: bytes) -> str:
    if docx is None:
        return "[DOCX extraction unavailable: install python-docx]"

    try:
        d = docx.Document(io.BytesIO(file_bytes))
        text = "\n".join(p.text for p in d.paragraphs if p.text and p.text.strip())
        if not text.strip():
            return "[DOCX contains no readable paragraph text]"
        return normalize_text(text)
    except Exception as ex:
        return f"[DOCX parsing failed: {str(ex)}]"


def extract_text_from_document(filename: str, content_type: str, file_bytes: bytes) -> str:
    name = (filename or "").lower()
    ctype = (content_type or "").lower()

    # Extension-first
    if name.endswith(".txt"):
        return extract_text_from_txt(file_bytes)
    if name.endswith(".csv"):
        return extract_text_from_csv(file_bytes)
    if name.endswith(".pdf"):
        return extract_text_from_pdf(file_bytes)
    if name.endswith(".docx"):
        return extract_text_from_docx(file_bytes)

    # MIME fallback
    if "text/plain" in ctype:
        return extract_text_from_txt(file_bytes)
    if "text/csv" in ctype:
        return extract_text_from_csv(file_bytes)
    if "pdf" in ctype:
        return extract_text_from_pdf(file_bytes)

    return "[Unsupported document type for parser]"


def chunk_for_smooth_stream(text: str):
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
        if ch in {" ", "\n", "\t", ".", ",", ";", ":", "!", "?", ")", "]", "}"}:
            flush()

    flush()
    return [p for p in pieces if p]


@app.post("/generate")
async def generate(
    images: List[UploadFile] = File(default=[]),  # keep key name aligned with your C# multipart: "images"
    messages: str = Form(default="[]")
):
    """
    Routing:
      - images (jpg/jpeg/png): sent directly to MedGemma as image parts
      - documents (pdf/txt/csv/docx/...): text extracted and appended to latest user message
    """
    try:
        # Parse JSON message history
        try:
            history = json.loads(messages)
            if not isinstance(history, list):
                return JSONResponse(status_code=422, content={"error": "messages must be a JSON array"})
        except json.JSONDecodeError as e:
            return JSONResponse(status_code=422, content={"error": f"Invalid JSON format: {str(e)}"})

        # Split uploads into model-images vs text-extracted docs
        image_inputs: List[Image.Image] = []
        extracted_docs: List[Tuple[str, str]] = []  # (filename, extracted_text)

        for upl in images:
            file_bytes = await upl.read()
            ctype = (upl.content_type or "").lower()
            fname = upl.filename or "unknown"

            is_image = ctype in {"image/jpeg", "image/jpg", "image/png"} or fname.lower().endswith(
                (".jpg", ".jpeg", ".png")
            )

            if is_image:
                try:
                    image_inputs.append(Image.open(io.BytesIO(file_bytes)).convert("RGB"))
                except Exception as ex:
                    extracted_docs.append((fname, f"[Image decode failed: {str(ex)}]"))
                continue

            # non-image document path
            text = extract_text_from_document(fname, ctype, file_bytes)
            text = truncate_text(text, MAX_DOC_CHARS_PER_FILE)
            extracted_docs.append((fname, text))

        # Build plain doc context text (no extra role injection)
        doc_context = ""
        if extracted_docs:
            blocks = []
            total_chars = 0
            for idx, (name, text) in enumerate(extracted_docs, start=1):
                block = f"[Document {idx}: {name}]\n{text}\n"
                if total_chars + len(block) > MAX_DOC_TOTAL_CHARS:
                    remain = MAX_DOC_TOTAL_CHARS - total_chars
                    if remain > 200:
                        blocks.append(truncate_text(block, remain))
                    blocks.append("\n...[DOCUMENT CONTEXT TRUNCATED: SIZE LIMIT]...")
                    break
                blocks.append(block)
                total_chars += len(block)

            doc_context = "\n".join(blocks)

        alternated = enforce_alternation(history)

        # Build structured chat without breaking strict role alternation
        structured_msgs = []
        for i, msg in enumerate(alternated):
            role = msg.get("role", "user")
            content = msg.get("content", "")

            # Preserve list content if caller already provided multimodal parts
            if isinstance(content, list):
                parts = content
            else:
                text_content = str(content)

                # Append extracted docs only to latest user message
                if doc_context and role == "user" and i == len(alternated) - 1:
                    text_content = (
                        f"{text_content}\n\n"
                        f"---\n"
                        f"EXTRACTED DOCUMENT TEXT (from uploaded files):\n"
                        f"{doc_context}\n"
                        f"---"
                    )

                parts = [{"type": "text", "text": text_content}]

            # Attach only real image uploads to latest user message
            if role == "user" and i == len(alternated) - 1 and image_inputs:
                for img in image_inputs:
                    parts.append({"type": "image", "image": img})

            structured_msgs.append({"role": role, "content": parts})

        # Tokenize and generate
        inputs = processor.apply_chat_template(
            structured_msgs,
            add_generation_prompt=True,
            tokenize=True,
            return_dict=True,
            return_tensors="pt",
        )
        inputs = {k: v.to(DEVICE) for k, v in inputs.items()}

        streamer = TextIteratorStreamer(
            tokenizer=processor.tokenizer,
            skip_prompt=True,
            skip_special_tokens=True,
            timeout=60.0,
        )

        generation_kwargs = dict(
            **inputs,
            streamer=streamer,
            max_new_tokens=1024,
            do_sample=False,
        )

        generation_error_q: Queue = Queue(maxsize=1)
        generation_finished_flag = {"done": False}

        def run_generation():
            try:
                model.generate(**generation_kwargs)
            except Exception as gen_ex:
                generation_error_q.put(gen_ex)
            except Exception as gen_ex:
                import traceback
                generation_error_q.put("".join(traceback.format_exception(type(gen_ex), gen_ex, gen_ex.__traceback__)))
            finally:
                generation_finished_flag["done"] = True

        Thread(target=run_generation, daemon=True).start()

        async def token_stream():
            """
            NDJSON stream:
              {"token":"..."} per piece
              {"done": true} at complete end
              {"error":"..."} on failure
            """
            try:
                idle_loops = 0
                max_idle_loops = 2000  # ~6s at 5ms sleep
                collected_any = False

                while True:
                    # Bubble generation error
                    try:
                        gen_ex = generation_error_q.get_nowait()
                        yield json.dumps({"error": f"Generation failed: {str(gen_ex)}"}) + "\n"
                        return
                    except Empty:
                        pass

                    got_text = False
                    new_text = None
                    try:
                        new_text = next(streamer)
                        got_text = bool(new_text)
                    except StopIteration:
                        new_text = None
                    except Exception as stream_read_ex:
                        yield json.dumps({"error": f"Stream read failed: {str(stream_read_ex)}"}) + "\n"
                        return
                    except Exception as stream_read_ex:
                        import traceback
                        err = "".join(traceback.format_exception(type(stream_read_ex), stream_read_ex, stream_read_ex.__traceback__))
                        yield json.dumps({"error": f"Stream read failed: {err}"}) + "\n"
                        return
                    

                    if got_text and new_text:
                        idle_loops = 0
                        collected_any = True
                        for piece in chunk_for_smooth_stream(new_text):
                            yield json.dumps({"token": piece}) + "\n"
                            await asyncio.sleep(0.008)
                        continue

                    idle_loops += 1

                    if generation_finished_flag["done"] and idle_loops > 2:
                        break

                    if idle_loops >= max_idle_loops:
                        if collected_any:
                            break
                        yield json.dumps({"error": "Stream timeout while waiting for model output"}) + "\n"
                        return

                    await asyncio.sleep(0.005)

                # Final flush for residual buffered streamer chunks
                try:
                    while True:
                        tail = next(streamer)
                        if not tail:
                            break
                        for piece in chunk_for_smooth_stream(tail):
                            yield json.dumps({"token": piece}) + "\n"
                            await asyncio.sleep(0.005)
                except StopIteration:
                    pass
                except Exception:
                    pass

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