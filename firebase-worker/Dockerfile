# build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy all csproj
COPY Shared/Shared.csproj Shared/
COPY firebase-worker/firebase-worker.csproj firebase-worker/

RUN dotnet restore firebase-worker/firebase-worker.csproj
RUN dotnet restore Shared/Shared.csproj

# copy toàn bộ code
COPY . .

# publish
RUN dotnet publish firebase-worker/firebase-worker.csproj -c Release -o /app

# runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "firebase-worker.dll"]