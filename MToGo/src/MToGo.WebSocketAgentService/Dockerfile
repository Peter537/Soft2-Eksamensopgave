# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["src/MToGo.Shared/MToGo.Shared.csproj", "src/MToGo.Shared/"]
COPY ["src/MToGo.WebSocketAgentService/MToGo.WebSocketAgentService.csproj", "src/MToGo.WebSocketAgentService/"]
RUN dotnet restore "src/MToGo.WebSocketAgentService/MToGo.WebSocketAgentService.csproj"
# Only copy the necessary directories
COPY ["Directory.Packages.props", "."]
COPY ["src/MToGo.Shared", "src/MToGo.Shared/"]
COPY ["src/MToGo.WebSocketAgentService", "src/MToGo.WebSocketAgentService/"]
WORKDIR "/src/src/MToGo.WebSocketAgentService"
RUN dotnet build "MToGo.WebSocketAgentService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MToGo.WebSocketAgentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN groupadd -r appgroup && useradd -r -g appgroup appuser
USER appuser
EXPOSE 8080
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MToGo.WebSocketAgentService.dll"]