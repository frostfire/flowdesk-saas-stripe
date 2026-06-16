import { apiRequest } from "@/lib/api";
import type { CaseDetail, CaseMutationValues, CaseSummary } from "./types";

export function listCases(token: string, onUnauthorized: () => void) {
  return apiRequest<CaseSummary[]>("/cases", {
    token,
    onUnauthorized,
  });
}

export function getCase(token: string, id: string, onUnauthorized: () => void) {
  return apiRequest<CaseDetail>(`/cases/${encodeURIComponent(id)}`, {
    token,
    onUnauthorized,
  });
}

export function createCase(token: string, values: CaseMutationValues, onUnauthorized: () => void) {
  return apiRequest<CaseDetail>("/cases", {
    method: "POST",
    token,
    onUnauthorized,
    body: JSON.stringify(values),
  });
}

export function updateCase(token: string, id: string, values: CaseMutationValues, onUnauthorized: () => void) {
  return apiRequest<CaseDetail>(`/cases/${encodeURIComponent(id)}`, {
    method: "PUT",
    token,
    onUnauthorized,
    body: JSON.stringify(values),
  });
}

export function deleteCase(token: string, id: string, onUnauthorized: () => void) {
  return apiRequest<void>(`/cases/${encodeURIComponent(id)}`, {
    method: "DELETE",
    token,
    onUnauthorized,
  });
}

export function approveCase(token: string, id: string, onUnauthorized: () => void) {
  return apiRequest<CaseDetail>(`/cases/${encodeURIComponent(id)}/approve`, {
    method: "POST",
    token,
    onUnauthorized,
  });
}

export function rejectCase(token: string, id: string, onUnauthorized: () => void) {
  return apiRequest<CaseDetail>(`/cases/${encodeURIComponent(id)}/reject`, {
    method: "POST",
    token,
    onUnauthorized,
  });
}
