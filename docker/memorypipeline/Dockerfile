# docker build -f docker/webapi/Dockerfile -t chat-copilot-memorypipeline .

# builder
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS builder
WORKDIR /source
# generate dev-certs for https
RUN dotnet dev-certs https
# copy everything else and build app
COPY memorypipeline memorypipeline
COPY shared shared
RUN cd memorypipeline && \
  dotnet restore --use-current-runtime && \
  apt update && apt install -y wget && \
  dotnet publish --use-current-runtime --self-contained false --no-restore -o /app && \
  mkdir /app/data && \
  wget -P /app/data https://raw.githubusercontent.com/tesseract-ocr/tessdata/main/eng.traineddata


# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=builder /app .
COPY --from=builder /root/.dotnet/corefx/cryptography/x509stores/my/* /root/.dotnet/corefx/cryptography/x509stores/my/
RUN apt update && \
  apt install -y libleptonica-dev libtesseract-dev libc6-dev libjpeg62-turbo-dev libgdiplus && \
  ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 x64/libleptonica-1.82.0.so && \
  ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.4.0.1 x64/libtesseract50.so
ENTRYPOINT ["./CopilotChatMemoryPipeline"]