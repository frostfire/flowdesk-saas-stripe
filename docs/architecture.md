# Architecture

FlowDesk is a .NET 10 backend and a React frontend that talk over a JSON API. The backend
follows the same clean-architecture split as CaseFlow: dependencies point inward, and the
outer layers are swappable.

## Backend layers

```
FlowDesk.Domain          plans, entitlements, subscription status — pure types, no dependencies
FlowDesk.Application      use cases and the interfaces the outside world implements
FlowDesk.Infrastructure   EF Core, the Stripe gateway, CaseFlow clients, the webhook service
FlowDesk.Contracts        request/response DTOs shared with the frontend
FlowDesk.Api             the host: endpoints, auth, DI wiring, the Stripe webhook
```

- **Domain** holds the billing model — `PlanCode`, `EntitlementSet`, the `PlanEntitlements`
  map, and `SubscriptionStatus`. It's pure and unit-testable, with no EF, Stripe, or ASP.NET
  references. The plan-to-entitlement mapping and the status-to-entitlement computation live
  here, so there's one definition of what a plan can do.
- **Application** defines the seams the rest of the system plugs into: `IBillingGateway`
  (Stripe), `ICaseFlowClient` (the product data), `IStripeWebhookService`, and the
  entitlement and diagnostics services. It depends only on Domain.
- **Infrastructure** implements those seams: EF Core against Postgres, `StripeBillingGateway`
  over Stripe.NET, the fake and HTTP CaseFlow clients, and `StripeWebhookService`. ASP.NET
  Core Identity's user store lives here too.
- **Contracts** is just DTOs, kept separate so wire shapes never couple to domain internals.
- **Api** is the composition root. Endpoints are grouped by area (auth, cases, billing,
  entitlements, analytics, admin, webhooks) as minimal-API endpoint groups, each thin: map a
  request to a call into Application/Infrastructure and back to a contract.

## Frontend

The React app under `web/` is organized by feature:

```
web/src/
  app/         routes, layout, the auth guard
  features/
    auth/      sign-up, sign-in, session
    billing/   pricing page, checkout/portal launch, dunning banner, gated controls
    cases/     the gated product (the CaseFlow list)
  components/   shared UI (shadcn primitives)
  lib/         the API client and TanStack Query setup
```

Server state goes through TanStack Query; the API client attaches the bearer token and
redirects to sign-in on a 401.

## Tests

`tests/` holds the xUnit projects. `FlowDesk.Api.Tests` drives the API through
`WebApplicationFactory` with the fake billing gateway and fake CaseFlow client wired in;
`FlowDesk.Webhook.Tests` covers webhook payload and fixture handling. Frontend tests live
beside the code under `web/` (Vitest + Testing Library). None of the suites touch a live
service.

## Local topology

`docker compose up` runs two services: Postgres 16 and the API (built from `Dockerfile`),
with the API applying EF Core migrations on startup and exposing `/health`. The frontend runs
separately under Vite during development. See [demo-hosting.md](demo-hosting.md) for
configuration.
