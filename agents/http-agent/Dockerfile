# Multi-stage Docker build for HTTP Agent
FROM golang:1.23-alpine AS builder

# Install build dependencies
RUN apk add --no-cache git ca-certificates

# Set working directory
WORKDIR /app

# Copy go mod files
COPY go.mod go.sum ./

# Download dependencies
RUN go mod download

# Copy source code
COPY . .

# Build the application
RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -ldflags '-extldflags "-static"' -o http-agent ./cmd/server/main.go

# Final stage
FROM alpine:latest

# Install ca-certificates for HTTPS requests
RUN apk --no-cache add ca-certificates

# Create non-root user
RUN addgroup -g 1000 httpagent && \
    adduser -D -u 1000 -G httpagent httpagent

WORKDIR /home/httpagent

# Copy binary from builder
COPY --from=builder /app/http-agent .

# Copy config example
COPY --from=builder /app/config/config.example.yaml ./config/

# Change ownership
RUN chown -R httpagent:httpagent /home/httpagent

# Switch to non-root user
USER httpagent

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Run the application
CMD ["./http-agent"]
