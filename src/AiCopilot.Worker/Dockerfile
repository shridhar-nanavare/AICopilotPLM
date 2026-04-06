FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "AiCopilot.sln"
RUN dotnet publish "src/AiCopilot.Worker/AiCopilot.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends procps \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=5 CMD pgrep -f "AiCopilot.Worker.dll" > /dev/null || exit 1
ENTRYPOINT ["dotnet", "AiCopilot.Worker.dll"]
