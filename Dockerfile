# FMC API — imagen de runtime (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Fmc.sln ./
COPY src/FMC.Api/Fmc.Api.csproj src/FMC.Api/
COPY src/Fmc.Domain/Fmc.Domain.csproj src/Fmc.Domain/
COPY src/Fmc.Application/Fmc.Application.csproj src/Fmc.Application/
COPY src/Fmc.Infrastructure/Fmc.Infrastructure.csproj src/Fmc.Infrastructure/
RUN dotnet restore src/FMC.Api/Fmc.Api.csproj

COPY src/FMC.Api/ src/FMC.Api/
COPY src/Fmc.Domain/ src/Fmc.Domain/
COPY src/Fmc.Application/ src/Fmc.Application/
COPY src/Fmc.Infrastructure/ src/Fmc.Infrastructure/
RUN dotnet publish src/FMC.Api/Fmc.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p /data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Fmc.Api.dll"]
