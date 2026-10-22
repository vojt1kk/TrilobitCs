# TrilobitCS — kontext pro AI agenty

## O projektu

**TrilobitCS** je ASP.NET Core 9 Web API backend sociální sítě primárně pro děti ve skautském/woodcraft prostředí. Projekt je **port existující Laravel PHP aplikace do C#**. Komentáře v kódu jsou psány v angličtině; Laravel reference se **nevkládají** — existující komentáře zachovávají jen netriviální invarianty (polymorfní relace, workaroundy, skryté constraints), nikoli triviální přejmenování tříd/metod.

Veškerý obsah je dostupný pouze přihlášeným uživatelům — žádné veřejné endpointy mimo auth.

---

## Produktová vize — 4 záložky frontendu

### 1. Homepage (Feed)

Chronologický feed příspěvků lidí, které přihlášený uživatel sleduje (followers). Charakter příspěvků je podobný aplikaci **Strava** — sdílení fyzických aktivit a úspěchů.

Každý příspěvek je vždy vázán na konkrétní `user_eagle_feather_id` (NOT NULL) — post vždy dokumentuje práci na perku. Volitelně může mít `challenge_id`, pokud post vznikl přes výzvu (pouze informace, nemá vlastní lifecycle).

Příspěvky podporují liky (polymorfní `likes` — na post i komentář) a komentáře (`comments`). Komentáře jsou polymorfní — mohou být na postu (`commentable_type = posts`) nebo jako odpověď na jiný komentář (`commentable_type = comments`).

### 2. Orlí pera

Katalog skautských aktivit scrapovaných z woodcraft.cz. Hierarchie je zachována:

```
Světlo (1–4)
  └── Sekce (např. 1A, 1B, 2A, ...)
        └── Pero (číslo, název, znění činu / velkého činu)
```

Každé pero má dvě varianty obtížnosti: **čin** (`challenge`) a **velký čin** (`grand_challenge`).

