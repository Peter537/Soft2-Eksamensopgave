FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PartnerService/PartnerService.csproj", "PartnerService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "PartnerService/PartnerService.csproj"
COPY PartnerService/ PartnerService/
COPY Shared/ Shared/
WORKDIR "/src/PartnerService"
RUN dotnet build "PartnerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PartnerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PartnerService.dll"]
