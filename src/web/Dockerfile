# syntax=docker/dockerfile:1
FROM node:24 AS build
WORKDIR /source

COPY --link package.json package-lock.json ./
RUN --mount=type=cache,target=/root/.npm \
    npm ci

COPY --link . .
RUN npx ng build

FROM nginx/nginx-unprivileged:1.30-alpine
COPY --link nginx.conf /etc/nginx/conf.d/default.conf
COPY --link --from=build /source/dist/chronos-web/browser /usr/share/nginx/html
EXPOSE 8080
