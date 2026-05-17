import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { useNavigate, Link } from 'react-router-dom';
import { Building2, CheckCircle2, Eye, EyeOff, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { login } from '../../api/portal';
import { dashboardFor, useAuth } from '../../stores/auth';
import { Button } from '../../components/ui/Button';

const schema = z.object({
  email: z.string().trim().min(1, 'Please enter username').email('Please enter a valid email address'),
  password: z.string().min(1, 'Please enter password')
});
type Form = z.infer<typeof schema>;

export function LoginPage() {
  const navigate = useNavigate();
  const setAuth = useAuth((s) => s.setAuth);
  const [showPassword, setShowPassword] = useState(false);
  const [formError, setFormError] = useState('');
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<Form>({ resolver: zodResolver(schema), defaultValues: { email: '', password: '' } });
  const onSubmit = handleSubmit(async (values) => {
    setFormError('');
    try {
      const auth = await login(values.email, values.password);
      setAuth(auth);
      toast.success('Welcome back');
      navigate(dashboardFor(auth.user));
    } catch (error: any) {
      const message = error?.response?.status === 401
        ? 'Invalid username or password.'
        : error?.response?.data?.errors?.[0] ?? error?.response?.data?.message ?? 'Unable to sign in. Please try again.';
      setFormError(message);
      toast.error(message);
    }
  });
  return <section className="grid min-h-screen w-full bg-white lg:grid-cols-[3fr_2fr]">
    <div className="hidden bg-gradient-to-br from-indigo-600 to-indigo-800 px-16 py-12 text-white lg:flex lg:flex-col lg:justify-between">
      <div>
        <Building2 size={64} />
        <h1 className="mt-8 text-4xl font-bold">LeavePortal</h1>
        <p className="mt-3 text-lg text-indigo-200">Manage leaves and expenses with ease</p>
        <div className="mt-12 grid gap-5 text-sm font-medium">
          {['Apply and track leaves in real-time', 'Submit and manage expense claims', 'Seamless HR and manager approvals'].map((item) => <div className="flex items-center gap-3" key={item}><CheckCircle2 size={20} />{item}</div>)}
        </div>
      </div>
      <p className="text-sm text-indigo-300">LeavePortal v1.0</p>
    </div>
    <div className="flex min-h-screen items-center justify-center px-6 py-10">
      <div className="w-full max-w-md">
        <div className="mb-8 flex h-10 w-10 items-center justify-center rounded-xl bg-primary text-white"><Building2 size={22} /></div>
        <h2 className="text-2xl font-bold text-slate-900">Welcome back</h2>
        <p className="mt-1 text-sm text-slate-500">Sign in to your account</p>
        <form className="mt-8 grid gap-5" onSubmit={onSubmit} noValidate>
          <label className="grid gap-1 text-sm">
            <span className="font-medium text-slate-700">Email address</span>
            <input className={`input ${errors.email ? 'input-error' : ''}`} placeholder="you@company.com" aria-invalid={Boolean(errors.email)} {...register('email')} />
            {errors.email && <span className="text-xs font-medium text-red-600">{errors.email.message}</span>}
          </label>
          <label className="grid gap-1 text-sm">
            <span className="font-medium text-slate-700">Password</span>
            <div className="relative"><input className={`input pr-10 ${errors.password ? 'input-error' : ''}`} type={showPassword ? 'text' : 'password'} placeholder="Enter your password" aria-invalid={Boolean(errors.password)} {...register('password')} /><button type="button" className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400" onClick={() => setShowPassword((value) => !value)} aria-label={showPassword ? 'Hide password' : 'Show password'}>{showPassword ? <EyeOff size={18} /> : <Eye size={18} />}</button></div>
            {errors.password && <span className="text-xs font-medium text-red-600">{errors.password.message}</span>}
          </label>
          {formError && <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{formError}</div>}
          <Link className="justify-self-end text-sm font-medium text-primary hover:text-primary-dark" to="/forgot-password">Forgot password?</Link>
          <Button className="w-full" disabled={isSubmitting}>{isSubmitting ? <Loader2 className="animate-spin" size={18} /> : 'Sign In'}</Button>
        </form>
        <p className="mt-8 text-center text-sm text-slate-500">Having trouble? Contact your HR administrator</p>
      </div>
    </div>
  </section>;
}

export function SimpleAuthPage({ title }: { title: string }) {
  return <section className="w-full max-w-md rounded-lg bg-white p-8 shadow-xl dark:bg-slate-900"><h1 className="text-2xl font-extrabold">{title}</h1><p className="mt-3 text-sm text-slate-500">Enter your email to continue securely.</p><input className="mt-6 w-full rounded-md border border-slate-300 bg-transparent px-3 py-2 dark:border-slate-700" placeholder="Email" /><Button className="mt-4 w-full">Continue</Button></section>;
}
