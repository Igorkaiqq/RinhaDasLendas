FROM node:24-alpine AS build
WORKDIR /app
ARG VITE_API_URL=/api
ARG VITE_ENABLE_FAKE_FALLBACK=false
ENV VITE_API_URL=$VITE_API_URL \
    VITE_ENABLE_FAKE_FALLBACK=$VITE_ENABLE_FAKE_FALLBACK
COPY FrontEnd/package*.json ./
RUN npm ci
COPY FrontEnd/ ./
RUN npm run build

FROM nginx:1.29-alpine AS runtime
COPY FrontEnd/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 CMD wget -qO- http://localhost/health || exit 1
