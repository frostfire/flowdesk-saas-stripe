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

export type CheckoutSession = {
  url: string;
};
