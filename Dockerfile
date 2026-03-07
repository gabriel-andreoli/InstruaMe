FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY src/*.csproj ./src/
RUN dotnet restore src/InstruaMe.csproj

COPY src/ ./src/
RUN dotnet publish src/InstruaMe.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

ENTRYPOINT ["dotnet", "InstruaMe.dll"]
