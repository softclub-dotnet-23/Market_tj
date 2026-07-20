import { createContext, useContext, useEffect, useState } from "react";
import type { ReactNode } from "react";
import { api } from "@/lib/api";

// Роль приходит от бэкенда числом (System.Text.Json сериализует enum как int) —
// см. MarketTJ.Domain.Enums.UserRole (Admin=1, Farmer=2, Customer=3, Courier=4).
export type UserRole = "Admin" | "Farmer" | "Customer" | "Courier";

const ROLE_MAP: Record<number, UserRole> = {
  1: "Admin",
  2: "Farmer",
  3: "Customer",
  4: "Courier",
};

export interface AuthUser {
  id: number;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
}

interface LoginApiResponse {
  id: number;
  fullName: string;
  email: string;
  phoneNumber: string;
  role: number;
}

interface AuthContextValue {
  user: AuthUser | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<AuthUser>;
  logout: () => void;
}

// В бэкенде пока нет JWT/Identity (см. TODO в AuthController) — "сессия"
// на фронте временно держится просто как объект пользователя в localStorage,
// без токена. Когда появится настоящая аутентификация — заменить на токен.
const STORAGE_KEY = "marketTJ.auth.user";

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      try {
        setUser(JSON.parse(raw) as AuthUser);
      } catch {
        localStorage.removeItem(STORAGE_KEY);
      }
    }
    setIsLoading(false);
  }, []);

  const login = async (email: string, password: string) => {
    const response = await api.post<LoginApiResponse>("/auth/login", { email, password });
    const authUser: AuthUser = {
      id: response.id,
      fullName: response.fullName,
      email: response.email,
      phoneNumber: response.phoneNumber,
      role: ROLE_MAP[response.role] ?? "Customer",
    };
    setUser(authUser);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(authUser));
    return authUser;
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem(STORAGE_KEY);
  };

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
