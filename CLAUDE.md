# TrilobitCS — kontext pro AI agenty

## O projektu

**TrilobitCS** je ASP.NET Core 9 Web API backend sociální sítě primárně pro děti ve skautském/woodcraft prostředí. Projekt je **port existující Laravel PHP aplikace do C#** — komentáře v kódu obsahují Laravel ekvivalenty (např. `// Laravel: AuthController@register`) a musí být zachovány při dalším vývoji.

Veškerý obsah je dostupný pouze přihlášeným uživatelům — žádné veřejné endpointy mimo auth.

---

## Produktová vize — 4 záložky frontendu

### 1. Homepage (Feed)

Chronologický feed příspěvků lidí, které přihlášený uživatel sleduje (followers). Charakter příspěvků je podobný aplikaci **Strava** — sdílení fyzických aktivit a úspěchů.

Typy příspěvků (`post_type`):
- `activity` — volný příspěvek (text + foto), analogie "manual activity" ve Stravě
- `achievement` — automaticky generovaný při získání orlího pera nebo splnění výzvy; nese odkaz na konkrétní `eagle_feather` nebo `challenge_completion`
- `announcement` — oznámení (detaily formátu zatím otevřené)

Příspěvky podporují liky (`post_likes`) a komentáře (`comments`). Komentáře jsou polymorfní (mohou být i na `challenge_completions`) a podporují vnořené odpovědi přes `parent_id`.

### 2. Orlí pera

Katalog skautských aktivit scrapovaných z woodcraft.cz. Hierarchie je zachována:

```
Světlo (1–4)
  └── Sekce (např. 1A, 1B, 2A, ...)
        └── Pero (číslo, název, znění činu / velkého činu)
```

Každé pero má dvě varianty obtížnosti: **čin** (`challenge`) a **velký čin** (`grand_challenge`).

**Workflow získání pera (user_eagle_feathers):**
1. Dítě označí pero jako splněné a přiloží fotky → vytvoří se `post_type = achievement`, `status = pending`
2. Vedoucí organizace uvidí příspěvek ke schválení
3. Vedoucí schválí (`status = approved`, `verified_by = leader_id`, `earned_at = now()`) → příspěvek dostane vizuální potvrzení (fajfka), pero se počítá do leaderboardu
4. Vedoucí zamítne (`status = rejected`)

Leaderboard počítá **pouze schválená pera** (`status = approved`). Detail rozdílu workflow čin vs. velký čin (`is_grand_challenge`) bude upřesněn.

### 3. Organizace

Každý uživatel patří do **jedné** organizace (1:N — `users.organisation_id`). Organizace je analogie skautského oddílu/spolku.

**Role uživatelů:**
- `child` — běžný člen, výchozí role
- `leader` — vedoucí organizace, přiřazuje superadmin mimo aplikaci

**Workflow připojení k organizaci:**
1. Nepřiřazený uživatel → záložka Organizace → "Připojit se" → zadá `invite_code`
2. Vedoucímu přijde žádost ke schválení (request flow, detaily implementace TBD)
3. Vedoucí schválí → `users.organisation_id` se nastaví

**Podzáložky organizace:**
1. **Feed** — příspěvky členů organizace (obdoba homepage, ale filtrované na danou organizaci)
2. **Leaderboard** — žebříček aktivit v rámci týmu, pouze schválená pera
3. **Memberlist** — seznam členů organizace

### 4. Profil uživatele

- Příspěvky uživatele
- Seznam získaných orlích per a statistiky (detaily zatím otevřené)
- Profil může být **soukromý** — obsah vidí jen followers
- Sledování je **jednostranné** (jako Instagram — bez schválení druhé strany)

---

## Technický stack

