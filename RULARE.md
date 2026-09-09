# Cum rulez si testez

Cheat sheet scurt. Detalii WhatsApp: [GHID-TESTARE-WHATSAPP.md](GHID-TESTARE-WHATSAPP.md).
Cont de test si token: [CONTURI-DEV.md](CONTURI-DEV.md).

## Pornire

```bash
# API — deschide singur Scalar in browser
dotnet watch --project AgentPlatform.Api        # http://localhost:5274/scalar/v1

# Client
cd AgentPlatform.Client && npm install          # doar prima data
npm run dev                                     # http://localhost:5173
```

`dotnet run` porneste API-ul dar NU deschide browserul.

## Verificari rapide

```bash
curl http://localhost:5274/api/test/ping        # conexiune DB
```

Login in client: http://localhost:5173/login cu contul din CONTURI-DEV.md.

## Token pentru API (expira in 1h)

```bash
# email, parola si apikey: vezi CONTURI-DEV.md (gitignored)
TOKEN=$(curl -s -X POST "https://ppguqihvromohocawsje.supabase.co/auth/v1/token?grant_type=password" \
  -H "apikey: <publishable-key>" \
  -H "Content-Type: application/json" \
  -d '{"email":"<email>","password":"<parola>"}' \
  | python3 -c "import json,sys; print(json.load(sys.stdin)['access_token'])")

curl http://localhost:5274/api/test/me -H "Authorization: Bearer $TOKEN"
```

In Scalar: butonul **Authorize**, pui `Bearer <token>`.

## Testat agentul AI fara WhatsApp

Cel mai rapid: pagina de simulare din client, sau direct:

```bash
curl -X POST http://localhost:5274/api/conversations/simulate \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"aiAgentId":"<guid-agent>","contactPhone":"+40722445019","message":"Caut apartament 2 camere in Cluj"}'
```

`aiAgentId` il iei din `GET /api/agents`. Agentul trebuie sa fie **activ**,
altfel nu raspunde.

## Sters o conversatie de test

Din client: deschizi conversatia, buton **Șterge** in bara de jos, apoi confirmi.
Sau direct:

```bash
curl -X DELETE http://localhost:5274/api/conversations/<guid-conversatie> \
  -H "Authorization: Bearer $TOKEN" -i          # 204 No Content
```

Se sterg si mesajele (cascade in DB). Leadul creat din conversatie **ramane**,
doar ca pierde legatura cu firul. Nu se poate anula.

## Cand primesti 405 la un endpoint nou

`dotnet watch` nu face hot reload la rute noi sau la schimbari de interfete —
serverul ramane pe codul vechi si raspunde 405 (metoda nu exista pe ruta).
Ctrl+C si pornesti din nou. Verifici ce stie serverul chiar acum:

```bash
curl -s http://localhost:5274/openapi/v1.json | grep -o '"/api/conversations[^"]*"'
```

## Testat pe WhatsApp real

```bash
ngrok http 5274
```

Pui URL-ul `https://xxxx.ngrok-free.app` in `PublicBaseUrl` din
`appsettings.Development.json` si ca webhook in Twilio Console:
`https://xxxx.ngrok-free.app/api/webhooks/twilio`. Pe planul gratuit URL-ul se
schimba la fiecare repornire ngrok — trebuie actualizat in ambele locuri.

Apoi scrii de pe telefon la `+1 415 523 8886`. Daca sandbox-ul a expirat (72h),
retrimiti `join <cod>`.

## Build / lint

```bash
dotnet build
cd AgentPlatform.Client && npm run build        # tsc + vite
npm run lint
```

## Migrari DB

```bash
supabase db push
```

Nu generam migrari EF — sursa de adevar e `supabase/migrations/`.
