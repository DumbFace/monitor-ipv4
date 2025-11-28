# build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj trước để restore cache
COPY Shared/Shared.csproj Shared/
COPY monitor-ipv4-tool/monitor-ipv4-tool.csproj monitor-ipv4-tool/

RUN dotnet restore monitor-ipv4-tool/monitor-ipv4-tool.csproj
RUN dotnet restore Shared/Shared.csproj

# copy toàn bộ code
COPY . .

# publish
RUN dotnet publish monitor-ipv4-tool/monitor-ipv4-tool.csproj -c Release -o /app

# runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "monitor-ipv4-tool.dll"]