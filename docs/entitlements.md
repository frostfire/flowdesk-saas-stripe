# Entitlements

Entitlements are the bridge between "what plan is this user on" and "what can they do." They
exist as a first-class set rather than as plan checks scattered through the code.

## The set

```
can_create_cases    bool
can_view_analytics  bool
max_cases           int
max_seats           int
```

Plans map to this set in `FlowDesk.Domain.Billing.PlanEntitlements`; see
[billing-model.md](billing-model.md) for the per-plan values.

## Computing current entitlements

The current set is derived from the user's cached subscription status:

- no subscription → Free
- `active` / `trialing` → the plan's set
- everything else, including `past_due` → Free

This is pure domain logic, so it's unit-tested directly.

## Server enforcement

The server is the real gate. Gated endpoints check the current set before doing anything:

- `POST /cases`, `POST /cases/{id}/approve`, `POST /cases/{id}/reject` require
  `can_create_cases`, else 403
- the analytics endpoint requires `can_view_analytics`, else 403

A Free user — or a `past_due` one — calling these directly gets a 403 regardless of what the
UI shows. See [ADR 0004](decisions/0004-server-side-entitlement-enforcement.md).

## Client behavior

The frontend reads `GET /entitlements/me` and uses it to:

- show or disable the create/approve controls
- show or hide the analytics view
- mark the current plan on the pricing page
- show the dunning banner and lock paid features when the subscription is `past_due`

This is presentation only; removing it client-side doesn't grant access.

## Test matrix

Coverage spans the mapping (each plan → expected set), the status computation (active vs
past_due vs none), and the server gates (Free 403 / Pro 200, plus the past_due lockout driven
through the webhook path).
