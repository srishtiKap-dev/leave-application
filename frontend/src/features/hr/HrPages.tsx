import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type React from 'react';
import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { AlertCircle, Eye, EyeOff, X } from 'lucide-react';
import { Bar, BarChart, CartesianGrid, Legend, Line, LineChart, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { approveFinanceExpense, approveHrLeave, createUser, dashboard, deleteUser, expenses, leaves, managers, markExpensePaid, nextEmployeeId, rejectFinanceExpense, rejectHrLeave, updateUser, users } from '../../api/portal';
import { Button } from '../../components/ui/Button';
import { StatusBadge } from '../../components/ui/StatusBadge';
import type { CreateUser, Role, UpsertUser, User } from '../../types';

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
  return <Table title="" search={search} onSearch={setSearch} rows={data?.items.map((x) => [x.employeeName, <LeaveTypeBadge code={x.leaveTypeCode} />, formatDateRange(x.startDate, x.endDate), <StatusBadge status={x.status} />, canHrActOnLeave(x.status) ? <div className="flex gap-2"><Button onClick={() => approve.mutate(x.id)}>Approve</Button><Button className="bg-slate-600 hover:bg-slate-700" onClick={() => reject.mutate(x.id)}>Reject</Button></div> : <span className="text-sm text-slate-500">No actions available</span>]) ?? []} />;
}

function LeaveTypeBadge({ code }: { code: string }) {
  return <span className="badge bg-indigo-100 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300">{code}</span>;
}

export function HrExpenses() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const debounced = useDebounced(search);
  const { data } = useQuery({ queryKey: ['hr-expenses', debounced], queryFn: () => expenses('/finance/expense-claims', debounced) });
  const approve = useMutation({ mutationFn: (id: string) => approveFinanceExpense(id, 'Approved for payment'), onSuccess: () => { toast.success('Approved'); qc.invalidateQueries({ queryKey: ['hr-expenses'] }); } });
  const reject = useMutation({ mutationFn: (id: string) => rejectFinanceExpense(id, 'Rejected by finance'), onSuccess: () => { toast.success('Rejected'); qc.invalidateQueries({ queryKey: ['hr-expenses'] }); } });
  const paid = useMutation({ mutationFn: markExpensePaid, onSuccess: () => { toast.success('Marked paid'); qc.invalidateQueries({ queryKey: ['hr-expenses'] }); } });
  return <Table title="" search={search} onSearch={setSearch} rows={data?.items.map((x) => [x.employeeName, x.title, money.format(x.totalAmount), <StatusBadge status={x.status} kind="expense" />, <ExpenseActions status={x.status} onApprove={() => approve.mutate(x.id)} onReject={() => reject.mutate(x.id)} onPaid={() => paid.mutate(x.id)} />]) ?? []} />;
}

function ExpenseActions({ status, onApprove, onReject, onPaid }: { status: string | number; onApprove: () => void; onReject: () => void; onPaid: () => void }) {
  if (canHrMarkPaid(status)) return <Button className="bg-emerald-600 hover:bg-emerald-700" onClick={onPaid}>Paid</Button>;
  if (canHrActOnExpense(status)) return <div className="flex flex-wrap gap-2"><Button onClick={onApprove}>Approve</Button><Button className="bg-slate-600 hover:bg-slate-700" onClick={onReject}>Reject</Button></div>;
  return <span className="text-sm text-slate-500">No actions available</span>;
}

