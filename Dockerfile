# Stage 1: Buill
FROM node:20-alpine AS builder

# Set working directory
WORKDIR /app

# Copy package.json and package-lock.json
COPY package.json ./package.json
COPY package-lock.json ./package-lock.json
COPY public ./public
COPY src ./src
COPY  eslint.config.js ./eslint.config.js
COPY  index.html ./index.html
COPY  vite.config.js ./vite.config.js
# Install dependencies
RUN npm install
RUN npm run build

# Expose Vite's default port
EXPOSE 5173

# Run Vite dev server
CMD ["npm", "run", "dev"]