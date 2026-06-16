# Webhooks

The webhook endpoint is `POST /webhooks/stripe`, and it's the only way subscription state
enters the app. The rationale is in
[ADR 0003](decisions/0003-idempotent-webhook-processing.md); this is the concrete behavior.

## Verification

The raw request body and the `Stripe-Signature` header are verified against
`STRIPE_WEBHOOK_SECRET` with Stripe's `EventUtility`. A missing or invalid signature returns
400, and nothing is recorded or processed.

## Events handled

```
checkout.session.completed
customer.subscription.created
customer.subscription.updated
customer.subscription.deleted
invoice.paid
invoice.payment_failed
```

Any other event type is recorded as seen and acknowledged with 200 without further work.

## Idempotency

Each event id is stored in the `StripeWebhookEvents` table (id, type, received-at,
processed-at, processing-error).

- An event whose row exists **and has a processed-at** is a duplicate: return 200, do nothing.
- An event whose row exists but isn't processed yet (a prior attempt failed) is reprocessed,
  not skipped.
- A new event is recorded, then processed.

## Reconciliation

For a relevant event, the handler extracts the subscription id from the payload, fetches the
current subscription from Stripe through the gateway, and overwrites the cached subscription
row (and the customer link) from that snapshot. It never trusts the event's own field values
or its position in the stream, so duplicate and out-of-order deliveries converge to the same
state. Status maps to entitlements as described in [entitlements.md](entitlements.md) —
`invoice.payment_failed` lands as `past_due`.

## Responses

- **400** — bad or missing signature
- **200** — handled successfully, or a duplicate of an already-processed event (so Stripe
  stops retrying)
- **5xx** — a genuine processing failure, so Stripe retries; the error is stored on the event
  row and cleared on the next successful attempt

## Tests

The webhook suite runs against recorded fixtures with the fake gateway: valid vs invalid
signature, a duplicate processed once, an unprocessed event reprocessed on retry, out-of-order
events converging via the gateway snapshot, and `invoice.payment_failed` driving past_due plus
the entitlement lockout. No live Stripe.
