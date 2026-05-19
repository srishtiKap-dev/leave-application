type PaginationProps = {
  page: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
};

const pageSizes = [10, 20, 50];

export function Pagination({ page, pageSize, totalPages, totalCount, onPageChange, onPageSizeChange }: PaginationProps) {
  const safeTotalPages = Math.max(1, totalPages || 1);
  return <div className="flex flex-col gap-3 border-t border-slate-200 pt-4 text-sm text-slate-600 dark:border-slate-800 sm:flex-row sm:items-center sm:justify-between">
    <div>{totalCount === 0 ? 'No records' : `Page ${page} of ${safeTotalPages} • ${totalCount} records`}</div>
    <div className="flex items-center gap-2">
      <select className="rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900" value={pageSize} onChange={(event) => onPageSizeChange(Number(event.target.value))} aria-label="Rows per page">
        {pageSizes.map((size) => <option key={size} value={size}>{size}</option>)}
      </select>
      <button className="rounded-md border border-slate-300 px-3 py-1.5 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>Previous</button>
      <button className="rounded-md border border-slate-300 px-3 py-1.5 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700" onClick={() => onPageChange(page + 1)} disabled={page >= safeTotalPages}>Next</button>
    </div>
  </div>;
}
