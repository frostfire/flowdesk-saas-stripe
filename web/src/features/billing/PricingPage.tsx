import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { createCheckoutSession, createPortalSession, getCurrentEntitlements } from "./billingApi";
import { DunningBanner } from "./DunningBanner";
import type { PlanCode } from "./types";

type PricingPageProps = {
  token: string;
  redirectTo?(url: string): void;
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

type BillingUiState =
  | { status: "idle" }
  | { status: "redirecting"; target: "checkout" | "portal"; plan?: Exclude<PlanCode, "Free"> }
  | { status: "success" }
  | { status: "cancelled" }
  | { status: "syncing" };

export function PricingPage({ token, redirectTo = (url) => window.location.assign(url), onUnauthorized }: PricingPageProps) {
  const [billingState, setBillingState] = useState<BillingUiState>({ status: "idle" });
  const entitlements = useQuery({
    queryKey: ["entitlements"],
    queryFn: () => getCurrentEntitlements(token, onUnauthorized),
  });
  const checkout = useMutation({
    mutationFn: (plan: Exclude<PlanCode, "Free">) => createCheckoutSession(token, plan, onUnauthorized),
    onMutate: (plan) => {
      setBillingState({ status: "redirecting", target: "checkout", plan });
    },
    onSuccess: (session) => {
      setBillingState({ status: "syncing" });
      redirectTo(session.url);
    },
    onError: () => {
      setBillingState({ status: "idle" });
    },
  });
  const portal = useMutation({
    mutationFn: () => createPortalSession(token, onUnauthorized),
    onMutate: () => {
      setBillingState({ status: "redirecting", target: "portal" });
    },
    onSuccess: (session) => {
      setBillingState({ status: "syncing" });
      redirectTo(session.url);
    },
    onError: () => {
      setBillingState({ status: "idle" });
    },
  });
  const isPortalRedirecting = billingState.status === "redirecting" && billingState.target === "portal";

  return (
    <section className="grid gap-6">
      <DunningBanner token={token} onUnauthorized={onUnauthorized} />
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium text-primary">Billing</p>
          <h1 className="mt-2 text-3xl font-semibold">Plans</h1>
        </div>
        <Button variant="outline" disabled={isPortalRedirecting} onClick={() => portal.mutate()}>
          {isPortalRedirecting ? "Redirecting" : "Manage billing"}
        </Button>
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        {plans.map((plan) => (
          <PlanCard
            key={plan.code}
            plan={plan}
            currentPlan={entitlements.data?.plan}
            isRedirecting={
              billingState.status === "redirecting" &&
              billingState.target === "checkout" &&
              billingState.plan === plan.code
            }
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
      {billingState.status === "syncing" ? (
        <div className="rounded-md border border-blue-200 bg-blue-50 p-4 text-sm text-blue-950">
          Billing is syncing.
        </div>
      ) : null}
      <section className="rounded-md border border-amber-200 bg-amber-50 p-4 text-sm text-amber-950">
        <h2 className="font-semibold">Stripe test mode</h2>
        <p className="mt-2">TODO(claude): hosted-demo test checkout note.</p>
        <dl className="mt-4 grid gap-2 sm:grid-cols-3">
          <div>
            <dt className="font-medium">Success</dt>
            <dd>4242 4242 4242 4242</dd>
          </div>
          <div>
            <dt className="font-medium">Decline</dt>
            <dd>4000 0000 0000 0002</dd>
          </div>
          <div>
            <dt className="font-medium">Failed payment</dt>
            <dd>4000 0000 0000 0341</dd>
          </div>
        </dl>
      </section>
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
      <Button
        aria-label={isFree ? "Free plan included" : `Upgrade to ${plan.code}`}
        className="mt-6 w-full"
        disabled={isFree || isCurrent || isRedirecting}
        onClick={onUpgrade}
      >
        {isRedirecting ? "Redirecting" : isFree ? "Included" : "Upgrade"}
      </Button>
    </article>
  );
}
