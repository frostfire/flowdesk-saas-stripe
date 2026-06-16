# Demo hosting

This repo contains the application and a local `docker-compose.yml`. It deliberately carries
no deploy manifests — hosting is wired up separately. What follows is what a host needs to
provide and how to verify a run.

## Configuration

Everything is environment-driven; `.env.sample` lists every key. Placeholder values are fine
for a local run, but a host must supply real **runtime** values for:

- `ConnectionStrings__DefaultConnection` — the Postgres instance
- `Jwt__SigningKey` — a real signing key, at least 32 bytes
- `STRIPE_SECRET_KEY`, `STRIPE_PUBLISHABLE_KEY`, `STRIPE_WEBHOOK_SECRET` — Stripe **test** mode
- `STRIPE_PRICE_PRO`, `STRIPE_PRICE_TEAM` — the price ids from the Stripe dashboard
- `Cors__AllowedOrigins__0` — the deployed SPA origin

The API applies its EF Core migrations on startup (`Database__ApplyMigrationsOnStartup`), so a
fresh database is set up on first run.

## CaseFlow

`CaseFlow__ClientMode` defaults to `Fake`, which serves seeded data and reaches nothing
external. To point the demo at a live CaseFlow instance, set it to `Http` and set
`CaseFlow__BaseUrl`.

## Stripe

Test mode only. The publishable key is the one Stripe value that belongs in the browser; the
secret key and webhook secret stay server-side. Point a Stripe webhook endpoint (or
`stripe listen`) at `/webhooks/stripe` and set `STRIPE_WEBHOOK_SECRET` to its signing secret.

## Operational checks

- `GET /health` returns 200 when the API is up.
- Sign up, sign in, and load the dashboard (the seeded case list).
- The auth-protected `/admin/billing-debug` shows the current synced subscription and recent
  webhook events — useful for confirming the Stripe sync is working.
- `/admin/reset-demo` cancels demo subscriptions and reseeds demo accounts; a host can call it
  on a schedule to keep a public demo clean (the scheduling itself lives outside this repo).

## Tests

`dotnet test` and, in `web/`, `npm test` both run offline.
