# 0001 — Stripe Checkout over a custom card form

## Status

Accepted

## Context

Subscription sign-up has to collect payment details. The options are building a card form
in the app (with Stripe Elements, or worse, raw card fields) or sending the user to
Stripe's hosted Checkout page.

Anything that touches raw card data pulls the application into PCI DSS scope — a serious,
ongoing compliance burden. For a subscription product, hosted Checkout gives up almost
nothing in exchange for staying out of that scope entirely.

## Decision

Use Stripe-hosted **Checkout** for subscription sign-up. The flow:

1. The user clicks Upgrade. `POST /billing/checkout-session` creates a Checkout session
   with `mode=subscription` and the selected plan's price id, and returns the session URL.
2. The browser redirects to Stripe. Card entry happens entirely on Stripe's domain.
3. Stripe redirects back to the app's success or cancel URL (both passed in the request).
4. The subscription becomes real in the app only when the matching webhook arrives and
   reconciles state — the success redirect is a UX signal, not the source of truth (see
   [0002](0002-stripe-as-source-of-truth.md)).

No card number, CVC, or expiry ever reaches the API.

## Consequences

- The application stays out of PCI scope: it never sees, stores, or transmits card data.
- Less control over the exact checkout UI, which is a fine trade for the compliance and
  security it removes.
- Sign-up depends on a redirect round-trip and the follow-up webhook, so the app treats
  "checkout started" and "subscription active" as distinct states rather than assuming
  success on return.
- The same reasoning is why billing management goes through the Stripe customer portal
  instead of custom screens.