| Oblast | Technologie |
|---|---|
| Framework | ASP.NET Core 9, .NET 9 |
| Databáze | PostgreSQL 16 (dev/prod), PostgreSQL 17 (docker-compose) |
| ORM | Entity Framework Core 9 + Npgsql |
| Pattern | CQRS s MediatR 12 |
| Auth | JWT (Microsoft.AspNetCore.Authentication.JwtBearer 9) |
| Validace | FluentValidation.AspNetCore 11 |
| Hesla | BCrypt.Net-Next 4 |
| Scraping | HtmlAgilityPack 1.12 |
| Testy | xUnit 2, WebApplicationFactory, Testcontainers.PostgreSql 3, FluentAssertions 6, Bogus 35 |

### Konfigurace (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=trilobit;Username=trilobit;Password=trilobit"
  },
  "Jwt": {
    "Key": "<min. 32 znakový secret>",
    "Issuer": "trilobit",
    "Audience": "trilobit",
    "AccessTokenExpiresInMinutes": 15,
    "RefreshTokenExpiresInDays": 180
  }
}
```

### Spuštění

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

Migrace se spouštějí **automaticky při startu aplikace** (`db.Database.MigrateAsync()` v `Program.cs`).

---

## Struktura projektu

```
TrilobitCS/
├── Auth/
│   ├── BcryptPasswordHasher.cs     # Hash::make() / Hash::check()
│   └── JwtTokenService.cs          # GenerateAccessToken() + GenerateRefreshToken()
├── Console/
│   └── ScrapeEagleFeathersCommand.cs  # dotnet run -- scrape
├── Controllers/
│   ├── AuthController.cs           # POST /api/auth/{register,login,refresh,logout}
│   └── EagleFeathersController.cs  # GET /api/eagle-feathers, GET /api/eagle-feathers/{id}
├── Data/
│   └── AppDbContext.cs             # EF Core DbContext
├── Dto/                            # Interní přenosové objekty (ne request/response)
├── Exceptions/                     # NotFoundException, ConflictException, UnauthorizedException
├── Features/                       # CQRS handlery (MediatR)
│   ├── Auth/                       # RegisterCommand, LoginCommand, LogoutCommand, RefreshCommand
│   └── EagleFeathers/              # UpdateOrCreateEagleFeatherCommand
├── Middleware/
│   └── ExceptionHandlerMiddleware.cs
├── Migrations/
├── Models/                         # EF Core entity (User, EagleFeather, RefreshToken, Gender)
├── Repositories/                   # IUserRepository, IRefreshTokenRepository + implementace
├── Requests/                       # RegisterRequest, LoginRequest, RefreshRequest
├── Responses/                      # AuthResponse, EagleFeatherResponse
├── Services/
│   └── SvitekScraper.cs            # Scraper woodcraft.cz (Windows-1250 encoding)
└── Validators/                     # RegisterRequestValidator, LoginRequestValidator

TrilobitCS.Tests/
├── ApiCollection.cs                # Sdílí 1 PostgreSQL kontejner napříč testy [Collection("Api")]
├── TrilobitWebApplicationFactory.cs
├── Auth/                           # RegisterApiTests, LoginApiTests, LogoutApiTests, RefreshApiTests
└── Factories/
    └── RegisterRequestFactory.cs   # Bogus factory — Make() vrací platný RegisterRequest
```

---

## Architektonické konvence

### CQRS pattern (MediatR)

Každá operace = Command nebo Query v `Features/<Oblast>/`. Controller pouze deleguje na MediatR:

```csharp
// Controller
[HttpPost("register")]
public async Task<IActionResult> Register(RegisterRequest request)
    => Ok(await _mediator.Send(new RegisterCommand(request)));

// Handler v Features/Auth/RegisterCommand.cs
public record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponse>;
public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse> { ... }
```

### Validace requestů

Každý request má vlastní validator (`Validators/<Request>Validator.cs`). FluentValidation je zapojeno automaticky — nevalidní vstup vrátí **HTTP 422** (ne 400):

```csharp
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = context =>
        new UnprocessableEntityObjectResult(context.ModelState));
