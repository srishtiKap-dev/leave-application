import { Outlet } from 'react-router-dom';

export function AuthLayout() {
  return <main className="min-h-screen bg-white"><Outlet /></main>;
}
