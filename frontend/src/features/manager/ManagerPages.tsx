import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { approveExpense, approveLeave, dashboard, expenses, leaves, rejectExpense, rejectLeave } from '../../api/portal';
import { Button } from '../../components/ui/Button';
import { StatusBadge } from '../../components/ui/StatusBadge';

function useDebounced(value: string) {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => { const id = window.setTimeout(() => setDebounced(value), 300); return () => window.clearTimeout(id); }, [value]);
  return debounced;
}

export function ManagerDashboard() {
  const { data } = useQuery({ queryKey: ['manager-dashboard'], queryFn: () => dashboard('manager') });
  return <div className="grid gap-4 md:grid-cols-3">{data?.metrics.map((m) => <div className="card" key={m.label}><p className="text-sm text-slate-500">{m.label}</p><p className="text-3xl font-extrabold">{m.value}</p></div>)}</div>;
}

export function ManagerLeaves() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const debounced = useDebounced(search);
  const { data } = useQuery({ queryKey: ['manager-leaves', debounced], queryFn: () => leaves('/leave-approvals/pending', debounced) });
  const approve = useMutation({ mutationFn: (id: string) => approveLeave(id, 'Approved'), onSuccess: () => { toast.success('Approved'); qc.invalidateQueries({ queryKey: ['manager-leaves'] }); } });
  const reject = useMutation({ mutationFn: (id: string) => rejectLeave(id, 'Rejected'), onSuccess: () => { toast.success('Rejected'); qc.invalidateQueries({ queryKey: ['manager-leaves'] }); } });
  return <div className="card"><div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><h1 className="text-xl font-bold">Leave Approvals</h1><input className="rounded-md border p-2 text-sm dark:bg-slate-900" placeholder="Search leaves" value={search} onChange={(e) => setSearch(e.target.value)} /></div><div className="mt-4 grid gap-3">{data?.items.map((x) => <div className="grid gap-3 rounded-md border p-3 dark:border-slate-800 lg:grid-cols-[1.4fr_1fr_auto_auto_auto] lg:items-center" key={x.id}><span className="flex items-center gap-2">{x.employeeName}<LeaveTypeBadge code={x.leaveTypeCode} /></span><span className="text-sm text-slate-600 dark:text-slate-300">{formatDateRange(x.startDate, x.endDate)}</span><StatusBadge status={x.status} />{canManagerActOnLeave(x.status) ? <><Button onClick={() => approve.mutate(x.id)}>Approve</Button><Button type="button" className="bg-slate-600 hover:bg-slate-700" onClick={() => reject.mutate(x.id)}>Reject</Button></> : <span className="text-sm text-slate-500 lg:col-span-2">No actions available</span>}</div>)}</div></div>;
}

function LeaveTypeBadge({ code }: { code: string }) {
  return <span className="badge bg-indigo-100 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300">{code}</span>;
}

export function ManagerExpenses() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const debounced = useDebounced(search);
  const { data } = useQuery({ queryKey: ['manager-expenses', debounced], queryFn: () => expenses('/expense-approvals/pending', debounced) });
  const approve = useMutation({ mutationFn: (id: string) => approveExpense(id, 'Approved'), onSuccess: () => { toast.success('Approved'); qc.invalidateQueries({ queryKey: ['manager-expenses'] }); } });
  const reject = useMutation({ mutationFn: (id: string) => rejectExpense(id, 'Rejected'), onSuccess: () => { toast.success('Rejected'); qc.invalidateQueries({ queryKey: ['manager-expenses'] }); } });
  return <div className="card"><div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><h1 className="text-xl font-bold">Expense Approvals</h1><input className="rounded-md border p-2 text-sm dark:bg-slate-900" placeholder="Search expenses" value={search} onChange={(e) => setSearch(e.target.value)} /></div><div className="mt-4 grid gap-3">{data?.items.map((x) => <div className="grid gap-3 rounded-md border p-3 dark:border-slate-800 sm:grid-cols-[1fr_auto_auto_auto] sm:items-center" key={x.id}><span>{x.employeeName} - {x.title} - {x.currency} {x.totalAmount}</span><StatusBadge status={x.status} kind="expense" />{canManagerActOnExpense(x.status) ? <><Button onClick={() => approve.mutate(x.id)}>Approve</Button><Button type="button" className="bg-slate-600 hover:bg-slate-700" onClick={() => reject.mutate(x.id)}>Reject</Button></> : <span className="text-sm text-slate-500 sm:col-span-2">No actions available</span>}</div>)}</div></div>;
}

function formatDateRange(startDate: string, endDate: string) {
  return startDate === endDate ? startDate : `${startDate} to ${endDate}`;
}

function canManagerActOnLeave(status: string | number) {
  return status === 'Pending' || status === 1;
}

function canManagerActOnExpense(status: string | number) {
  return status === 'Submitted' || status === 1;
}
