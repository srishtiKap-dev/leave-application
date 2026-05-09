import { api, unwrap } from './client';
import type { AuthResult, Dashboard, ExpenseClaim, LeaveApplication, LeaveBalance, LeaveType, Notification, PagedResult, User } from '../types';

export const login = (email: string, password: string) => unwrap<AuthResult>(api.post('/auth/login', { email, password }));
export const me = () => unwrap<User>(api.get('/users/me'));
export const dashboard = (kind: 'employee' | 'manager' | 'hr') => unwrap<Dashboard>(api.get(`/dashboard/${kind}`));
export const balances = () => unwrap<LeaveBalance[]>(api.get('/leave-balance'));
export const leaveTypes = () => unwrap<LeaveType[]>(api.get('/leave-types'));
export const leaves = (scope = '/leave-applications') => unwrap<PagedResult<LeaveApplication>>(api.get(scope));
export const applyLeave = (body: unknown) => unwrap<LeaveApplication>(api.post('/leave-applications', body));
export const approveLeave = (id: string, remarks?: string) => unwrap<LeaveApplication>(api.post(`/leave-approvals/${id}/approve`, { remarks }));
export const expenses = (scope = '/expense-claims') => unwrap<PagedResult<ExpenseClaim>>(api.get(scope));
export const saveExpense = (body: unknown) => unwrap<ExpenseClaim>(api.post('/expense-claims', body));
export const notifications = () => unwrap<PagedResult<Notification>>(api.get('/notifications'));
export const users = () => unwrap<PagedResult<User>>(api.get('/users?pageSize=100'));
