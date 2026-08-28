FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY TrilobitCS/TrilobitCS.csproj TrilobitCS/
RUN dotnet restore TrilobitCS/TrilobitCS.csproj
COPY TrilobitCS/ TrilobitCS/
RUN dotnet publish TrilobitCS/TrilobitCS.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
# Disables the appsettings.json FileSystemWatcher: containers often have a low
# inotify instance limit, and the watcher isn't needed since config doesn't
# change at runtime in production.
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
ENTRYPOINT ["dotnet", "TrilobitCS.dll"]
