import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { getCurrentEntitlements } from "./billingApi";

type DunningBannerProps = {
  token: string;
  onUnauthorized(): void;
};

export function DunningBanner({ token, onUnauthorized }: DunningBannerProps) {
  const query = useQuery({
    queryKey: ["entitlements"],
    queryFn: () => getCurrentEntitlements(token, onUnauthorized),
  });

  if (query.data?.status !== "PastDue") {
    return null;
  }

  return (
    <div className="rounded-md border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950">
      <div className="font-semibold">Payment failed</div>
      <p className="mt-1">
        Billing is past due. Case actions and analytics are locked until the payment method is updated.
      </p>
      <Link
        to="/pricing"
        className="mt-3 inline-flex h-9 items-center justify-center rounded-md border border-amber-400 bg-white px-3 text-sm font-medium transition-colors hover:bg-amber-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      >
        Manage billing
      </Link>
    </div>
  );
}
