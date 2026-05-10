import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type React from 'react';
import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { Bar, BarChart, CartesianGrid, Legend, Line, LineChart, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { approveFinanceExpense, approveHrLeave, createUser, dashboard, expenses, leaves, managers, markExpensePaid, rejectFinanceExpense, rejectHrLeave, updateUser, users } from '../../api/portal';
import { Button } from '../../components/ui/Button';
import { StatusBadge } from '../../components/ui/StatusBadge';
import type { Role, UpsertUser, User } from '../../types';

const money = new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' });
const roles: Role[] = ['Employee', 'Manager', 'HRAdmin'];

function useDebounced(value: string) {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => { const id = window.setTimeout(() => setDebounced(value), 300); return () => window.clearTimeout(id); }, [value]);
  return debounced;
}

export function HrDashboard() {
  const { data } = useQuery({ queryKey: ['hr-dashboard'], queryFn: () => dashboard('hr') });
  const chart = data?.metrics.map((m) => ({ name: m.label, value: m.value })) ?? [];
  return <div className="grid gap-4"><div className="grid gap-4 md:grid-cols-3">{data?.metrics.map((m) => <div className="card" key={m.label}><p className="text-sm text-slate-500">{m.label}</p><p className="text-3xl font-extrabold">{m.value}</p></div>)}</div><div className="card h-80"><ResponsiveContainer><BarChart data={chart}><CartesianGrid strokeDasharray="3 3" /><XAxis dataKey="name" /><YAxis /><Tooltip /><Legend /><Bar dataKey="value" fill="#4F46E5" /></BarChart></ResponsiveContainer></div></div>;
}

export function HrLeaves() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const debounced = useDebounced(search);
  const { data } = useQuery({ queryKey: ['hr-leaves', debounced], queryFn: () => leaves('/hr/leave-applications', debounced) });
  const approve = useMutation({ mutationFn: (id: string) => approveHrLeave(id, 'Approved by HR'), onSuccess: () => { toast.success('Approved'); qc.invalidateQueries({ queryKey: ['hr-leaves'] }); } });
  const reject = useMutation({ mutationFn: (id: string) => rejectHrLeave(id, 'Rejected by HR'), onSuccess: () => { toast.success('Rejected'); qc.invalidateQueries({ queryKey: ['hr-leaves'] }); } });
  return <Table title="All Leave Applications" search={search} onSearch={setSearch} rows={data?.items.map((x) => [x.employeeName, x.leaveTypeCode, x.startDate, <StatusBadge status={x.status} />, <div className="flex gap-2"><Button onClick={() => approve.mutate(x.id)}>Approve</Button><Button className="bg-slate-600 hover:bg-slate-700" onClick={() => reject.mutate(x.id)}>Reject</Button></div>]) ?? []} />;
}

export function HrExpenses() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const debounced = useDebounced(search);
  const { data } = useQuery({ queryKey: ['hr-expenses', debounced], queryFn: () => expenses('/finance/expense-claims', debounced) });
  const approve = useMutation({ mutationFn: (id: string) => approveFinanceExpense(id, 'Approved for payment'), onSuccess: () => { toast.success('Approved'); qc.invalidateQueries({ queryKey: ['hr-expenses'] }); } });
  const reject = useMutation({ mutationFn: (id: string) => rejectFinanceExpense(id, 'Rejected by finance'), onSuccess: () => { toast.success('Rejected'); qc.invalidateQueries({ queryKey: ['hr-expenses'] }); } });
  const paid = useMutation({ mutationFn: markExpensePaid, onSuccess: () => { toast.success('Marked paid'); qc.invalidateQueries({ queryKey: ['hr-expenses'] }); } });
  return <Table title="All Expense Claims" search={search} onSearch={setSearch} rows={data?.items.map((x) => [x.employeeName, x.title, money.format(x.totalAmount), <StatusBadge status={x.status} kind="expense" />, <div className="flex flex-wrap gap-2"><Button onClick={() => approve.mutate(x.id)}>Approve</Button><Button className="bg-emerald-600 hover:bg-emerald-700" onClick={() => paid.mutate(x.id)}>Paid</Button><Button className="bg-slate-600 hover:bg-slate-700" onClick={() => reject.mutate(x.id)}>Reject</Button></div>]) ?? []} />;
}

export function HrUsers() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<User | null>(null);
  const [open, setOpen] = useState(false);
  const debounced = useDebounced(search);
  const { data } = useQuery({ queryKey: ['users', debounced], queryFn: () => users(debounced) });
  const save = useMutation({ mutationFn: (body: UpsertUser) => editing ? updateUser(editing.id, body) : createUser(body), onSuccess: () => { toast.success('Employee saved'); setOpen(false); setEditing(null); qc.invalidateQueries({ queryKey: ['users'] }); } });
  return <div className="grid gap-4">
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><h1 className="text-xl font-bold">User Management</h1><div className="flex gap-2"><input className="rounded-md border p-2 text-sm dark:bg-slate-900" placeholder="Search employees" value={search} onChange={(e) => setSearch(e.target.value)} /><Button onClick={() => { setEditing(null); setOpen(true); }}>Create Employee</Button></div></div>
    <Table title="" rows={data?.items.map((x) => [x.employeeId, `${x.firstName} ${x.lastName}`, x.email, x.department, x.designation, <Button onClick={() => { setEditing(x); setOpen(true); }}>Edit</Button>]) ?? []} />
    {open && <UserDrawer user={editing} onClose={() => { setOpen(false); setEditing(null); }} onSave={(body) => save.mutate(body)} />}
  </div>;
}

