FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KafkaOrderDemo/KafkaOrderDemo.csproj", "KafkaOrderDemo/"]
RUN dotnet restore "KafkaOrderDemo/KafkaOrderDemo.csproj"
COPY KafkaOrderDemo/ KafkaOrderDemo/
WORKDIR "/src/KafkaOrderDemo"
RUN dotnet build "KafkaOrderDemo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KafkaOrderDemo.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KafkaOrderDemo.dll"]
