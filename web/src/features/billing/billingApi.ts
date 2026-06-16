import { apiRequest } from "@/lib/api";
import type { CheckoutSession, CurrentEntitlements, PlanCode } from "./types";

export function getCurrentEntitlements(token: string, onUnauthorized: () => void) {
  return apiRequest<CurrentEntitlements>("/entitlements/me", {
    token,
    onUnauthorized,
  });
}

export function createCheckoutSession(
  token: string,
  plan: Exclude<PlanCode, "Free">,
  onUnauthorized: () => void,
) {
  return apiRequest<CheckoutSession>("/billing/checkout-session", {
    method: "POST",
    token,
    onUnauthorized,
    body: JSON.stringify({
      plan,
      successUrl: `${window.location.origin}/billing/success`,
      cancelUrl: `${window.location.origin}/billing/cancel`,
    }),
  });
}
