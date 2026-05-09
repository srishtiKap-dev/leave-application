import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { approveLeave, dashboard, leaves, expenses } from '../../api/portal';
import { Button } from '../../components/ui/Button';
import { StatusBadge } from '../../components/ui/StatusBadge';

export function ManagerDashboard() {
  const { data } = useQuery({ queryKey: ['manager-dashboard'], queryFn: () => dashboard('manager') });
  return <div className="grid gap-4 md:grid-cols-3">{data?.metrics.map((m) => <div className="card" key={m.label}><p className="text-sm text-slate-500">{m.label}</p><p className="text-3xl font-extrabold">{m.value}</p></div>)}</div>;
}

export function ManagerLeaves() {
  const qc = useQueryClient();
  const { data } = useQuery({ queryKey: ['manager-leaves'], queryFn: () => leaves('/leave-approvals/pending') });
  const approve = useMutation({ mutationFn: (id: string) => approveLeave(id, 'Approved'), onSuccess: () => { toast.success('Approved'); qc.invalidateQueries({ queryKey: ['manager-leaves'] }); } });
  return <div className="card"><h1 className="text-xl font-bold">Leave Approvals</h1><div className="mt-4 grid gap-3">{data?.items.map((x) => <div className="flex items-center justify-between rounded-md border p-3 dark:border-slate-800" key={x.id}><span>{x.employeeName} {x.leaveTypeCode}</span><StatusBadge status={x.status} /><Button onClick={() => approve.mutate(x.id)}>Approve</Button></div>)}</div></div>;
}

export function ManagerExpenses() {
  const { data } = useQuery({ queryKey: ['manager-expenses'], queryFn: () => expenses('/expense-approvals/pending') });
  return <div className="card"><h1 className="text-xl font-bold">Expense Approvals</h1><div className="mt-4 grid gap-3">{data?.items.map((x) => <div className="rounded-md border p-3 dark:border-slate-800" key={x.id}>{x.employeeName} - {x.title} - {x.currency} {x.totalAmount}</div>)}</div></div>;
}
