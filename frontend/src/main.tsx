import React from 'react';
import ReactDOM from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom';
import './index.css';
import { AppLayout } from './layouts/AppLayout';
import { AuthLayout } from './layouts/AuthLayout';
import { ProtectedRoute } from './router/ProtectedRoute';
import { RoleRoute } from './router/RoleRoute';
import { LoginPage, SimpleAuthPage } from './features/auth/LoginPage';
import { Dashboard } from './pages/Dashboard';
import { ApplyLeavePage, LeaveCalendarPage, MyLeavesPage } from './features/leaves/LeavesPages';
import { ExpenseDetailPage, ExpenseFormPage, ExpenseListPage } from './features/expenses/ExpensePages';
import { ManagerDashboard, ManagerExpenses, ManagerLeaves } from './features/manager/ManagerPages';
import { HrDashboard, HrExpenses, HrLeaves, HrUsers, Reports } from './features/hr/HrPages';
import { NotificationsPage, ProfilePage } from './pages/OtherPages';

const queryClient = new QueryClient();
const router = createBrowserRouter([
  { element: <AuthLayout />, children: [{ path: '/login', element: <LoginPage /> }, { path: '/forgot-password', element: <SimpleAuthPage title="Forgot Password" /> }, { path: '/reset-password', element: <SimpleAuthPage title="Reset Password" /> }] },
  { element: <ProtectedRoute />, children: [{ element: <AppLayout />, children: [
    { path: '/', element: <Navigate to="/dashboard" replace /> },
    { path: '/dashboard', element: <Dashboard /> },
    { path: '/leaves', element: <MyLeavesPage /> }, { path: '/leaves/apply', element: <ApplyLeavePage /> }, { path: '/leaves/calendar', element: <LeaveCalendarPage /> },
    { path: '/expenses', element: <ExpenseListPage /> }, { path: '/expenses/new', element: <ExpenseFormPage /> }, { path: '/expenses/:id', element: <ExpenseDetailPage /> }, { path: '/expenses/:id/edit', element: <ExpenseFormPage /> },
    { path: '/profile', element: <ProfilePage /> }, { path: '/notifications', element: <NotificationsPage /> },
    { element: <RoleRoute roles={['Manager', 'SuperAdmin']} />, children: [
      { path: '/manager/dashboard', element: <ManagerDashboard /> }, { path: '/manager/leaves', element: <ManagerLeaves /> }, { path: '/manager/expenses', element: <ManagerExpenses /> }, { path: '/manager/team-calendar', element: <LeaveCalendarPage /> }
    ] },
    { element: <RoleRoute roles={['HRAdmin', 'SuperAdmin']} />, children: [
      { path: '/hr/dashboard', element: <HrDashboard /> }, { path: '/hr/leaves', element: <HrLeaves /> }, { path: '/hr/leave-balances', element: <HrUsers /> }, { path: '/hr/holidays', element: <LeaveCalendarPage /> }, { path: '/hr/expenses', element: <HrExpenses /> }, { path: '/hr/reports', element: <Reports /> }, { path: '/hr/users', element: <HrUsers /> }
    ] }
  ] }] }
]);

ReactDOM.createRoot(document.getElementById('root')!).render(<React.StrictMode><QueryClientProvider client={queryClient}><RouterProvider router={router} /></QueryClientProvider></React.StrictMode>);
