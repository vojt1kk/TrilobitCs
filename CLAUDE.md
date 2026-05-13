# TrilobitCS — kontext pro AI agenty

## O projektu

**TrilobitCS** je ASP.NET Core 9 Web API backend sociální sítě primárně pro děti ve skautském/woodcraft prostředí. Projekt je **port existující Laravel PHP aplikace do C#** — komentáře v kódu obsahují Laravel ekvivalenty (např. `// Laravel: AuthController@register`) a musí být zachovány při dalším vývoji.

Veškerý obsah je dostupný pouze přihlášeným uživatelům — žádné veřejné endpointy mimo auth.

---

## Produktová vize — 4 záložky frontendu

### 1. Homepage (Feed)

Chronologický feed příspěvků lidí, které přihlášený uživatel sleduje (followers). Charakter příspěvků je podobný aplikaci **Strava** — sdílení fyzických aktivit a úspěchů.

Typ příspěvku se odvozuje z přítomnosti volitelných FK:
- volný příspěvek (text + foto) — bez `eagle_feather_id` i `challenge_completion_id`
- získání orlího pera — `eagle_feather_id` vyplněno (auto-generovaný při schválení)
- splnění výzvy — `challenge_completion_id` vyplněno

Příspěvky podporují liky (polymorfní `likes` — na post i komentář) a komentáře (`comments`). Komentáře jsou polymorfní — mohou být na postu (`commentable_type = posts`) nebo jako odpověď na jiný komentář (`commentable_type = comments`).

### 2. Orlí pera

Katalog skautských aktivit scrapovaných z woodcraft.cz. Hierarchie je zachována:

```
Světlo (1–4)
  └── Sekce (např. 1A, 1B, 2A, ...)
        └── Pero (číslo, název, znění činu / velkého činu)
```

Každé pero má dvě varianty obtížnosti: **čin** (`challenge`) a **velký čin** (`grand_challenge`).

**Workflow získání pera (user_eagle_feathers) — zatím bez API:**
1. Dítě označí pero jako splněné a přiloží fotky → vytvoří se `user_eagle_feathers` se `status = Pending` a po schválení i příspěvek s `eagle_feather_id`
2. Vedoucí organizace uvidí příspěvek ke schválení
3. Vedoucí schválí (`status = Approved`, `verified_by = leader_id`, `earned_at = now()`) → příspěvek dostane vizuální potvrzení (fajfka), pero se počítá do leaderboardu
4. Vedoucí zamítne (`status = Rejected`)

Leaderboard počítá **pouze schválená pera** (`status = Approved`). Detail rozdílu workflow čin vs. velký čin (`is_grand_challenge`) bude upřesněn.

### 3. Organizace

Každý uživatel patří do **jedné** organizace (1:N — `users.organisation_id`). Organizace je analogie skautského oddílu/spolku.

**Role uživatelů:**
- `user` — běžný člen, výchozí role
- `leader` — vedoucí organizace, přiřazuje superadmin mimo aplikaci

**Workflow pozvánky do organizace (OrganisationInvite — implementováno):**

Přístup je **leader-driven**: leader pošle pozvánku konkrétnímu uživateli (podle nickname), uživatel ji přijme nebo odmítne.

1. Leader pošle pozvánku → `POST /api/organisation-invites` s `{ "nickname": "..." }` → vytvoří se `OrganisationInvite` se `status = Pending`
2. Pozvaný uživatel vidí své pozvánky → `GET /api/organisation-invites`
3. Uživatel přijme → `POST /api/organisation-invites/{id}/accept` → `user.organisation_id = invite.organisation_id`, ostatní pending pozvánky uživatele se auto-odmítnou
4. Uživatel odmítne → `POST /api/organisation-invites/{id}/decline`

**Pravidla:**
- Pozvánku může poslat jen Leader, který má organizaci (`role = Leader && organisation_id IS NOT NULL`)
- Nelze pozvat uživatele, který je již v organizaci → 422
- Duplicitní pending pozvánka na stejnou (user, org) kombinaci → 422 (partial unique index)
- Accept blokován pokud uživatel je mezitím přiřazen do jiné org → 422
- Decline/Accept blokován pokud pozvánka není Pending → 422

