import { Bell, CalendarDays, CreditCard, LayoutDashboard, LogOut, Users } from 'lucide-react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { useAuth } from '../stores/auth';
import type { Role } from '../types';

const links = [
  ['Dashboard', '/dashboard', LayoutDashboard, ['Employee', 'Manager', 'HRAdmin', 'SuperAdmin']],
  ['My Leaves', '/leaves', CalendarDays, ['Employee', 'Manager', 'HRAdmin', 'SuperAdmin']],
  ['My Expenses', '/expenses', CreditCard, ['Employee', 'Manager', 'HRAdmin', 'SuperAdmin']],
  ['Manager Dashboard', '/manager/dashboard', LayoutDashboard, ['Manager', 'HRAdmin', 'SuperAdmin']],
  ['Leave Approvals', '/manager/leaves', CalendarDays, ['Manager', 'HRAdmin', 'SuperAdmin']],
  ['Expense Approvals', '/manager/expenses', CreditCard, ['Manager', 'HRAdmin', 'SuperAdmin']],
  ['HR Dashboard', '/hr/dashboard', LayoutDashboard, ['HRAdmin', 'SuperAdmin']],
  ['HR Leaves', '/hr/leaves', CalendarDays, ['HRAdmin', 'SuperAdmin']],
  ['HR Expenses', '/hr/expenses', CreditCard, ['HRAdmin', 'SuperAdmin']],
  ['Employees', '/hr/users', Users, ['HRAdmin', 'SuperAdmin']],
  ['Notifications', '/notifications', Bell, ['Employee', 'Manager', 'HRAdmin', 'SuperAdmin']]
] as const;

export function AppLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const userRoles = user?.roles ?? [];
  const visibleLinks = links.filter(([, , , roles]) => roles.some((role) => userRoles.includes(role as Role)));
  return (
    <div className="min-h-screen md:flex">
      <aside className="border-b border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900 md:w-64 md:border-b-0 md:border-r">
        <div className="mb-6 px-2 text-xl font-extrabold text-primary">LeavePortal</div>
        <nav className="grid gap-1">
          {visibleLinks.map(([label, href, Icon]) => <NavLink key={href} to={href} className={({ isActive }) => `flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium ${isActive ? 'bg-indigo-50 text-primary dark:bg-indigo-950' : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'}`}><Icon size={18} />{label}</NavLink>)}
        </nav>
      </aside>
      <main className="flex-1">
        <header className="sticky top-0 z-10 flex h-16 items-center justify-between border-b border-slate-200 bg-white/90 px-4 backdrop-blur dark:border-slate-800 dark:bg-slate-900/90">
          <input aria-label="Global search" placeholder="Search employees, leaves, expenses" className="h-10 w-full max-w-md rounded-md border border-slate-300 bg-transparent px-3 text-sm dark:border-slate-700" />
          <div className="ml-4 flex items-center gap-3 text-sm"><span>{user?.firstName}</span><button aria-label="Logout" onClick={() => { logout(); navigate('/login'); }}><LogOut size={18} /></button></div>
        </header>
        <div className="p-4 md:p-6"><Outlet /></div>
      </main>
      <Toaster position="top-right" />
    </div>
  );
}
