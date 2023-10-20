# docker build -f docker/plugins/web-searcher/Dockerfile -t web-searcher .

# builder
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS installer-env
ARG AZURE_FUNCTION_MASTER_KEY
WORKDIR /source
COPY plugins/shared shared
COPY plugins/web-searcher web-searcher
RUN cd /source/web-searcher && \
  mkdir -p /home/site/wwwroot && \
  dotnet publish *.csproj --output /home/site/wwwroot && \
  mkdir -p /azure-functions-host/Secrets/ && \
  echo "{\"masterKey\":{\"name\":\"master\",\"value\":\"$AZURE_FUNCTION_MASTER_KEY\",\"encrypted\":false},\"functionKeys\":[]}" > /azure-functions-host/Secrets/host.json

# final stage/image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4.0-dotnet-isolated6.0-appservice
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
  AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
  AzureWebJobsSecretStorageType=files
COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
COPY --from=installer-env ["/azure-functions-host/Secrets", "/azure-functions-host/Secrets"]