**Status enum `OrganisationInviteStatus`:** `0 = Pending`, `1 = Accepted`, `2 = Declined`

**Podzáložky organizace:**
1. **Feed** — příspěvky členů organizace (obdoba homepage, ale filtrované na danou organizaci) — **zatím bez API**
2. **Leaderboard** — žebříček aktivit v rámci týmu, pouze schválená pera — **zatím bez API**
3. **Memberlist** — seznam členů organizace (`GET /api/organisations/{id}/members`)

### 4. Profil uživatele

- Příspěvky uživatele — **zatím bez API**
- Seznam získaných orlích per a statistiky — **zatím bez API**
- Profil může být **soukromý** — obsah vidí jen followers — **zatím bez `is_private` pole**
- Sledování je **jednostranné** (jako Instagram — bez schválení druhé strany) — **zatím bez API**

---

## Technický stack

| Oblast | Technologie |
|---|---|
| Framework | ASP.NET Core 9, .NET 9 |
| Databáze | PostgreSQL 16 (dev/prod), PostgreSQL 17 (docker-compose) |
| ORM | Entity Framework Core 9 + Npgsql |
| Pattern | CQRS s MediatR **12.x (pin na 12.4.\* — viz Architektonická rozhodnutí)** |
| Auth | JWT (Microsoft.AspNetCore.Authentication.JwtBearer 9) |
| Validace | FluentValidation.AspNetCore 11 |
| Hesla | BCrypt.Net-Next 4 |
| Scraping | HtmlAgilityPack 1.12 |
| API docs | Microsoft.AspNetCore.OpenApi + Scalar UI |
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
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
  },
  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "WriteTo": [{ "Name": "Console" }]
  }
}
```

> ⚠️ **JWT poznámka:** `AccessTokenExpiresInMinutes` v `appsettings.json` je nastaveno na 7 dní pro pohodlí při vývoji. Pro produkci snížit na **15 minut**.

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

API dokumentace (Scalar UI): `http://localhost:5xxx/scalar/v1`
OpenAPI JSON: `http://localhost:5xxx/openapi/v1.json`

Migrace se spouštějí **automaticky při startu aplikace** (`db.Database.MigrateAsync()` v `Program.cs`).

---

## Struktura projektu