export function Reports() {
  const data = [{ name: 'Engineering', leave: 42 }, { name: 'Sales', leave: 28 }, { name: 'Finance', leave: 18 }];
  return <div className="grid gap-4 lg:grid-cols-2"><div className="card h-80"><ResponsiveContainer><LineChart data={data}><XAxis dataKey="name" /><YAxis /><Tooltip /><Line dataKey="leave" stroke="#10B981" /></LineChart></ResponsiveContainer></div><div className="card h-80"><ResponsiveContainer><PieChart><Pie data={data} dataKey="leave" nameKey="name" fill="#F59E0B" /></PieChart></ResponsiveContainer></div></div>;
}

function UserDrawer({ user, onClose, onSave }: { user: User | null; onClose: () => void; onSave: (body: UpsertUser) => void }) {
  const { data: managerOptions } = useQuery({ queryKey: ['managers'], queryFn: managers });
  const [form, setForm] = useState<UpsertUser>({
    employeeId: user?.employeeId ?? '',
    firstName: user?.firstName ?? '',
    lastName: user?.lastName ?? '',
    email: user?.email ?? '',
    phoneNumber: user?.phoneNumber ?? '',
    department: user?.department ?? '',
    designation: user?.designation ?? '',
    dateOfJoining: user?.dateOfJoining?.slice(0, 10) ?? new Date().toISOString().slice(0, 10),
    managerId: user?.managerId ?? null,
    role: user?.roles?.[0] ?? 'Employee',
    isActive: user?.isActive ?? true
  });
  const set = (key: keyof UpsertUser, value: string | boolean | null) => setForm((current) => ({ ...current, [key]: value }));
  return <div className="fixed inset-0 z-40 bg-slate-950/40"><aside className="ml-auto grid h-full w-full max-w-xl gap-3 overflow-y-auto bg-white p-6 shadow-xl dark:bg-slate-900">
    <div className="flex items-center justify-between"><h2 className="text-lg font-bold">{user ? 'Edit Employee' : 'Create Employee'}</h2><button className="text-sm text-slate-500" onClick={onClose}>Close</button></div>
    <input className="rounded-md border p-2 dark:bg-slate-900" placeholder="Auto Employee ID" value={form.employeeId} onChange={(e) => set('employeeId', e.target.value)} />
    <div className="grid gap-3 sm:grid-cols-2"><input className="rounded-md border p-2 dark:bg-slate-900" placeholder="First name" value={form.firstName} onChange={(e) => set('firstName', e.target.value)} /><input className="rounded-md border p-2 dark:bg-slate-900" placeholder="Last name" value={form.lastName} onChange={(e) => set('lastName', e.target.value)} /></div>
    <input className="rounded-md border p-2 dark:bg-slate-900" placeholder="Email" value={form.email} onChange={(e) => set('email', e.target.value)} />
    <div className="grid gap-3 sm:grid-cols-2"><input className="rounded-md border p-2 dark:bg-slate-900" placeholder="Department" value={form.department} onChange={(e) => set('department', e.target.value)} /><input className="rounded-md border p-2 dark:bg-slate-900" placeholder="Designation" value={form.designation} onChange={(e) => set('designation', e.target.value)} /></div>
    <div className="grid gap-3 sm:grid-cols-2"><select className="rounded-md border p-2 dark:bg-slate-900" value={form.role} onChange={(e) => set('role', e.target.value as Role)}>{roles.map((role) => <option key={role}>{role}</option>)}</select><input className="rounded-md border p-2 dark:bg-slate-900" type="date" value={form.dateOfJoining} onChange={(e) => set('dateOfJoining', e.target.value)} /></div>
    <select className="rounded-md border p-2 dark:bg-slate-900" value={form.managerId ?? ''} onChange={(e) => set('managerId', e.target.value || null)}><option value="">No manager</option>{managerOptions?.map((m) => <option key={m.id} value={m.id}>{m.firstName} {m.lastName}</option>)}</select>
    <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isActive} onChange={(e) => set('isActive', e.target.checked)} /> Active</label>
    <Button onClick={() => onSave(form)}>Save</Button>
  </aside></div>;
}

function Table({ title, rows, search, onSearch }: { title: string; rows: React.ReactNode[][]; search?: string; onSearch?: (value: string) => void }) {
  return <div className="card overflow-x-auto">{title && <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><h1 className="text-xl font-bold">{title}</h1>{onSearch && <input className="rounded-md border p-2 text-sm dark:bg-slate-900" placeholder={`Search ${title.toLowerCase()}`} value={search} onChange={(e) => onSearch(e.target.value)} />}</div>}<table className="w-full text-sm"><tbody>{rows.map((r, i) => <tr className="border-t border-slate-200 dark:border-slate-800" key={i}>{r.map((c, j) => <td className="p-2" key={j}>{c}</td>)}</tr>)}</tbody></table></div>;
}
