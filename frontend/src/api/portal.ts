import { api, unwrap } from './axios';
import type { AuthResult, CreateUser, Dashboard, ExpenseClaim, LeaveApplication, LeaveBalance, LeaveType, Notification, PagedResult, UpsertUser, User } from '../types';

export type ListParams = { search?: string; page?: number; pageSize?: number };

const cleanParams = (params?: ListParams) => ({
  search: params?.search || undefined,
  page: params?.page,
  pageSize: params?.pageSize
});

export const login = (email: string, password: string) => unwrap<AuthResult>(api.post('/auth/login', { email, password }));
export const changePassword = (currentPassword: string, newPassword: string) => unwrap<object>(api.post('/auth/change-password', { currentPassword, newPassword }));
export const me = () => unwrap<User>(api.get('/users/me'));
export const dashboard = (kind: 'employee' | 'manager' | 'hr') => unwrap<Dashboard>(api.get(`/dashboard/${kind}`));
export const balances = () => unwrap<LeaveBalance[]>(api.get('/leave-balance'));
export const leaveTypes = () => unwrap<LeaveType[]>(api.get('/leave-types'));
export const leaves = (scope = '/leave-applications', params?: ListParams) => unwrap<PagedResult<LeaveApplication>>(api.get(scope, { params: cleanParams(params) }));
export const applyLeave = (body: unknown) => unwrap<LeaveApplication>(api.post('/leave-applications', body));
export const approveLeave = (id: string, remarks?: string) => unwrap<LeaveApplication>(api.post(`/leave-approvals/${id}/approve`, { remarks }));
export const rejectLeave = (id: string, remarks?: string) => unwrap<LeaveApplication>(api.post(`/leave-approvals/${id}/reject`, { remarks }));
export const approveHrLeave = (id: string, remarks?: string) => unwrap<LeaveApplication>(api.post(`/hr/leave-applications/${id}/approve`, { remarks }));
export const rejectHrLeave = (id: string, remarks?: string) => unwrap<LeaveApplication>(api.post(`/hr/leave-applications/${id}/reject`, { remarks }));
export const withdrawLeave = (id: string) => unwrap<LeaveApplication>(api.delete(`/leave-applications/${id}`));
export const expenses = (scope = '/expense-claims', params?: ListParams) => unwrap<PagedResult<ExpenseClaim>>(api.get(scope, { params: cleanParams(params) }));
export const saveExpense = (body: unknown) => unwrap<ExpenseClaim>(api.post('/expense-claims', body));
export const submitExpense = (id: string) => unwrap<ExpenseClaim>(api.post(`/expense-claims/${id}/submit`));
export const withdrawExpense = (id: string) => unwrap<ExpenseClaim>(api.post(`/expense-claims/${id}/withdraw`));
export const approveExpense = (id: string, remarks?: string) => unwrap<ExpenseClaim>(api.post(`/expense-approvals/${id}/approve`, { remarks }));
export const rejectExpense = (id: string, remarks?: string) => unwrap<ExpenseClaim>(api.post(`/expense-approvals/${id}/reject`, { remarks }));
export const approveFinanceExpense = (id: string, remarks?: string) => unwrap<ExpenseClaim>(api.post(`/finance/expense-claims/${id}/approve`, { remarks }));
export const rejectFinanceExpense = (id: string, remarks?: string) => unwrap<ExpenseClaim>(api.post(`/finance/expense-claims/${id}/reject`, { remarks }));
export const markExpensePaid = (id: string) => unwrap<ExpenseClaim>(api.post(`/finance/expense-claims/${id}/mark-paid`));
export const notifications = () => unwrap<PagedResult<Notification>>(api.get('/notifications'));
export const users = (params?: ListParams) => unwrap<PagedResult<User>>(api.get('/users', { params: cleanParams(params) }));
export const managers = () => unwrap<User[]>(api.get('/users/managers'));
export const nextEmployeeId = () => unwrap<{ employeeId: string }>(api.get('/users/next-employee-id'));
export const createUser = (body: CreateUser) => unwrap<User>(api.post('/users', body));
export const updateUser = (id: string, body: UpsertUser) => unwrap<User>(api.put(`/users/${id}`, body));
export const deleteUser = (id: string) => unwrap<object>(api.delete(`/users/${id}`));
export const activateUser = (id: string) => unwrap<User>(api.post(`/users/${id}/activate`));
