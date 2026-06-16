import { useQuery } from "@tanstack/react-query";
import { getCurrentEntitlements } from "./billingApi";

type EntitlementSummaryProps = {
  token: string;
  onUnauthorized(): void;
};

export function EntitlementSummary({ token, onUnauthorized }: EntitlementSummaryProps) {
  const query = useQuery({
    queryKey: ["entitlements"],
    queryFn: () => getCurrentEntitlements(token, onUnauthorized),
  });

  if (query.isLoading) {
    return <div className="h-24 animate-pulse rounded-md border border-border bg-slate-100" />;
  }

  if (query.isError || !query.data) {
    return (
      <div className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-900">
        Entitlements unavailable.
      </div>
    );
  }

  const { entitlements } = query.data;

  return (
    <div className="grid gap-3 rounded-md border border-border bg-white p-4 text-sm sm:grid-cols-4">
      <Metric label="Plan" value={query.data.plan} />
      <Metric label="Case actions" value={entitlements.canCreateCases ? "Enabled" : "Locked"} />
      <Metric label="Analytics" value={entitlements.canViewAnalytics ? "Enabled" : "Locked"} />
      <Metric label="Seats" value={entitlements.maxSeats.toString()} />
    </div>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-xs font-medium uppercase text-slate-500">{label}</div>
      <div className="mt-1 font-semibold text-slate-950">{value}</div>
    </div>
  );
}
