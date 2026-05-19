import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { FileText, UploadCloud } from 'lucide-react';
import { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { useNavigate } from 'react-router-dom';
import { expenses, saveExpense, submitExpense, withdrawExpense } from '../../api/portal';
import { Button } from '../../components/ui/Button';
import { StatusBadge } from '../../components/ui/StatusBadge';

const money = new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' });

type ExpenseForm = { title: string; description: string; currency: string; amount: number };

export function ExpenseListPage() {
  const qc = useQueryClient();
  const { data } = useQuery({ queryKey: ['expenses'], queryFn: () => expenses() });
  const withdraw = useMutation({ mutationFn: withdrawExpense, onSuccess: () => { toast.success('Expense withdrawn'); qc.invalidateQueries({ queryKey: ['expenses'] }); } });
  return <div className="card overflow-x-auto"><div className="mb-4 flex justify-end"><a className="text-primary" href="/expenses/new">Create new claim</a></div><table className="w-full min-w-[640px] text-sm"><thead><tr className="text-left text-slate-500"><th className="p-2">Number</th><th>Title</th><th>Amount</th><th>Status</th><th>Action</th></tr></thead><tbody>{data?.items.map((x) => <tr className="border-t border-slate-200 dark:border-slate-800" key={x.id}><td className="p-2">{x.claimNumber}</td><td>{x.title}</td><td>{money.format(x.totalAmount)}</td><td><StatusBadge status={x.status} kind="expense" /></td><td>{isSubmitted(x.status) && <Button className="h-8 bg-slate-600 px-3 hover:bg-slate-700" onClick={() => withdraw.mutate(x.id)}>Withdraw</Button>}</td></tr>)}</tbody></table></div>;
}

export function ExpenseFormPage() {
  const qc = useQueryClient();
  const navigate = useNavigate();
  const [receipt, setReceipt] = useState<File | null>(null);
  const [receiptError, setReceiptError] = useState('');
  const onDrop = useCallback((accepted: File[]) => { setReceiptError(''); setReceipt(accepted[0] ?? null); }, []);
  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    multiple: false,
    maxSize: 5 * 1024 * 1024,
    accept: { 'application/pdf': ['.pdf'] },
    onDropRejected: (rejections) => setReceiptError(rejections[0]?.errors[0]?.code === 'file-too-large' ? 'PDF must be 5MB or smaller.' : 'Only PDF receipts are allowed.')
  });
  const { register, handleSubmit, formState: { errors } } = useForm<ExpenseForm>({ defaultValues: { title: '', description: '', currency: 'INR', amount: 0 } });
  const mutation = useMutation({
    mutationFn: async (body: ExpenseForm) => {
      const claim = await saveExpense({ ...body, amount: Number(body.amount) });
      return submitExpense(claim.id);
    },
    onSuccess: () => { toast.success('Expense submitted'); qc.invalidateQueries({ queryKey: ['expenses'] }); navigate('/expenses'); },
    onError: (error: any) => toast.error(error?.response?.data?.errors?.[0] ?? error?.response?.data?.message ?? 'Unable to submit expense')
  });

  return <form className="card grid max-w-2xl gap-4" onSubmit={handleSubmit((v) => mutation.mutate(v))}>
    <button type="button" className="w-fit text-sm font-medium text-primary" onClick={() => navigate(-1)}>Back</button>
    <h1 className="text-xl font-bold">New Expense Claim</h1>
    <input className="rounded-md border p-2 dark:bg-slate-900" placeholder="Claim title" {...register('title', { required: 'Title is required' })} />
    {errors.title && <span className="text-xs text-red-600">{errors.title.message}</span>}
    <textarea className="rounded-md border p-2 dark:bg-slate-900" placeholder="Description" {...register('description')} />
    <input className="rounded-md border p-2 dark:bg-slate-900" type="number" min="1" step="0.01" placeholder="Amount" {...register('amount', { required: 'Amount is required', min: { value: 1, message: 'Amount must be greater than zero' }, valueAsNumber: true })} />
    {errors.amount && <span className="text-xs text-red-600">{errors.amount.message}</span>}
    <input className="rounded-md border p-2 dark:bg-slate-900" {...register('currency')} />
    <div {...getRootProps()} className={`grid cursor-pointer place-items-center rounded-md border border-dashed p-8 text-center text-sm transition ${isDragActive ? 'border-primary bg-indigo-50 text-primary dark:bg-indigo-950' : 'border-slate-300 text-slate-500 dark:border-slate-700'}`}>
      <input {...getInputProps()} />
      <UploadCloud className="mb-2" size={28} />
      <p className="font-medium">{receipt ? receipt.name : 'Drop a PDF receipt here, or browse'}</p>
      <p className="mt-1 text-xs">PDF only, up to 5MB</p>
    </div>
    {receipt && <div className="flex items-center gap-2 rounded-md bg-slate-100 p-2 text-sm dark:bg-slate-800"><FileText size={16} />{receipt.name}</div>}
    {receiptError && <p className="text-sm text-red-600">{receiptError}</p>}
    <Button disabled={mutation.isPending}>{mutation.isPending ? 'Submitting...' : 'Submit Expense'}</Button>
  </form>;
}

export function ExpenseDetailPage() {
  return <div className="card"><h1 className="text-xl font-bold">Expense Claim Detail</h1><p className="mt-3 text-sm text-slate-500">Claim summary, line items, approval timeline, and remarks appear here.</p></div>;
}

function isSubmitted(status: string | number) {
  return status === 'Submitted' || status === 1;
}
