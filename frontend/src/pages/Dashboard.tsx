import { useQuery } from '@tanstack/react-query';
import { Cell, Pie, PieChart, ResponsiveContainer } from 'recharts';
import { dashboard, balances } from '../api/portal';
import { StatusBadge } from '../components/ui/StatusBadge';

export function Dashboard() {
  const { data } = useQuery({ queryKey: ['dashboard'], queryFn: () => dashboard('employee') });
  const { data: balanceData } = useQuery({ queryKey: ['balances'], queryFn: balances });
  return <div className="grid gap-5">
    <div className="grid gap-4 md:grid-cols-3">{balanceData?.map((b) => <div className="card" key={b.id}><div className="text-sm font-semibold text-slate-500">{b.leaveTypeCode}</div><div className="mt-2 flex items-center justify-between"><div><p className="text-3xl font-extrabold">{b.remaining}</p><p className="text-sm text-slate-500">of {b.totalAllocated + b.carryForward} days</p></div><ResponsiveContainer width={86} height={86}><PieChart><Pie data={[{ value: b.remaining }, { value: Math.max(0, b.totalAllocated - b.remaining) }]} dataKey="value" innerRadius={28} outerRadius={40}><Cell fill="#4F46E5" /><Cell fill="#E2E8F0" /></Pie></PieChart></ResponsiveContainer></div></div>)}</div>
    <section className="grid gap-4 lg:grid-cols-3"><div className="card lg:col-span-2"><h2 className="font-bold">Upcoming leaves</h2><div className="mt-3 grid gap-2">{data?.leaves.map((x) => <div className="flex items-center justify-between rounded-md border border-slate-200 p-3 dark:border-slate-800" key={x.id}><span>{x.leaveTypeCode} {x.startDate}</span><StatusBadge status={x.status} /></div>)}</div></div><div className="card"><h2 className="font-bold">Notifications</h2><div className="mt-3 grid gap-3">{data?.notifications.map((n) => <p className="text-sm" key={n.id}>{n.title}</p>)}</div></div></section>
  </div>;
}
