# FMC API — imagen de runtime (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Fmc.sln ./
COPY Api/Api.csproj Api/
COPY Domain/Domain.csproj Domain/
COPY Application/Application.csproj Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
RUN dotnet restore Api/Api.csproj

COPY Api/ Api/
COPY Domain/ Domain/
COPY Application/ Application/
COPY Infrastructure/ Infrastructure/
RUN dotnet publish Api/Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p /data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Fmc.Api.dll"]
