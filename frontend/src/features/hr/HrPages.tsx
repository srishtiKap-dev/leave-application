import { useQuery } from '@tanstack/react-query';
import type React from 'react';
import { Bar, BarChart, CartesianGrid, Legend, Line, LineChart, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { dashboard, leaves, expenses, users } from '../../api/portal';
import { StatusBadge } from '../../components/ui/StatusBadge';

export function HrDashboard() {
  const { data } = useQuery({ queryKey: ['hr-dashboard'], queryFn: () => dashboard('hr') });
  const chart = data?.metrics.map((m) => ({ name: m.label, value: m.value })) ?? [];
  return <div className="grid gap-4"><div className="grid gap-4 md:grid-cols-3">{data?.metrics.map((m) => <div className="card" key={m.label}><p className="text-sm text-slate-500">{m.label}</p><p className="text-3xl font-extrabold">{m.value}</p></div>)}</div><div className="card h-80"><ResponsiveContainer><BarChart data={chart}><CartesianGrid strokeDasharray="3 3" /><XAxis dataKey="name" /><YAxis /><Tooltip /><Legend /><Bar dataKey="value" fill="#4F46E5" /></BarChart></ResponsiveContainer></div></div>;
}

export function HrLeaves() {
  const { data } = useQuery({ queryKey: ['hr-leaves'], queryFn: () => leaves('/hr/leave-applications') });
  return <Table title="All Leave Applications" rows={data?.items.map((x) => [x.employeeName, x.leaveTypeCode, x.startDate, <StatusBadge status={x.status} />]) ?? []} />;
}

export function HrExpenses() {
  const { data } = useQuery({ queryKey: ['hr-expenses'], queryFn: () => expenses('/finance/expense-claims') });
  return <Table title="All Expense Claims" rows={data?.items.map((x) => [x.employeeName, x.title, `${x.currency} ${x.totalAmount}`, <StatusBadge status={x.status} />]) ?? []} />;
}

export function HrUsers() {
  const { data } = useQuery({ queryKey: ['users'], queryFn: users });
  return <Table title="User Management" rows={data?.items.map((x) => [x.employeeId, `${x.firstName} ${x.lastName}`, x.department, x.designation]) ?? []} />;
}

export function Reports() {
  const data = [{ name: 'Engineering', leave: 42 }, { name: 'Sales', leave: 28 }, { name: 'Finance', leave: 18 }];
  return <div className="grid gap-4 lg:grid-cols-2"><div className="card h-80"><ResponsiveContainer><LineChart data={data}><XAxis dataKey="name" /><YAxis /><Tooltip /><Line dataKey="leave" stroke="#10B981" /></LineChart></ResponsiveContainer></div><div className="card h-80"><ResponsiveContainer><PieChart><Pie data={data} dataKey="leave" nameKey="name" fill="#F59E0B" /></PieChart></ResponsiveContainer></div></div>;
}

function Table({ title, rows }: { title: string; rows: React.ReactNode[][] }) {
  return <div className="card overflow-x-auto"><h1 className="mb-4 text-xl font-bold">{title}</h1><table className="w-full text-sm"><tbody>{rows.map((r, i) => <tr className="border-t border-slate-200 dark:border-slate-800" key={i}>{r.map((c, j) => <td className="p-2" key={j}>{c}</td>)}</tr>)}</tbody></table></div>;
}