export function HrUsers() {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<User | null>(null);
  const [open, setOpen] = useState(false);
  const debounced = useDebounced(search);
  const { data } = useQuery({ queryKey: ['users', debounced], queryFn: () => users(debounced) });
  const save = useMutation({
    mutationFn: (body: CreateUser | UpsertUser) => editing ? updateUser(editing.id, body) : createUser(body as CreateUser),
    onSuccess: (_user, body) => {
      const message = editing ? 'Employee saved' : `${body.firstName} ${body.lastName} has been added. They can now log in with the email ${body.email} and the password you set.`;
      toast.success(message, { duration: 6000 });
      setOpen(false);
      setEditing(null);
      qc.invalidateQueries({ queryKey: ['users'] });
    }
  });
  const remove = useMutation({ mutationFn: deleteUser, onSuccess: () => { toast.success('Employee deactivated'); qc.invalidateQueries({ queryKey: ['users'] }); } });
  return <div className="grid gap-4">
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><h1 className="text-xl font-bold">User Management</h1><div className="flex gap-2"><input className="rounded-md border p-2 text-sm dark:bg-slate-900" placeholder="Search employees" value={search} onChange={(e) => setSearch(e.target.value)} /><Button onClick={() => { setEditing(null); setOpen(true); }}>Create Employee</Button></div></div>
    <Table title="" rows={data?.items.map((x) => [x.employeeId, `${x.firstName} ${x.lastName}`, x.email, x.department, x.designation, <div className="flex gap-2"><Button onClick={() => { setEditing(x); setOpen(true); }}>Edit</Button><Button className="bg-slate-600 hover:bg-slate-700" onClick={() => remove.mutate(x.id)}>Deactivate</Button></div>]) ?? []} />
    {open && <UserDrawer user={editing} onClose={() => { setOpen(false); setEditing(null); }} onSave={(body) => save.mutate(body)} />}
  </div>;
}

export function Reports() {
  const data = [{ name: 'Engineering', leave: 42 }, { name: 'Sales', leave: 28 }, { name: 'Finance', leave: 18 }];
  return <div className="grid gap-4 lg:grid-cols-2"><div className="card h-80"><ResponsiveContainer><LineChart data={data}><XAxis dataKey="name" /><YAxis /><Tooltip /><Line dataKey="leave" stroke="#10B981" /></LineChart></ResponsiveContainer></div><div className="card h-80"><ResponsiveContainer><PieChart><Pie data={data} dataKey="leave" nameKey="name" fill="#F59E0B" /></PieChart></ResponsiveContainer></div></div>;
}

type UserForm = UpsertUser & { password: string; confirmPassword: string };
type UserFormErrors = Partial<Record<'firstName' | 'lastName' | 'email' | 'department' | 'designation' | 'managerId' | 'password' | 'confirmPassword', string>>;

