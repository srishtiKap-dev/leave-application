import { Bell, Building2, CalendarDays, CreditCard, Key, LayoutDashboard, LogOut, Menu, Search, User as UserIcon, Users, X } from 'lucide-react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useMemo, useState } from 'react';
import { useAuth } from '../stores/auth';
import type { Role } from '../types';

const links = [
  ['Dashboard', '/dashboard', LayoutDashboard, ['Employee', 'Manager', 'HRAdmin', 'SuperAdmin']],
  ['My Leaves', '/leaves', CalendarDays, ['Employee', 'Manager', 'HRAdmin', 'SuperAdmin']],
  ['My Expenses', '/expenses', CreditCard, ['Employee', 'Manager', 'HRAdmin', 'SuperAdmin']],
  ['Manager Dashboard', '/manager/dashboard', LayoutDashboard, ['Manager', 'SuperAdmin']],
  ['Leave Approvals', '/manager/leaves', CalendarDays, ['Manager', 'SuperAdmin']],
  ['Expense Approvals', '/manager/expenses', CreditCard, ['Manager', 'SuperAdmin']],
  ['HR Dashboard', '/hr/dashboard', LayoutDashboard, ['HRAdmin', 'SuperAdmin']],
  ['HR Leaves', '/hr/leaves', CalendarDays, ['HRAdmin', 'SuperAdmin']],
  ['HR Expenses', '/hr/expenses', CreditCard, ['HRAdmin', 'SuperAdmin']],
  ['Employees', '/hr/users', Users, ['HRAdmin', 'SuperAdmin']],
  ['Notifications', '/notifications', Bell, ['Employee', 'Manager', 'HRAdmin', 'SuperAdmin']]
] as const;

const pageTitles: Record<string, string> = {
  '/profile': 'My Profile',
  '/change-password': 'Change Password',
  '/notifications': 'Notifications',
  '/leaves/apply': 'Apply for Leave',
  '/expenses/new': 'New Expense Claim'
};

