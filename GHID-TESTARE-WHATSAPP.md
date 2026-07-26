# Ghid: testare WhatsApp local

Cum pui la treabă agentul AI pe WhatsApp, de pe telefonul tău, cu totul rulând
local. Twilio WhatsApp Sandbox e gratuit și nu cere verificare de business.

---

## Numerele din sistem

### Numărul sandbox Twilio — aici scrii de pe telefon

```
+1 415 523 8886
```

Comun tuturor clienților Twilio. De aceea `To` nu poate identifica tenantul, iar
mesajele merg la primul agent activ al contului configurat în
`Twilio:SandboxTenantId`. În producție fiecare agent are numărul lui.

Sandbox-ul expiră după **72 de ore** de inactivitate. Retrimiți `join <cod>`
(codul e în Twilio Console → Messaging → Try it out) și merge din nou.

### Telefonul tău

```
+40 766 318 375     Osadici Darius
```

Deja înscris în sandbox. Conversația ta apare în panou ca orice alt client.

### Agenții AI și numerele lor

| Număr | Agent | Stare |
|---|---|---|
| `+40721118204` | Maria — Apartamente Cluj | **activ** |
| `+40733902517` | Andrei — Case și terenuri | oprit |

În sandbox numerele astea nu se folosesc la rutare — doar în producție. Contează
însă **care agent e activ**: un agent oprit nu răspunde, iar dacă niciunul nu e
activ mesajul e ignorat (scrie în log).

Ca să testezi Andrei: pornește-l din **Agenți AI** și oprește Maria.

### Contacte de test deja în bază

Numere pe care poți simula clienți din panou. Cele cu istoric arată dacă agentul
ține minte contextul.

| Număr | Nume | Conversație | Mesaje |
|---|---|---|---|
| `+40745213897` | Andrei Mureșan | convertită | 6 |
| `+40751660342` | Vlad Cristea | convertită | 5 |
| `+40722445019` | Ioana Petrescu | activă | 4 |
| `+40730887125` | Mihai Dobre | activă | 4 |
| `+40727314508` | Raluca Ionescu | activă | 3 |
| `+40744129673` | George Tătaru | **închisă** | 4 |
| `+40768402991` | Elena Barbu | fără conversație | — |

Două utile pentru cazuri limită:

- **George Tătaru** are conversația `closed`. Un mesaj nou de la el pornește un
  fir nou, fiindcă firele închise nu se continuă.
- **Elena Barbu** are lead fără conversație — vezi cum arată drawerul fără istoric.

---

## Pornirea, pas cu pas

### 1. API-ul, primul

```bash
dotnet watch --project AgentPlatform.Api
```

Lasă terminalul deschis: aici vezi fiecare mesaj primit și eventualele erori.

### 2. Tunelul

```bash
ngrok http 5274
```

Copiază URL-ul `https://xxxx.ngrok-free.app`.

### 3. URL-ul în DOUĂ locuri

Ăsta e pasul care se uită. URL-ul trebuie identic în ambele.

**a) În `AgentPlatform.Api/appsettings.Development.json`:**

```json
"Twilio": {
  "AuthToken": "<Auth Token din Twilio Console → Account Info>",
  "PublicBaseUrl": "https://xxxx.ngrok-free.app",
  "SandboxTenantId": "02bab41f-fe48-48f1-8f41-bbcca8969e53"
}
```

**b) În Twilio Console** → Messaging → Try it out → Send a WhatsApp message →
Sandbox settings, la *When a message comes in*:

```
https://xxxx.ngrok-free.app/api/webhooks/twilio
```

Metoda **POST**. Salvezi.

### 4. Verifică tunelul înainte să scrii

```bash
curl https://xxxx.ngrok-free.app/api/test/ping
```

Trebuie `{"status":"ok",...}`. Dacă primești HTML, URL-ul nu mai e valid.

### 5. Scrie de pe telefon

Deschizi WhatsApp, conversația cu `+1 415 523 8886`:

> Bună! Caut un apartament cu 2 camere în Cluj, buget maxim 100.000 €.

Răspunsul vine în 2–3 secunde, în același chat. Continui normal — agentul ține
minte ce ai spus:

> Vreau obligatoriu cu parcare

> Ok, când putem vedea?

Conversația apare live în panou, la **Conversații**.

---

## Alternativa fără telefon

Panou → **Conversații** → butonul cu iconița de trimitere, lângă căutare.

Trece prin **exact același cod** ca webhookul — logica e extrasă în comun în
`ConversationService.ProcessInboundAsync`. Diferența e doar că nu implică Twilio.

Util când ngrok s-a repornit sau vrei să încerci rapid mai multe formulări.

---

## Ce merită testat

Cazurile care arată dacă promptul ține, nu doar că răspunde ceva:

| Ce scrii | Ce trebuie să facă |
|---|---|
| „Aveți vile cu piscină în Bănești la 30.000?" | să refuze — nu există în portofoliu — și să întrebe ce ai flexibiliza |
| „Puteți lăsa la 70.000 apartamentul din Gheorgheni?" | să nu coboare sub prețul listat |
| „Do you have anything in Cluj?" | să răspundă **în română**, agentul e configurat pe `ro` |
| trei mesaje despre buget și zonă | să nu repete întrebarea la care ai răspuns deja |
| trimite o poză, fără text | să nu răspundă nimic (e normal) |

Prima e cea mai importantă. Fără regula de grounding din prompt, un model
inventează o vilă plauzibilă cu preț plauzibil — iar clientul te întreabă apoi
pe tine despre ea.

---

## Când nu merge

Trei cauze, în ordinea în care apar:

| Simptom | Cauză | Fix |
|---|---|---|
| `ERR_NGROK_8012` | API-ul nu rulează | pornește `dotnet watch` |
| Twilio zice că a trimis, dar nu se întâmplă nimic | URL vechi în **Twilio Console** | actualizează *When a message comes in* |
| **401** la fiecare mesaj, în terminal | URL vechi în **`PublicBaseUrl`** | actualizează configul |
| **503** în terminal | `Twilio:AuthToken` gol | completează-l |
| Mesaj primit, dar fără răspuns | agent oprit, sau eroare Groq | vezi terminalul, scrie explicit care |

Al treilea păcălește cel mai des: tunelul merge, API-ul răspunde, dar semnătura
se verifică față de URL-ul din config. Dacă nu e identic cu cel pe care a semnat
Twilio, cererea e respinsă. Corect, dar arată ca o problemă de credențiale.

**Pe planul gratuit ngrok schimbă URL-ul la fiecare repornire.** Dacă te
încurcă, `cloudflared tunnel` (deja instalat) îți dă domeniu stabil, gratuit.

### Cum afli URL-ul ngrok curent

```bash
curl -s http://127.0.0.1:4040/api/tunnels \
  | python3 -c "import json,sys; print(json.load(sys.stdin)['tunnels'][0]['public_url'])"
```

---

## Ce nu poate face agentul

**Nu poate scrie primul.** Răspunde doar la mesajele tale. Trimiterea proactivă
(follow-up automat, „ți-am găsit ceva nou") cere apel către API-ul Twilio cu
Account SID și, pe WhatsApp, un template aprobat de Meta. În sandbox poți
răspunde doar în fereastra de 24 de ore după ultimul mesaj al clientului.

**Nu procesează poze sau audio.** Mesajele fără text sunt confirmate și ignorate.

---

## Credențiale

Toate stau în `AgentPlatform.Api/appsettings.Development.json`, care e gitignored.
Contul de test pentru panou e în `CONTURI-DEV.md`.
