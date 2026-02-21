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

function AppRoutes() {
    const { user } = useAuth();

    return (
        <Routes>
            <Route path="/login" element={user ? <Navigate to="/" /> : <LoginPage />} />
            <Route path="/" element={<ProtectedRoute><Layout /></ProtectedRoute>}>
                <Route index element={<Dashboard />} />
                <Route path="vehicles" element={<VehicleRegistry />} />
                <Route path="trips" element={<TripDispatcher />} />
                <Route path="maintenance" element={<MaintenanceLogs />} />
                <Route path="expenses" element={<ExpenseFuel />} />
                <Route path="drivers" element={<DriverProfiles />} />
                <Route path="analytics" element={<Analytics />} />
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
