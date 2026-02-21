import { useState, useEffect } from 'react';
import api from '../services/api';
import { Truck, AlertTriangle, Activity, Package, Users, MapPin, BarChart3 } from 'lucide-react';
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, Legend } from 'recharts';

const COLORS = ['#3b82f6', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6'];
const STATUS_COLORS = { Available: '#22c55e', OnTrip: '#3b82f6', InShop: '#f59e0b', Retired: '#6b7280' };

export default function Dashboard() {
    const [dashboard, setDashboard] = useState(null);
    const [recentTrips, setRecentTrips] = useState([]);
    const [breakdown, setBreakdown] = useState(null);
    const [region, setRegion] = useState('');
    const [vehicleType, setVehicleType] = useState('');
    const [vehicleStatus, setVehicleStatus] = useState('');
    const [loading, setLoading] = useState(true);

    useEffect(() => { loadData(); }, [region, vehicleType, vehicleStatus]);

    const loadData = async () => {
        try {
            const params = new URLSearchParams();
            if (region) params.append('region', region);
            if (vehicleType) params.append('type', vehicleType);
            if (vehicleStatus) params.append('status', vehicleStatus);
            const qs = params.toString() ? `?${params.toString()}` : '';
            const [dashRes, tripsRes, breakdownRes] = await Promise.all([
                api.get(`/analytics/dashboard${qs}`),
                api.get('/trips'),
                api.get('/analytics/vehicle-breakdown'),
            ]);
            setDashboard(dashRes.data);
            setRecentTrips(tripsRes.data.slice(0, 5));
            setBreakdown(breakdownRes.data);
        } catch (err) {
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    if (loading) return <div className="loading-spinner"><div className="spinner"></div></div>;

    const kpis = [
        { label: 'Active Fleet', value: dashboard?.activeFleet || 0, icon: Truck, color: 'blue', sub: 'Vehicles on trip' },
        { label: 'Maintenance Alerts', value: dashboard?.maintenanceAlerts || 0, icon: AlertTriangle, color: 'amber', sub: 'Vehicles in shop' },
        { label: 'Utilization Rate', value: `${dashboard?.utilizationRate || 0}%`, icon: Activity, color: 'green', sub: 'Fleet assigned vs idle' },
        { label: 'Pending Cargo', value: dashboard?.pendingCargo || 0, icon: Package, color: 'purple', sub: 'Awaiting dispatch' },
    ];

    const stats = [
        { label: 'Total Vehicles', value: dashboard?.totalVehicles || 0, icon: Truck },
        { label: 'Total Drivers', value: dashboard?.totalDrivers || 0, icon: Users },
        { label: 'Completed Trips', value: dashboard?.totalTrips || 0, icon: MapPin },
        { label: 'Total Revenue', value: `₹${(dashboard?.totalRevenue || 0).toLocaleString()}`, icon: BarChart3 },
    ];

    const getStatusClass = (status) => {
        const map = { Draft: 'status-draft', Dispatched: 'status-dispatched', Completed: 'status-completed', Cancelled: 'status-cancelled' };
        return map[status] || '';
    };

    return (
        <div className="dashboard-page">
            <div className="page-header">
                <div>
                    <h2>Command Center</h2>
                    <p>Real-time fleet oversight at a glance</p>
                </div>
                <div className="filters-bar">
                    <select value={vehicleType} onChange={e => setVehicleType(e.target.value)}>
                        <option value="">All Types</option>
                        <option value="Truck">Truck</option>
                        <option value="Van">Van</option>
                        <option value="Bike">Bike</option>
                    </select>
                    <select value={vehicleStatus} onChange={e => setVehicleStatus(e.target.value)}>
                        <option value="">All Statuses</option>
                        <option value="Available">Available</option>
                        <option value="OnTrip">On Trip</option>
                        <option value="InShop">In Shop</option>
                        <option value="Retired">Retired</option>
                    </select>
                    <select value={region} onChange={e => setRegion(e.target.value)}>
                        <option value="">All Regions</option>
                        <option value="North">North</option>
                        <option value="East">East</option>
                        <option value="West">West</option>
                        <option value="South">South</option>
                    </select>
                </div>
            </div>

            <div className="kpi-grid">
                {kpis.map((kpi) => (
                    <div key={kpi.label} className={`kpi-card kpi-${kpi.color}`}>
                        <div className="kpi-icon">
                            <kpi.icon size={24} />
                        </div>
                        <div className="kpi-content">
                            <span className="kpi-value">{kpi.value}</span>
                            <span className="kpi-label">{kpi.label}</span>
                            <span className="kpi-sub">{kpi.sub}</span>
                        </div>
                    </div>
                ))}
            </div>

            <div className="stats-row">
                {stats.map((s) => (
                    <div key={s.label} className="stat-card">
                        <s.icon size={20} />
                        <span className="stat-value">{s.value}</span>
                        <span className="stat-label">{s.label}</span>
                    </div>
                ))}
            </div>

            {breakdown && (
                <div className="charts-grid">
                    <div className="chart-card">
                        <h3>Fleet by Vehicle Type</h3>
                        <ResponsiveContainer width="100%" height={250}>
                            <PieChart>
                                <Pie data={breakdown.byType} cx="50%" cy="50%" outerRadius={80} dataKey="value"
                                    label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}>
                                    {breakdown.byType.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                                </Pie>
                                <Tooltip />
                                <Legend />
                            </PieChart>
                        </ResponsiveContainer>
                    </div>
                    <div className="chart-card">
                        <h3>Fleet by Status</h3>
                        <ResponsiveContainer width="100%" height={250}>
                            <PieChart>
                                <Pie data={breakdown.byStatus} cx="50%" cy="50%" outerRadius={80} dataKey="value"
                                    label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}>
                                    {breakdown.byStatus.map((entry, i) => <Cell key={i} fill={STATUS_COLORS[entry.name] || COLORS[i % COLORS.length]} />)}
                                </Pie>
                                <Tooltip />
                                <Legend />
                            </PieChart>
                        </ResponsiveContainer>
                    </div>
                </div>
            )}

            <div className="dashboard-section">
                <h3>Recent Trips</h3>
                {recentTrips.length === 0 ? (
                    <div className="empty-state">No trips yet. Create one from the Trips page.</div>
                ) : (
                    <div className="table-container">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Trip #</th>
                                    <th>Vehicle</th>
                                    <th>Driver</th>
                                    <th>Route</th>
                                    <th>Cargo</th>
                                    <th>Status</th>
                                </tr>
                            </thead>
                            <tbody>
                                {recentTrips.map((t) => (
                                    <tr key={t.id}>
                                        <td>#{t.id}</td>
                                        <td>{t.vehicleName}</td>
                                        <td>{t.driverName}</td>
                                        <td className="route-cell">{t.origin} → {t.destination}</td>
                                        <td>{t.cargoWeight.toLocaleString()} kg</td>
                                        <td><span className={`status-pill ${getStatusClass(t.status)}`}>{t.status}</span></td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </div>
    );
}
