FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS base

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

WORKDIR /AzJob

COPY ["AzJob.csproj", "./"]
RUN dotnet restore "AzJob.csproj"
COPY . .
WORKDIR /AzJob
RUN dotnet build "AzJob.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzJob.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

RUN adduser -u 2345 --disable-password --gecos "" adduser && chown -R adduser /app
USER appuser

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzJob.dll"]