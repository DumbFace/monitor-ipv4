# build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj trước để restore cache
COPY example-api.csproj . 

RUN dotnet restore example-api.csproj 

# copy toàn bộ code
COPY . .

# publish
RUN dotnet publish example-api.csproj -c Release -o /app

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "example-api.dll"]