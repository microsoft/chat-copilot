# docker build -f docker/webapi/Dockerfile -t chat-copilot-webapi .

# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

# generate dev-certs for https
RUN dotnet dev-certs https

# copy csproj and restore as distinct layers
COPY webapi/*.csproj .
RUN dotnet restore --use-current-runtime

# copy everything else and build app
COPY webapi/. .
RUN apt update && apt install -y wget
RUN wget -P data https://raw.githubusercontent.com/tesseract-ocr/tessdata/main/eng.traineddata
RUN dotnet publish --use-current-runtime --self-contained false --no-restore -o /app


# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
ENV Kestrel__Endpoints__Http__Url=http://0.0.0.0:8080
WORKDIR /app
COPY --from=build /app .
COPY --from=build /root/.dotnet/corefx/cryptography/x509stores/my/* /root/.dotnet/corefx/cryptography/x509stores/my/

ENTRYPOINT ["./CopilotChatWebApi"]