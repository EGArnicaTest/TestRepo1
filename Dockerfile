FROM registry-app.eng.qops.net:5001/python-3.13/gold/bookworm-slim-python-3.13-gold:1753100295

# Switch to root for system-level operations
USER root

WORKDIR /tmp

ARG PIP_VERSION
ARG POETRY_VERSION

# Environment variables for Python
ENV PYTHONFAULTHANDLER=1 \
    PYTHONUNBUFFERED=1 \
    PYTHONHASHSEED=random \
    # prevents python creating .pyc files
    PYTHONDONTWRITEBYTECODE=1 \
    PIP_DEFAULT_TIMEOUT=100 \
    POETRY_VIRTUALENVS_CREATE=false

# Install system dependencies
RUN apt-get update \
    && apt-get upgrade -y \
    && apt-get install -y \
        git \
    && rm -rf /var/lib/apt/lists/*

COPY pip.conf .

# Install Python dependencies
RUN pip install --upgrade pip==$PIP_VERSION --no-cache-dir
RUN PIP_CONFIG_FILE=pip.conf pip install poetry==$POETRY_VERSION --no-cache-dir

# Copy only requirements to cache them in docker layer
COPY poetry.lock pyproject.toml poetry.toml /tmp/

# Project initialization
RUN python3 -m poetry install --no-interaction --no-ansi --verbose --no-root

# Configure git for security
RUN git config --system --add safe.directory '*'

# Switch back to non-root user for security
USER user
