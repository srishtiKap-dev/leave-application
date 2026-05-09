import axios from 'axios';
import { useAuth } from '../stores/auth';
import type { ApiResponse, AuthResult } from '../types';

export const api = axios.create({ baseURL: import.meta.env.VITE_API_URL ?? 'https://localhost:7109/api/v1' });

api.interceptors.request.use((config) => {
  const token = useAuth.getState().accessToken ?? sessionStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(undefined, async (error) => {
  const original = error.config;
  if (error.response?.status === 401 && !original._retry) {
    original._retry = true;
    const refreshToken = useAuth.getState().refreshToken ?? localStorage.getItem('refreshToken');
    if (refreshToken) {
      const { data } = await axios.post<ApiResponse<AuthResult>>(`${api.defaults.baseURL}/auth/refresh`, { refreshToken });
      useAuth.getState().setAuth(data.data);
      original.headers.Authorization = `Bearer ${data.data.accessToken}`;
      return api(original);
    }
  }
  return Promise.reject(error);
});

export const unwrap = async <T>(promise: Promise<{ data: ApiResponse<T> }>) => (await promise).data.data;
