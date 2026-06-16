import { Link } from "react-router-dom";

type BillingResultState = "success" | "cancelled" | "syncing";

export function BillingResultPage({ status }: { status: "success" | "cancel" }) {
  const state: BillingResultState = status === "success" ? "syncing" : "cancelled";
  const isSuccess = state === "syncing";

  return (
    <section className="mx-auto max-w-xl">
      <h1 className="text-3xl font-semibold">{isSuccess ? "Billing updated" : "Checkout cancelled"}</h1>
      <p className="mt-3 text-slate-600">
        {isSuccess ? "Your subscription is syncing." : "Your subscription was not changed."}
      </p>
      <Link
        className="mt-6 inline-flex h-10 items-center justify-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
        to="/app"
      >
        Return to app
      </Link>
    </section>
  );
}
