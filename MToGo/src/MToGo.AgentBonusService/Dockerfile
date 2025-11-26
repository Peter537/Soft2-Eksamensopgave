# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["src/MToGo.Shared/MToGo.Shared.csproj", "src/MToGo.Shared/"]
COPY ["src/MToGo.AgentBonusService/MToGo.AgentBonusService.csproj", "src/MToGo.AgentBonusService/"]
RUN dotnet restore "src/MToGo.AgentBonusService/MToGo.AgentBonusService.csproj"
# Only copy the necessary directories
COPY ["Directory.Packages.props", "."]
COPY ["src/MToGo.Shared", "src/MToGo.Shared/"]
COPY ["src/MToGo.AgentBonusService", "src/MToGo.AgentBonusService/"]
WORKDIR "/src/src/MToGo.AgentBonusService"
RUN dotnet build "MToGo.AgentBonusService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MToGo.AgentBonusService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN groupadd -r appgroup && useradd -r -g appgroup appuser
USER appuser
EXPOSE 8080
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MToGo.AgentBonusService.dll"]