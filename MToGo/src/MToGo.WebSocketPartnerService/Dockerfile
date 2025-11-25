# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["src/MToGo.Shared/MToGo.Shared.csproj", "src/MToGo.Shared/"]
COPY ["src/MToGo.WebSocketPartnerService/MToGo.WebSocketPartnerService.csproj", "src/MToGo.WebSocketPartnerService/"]
RUN dotnet restore "src/MToGo.WebSocketPartnerService/MToGo.WebSocketPartnerService.csproj"
# Only copy the necessary directories
COPY ["Directory.Packages.props", "."]
COPY ["src/MToGo.Shared", "src/MToGo.Shared/"]
COPY ["src/MToGo.WebSocketPartnerService", "src/MToGo.WebSocketPartnerService/"]
WORKDIR "/src/MToGo.WebSocketPartnerService"
RUN dotnet build "MToGo.WebSocketPartnerService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MToGo.WebSocketPartnerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN groupadd -r appgroup && useradd -r -g appgroup appuser
USER appuser
EXPOSE 8080
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MToGo.WebSocketPartnerService.dll"]