```

### Chybové stavy — vlastní výjimky

Výjimky se zahazují v `Features/` handlerech, middleware je mapuje na HTTP odpovědi:

| Výjimka | HTTP | Použití |
|---|---|---|
| `UnauthorizedException` | 401 | Neplatné přihlašovací údaje, neplatný token |
| `NotFoundException` | 404 | Entita nenalezena |
| `ConflictException` | 422 | Duplicita (email, nickname) |

Response body: `{ "message": "..." }` — zpráva je i18n klíč (např. `"errors.email_taken"`).

### Repository pattern

Databázový přístup jde přes interfaces (`IUserRepository`, `IRefreshTokenRepository`). Přidávej interface i implementaci pro každý nový repozitář. EF Core DbContext (`AppDbContext`) se používá přímo jen v `EagleFeathersController` (read-only, zatím bez command handleru).

### Response objekty

Každý model má dedikovaný response record/class v `Responses/` se statickou factory metodou `FromModel()`. Nikdy nevracej EF entity přímo z controlleru.

### Testy

Každá testovací třída sdílí jeden PostgreSQL Testcontainer přes `[Collection("Api")]` — kontejner se spustí jednou pro celou test suite. Nové testy přidávej do `TrilobitCS.Tests/` se stejnou strukturou složek jako `TrilobitCS/`.

Factory pro request data: `Factories/<Model>RequestFactory.cs` s Bogus generátorem.

---

## Existující API endpointy

### Auth (`/api/auth`)

| Method | Path | Request body | Response | Popis |
|---|---|---|---|---|
| POST | `/api/auth/register` | `RegisterRequest` | `AuthResponse` (200) | Registrace nového uživatele |
| POST | `/api/auth/login` | `LoginRequest` | `AuthResponse` (200) | Přihlášení |
| POST | `/api/auth/refresh` | `RefreshRequest` | `AuthResponse` (200) | Výměna refresh tokenu (rotation) |
| POST | `/api/auth/logout` | `RefreshRequest` | 204 No Content | Zneplatnění refresh tokenu |

**RegisterRequest:**
```json
{
  "nickname": "string (max 20)",
  "firstName": "string (max 20)",
  "lastName": "string (max 20)",
  "email": "string (email formát)",
  "password": "string (min 10)",
  "passwordConfirm": "string (musí = password)",
  "gender": "Male | Female | Other",
  "birthDate": "YYYY-MM-DD (musí být v minulosti)"
}
```

**AuthResponse:**
```json
{
  "accessToken": "JWT (platný 15 minut)",
  "refreshToken": "base64 string (platný 180 dní, single-use — rotation)"
}
```

**Refresh token rotation:** každý refresh token lze použít **jednou**. Po použití se revokuje a vydá se nový pár tokenů.

### Eagle Feathers (`/api/eagle-feathers`)

| Method | Path | Auth | Response | Popis |
|---|---|---|---|---|
| GET | `/api/eagle-feathers` | ne | `EagleFeatherResponse[]` (200) | Všechna pera |
| GET | `/api/eagle-feathers/{id}` | ne | `EagleFeatherResponse` (200) | Jedno pero |

---

## Databázové schéma (kompletní)

```
-- HOTOVO V DATABÁZI
users
  id              PK
  nickname        varchar(50)  UNIQUE NOT NULL
  first_name      varchar(100) NOT NULL
  last_name       varchar(100) NOT NULL
  email           varchar(100) UNIQUE NOT NULL
  password        varchar(255) NOT NULL        -- bcrypt hash
  gender          varchar(10)                  -- Male | Female | Other
  birth_date      date         NOT NULL
  profile_picture varchar(255)
  role            varchar(20)  DEFAULT 'child' -- child | leader
  organisation_id int          FK → organisations.id (nullable)
  created_at      timestamp    DEFAULT now()

refresh_tokens
  id          PK
  token       varchar(255) NOT NULL            -- base64(64 random bytes)
  user_id     int          FK → users.id
  expires_at  timestamp    NOT NULL
  created_at  timestamp    NOT NULL
  revoked_at  timestamp    (nullable — null = aktivní)
  -- IsValid = !IsExpired && !IsRevoked

