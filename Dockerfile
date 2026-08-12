# syntax=docker/dockerfile:1

# ---- Build stage ------------------------------------------------------------
# Only UnizaPlus.Web and its one project reference (UnizaPlus.Models) are built
# here. UnizaPlusBackEnd - the console app that drives Selenium/Chrome for
# "Live" mode - is never copied into this build context, so its Selenium.*
# packages never enter this image at all.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY UnizaPlus.Web/UnizaPlus.Web.csproj UnizaPlus.Web/
COPY UnizaPlus.Models/UnizaPlus.Models.csproj UnizaPlus.Models/
RUN dotnet restore UnizaPlus.Web/UnizaPlus.Web.csproj

COPY UnizaPlus.Web/ UnizaPlus.Web/
COPY UnizaPlus.Models/ UnizaPlus.Models/
COPY sample-data/ sample-data/

RUN dotnet publish UnizaPlus.Web/UnizaPlus.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime stage ------------------------------------------------------------
# Plain ASP.NET runtime image - no Chrome, no chromedriver. This image only ever
# runs in "Csv" (demo) mode, which loads schedule.csv from disk and never touches
# a browser, so none of that is needed.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    UnizaPlus__DataSource=Csv

COPY --from=build /app/publish .
RUN chown -R app:app /app

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "UnizaPlus.Web.dll"]
