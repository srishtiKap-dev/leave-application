import { useQuery } from '@tanstack/react-query';
import type React from 'react';
import { ArrowRight, Calendar, CalendarPlus, ReceiptText } from 'lucide-react';
import { Link } from 'react-router-dom';
import { balances } from '../api/portal';

export function Dashboard() {
  const { data: balanceData } = useQuery({ queryKey: ['balances'], queryFn: balances });
  const orderedBalances = [...(balanceData ?? [])].sort((a, b) => leaveTypeOrder(a.leaveTypeCode) - leaveTypeOrder(b.leaveTypeCode));
  return <div className="mx-auto grid w-full max-w-7xl gap-5 sm:gap-6">
    <div className="mb-1 sm:mb-2"><h1 className="text-xl font-bold text-slate-900 sm:text-2xl">Employee Dashboard</h1><p className="mt-1 text-sm text-slate-500">Track your leave balances, recent claims, and approvals.</p></div>
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">{orderedBalances.map((b) => <BalanceCard key={b.id} code={b.leaveTypeCode} remaining={b.remaining} total={b.totalAllocated + b.carryForward} />)}</div>
    <div className="grid gap-4 md:grid-cols-2">
      <ActionCard to="/leaves/apply" icon={<CalendarPlus size={22} />} title="Apply for Leave" subtitle="Submit a new leave request" tone="indigo" />
      <ActionCard to="/expenses/new" icon={<ReceiptText size={22} />} title="New Expense Claim" subtitle="Submit a reimbursable expense" tone="emerald" />
    </div>
  </div>;
}

function BalanceCard({ code, remaining, total }: { code: string; remaining: number; total: number }) {
  const percent = total > 0 ? Math.max(0, Math.min(100, (remaining / total) * 100)) : 0;
  const bar = percent > 50 ? 'bg-indigo-500' : percent >= 20 ? 'bg-amber-500' : 'bg-red-500';
  const iconTone = code === 'SL' ? 'bg-amber-50 text-amber-600' : code === 'EL' ? 'bg-emerald-50 text-emerald-600' : 'bg-indigo-50 text-indigo-600';
  return <div className="card">
    <div className={`flex h-11 w-11 items-center justify-center rounded-full ${iconTone}`}><Calendar size={20} /></div>
    <p className="mt-5 text-sm font-medium text-slate-500">{code}</p>
    <p className="mt-1 text-3xl font-bold text-slate-900">{remaining}</p>
    <p className="mt-1 text-sm text-slate-400">of {total} days remaining</p>
    <div className="mt-5 h-1.5 overflow-hidden rounded-full bg-slate-100"><div className={`h-full ${bar}`} style={{ width: `${percent}%` }} /></div>
  </div>;
}

function ActionCard({ to, icon, title, subtitle, tone }: { to: string; icon: React.ReactNode; title: string; subtitle: string; tone: 'indigo' | 'emerald' }) {
  const iconClass = tone === 'emerald' ? 'bg-emerald-50 text-emerald-600' : 'bg-indigo-50 text-indigo-600';
  return <Link to={to} className="flex items-center gap-4 rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:border-indigo-200 hover:shadow-md"><div className={`rounded-lg p-3 ${iconClass}`}>{icon}</div><div className="flex-1"><p className="font-semibold text-slate-900">{title}</p><p className="text-sm text-slate-500">{subtitle}</p></div><ArrowRight className="text-slate-400" size={20} /></Link>;
}

function leaveTypeOrder(code: string) {
  return code === 'CL' ? 0 : code === 'SL' ? 1 : code === 'EL' ? 2 : 99;
}
