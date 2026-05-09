import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { applyLeave, leaveTypes, leaves } from '../../api/portal';
import { Button } from '../../components/ui/Button';
import { StatusBadge } from '../../components/ui/StatusBadge';

export function ApplyLeavePage() {
  const qc = useQueryClient();
  const { data: types } = useQuery({ queryKey: ['leave-types'], queryFn: leaveTypes });
  const { register, handleSubmit } = useForm();
  const mutation = useMutation({ mutationFn: applyLeave, onSuccess: () => { toast.success('Leave submitted'); qc.invalidateQueries({ queryKey: ['leaves'] }); } });
  return <form className="card grid max-w-2xl gap-4" onSubmit={handleSubmit((v) => mutation.mutate({ ...v, isHalfDay: Boolean(v.isHalfDay), saveAsDraft: false }))}>
    <h1 className="text-xl font-bold">Apply for Leave</h1>
    <select className="rounded-md border p-2 dark:bg-slate-900" {...register('leaveTypeId')}>{types?.map((t) => <option key={t.id} value={t.id}>{t.code} - {t.name}</option>)}</select>
    <div className="grid gap-3 sm:grid-cols-2"><input className="rounded-md border p-2 dark:bg-slate-900" type="date" {...register('startDate')} /><input className="rounded-md border p-2 dark:bg-slate-900" type="date" {...register('endDate')} /></div>
    <label className="flex items-center gap-2 text-sm"><input type="checkbox" {...register('isHalfDay')} /> Half day</label>
    <textarea className="min-h-28 rounded-md border p-2 dark:bg-slate-900" placeholder="Reason" {...register('reason')} />
    <Button>Submit</Button>
  </form>;
}

export function MyLeavesPage() {
  const { data, isLoading } = useQuery({ queryKey: ['leaves'], queryFn: () => leaves() });
  if (isLoading) return <div className="card">Loading leaves...</div>;
  return <div className="card overflow-x-auto"><div className="mb-4 flex items-center justify-between"><h1 className="text-xl font-bold">My Leaves</h1><a className="text-primary" href="/leaves/apply">Apply Leave</a></div><table className="w-full text-sm"><thead><tr className="text-left text-slate-500"><th className="p-2">Number</th><th>Type</th><th>Dates</th><th>Days</th><th>Status</th></tr></thead><tbody>{data?.items.map((x) => <tr className="border-t border-slate-200 dark:border-slate-800" key={x.id}><td className="p-2">{x.applicationNumber}</td><td>{x.leaveTypeCode}</td><td>{x.startDate} to {x.endDate}</td><td>{x.totalDays}</td><td><StatusBadge status={x.status} /></td></tr>)}</tbody></table></div>;
}

export function LeaveCalendarPage() {
  const { data } = useQuery({ queryKey: ['calendar'], queryFn: () => leaves() });
  return <div className="card"><h1 className="text-xl font-bold">Leave Calendar</h1><div className="mt-4 grid gap-2 md:grid-cols-2">{data?.items.map((x) => <div className="rounded-md bg-indigo-50 p-3 text-sm dark:bg-indigo-950" key={x.id}>{x.employeeName}: {x.startDate} to {x.endDate}</div>)}</div></div>;
}
