import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BriefcaseBusiness } from "lucide-react";
import { BrowserRouter, Link, Navigate, Route, Routes } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { AuthForm } from "@/features/auth/AuthForm";
import { AuthProvider, useAuth } from "@/features/auth/AuthContext";
import { ProtectedRoute } from "@/features/auth/ProtectedRoute";
import { CaseList } from "@/features/cases/CaseList";

const queryClient = new QueryClient();

function Shell() {
  const { signOut, token, user } = useAuth();

  return (
    <div className="min-h-screen">
      <header className="border-b border-border bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
          <Link to="/" className="flex items-center gap-2 text-base font-semibold">
            <BriefcaseBusiness aria-hidden="true" className="h-5 w-5 text-primary" />
            FlowDesk
          </Link>
          <nav className="flex items-center gap-2">
            {token ? (
              <>
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
      <main className="mx-auto max-w-6xl px-4 py-10">
        <Routes>
          <Route path="/" element={<Navigate to="/app" replace />} />
          <Route path="/login" element={<AuthPage mode="signin" />} />
          <Route path="/register" element={<AuthPage mode="signup" />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/app" element={<Dashboard />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
}

function Dashboard() {
  const { signOut, token, user } = useAuth();

  return (
    <section className="grid gap-8">
      <div>
        <p className="text-sm font-medium text-primary">Case workspace</p>
        <h1 className="mt-2 text-3xl font-semibold">Welcome, {user?.email}</h1>
        <p className="mt-3 max-w-2xl text-slate-600">
          Review the current CaseFlow queue before billing gates and case actions are added.
        </p>
      </div>
      {token ? <CaseList token={token} onUnauthorized={signOut} /> : null}
    </section>
  );
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
