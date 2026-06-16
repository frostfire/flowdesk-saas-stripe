# Frontend architecture

The frontend is a Vite + React + TypeScript app under `web/`, organized by feature with
server state handled by TanStack Query.

## Routing and auth

React Router defines the routes, and an auth guard wraps the protected ones. The access token
is held in memory in an auth context (not `localStorage`), and the shared API client attaches
it as a bearer token. A 401 from the API clears the session and redirects to sign-in. On route
change, focus moves to the main landmark so keyboard and screen-reader users follow the
navigation.

## Data layer

TanStack Query owns server state — caching, refetching, and invalidation. After an action that
changes entitlements or cases, the relevant queries are invalidated so the UI reflects the new
state instead of stale data. Components don't fetch ad hoc in effects.

## Forms

Forms use react-hook-form with zod schemas, so the same shapes validate input and render
field-level errors.

## Data views

Every list or data view handles four states explicitly: loading (a skeleton), empty, error
(with retry), and data. The case list is the reference implementation.

## Billing UI

The upgrade flow is modeled as an explicit state machine (idle → redirecting →
success/cancelled → syncing) rather than a tangle of boolean flags, because a checkout
round-trip genuinely has those stages. The pricing page marks the current plan and starts
checkout by requesting a session URL and redirecting. When the subscription is `past_due`, a
dunning banner appears and the gated controls lock, driven by `GET /entitlements/me`.

## Accessibility

Controls are labelled, the upgrade buttons carry plan-specific accessible names, and focus is
managed on navigation. (There are no dialog components yet; when they're added they'll need
focus trapping.)

## Tests

Component tests use Vitest + Testing Library with the API mocked: the pricing page renders the
plans, upgrade initiates checkout, a gated feature is hidden on Free and visible on Pro, and
the dunning banner appears when the subscription is past_due.
