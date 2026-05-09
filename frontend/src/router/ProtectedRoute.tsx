import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../stores/auth';

export function ProtectedRoute() {
  return useAuth((s) => s.accessToken || s.user) ? <Outlet /> : <Navigate to="/login" replace />;
}
