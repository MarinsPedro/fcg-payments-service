# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN --mount=type=secret,id=nuget_config,target=/root/.nuget/NuGet/NuGet.Config,required=true \
    dotnet restore FiapCloudGames.Payments.slnx --configfile /root/.nuget/NuGet/NuGet.Config

RUN dotnet publish src/FiapCloudGames.Payments.Api/FiapCloudGames.Payments.Api.csproj \
    -c Release --no-restore -o /app/publish

RUN dotnet publish src/FiapCloudGames.Payments.Migrations/FiapCloudGames.Payments.Migrations.csproj \
    -c Release --no-restore -o /app/migrations

RUN dotnet publish src/FiapCloudGames.Payments.Worker/FiapCloudGames.Payments.Worker.csproj \
    -c Release --no-restore -o /app/worker


FROM runtime AS migrations
WORKDIR /app
COPY --from=build /app/migrations .
ENTRYPOINT ["dotnet", "FiapCloudGames.Payments.Migrations.dll"]


FROM runtime AS worker
WORKDIR /app
COPY --from=build /app/worker .
ENTRYPOINT ["dotnet", "FiapCloudGames.Payments.Worker.dll"]


FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FiapCloudGames.Payments.Api.dll"]