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
- The API validates issuer, audience, signing key, and lifetime on every request.
- The SPA keeps the access token in memory and sends it as a bearer token. When it expires,
  the user signs in again — there's no refresh-token flow, which keeps the demo simple and
  avoids storing long-lived credentials in the browser. A 401 from the API redirects to
  sign-in.

## Consequences

- Password handling is delegated to a well-tested framework instead of improvised.
- Stateless bearer auth keeps the API simple and works cleanly with the separate frontend.
- Holding the token in memory rather than `localStorage` means a full page reload requires
  signing in again. That's an acceptable trade and sidesteps a common XSS exposure.
- There's no account lockout on the login path yet; brute-force protection is left to rate
  limiting at the proxy and is a hardening item before any real exposure.
