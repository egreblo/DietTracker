-- Diet Tracker: shema + Row Level Security.
-- Pokreni cijelu skriptu u Supabase dashboardu: SQL Editor -> New query -> Run.

-- 1) Tko smije u aplikaciju -------------------------------------------------
create table if not exists public.allowed_users (
  user_id uuid primary key references auth.users (id) on delete cascade
);
-- RLS uključen, a politika nema => nitko ne može čitati/pisati tablicu preko API-ja.
alter table public.allowed_users enable row level security;

create or replace function public.is_allowed()
returns boolean
language sql
stable
security definer
set search_path = public
as $$
  select exists (select 1 from public.allowed_users where user_id = auth.uid());
$$;

-- 2) Jela ---------------------------------------------------------------------
create table if not exists public.recipes (
  id uuid primary key default gen_random_uuid(),
  name text not null check (char_length(name) between 1 and 200),
  source text not null default 'manual' check (source in ('manual', 'imported')),
  in_pool boolean not null default true,
  ingredients jsonb not null default '[]'::jsonb,
  instructions text,
  link_url text,
  image_url text,
  servings integer not null default 1 check (servings > 0),
  kcal numeric,
  protein numeric,
  carbs numeric,
  fat numeric,
  macros_estimated boolean not null default false,
  external_id text,
  created_at timestamptz not null default now()
);
create unique index if not exists recipes_external_id_key
  on public.recipes (external_id) where external_id is not null;

-- 3) Plan obroka --------------------------------------------------------------
create table if not exists public.plan_entries (
  id uuid primary key default gen_random_uuid(),
  date date not null,
  meal_type text not null default 'lunch' check (meal_type in ('breakfast', 'lunch', 'dinner')),
  recipe_id uuid references public.recipes (id) on delete set null,
  locked boolean not null default false,
  unique (date, meal_type)
);

-- 4) RLS: samo korisnici iz allowed_users --------------------------------------
alter table public.recipes enable row level security;
alter table public.plan_entries enable row level security;

drop policy if exists "household only" on public.recipes;
create policy "household only" on public.recipes
  for all to authenticated
  using (public.is_allowed())
  with check (public.is_allowed());

drop policy if exists "household only" on public.plan_entries;
create policy "household only" on public.plan_entries
  for all to authenticated
  using (public.is_allowed())
  with check (public.is_allowed());

-- Neprijavljeni (anon) ne smiju ništa dirati.
revoke all on public.recipes, public.plan_entries, public.allowed_users from anon;
grant select, insert, update, delete on public.recipes, public.plan_entries to authenticated;

-- 5) Keep-alive: Supabase free projekt se pauzira nakon 7 dana neaktivnosti.
-- GitHub Action povremeno zove ovu funkciju (ne vraća nikakve podatke).
create or replace function public.ping()
returns text
language sql
stable
as $$ select 'ok'::text; $$;
grant execute on function public.ping() to anon, authenticated;

-- 6) NAKON što u Authentication -> Users napraviš vas dvoje, upiši njihove emailove
-- i pokreni ovo (zamijeni adrese):
--
--   insert into public.allowed_users (user_id)
--   select id from auth.users where email in ('tvoj@email.com', 'njen@email.com')
--   on conflict do nothing;
