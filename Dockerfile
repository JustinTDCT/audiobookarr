FROM node:25-alpine AS frontend
WORKDIR /app/frontend
COPY frontend/package*.json ./
RUN npm install
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY AudioBookarr.slnx ./
COPY src/ ./src/
COPY --from=frontend /app/src/AudioBookarr.Api/wwwroot ./src/AudioBookarr.Api/wwwroot
RUN dotnet publish src/AudioBookarr.Api/AudioBookarr.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8787 \
    AUDIOBOOKARR_CONFIG=/config \
    AUDIOBOOKARR_AUDIOBOOKS=/audiobooks \
    AUDIOBOOKARR_DOWNLOADS=/downloads
EXPOSE 8787
VOLUME ["/config", "/audiobooks", "/downloads"]
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "AudioBookarr.Api.dll"]
