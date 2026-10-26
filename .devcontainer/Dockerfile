# FamilyHome DevContainer: .NET 10 + Android-Workload + JDK 17 + Android SDK
FROM mcr.microsoft.com/devcontainers/base:2.0-ubuntu-24.04

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
    && yes | sdkmanager --licenses > /dev/null \
    && sdkmanager "platform-tools" "platforms;${ANDROID_PLATFORM}" "build-tools;${ANDROID_BUILD_TOOLS}" \
    && chown -R vscode:vscode ${ANDROID_HOME}

USER vscode
RUN dotnet tool install --global dotnet-ef
