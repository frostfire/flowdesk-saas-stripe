import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { getAdminBillingDebug, resetDemo } from "./billingApi";

type AdminDebugPageProps = {
  token: string;
  onUnauthorized(): void;
};

export function AdminDebugPage({ token, onUnauthorized }: AdminDebugPageProps) {
  const queryClient = useQueryClient();
  const debug = useQuery({
    queryKey: ["admin-billing-debug"],
    queryFn: () => getAdminBillingDebug(token, onUnauthorized),
  });
  const reset = useMutation({
    mutationFn: () => resetDemo(token, onUnauthorized),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-billing-debug"] });
      queryClient.invalidateQueries({ queryKey: ["entitlements"] });
    },
  });

  if (debug.isLoading) {
    return <div className="rounded-md border border-border bg-white p-4 text-sm text-slate-600">Loading billing state.</div>;
  }

  if (debug.isError) {
    return (
      <div className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-900">
        Billing debug state is unavailable.
      </div>
    );
  }

  if (!debug.data) {
    return null;
  }

  const data = debug.data;
  const subscription = data.subscription;

  return (
    <section className="grid gap-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium text-primary">Admin</p>
          <h1 className="mt-2 text-3xl font-semibold">Billing debug</h1>
          <p className="mt-3 max-w-2xl text-slate-600">TODO(claude): hosted-demo debug panel note.</p>
        </div>
        <Button variant="outline" disabled={reset.isPending} onClick={() => reset.mutate()}>
          {reset.isPending ? "Resetting" : "Reset demo"}
        </Button>
      </div>

      {reset.isSuccess ? (
        <div className="rounded-md border border-green-200 bg-green-50 p-4 text-sm text-green-950">
          Reset complete for {reset.data.demoUserEmail}.
        </div>
      ) : null}
      {reset.isError ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-900">Reset failed.</div>
      ) : null}

      <section className="rounded-md border border-border bg-white p-5">
        <h2 className="text-xl font-semibold">Synced subscription</h2>
        <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2">
          <DebugValue label="Plan" value={subscription.plan} />
          <DebugValue label="Status" value={subscription.status} />
          <DebugValue label="Stripe customer" value={subscription.stripeCustomerId ?? "None"} />
          <DebugValue label="Stripe subscription" value={subscription.stripeSubscriptionId ?? "None"} />
          <DebugValue label="Stripe price" value={subscription.stripePriceId ?? "None"} />
          <DebugValue label="Current period end" value={subscription.currentPeriodEnd ?? "None"} />
          <DebugValue label="Cancel at period end" value={subscription.cancelAtPeriodEnd ? "Yes" : "No"} />
          <DebugValue label="Updated" value={subscription.updatedAt ?? "Never"} />
        </dl>
      </section>

      <section className="rounded-md border border-border bg-white p-5">
        <h2 className="text-xl font-semibold">Latest webhook events</h2>
        {data.webhookEvents.length === 0 ? (
          <p className="mt-4 text-sm text-slate-600">No webhook events received.</p>
        ) : (
          <div className="mt-4 overflow-x-auto">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead className="border-b border-border text-slate-600">
                <tr>
                  <th className="py-2 pr-4 font-medium">Event</th>
                  <th className="py-2 pr-4 font-medium">Type</th>
                  <th className="py-2 pr-4 font-medium">Received</th>
                  <th className="py-2 pr-4 font-medium">Processed</th>
                  <th className="py-2 pr-4 font-medium">Error</th>
                </tr>
              </thead>
              <tbody>
                {data.webhookEvents.map((event) => (
                  <tr key={event.stripeEventId} className="border-b border-border last:border-0">
                    <td className="py-2 pr-4 font-mono text-xs">{event.stripeEventId}</td>
                    <td className="py-2 pr-4">{event.type}</td>
                    <td className="py-2 pr-4">{event.receivedAt}</td>
                    <td className="py-2 pr-4">{event.processedAt ?? "Pending"}</td>
                    <td className="py-2 pr-4">{event.processingError ?? "None"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </section>
  );
}

function DebugValue({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="font-medium text-slate-600">{label}</dt>
      <dd className="mt-1 break-words font-mono text-xs text-slate-950">{value}</dd>
    </div>
  );
}
