import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import { apiRequest } from "@/lib/api";
import type { AuthFormValues, AuthResponse, AuthUser } from "./types";

type AuthContextValue = {
  token: string | null;
  user: AuthUser | null;
  signIn(values: AuthFormValues): Promise<void>;
  signUp(values: AuthFormValues): Promise<void>;
  signOut(): void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<AuthUser | null>(null);

  const applyAuth = useCallback((response: AuthResponse) => {
    setToken(response.accessToken);
    setUser(response.user);
  }, []);

  const signOut = useCallback(() => {
    setToken(null);
    setUser(null);
  }, []);

  const signIn = useCallback(
    async (values: AuthFormValues) => {
      applyAuth(
        await apiRequest<AuthResponse>("/auth/login", {
          method: "POST",
          body: JSON.stringify(values),
          onUnauthorized: signOut,
        }),
      );
    },
    [applyAuth, signOut],
  );

  const signUp = useCallback(
    async (values: AuthFormValues) => {
      applyAuth(
        await apiRequest<AuthResponse>("/auth/register", {
          method: "POST",
          body: JSON.stringify(values),
          onUnauthorized: signOut,
        }),
      );
    },
    [applyAuth, signOut],
  );

  const value = useMemo(
    () => ({
      token,
      user,
      signIn,
      signUp,
      signOut,
    }),
    [signIn, signOut, signUp, token, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return value;
}
