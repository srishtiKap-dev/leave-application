import { Outlet } from 'react-router-dom';
import { useAuth } from '../stores/auth';
import type { Role } from '../types';

export function RoleRoute({ roles }: { roles: Role[] }) {
  const userRoles = useAuth((state) => state.user?.roles ?? []);
  const allowed = roles.some((role) => userRoles.includes(role));

  if (!allowed) {
    return (
      <section className="card max-w-2xl">
        <h1 className="text-xl font-bold">You are not allowed to view this</h1>
        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Your account does not have permission to access this portal.</p>
      </section>
    );
  }

  return <Outlet />;
}
