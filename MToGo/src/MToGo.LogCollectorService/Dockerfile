FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/MToGo.LogCollectorService/MToGo.LogCollectorService.csproj", "src/MToGo.LogCollectorService/"]
COPY ["src/MToGo.Shared/MToGo.Shared.csproj", "src/MToGo.Shared/"]
COPY ["Directory.Packages.props", "./"]
RUN dotnet restore "src/MToGo.LogCollectorService/MToGo.LogCollectorService.csproj"
COPY . .
WORKDIR "/src/src/MToGo.LogCollectorService"
RUN dotnet build "MToGo.LogCollectorService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MToGo.LogCollectorService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Create logs directory
RUN mkdir -p /var/log/mtogo && chmod 777 /var/log/mtogo
ENTRYPOINT ["dotnet", "MToGo.LogCollectorService.dll"]
