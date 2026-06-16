import { apiRequest } from "@/lib/api";

export type AnalyticsSummary = {
  totalCases: number;
  pendingCases: number;
  approvedCases: number;
  rejectedCases: number;
};

export function getAnalyticsSummary(token: string, onUnauthorized: () => void) {
  return apiRequest<AnalyticsSummary>("/analytics/summary", {
    token,
    onUnauthorized,
  });
}
