import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider, useAuth } from './context/AuthContext';
import Layout from './components/Layout';
import LoginPage from './pages/LoginPage';
import Dashboard from './pages/Dashboard';
import VehicleRegistry from './pages/VehicleRegistry';
import TripDispatcher from './pages/TripDispatcher';
import MaintenanceLogs from './pages/MaintenanceLogs';
import ExpenseFuel from './pages/ExpenseFuel';
import DriverProfiles from './pages/DriverProfiles';
import Analytics from './pages/Analytics';

function ProtectedRoute({ children }) {
    const { user, loading } = useAuth();
    if (loading) return <div className="loading-spinner"><div className="spinner"></div></div>;
    return user ? children : <Navigate to="/login" />;
}

function RoleRoute({ children, allowedRoles }) {
    const { user } = useAuth();
    if (!user || !allowedRoles.includes(user.role)) return <Navigate to="/" replace />;
    return children;
}

function AppRoutes() {
    const { user } = useAuth();

    return (
        <Routes>
            <Route path="/login" element={user ? <Navigate to="/" /> : <LoginPage />} />
            <Route path="/" element={<ProtectedRoute><Layout /></ProtectedRoute>}>
                <Route index element={<Dashboard />} />
                <Route path="vehicles" element={<VehicleRegistry />} />
                <Route path="trips" element={<RoleRoute allowedRoles={['Manager', 'Dispatcher']}><TripDispatcher /></RoleRoute>} />
                <Route path="maintenance" element={<RoleRoute allowedRoles={['Manager']}><MaintenanceLogs /></RoleRoute>} />
                <Route path="expenses" element={<RoleRoute allowedRoles={['Manager', 'Dispatcher']}><ExpenseFuel /></RoleRoute>} />
                <Route path="drivers" element={<RoleRoute allowedRoles={['Manager', 'SafetyOfficer']}><DriverProfiles /></RoleRoute>} />
                <Route path="analytics" element={<RoleRoute allowedRoles={['Manager', 'Analyst']}><Analytics /></RoleRoute>} />
            </Route>
        </Routes>
    );
}

export default function App() {
    return (
        <BrowserRouter>
            <AuthProvider>
                <Toaster position="top-right" toastOptions={{ duration: 3000, style: { background: '#1e2028', color: '#fff', borderRadius: '10px' } }} />
                <AppRoutes />
            </AuthProvider>
        </BrowserRouter>
    );
}
