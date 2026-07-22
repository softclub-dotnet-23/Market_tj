import { createContext, useContext, useState } from "react";
import type { ReactNode } from "react";
import { api } from "@/lib/api";

export type UserRole = "Admin" | "Farmer" | "Customer" | "Courier";

export interface AuthUser {
  userId: number;
  email: string;
  fullName: string;
  role: UserRole;
}

interface StoredAuth {
  token: string;
  user: AuthUser;
}

interface LoginResponseDto {
  token: string;
  userId: number;
  email: string;
  fullName: string;
  role: UserRole;
}

interface AuthContextValue {
  token: string | null;
  user: AuthUser | null;
  login: (email: string, password: string, remember?: boolean) => Promise<AuthUser>;
  logout: () => void;
}
    
const AUTH_STORAGE_KEY = "market-tj-auth";

const AuthContext = createContext<AuthContextValue | null>(null);

function readStoredAuth(): StoredAuth | null {
  try {
    // "Запомнить меня" решает, куда попал токен при входе: localStorage
    // переживает закрытие браузера, sessionStorage — только текущую вкладку.
    const raw = localStorage.getItem(AUTH_STORAGE_KEY) ?? sessionStorage.getItem(AUTH_STORAGE_KEY);
    return raw ? (JSON.parse(raw) as StoredAuth) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<StoredAuth | null>(readStoredAuth);

  const login = async (email: string, password: string, remember = true) => {
    const data = await api.post<LoginResponseDto>("/auth/login", { email, password });
    const next: StoredAuth = {
      token: data.token,
      user: { userId: data.userId, email: data.email, fullName: data.fullName, role: data.role },
    };
    const serialized = JSON.stringify(next);
    if (remember) {
      localStorage.setItem(AUTH_STORAGE_KEY, serialized);
      sessionStorage.removeItem(AUTH_STORAGE_KEY);
    } else {
      sessionStorage.setItem(AUTH_STORAGE_KEY, serialized);
      localStorage.removeItem(AUTH_STORAGE_KEY);
    }
    setAuth(next);
    return next.user;
  };

  const logout = () => {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    sessionStorage.removeItem(AUTH_STORAGE_KEY);
    setAuth(null);
  };

  return (
    <AuthContext.Provider value={{ token: auth?.token ?? null, user: auth?.user ?? null, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
