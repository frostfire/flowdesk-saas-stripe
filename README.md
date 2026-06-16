# FlowDesk

FlowDesk is a small full-stack SaaS: sign up, pick a plan, and get a dashboard whose
features are gated by your subscription tier. The product behind the paywall is a
lightweight client for the CaseFlow API — the Free tier sees a read-only case list, and
paid tiers unlock creating and approving cases plus analytics.

Billing runs on Stripe. Sign-up uses hosted Stripe Checkout, subscription changes go
through the Stripe customer portal, and the app keeps its own state in sync from
signature-verified webhooks. Stripe is the source of truth: FlowDesk never decides locally
that someone is "Pro," it reflects what Stripe reports.

- **Frontend:** React 19, TypeScript, Vite, Tailwind, shadcn/ui, TanStack Query, React Router, react-hook-form + zod
- **Backend:** .NET 10, ASP.NET Core, clean-architecture layers
- **Data:** PostgreSQL 16, EF Core
- **Payments:** Stripe Checkout, the Stripe customer portal, Stripe.NET, and the Stripe CLI for local webhooks

## Running it locally

You need .NET 10, Node 20+, and Docker.

Copy the sample config and start Postgres plus the API:

```bash
cp .env.sample .env
docker compose up --build
```

That brings up Postgres on `5432` and the API on `http://localhost:8080`, and the API
applies its EF Core migrations on startup. Confirm it with `http://localhost:8080/health`.

In a second terminal, run the frontend:

```bash
cd web
npm install
npm run dev
```

The app is at `http://localhost:5173` and calls the API at `VITE_API_BASE_URL`.

## Configuration

Everything is configured through environment variables; `.env.sample` lists every key with
safe local defaults. The ones worth knowing:

| Key | Purpose |
|-----|---------|
| `ConnectionStrings__DefaultConnection` | Postgres connection string |
| `Jwt__SigningKey` | HMAC signing key for access tokens (must be at least 32 bytes) |
| `Jwt__AccessTokenMinutes` | Access-token lifetime (default 120) |
| `CaseFlow__ClientMode` | `Fake` (default) or `Http` |
| `CaseFlow__BaseUrl` | CaseFlow API base URL when `ClientMode=Http` |
| `STRIPE_SECRET_KEY` / `STRIPE_PUBLISHABLE_KEY` | Stripe API keys (test mode) |
| `STRIPE_WEBHOOK_SECRET` | Signing secret used to verify webhooks |
| `STRIPE_PRICE_PRO` / `STRIPE_PRICE_TEAM` | Stripe price IDs for the paid plans |

Use Stripe **test** keys only. Real keys don't belong in this repo or anywhere near a
public demo.

## How it fits together

```
src/
  FlowDesk.Domain          plans, entitlements, subscription status (pure, no dependencies)
  FlowDesk.Application      use cases and gateway interfaces (IBillingGateway, ICaseFlowClient)
  FlowDesk.Infrastructure   EF Core, Stripe gateway, CaseFlow clients, webhook service
  FlowDesk.Contracts        request/response DTOs
  FlowDesk.Api             endpoints, auth, the Stripe webhook
tests/                     xUnit + WebApplicationFactory, webhook fixtures
web/                       the React app
docs/                      architecture, billing, webhooks, security, ADRs
```

### Auth
Sign-up and sign-in use ASP.NET Core Identity for the user store and password hashing.
The API issues a short-lived JWT (HMAC-SHA256, two hours by default); the SPA holds it in
memory and signs in again when it expires. There's no refresh-token flow — it isn't needed
here. A 401 from the API sends the user back to sign-in. See
[ADR 0005](docs/decisions/0005-auth-approach.md).

### Plans and entitlements
Three plans map to a set of entitlements (`can_create_cases`, `can_view_analytics`,
`max_cases`, `max_seats`) in one place, `FlowDesk.Domain.Billing.PlanEntitlements`:

| Plan | Price (test) | What you get |
|------|-------------:|--------------|
| Free | $0 | read-only case list |
| Pro | $19/mo | create and approve cases, analytics |
| Team | $49/mo | everything in Pro, plus more seats and higher limits |

Every gated action is authorized on the server against the current entitlement set; the
client reads the same entitlements only to show, hide, or disable controls. Hiding a button
is convenience; the server is the gate. See
[ADR 0004](docs/decisions/0004-server-side-entitlement-enforcement.md).

### Billing
"Upgrade" creates a Stripe Checkout session (`mode=subscription`) and redirects to Stripe's
hosted page, so no card data ever reaches the API
([ADR 0001](docs/decisions/0001-stripe-checkout-over-custom-card-form.md)). "Manage billing"
opens the Stripe customer portal for upgrades, downgrades, cancellation, and payment-method
changes — the app doesn't rebuild any of that.

### Webhooks
Subscription state comes back through `POST /webhooks/stripe`. Each event is
signature-verified, deduplicated by event id, and — rather than trusting event order — the
handler re-fetches the current subscription from Stripe and reconciles local state to it. A
failed payment flips the subscription to `past_due`, which drops the account to Free
entitlements and shows a dunning banner until it's resolved. See
[ADR 0002](docs/decisions/0002-stripe-as-source-of-truth.md) and
[ADR 0003](docs/decisions/0003-idempotent-webhook-processing.md).

For local webhook development, forward events with the Stripe CLI:

```bash
stripe listen --forward-to localhost:8080/webhooks/stripe
stripe trigger checkout.session.completed
```

Set `STRIPE_WEBHOOK_SECRET` to the signing secret that `stripe listen` prints.

## Testing

```bash
dotnet test            # backend: xUnit + WebApplicationFactory + webhook fixtures
cd web && npm test     # frontend: Vitest + Testing Library
```

The tests run entirely offline: a fake billing gateway and recorded webhook fixtures stand
in for Stripe, and a fake CaseFlow client serves seeded data, so no keys or live services
are required.

## Docs

- [architecture.md](docs/architecture.md) — how the pieces fit
- [billing-model.md](docs/billing-model.md) — plans, entitlements, the source-of-truth rule
- [stripe-integration.md](docs/stripe-integration.md) — checkout, portal, and webhook lifecycle
- [webhooks.md](docs/webhooks.md) — signature, idempotency, ordering
- [entitlements.md](docs/entitlements.md) — how gating works on client and server
- [frontend-architecture.md](docs/frontend-architecture.md) — data layer, forms, UI states
- [security.md](docs/security.md) — PCI scope, secrets, auth
- [docs/decisions](docs/decisions) — architecture decision records
