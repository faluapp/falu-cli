#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# TODO: change this Dockerfile to use Aot in from .NET 8 for smaller image size

FROM mcr.microsoft.com/dotnet/runtime:7.0-bullseye-slim AS base
WORKDIR /app

LABEL org.opencontainers.image.description="The official CLI tool for Falu (https://falu.io)" \
      org.opencontainers.image.documentation="https://falu.io/docs/cli" \
      org.opencontainers.image.source="https://github.com/faluapp/falu-cli" \
      org.opencontainers.image.vendor="FALU TECHNOLOGIES LTD"

FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim AS build
WORKDIR /src
COPY ["src/FaluCli/FaluCli.csproj", "src/FaluCli/"]
RUN dotnet restore "src/FaluCli/FaluCli.csproj"
COPY . .
WORKDIR "/src/src/FaluCli"
RUN dotnet build "FaluCli.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FaluCli.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FaluCli.dll"]