# 0003 — Idempotent webhook processing

## Status

Accepted

## Context

Stripe delivers webhooks at least once and in no guaranteed order. The same event can
arrive twice, events can arrive out of sequence, and Stripe retries anything that doesn't
return a quick 2xx. A billing integration that assumes exactly-once, in-order delivery will
eventually double-apply something or settle on stale state. This is the part of a Stripe
integration that's easiest to get subtly wrong.

## Decision

`POST /webhooks/stripe` handles each event defensively:

- **Verify first.** The raw body and `Stripe-Signature` header are checked against the
  webhook signing secret with Stripe's `EventUtility`. A bad or missing signature is a 400
  and nothing else happens.
- **Deduplicate on processed events.** Each event id is recorded in a `StripeWebhookEvents`
  table. If an event id is already present *and was processed successfully*, the endpoint
  returns 200 immediately without redoing the work.
- **Let failed events retry.** If a record exists but processing didn't finish (a previous
  attempt threw), the event is processed again instead of skipped. The last error message
  is stored and cleared once processing succeeds. This is safe because the work itself is
  idempotent: it re-fetches the current subscription from Stripe and overwrites the cached
  row, so running it twice converges to the same result (see
  [0002](0002-stripe-as-source-of-truth.md)).
- **Don't trust order.** Reconciliation always uses Stripe's current subscription object,
  never the contents of the event, so an out-of-order delivery can't move state backward.
- **Signal correctly.** Successful handling returns 200 so Stripe stops retrying; a genuine
  processing failure returns 5xx so Stripe retries later.

## Consequences

- Duplicate deliveries are a no-op; out-of-order deliveries converge to the right state.
- A transient failure (a database hiccup, a timed-out Stripe call) is recoverable: Stripe
  retries, and the retry actually reprocesses instead of being deduplicated away.
- The idempotency table grows by one row per distinct event. It doubles as an audit trail
  and is what the debug panel reads to show recent activity.
- Recording the event before processing means a row can exist before processing has
  succeeded — which is exactly why dedup keys on *processed*, not merely *seen*.
