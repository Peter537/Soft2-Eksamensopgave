FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CentralHub.API/CentralHub.API.csproj", "CentralHub.API/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "CentralHub.API/CentralHub.API.csproj"
COPY CentralHub.API/ CentralHub.API/
COPY Shared/ Shared/
WORKDIR "/src/CentralHub.API"
RUN dotnet build "CentralHub.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CentralHub.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CentralHub.API.dll"]
