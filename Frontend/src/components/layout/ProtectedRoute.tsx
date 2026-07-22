import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";

export function ProtectedRoute({ role }: { role: string }) {
  const { token, user } = useAuth();

  if (!token || user?.role !== role) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
