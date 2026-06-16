import { useQuery } from "@tanstack/react-query";
import { listCases } from "./caseQueries";
import type { CaseSummary } from "./types";

type CaseListProps = {
  token: string;
  onUnauthorized(): void;
};

export function CaseList({ token, onUnauthorized }: CaseListProps) {
  const query = useQuery({
    queryKey: ["cases"],
    queryFn: () => listCases(token, onUnauthorized),
  });

  if (query.isLoading) {
    return <CaseListLoading />;
  }

  if (query.isError) {
    return <CaseListError onRetry={() => void query.refetch()} />;
  }

  if (!query.data?.length) {
    return <CaseListEmpty />;
  }

  return <CaseListData cases={query.data} />;
}

function CaseListLoading() {
  return (
    <div className="grid gap-3" aria-label="Loading cases">
      {["one", "two", "three"].map((item) => (
        <div key={item} className="h-20 animate-pulse rounded-md border border-border bg-slate-100" />
      ))}
    </div>
  );
}

function CaseListEmpty() {
  return (
    <div className="rounded-md border border-dashed border-border bg-white p-6">
      <h2 className="text-base font-semibold">No cases</h2>
      <p className="mt-2 text-sm text-slate-600">CaseFlow has no cases ready for this workspace.</p>
    </div>
  );
}

function CaseListError({ onRetry }: { onRetry(): void }) {
  return (
    <div className="rounded-md border border-red-200 bg-red-50 p-6">
      <h2 className="text-base font-semibold text-red-900">Cases unavailable</h2>
      <p className="mt-2 text-sm text-red-800">Refresh the list or sign in again.</p>
      <button
        type="button"
        className="mt-4 inline-flex h-9 items-center justify-center rounded-md border border-red-300 bg-white px-3 text-sm font-medium text-red-900"
        onClick={onRetry}
      >
        Retry
      </button>
    </div>
  );
}

function CaseListData({ cases }: { cases: CaseSummary[] }) {
  return (
    <div className="overflow-hidden rounded-md border border-border bg-white">
      <table className="w-full border-collapse text-left text-sm">
        <thead className="bg-slate-100 text-xs uppercase text-slate-600">
          <tr>
            <th className="px-4 py-3 font-semibold">Case</th>
            <th className="px-4 py-3 font-semibold">Customer</th>
            <th className="px-4 py-3 font-semibold">Status</th>
            <th className="px-4 py-3 font-semibold">Updated</th>
          </tr>
        </thead>
        <tbody>
          {cases.map((item) => (
            <tr key={item.id} className="border-t border-border">
              <td className="px-4 py-3">
                <div className="font-medium text-slate-950">{item.title}</div>
                <div className="mt-1 text-xs text-slate-500">{item.reference}</div>
              </td>
              <td className="px-4 py-3 text-slate-700">{item.customerName}</td>
              <td className="px-4 py-3">
                <span className="inline-flex rounded-md bg-slate-100 px-2 py-1 text-xs font-medium text-slate-700">
                  {item.status}
                </span>
              </td>
              <td className="px-4 py-3 text-slate-600">{formatDate(item.updatedAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));
}
