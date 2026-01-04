# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/PrivateApi/PrivateApi.csproj", "src/PrivateApi/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/ServiceDefaults/ServiceDefaults.csproj", "src/ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/PrivateApi/PrivateApi.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/PrivateApi"
RUN dotnet build "PrivateApi.csproj" -c Release -o /app/build
RUN dotnet publish "PrivateApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published files
COPY --from=build /app/publish .

# Configure runtime
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PrivateApi.dll"]
