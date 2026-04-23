# TrilobitCS

ASP.NET Core 9 Web API backend sociální sítě pro děti ve skautském/woodcraft prostředí. Port existující Laravel PHP aplikace do C#.

## Živá instance

**Swagger UI:** <https://trilobit.onrender.com/swagger/index.html>

Aplikace je nasazená na [Render](https://render.com). První request po delší nečinnosti může trvat ~30 s (cold start free tieru).

## Stack

- ASP.NET Core 9 / .NET 9
- PostgreSQL 16 + EF Core 9 (Npgsql)
- MediatR 12 (CQRS)
- JWT auth, FluentValidation, BCrypt
- xUnit + Testcontainers.PostgreSql pro integrační testy

## Spuštění

```bash
# Lokální vývoj (vyžaduje běžící PostgreSQL)
dotnet run --project TrilobitCS

# Scraping orlích per z woodcraft.cz
dotnet run --project TrilobitCS -- scrape

# Docker (app + PostgreSQL)
docker-compose up

# Testy (automaticky spustí PostgreSQL v Dockeru přes Testcontainers)
dotnet test
```

Migrace se aplikují automaticky při startu.

## Dokumentace

Kompletní kontext projektu (produktová vize, databázové schéma, konvence) je v [CLAUDE.md](CLAUDE.md).
