import { createContext, useContext, useState } from "react";
import type { ReactNode } from "react";
import { api, apiPost, apiUpload } from "@/lib/api";

export type UserRole = "Admin" | "Farmer" | "Customer" | "Courier";

export interface AuthUser {
  userId: number;
  email: string;
  fullName: string;
  avatarUrl: string | null;
  role: UserRole;
}

interface StoredAuth {
  token: string;
  user: AuthUser;
}

interface AuthResponseDto {
  token: string;
  // refreshToken/expiresAt тоже приходят в ответе (раздел 16 ТЗ), но пока
  // нигде не используются — silent-refresh истёкшего access-токена не
  // реализован ни здесь, ни в Login, отдельная задача на будущее.
  userId: number;
  email: string;
  fullName: string;
  avatarUrl: string | null;
  role: UserRole;
}

export interface RegisterPayload {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
  role: number;
}

interface AuthContextValue {
  token: string | null;
  user: AuthUser | null;
  login: (email: string, password: string, remember?: boolean) => Promise<AuthUser>;
  register: (payload: RegisterPayload, remember?: boolean) => Promise<AuthUser>;
  logout: () => void;
  uploadAvatar: (file: File) => Promise<void>;
  removeAvatar: () => Promise<void>;
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

  const persistAuth = (next: StoredAuth, remember: boolean) => {
    const serialized = JSON.stringify(next);
    if (remember) {
      localStorage.setItem(AUTH_STORAGE_KEY, serialized);
      sessionStorage.removeItem(AUTH_STORAGE_KEY);
    } else {
      sessionStorage.setItem(AUTH_STORAGE_KEY, serialized);
      localStorage.removeItem(AUTH_STORAGE_KEY);
    }
    setAuth(next);
  };

  const login = async (email: string, password: string, remember = true) => {
    const data = await api.post<AuthResponseDto>("/auth/login", { email, password });
    const next: StoredAuth = {
      token: data.token,
      user: { userId: data.userId, email: data.email, fullName: data.fullName, avatarUrl: data.avatarUrl, role: data.role },
    };
    persistAuth(next, remember);
    return next.user;
  };

  const register = async (payload: RegisterPayload, remember = true) => {
    const data = await api.post<AuthResponseDto>("/auth/register", payload);
    const next: StoredAuth = {
      token: data.token,
      user: { userId: data.userId, email: data.email, fullName: data.fullName, avatarUrl: data.avatarUrl, role: data.role },
    };
    persistAuth(next, remember);
    return next.user;
  };

  const logout = () => {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    sessionStorage.removeItem(AUTH_STORAGE_KEY);
    setAuth(null);
  };

  // Сохраняет обновлённого user в то же хранилище (localStorage/sessionStorage),
  // где он уже лежит — то же самое решает "Запомнить меня" при логине,
  // здесь просто патчим объект на месте, не трогая сам токен.
  const updateStoredUser = (patch: Partial<AuthUser>) => {
    setAuth((prev) => {
      if (!prev) return prev;
      const next: StoredAuth = { ...prev, user: { ...prev.user, ...patch } };
      const serialized = JSON.stringify(next);
      if (localStorage.getItem(AUTH_STORAGE_KEY)) localStorage.setItem(AUTH_STORAGE_KEY, serialized);
      else if (sessionStorage.getItem(AUTH_STORAGE_KEY)) sessionStorage.setItem(AUTH_STORAGE_KEY, serialized);
      return next;
    });
  };

  const uploadAvatar = async (file: File) => {
    const formData = new FormData();
    formData.append("file", file);
    const data = await apiUpload<{ avatarUrl: string }>("/me/avatar", formData);
    updateStoredUser({ avatarUrl: data.avatarUrl });
  };

  const removeAvatar = async () => {
    await api.delete("/me/avatar");
    updateStoredUser({ avatarUrl: null });
  };

  return (
    <AuthContext.Provider
      value={{ token: auth?.token ?? null, user: auth?.user ?? null, login, register, logout, uploadAvatar, removeAvatar }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}

// Шаг подтверждения email перед регистрацией (по явному запросу пользователя
// — до этого регистрация была одношаговой). Вне AuthProvider/контекста —
// вызывается ДО того, как у человека вообще появляется аккаунт/токен.
export function sendVerificationCode(email: string) {
  return apiPost<string>("/auth/send-verification-code", { email });
}

export function verifyEmailCode(email: string, code: string) {
  return apiPost<string>("/auth/verify-email-code", { email, code });
}

// "Забыли пароль?" — переиспользует тот же код-на-email механизм на бэкенде
// (тот же /auth/send-verification-code под капотом, просто с проверкой, что
// аккаунт существует), но это отдельные, семантически честные эндпоинты, а
// не подмена регистрационных.
export function forgotPassword(email: string) {
  return apiPost<string>("/auth/forgot-password", { email });
}

export function resetPassword(email: string, code: string, newPassword: string) {
  return apiPost<string>("/auth/reset-password", { email, code, newPassword });
}