```
TrilobitCS/
├── Auth/
│   ├── BcryptPasswordHasher.cs          # Hash::make() / Hash::check()
│   └── JwtTokenService.cs               # GenerateAccessToken() + GenerateRefreshToken()
├── Console/
│   └── ScrapeEagleFeathersCommand.cs    # dotnet run -- scrape
├── Controllers/
│   ├── AuthController.cs                # POST /api/auth/{register,login,refresh,logout}
│   ├── EagleFeathersController.cs       # GET /api/eagle-feathers[/{id}]  [Authorize]
│   ├── OrganisationInvitesController.cs # POST|GET /api/organisation-invites, POST /api/organisation-invites/{id}/{accept,decline}
│   ├── OrganisationsController.cs       # POST|GET /api/organisations[/{id}], PUT /api/organisations/{id}, GET /api/organisations/{id}/members
│   └── UsersController.cs               # GET /api/users/{id}, GET|PUT|DELETE /api/user, DELETE /api/user/organisation
├── Data/
│   └── AppDbContext.cs                  # EF Core DbContext — používá se přímo v handlerech
├── Exceptions/                          # NotFoundException, ConflictException, UnauthorizedException, ForbiddenException
├── Extensions/
│   └── ClaimsPrincipalExtensions.cs     # GetUserId() — čte sub claim z JWT
├── Features/                            # CQRS handlery (MediatR)
│   ├── Auth/                            # RegisterCommand, LoginCommand, LogoutCommand, RefreshCommand
│   ├── EagleFeathers/                   # UpdateOrCreateEagleFeatherCommand
│   ├── OrganisationInvites/             # SendOrganisationInviteCommand, GetOrganisationInvitesQuery, AcceptOrganisationInviteCommand, DeclineOrganisationInviteCommand
│   ├── Organisations/                   # CreateOrganisationCommand, GetOrganisationQuery, UpdateOrganisationCommand, GetOrganisationMembersQuery
│   └── Users/                           # GetUserQuery, GetCurrentUserQuery, UpdateUserCommand, DeleteUserCommand, LeaveOrganisationCommand
├── Middleware/
│   └── ExceptionHandlerMiddleware.cs
├── Migrations/
├── Models/                              # EF Core entity (User, Organisation, OrganisationInvite, EagleFeather, RefreshToken, Gender, UserRole, OrganisationInviteStatus, ...)
├── OpenApi/
│   └── BearerSecuritySchemeTransformer.cs  # Přidá JWT Bearer do OpenAPI schématu
├── Requests/                            # RegisterRequest, LoginRequest, RefreshRequest, UpdateUserRequest, CreateOrganisationRequest, UpdateOrganisationRequest, SendOrganisationInviteRequest
├── Responses/                           # AuthResponse, EagleFeatherResponse, PublicUserResponse, SelfUserResponse, OrganisationResponse, OrganisationMemberResponse, OrganisationInviteResponse
├── Services/
│   ├── RefreshTokenCleanupService.cs    # BackgroundService — denně maže expired/revoked refresh tokeny
│   └── SvitekScraper.cs                 # Scraper woodcraft.cz (Windows-1250 encoding)
└── Validators/                          # RegisterRequestValidator, LoginRequestValidator, UpdateUserRequestValidator, CreateOrganisationRequestValidator, UpdateOrganisationRequestValidator, SendOrganisationInviteValidator

TrilobitCS.Tests/
├── ApiCollection.cs                     # Sdílí 1 PostgreSQL kontejner napříč testy [Collection("Api")]
├── TrilobitWebApplicationFactory.cs
├── Auth/                                # RegisterApiTests, LoginApiTests, LogoutApiTests, RefreshApiTests
├── OrganisationInvites/                 # OrganisationInvitesApiTests
├── Organisations/                       # OrganisationApiTests
├── Users/                               # UsersApiTests (GET, GET me, PUT, DELETE, Leave Organisation)
└── Factories/                           # RegisterRequestFactory, UpdateUserRequestFactory, CreateOrganisationRequestFactory
```

---

## Architektonické konvence

### CQRS pattern (MediatR)

Každá operace = Command nebo Query v `Features/<Oblast>/`. Controller pouze deleguje na MediatR:

