import { useState, useEffect } from 'react';
import api from '../services/api';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend } from 'recharts';
import { Download, FileText } from 'lucide-react';
import toast from 'react-hot-toast';

const COLORS = ['#3b82f6', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

export default function Analytics() {
    const [costs, setCosts] = useState([]);
    const [dashboard, setDashboard] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => { loadData(); }, []);

    const loadData = async () => {
        try {
            const [costsRes, dashRes] = await Promise.all([
                api.get('/analytics/vehicle-costs'),
                api.get('/analytics/dashboard'),
            ]);
            setCosts(costsRes.data);
            setDashboard(dashRes.data);
        } catch (err) { console.error(err); }
        finally { setLoading(false); }
    };

    const exportCsv = async () => {
        try {
            const res = await api.get('/analytics/export/csv', { responseType: 'blob' });
            const url = window.URL.createObjectURL(new Blob([res.data]));
            const a = document.createElement('a'); a.href = url; a.download = 'fleet_report.csv'; a.click();
            toast.success('CSV report downloaded');
        } catch (err) { toast.error('Export failed'); }
    };

    const exportPdf = async () => {
        try {
            const res = await api.get('/analytics/export/pdf', { responseType: 'blob' });
            const url = window.URL.createObjectURL(new Blob([res.data], { type: 'application/pdf' }));
            const a = document.createElement('a'); a.href = url; a.download = 'fleet_report.pdf'; a.click();
            toast.success('PDF report downloaded');
        } catch (err) { toast.error('PDF export failed'); }
    };

    if (loading) return <div className="loading-spinner"><div className="spinner"></div></div>;

    const costData = costs.map(c => ({ name: c.vehicleName, Fuel: c.fuelCost, Maintenance: c.maintenanceCost }));
    const pieData = costs.filter(c => c.totalCost > 0).map(c => ({ name: c.vehicleName, value: c.totalCost }));

    return (
        <div className="page-content">
            <div className="page-header">
                <div><h2>Analytics & Reports</h2><p>Data-driven fleet insights</p></div>
                <div className="action-buttons">
                    <button className="btn btn-secondary" onClick={exportCsv}><Download size={18} /> Export CSV</button>
                    <button className="btn btn-primary" onClick={exportPdf}><FileText size={18} /> Export PDF</button>
                </div>
            </div>

            <div className="charts-grid">
                <div className="chart-card">
                    <h3>Cost Breakdown by Vehicle</h3>
                    <ResponsiveContainer width="100%" height={300}>
                        <BarChart data={costData}>
                            <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                            <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                            <YAxis tick={{ fontSize: 12 }} />
                            <Tooltip formatter={(v) => `₹${v.toLocaleString()}`} />
                            <Legend />
                            <Bar dataKey="Fuel" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                            <Bar dataKey="Maintenance" fill="#f59e0b" radius={[4, 4, 0, 0]} />
                        </BarChart>
                    </ResponsiveContainer>
                </div>

                <div className="chart-card">
                    <h3>Cost Distribution</h3>
                    <ResponsiveContainer width="100%" height={300}>
                        <PieChart>
                            <Pie data={pieData} cx="50%" cy="50%" outerRadius={100} dataKey="value" label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}>
                                {pieData.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                            </Pie>
                            <Tooltip formatter={(v) => `₹${v.toLocaleString()}`} />
                        </PieChart>
                    </ResponsiveContainer>
                </div>
            </div>

            <div className="dashboard-section">
                <h3>Vehicle Financial Summary</h3>
                <div className="table-container">
                    <table className="data-table">
                        <thead>
                            <tr><th>Vehicle</th><th>Plate</th><th>Fuel Cost</th><th>Maint. Cost</th><th>Total Cost</th><th>Odometer</th><th>₹/km</th><th>Fuel Eff.</th><th>ROI %</th></tr>
                        </thead>
                        <tbody>
                            {costs.map(c => (
                                <tr key={c.vehicleId}>
                                    <td className="font-medium">{c.vehicleName}</td>
                                    <td><code>{c.licensePlate}</code></td>
                                    <td>₹{c.fuelCost.toLocaleString()}</td>
                                    <td>₹{c.maintenanceCost.toLocaleString()}</td>
                                    <td className="font-medium">₹{c.totalCost.toLocaleString()}</td>
                                    <td>{c.odometer.toLocaleString()} km</td>
                                    <td>₹{c.costPerKm}</td>
                                    <td>{c.fuelEfficiency > 0 ? `${c.fuelEfficiency} km/L` : <span className="text-muted">N/A</span>}</td>
                                    <td><span className={`status-pill ${c.roi >= 0 ? 'status-completed' : 'status-cancelled'}`}>{c.roi}%</span></td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}
