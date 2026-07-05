FROM nvidia/cuda:12.1.0-cudnn8-runtime-ubuntu22.04

# System dependencies
RUN apt-get update && apt-get install -y \
    python3.10 python3-pip \
    && rm -rf /var/lib/apt/lists/*

RUN update-alternatives --install /usr/bin/python python /usr/bin/python3.10 1

WORKDIR /app

# Install PyTorch 2.4.0 with CUDA 12.1 (compatible with transformers>=4.45)
RUN pip install --no-cache-dir \
    torch==2.4.0 \
    torchvision==0.19.0 \
    --index-url https://download.pytorch.org/whl/cu121

# Force NumPy <2 to avoid the "ARRAY_API" error
RUN pip install --no-cache-dir "numpy<2"

# Copy and install remaining requirements (transformers, etc.)
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY main.py .
EXPOSE 8000
CMD ["python", "main.py"]