```csharp
// Controller
[HttpPost("register")]
public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    => Ok(await _mediator.Send(new RegisterCommand(request), ct));

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
| `ConflictException` | 422 | Duplicita nebo porušení business rule |
| `ForbiddenException` | 403 | Nedostatečná oprávnění (např. non-Leader volá Leader-only endpoint) |

Response body: `{ "message": "..." }` — zpráva je i18n klíč (např. `"errors.email_taken"`).

### Data access — přímo přes `AppDbContext`

**Žádná repository vrstva.** `DbSet<T>` už je repository + unit of work, wrapovat ho dalším interface nepřináší hodnotu (EF Core tým to nedoporučuje). Handlery v `Features/` injektují `AppDbContext` přímo:

- **Queries** projektují rovnou na response DTO přes `.Select()` — nenačítá se celá entita, do DB jde jen to, co je v response (žádné `password` hashe na drátě):
  ```csharp
  await _db.Users.Where(u => u.Id == id)
      .Select(u => new PublicUserResponse(u.Id, u.Nickname, ..., u.CreatedAt))
      .FirstOrDefaultAsync(ct)
      ?? throw new NotFoundException("errors.user_not_found");
  ```
- **Commands** načtou entitu, zmutují ji, a volají `_db.SaveChangesAsync(ct)`:
  ```csharp
  var user = await _db.Users.FindAsync([id], ct) ?? throw new NotFoundException(...);
  user.Nickname = request.Nickname;
  await _db.SaveChangesAsync(ct);
  ```

Integrační testy přes Testcontainers běží nad reálnou PostgreSQL, takže repozitáře nejsou potřeba ani pro mockování. Vzor: `Features/EagleFeathers/UpdateOrCreateEagleFeatherCommand.cs`.

### Response objekty — Privacy model

- `PublicUserResponse` — veřejný profil (bez emailu), vrací `GET /api/users/{id}`
- `SelfUserResponse` — vlastní profil (s emailem, `role`, `organisationId`), vrací `GET /api/user/me`
- Ostatní response typy jsou v `Responses/`

### Testy

Každá testovací třída sdílí jeden PostgreSQL Testcontainer přes `[Collection("Api")]` — kontejner se spustí jednou pro celou test suite. Nové testy přidávej do `TrilobitCS.Tests/` se stejnou strukturou složek jako `TrilobitCS/`.

Factory pro request data: `Factories/<Model>RequestFactory.cs` s Bogus generátorem.

---

## Architektonická rozhodnutí

### MediatR pin na v12

MediatR 13+ (od srpna 2025) vyžaduje komerční licenci. Projekt je pinnut na `12.4.*` (poslední free verze). **Neupgradovat na 13+ bez rozhodnutí o licenci.** Alternativa: nahradit vlastním minimálním CQRS dispatcherem (~50 řádků).

### JWT token expiration

`appsettings.json` má `AccessTokenExpiresInMinutes: 10080` (7 dní) pro pohodlí při vývoji. **Pro produkci nastavit na 15.** `RefreshTokenExpiresInDays: 365` — pro produkci doporučeno 180.

### Legacy JWT handler

Program.cs používá `JwtSecurityTokenHandler` místo moderního `JsonWebTokenHandler`. Důvod: legacy handler automaticky mapuje `sub` claim na `ClaimTypes.NameIdentifier`. Extension `ClaimsPrincipalExtensions.GetUserId()` na toto mapování spoléhá. Plánovaná migrace: přejít na `JsonWebTokenHandler` + `MapInboundClaims = false` + číst `sub` přímo.

### OpenAPI: Microsoft.AspNetCore.OpenApi + Scalar

Nahrazuje Swashbuckle (byl nutný regex hack na 3.0.4 → 3.0.1). Scalar UI na `/scalar/v1`, OpenAPI dokument na `/openapi/v1.json`. FluentValidation pravidla (max length, required) nejsou automaticky propagovány do OpenAPI schématu — akceptovaný kompromis.

---

## Existující API endpointy

### Auth (`/api/auth`)

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | ne | `RegisterRequest` | `AuthResponse` (200) | Registrace nového uživatele |
| POST | `/api/auth/login` | ne | `LoginRequest` | `AuthResponse` (200) | Přihlášení |
| POST | `/api/auth/refresh` | ne | `RefreshRequest` | `AuthResponse` (200) | Výměna refresh tokenu (rotation) |
| POST | `/api/auth/logout` | ne | `RefreshRequest` | 204 No Content | Zneplatnění refresh tokenu |

**RegisterRequest:**
```json
{
  "nickname": "string (max 20, min 3)",
  "firstName": "string (max 20, min 3)",
  "lastName": "string (max 20, min 3)",
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
  "accessToken": "JWT (platný 15 minut v prod)",
  "refreshToken": "base64 string (platný 180 dní, single-use — rotation)"
}
```

**Refresh token rotation:** každý refresh token lze použít **jednou**. Po použití se revokuje a vydá se nový pár tokenů.

### Eagle Feathers (`/api/eagle-feathers`)

| Method | Path | Auth | Response | Popis |
|---|---|---|---|---|
| GET | `/api/eagle-feathers` | **ano** | `EagleFeatherResponse[]` (200) | Všechna pera |
| GET | `/api/eagle-feathers/{id}` | **ano** | `EagleFeatherResponse` (200) | Jedno pero |

### Users (`/api/users`, `/api/user`)

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| GET | `/api/users/{id}` | ano | — | `PublicUserResponse` (200) | Veřejný profil uživatele (bez emailu) |
| GET | `/api/user/me` | ano | — | `SelfUserResponse` (200) | Vlastní profil (s emailem, role, organisationId) |
| PUT | `/api/user` | ano | `UpdateUserRequest` | `SelfUserResponse` (200) | Aktualizace vlastního profilu |
| DELETE | `/api/user` | ano | — | 204 No Content | Smazání vlastního účtu (včetně refresh tokenů) |
| DELETE | `/api/user/organisation` | ano | — | 204 No Content | Odchod z organizace (Leader vlastní org blokován) |

**PublicUserResponse:**
```json
{
  "id": 1,
  "nickname": "jan99",
  "firstName": "Jan",
  "lastName": "Novák",
  "profilePicture": null,
  "createdAt": "2026-04-25T..."
}
```

**SelfUserResponse:**
```json
{
  "id": 1,
  "nickname": "jan99",
  "firstName": "Jan",
  "lastName": "Novák",
  "email": "jan@example.com",
  "gender": "Male",
  "birthDate": "2000-01-01",
  "profilePicture": null,
  "role": "User",
  "organisationId": null,
  "createdAt": "2026-04-25T..."
}
```

### Organisations (`/api/organisations`)

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| POST | `/api/organisations` | Leader | `CreateOrganisationRequest` | `OrganisationResponse` (200) | Vytvoří org, leader_id = current user |
| GET | `/api/organisations/{id}` | ano | — | `OrganisationResponse` (200) | Detail organizace s počtem členů |
| PUT | `/api/organisations/{id}` | Leader dané org | `UpdateOrganisationRequest` | `OrganisationResponse` (200) | Aktualizace org (jen vlastní org) |
| GET | `/api/organisations/{id}/members` | ano | — | `OrganisationMemberResponse[]` (200) | Seznam členů organizace |

**CreateOrganisationRequest:**
```json
{
  "name": "string (max 100, required)",
  "description": "string (max 1000, optional)",
  "avatarUrl": "string (max 255, optional)"
}
```

**UpdateOrganisationRequest** — všechna pole optional, aktualizují se jen vyplněná:
```json
{
  "name": "string (max 100)",
  "description": "string (max 1000)",
  "avatarUrl": "string (max 255)"
}
```

**OrganisationResponse:**
```json
{
  "id": 1,
  "name": "Skautský oddíl Liška",
  "description": "...",
  "avatarUrl": null,
  "memberCount": 5,
  "leader": { "id": 1, "nickname": "vedouci123" },
  "createdAt": "2026-04-25T..."
}
```

### Organisation Invites (`/api/organisation-invites`)

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| POST | `/api/organisation-invites` | Leader s org | `SendOrganisationInviteRequest` | `OrganisationInviteResponse` (200) | Pošle pozvánku uživateli (podle nickname) |
| GET | `/api/organisation-invites` | ano | — | `OrganisationInviteResponse[]` (200) | Moje pozvánky (všechny statusy) |
| POST | `/api/organisation-invites/{id}/accept` | pozvaný user | — | `OrganisationInviteResponse` (200) | Přijme pozvánku, nastaví user.organisation_id |
| POST | `/api/organisation-invites/{id}/decline` | pozvaný user | — | `OrganisationInviteResponse` (200) | Odmítne pozvánku |

**SendOrganisationInviteRequest:**
```json
{ "nickname": "cil_uzivatele" }
```

**OrganisationInviteResponse:**
```json
{
  "id": 1,
  "organisationId": 2,
  "invitedUserId": 5,
  "invitedUserNickname": "novacek99",
  "invitedById": 3,
  "status": 0,
  "createdAt": "2026-04-25T..."
}
```
Status: `0` = Pending, `1` = Accepted, `2` = Declined

---

## Databázové schéma (kompletní)

```
-- IMPLEMENTOVÁNO (API existuje)
users
  id              PK
  nickname        varchar(50)  UNIQUE NOT NULL
  first_name      varchar(100) NOT NULL
  last_name       varchar(100) NOT NULL
  email           varchar(100) UNIQUE NOT NULL
  password        varchar(255) NOT NULL        -- bcrypt hash
  gender          int          NOT NULL        -- enum: 0=Male, 1=Female, 2=Other
  birth_date      date         NOT NULL
  profile_picture varchar(255)
  role            int          DEFAULT 0       -- enum: 0=User, 1=Leader (přiřazuje superadmin v DB)
  organisation_id int          FK → organisations.id (nullable, SetNull on delete)
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
  light           smallint     NOT NULL        -- 1=1.světlo ... 4=4.světlo
  section         varchar(10)  NOT NULL        -- 1A, 1B, 2A, ...
  number          smallint     NOT NULL        -- pořadí v rámci sekce
  name            varchar(150) NOT NULL
  challenge       text         NOT NULL        -- znění ČINU (stripped HTML)
  grand_challenge text         NOT NULL        -- znění VELKÉHO ČINU (stripped HTML)
  source_url      varchar(255) NOT NULL
  created_at      timestamp
  updated_at      timestamp
  UNIQUE(light, section, number)

