import { Bell, CalendarDays, CreditCard, LayoutDashboard, LogOut, Users } from 'lucide-react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { useAuth } from '../stores/auth';

const links = [
  ['Dashboard', '/dashboard', LayoutDashboard],
  ['Leaves', '/leaves', CalendarDays],
  ['Expenses', '/expenses', CreditCard],
  ['Manager', '/manager/dashboard', Users],
  ['HR', '/hr/dashboard', Users],
  ['Notifications', '/notifications', Bell]
] as const;

export function AppLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  return (
    <div className="min-h-screen md:flex">
      <aside className="border-b border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900 md:w-64 md:border-b-0 md:border-r">
        <div className="mb-6 px-2 text-xl font-extrabold text-primary">LeavePortal</div>
        <nav className="grid gap-1">
          {links.map(([label, href, Icon]) => <NavLink key={href} to={href} className={({ isActive }) => `flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium ${isActive ? 'bg-indigo-50 text-primary dark:bg-indigo-950' : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'}`}><Icon size={18} />{label}</NavLink>)}
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
