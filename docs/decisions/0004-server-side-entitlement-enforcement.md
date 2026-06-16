# 0004 — Entitlements are enforced on the server

## Status

Accepted

## Context

Plan tiers gate features: Free can read cases, paid tiers can create and approve them and
see analytics. The frontend already knows the user's entitlements, so it can show, hide, or
disable controls. But anything the client enforces, a user can bypass — open devtools,
un-hide the button, or just call the API directly.

## Decision

Every gated action is authorized on the server, against the entitlement set computed from
the user's cached subscription status. The client gate exists only for UX.

- Creating, approving, and rejecting cases require `can_create_cases`; without it the
  endpoint returns 403.
- The analytics endpoint requires `can_view_analytics`; without it, 403.
- Entitlements are derived server-side from the cached subscription (see
  [0002](0002-stripe-as-source-of-truth.md)) through the single `PlanEntitlements` mapping,
  so client and server always reason from the same definition.

The frontend reads `GET /entitlements/me` to hide or disable gated controls, but that's a
convenience layer over the real check.

## Consequences

- Un-hiding a control in the browser changes nothing: the server still returns 403.
- A `past_due` account drops to Free entitlements, so a lapsed subscriber is locked out of
  paid actions at the API, not just in the UI.
- Gating logic lives in one place. Adding a gated feature means adding an entitlement and
  one server check, not scattering plan comparisons through the code.
- This is covered by tests that call gated endpoints as Free and as Pro users and assert
  403 versus 200, including the past_due lockout path.
