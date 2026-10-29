# The Dialogporten agent image, built on the agentctl base image. The base carries the agent user, the harnesses at
# the versions agentctl is tested with, Node.js, gh, asciinema and agg and the common build tools, and offers the
# browser, containers and desktop as features:
# https://github.com/digdir/digdir-agents/tree/main/images/agentctl-base
# Renovate moves the digest, and with it the harness versions.
FROM ghcr.io/digdir/agentctl-base:ubuntu-26.04@sha256:2ee51d4619a9e13b1d1e6037b48c76fcc6bbed46ef6203aadde1270c1a2fb790 AS minimal

# The base ends as the unprivileged agent user; installing packages needs root again.
USER root

ENV DOTNET_ROOT=/usr/local/share/dotnet
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_NOLOGO=1

# zsh runs scripts/e2e/run-webapi-e2e.zsh, openssl generates the local Ed25519 key pair at boot, and the Postgres
# client inspects the database the E2E stack starts.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends \
        libicu-dev \
        openssl \
        postgresql-client \
        zsh \
    && rm -rf /var/lib/apt/lists/*

# Norwegian local time, as the team's own machines keep.
RUN ln -sf /usr/share/zoneinfo/Europe/Oslo /etc/localtime

ARG TARGETARCH
# The SDK global.json pins, with rollForward disabled, so it must match exactly. Move it together with global.json.
ARG DOTNET_SDK_VERSION=10.0.402
ARG DOTNET_INSTALL_SCRIPT_COMMIT=6f559c420847ded38591392dafe785ad511f39f5
ARG DOTNET_INSTALL_SCRIPT_SHA256=082f7685e156738a1b2e2ed8381a621870d4ce8e8c59278034556f05c186eb2e

RUN case "${TARGETARCH}" in \
        amd64) DOTNET_ARCH=x64 ;; \
        arm64) DOTNET_ARCH=arm64 ;; \
        *) echo "unsupported target architecture: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL \
        "https://raw.githubusercontent.com/dotnet/install-scripts/${DOTNET_INSTALL_SCRIPT_COMMIT}/src/dotnet-install.sh" \
        -o /tmp/dotnet-install.sh \
    && echo "${DOTNET_INSTALL_SCRIPT_SHA256}  /tmp/dotnet-install.sh" | sha256sum -c - \
    && bash /tmp/dotnet-install.sh \
        --architecture "${DOTNET_ARCH}" \
        --install-dir "${DOTNET_ROOT}" \
        --no-path \
        --version "${DOTNET_SDK_VERSION}" \
    && ln -s "${DOTNET_ROOT}/dotnet" /usr/local/bin/dotnet \
    && install -d /etc/dotnet \
    && printf '%s\n' "${DOTNET_ROOT}" >/etc/dotnet/install_location \
    && rm -f /tmp/dotnet-install.sh

# dotnet-ef at the version of the repository's Microsoft.EntityFrameworkCore packages, installed to a shared path
# because the host resynchronizes the agent's home directory. Root steps that run the SDK use a scratch HOME, so no
# root-owned cache is left in the agent's.
ARG DOTNET_EF_VERSION=10.0.12

RUN set -eu; \
    scratch="$(mktemp -d)"; \
    export HOME="${scratch}" TMPDIR="${scratch}" XDG_RUNTIME_DIR="${scratch}"; \
    dotnet tool install dotnet-ef --tool-path /usr/local/share/dotnet-tools --version "${DOTNET_EF_VERSION}"; \
    ln -s /usr/local/share/dotnet-tools/dotnet-ef /usr/local/bin/dotnet-ef; \
    find /usr/local/share/dotnet-tools/.store -name '*.nupkg' -delete; \
    dotnet --list-sdks | grep -q "^${DOTNET_SDK_VERSION} "; \
    dotnet-ef --version; \
    rm -rf "${scratch}"

# Podman, with the `docker` command and Docker Compose, for Testcontainers in the integration tests, the E2E
# launcher's docker-compose-db-redis.yml and building this image in a nested agent.
RUN agent-feature install containers

# The overlay storage driver through fuse-overlayfs, since the agent's root filesystem is itself overlayfs.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends fuse-overlayfs \
    && rm -rf /var/lib/apt/lists/*
COPY common/containers-storage.conf /etc/containers/storage.conf

# agentctl and agentd run nested agents, so this agent can try changes to its own definition.
COPY --from=ghcr.io/digdir/agentctl:v0.1.0-preview.13@sha256:5451dd5e912865d7ed4544f0bf2e6d0912cd274b878515224cd05896a286b28f \
    /agentctl /agentd /usr/local/bin/

# The repositories to clone at boot, and the boot unit that prepares local development: an HTTPS dev certificate
# trusted system-wide, a local Ed25519 key pair and the local database connection string in user secrets.
COPY common/workspace.list /etc/agent-workspace.d/dialogporten.list
COPY common/dialogporten-dev-init.service /etc/systemd/system/agent-dialogporten-dev-init.service
COPY --chmod=0755 common/dialogporten-dev-init.sh /usr/local/libexec/agent-dialogporten-dev-init

RUN systemctl enable agent-dialogporten-dev-init.service \
    && scratch="$(mktemp -d)" \
    && HOME="${scratch}" TMPDIR="${scratch}" sh -c ' \
        agentctl --version \
        && agentd --version \
        && podman --version \
        && fuse-overlayfs --version \
        && zsh --version \
        && psql --version' \
    && rm -rf "${scratch}" \
    && agent-image-check

USER agent
