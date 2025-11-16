FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LocationService/LocationService.csproj", "LocationService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "LocationService/LocationService.csproj"
COPY LocationService/ LocationService/
COPY Shared/ Shared/
WORKDIR "/src/LocationService"
RUN dotnet build "LocationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LocationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LocationService.dll"]
