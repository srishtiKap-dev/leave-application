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
  const tones: Record<string, string> = {
    Pending: 'bg-amber-100 text-amber-700',
    ApprovedByManager: 'bg-blue-100 text-blue-700',
    ApprovedByHR: 'bg-emerald-100 text-emerald-700',
    ApprovedByFinance: 'bg-emerald-100 text-emerald-700',
    Rejected: 'bg-red-100 text-red-700',
    Cancelled: 'bg-slate-100 text-slate-600',
    Draft: 'bg-slate-100 text-slate-600',
    Submitted: 'bg-indigo-100 text-indigo-700',
    Paid: 'bg-emerald-100 text-emerald-700',
    Withdrawn: 'bg-slate-100 text-slate-600'
  };
  const tone = tones[label] ?? 'bg-slate-100 text-slate-600';
  return <span className={`badge ${tone}`}>{label}</span>;
}
