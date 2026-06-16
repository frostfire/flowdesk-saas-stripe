import { apiRequest } from "@/lib/api";
import type { AdminBillingDebug, BillingSession, CurrentEntitlements, PlanCode, ResetDemoResult } from "./types";

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
  return apiRequest<BillingSession>("/billing/checkout-session", {
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

export function createPortalSession(token: string, onUnauthorized: () => void) {
  return apiRequest<BillingSession>("/billing/portal-session", {
    method: "POST",
    token,
    onUnauthorized,
    body: JSON.stringify({
      returnUrl: `${window.location.origin}/pricing`,
    }),
  });
}

export function getAdminBillingDebug(token: string, onUnauthorized: () => void) {
  return apiRequest<AdminBillingDebug>("/admin/billing-debug", {
    token,
    onUnauthorized,
  });
}

export function resetDemo(token: string, onUnauthorized: () => void) {
  return apiRequest<ResetDemoResult>("/admin/reset-demo", {
    method: "POST",
    token,
    onUnauthorized,
  });
}
