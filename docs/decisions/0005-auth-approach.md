# 0005 — Authentication approach

## Status

Accepted

## Context

FlowDesk needs accounts: sign-up, sign-in, and protected endpoints. The realistic options
on .NET are ASP.NET Core Identity, a hand-rolled user store, or an external identity
provider. An external IdP is overkill here, and rolling password storage by hand is a
mistake nobody should make.

## Decision

Use **ASP.NET Core Identity** for the user store and password hashing, and **JWT bearer**
tokens for API authentication.

- Register and login go through Identity, which handles hashing and verification.
- On success the API issues a JWT signed with HMAC-SHA256. The signing key comes from
  configuration and must be at least 32 bytes; the app refuses to start otherwise. Tokens
  carry the user id and email and expire after a configurable lifetime (two hours by
  default).
- The API validates issuer, audience, signing key, and lifetime on every request. Every
  token carries a `token_type` claim (`access` or `refresh`), and the bearer pipeline rejects
  anything that isn't an `access` token — so a refresh token can never be replayed as an API
  credential.
- The SPA keeps the **access** token in memory and sends it as a bearer token. It is never
  written to `localStorage` or `sessionStorage`, which keeps it out of reach of XSS.

### Staying signed in ("Remember me")

Holding the access token only in memory means a page reload — including the full-page
round-trip out to Stripe Checkout and back — loses it. To let users stay signed in across
reloads without parking a long-lived credential in JavaScript-reachable storage, sign-up and
sign-in accept an optional **Remember me** flag:

- When set, the API issues a second, longer-lived **refresh** JWT (7 days, `token_type=refresh`)
  and returns it in an **HttpOnly, Secure, `SameSite=Lax` cookie scoped to `Path=/auth`**.
  Because it's HttpOnly it is invisible to scripts; because it's path-scoped it rides only on
  the `/auth/*` calls that need it, not on ordinary API traffic.
- On load the SPA calls `POST /auth/refresh`; if the cookie is present and valid the API mints
  a fresh in-memory access token and the session is restored silently. This is also what
  smooths the post-Checkout return — the redirect back from Stripe lands authenticated instead
  of bouncing to the sign-in page. `SameSite=Lax` is deliberate: it permits the cookie on that
  top-level GET navigation while still blocking it on cross-site subrequests.
- `POST /auth/logout` clears the cookie.

The CORS policy allows credentials and is pinned to explicit origins (never `*`), which is
required for the cookie to flow.

## Consequences

- Password handling is delegated to a well-tested framework instead of improvised.
- Stateless bearer auth keeps the API simple and works cleanly with the separate frontend.
- The access token lives only in memory, so it's never exposed to `localStorage`/`sessionStorage`
  XSS. The only persisted credential is the HttpOnly refresh cookie, which scripts can't read.
- The refresh token is a **stateless** JWT — there is no server-side session/revocation store.
  Logout deletes the cookie from the browser, but a refresh token that was captured before
  logout stays valid until it expires (max 7 days), and refresh does not rotate the token. For
  a hosted demo that's an accepted trade; a production build would back refresh tokens with a
  revocable server-side store (and rotate on use).
- The refresh cookie is always `Secure`, so the Remember-me path only works over HTTPS — fine
  for the deployed site (TLS-terminated at the proxy), but it won't set over plain-HTTP local
  dev.
- There's no account lockout on the login path yet; brute-force protection is left to rate
  limiting at the proxy and is a hardening item before any real exposure.
