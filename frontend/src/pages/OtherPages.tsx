import { useQuery } from '@tanstack/react-query';
import { notifications } from '../api/portal';

export function ProfilePage() {
  return <div className="card max-w-2xl"><h1 className="text-xl font-bold">Profile</h1><div className="mt-4 grid gap-3"><input className="rounded-md border p-2 dark:bg-slate-900" placeholder="First name" /><input className="rounded-md border p-2 dark:bg-slate-900" placeholder="Last name" /><input className="rounded-md border p-2 dark:bg-slate-900" placeholder="Phone" /></div></div>;
}

export function NotificationsPage() {
  const { data } = useQuery({ queryKey: ['notifications'], queryFn: notifications });
  return <div className="card"><h1 className="text-xl font-bold">Notifications</h1><div className="mt-4 grid gap-3">{data?.items.map((n) => <div className={`rounded-md border p-3 dark:border-slate-800 ${n.isRead ? 'opacity-70' : ''}`} key={n.id}><strong>{n.title}</strong><p className="text-sm text-slate-500">{n.message}</p></div>)}</div></div>;
}