function UserDrawer({ user, onClose, onSave }: { user: User | null; onClose: () => void; onSave: (body: CreateUser | UpsertUser) => void }) {
  const { data: managerOptions } = useQuery({ queryKey: ['managers'], queryFn: managers });
  const { data: nextId } = useQuery({ queryKey: ['next-employee-id'], queryFn: nextEmployeeId, enabled: !user });
  const [form, setForm] = useState<UserForm>({
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
    isActive: user?.isActive ?? true,
    password: '',
    confirmPassword: ''
  });
  const [errors, setErrors] = useState<UserFormErrors>({});
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const set = (key: keyof UserForm, value: string | boolean | null) => setForm((current) => ({ ...current, [key]: value }));
  useEffect(() => {
    if (!user && nextId?.employeeId) set('employeeId', nextId.employeeId);
  }, [nextId?.employeeId, user]);
  const strength = getPasswordStrength(form.password);
  const submit = () => {
    const nextErrors: UserFormErrors = {};
    if (!form.firstName.trim()) nextErrors.firstName = 'First name is required.';
    if (!form.lastName.trim()) nextErrors.lastName = 'Last name is required.';
    if (!/^\S+@\S+\.\S+$/.test(form.email)) nextErrors.email = 'Enter a valid email address.';
    if (!form.department.trim()) nextErrors.department = 'Department is required.';
    if (!form.designation.trim()) nextErrors.designation = 'Designation is required.';
    if (form.role === 'Employee' && !form.managerId) nextErrors.managerId = 'Select a manager so the employee can apply for leave.';
    if (!user) {
      if (!form.password) nextErrors.password = 'Password is required.';
      else if (!isStrongPassword(form.password)) nextErrors.password = 'Password must be at least 8 characters and include uppercase, lowercase, number, and special character.';
      if (form.confirmPassword !== form.password) nextErrors.confirmPassword = 'Confirm password must match password.';
    }
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;
    const { confirmPassword: _confirmPassword, ...payload } = form;
    if (user) {
      const { password: _password, ...updatePayload } = payload;
      onSave(updatePayload);
    } else {
      onSave(payload);
    }
  };
  return <div className="fixed inset-0 z-40 bg-black/40 backdrop-blur-sm"><aside className="fixed right-0 top-0 z-50 flex h-full w-full max-w-full flex-col bg-white shadow-2xl transition-transform duration-300 dark:bg-slate-900 sm:max-w-lg">
    <div className="flex items-center justify-between border-b border-slate-200 px-6 py-5 dark:border-slate-800"><h2 className="text-lg font-semibold text-slate-900 dark:text-white">{user ? 'Edit Employee' : 'Create Employee'}</h2><button className="rounded-full p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800" onClick={onClose} aria-label="Close"><X size={18} /></button></div>
    <div className="grid flex-1 gap-4 overflow-y-auto px-6 py-6">
      <Field label="Employee ID"><input className="input cursor-not-allowed bg-slate-100 text-slate-500" placeholder="Auto Employee ID" value={form.employeeId || 'Auto-generating...'} disabled readOnly /></Field>
      <div className="grid gap-4 sm:grid-cols-2"><Field label="First name *" error={errors.firstName}><input className={`input ${errors.firstName ? 'input-error' : ''}`} value={form.firstName} onChange={(e) => set('firstName', e.target.value)} /></Field><Field label="Last name *" error={errors.lastName}><input className={`input ${errors.lastName ? 'input-error' : ''}`} value={form.lastName} onChange={(e) => set('lastName', e.target.value)} /></Field></div>
      <Field label="Email *" error={errors.email}><input className={`input ${errors.email ? 'input-error' : ''}`} value={form.email} onChange={(e) => set('email', e.target.value)} /></Field>
      <div className="grid gap-4 sm:grid-cols-2"><Field label="Department *" error={errors.department}><input className={`input ${errors.department ? 'input-error' : ''}`} value={form.department} onChange={(e) => set('department', e.target.value)} /></Field><Field label="Designation *" error={errors.designation}><input className={`input ${errors.designation ? 'input-error' : ''}`} value={form.designation} onChange={(e) => set('designation', e.target.value)} /></Field></div>
      <div className="grid gap-4 sm:grid-cols-2"><Field label="Role"><select className="input" value={form.role} onChange={(e) => set('role', e.target.value as Role)}>{roles.map((role) => <option key={role}>{role}</option>)}</select></Field><Field label="Date of joining"><input className="input" type="date" value={form.dateOfJoining} onChange={(e) => set('dateOfJoining', e.target.value)} /></Field></div>
      <Field label="Manager" error={errors.managerId}><select className={`input ${errors.managerId ? 'input-error' : ''}`} value={form.managerId ?? ''} onChange={(e) => set('managerId', e.target.value || null)}><option value="">No manager</option>{managerOptions?.map((m) => <option key={m.id} value={m.id}>{m.firstName} {m.lastName}</option>)}</select></Field>
      {!user && <>
        <Field label="Password *" error={errors.password}>
          <div className="relative"><input className={`input pr-10 ${errors.password ? 'input-error' : ''}`} type={showPassword ? 'text' : 'password'} placeholder="Minimum 8 characters" value={form.password} onChange={(e) => set('password', e.target.value)} /><button type="button" className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400" onClick={() => setShowPassword((value) => !value)}>{showPassword ? <EyeOff size={18} /> : <Eye size={18} />}</button></div>
          <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-100"><div className={`h-full ${strength.color} ${strength.width} transition-all duration-200`} /></div>
          <p className="mt-1 text-xs font-medium text-slate-500">{strength.label}</p>
        </Field>
        <Field label="Confirm Password *" error={errors.confirmPassword}>
          <div className="relative"><input className={`input pr-10 ${errors.confirmPassword ? 'input-error' : ''}`} type={showConfirmPassword ? 'text' : 'password'} value={form.confirmPassword} onChange={(e) => set('confirmPassword', e.target.value)} /><button type="button" className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400" onClick={() => setShowConfirmPassword((value) => !value)}>{showConfirmPassword ? <EyeOff size={18} /> : <Eye size={18} />}</button></div>
        </Field>
      </>}
      <label className="flex items-center gap-2 text-sm text-slate-700"><input type="checkbox" checked={form.isActive} onChange={(e) => set('isActive', e.target.checked)} /> Active</label>
    </div>
    <div className="flex justify-end gap-3 border-t border-slate-200 bg-white px-6 py-4 dark:border-slate-800 dark:bg-slate-900"><Button className="bg-white text-slate-700 ring-1 ring-slate-300 hover:bg-slate-50" onClick={onClose}>Cancel</Button><Button onClick={submit}>Save Employee</Button></div>
  </aside></div>;
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return <label className="grid gap-1 text-sm"><span className="font-medium text-slate-700 dark:text-slate-200">{label}</span>{children}{error && <span className="mt-1 flex items-center gap-1 text-xs text-red-500"><AlertCircle size={12} />{error}</span>}</label>;
}

