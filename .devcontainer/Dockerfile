FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-bookworm

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        build-essential \
        curl \
        git \
        make \
        postgresql-client \
    && rm -rf /var/lib/apt/lists/*

USER vscode

ENV PATH="/home/vscode/.local/bin:/home/vscode/.dotnet/tools:${PATH}"

RUN curl -LsSf https://astral.sh/uv/install.sh | sh \
    && uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@v0.8.15 \
    && dotnet tool install --global dotnet-ef --version 10.0.8

WORKDIR /workspaces/RinhaDasLendas

