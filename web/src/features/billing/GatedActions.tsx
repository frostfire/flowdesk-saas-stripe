import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { getCurrentEntitlements } from "./billingApi";

type GatedActionsProps = {
  token: string;
  onUnauthorized(): void;
};

export function GatedActions({ token, onUnauthorized }: GatedActionsProps) {
  const query = useQuery({
    queryKey: ["entitlements"],
    queryFn: () => getCurrentEntitlements(token, onUnauthorized),
  });
  const entitlements = query.data?.entitlements;

  return (
    <div className="flex flex-wrap gap-3">
      <Button disabled={!entitlements?.canCreateCases}>Create case</Button>
      {entitlements?.canViewAnalytics ? (
        <Link
          to="/analytics"
          className="inline-flex h-10 items-center justify-center rounded-md border border-border bg-background px-4 py-2 text-sm font-medium transition-colors hover:bg-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
        >
          Analytics
        </Link>
      ) : null}
      <Link
        to="/pricing"
        className="inline-flex h-10 items-center justify-center rounded-md border border-border bg-background px-4 py-2 text-sm font-medium transition-colors hover:bg-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      >
        Manage plan
      </Link>
    </div>
  );
}
