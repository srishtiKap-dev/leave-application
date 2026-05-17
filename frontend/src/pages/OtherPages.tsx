import { useQuery } from '@tanstack/react-query';
import { me, notifications } from '../api/portal';
import { useAuth } from '../stores/auth';

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

function ProfileField({ label, value }: { label: string; value?: string | null }) {
  return <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
    <p className="text-xs font-medium uppercase text-slate-500">{label}</p>
    <p className="mt-1 min-h-6 text-sm font-semibold text-slate-900">{value || '-'}</p>
  </div>;
}
