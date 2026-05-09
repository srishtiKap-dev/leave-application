import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { useNavigate, Link } from 'react-router-dom';
import { login } from '../../api/portal';
import { useAuth } from '../../stores/auth';
import { Button } from '../../components/ui/Button';

const schema = z.object({ email: z.string().email(), password: z.string().min(8) });
type Form = z.infer<typeof schema>;

export function LoginPage() {
  const navigate = useNavigate();
  const setAuth = useAuth((s) => s.setAuth);
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<Form>({ resolver: zodResolver(schema), defaultValues: { email: 'admin@company.com', password: 'Admin@123!' } });
  return <section className="w-full max-w-md rounded-lg bg-white p-8 shadow-xl dark:bg-slate-900">
    <h1 className="text-2xl font-extrabold">Sign in</h1>
    <form className="mt-6 grid gap-4" onSubmit={handleSubmit(async (values) => { const auth = await login(values.email, values.password); setAuth(auth); toast.success('Welcome back'); navigate('/dashboard'); })}>
      <input className="rounded-md border border-slate-300 bg-transparent px-3 py-2 dark:border-slate-700" placeholder="Email" {...register('email')} />
      <input className="rounded-md border border-slate-300 bg-transparent px-3 py-2 dark:border-slate-700" type="password" placeholder="Password" {...register('password')} />
      <Button disabled={isSubmitting}>Login</Button>
    </form>
    <Link className="mt-4 block text-sm text-primary" to="/forgot-password">Forgot password?</Link>
  </section>;
}

export function SimpleAuthPage({ title }: { title: string }) {
  return <section className="w-full max-w-md rounded-lg bg-white p-8 shadow-xl dark:bg-slate-900"><h1 className="text-2xl font-extrabold">{title}</h1><p className="mt-3 text-sm text-slate-500">Enter your email to continue securely.</p><input className="mt-6 w-full rounded-md border border-slate-300 bg-transparent px-3 py-2 dark:border-slate-700" placeholder="Email" /><Button className="mt-4 w-full">Continue</Button></section>;
}
