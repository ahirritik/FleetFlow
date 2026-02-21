import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import {
    LayoutDashboard, Truck, Route, Wrench, Receipt, Users, BarChart3, LogOut, Menu, X
} from 'lucide-react';
import { useState } from 'react';

const navItems = [
    { to: '/', label: 'Dashboard', icon: LayoutDashboard },
    { to: '/vehicles', label: 'Vehicles', icon: Truck },
    { to: '/trips', label: 'Trips', icon: Route },
    { to: '/maintenance', label: 'Maintenance', icon: Wrench },
    { to: '/expenses', label: 'Expenses', icon: Receipt },
    { to: '/drivers', label: 'Drivers', icon: Users },
    { to: '/analytics', label: 'Analytics', icon: BarChart3 },
];

export default function Layout() {
    const { user, logout } = useAuth();
    const navigate = useNavigate();
    const [sidebarOpen, setSidebarOpen] = useState(false);

    const handleLogout = () => {
        logout();
        navigate('/login');
    };

    return (
        <div className="app-layout">
            <aside className={`sidebar ${sidebarOpen ? 'open' : ''}`}>
                <div className="sidebar-header">
                    <div className="logo">
                        <Truck size={28} />
                        <span>FleetFlow</span>
                    </div>
                    <button className="sidebar-close" onClick={() => setSidebarOpen(false)}>
                        <X size={20} />
                    </button>
                </div>

                <nav className="sidebar-nav">
                    {navItems.map(({ to, label, icon: Icon }) => (
                        <NavLink
                            key={to}
                            to={to}
                            end={to === '/'}
                            className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
                            onClick={() => setSidebarOpen(false)}
                        >
                            <Icon size={20} />
                            <span>{label}</span>
                        </NavLink>
                    ))}
                </nav>

                <div className="sidebar-footer">
                    <div className="user-info">
                        <div className="user-avatar">{user?.fullName?.[0] || 'U'}</div>
                        <div className="user-details">
                            <span className="user-name">{user?.fullName}</span>
                            <span className="user-role">{user?.role}</span>
                        </div>
                    </div>
                    <button className="btn-logout" onClick={handleLogout}>
                        <LogOut size={18} />
                        <span>Logout</span>
                    </button>
                </div>
            </aside>

            {sidebarOpen && <div className="sidebar-overlay" onClick={() => setSidebarOpen(false)} />}

            <main className="main-content">
                <header className="top-bar">
                    <button className="menu-toggle" onClick={() => setSidebarOpen(true)}>
                        <Menu size={22} />
                    </button>
                    <h1 className="page-title"></h1>
                </header>
                <div className="page-container">
                    <Outlet />
                </div>
            </main>
        </div>
    );
}