organisations
  id          PK
  name        varchar(100) NOT NULL
  description text
  avatar_url  varchar(255)
  leader_id   int          NOT NULL FK → users.id (Restrict on delete)
  created_at  timestamp    DEFAULT now()

organisation_invites
  id              PK
  organisation_id FK → organisations.id (Cascade delete)
  invited_user_id FK → users.id (Cascade delete)
  invited_by_id   FK → users.id (nullable, SetNull on delete)
  status          int          DEFAULT 0       -- enum: 0=Pending, 1=Accepted, 2=Declined
  created_at      timestamp    DEFAULT now()
  UNIQUE(invited_user_id, organisation_id) WHERE status = 0  -- partial index: jen jedna pending pozvánka na (user, org)

-- MODELY EXISTUJÍ, API ZATÍM NEIMPLEMENTOVÁNO
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
  status           int          DEFAULT 0      -- enum: 0=Pending, 1=Approved, 2=Rejected
  verified_by      FK → users.id (leader, nullable)
  earned_at        timestamp (nullable)
  created_at       timestamp
  UNIQUE(user_id, eagle_feather_id)

posts
  id                      PK
  user_id                 FK → users.id
  organisation_id         FK → organisations.id (nullable — null = jen pro followers)
  eagle_feather_id        FK → eagle_feathers.id (nullable — post o získání pera)
  challenge_completion_id FK → challenge_completions.id (nullable — post o splnění výzvy)
  content                 text
  image_url               varchar(255)
  created_at              timestamp

