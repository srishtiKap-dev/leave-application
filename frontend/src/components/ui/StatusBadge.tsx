const leaveStatus: Record<number, string> = {
  0: 'Draft',
  1: 'Pending',
  2: 'ApprovedByManager',
  3: 'ApprovedByHR',
  4: 'Rejected',
  5: 'Cancelled',
  6: 'Withdrawn'
};

const expenseStatus: Record<number, string> = {
  0: 'Draft',
  1: 'Submitted',
  2: 'ApprovedByManager',
  3: 'ApprovedByFinance',
  4: 'Rejected',
  5: 'Paid',
  6: 'Withdrawn'
};

export function StatusBadge({ status, kind = 'leave' }: { status: string | number; kind?: 'leave' | 'expense' }) {
  const label = typeof status === 'number' ? (kind === 'expense' ? expenseStatus[status] : leaveStatus[status]) ?? String(status) : status;
  const tone = label.includes('Approved') || label === 'Paid' ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300' : label.includes('Reject') ? 'bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300' : label.includes('Pending') || label === 'Submitted' ? 'bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300' : 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300';
  return <span className={`badge ${tone}`}>{label}</span>;
}