eagle_feathers
  id              PK
  light           tinyint      NOT NULL        -- 1=1.světlo ... 4=4.světlo
  section         varchar(10)  NOT NULL        -- 1A, 1B, 2A, ...
  number          smallint     NOT NULL        -- pořadí v rámci sekce
  name            varchar(150) NOT NULL
  challenge       text         NOT NULL        -- znění ČINU (stripped HTML)
  grand_challenge text         NOT NULL        -- znění VELKÉHO ČINU (stripped HTML)
  source_url      varchar(255) NOT NULL
  created_at      timestamp
  updated_at      timestamp
  UNIQUE(section, number)

-- ZATÍM NEIMPLEMENTOVÁNO
followers
  id            PK
  follower_id   FK → users.id
  following_id  FK → users.id
  created_at    timestamp
  UNIQUE(follower_id, following_id)

user_eagle_feathers
  id               PK
  user_id          FK → users.id
  eagle_feather_id FK → eagle_feathers.id
  is_grand_challenge boolean DEFAULT false     -- false=čin, true=velký čin
  status           varchar(20) DEFAULT 'pending' -- pending | approved | rejected
  verified_by      FK → users.id (leader, nullable)
  earned_at        timestamp (nullable)
  created_at       timestamp
  UNIQUE(user_id, eagle_feather_id)

organisations
  id          PK
  name        varchar(100) NOT NULL
  description text
  avatar_url  varchar(255)
  invite_code varchar(20)  UNIQUE
  created_at  timestamp

posts
  id                    PK
  user_id               FK → users.id
  organisation_id       FK → organisations.id (nullable — null = jen pro followers)
  eagle_feather_id      FK → eagle_feathers.id (nullable — achievement post)
  challenge_completion_id FK → challenge_completions.id (nullable)
  post_type             varchar(30)  -- activity | achievement | announcement
  content               text
  image_url             varchar(255)
  created_at            timestamp

post_likes
  id         PK
  post_id    FK → posts.id
  user_id    FK → users.id
  created_at timestamp
  UNIQUE(post_id, user_id)

comments
  id               PK
  user_id          FK → users.id
  commentable_type varchar(50)  -- 'posts' | 'challenge_completions'
  commentable_id   int
  parent_id        FK → comments.id (nullable — null=top-level, jinak reply)
  content          text NOT NULL
  created_at       timestamp
  INDEX(commentable_type, commentable_id)

challenges
  id               PK
  title            varchar(150) NOT NULL
  description      text
  eagle_feather_id FK → eagle_feathers.id (nullable — odměna za splnění)
  difficulty_level int          -- 1–3 hvězdičky
  valid_from       timestamp
  valid_to         timestamp
  created_at       timestamp

challenge_completions
  id           PK
  user_id      FK → users.id
  challenge_id FK → challenges.id
  result_value decimal(8,2) (nullable) -- např. 11.25
  result_unit  varchar(20)  (nullable) -- s | m | reps | ...
  completed_at timestamp
  UNIQUE(user_id, challenge_id)
```

---

## Scraper woodcraft.cz

`SvitekScraper` stahuje data z `https://www.woodcraft.cz/files/web/svitek/index.php`. Zdrojový web používá **Windows-1250** encoding — scraper jej dekóduje před parsováním.

Logika rozdělení činu a velkého činu (`SplitChallenges`):
- Pokud HTML obsahuje `<ul class="OpPodminky">` → `SplitFromList()` — rozděluje podle labelu (`VELKÝ ČIN` / `V. ČIN`)
- Pokud HTML obsahuje `<table>` → `SplitFromTable()` — rozděluje podle hodnoty v poslední buňce řádku
- Jinak → challenge i grand_challenge = stejný obsah

`StripHtml()` odstraní HTML tagy a zachová pouze tabulky jako inline HTML (tabulky nesou strukturovaná data, která nesmí být ztracena).

Scraper se spouští: `dotnet run --project TrilobitCS -- scrape`
Uloží/aktualizuje záznamy přes `UpdateOrCreateEagleFeatherCommand` (upsert podle `light + section + number`).
