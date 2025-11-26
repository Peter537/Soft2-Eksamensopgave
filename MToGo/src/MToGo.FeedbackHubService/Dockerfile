# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["src/MToGo.Shared/MToGo.Shared.csproj", "src/MToGo.Shared/"]
COPY ["src/MToGo.FeedbackHubService/MToGo.FeedbackHubService.csproj", "src/MToGo.FeedbackHubService/"]
RUN dotnet restore "src/MToGo.FeedbackHubService/MToGo.FeedbackHubService.csproj"
# Only copy the necessary directories
COPY ["Directory.Packages.props", "."]
COPY ["src/MToGo.Shared", "src/MToGo.Shared/"]
COPY ["src/MToGo.FeedbackHubService", "src/MToGo.FeedbackHubService/"]
WORKDIR "/src/src/MToGo.FeedbackHubService"
RUN dotnet build "MToGo.FeedbackHubService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MToGo.FeedbackHubService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN groupadd -r appgroup && useradd -r -g appgroup appuser
USER appuser
EXPOSE 8080
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MToGo.FeedbackHubService.dll"]