export function StatusBadge({ status }: { status: string }) {
  const tone = status.includes('Approved') || status === 'Paid' ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300' : status.includes('Reject') ? 'bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300' : status.includes('Pending') || status === 'Submitted' ? 'bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300' : 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300';
  return <span className={`badge ${tone}`}>{status}</span>;
}
