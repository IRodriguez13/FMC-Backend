# FMC API — imagen de runtime (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Fmc.sln ./
COPY src/FMC.Api/Fmc.Api.csproj src/FMC.Api/
RUN dotnet restore src/FMC.Api/Fmc.Api.csproj

COPY src/FMC.Api/ src/FMC.Api/
RUN dotnet publish src/FMC.Api/Fmc.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p /data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Fmc.Api.dll"]
