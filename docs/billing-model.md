# Billing model

FlowDesk has three plans. Prices are Stripe test-mode amounts.

| Plan | Price | Entitlements |
|------|------:|--------------|
| Free | $0 | read-only case list (`max_cases` 10, 1 seat) |
| Pro | $19/mo | create and approve cases, analytics (`max_cases` 250, 1 seat) |
| Team | $49/mo | everything in Pro, `max_cases` 1000, 10 seats |

## Entitlements

A plan resolves to an `EntitlementSet`:

- `can_create_cases` — create, approve, and reject cases
- `can_view_analytics` — the analytics summary
- `max_cases` — case ceiling
- `max_seats` — seat ceiling

The mapping lives in one place, `FlowDesk.Domain.Billing.PlanEntitlements`. Free grants
neither capability; Pro and Team grant both and differ on limits.

## Subscription state and entitlement computation

The app caches each user's subscription — Stripe customer id, subscription id, price id,
status, current-period-end, and the derived plan — and computes entitlements from it at
request time:

- no subscription row → **Free**
- status `active` or `trialing` → the plan's entitlements
- any other status (`past_due`, `canceled`, `incomplete`, `incomplete_expired`, `unpaid`,
  `paused`) → **Free**

A lapsed or unpaid subscription is simply Free until Stripe says otherwise. There's no
separate grace period — `past_due` locks paid features immediately, which is the behavior the
failed-payment path is meant to show. Stripe is the source of truth for the cached status; see
[stripe-integration.md](stripe-integration.md).

## Where it's enforced

Entitlements are authorized on the server on every gated endpoint (case
create/approve/reject need `can_create_cases`; analytics needs `can_view_analytics`). The
frontend reads `GET /entitlements/me` and uses the same set to show, hide, or disable
controls — UX only, not the gate. See [entitlements.md](entitlements.md) and
[ADR 0004](decisions/0004-server-side-entitlement-enforcement.md).

## Plan changes

Upgrades happen through Stripe Checkout; downgrades, cancellation, and payment-method changes
happen in the Stripe customer portal. Either way, the resulting change comes back as a webhook
that reconciles the cached state — the app never changes a user's plan on its own.

## Tests

The plan-to-entitlement mapping is unit-tested per plan, and the gated endpoints are tested as
Free (403) and Pro (200), including the `past_due` lockout.