**Workflow získání pera (user_eagle_feathers) — API implementováno (viz „User Eagle Feathers"):**
1. Dítě označí pero jako splněné a přiloží fotky → vytvoří se `user_eagle_feathers` se `status = Pending` a po schválení i příspěvek s `user_eagle_feather_id`
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
1. **Feed** — příspěvky publikované do organizace (`GET /api/organisations/{id}/feed`), filtrované privacy pravidlem
2. **Leaderboard** — žebříček aktivit v rámci týmu, pouze schválená pera (`GET /api/organisations/{id}/leaderboard`)
3. **Memberlist** — seznam členů organizace (`GET /api/organisations/{id}/members`)

### 4. Profil uživatele

- Příspěvky uživatele (`GET /api/users/{id}/posts`)
- Seznam získaných orlích per a statistiky (`GET /api/users/{id}/eagle-feathers`, `GET /api/users/{id}/stats`)
- Profil může být **soukromý** (`is_private`) — obsah vidí jen followers a sám uživatel (viz „Privacy model")
- Sledování je **jednostranné** (jako Instagram — bez schválení druhé strany) — `POST|DELETE /api/users/{id}/follow`

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
│   ├── JwtSigningKey.cs                 # Signing key pro JWT
│   └── JwtTokenService.cs               # GenerateAccessToken() + GenerateRefreshToken()
├── Console/
│   └── ScrapeEagleFeathersCommand.cs    # dotnet run -- scrape
├── Controllers/
│   ├── AnnouncementsController.cs       # POST|GET /api/organisations/{id}/announcements, PUT|DELETE /api/announcements/{id}
│   ├── AuthController.cs                # POST /api/auth/{register,login,refresh,logout}
│   ├── ChallengesController.cs          # GET /api/challenges/current, GET /api/challenges/{id}, POST /api/challenges/{id}/{pin,unpin}
│   ├── CommentsController.cs            # POST|GET /api/posts/{id}/comments, POST|GET /api/comments/{id}/comments, PUT|DELETE /api/comments/{id}
│   ├── EagleFeathersController.cs       # GET /api/eagle-feathers[/{id}], GET /api/eagle-feathers/lights[/{light}/sections[/{section}]]  [Authorize]
│   ├── FeedController.cs                # GET /api/feed
│   ├── FollowersController.cs           # POST|DELETE /api/users/{id}/follow, GET /api/users/{id}/{followers,following}
│   ├── LikesController.cs               # POST|DELETE /api/posts/{id}/likes, POST|DELETE /api/comments/{id}/likes
│   ├── OrganisationInvitesController.cs # POST|GET /api/organisation-invites, POST /api/organisation-invites/{id}/{accept,decline}
│   ├── OrganisationsController.cs       # POST|GET /api/organisations[/{id}], PUT /api/organisations/{id}, GET /api/organisations/{id}/{members,leaderboard,feed}
│   ├── PostsController.cs               # GET|PUT|DELETE /api/posts/{id}, POST /api/user-eagle-feathers/{uefId}/posts
│   ├── UserEagleFeathersController.cs   # /api/user-eagle-feathers[/{id}[/retry|/approve|/reject]], /api/user-eagle-feathers/pending
│   └── UsersController.cs               # GET /api/users/{id}[/posts|/eagle-feathers|/stats], GET|PUT|DELETE /api/user, DELETE /api/user/organisation
├── Data/
│   └── AppDbContext.cs                  # EF Core DbContext — používá se přímo v handlerech
├── Exceptions/                          # NotFoundException, ConflictException, UnauthorizedException, ForbiddenException
├── Extensions/
│   └── ClaimsPrincipalExtensions.cs     # GetUserId() — čte sub claim z JWT
├── Features/                            # CQRS handlery (MediatR)
│   ├── Announcements/                   # CreateAnnouncementCommand, GetAnnouncementsQuery, UpdateAnnouncementCommand, DeleteAnnouncementCommand
│   ├── Auth/                            # RegisterCommand, LoginCommand, LogoutCommand, RefreshCommand
│   ├── Challenges/                      # GetCurrentChallengeQuery, GetChallengeQuery, PinChallengeCommand, UnpinChallengeCommand, GenerateWeeklyChallengeCommand, ChallengeProjections
│   ├── Comments/                        # CreateCommentCommand, GetCommentsQuery, UpdateCommentCommand, DeleteCommentCommand
│   ├── EagleFeathers/                   # UpdateOrCreateEagleFeather.cs (UpdateOrCreateEagleFeatherCommand), GetEagleFeather*Query
│   ├── Feed/                            # GetPersonalFeedQuery
│   ├── Followers/                       # FollowUserCommand, UnfollowUserCommand, GetFollowersQuery, GetFollowingQuery
│   ├── Likes/                           # LikeTargetCommand, UnlikeTargetCommand
│   ├── OrganisationInvites/             # SendOrganisationInviteCommand, GetOrganisationInvitesQuery, AcceptOrganisationInviteCommand, DeclineOrganisationInviteCommand
│   ├── Organisations/                   # CreateOrganisationCommand, GetOrganisationQuery, UpdateOrganisationCommand, GetOrganisationMembersQuery, GetOrganisationLeaderboardQuery, GetOrganisationFeedQuery
│   ├── Posts/                           # CreatePostCommand, GetPostQuery, UpdatePostCommand, DeletePostCommand
│   ├── Shared/                          # PrivacyGuard (privacy pravidlo), PolymorphicTargetHelper, CommentCascadeHelper
│   ├── UserEagleFeathers/               # CreateUserEagleFeatherCommand, DeleteUserEagleFeatherCommand, RetryUserEagleFeatherCommand, GetMyUserEagleFeathersQuery, GetPendingUserEagleFeathersQuery, Approve/RejectUserEagleFeatherCommand
│   └── Users/                           # GetUserQuery, GetUserPostsQuery, GetUserEagleFeathersQuery, GetUserStatsQuery, GetCurrentUserQuery, UpdateUserCommand, DeleteUserCommand, LeaveOrganisationCommand
├── Middleware/
│   └── ExceptionHandlerMiddleware.cs
├── Migrations/
├── Models/                              # EF Core entity (User, Organisation, OrganisationInvite, EagleFeather, RefreshToken, Post, Like, Comment, Follower, Challenge, UserChallenge, UserEagleFeather, Announcement, Gender, UserRole, OrganisationInviteStatus, EagleFeatherStatus, LikeableType, CommentableType)
├── OpenApi/
│   ├── BearerSecuritySchemeTransformer.cs  # Přidá JWT Bearer do OpenAPI schématu
│   └── EnumSchemaTransformer.cs
├── Pagination/                          # PaginationQuery (page, pageSize; max 100), PagedResponse<T>, PaginationExtensions
├── Queries/                             # IQueryOptions.cs (podsložky jsou prázdné)
├── Requests/                            # Request DTO (RegisterRequest, LoginRequest, RefreshRequest, UpdateUserRequest, CreatePostRequest, CreateCommentRequest, CreateUserEagleFeatherRequest, ModerateUserEagleFeatherRequest, CreateAnnouncementRequest, ...)
├── Responses/                           # AuthResponse, UserMeResponse, UserProfileResponse, PublicUserResponse, UserStatsResponse, OrganisationResponse, OrganisationMemberResponse, OrganisationInviteResponse, LeaderboardEntryResponse, PostResponse, CommentResponse, LikeResponse, FollowResponse, ChallengeResponse, CurrentChallengeResponse, EagleFeatherResponse, UserEagleFeatherResponse, AnnouncementResponse
├── Services/
│   ├── DemoDataSeeder.cs                # Demo data (jen při DemoSeeding:Enabled, ne v Testing)
│   ├── DemoDataSeederService.cs         # BackgroundService — spouští seeder každých 24 hodin
│   ├── RefreshTokenCleanupService.cs    # BackgroundService — denně maže expired/revoked refresh tokeny
│   ├── SvitekScraper.cs                 # Scraper woodcraft.cz (Windows-1250 encoding)
│   └── WeeklyChallengeRotationService.cs # BackgroundService — hodinový tick, generuje týdenní výzvu
└── Validators/                          # Jeden validator na request (`<Request>Validator.cs`), Shared/ pro sdílená pravidla (UserFieldRules.cs)

TrilobitCS.Tests/
├── ApiCollection.cs                     # Sdílí 1 PostgreSQL kontejner napříč testy [Collection("Api")]
├── ApiTestBase.cs
├── TrilobitWebApplicationFactory.cs
├── Announcements/                       # AnnouncementsApiTests
├── Auth/                                # RegisterApiTests, LoginApiTests, LogoutApiTests, RefreshApiTests
├── Comments/                            # CommentsApiTests
├── EagleFeathers/                       # EagleFeathersApiTests, SvitekScraperTests, Fixtures/
├── Feed/                                # FeedApiTests
├── Followers/                           # FollowersApiTests
├── Likes/                               # LikesApiTests
├── OrganisationInvites/                 # OrganisationInvitesApiTests
├── Organisations/                       # OrganisationApiTests, OrganisationFeedApiTests
├── Privacy/                             # PrivacyEnforcementApiTests — tabulka gated endpointů (viz „Privacy model")
├── UserEagleFeathers/                   # UserEagleFeathersApiTests
├── Users/                               # UsersApiTests (GET, GET me, PUT, DELETE, Leave Organisation)
└── Factories/                           # Bogus generátory request DTO (<Model>RequestFactory.cs)
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

Integrační testy přes Testcontainers běží nad reálnou PostgreSQL, takže repozitáře nejsou potřeba ani pro mockování. Vzor: `Features/EagleFeathers/UpdateOrCreateEagleFeather.cs` (handler `UpdateOrCreateEagleFeatherHandler`).

### Response objekty — profily

- `PublicUserResponse` — štíhlý tvar pro listy uživatelů (bez emailu), vrací `GET /api/users/{id}/followers|following`
- `UserProfileResponse` — profil cizího/veřejného uživatele, vrací `GET /api/users/{id}`; počty jsou `null`, pokud je profil privátní a viewer ho nesleduje (viz „Privacy model")
- `UserMeResponse` — vlastní profil (s emailem, `role`, `organisationId`, `isPrivate` a počty), vrací `GET /api/user/me` a `PUT /api/user`
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

### User Eagle Feathers TODO (známé zjednodušení pro MVP)

- **Pending fronta:** dnes vrací všechny pending UEF v systému. Later: 2. subresource `/api/user-eagle-feathers/pending/my-org` pro filtr na Leaderovu organizaci (FE zobrazí 2 subkarty „Všichni" / „Moje org").
- **UEF filtry v GET `/api/user/eagle-feathers`:** dnes bez query filtrů. Later: `?status`, `?isGrandChallenge`, `?eagleFeatherId`.
- **Retry spam:** `POST /retry` bez rate limitu / retry_count / rejected_at. Later: pokud v praxi spam, přidat sloupec `retry_count` + limit.
- **Grand challenge upgrade:** `is_grand_challenge` je immutable. Známé limitation: dítě se schváleným činem, které chce jít na velký čin téhož pera, musí DELETE + POST nový (ztratí historii Approved). Produktové rozhodnutí: paralelní čin + velký čin na jednom peru? Řešení: změnit UNIQUE na `(user_id, eagle_feather_id, is_grand_challenge)` + rozhodnout leaderboard logiku.

### Privacy model

Viditelnost obsahu uživatele je definovaná na jednom místě: `Features/Shared/PrivacyGuard.cs` (statická třída). Handlery ji volají explicitně — žádný EF global query filter, žádná RLS.

Pravidlo (V = viewer, T = cílový uživatel, P = post, A = autor postu):

```
CanViewProfile(V, T) = V.Id == T.Id || !T.IsPrivate || V sleduje T
CanViewPost(V, P)    = CanViewProfile(V, A)
                       || (P.OrganisationId != null && P.OrganisationId == V.OrganisationId)
```

- `ProfileVisibleTo(viewerId)` a `PostVisibleTo(viewerId, viewerOrgId)` jsou `Expression<Func<…, bool>>` predikáty pro použití přímo v EF dotazech (`Where`). `PostVisibleTo` je psaný explicitně, ne skládaný, aby ho EF převedl do SQL.
- `EnsureProfileVisibleAsync` hodí 403 pro skrytý profil. `IsPostVisibleAsync` vrací `bool`, u komentářů a lajků se nejprve přes `ResolveRootPostIdAsync` (walk-up reply chain až na root post) najde post.
- Viewer org se načítá jednou per request (`GetViewerOrganisationIdAsync`).

**Gate tabulka:**

| Gate | Odpověď při zamítnutí | Endpointy |
|---|---|---|
| **Profile gate** | 403 `errors.user_is_private` | `GET /api/users/{id}/posts`, `/eagle-feathers`, `/stats`, `GET /api/users/{id}/followers`, `/following` |
| **Post gate (single)** | 404 — **stejný klíč jako pro neexistující cíl na dané cestě**, takže skrytý a neexistující post nelze rozlišit | `GET /api/posts/{id}` → `errors.post_not_found`; `GET|POST /api/posts/{id}/comments`, `GET|POST /api/comments/{id}/comments` → `errors.commentable_target_not_found`; `POST /api/posts/{id}/likes`, `POST /api/comments/{id}/likes` → `errors.likeable_target_not_found` |
| **Post gate (list filter)** | položka se vynechá, stránka se nerozbije | `GET /api/organisations/{id}/feed` |
| **Profile shape** | 200, počty `null` | `GET /api/users/{id}` (`UserProfileResponse`) |

Záměrné výjimky:
- **Unlike** (`DELETE /api/posts/{id}/likes`, `DELETE /api/comments/{id}/likes`) nemá gate. Vrací vždy 204 a neprozrazuje obsah.
- **Úprava a mazání obsahu** (`PUT|DELETE /api/posts/{id}`, `PUT|DELETE /api/comments/{id}`): autor projde vždy. Ne-autor dostane u skrytého postu/komentáře 404 (`errors.post_not_found` / `errors.comment_not_found`, stejně jako u neexistujícího id), ne 403. 403 `errors.forbidden` dostane jen ne-autor viditelného obsahu.
- **Org-mate, který nesleduje privátního uživatele:** jeho profilové listy dostane 403, ale jeho posty v org feedu, `GET /api/posts/{id}` a komentáře/lajky vidí (přes `organisationId`).
- **Členství v organizaci:** `GET /api/organisations/{id}/members` a `GET /api/organisations/{id}/leaderboard` ověřují jen existenci organizace, ne členství. Každý přihlášený uživatel vidí členy a leaderboard libovolné organizace. Jde o záměrnou výjimku z privacy pravidla: členové i privátní se zobrazují včetně `approvedFeatherCount`.
- **Členství v organizaci (announcements):** `GET /api/organisations/{id}/announcements` vyžaduje členství (`user.organisationId == id`).
- **Self-only nebo role-only:** `GET /api/user/eagle-feathers`, `GET /api/organisation-invites` (self), `GET /api/user-eagle-feathers/pending` (Leader moderace vidí vše), `GET /api/feed` (jen self + followed).
- **Publikace do organizace:** `CreatePost` s `organisationId` povolí jen autorovi, který je členem té organizace. Jinak 403 `errors.not_organisation_member`. Tím se nedá publikovat do cizí org a obejít privacy pravidlo.

**Konvence pro nové endpointy:**
- Každý nový endpoint, který vrací obsah uživatele (profil, posty, komentáře, lajky, pera, statistiky, seznamy), musí volat `PrivacyGuard` (`EnsureProfileVisibleAsync`, `IsPostVisibleAsync` nebo `PostVisibleTo`).
- Pro cíl, který je post nebo komentář, použij 404 se stejným klíčem jako pro neexistující cíl na dané cestě.
- Každý takový endpoint přidej jako řádek do `TrilobitCS.Tests/Privacy/PrivacyEnforcementApiTests.cs` (tabulka gated endpointů × role viewera).

---

## Existující API endpointy

Stránkované endpointy přijímají `?page=1&pageSize=20` (`pageSize` max 100). Neplatné hodnoty → 422. Odpověď `PagedResponse<T>` má tvar `{ "items": [...], "page": 1, "pageSize": 20, "totalCount": 0, "totalPages": 0 }`.

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
| GET | `/api/eagle-feathers` | **ano** | `PagedResponse<EagleFeatherResponse>` (200) | Všechna pera (stránkované) |
| GET | `/api/eagle-feathers/{id}` | **ano** | `EagleFeatherResponse` (200) | Jedno pero |
| GET | `/api/eagle-feathers/lights` | **ano** | `int[]` (200) | Seznam světel |
| GET | `/api/eagle-feathers/lights/{light}/sections` | **ano** | `string[]` (200) | Sekce daného světla (např. `1A`, `1B`) |
| GET | `/api/eagle-feathers/lights/{light}/sections/{section}` | **ano** | `EagleFeatherResponse[]` (200) | Pera v sekci |

### Users (`/api/users`, `/api/user`)

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| GET | `/api/users/{id}` | ano | — | `UserProfileResponse` (200) | Profil uživatele (bez emailu). Pro privátní profil, který viewer nesleduje, jsou počty `null` (`isPrivate: true`, `isFollowing: false`) |
| GET | `/api/user/me` | ano | — | `UserMeResponse` (200) | Vlastní profil (s emailem, role, organisationId, isPrivate a počty) |
| PUT | `/api/user` | ano | `UpdateUserRequest` | `UserMeResponse` (200) | Aktualizace vlastního profilu. Pole `nickname`, `firstName`, `lastName`, `gender`, `birthDate`, `profilePicture` se posílají celá; `isPrivate` je volitelné (když chybí, nemění se) |
| DELETE | `/api/user` | ano | — | 204 No Content | Smazání vlastního účtu (včetně refresh tokenů) |
| DELETE | `/api/user/organisation` | ano | — | 204 No Content | Odchod z organizace (Leader vlastní org blokován) |

**UpdateUserRequest:**
```json
{
  "nickname": "jan99",
  "firstName": "Jan",
  "lastName": "Novák",
  "gender": 0,
  "birthDate": "2000-01-01",
  "profilePicture": null,
  "isPrivate": true
}
```

**UserProfileResponse:**
```json
{
  "id": 1,
  "nickname": "jan99",
  "firstName": "Jan",
  "lastName": "Novák",
  "profilePicture": null,
  "createdAt": "2026-04-25T...",
  "isPrivate": false,
  "isFollowing": true,
  "followerCount": 12,
  "followingCount": 8,
  "approvedFeatherCount": 3,
  "postCount": 5
}
```

**UserMeResponse:**
```json
{
  "id": 1,
  "nickname": "jan99",
  "firstName": "Jan",
  "lastName": "Novák",
  "email": "jan@example.com",
  "gender": 0,
  "birthDate": "2000-01-01",
  "profilePicture": null,
  "role": 0,
  "organisationId": null,
  "createdAt": "2026-04-25T...",
  "isPrivate": false,
  "followerCount": 12,
  "followingCount": 8,
  "approvedFeatherCount": 3,
  "postCount": 5
}
```

**PublicUserResponse** (položky listů followers/following):
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

### User profile aggregates (`/api/users/{id}/posts|eagle-feathers|stats`)

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| GET | `/api/users/{id}/posts` | ano | — | `PagedResponse<PostResponse>` (200) | Posty uživatele, řazeno `createdAt DESC`. 403 `errors.user_is_private` pro cizí privátní profil bez followingu, 404 `errors.user_not_found` |
| GET | `/api/users/{id}/eagle-feathers` | ano | — | `PagedResponse<UserEagleFeatherResponse>` (200) | Pera uživatele. Vlastní profil vidí všechny statusy, ostatní jen `Approved`. Stejné 403/404 jako výše |
| GET | `/api/users/{id}/stats` | ano | — | `UserStatsResponse` (200) | Počty: posty, schválená pera, followers, following. Stejné 403/404 jako výše |

**UserStatsResponse:**
```json
{
  "postCount": 5,
  "approvedFeatherCount": 3,
  "followerCount": 12,
  "followingCount": 8
}
```

### Organisations (`/api/organisations`)

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| POST | `/api/organisations` | Leader | `CreateOrganisationRequest` | `OrganisationResponse` (200) | Vytvoří org, leader_id = current user |
| GET | `/api/organisations/{id}` | ano | — | `OrganisationResponse` (200) | Detail organizace s počtem členů |
| PUT | `/api/organisations/{id}` | Leader dané org | `UpdateOrganisationRequest` | `OrganisationResponse` (200) | Aktualizace org (jen vlastní org) |
| GET | `/api/organisations/{id}/members` | ano | — | `PagedResponse<OrganisationMemberResponse>` (200) | Seznam členů organizace (bez kontroly členství volajícího). `isFollowing` = volající sleduje člena. 404 `errors.organisation_not_found` |

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

**OrganisationMemberResponse:**
```json
{
  "id": 1,
  "nickname": "jan99",
  "firstName": "Jan",
  "lastName": "Novák",
  "profilePicture": null,
  "role": 0,
  "createdAt": "2026-04-25T...",
  "isFollowing": true
}
```

### Leaderboard (`/api/organisations/{id}/leaderboard`)

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| GET | `/api/organisations/{id}/leaderboard` | ano | — | `PagedResponse<LeaderboardEntryResponse>` (200) | Žebříček členů organizace podle počtu schválených per (`Status == Approved`). Řazení `approvedFeatherCount DESC, userId ASC`, členové s 0 jsou zahrnuti. 404 `errors.organisation_not_found` |

**LeaderboardEntryResponse** (položka `items`):
```json
{
  "rank": 1,
  "userId": 3,
  "nickname": "jan99",
  "profilePicture": null,
  "approvedFeatherCount": 7
}
```

`rank = (page - 1) * pageSize + index + 1`, počítá se až nad stránkou (stejné pořadí napříč stránkami).

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

### Feed (`/api/feed`, `/api/organisations/{id}/feed`)

| Method | Path | Auth | Response | Popis |
|---|---|---|---|---|
| GET | `/api/feed` | ano | `PagedResponse<PostResponse>` (200) | Osobní feed: moje posty + posty sledovaných uživatelů (bez ohledu na `organisationId`), řazeno `createdAt DESC` |
| GET | `/api/organisations/{id}/feed` | ano | `PagedResponse<PostResponse>` (200) | Posty publikované do dané organizace (`organisationId == id`), řazeno `createdAt DESC`. Filtrováno privacy pravidlem (`PrivacyGuard.PostVisibleTo`): posty privátního autora vidí jen jeho followeři, autor a členové téže organizace. Přístupné komukoli přihlášenému, bez kontroly členství organizace (na rozdíl od Announcements). 404 `errors.organisation_not_found` |

Obě vrací stejný `PostResponse` tvar jako `GET /api/posts/{id}` (viz níže).

### Posts (`/api/user-eagle-feathers/{uefId}/posts`, `/api/posts`)

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| POST | `/api/user-eagle-feathers/{uefId}/posts` | ano | `CreatePostRequest` | `PostResponse` (200) | Vytvoří post vázaný na user_eagle_feather; nastaví UEF.IsCompleted = true. Pokud je `organisationId` vyplněné, autor musí být členem té organizace: 403 `errors.not_organisation_member`. 404 `errors.user_eagle_feather_not_found` / `errors.organisation_not_found`, 403 `errors.forbidden` pro cizí UEF |
| GET | `/api/posts/{id}` | ano | — | `PostResponse` (200) | Detail příspěvku. Skrytý post (privacy gate) vrací 404 `errors.post_not_found`, stejně jako neexistující |
| PUT | `/api/posts/{id}` | ano (autor) | `UpdatePostRequest` | `PostResponse` (200) | Aktualizace příspěvku |
| DELETE | `/api/posts/{id}` | ano (autor) | — | 204 No Content | Smazání příspěvku; nastaví UEF.IsCompleted = false |

**CreatePostRequest:**
```json
{
  "content": "string (max 2000, optional)",
  "imageUrl": "string (max 255, optional)",
  "organisationId": "int? (optional)",
  "challengeId": "int? (optional)"
}
```

**PostResponse:**
```json
{
  "id": 1,
  "author": { "id": 1, "nickname": "jan99" },
  "organisationId": null,
  "content": "...",
  "imageUrl": null,
  "userEagleFeatherId": 5,
  "challengeId": null,
  "likeCount": 0,
  "commentCount": 0,
  "createdAt": "2026-05-15T..."
}
```

### Followers (`/api/users/{id}/follow`, `/api/users/{id}/followers`, `/api/users/{id}/following`)

Sledování je jednostranné, bez schválení druhé strany — i na privátní profil lze sledovat okamžitě. Rate limit policy `social` (viz „Rate limiting").

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| POST | `/api/users/{id}/follow` | ano | — | `FollowResponse` (200) | Sleduje uživatele (idempotentní — opakované volání vrátí existující vztah). 404 `errors.user_not_found`, 422 `errors.cannot_follow_self` |
| DELETE | `/api/users/{id}/follow` | ano | — | 204 No Content | Zruší sledování (no-op, pokud nesleduje) |
| GET | `/api/users/{id}/followers` | ano | — | `PagedResponse<PublicUserResponse>` (200) | Followeři uživatele. Profile gate: 403 `errors.user_is_private`, 404 `errors.user_not_found` |
| GET | `/api/users/{id}/following` | ano | — | `PagedResponse<PublicUserResponse>` (200) | Uživatelé, které `{id}` sleduje. Stejné 403/404 jako `followers` |

**FollowResponse:**
```json
{
  "followerId": 3,
  "followingId": 1,
  "createdAt": "2026-10-14T..."
}
```

### Comments (`/api/posts/{id}/comments`, `/api/comments/{id}/comments`, `/api/comments/{id}`)

Komentáře jsou polymorfní: na post (`commentableType = 0`) nebo na jiný komentář (`commentableType = 1`). GET vrací jen přímé potomky. Hidden post a neexistující cíl vrací stejnou 404 odpověď (`errors.commentable_target_not_found`), viz „Privacy model".

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| POST | `/api/posts/{id}/comments` | ano | `CreateCommentRequest` | `CommentResponse` (200) | Komentář k postu. Rate limit `social`. 404 `errors.commentable_target_not_found`, 422 validace |
| GET | `/api/posts/{id}/comments` | ano | — | `PagedResponse<CommentResponse>` (200) | Přímé komentáře k postu. 404 `errors.commentable_target_not_found` |
| POST | `/api/comments/{id}/comments` | ano | `CreateCommentRequest` | `CommentResponse` (200) | Odpověď na komentář. Rate limit `social`. 404 `errors.commentable_target_not_found` |
| GET | `/api/comments/{id}/comments` | ano | — | `PagedResponse<CommentResponse>` (200) | Přímé odpovědi na komentář. 404 `errors.commentable_target_not_found` |
| PUT | `/api/comments/{id}` | autor | `UpdateCommentRequest` | `CommentResponse` (200) | Úprava vlastního komentáře. 403 `errors.forbidden`, 404 `errors.comment_not_found`, 422 validace |
| DELETE | `/api/comments/{id}` | autor | — | 204 No Content | Smaže komentář a rekurzivně všechny potomky včetně jejich lajků. 403 `errors.forbidden`, 404 `errors.comment_not_found` |

**CreateCommentRequest / UpdateCommentRequest:**
```json
{ "content": "string (required)" }
```

**CommentResponse:**
```json
{
  "id": 1,
  "author": { "id": 3, "nickname": "jan99", "profilePicture": null },
  "commentableType": 0,
  "commentableId": 5,
  "content": "Super výkon!",
  "createdAt": "2026-10-14T..."
}
```

### Likes (`/api/posts/{id}/likes`, `/api/comments/{id}/likes`)

Lajk je idempotentní (opakované volání vrátí existující lajk), self-like je povolen. Unlike je no-op, pokud lajk neexistuje.

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| POST | `/api/posts/{id}/likes` | ano | — | `LikeResponse` (200) | Lajk postu. 404 `errors.likeable_target_not_found` (i pro skrytý post). Rate limit `social` |
| DELETE | `/api/posts/{id}/likes` | ano | — | 204 No Content | Odlajkování postu (bez privacy gate). Rate limit `social` |
| POST | `/api/comments/{id}/likes` | ano | — | `LikeResponse` (200) | Lajk komentáře. 404 `errors.likeable_target_not_found` (i když je root post skrytý). Rate limit `social` |
| DELETE | `/api/comments/{id}/likes` | ano | — | 204 No Content | Odlajkování komentáře (bez privacy gate). Rate limit `social` |

**LikeResponse:**
```json
{
  "id": 1,
  "userId": 3,
  "likeableType": 0,
  "likeableId": 5,
  "createdAt": "2026-10-14T..."
}
```

### Challenges (`/api/challenges`)

Týdenní výzva je jedna globální `Challenge` (náhodné pero z katalogu jako zadání), generovaná `WeeklyChallengeRotationService` (viz „Cross-cutting infrastruktura"). Uživatel si výzvu může připnout (`pin`) a odepnout (`unpin`). Pin je vázaný na aktivní výzvu.

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| GET | `/api/challenges/current` | ano | — | `CurrentChallengeResponse` (200) | Nejnovější aktivní výzva (`ValidFrom <= now < ValidTo`). 404 `errors.challenge_not_found`, pokud žádná není |
| GET | `/api/challenges/{id}` | ano | — | `ChallengeResponse` (200) | Výzva podle id (i neaktivní). 404 `errors.challenge_not_found` |
| POST | `/api/challenges/{id}/pin` | ano | — | `CurrentChallengeResponse` (200) | Připne aktivní výzvu. Znovu připnutí dříve odepnuté výzvy je povoleno. 404 `errors.challenge_not_found` (i pro neaktivní), 422 `errors.challenge_already_pinned` |
| POST | `/api/challenges/{id}/unpin` | ano | — | `CurrentChallengeResponse` (200) | Odepne výzvu. 404 `errors.challenge_not_found`, 422 `errors.challenge_not_pinned` |

**ChallengeResponse:**
```json
{
  "id": 7,
  "title": "Zimní výzva",
  "description": "Splň libovolné pero 1. světla venku v mrazu.",
  "eagleFeatherId": 12,
  "difficultyLevel": 2,
  "validFrom": "2026-10-12T00:00:00Z",
  "validTo": "2026-10-19T00:00:00Z",
  "createdAt": "2026-10-12T00:00:00Z"
}
```

**CurrentChallengeResponse** (odpovědi pin/unpin i current):
```json
{
  "challenge": { "id": 7, "title": "...", "description": "...", "eagleFeatherId": 12, "difficultyLevel": 2, "validFrom": "...", "validTo": "...", "createdAt": "..." },
  "isPinned": true,
  "pinnedCount": 4
}
```

`pinnedCount` = počet uživatelů, kteří výzvu právě drží připnutou (`UnpinnedAt == null`). `isPinned` je z pohledu volajícího.

### User Eagle Feathers (`/api/user-eagle-feathers`, `/api/user/eagle-feathers`)

Workflow získání pera. UEF = jeden pokus dítěte získat konkrétní pero. Vzniká explicitně (dítě si pero „nastartuje"), posty se k němu přidávají přes existující `POST /api/user-eagle-feathers/{uefId}/posts`.

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| POST | `/api/user-eagle-feathers` | ano | `CreateUserEagleFeatherRequest` | `UserEagleFeatherResponse` (201) | Vytvoří UEF (Status=Pending). 404 pokud pero neexistuje, 422 pro duplicitu (user, feather). |
| GET | `/api/user/eagle-feathers` | ano | — | `PagedResponse<UserEagleFeatherResponse>` (200) | Moje UEF (paginated, bez filtrů). |
| DELETE | `/api/user-eagle-feathers/{id}` | vlastník | — | 204 No Content | Smaže UEF + **cascade smaže připojené posty**. Povoleno ve všech statusech (i Approved). |
| POST | `/api/user-eagle-feathers/{id}/retry` | vlastník | — | `UserEagleFeatherResponse` (201) | Rejected → Pending. Vynuluje VerifiedById/EarnedAt/ModeratorNote. 422 pokud není Rejected. |
| GET | `/api/user-eagle-feathers/pending` | Leader | — | `PagedResponse<UserEagleFeatherResponse>` (200) | Všechny pending UEF v systému (napříč orgy). 403 pro non-Leadera. |
| POST | `/api/user-eagle-feathers/{id}/approve` | Leader | `ModerateUserEagleFeatherRequest` | `UserEagleFeatherResponse` (201) | Status=Approved, EarnedAt=now, VerifiedById=leader, ModeratorNote=note. 422 pokud není Pending. |
| POST | `/api/user-eagle-feathers/{id}/reject` | Leader | `ModerateUserEagleFeatherRequest` | `UserEagleFeatherResponse` (201) | Status=Rejected, VerifiedById=leader, ModeratorNote=note. **Posty se nemažou.** 422 pokud není Pending. |

Schvalovat/zamítat smí **kterýkoli Leader** (`role = Leader`), bez vazby na organizaci dítěte. `IsGrandChallenge` je immutable po vytvoření.

**CreateUserEagleFeatherRequest:**
```json
{ "eagleFeatherId": 5, "isGrandChallenge": false }
```

**ModerateUserEagleFeatherRequest** (approve i reject):
```json
{ "note": "string? (max 255, optional)" }
```

**UserEagleFeatherResponse:**
```json
{
  "id": 1,
  "userId": 3,
  "eagleFeatherId": 5,
  "isGrandChallenge": false,
  "isCompleted": false,
  "status": 0,
  "verifiedById": null,
  "moderatorNote": null,
  "earnedAt": null,
  "createdAt": "2026-07-13T..."
}
```
Status: `0` = Pending, `1` = Approved, `2` = Rejected (serializováno jako int).

### Announcements (`/api/organisations/{id}/announcements`, `/api/announcements`)

Oznámení organizace. Vytváří/upravuje/maže jen Leader dané organizace, číst (paged) smí jen její členové.

| Method | Path | Auth | Request body | Response | Popis |
|---|---|---|---|---|---|
| POST | `/api/organisations/{id}/announcements` | Leader dané org | `CreateAnnouncementRequest` | `AnnouncementResponse` (201) | Vytvoří announcement. 403 pokud caller není Leader dané org, 404 pro neexistující org, 422 pro validaci |
| GET | `/api/organisations/{id}/announcements` | člen dané org | — | `PagedResponse<AnnouncementResponse>` (200) | Announcements organizace, řazeno `createdAt DESC`. 403 pokud caller není členem (`user.organisationId == id`), 404 pro neexistující org |
| PUT | `/api/announcements/{id}` | Leader dané org | `UpdateAnnouncementRequest` | `AnnouncementResponse` (200) | Aktualizace (patch-style, alespoň jedno pole). Nastaví `updatedAt`. 403/404 jako výše |
| DELETE | `/api/announcements/{id}` | Leader dané org | — | 204 No Content | Hard delete. 403/404 jako výše |

**CreateAnnouncementRequest:**
```json
{ "title": "string (max 60, required)", "content": "string (max 300, required)" }
```

**UpdateAnnouncementRequest** — obě pole optional, alespoň jedno vyplněné:
```json
{ "title": "string? (max 60)", "content": "string? (max 300)" }
```

**AnnouncementResponse:**
```json
{
  "id": 1,
  "organisationId": 2,
  "title": "Brigáda o víkendu",
  "content": "...",
  "createdBy": { "id": 3, "nickname": "vedouci123" },
  "createdAt": "2026-08-26T...",
  "updatedAt": null
}
```

---

## Databázové schéma (kompletní)

```
-- IMPLEMENTOVÁNO (API existuje)
-- Názvy níže jsou logické (snake_case). Skutečné názvy tabulek a sloupců v EF migracích jsou PascalCase
-- (např. tabulka `Users`, sloupec `IsPrivate`), protože projekt nepoužívá snake_case naming convention.
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
  is_private      boolean      NOT NULL DEFAULT false  -- migrace AddUserIsPrivate; true = profil a posty vidí jen followers (+ autor; posty v org feedu navíc členové té org, viz „Privacy model")
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
  status          int          DEFAULT 0       -- enum: 0=Pending, 1=Accepted, 2=Declined (primární zdroj pravdy)
  accepted_at     timestamp    (nullable — nastaveno handlerem při Accept)
  declined_at     timestamp    (nullable — nastaveno handlerem při Decline)
  created_at      timestamp    DEFAULT now()
  UNIQUE(invited_user_id, organisation_id, declined_at)  -- PostgreSQL NULL!=NULL: pending (declined_at=NULL) koexistuje s declined (declined_at!=NULL)

announcements
  id              PK
  organisation_id FK → organisations.id (Cascade delete)
  title           varchar(60)  NOT NULL
  content         varchar(300) NOT NULL
  created_by_id   FK → users.id (Restrict on delete)
  created_at      timestamp
  updated_at      timestamp (nullable — nastaveno jen při update)
  INDEX(organisation_id)

followers  -- API implementováno (follow/unfollow/followers/following)
  id            PK
  follower_id   FK → users.id
  following_id  FK → users.id
  created_at    timestamp
  UNIQUE(follower_id, following_id)
  CHECK(follower_id <> following_id)

user_eagle_feathers  -- API implementováno (create/list/delete/retry/pending/approve/reject)
  id               PK
  user_id          FK → users.id
  eagle_feather_id FK → eagle_feathers.id
  is_grand_challenge boolean DEFAULT false     -- false=čin, true=velký čin (immutable po vytvoření)
  is_completed     boolean DEFAULT false       -- true při vytvoření postu, false při smazání postu (aplikační logika)
  status           int          DEFAULT 0      -- enum: 0=Pending, 1=Approved, 2=Rejected
  verified_by      FK → users.id (leader, nullable)
  moderator_note   text (nullable)             -- poznámka Leadera při approve/reject (max 255 hlídá validátor); vynulována při retry
  earned_at        timestamp (nullable)        -- nastaveno jen při Approved
  created_at       timestamp
  UNIQUE(user_id, eagle_feather_id)

posts
  id                    PK
  user_id               FK → users.id
  organisation_id       FK → organisations.id (nullable — null = nepublikováno do organizace; viditelnost řeší privacy pravidlo)
  user_eagle_feather_id FK → user_eagle_feathers.id NOT NULL, Cascade on delete — smazání UEF smaže i posty
  challenge_id          FK → challenges.id (nullable — post vznikl přes challenge)
  content               text
  image_url             varchar(255)
  created_at            timestamp

likes  -- API implementováno (lajky postů a komentářů)
  id             PK
  user_id        FK → users.id
  likeable_type  int          -- enum: 0=Posts, 1=Comments
  likeable_id    int          -- FK není vynuceno DB, zajišťuje aplikační vrstva
  created_at     timestamp
  UNIQUE(user_id, likeable_type, likeable_id)
  INDEX(likeable_type, likeable_id)

comments  -- API implementováno (komentáře a odpovědi)
  id               PK
  user_id          FK → users.id
  commentable_type int          -- enum: 0=Posts, 1=Comments
  commentable_id   int          -- FK není vynuceno DB, zajišťuje aplikační vrstva
  content          text NOT NULL
  created_at       timestamp
  INDEX(commentable_type, commentable_id)

challenges  -- API implementováno (current/detail/pin/unpin); generuje WeeklyChallengeRotationService
  id               PK
  eagle_feather_id FK → eagle_feathers.id NOT NULL (Cascade) — náhodně vybrané pero jako zadání výzvy
  title            varchar(150) NOT NULL
  description      text
  difficulty_level int          -- 1–3 hvězdičky
  valid_from       timestamp
  valid_to         timestamp
  created_at       timestamp

user_challenges  -- API implementováno (pin/unpin)
  id           PK
  user_id      FK → users.id (Cascade)
  challenge_id FK → challenges.id (Cascade)
  pinned_at    timestamp NOT NULL DEFAULT now()
  unpinned_at  timestamp (nullable — null = aktuálně pinnuto)
  UNIQUE(user_id, challenge_id)
```

---

## Cross-cutting infrastruktura

### CORS
Allowed origins se čtou z `appsettings.json` (`Cors:AllowedOrigins`). Politika povoluje libovolné hlavičky a metody pro nakonfigurované origins. Pro produkci nastavit na skutečnou doménu frontendu.

### Rate limiting
.NET 9 built-in `AddRateLimiter`. Fixed window, partition podle IP. Dvě policy:
- **`auth`** — **5 requestů / 1 minuta** na `POST /api/auth/login` a `POST /api/auth/refresh` (`[EnableRateLimiting("auth")]`)
- **`social`** — **30 requestů / 1 minuta** na write endpointy sociálních interakcí: follow/unfollow (`/api/users/{id}/follow`), lajky (`/api/posts/{id}/likes`, `/api/comments/{id}/likes`, včetně DELETE), vytvoření a úprava/smazání komentáře (`/api/posts/{id}/comments`, `/api/comments/{id}/comments`, `/api/comments/{id}`)

Rejection status: 429. V testovacím prostředí (`IsEnvironment("Testing")`) je rate limiter vypnutý.

### Refresh token cleanup
`Services/RefreshTokenCleanupService.cs` jako `BackgroundService`. Spouští se každých 24 hodin a maže:
```sql
DELETE FROM refresh_tokens WHERE expires_at < now()
   OR (revoked_at IS NOT NULL AND revoked_at < now() - INTERVAL '30 days')
```

### Weekly challenge rotation
`Services/WeeklyChallengeRotationService.cs` jako `BackgroundService`. Každou hodinu (`PeriodicTimer`) pošle `GenerateWeeklyChallengeCommand(DateTime.UtcNow)` přes MediatR. Výjimky se zalogují a služba běží dál.

- Command běží v transakci s `SELECT pg_advisory_xact_lock(424242)` a trigger vyhodnotí znovu uvnitř zámku, takže více instancí nevytvoří dvě výzvy.
- **Trigger:** generuje se, pokud neexistuje aktivní výzva (`ValidFrom <= now < ValidTo`), nebo nejnovější aktivní má `ValidFrom <= now - 7 dní`. „Aktivní" a „nejnovější" se počítají vůči předanému `Now`.
- Výzva: náhodné pero z katalogu (`Title = feather.Name`, `Description = feather.Challenge`, nezkrácené), `DifficultyLevel` 1–3, `ValidFrom = now`, `ValidTo = now + 7 dní`. Prázdný katalog → warning, bez výjimky.
- Služba se **neregistruje v prostředí `Testing`** (`Program.cs`).
- `DemoDataSeeder` seeduje jen historické výzvy (`ValidTo < now`), aktivní výzvu tedy vygeneruje až rotace.

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
