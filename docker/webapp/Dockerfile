# docker build -f docker/webapp/Dockerfile -t chat-copilot-webapp .

# builder
FROM node:lts-alpine as builder
WORKDIR /app
COPY webapp/ .
RUN yarn install \
  --prefer-offline \
  --frozen-lockfile \
  --non-interactive \
  --production=false

# final stage/image
FROM node:lts-alpine
WORKDIR /app
COPY --from=builder /app  .
ENV HOST 0.0.0.0
EXPOSE 3000
ENTRYPOINT [ "yarn", "start" ]