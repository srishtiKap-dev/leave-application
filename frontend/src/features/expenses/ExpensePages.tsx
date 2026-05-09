import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { expenses, saveExpense } from '../../api/portal';
import { Button } from '../../components/ui/Button';
import { StatusBadge } from '../../components/ui/StatusBadge';

export function ExpenseListPage() {
  const { data } = useQuery({ queryKey: ['expenses'], queryFn: () => expenses() });
  return <div className="card overflow-x-auto"><div className="mb-4 flex justify-between"><h1 className="text-xl font-bold">My Expense Claims</h1><a className="text-primary" href="/expenses/new">Create new claim</a></div><table className="w-full text-sm"><tbody>{data?.items.map((x) => <tr className="border-t border-slate-200 dark:border-slate-800" key={x.id}><td className="p-2">{x.claimNumber}</td><td>{x.title}</td><td>{x.currency} {x.totalAmount}</td><td><StatusBadge status={x.status} kind="expense" /></td></tr>)}</tbody></table></div>;
}

export function ExpenseFormPage() {
  const qc = useQueryClient();
  const { register, handleSubmit } = useForm({ defaultValues: { title: '', description: '', currency: 'INR' } });
  const mutation = useMutation({ mutationFn: saveExpense, onSuccess: () => { toast.success('Expense saved'); qc.invalidateQueries({ queryKey: ['expenses'] }); } });
  return <form className="card grid max-w-2xl gap-4" onSubmit={handleSubmit((v) => mutation.mutate(v))}><h1 className="text-xl font-bold">New Expense Claim</h1><input className="rounded-md border p-2 dark:bg-slate-900" placeholder="Claim title" {...register('title')} /><textarea className="rounded-md border p-2 dark:bg-slate-900" placeholder="Description" {...register('description')} /><input className="rounded-md border p-2 dark:bg-slate-900" {...register('currency')} /><div className="rounded-md border border-dashed border-slate-300 p-8 text-center text-sm text-slate-500 dark:border-slate-700">Drop receipts here</div><Button>Save Draft</Button></form>;
}

export function ExpenseDetailPage() {
  return <div className="card"><h1 className="text-xl font-bold">Expense Claim Detail</h1><p className="mt-3 text-sm text-slate-500">Claim summary, line items, approval timeline, and remarks appear here.</p></div>;
}
