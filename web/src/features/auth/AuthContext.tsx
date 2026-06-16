import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { apiRequest } from "@/lib/api";
import type { AuthFormValues, AuthResponse, AuthUser } from "./types";

type AuthContextValue = {
  token: string | null;
  user: AuthUser | null;
  isRestoring: boolean;
  signIn(values: AuthFormValues): Promise<void>;
  signUp(values: AuthFormValues): Promise<void>;
  signOut(): void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isRestoring, setIsRestoring] = useState(true);

  const applyAuth = useCallback((response: AuthResponse) => {
    setToken(response.accessToken);
    setUser(response.user);
  }, []);

  const signOut = useCallback(() => {
    setToken(null);
    setUser(null);
    void apiRequest<void>("/auth/logout", { method: "POST" }).catch(() => undefined);
  }, []);

  useEffect(() => {
    let isMounted = true;

    apiRequest<AuthResponse>("/auth/refresh", { method: "POST" })
      .then((response) => {
        if (isMounted) {
          applyAuth(response);
        }
      })
      .catch(() => undefined)
      .finally(() => {
        if (isMounted) {
          setIsRestoring(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [applyAuth]);

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
      isRestoring,
      signIn,
      signUp,
      signOut,
    }),
    [isRestoring, signIn, signOut, signUp, token, user],
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