function getPasswordStrength(password: string) {
  const checks = {
    length: password.length >= 8,
    upper: /[A-Z]/.test(password),
    lower: /[a-z]/.test(password),
    number: /[0-9]/.test(password),
    special: /[^a-zA-Z0-9]/.test(password)
  };
  const score = Object.values(checks).filter(Boolean).length;
  if (score <= 2) return { label: 'Weak', color: 'bg-red-500', width: 'w-1/4' };
  if (score <= 4) return { label: 'Medium', color: 'bg-amber-500', width: 'w-2/4' };
  return { label: 'Strong', color: 'bg-green-500', width: 'w-full' };
}

function isStrongPassword(password: string) {
  return password.length >= 8 && /[A-Z]/.test(password) && /[a-z]/.test(password) && /[0-9]/.test(password) && /[^a-zA-Z0-9]/.test(password);
}

function Table({ title, rows, search, onSearch }: { title: string; rows: React.ReactNode[][]; search?: string; onSearch?: (value: string) => void }) {
  return <div className="card overflow-x-auto">{(title || onSearch) && <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">{title && <h1 className="text-xl font-bold">{title}</h1>}{onSearch && <input className="rounded-md border p-2 text-sm dark:bg-slate-900 sm:ml-auto" placeholder={title ? `Search ${title.toLowerCase()}` : 'Search'} value={search} onChange={(e) => onSearch(e.target.value)} />}</div>}<table className="w-full text-sm"><tbody>{rows.map((r, i) => <tr className="border-t border-slate-200 dark:border-slate-800" key={i}>{r.map((c, j) => <td className="p-2" key={j}>{c}</td>)}</tr>)}</tbody></table></div>;
}

function formatDateRange(startDate: string, endDate: string) {
  return startDate === endDate ? startDate : `${startDate} to ${endDate}`;
}

function canHrActOnLeave(status: string | number) {
  return status === 'Pending' || status === 'ApprovedByManager' || status === 1 || status === 2;
}

function canHrActOnExpense(status: string | number) {
  return status === 'Submitted' || status === 'ApprovedByManager' || status === 1 || status === 2;
}

function canHrMarkPaid(status: string | number) {
  return status === 'ApprovedByFinance' || status === 3;
}
