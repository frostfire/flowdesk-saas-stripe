# Security

A lot of the security posture here is about what the app deliberately doesn't do.

## Payments

- **No card data touches the server.** Sign-up uses Stripe-hosted Checkout, so card entry
  happens on Stripe's domain and the app stays out of PCI scope. See
  [ADR 0001](decisions/0001-stripe-checkout-over-custom-card-form.md).
- **Webhooks are signature-verified.** Every event is checked against the signing secret with
  Stripe's `EventUtility`; an unsigned or forged event is rejected with a 400 before any
  processing. See [webhooks.md](webhooks.md).

## Authorization

- **Entitlements are enforced server-side** on every gated endpoint; the client gate is
  cosmetic. See [ADR 0004](decisions/0004-server-side-entitlement-enforcement.md).

## Auth

- ASP.NET Core Identity handles password hashing. The API uses JWT bearer tokens
  (HMAC-SHA256); the signing key comes from config and must be at least 32 bytes, or the app
  won't start. Issuer, audience, signing key, and lifetime are validated per request. See
  [ADR 0005](decisions/0005-auth-approach.md).
- The SPA keeps the token in memory, not `localStorage`.

## Secrets

- All secrets come from environment variables. `.env.sample` ships placeholder values only; a
  real `.env` is git-ignored. Stripe **test** keys only — the publishable key is the one
  Stripe value that belongs in the browser.
- CORS is restricted to the configured SPA origins.
- The CaseFlow client defaults to the in-memory fake, so the app reaches nothing external
  unless explicitly configured to. The test suites never call a live service.

## Known follow-ups before any real exposure

- No account lockout on login yet; add rate limiting on the auth and webhook endpoints, at the
  proxy and in-app.
- Data Protection uses ephemeral keys in the local container (it logs a development warning);
  persist them before hosting.
- Seeded demo data only — no real user PII.
