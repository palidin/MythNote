# Multi-platform Dockerfile for MythNote
# Build for multiple platforms including macOS

# Use multi-stage builds for different platforms
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /app

# Copy project files
COPY *.csproj ./
COPY ./Assets ./Assets
COPY ./Services ./Services
COPY ./ViewModels ./ViewModels
COPY ./Views ./Views
COPY ./App.axaml* ./
COPY ./Program.cs ./

# Restore dependencies
RUN dotnet restore

# Build for different platforms
RUN dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/win-x64
RUN dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/osx-x64
RUN dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ./publish/osx-arm64
RUN dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/linux-x64

# Runtime image for packaging
FROM ubuntu:22.04 AS packager

WORKDIR /app

# Install packaging tools
RUN apt-get update && apt-get install -y zip && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=builder /app/publish ./publish

# Create archives
RUN cd publish/win-x64 && zip -r ../../mythnote-windows-x64.zip .
RUN cd publish/osx-x64 && zip -r ../../mythnote-macos-x64.zip .
RUN cd publish/osx-arm64 && zip -r ../../mythnote-macos-arm64.zip .
RUN cd publish/linux-x64 && zip -r ../../mythnote-linux-x64.zip .

# Output
CMD ["ls", "-la", "*.zip"]
