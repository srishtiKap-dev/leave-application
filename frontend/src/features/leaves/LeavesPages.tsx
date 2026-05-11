import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { DayPicker, type DateRange } from 'react-day-picker';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { useNavigate } from 'react-router-dom';
import { applyLeave, leaveTypes, leaves, withdrawLeave } from '../../api/portal';
import { Button } from '../../components/ui/Button';
import { StatusBadge } from '../../components/ui/StatusBadge';

type LeaveForm = { leaveTypeId: string; reason: string; isHalfDay: boolean };

function dateOnly(date: Date) {
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function ApplyLeavePage() {
  const qc = useQueryClient();
  const navigate = useNavigate();
  const [range, setRange] = useState<DateRange | undefined>();
  const [dateError, setDateError] = useState('');
  const { data: types } = useQuery({ queryKey: ['leave-types'], queryFn: leaveTypes });
  const { register, handleSubmit, formState: { errors } } = useForm<LeaveForm>({ defaultValues: { leaveTypeId: '', reason: '', isHalfDay: false } });
  const mutation = useMutation({
    mutationFn: applyLeave,
    onSuccess: () => { toast.success('Leave submitted'); qc.invalidateQueries({ queryKey: ['leaves'] }); navigate('/leaves'); },
    onError: (error: any) => toast.error(error?.response?.data?.errors?.[0] ?? error?.response?.data?.message ?? 'Unable to submit leave')
  });

  return <form className="card grid max-w-2xl gap-4" onSubmit={handleSubmit((v) => {
    if (!range?.from || !range?.to) { setDateError('Select a start and end date.'); return; }
    setDateError('');
    mutation.mutate({ ...v, startDate: dateOnly(range.from), endDate: dateOnly(range.to), isHalfDay: Boolean(v.isHalfDay), halfDayType: null, saveAsDraft: false });
  })}>
    <button type="button" className="w-fit text-sm font-medium text-primary" onClick={() => navigate(-1)}>Back</button>
    <h1 className="text-xl font-bold">Apply for Leave</h1>
    <label className="grid gap-1 text-sm">
      <span className="font-medium">Leave type</span>
      <select className="rounded-md border p-2 dark:bg-slate-900" {...register('leaveTypeId', { required: 'Select a leave type' })}>
        <option value="">Select leave type</option>
        {types?.map((t) => <option key={t.id} value={t.id}>{t.code} - {t.name}</option>)}
      </select>
      {errors.leaveTypeId && <span className="text-xs text-red-600">{errors.leaveTypeId.message}</span>}
    </label>
    <div className="rounded-md border border-slate-200 p-3 dark:border-slate-800">
      <div className="mb-2 text-sm font-medium">Date range</div>
      <DayPicker
        mode="range"
        selected={range}
        onSelect={setRange}
        disabled={{ dayOfWeek: [0, 6] }}
        showOutsideDays
        classNames={{
          root: 'w-full',
          month: 'space-y-3',
          month_caption: 'flex items-center justify-center py-2 text-sm font-semibold',
          nav: 'flex items-center justify-between',
          button_previous: 'rounded-md border border-slate-300 px-3 py-2 text-slate-700 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800',
          button_next: 'rounded-md border border-slate-300 px-3 py-2 text-slate-700 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800',
          chevron: 'fill-primary',
          weekdays: 'grid grid-cols-7 text-center text-xs font-semibold text-slate-500',
          week: 'grid grid-cols-7',
          day: 'grid place-items-center p-1',
          day_button: 'h-9 w-9 rounded-md text-sm hover:bg-indigo-50 hover:text-primary dark:hover:bg-indigo-950',
          today: 'rounded-md ring-2 ring-primary ring-offset-2 dark:ring-offset-slate-900',
          selected: 'rounded-md bg-primary text-white hover:bg-primary hover:text-white',
          range_middle: 'rounded-none bg-indigo-50 text-primary dark:bg-indigo-950',
          disabled: 'text-slate-300 opacity-60 dark:text-slate-700'
        }}
      />
      <div className="mt-2 text-sm text-slate-500">{range?.from ? dateOnly(range.from) : 'Start date'} to {range?.to ? dateOnly(range.to) : 'End date'}</div>
      {dateError && <span className="text-xs text-red-600">{dateError}</span>}
    </div>
    <label className="flex items-center gap-2 text-sm"><input type="checkbox" {...register('isHalfDay')} /> Half day</label>
    <textarea className="min-h-28 rounded-md border p-2 dark:bg-slate-900" placeholder="Reason" {...register('reason', { required: 'Reason is required' })} />
    {errors.reason && <span className="text-xs text-red-600">{errors.reason.message}</span>}
    <Button disabled={mutation.isPending}>{mutation.isPending ? 'Submitting...' : 'Submit'}</Button>
  </form>;
}

export function MyLeavesPage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ['leaves'], queryFn: () => leaves() });
  const withdraw = useMutation({ mutationFn: withdrawLeave, onSuccess: () => { toast.success('Leave withdrawn'); qc.invalidateQueries({ queryKey: ['leaves'] }); } });
  if (isLoading) return <div className="card">Loading leaves...</div>;
  return <div className="card overflow-x-auto"><div className="mb-4 flex items-center justify-between"><h1 className="text-xl font-bold">My Leaves</h1><a className="text-primary" href="/leaves/apply">Apply Leave</a></div><table className="w-full text-sm"><thead><tr className="text-left text-slate-500"><th className="p-2">Number</th><th>Type</th><th>Dates</th><th>Days</th><th>Status</th><th>Action</th></tr></thead><tbody>{data?.items.map((x) => <tr className="border-t border-slate-200 dark:border-slate-800" key={x.id}><td className="p-2">{x.applicationNumber}</td><td><LeaveTypeBadge code={x.leaveTypeCode} /></td><td>{x.startDate} to {x.endDate}</td><td>{x.totalDays}</td><td><StatusBadge status={x.status} /></td><td>{isPending(x.status) && <Button className="h-8 bg-slate-600 px-3 hover:bg-slate-700" onClick={() => withdraw.mutate(x.id)}>Withdraw</Button>}</td></tr>)}</tbody></table></div>;
}

export function LeaveCalendarPage() {
  const { data } = useQuery({ queryKey: ['calendar'], queryFn: () => leaves() });
  return <div className="card"><h1 className="text-xl font-bold">Leave Calendar</h1><div className="mt-4 grid gap-2 md:grid-cols-2">{data?.items.map((x) => <div className="rounded-md bg-indigo-50 p-3 text-sm dark:bg-indigo-950" key={x.id}>{x.employeeName}: {x.startDate} to {x.endDate}</div>)}</div></div>;
}

function isPending(status: string | number) {
  return status === 'Pending' || status === 1;
}

function LeaveTypeBadge({ code }: { code: string }) {
  return <span className="badge bg-indigo-100 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300">{code}</span>;
}
