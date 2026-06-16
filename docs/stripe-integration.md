# Stripe integration

All Stripe access goes through `IBillingGateway`. `StripeBillingGateway` implements it with
Stripe.NET; tests use a fake gateway, so nothing in the build or test path needs a live key.

## Checkout (sign-up / upgrade)

1. `POST /billing/checkout-session` takes the target plan and success/cancel URLs.
2. The gateway creates a Checkout session with `mode=subscription` and the plan's configured
   price id (`STRIPE_PRICE_PRO` / `STRIPE_PRICE_TEAM`), and returns the session URL.
3. The browser redirects to Stripe; card entry stays on Stripe's domain
   ([ADR 0001](decisions/0001-stripe-checkout-over-custom-card-form.md)).
4. Stripe redirects back to the success or cancel URL. The subscription becomes real in the
   app only once the webhook arrives and reconciles state — the redirect is a UX signal.

## Customer portal (manage / downgrade / cancel)

`POST /billing/portal-session` creates a Stripe Billing Portal session and returns its URL;
"Manage billing" redirects there. The portal needs an existing Stripe customer, so the
endpoint returns 409 if the user has no reconciled customer id yet (i.e. they haven't been
through checkout). Upgrades, proration on downgrade, payment-method updates, and cancellation
all happen in the portal, and the changes flow back as webhooks.

## Source of truth and reconciliation

The local database caches subscription state but never treats it as authoritative. When a
relevant webhook arrives, the handler fetches the current subscription from Stripe and
reconciles the cached row to that snapshot:

```
customer id, subscription id, price id, plan, status, current-period-end, cancel-at-period-end
```

Because reconciliation reads the live object, out-of-order events can't move state backward.
See [ADR 0002](decisions/0002-stripe-as-source-of-truth.md) and [webhooks.md](webhooks.md).

## Payment failure

`invoice.payment_failed` reconciles the subscription to `past_due`, which drops the account to
Free entitlements and triggers the in-app dunning banner. When payment recovers, the
follow-up events reconcile it back to `active`.

## Local development

Forward events to the running API with the Stripe CLI:

```bash
stripe listen --forward-to localhost:8080/webhooks/stripe
stripe trigger checkout.session.completed
stripe trigger invoice.payment_failed
```

Set `STRIPE_WEBHOOK_SECRET` to the secret `stripe listen` prints. The webhook tests don't need
any of this — they replay recorded fixtures against the handler with the fake gateway.
