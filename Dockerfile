# Stage 1: Build
FROM node:20-alpine AS builder

# Set working directory
WORKDIR /app

# Copy files
COPY package.json package-lock.json ./
COPY public ./public
COPY src ./src
COPY eslint.config.js .
COPY index.html .
COPY vite.config.js .

# Install deps & build
RUN npm install
RUN npm run build

# Stage 2: Serve with http-server
FROM node:20-alpine

# Install http-server globally
RUN npm install -g http-server

# Set working dir
WORKDIR /app

# Copy built files from builder
COPY --from=builder /app/dist .

# Expose port
EXPOSE 8080

# Run static server
CMD ["http-server", ".", "-p", "8080"]
