# AgentPlatform

Platforma multi-tenant pentru agenti imobiliari: agenti AI care raspund pe WhatsApp,
cauta proprietati prin RAG si califica leaduri.

## Structura

```
AgentPlatform.Api/        .NET Web API (C#)
AgentPlatform.Client/     React + Vite + TypeScript
supabase/migrations/      schema SQL, aplicata pe Supabase cloud
```

## Stack

**Backend** — .NET (`net10.0`, nu net8.0), EF Core + Npgsql, Pgvector, Hangfire,
Serilog, Groq AI, Twilio (WhatsApp), Scalar pentru documentatie API.

**Frontend** — React 19, Vite, TypeScript, Tailwind 4, `@supabase/supabase-js`,
TanStack Query, React Router, axios, lucide-react.

**DB** — Supabase PostgreSQL (`ppguqihvromohocawsje`), pgvector pentru embeddings.

## Conventii

- Backend: **Controller -> Service -> Repository**. Controllerele nu ating
  niciodata `AppDbContext` direct.
- Toata configurarea sta in `AgentPlatform.Api/appsettings.Development.json`
  (gitignored). `.env.local` din radacina e doar pentru tooling — .NET **nu** il citeste.
- Modelele EF folosesc `[Table]` / `[Column]` cu nume snake_case, ca sa se
  potriveasca peste schema Supabase existenta. Nu generam migrari EF —
  sursa de adevar e `supabase/migrations/`.
- Comentariile din cod sunt in romana, fara diacritice.

## Auth si multi-tenancy

Supabase Auth, nu parole proprii. `agents.id` este FK catre `auth.users(id)`,
deci `auth.uid()` **este** `tenant_id`-ul. Un trigger pe `auth.users` creeaza
automat randul din `agents` la signup.

RLS e activ pe toate tabelele, cu politici per-tenant. `service_role`
(secret key, folosit de API) ocoleste RLS — clientul React, care foloseste
publishable key, nu.

## Comenzi

```bash
# API (deschide automat Scalar in browser)
dotnet watch --project AgentPlatform.Api        # http://localhost:5274/scalar/v1

# dotnet run porneste serverul dar NU deschide browserul (ignora launchBrowser)

# Client
cd AgentPlatform.Client && npm run dev          # http://localhost:5173

# Migrari
supabase db push
```

`GET /api/test/ping` verifica rapid conexiunea la DB.

## Capcane verificate

- **Connection string**: doar poolerul merge — `aws-0-eu-central-1.pooler.supabase.com:5432`,
  user `postgres.ppguqihvromohocawsje`. Hostul direct `db.<ref>.supabase.co` **nu rezolva DNS**
  pe proiectele noi.
- Npgsql 8+ valideaza certificatul la `SSL Mode=Require`; de aceea e nevoie de
  `Trust Server Certificate=true` pe pooler.
- Indexul vectorial e **HNSW**, nu ivfflat: ivfflat construit pe tabela goala
  are recall prost pana la un REINDEX dupa populare.
- `Microsoft.OpenApi` 2.0.0 are o vulnerabilitate high (GHSA-v5pm-xwqc-g5wc),
  intra tranzitiv prin `Microsoft.AspNetCore.OpenApi`. Nerezolvat.

## Stare actuala

Schema aplicata pe cloud, conexiunea la DB confirmata. API-ul are doar
`TestController` (ping) — **inca fara Service/Repository**, e cod de proba
care va fi inlocuit. Frontendul e inca starter-ul Vite gol.
