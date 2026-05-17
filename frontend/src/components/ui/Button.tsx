import { ButtonHTMLAttributes } from 'react';
import { twMerge } from 'tailwind-merge';

export function Button({ className, ...props }: ButtonHTMLAttributes<HTMLButtonElement>) {
  return <button className={twMerge('inline-flex items-center justify-center rounded-lg bg-primary px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:bg-primary-dark disabled:cursor-not-allowed disabled:opacity-50', className)} {...props} />;
}
