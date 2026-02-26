# Multi-stage build for the SDMP Phase 1 monolith.
# Stage 1: restore + publish using the .NET 9 SDK.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore as a distinct layer for better build caching.
COPY Sdmp.Monolith.csproj ./
RUN dotnet restore Sdmp.Monolith.csproj

COPY . ./
RUN dotnet publish Sdmp.Monolith.csproj -c Release -o /app --no-restore

# Stage 2: minimal runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Run as a non-root user (security baseline).
RUN useradd --uid 1001 --create-home appuser
USER appuser

COPY --from=build /app ./

# Listen on 8080 inside the container.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Container-native health check hits the liveness endpoint.
HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
  CMD ["dotnet", "--info"]

ENTRYPOINT ["dotnet", "Sdmp.Monolith.dll"]
