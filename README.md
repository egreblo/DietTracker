# Ručak — planer ručkova za dvoje

Blazor WebAssembly (PWA) na GitHub Pages + Supabase (Postgres + Auth). Sve na besplatnim planovima.

- Jela (ručno dodana), skup jela iz kojeg se generira tjedan
- "Generiraj tjedan": nasumični raspored ručkova bez ponavljanja, izbjegava prošli tjedan
- Zaključavanje dana, nasumična zamjena, ručni odabir, zamjena dvaju dana, "bez ručka"
- Ispis / spremanje u PDF (print stranica), izvoz u kalendar (`.ics`, događaji `RUČAK - naziv jela`)
- Svijetla / tamna tema, prilagođeno mobitelu (dodaj na početni zaslon)

## Postavljanje (jednom)

### 1. Supabase
1. Napravi projekt na [supabase.com](https://supabase.com) (free).
2. **SQL Editor** → zalijepi i pokreni [`supabase/migrations/001_init.sql`](supabase/migrations/001_init.sql).
3. **Authentication → Sign In / Providers → Email**: isključi *Allow new users to sign up* (nitko drugi se ne može registrirati).
4. **Authentication → Users → Add user**: napravi sebe i curu (email + lozinka, "Auto confirm").
5. **SQL Editor**: upiši vaše emailove i pokreni zadnji (zakomentirani) `insert into public.allowed_users ...` blok iz skripte. Samo korisnici u toj tablici vide podatke.
6. **Project Settings → API**: zapiši *Project URL* i *anon public* ključ. (`service_role` ključ nikad ne ide u kod ni u secrets ove aplikacije.)

### 2. GitHub
1. Napravi **javan** repo (zbog besplatnih Pagesa) i pushaj ovaj projekt na `main`.
2. **Settings → Pages → Source: GitHub Actions**.
3. **Settings → Secrets and variables → Actions**: dodaj `SUPABASE_URL` i `SUPABASE_ANON_KEY`.
4. Push na `main` pokreće `deploy.yml` (testovi → build → deploy). Adresa je `https://<korisnik>.github.io/<repo>/`.
5. Supabase → **Authentication → URL Configuration**: Site URL postavi na tu adresu.

`keepalive.yml` svaka 3 dana zove `ping()` jer se free Supabase projekt pauzira nakon 7 dana neaktivnosti.

## Lokalni razvoj

Napravi `src/DietTracker.Web/wwwroot/appsettings.Development.json` (u `.gitignore`):

```json
{ "Supabase": { "Url": "https://xxxx.supabase.co", "AnonKey": "..." } }
```

```bash
dotnet run --project src/DietTracker.Web
dotnet test
```

## Struktura

- `src/DietTracker.Core` — modeli, `MealGenerator`, `IcsBuilder` (čisti C#, testirano)
- `src/DietTracker.Web` — Blazor UI, servisi za Supabase (obični `HttpClient` prema GoTrue/PostgREST)
- `tests/DietTracker.Tests` — xUnit
- `supabase/migrations` — shema i RLS

## Sljedeće (faza 2)

Procjena kcal/makroa (Open Food Facts), uvoz popularnih jela (TheMealDB), prikaz pripreme u ispisu, doručak/večera, engleski jezik.
