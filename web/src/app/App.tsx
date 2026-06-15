import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BriefcaseBusiness } from "lucide-react";
import { BrowserRouter, Link, Navigate, Route, Routes } from "react-router-dom";
import { Button } from "@/components/ui/button";

const queryClient = new QueryClient();

function Shell() {
  return (
    <div className="min-h-screen">
      <header className="border-b border-border bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
          <Link to="/" className="flex items-center gap-2 text-base font-semibold">
            <BriefcaseBusiness aria-hidden="true" className="h-5 w-5 text-primary" />
            FlowDesk
          </Link>
          <nav className="flex items-center gap-2">
            <Link
              to="/login"
              className="inline-flex h-10 items-center justify-center rounded-md border border-border bg-background px-4 py-2 text-sm font-medium transition-colors hover:bg-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              Sign in
            </Link>
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-10">
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/login" element={<AuthPlaceholder />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
}

function Dashboard() {
  return (
    <section className="grid gap-6 md:grid-cols-[1fr_320px]">
      <div>
        <p className="text-sm font-medium text-primary">Case workspace</p>
        <h1 className="mt-2 text-3xl font-semibold">FlowDesk</h1>
        <p className="mt-3 max-w-2xl text-slate-600">
          The product shell is ready for auth, CaseFlow data, and billing gates.
        </p>
      </div>
      <div className="rounded-md border border-border bg-white p-5">
        <h2 className="text-sm font-semibold">Next scaffold step</h2>
        <p className="mt-2 text-sm text-slate-600">
          Sign-up and sign-in routes will attach to the API once the auth task starts.
        </p>
        <Button className="mt-4" disabled>
          Auth pending
        </Button>
      </div>
    </section>
  );
}

function AuthPlaceholder() {
  return <h1 className="text-2xl font-semibold">Sign in</h1>;
}

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Shell />
      </BrowserRouter>
    </QueryClientProvider>
  );
}
