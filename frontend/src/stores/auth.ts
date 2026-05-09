import { create } from 'zustand';
import type { AuthResult, User } from '../types';

type AuthState = { user?: User; accessToken?: string; refreshToken?: string; setAuth: (auth: AuthResult) => void; logout: () => void };

export const useAuth = create<AuthState>((set) => ({
  user: JSON.parse(localStorage.getItem('user') || 'null') || undefined,
  accessToken: sessionStorage.getItem('accessToken') || undefined,
  refreshToken: localStorage.getItem('refreshToken') || undefined,
  setAuth: (auth) => {
    sessionStorage.setItem('accessToken', auth.accessToken);
    localStorage.setItem('refreshToken', auth.refreshToken);
    localStorage.setItem('user', JSON.stringify(auth.user));
    set({ user: auth.user, accessToken: auth.accessToken, refreshToken: auth.refreshToken });
  },
  logout: () => {
    sessionStorage.clear();
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    set({ user: undefined, accessToken: undefined, refreshToken: undefined });
  }
}));
