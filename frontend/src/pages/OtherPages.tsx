import { useMutation, useQuery } from '@tanstack/react-query';
import type React from 'react';
import { useState } from 'react';
import toast from 'react-hot-toast';
import { Eye, EyeOff } from 'lucide-react';
import { changePassword, me, notifications } from '../api/portal';
import { useAuth } from '../stores/auth';
import { Button } from '../components/ui/Button';

export function ProfilePage() {
  const authUser = useAuth((s) => s.user);
  const { data } = useQuery({ queryKey: ['me'], queryFn: me });
  const user = data ?? authUser;
  return <div className="card max-w-2xl">
    <div className="grid gap-4 sm:grid-cols-2">
      <ProfileField label="First name" value={user?.firstName} />
      <ProfileField label="Last name" value={user?.lastName} />
      <ProfileField label="Email" value={user?.email} />
      <ProfileField label="Role" value={user?.roles?.join(', ')} />
      <ProfileField label="Phone number" value={user?.phoneNumber} />
      <ProfileField label="Employee ID" value={user?.employeeId} />
      <ProfileField label="Department" value={user?.department} />
      <ProfileField label="Designation" value={user?.designation} />
    </div>
  </div>;
}

export function NotificationsPage() {
  const { data } = useQuery({ queryKey: ['notifications'], queryFn: notifications });
  return <div className="card"><div className="grid gap-3">{data?.items.map((n) => <div className={`rounded-md border p-3 dark:border-slate-800 ${n.isRead ? 'opacity-70' : ''}`} key={n.id}><strong>{n.title}</strong><p className="text-sm text-slate-500">{n.message}</p></div>)}</div></div>;
}

export function ChangePasswordPage() {
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const mutation = useMutation({
    mutationFn: () => changePassword(currentPassword, newPassword),
    onSuccess: () => {
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      setErrors({});
      toast.success('Password changed successfully');
    },
    onError: (error: any) => toast.error(error?.response?.data?.errors?.[0] ?? error?.response?.data?.message ?? 'Unable to change password')
  });

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    const nextErrors: Record<string, string> = {};
    if (!currentPassword) nextErrors.currentPassword = 'Please enter current password.';
    if (!isStrongPassword(newPassword)) nextErrors.newPassword = 'Password must be at least 8 characters and include uppercase, lowercase, number, and special character.';
    if (confirmPassword !== newPassword) nextErrors.confirmPassword = 'Confirm password must match new password.';
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length === 0) mutation.mutate();
  };

  return <form className="card grid max-w-xl gap-4" onSubmit={submit} noValidate>
    <PasswordField label="Current password" value={currentPassword} onChange={setCurrentPassword} show={showCurrent} setShow={setShowCurrent} error={errors.currentPassword} />
    <PasswordField label="New password" value={newPassword} onChange={setNewPassword} show={showNew} setShow={setShowNew} error={errors.newPassword} />
    <PasswordField label="Confirm new password" value={confirmPassword} onChange={setConfirmPassword} show={showNew} setShow={setShowNew} error={errors.confirmPassword} />
    <Button className="w-full sm:w-fit" disabled={mutation.isPending}>{mutation.isPending ? 'Changing...' : 'Change Password'}</Button>
  </form>;
}

function ProfileField({ label, value }: { label: string; value?: string | null }) {
  return <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
    <p className="text-xs font-medium uppercase text-slate-500">{label}</p>
    <p className="mt-1 min-h-6 text-sm font-semibold text-slate-900">{value || '-'}</p>
  </div>;
}

function PasswordField({ label, value, onChange, show, setShow, error }: { label: string; value: string; onChange: (value: string) => void; show: boolean; setShow: (value: boolean) => void; error?: string }) {
  return <label className="grid gap-1 text-sm">
    <span className="font-medium text-slate-700">{label}</span>
    <div className="relative">
      <input className={`input pr-10 ${error ? 'input-error' : ''}`} type={show ? 'text' : 'password'} value={value} onChange={(event) => onChange(event.target.value)} />
      <button type="button" className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400" onClick={() => setShow(!show)} aria-label={show ? 'Hide password' : 'Show password'}>{show ? <EyeOff size={18} /> : <Eye size={18} />}</button>
    </div>
    {error && <span className="text-xs font-medium text-red-600">{error}</span>}
  </label>;
}

function isStrongPassword(password: string) {
  return password.length >= 8 && /[A-Z]/.test(password) && /[a-z]/.test(password) && /[0-9]/.test(password) && /[^a-zA-Z0-9]/.test(password);
}