export function AppLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [userOpen, setUserOpen] = useState(false);
  const userRoles = user?.roles ?? [];
  const visibleLinks = links.filter(([, , , roles]) => roles.some((role) => userRoles.includes(role as Role)));
  const title = useMemo(() => visibleLinks.find(([, href]) => location.pathname === href)?.[0] ?? pageTitles[location.pathname] ?? 'Dashboard', [location.pathname, visibleLinks]);
  const initials = `${user?.firstName?.[0] ?? 'U'}${user?.lastName?.[0] ?? ''}`.toUpperCase();
  const fullName = `${user?.firstName ?? ''} ${user?.lastName ?? ''}`.trim() || 'User';
  const roleLabel = userRoles[0] ?? 'Employee';
  const signOut = () => { logout(); navigate('/login'); };
  return (
    <div className="min-h-screen bg-slate-50 md:flex">
      {mobileOpen && <button aria-label="Close sidebar overlay" className="fixed inset-0 z-30 bg-black/40 md:hidden" onClick={() => setMobileOpen(false)} />}
      <aside className={`fixed inset-y-0 left-0 z-40 flex max-w-[86vw] bg-white shadow-lg transition-all duration-300 md:static md:max-w-none md:shadow-none ${collapsed ? 'w-[72px]' : 'w-[260px]'} ${mobileOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'}`}>
        <div className="flex w-full flex-col border-r border-slate-200">
          <div className="flex h-16 items-center gap-3 px-4">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary text-white"><Building2 size={20} /></div>
            {!collapsed && <div><p className="text-sm font-bold text-slate-900">LeavePortal</p><p className="text-xs text-slate-500">Company Portal</p></div>}
            <button className="ml-auto hidden rounded-lg p-2 text-slate-400 hover:bg-slate-50 hover:text-slate-900 md:block" onClick={() => setCollapsed((value) => !value)} aria-label="Toggle sidebar">{collapsed ? <Menu size={18} /> : <X size={18} />}</button>
          </div>
          <div className="border-t border-slate-100" />
          <nav className="flex flex-1 flex-col gap-1 py-4">
            {visibleLinks.map(([label, href, Icon]) => <NavLink key={href} to={href} title={collapsed ? label : undefined} onClick={() => setMobileOpen(false)} className={({ isActive }) => `mx-2 flex items-center gap-3 rounded-lg border-l-2 px-3 py-2.5 text-sm transition-all duration-200 ${isActive ? 'border-indigo-600 bg-indigo-50 font-medium text-indigo-700' : 'border-transparent text-slate-600 hover:bg-slate-50 hover:text-slate-900'}`}><Icon className="shrink-0" size={18} />{!collapsed && <span>{label}</span>}</NavLink>)}
          </nav>
          <div className="border-t border-slate-100 p-3">
            <div className="flex items-center gap-3">
              <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-indigo-100 text-sm font-semibold text-indigo-700">{initials}</div>
              {!collapsed && <><div className="min-w-0 flex-1"><p className="truncate text-sm font-medium text-slate-900">{fullName}</p><p className="truncate text-xs text-slate-500">{roleLabel}</p></div><button className="rounded-lg p-2 text-slate-500 hover:bg-red-50 hover:text-red-500" onClick={signOut} aria-label="Logout"><LogOut size={18} /></button></>}
            </div>
          </div>
        </div>
      </aside>
      <main className="min-w-0 flex-1">
        <header className="sticky top-0 z-20 flex min-h-16 items-center justify-between gap-2 border-b border-slate-200 bg-white px-3 py-3 sm:px-4 md:px-6">
          <div className="flex min-w-0 items-center gap-2 sm:gap-3"><button className="rounded-lg p-2 text-slate-500 hover:bg-slate-50 md:hidden" onClick={() => setMobileOpen(true)} aria-label="Open sidebar"><Menu size={20} /></button><h1 className="truncate text-lg font-semibold text-slate-900 sm:text-xl">{title}</h1></div>
          <div className="relative flex shrink-0 items-center gap-1 sm:gap-3">
            <div className="flex items-center">{searchOpen && <input aria-label="Global search" autoFocus placeholder="Search" className="input h-9 w-32 py-1.5 sm:w-44 md:w-64" />}<button aria-label="Open search" className="rounded-lg p-2 text-slate-500 hover:bg-slate-50" onClick={() => setSearchOpen((value) => !value)}><Search size={20} /></button></div>
            <button aria-label="Notifications" className="relative rounded-lg p-2 text-slate-500 hover:bg-slate-50"><Bell size={20} /><span className="absolute right-1.5 top-1.5 h-2.5 w-2.5 rounded-full bg-red-500" /></button>
            <button className="flex h-9 w-9 items-center justify-center rounded-full bg-indigo-100 text-sm font-semibold text-indigo-700" onClick={() => setUserOpen((value) => !value)}>{initials}</button>
            {userOpen && <div className="absolute right-0 top-12 w-64 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-lg">
              <div className="px-4 py-3"><p className="text-sm font-semibold text-slate-900">{fullName}</p><p className="truncate text-xs text-slate-500">{user?.email}</p></div>
              <div className="border-t border-slate-100" />
              <button className="flex w-full items-center gap-2 px-4 py-2 text-sm text-slate-600 hover:bg-slate-50" onClick={() => { setUserOpen(false); navigate('/profile'); }}><UserIcon size={16} />My Profile</button>
              <button className="flex w-full items-center gap-2 px-4 py-2 text-sm text-slate-600 hover:bg-slate-50" onClick={() => { setUserOpen(false); navigate('/change-password'); }}><Key size={16} />Change Password</button>
              <div className="border-t border-slate-100" />
              <button className="flex w-full items-center gap-2 px-4 py-2 text-sm text-slate-600 hover:bg-red-50 hover:text-red-600" onClick={signOut}><LogOut size={16} />Sign Out</button>
            </div>}
          </div>
        </header>
        <div className="animate-fadeIn px-3 py-4 sm:px-4 sm:py-6 md:px-6 md:py-8"><Outlet /></div>
      </main>
    </div>
  );
}
