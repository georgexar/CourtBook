# ---------- Στάδιο 1: build (έχει το SDK) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Πρώτα ΜΟΝΟ τα .csproj, για να γίνει cache το restore
COPY src/CourtBook.Domain/CourtBook.Domain.csproj                 src/CourtBook.Domain/
COPY src/CourtBook.Application/CourtBook.Application.csproj       src/CourtBook.Application/
COPY src/CourtBook.Infrastructure/CourtBook.Infrastructure.csproj src/CourtBook.Infrastructure/
COPY src/CourtBook.Api/CourtBook.Api.csproj                       src/CourtBook.Api/
RUN dotnet restore src/CourtBook.Api/CourtBook.Api.csproj

# Μετά όλος ο κώδικας
COPY src/ src/
RUN dotnet publish src/CourtBook.Api/CourtBook.Api.csproj -c Release -o /app/publish --no-restore

# ---------- Στάδιο 2: runtime (μόνο ό,τι χρειάζεται για να τρέξει) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# curl για το health check του Docker
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CourtBook.Api.dll"]