import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

const TOKEN_KEY = "productservice.auth.token";
const USER_KEY = "productservice.auth.user";

interface AuthUser {
  username: string;
  roles: string[];
  expiresAtUtc: string;
}

interface AuthContextValue {
  user: AuthUser | null;
  token: string | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function loadFromStorage(): { token: string | null; user: AuthUser | null } {
  if (typeof window === "undefined") return { token: null, user: null };
  try {
    const token = window.localStorage.getItem(TOKEN_KEY);
    const userRaw = window.localStorage.getItem(USER_KEY);
    const user = userRaw ? (JSON.parse(userRaw) as AuthUser) : null;

    // Drop the token if expired (lazy cleanup).
    if (user && new Date(user.expiresAtUtc).getTime() < Date.now()) {
      window.localStorage.removeItem(TOKEN_KEY);
      window.localStorage.removeItem(USER_KEY);
      return { token: null, user: null };
    }

    return { token, user };
  } catch {
    return { token: null, user: null };
  }
}

const API_BASE =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  "https://productservice-api-jk2026.azurewebsites.net/api";

export function AuthProvider({ children }: { children: ReactNode }) {
  const initial = loadFromStorage();
  const [token, setToken] = useState<string | null>(initial.token);
  const [user, setUser] = useState<AuthUser | null>(initial.user);

  // Periodically drop the token when it expires (every minute).
  useEffect(() => {
    if (!user) return;
    const expiresAt = new Date(user.expiresAtUtc).getTime();
    const remaining = expiresAt - Date.now();
    if (remaining <= 0) {
      logout();
      return;
    }
    const t = setTimeout(() => logout(), remaining);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.expiresAtUtc]);

  const login = useCallback(async (username: string, password: string) => {
    const res = await fetch(`${API_BASE}/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
    });
    if (!res.ok) {
      let detail = "Login failed.";
      try {
        const body = (await res.json()) as { error?: string };
        if (body?.error) detail = body.error;
      } catch {
        /* ignore */
      }
      throw new Error(detail);
    }
    const body = (await res.json()) as {
      token: string;
      username: string;
      roles: string[];
      expiresAtUtc: string;
    };

    setToken(body.token);
    const u: AuthUser = {
      username: body.username,
      roles: body.roles,
      expiresAtUtc: body.expiresAtUtc,
    };
    setUser(u);
    window.localStorage.setItem(TOKEN_KEY, body.token);
    window.localStorage.setItem(USER_KEY, JSON.stringify(u));
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    setUser(null);
    window.localStorage.removeItem(TOKEN_KEY);
    window.localStorage.removeItem(USER_KEY);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: token !== null,
      isAdmin: user?.roles.includes("Admin") ?? false,
      login,
      logout,
    }),
    [user, token, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>.");
  return ctx;
}

/**
 * Returns the current JWT for one-off fetch calls. Hook-free because callers
 * outside React (like the fetch wrapper in api.ts) can't use useContext.
 */
export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(TOKEN_KEY);
}
