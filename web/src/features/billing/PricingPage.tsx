import { useMutation, useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { createCheckoutSession, createPortalSession, getCurrentEntitlements } from "./billingApi";
import { DunningBanner } from "./DunningBanner";
import type { PlanCode } from "./types";

type PricingPageProps = {
  token: string;
  onUnauthorized(): void;
};

type Plan = {
  code: PlanCode;
  price: string;
  features: string[];
};

const plans: Plan[] = [
  {
    code: "Free",
    price: "$0",
    features: ["Read-only cases", "1 seat", "10 case limit"],
  },
  {
    code: "Pro",
    price: "$19",
    features: ["Create and approve cases", "Analytics", "250 case limit"],
  },
  {
    code: "Team",
    price: "$49",
    features: ["Create and approve cases", "Analytics", "10 seats"],
  },
];

export function PricingPage({ token, onUnauthorized }: PricingPageProps) {
  const entitlements = useQuery({
    queryKey: ["entitlements"],
    queryFn: () => getCurrentEntitlements(token, onUnauthorized),
  });
  const checkout = useMutation({
    mutationFn: (plan: Exclude<PlanCode, "Free">) => createCheckoutSession(token, plan, onUnauthorized),
    onSuccess: (session) => {
      window.location.assign(session.url);
    },
  });
  const portal = useMutation({
    mutationFn: () => createPortalSession(token, onUnauthorized),
    onSuccess: (session) => {
      window.location.assign(session.url);
    },
  });

  return (
    <section className="grid gap-6">
      <DunningBanner token={token} onUnauthorized={onUnauthorized} />
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium text-primary">Billing</p>
          <h1 className="mt-2 text-3xl font-semibold">Plans</h1>
        </div>
        <Button variant="outline" disabled={portal.isPending} onClick={() => portal.mutate()}>
          {portal.isPending ? "Redirecting" : "Manage billing"}
        </Button>
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        {plans.map((plan) => (
          <PlanCard
            key={plan.code}
            plan={plan}
            currentPlan={entitlements.data?.plan}
            isRedirecting={checkout.isPending && checkout.variables === plan.code}
            onUpgrade={() => {
              if (plan.code !== "Free") {
                checkout.mutate(plan.code);
              }
            }}
          />
        ))}
      </div>
      {checkout.isError ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-900">
          Checkout is unavailable.
        </div>
      ) : null}
      {portal.isError ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-4 text-sm text-red-900">
          Billing portal is unavailable.
        </div>
      ) : null}
      <p className="text-sm text-slate-600">Test checkout details pending.</p>
    </section>
  );
}

function PlanCard({
  currentPlan,
  isRedirecting,
  onUpgrade,
  plan,
}: {
  currentPlan?: PlanCode;
  isRedirecting: boolean;
  onUpgrade(): void;
  plan: Plan;
}) {
  const isCurrent = currentPlan === plan.code;
  const isFree = plan.code === "Free";

  return (
    <article className="rounded-md border border-border bg-white p-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold">{plan.code}</h2>
          <p className="mt-1 text-sm text-slate-600">{plan.price}/mo</p>
        </div>
        {isCurrent ? <span className="rounded-md bg-slate-100 px-2 py-1 text-xs font-medium">Current</span> : null}
      </div>
      <ul className="mt-5 grid gap-2 text-sm text-slate-700">
        {plan.features.map((feature) => (
          <li key={feature}>{feature}</li>
        ))}
      </ul>
      <Button className="mt-6 w-full" disabled={isFree || isCurrent || isRedirecting} onClick={onUpgrade}>
        {isRedirecting ? "Redirecting" : isFree ? "Included" : "Upgrade"}
      </Button>
    </article>
  );
}