likes
  id             PK
  user_id        FK → users.id
  likeable_type  int          -- enum: 0=Posts, 1=Comments
  likeable_id    int
  post_id        FK → posts.id (nullable)
  comment_id     FK → comments.id (nullable)
  created_at     timestamp
  UNIQUE(user_id, likeable_type, likeable_id)
  INDEX(likeable_type, likeable_id)

comments
  id               PK
  user_id          FK → users.id
  post_id          FK → posts.id (nullable — null pokud je reply)
  commentable_type int          -- enum: 0=Posts, 1=Comments
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

## Cross-cutting infrastruktura

### CORS
Allowed origins se čtou z `appsettings.json` (`Cors:AllowedOrigins`). Politika povoluje libovolné hlavičky a metody pro nakonfigurované origins. Pro produkci nastavit na skutečnou doménu frontendu.

### Rate limiting
.NET 9 built-in `AddRateLimiter`. Fixed window: **5 requestů / 1 minuta na IP** pro:
- `POST /api/auth/login`
- `POST /api/auth/refresh`

Označení `[EnableRateLimiting("auth")]` na akcích. V testovacím prostředí (`IsEnvironment("Testing")`) je rate limiter vypnutý.

### Refresh token cleanup
`Services/RefreshTokenCleanupService.cs` jako `BackgroundService`. Spouští se každých 24 hodin a maže:
```sql
DELETE FROM refresh_tokens WHERE expires_at < now()
   OR (revoked_at IS NOT NULL AND revoked_at < now() - INTERVAL '30 days')
```

### Structured logging (Serilog)
`Serilog.AspNetCore` s konfigurací z `appsettings.json`. Request logging middleware zaznamenává každý HTTP request. Výchozí sink: console (JSON formát).

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
