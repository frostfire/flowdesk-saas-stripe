import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "./AuthContext";

export function ProtectedRoute() {
  const { isRestoring, token } = useAuth();
  const location = useLocation();

  if (isRestoring) {
    return <div className="text-sm text-slate-600">Restoring session.</div>;
  }

  if (!token) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
