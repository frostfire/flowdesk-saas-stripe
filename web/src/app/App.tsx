import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BriefcaseBusiness } from "lucide-react";
import { useEffect } from "react";
import { BrowserRouter, Link, Navigate, Route, Routes, useLocation } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { AuthForm } from "@/features/auth/AuthForm";
import { AuthProvider, useAuth } from "@/features/auth/AuthContext";
import { ProtectedRoute } from "@/features/auth/ProtectedRoute";
import { AdminDebugPage } from "@/features/billing/AdminDebugPage";
import { AnalyticsPage } from "@/features/billing/AnalyticsPage";
import { BillingResultPage } from "@/features/billing/BillingResultPage";
import { DunningBanner } from "@/features/billing/DunningBanner";
import { EntitlementSummary } from "@/features/billing/EntitlementSummary";
import { GatedActions } from "@/features/billing/GatedActions";
import { PricingPage } from "@/features/billing/PricingPage";
import { CaseList } from "@/features/cases/CaseList";

const queryClient = new QueryClient();

function Shell() {
  const { signOut, token, user } = useAuth();

  return (
    <div className="min-h-screen">
      <RouteFocusManager />
      <header className="border-b border-border bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
          <Link to="/" className="flex items-center gap-2 text-base font-semibold">
            <BriefcaseBusiness aria-hidden="true" className="h-5 w-5 text-primary" />
            FlowDesk
          </Link>
          <nav className="flex items-center gap-2">
            {token ? (
              <>
                <Link
                  to="/pricing"
                  className="inline-flex h-10 items-center justify-center rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                >
                  Plans
                </Link>
                <Link
                  to="/admin"
                  className="inline-flex h-10 items-center justify-center rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                >
                  Admin
                </Link>
                <span className="hidden text-sm text-slate-600 sm:inline">{user?.email}</span>
                <Button variant="outline" onClick={signOut}>
                  Sign out
                </Button>
              </>
            ) : (
              <Link
                to="/login"
                className="inline-flex h-10 items-center justify-center rounded-md border border-border bg-background px-4 py-2 text-sm font-medium transition-colors hover:bg-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
              >
                Sign in
              </Link>
            )}
          </nav>
        </div>
      </header>
      <main id="main-content" tabIndex={-1} className="mx-auto max-w-6xl px-4 py-10 outline-none">
        <Routes>
          <Route path="/" element={<Navigate to="/app" replace />} />
          <Route path="/login" element={<AuthPage mode="signin" />} />
          <Route path="/register" element={<AuthPage mode="signup" />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/app" element={<Dashboard />} />
            <Route path="/admin" element={<AdminRoute />} />
            <Route path="/analytics" element={<AnalyticsRoute />} />
            <Route path="/pricing" element={<PricingRoute />} />
            <Route path="/billing/success" element={<BillingResultPage status="success" />} />
            <Route path="/billing/cancel" element={<BillingResultPage status="cancel" />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
}

function RouteFocusManager() {
  const location = useLocation();

  useEffect(() => {
    document.getElementById("main-content")?.focus();
  }, [location.pathname]);

  return null;
}

function Dashboard() {
  const { signOut, token, user } = useAuth();

  return (
    <section className="grid gap-8">
      <div>
        <p className="text-sm font-medium text-primary">Case workspace</p>
        <h1 className="mt-2 text-3xl font-semibold">Welcome, {user?.email}</h1>
        <p className="mt-3 max-w-2xl text-slate-600">
          Review the current CaseFlow queue and plan-gated workspace actions.
        </p>
      </div>
      {token ? (
        <>
          <DunningBanner token={token} onUnauthorized={signOut} />
          <section className="rounded-md border border-border bg-white p-5">
            <h2 className="text-xl font-semibold">How this demo works</h2>
            <p className="mt-3 text-sm text-slate-700">
              FlowDesk is a working subscription-billing app running in Stripe test mode — nothing is ever charged.
            </p>
            <ul className="mt-4 grid gap-2 text-sm text-slate-700">
              <li>
                You start on the <strong>Free</strong> plan: view cases, read-only.
              </li>
              <li>
                <strong>Upgrade to Pro</strong> from Pricing with a test card (
                <code>4242 4242 4242 4242</code>, any future date and CVC). Checkout runs on Stripe; when you
                return, Pro unlocks — create, edit, approve, and delete cases, plus analytics.
              </li>
              <li>
                <strong>Manage billing</strong> opens Stripe's customer portal to change or cancel your plan.
              </li>
              <li>
                <strong>Trigger a failed payment</strong> (decline card <code>4000 0000 0000 0341</code>, or a
                failed renewal) to watch the account flip to past-due: a dunning banner appears and Pro features lock
                again.
              </li>
            </ul>
            <p className="mt-4 text-sm text-slate-700">
              Plan changes flow back through signed Stripe webhooks, so the app always reflects Stripe. Case data is
              per-visitor and in-memory, and resets periodically.
            </p>
          </section>
          <EntitlementSummary token={token} onUnauthorized={signOut} />
          <GatedActions token={token} onUnauthorized={signOut} />
          <CaseList token={token} onUnauthorized={signOut} />
        </>
      ) : null}
    </section>
  );
}

function PricingRoute() {
  const { signOut, token } = useAuth();

  return token ? <PricingPage token={token} onUnauthorized={signOut} /> : null;
}

function AnalyticsRoute() {
  const { signOut, token } = useAuth();

  return token ? <AnalyticsPage token={token} onUnauthorized={signOut} /> : null;
}

function AdminRoute() {
  const { signOut, token } = useAuth();

  return token ? <AdminDebugPage token={token} onUnauthorized={signOut} /> : null;
}

function AuthPage({ mode }: { mode: "signin" | "signup" }) {
  const { signIn, signUp } = useAuth();

  return <AuthForm mode={mode} onSubmit={mode === "signin" ? signIn : signUp} />;
}

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Shell />
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}
