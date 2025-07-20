# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Lấy PORT từ biến môi trường Render
ENV ASPNETCORE_URLS=http://+:$PORT

EXPOSE 7000
ENTRYPOINT ["dotnet", "Integration-System.dll"]
