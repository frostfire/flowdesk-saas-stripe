export type PlanCode = "Free" | "Pro" | "Team";

export type EntitlementSet = {
  canCreateCases: boolean;
  canViewAnalytics: boolean;
  maxCases: number;
  maxSeats: number;
};

export type CurrentEntitlements = {
  plan: PlanCode;
  status: string;
  entitlements: EntitlementSet;
};

export type BillingSession = {
  url: string;
};

export type AdminSubscriptionState = {
  plan: PlanCode;
  status: string;
  stripeCustomerId: string | null;
  stripeSubscriptionId: string | null;
  stripePriceId: string | null;
  currentPeriodEnd: string | null;
  cancelAtPeriodEnd: boolean;
  updatedAt: string | null;
};

export type AdminStripeWebhookEvent = {
  stripeEventId: string;
  type: string;
  receivedAt: string;
  processedAt: string | null;
  processingError: string | null;
};

export type AdminBillingDebug = {
  subscription: AdminSubscriptionState;
  webhookEvents: AdminStripeWebhookEvent[];
};

export type ResetDemoResult = {
  canceledSubscriptions: number;
  resetSubscriptions: number;
  deletedBillingCustomers: number;
  deletedWebhookEvents: number;
  demoUserEmail: string;
};
