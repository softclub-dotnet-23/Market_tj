import { createContext, useContext, useState } from "react";
import type { ReactNode } from "react";
import { api } from "@/lib/api";

export type UserRole = "Admin" | "Farmer" | "Customer" | "Courier";

export interface AuthUser {
  id: number;
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
  login: (email: string, password: string) => Promise<AuthUser>;
  logout: () => void;
}

const AUTH_STORAGE_KEY = "market-tj-auth";

const AuthContext = createContext<AuthContextValue | null>(null);

function readStoredAuth(): StoredAuth | null {
  try {
    const raw = localStorage.getItem(AUTH_STORAGE_KEY);
    return raw ? (JSON.parse(raw) as StoredAuth) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<StoredAuth | null>(readStoredAuth);

  const login = async (email: string, password: string) => {
    const data = await api.post<LoginResponseDto>("/auth/login", { email, password });
    const user: AuthUser = { id: data.userId, email: data.email, fullName: data.fullName, role: data.role };
    setAuth({ token: data.token, user });
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify({ token: data.token, user }));
    return user;
  };

  const logout = () => {
    localStorage.removeItem(AUTH_STORAGE_KEY);
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
