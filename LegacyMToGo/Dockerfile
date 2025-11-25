# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LegacyMToGo.csproj", "."]
RUN dotnet restore "LegacyMToGo.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "LegacyMToGo.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "LegacyMToGo.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN groupadd -r appgroup && useradd -r -g appgroup appuser
USER appuser
EXPOSE 8080
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LegacyMToGo.dll"]