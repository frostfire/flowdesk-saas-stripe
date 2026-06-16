import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { authSchema } from "./schema";
import type { AuthFormValues } from "./types";

type AuthFormProps = {
  mode: "signin" | "signup";
  onSubmit(values: AuthFormValues): Promise<void>;
};

export function AuthForm({ mode, onSubmit }: AuthFormProps) {
  const navigate = useNavigate();
  const [formError, setFormError] = useState<string | null>(null);
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
  } = useForm<AuthFormValues>({
    resolver: zodResolver(authSchema),
    defaultValues: {
      email: "",
      password: "",
      rememberMe: false,
    },
  });

  const title = mode === "signin" ? "Sign in" : "Create account";
  const alternate = mode === "signin" ? "/register" : "/login";
  const alternateText = mode === "signin" ? "Create account" : "Sign in";
  const helpText =
    mode === "signup"
      ? "This is a sandbox running in Stripe test mode — no real data and no real charges. Sign up with any email (a real address isn't required); your password needs at least 8 characters including a lowercase letter and a number, for example flowdesk1."
      : "Demo accounts are self-serve — there are no shared logins. If you don't have one yet, create an account below. It's a sandbox in Stripe test mode.";

  return (
    <section className="mx-auto max-w-md">
      <div className="rounded-md border border-border bg-white p-6">
        <h1 className="text-2xl font-semibold">{title}</h1>
        <p className="mt-2 text-sm text-zinc-600">{helpText}</p>
        <form
          className="mt-6 grid gap-4"
          onSubmit={handleSubmit(async (values) => {
            setFormError(null);
            try {
              await onSubmit(values);
              navigate("/app", { replace: true });
            } catch {
              setFormError("Check your email and password, then try again.");
            }
          })}
        >
          <label className="grid gap-2 text-sm font-medium">
            Email
            <input
              className="h-10 rounded-md border border-border px-3 text-sm outline-none focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
              type="email"
              autoComplete="email"
              {...register("email")}
            />
            {errors.email ? <span className="text-sm font-normal text-red-600">{errors.email.message}</span> : null}
          </label>
          <label className="grid gap-2 text-sm font-medium">
            Password
            <input
              className="h-10 rounded-md border border-border px-3 text-sm outline-none focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
              type="password"
              autoComplete={mode === "signin" ? "current-password" : "new-password"}
              {...register("password")}
            />
            {errors.password ? (
              <span className="text-sm font-normal text-red-600">{errors.password.message}</span>
            ) : null}
          </label>
          <label className="flex items-center gap-2 text-sm font-medium">
            <input
              className="h-4 w-4 rounded border-border text-primary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
              type="checkbox"
              {...register("rememberMe")}
            />
            Remember Me
          </label>
          {formError ? <p className="text-sm text-red-600">{formError}</p> : null}
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Working" : title}
          </Button>
        </form>
        <Link className="mt-4 inline-flex text-sm font-medium text-primary" to={alternate}>
          {alternateText}
        </Link>
      </div>
    </section>
  );
}
