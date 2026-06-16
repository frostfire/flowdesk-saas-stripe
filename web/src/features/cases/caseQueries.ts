import { apiRequest } from "@/lib/api";
import type { CaseSummary } from "./types";

export function listCases(token: string, onUnauthorized: () => void) {
  return apiRequest<CaseSummary[]>("/cases", {
    token,
    onUnauthorized,
  });
}
