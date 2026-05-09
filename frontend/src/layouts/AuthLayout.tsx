import { Outlet } from 'react-router-dom';

export function AuthLayout() {
  return <main className="grid min-h-screen place-items-center bg-slate-950 px-4"><Outlet /></main>;
}
