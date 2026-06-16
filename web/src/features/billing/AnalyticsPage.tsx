import { useQuery } from "@tanstack/react-query";
import { getAnalyticsSummary } from "./analyticsApi";

type AnalyticsPageProps = {
  token: string;
  onUnauthorized(): void;
};

export function AnalyticsPage({ token, onUnauthorized }: AnalyticsPageProps) {
  const query = useQuery({
    queryKey: ["analytics-summary"],
    queryFn: () => getAnalyticsSummary(token, onUnauthorized),
  });

  if (query.isLoading) {
    return <div className="h-32 animate-pulse rounded-md border border-border bg-slate-100" />;
  }

  if (query.isError || !query.data) {
    return (
      <section className="rounded-md border border-red-200 bg-red-50 p-6 text-red-900">
        <h1 className="text-xl font-semibold">Analytics unavailable</h1>
      </section>
    );
  }

  return (
    <section className="grid gap-6">
      <div>
        <p className="text-sm font-medium text-primary">Analytics</p>
        <h1 className="mt-2 text-3xl font-semibold">Summary</h1>
      </div>
      <div className="grid gap-4 sm:grid-cols-4">
        <Metric label="Total" value={query.data.totalCases} />
        <Metric label="Pending" value={query.data.pendingCases} />
        <Metric label="Approved" value={query.data.approvedCases} />
        <Metric label="Rejected" value={query.data.rejectedCases} />
      </div>
    </section>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-md border border-border bg-white p-5">
      <div className="text-sm font-medium text-slate-600">{label}</div>
      <div className="mt-2 text-3xl font-semibold">{value}</div>
    </div>
  );
}
