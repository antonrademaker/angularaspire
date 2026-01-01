# Build stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files
COPY src/PublicApp/package*.json ./

# Install dependencies
RUN npm ci

# Copy source files
COPY src/PublicApp/ .

# Build the Angular application
RUN npm run build -- --configuration production

# Runtime stage
FROM nginx:alpine AS final

# Copy nginx configuration
COPY src/PublicApp/nginx.conf /etc/nginx/nginx.conf

# Copy built application
COPY --from=build /app/dist/public-app/browser /usr/share/nginx/html

# Create non-root user for security
RUN addgroup -g 1001 appgroup && \
    adduser -D -u 1001 -G appgroup appuser && \
    chown -R appuser:appgroup /var/cache/nginx /var/run /var/log/nginx /usr/share/nginx/html

USER appuser

EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
