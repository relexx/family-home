# FamilyHome DevContainer: .NET 10 + Android-Workload + JDK 17 + Android SDK

# Scanners of the Security Gate (scripts/security-gate.sh): the same images and versions as .github/workflows/security-lint.yml
# (.github/workflows/devcontainer.yml checks that they match). The container has no Docker, so the binaries are copied.
FROM zricethezav/gitleaks:v8.30.1@sha256:c00b6bd0aeb3071cbcb79009cb16a60dd9e0a7c60e2be9ab65d25e6bc8abbb7f AS gitleaks
FROM hadolint/hadolint:v2.15.1@sha256:32dac94127fd60b7b7e3fbfc65e1383b9b5e25c9bfd7b8536de7a539fe68a12d AS hadolint
FROM rhysd/actionlint:1.7.12@sha256:b1934ee5f1c509618f2508e6eb47ee0d3520686341fec936f3b79331f9315667 AS actionlint
FROM koalaman/shellcheck:v0.11.0@sha256:61862eba1fcf09a484ebcc6feea46f1782532571a34ed51fedf90dd25f925a8d AS shellcheck

FROM mcr.microsoft.com/devcontainers/base:2.0-ubuntu-24.04@sha256:d94c97dd9cacf183d0a6fd12a8e87b526e9e928307674ae9c94139139c0c6eae

ARG DOTNET_CHANNEL=10.0
ARG ANDROID_CMDLINE_TOOLS=16111833
ARG ANDROID_PLATFORM=android-36
ARG ANDROID_BUILD_TOOLS=36.0.0

ENV DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64 \
    ANDROID_HOME=/opt/android-sdk \
    ANDROID_SDK_ROOT=/opt/android-sdk
ENV PATH=${PATH}:${DOTNET_ROOT}:/home/vscode/.dotnet/tools:${ANDROID_HOME}/cmdline-tools/latest/bin:${ANDROID_HOME}/platform-tools

SHELL ["/bin/bash", "-o", "pipefail", "-c"]

# Dev-Image: Pakete bewusst nicht versionsgepinnt (folgen den Ubuntu-Sicherheitsupdates beim Rebuild)
# hadolint ignore=DL3008
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        openjdk-17-jdk-headless unzip curl ca-certificates libicu74 \
        postgresql-client sqlite3 pipx \
        iptables ipset dnsutils aggregate jq \
    && rm -rf /var/lib/apt/lists/*

# .NET SDK
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --channel ${DOTNET_CHANNEL} --install-dir ${DOTNET_ROOT} \
    && ln -sf ${DOTNET_ROOT}/dotnet /usr/local/bin/dotnet \
    && rm /tmp/dotnet-install.sh \
    && dotnet workload install android \
    && dotnet --info

# Android SDK (Command-line tools, Platform, Build-Tools, Platform-Tools/adb)
RUN mkdir -p ${ANDROID_HOME}/cmdline-tools \
    && curl -fsSL https://dl.google.com/android/repository/commandlinetools-linux-${ANDROID_CMDLINE_TOOLS}_latest.zip -o /tmp/clt.zip \
    && unzip -q /tmp/clt.zip -d ${ANDROID_HOME}/cmdline-tools \
    && mv ${ANDROID_HOME}/cmdline-tools/cmdline-tools ${ANDROID_HOME}/cmdline-tools/latest \
    && rm /tmp/clt.zip \
    # "yes" endet mit SIGPIPE, sobald sdkmanager fertig ist – mit pipefail sonst Exit 141
    && (yes || true) | sdkmanager --licenses > /dev/null \
    && sdkmanager "platform-tools" "platforms;${ANDROID_PLATFORM}" "build-tools;${ANDROID_BUILD_TOOLS}" \
    && chown -R vscode:vscode ${ANDROID_HOME}

# Security Gate scanners, root-owned so that code in the container cannot change them
ARG SEMGREP_VERSION=1.182.1
ARG ZIZMOR_VERSION=1.31.1
COPY --from=gitleaks --chmod=0755 /usr/bin/gitleaks /usr/local/bin/gitleaks
COPY --from=hadolint --chmod=0755 /bin/hadolint /usr/local/bin/hadolint
COPY --from=actionlint --chmod=0755 /usr/local/bin/actionlint /usr/local/bin/actionlint
COPY --from=shellcheck --chmod=0755 /bin/shellcheck /usr/local/bin/shellcheck
RUN export PIPX_HOME=/opt/pipx PIPX_BIN_DIR=/usr/local/bin \
    && pipx install "semgrep==${SEMGREP_VERSION}" \
    && pipx install "zizmor==${ZIZMOR_VERSION}"

# Egress firewall (docs/SICHERHEIT.md ch. 8), started as root by docker-compose.yml; not writable for the user
COPY --chmod=0755 init-firewall.sh /usr/local/bin/init-firewall.sh

# No sudo for the user: the firewall must not be switchable off from inside. Tools come only through this file.
# Directories for the named volumes belong to the user, so that new volumes do too (no chown at runtime).
RUN rm -f /etc/sudoers.d/vscode \
    && install -d -o vscode -g vscode /home/vscode/.nuget /home/vscode/.claude \
    && install -d -m 700 -o vscode -g vscode /home/vscode/.signing

USER vscode
RUN dotnet tool install --global dotnet-ef
