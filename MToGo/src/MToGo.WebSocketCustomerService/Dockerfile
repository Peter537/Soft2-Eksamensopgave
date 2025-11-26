# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["src/MToGo.Shared/MToGo.Shared.csproj", "src/MToGo.Shared/"]
COPY ["src/MToGo.WebSocketCustomerService/MToGo.WebSocketCustomerService.csproj", "src/MToGo.WebSocketCustomerService/"]
RUN dotnet restore "src/MToGo.WebSocketCustomerService/MToGo.WebSocketCustomerService.csproj"
# Only copy the necessary directories
COPY ["Directory.Packages.props", "."]
COPY ["src/MToGo.Shared", "src/MToGo.Shared/"]
COPY ["src/MToGo.WebSocketCustomerService", "src/MToGo.WebSocketCustomerService/"]
WORKDIR "/src/src/MToGo.WebSocketCustomerService"
RUN dotnet build "MToGo.WebSocketCustomerService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MToGo.WebSocketCustomerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN groupadd -r appgroup && useradd -r -g appgroup appuser
USER appuser
EXPOSE 8080
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MToGo.WebSocketCustomerService.dll"]