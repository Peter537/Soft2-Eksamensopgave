FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebsocketPartnerService/WebsocketPartnerService.csproj", "WebsocketPartnerService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "WebsocketPartnerService/WebsocketPartnerService.csproj"
COPY . .
WORKDIR "/src/WebsocketPartnerService"
RUN dotnet build "WebsocketPartnerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebsocketPartnerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebsocketPartnerService.dll"]
