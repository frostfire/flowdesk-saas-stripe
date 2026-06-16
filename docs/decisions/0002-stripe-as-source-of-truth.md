# 0002 — Stripe is the source of truth for subscription state

## Status

Accepted

## Context

The app needs each user's plan and subscription status to decide what they can do. That
state changes outside the app constantly: a card expires, a retried payment succeeds, a
user downgrades in the portal, dunning gives up and cancels. If the app kept its own
authoritative copy of subscription status, it would drift from reality the moment any of
that happened.

## Decision

Stripe owns subscription state. The local database holds only a **cache**: the Stripe
customer id, subscription id, price id, status, current-period-end, and the derived plan.
Webhooks keep that cache in sync, and the app reads from it for fast, request-time
authorization.

The cache is never updated straight from event payloads. When a relevant event arrives,
the webhook handler fetches the current subscription object from Stripe and reconciles the
cached row to that snapshot (see [0003](0003-idempotent-webhook-processing.md)). The local
row is a projection of Stripe, not an independent record.

Entitlements are computed from the cached status: `active` or `trialing` maps the plan to
its entitlements; anything else (`past_due`, `canceled`, `unpaid`, `incomplete`, and so on)
falls back to Free.

## Consequences

- The app can't disagree with Stripe about who is paying. Whatever Stripe last reported is
  what the app enforces.
- A failed payment that Stripe marks `past_due` immediately drops the account to Free
  entitlements; the lockout is just the normal status-to-entitlement mapping, not a special
  case.
- The cache can briefly lag reality between an event happening and its webhook being
  processed. That window is small and self-correcting, and reads are authorized against the
  cache — the right trade for not calling Stripe on every request.
- Because reconciliation re-fetches the live object, event delivery order doesn't matter.
