# Build steg: bygger projektet med .NET 8 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Kopiera hela projektmappen
COPY . ./EPD_Finder

# Byt arbetsmapp till där csproj ligger
WORKDIR /app/EPD_Finder

# Återställ dependencies för EPD_Finder.csproj
RUN dotnet restore EPD_Finder.csproj

# Builda projektet
RUN dotnet publish -c Release -o out EPD_Finder.csproj

# Runtime steg: kör appen i lättviktsbild
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Kopiera byggda filer från build-steget
COPY --from=build /app/EPD_Finder/out ./

# Exponera porten appen använder
EXPOSE 5000

# Starta appen
ENTRYPOINT ["dotnet", "EPD_Finder.dll"]
