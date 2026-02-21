import { useState, useEffect } from 'react';
import api from '../services/api';
import toast from 'react-hot-toast';
import { Plus, CheckCircle, X, Pencil } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export default function MaintenanceLogs() {
    const { user } = useAuth();
    const canWrite = user?.role === 'Manager';
    const [logs, setLogs] = useState([]);
    const [vehicles, setVehicles] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showModal, setShowModal] = useState(false);
    const [editing, setEditing] = useState(null);
    const [vehicleFilter, setVehicleFilter] = useState('');
    const [form, setForm] = useState({ vehicleId: '', serviceType: '', description: '', cost: '', date: '' });

    useEffect(() => { loadData(); }, [vehicleFilter]);

    const loadData = async () => {
        try {
            const params = vehicleFilter ? { vehicleId: vehicleFilter } : {};
            const [logsRes, vehRes] = await Promise.all([
                api.get('/maintenance', { params }),
                api.get('/vehicles'),
            ]);
            setLogs(logsRes.data);
            setVehicles(vehRes.data);
        } catch (err) { console.error(err); }
        finally { setLoading(false); }
    };

    const openCreate = () => { setEditing(null); setForm({ vehicleId: '', serviceType: '', description: '', cost: '', date: '' }); setShowModal(true); };
    const openEdit = (m) => {
        setEditing(m.id);
        setForm({ vehicleId: String(m.vehicleId), serviceType: m.serviceType, description: m.description, cost: String(m.cost), date: m.date.split('T')[0] });
        setShowModal(true);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        const payload = { vehicleId: +form.vehicleId, serviceType: form.serviceType, description: form.description, cost: +form.cost, date: form.date };
        try {
            if (editing) { await api.put(`/maintenance/${editing}`, payload); toast.success('Maintenance updated'); }
            else { await api.post('/maintenance', payload); toast.success('Maintenance logged — vehicle moved to In Shop'); }
            setShowModal(false); setEditing(null);
            setForm({ vehicleId: '', serviceType: '', description: '', cost: '', date: '' });
            loadData();
        } catch (err) { toast.error(err.response?.data?.message || 'Error'); }
    };

    const handleComplete = async (id) => {
        try { await api.patch(`/maintenance/${id}/complete`); toast.success('Maintenance completed'); loadData(); }
        catch (err) { toast.error(err.response?.data?.message || 'Error'); }
    };

    if (loading) return <div className="loading-spinner"><div className="spinner"></div></div>;

    return (
        <div className="page-content">
            <div className="page-header">
                <div>
                    <h2>Maintenance & Service Logs</h2>
                    <p>Track vehicle health and preventative maintenance</p>
                </div>
                {canWrite && <button className="btn btn-primary" onClick={openCreate}><Plus size={18} /> Log Maintenance</button>}
            </div>

            <div className="filters-bar">
                <select value={vehicleFilter} onChange={(e) => setVehicleFilter(e.target.value)}>
                    <option value="">All Vehicles</option>
                    {vehicles.map(v => <option key={v.id} value={v.id}>{v.name} ({v.licensePlate})</option>)}
                </select>
            </div>

            <div className="table-container">
                <table className="data-table">
                    <thead>
                        <tr>
                            <th>Vehicle</th><th>Service Type</th><th>Description</th>
                            <th>Cost</th><th>Date</th><th>Status</th>{canWrite && <th>Actions</th>}
                        </tr>
                    </thead>
                    <tbody>
                        {logs.length === 0 ? (
                            <tr><td colSpan="7" className="empty-cell">No maintenance logs</td></tr>
                        ) : logs.map((m) => (
                            <tr key={m.id}>
                                <td>{m.vehicleName}</td>
                                <td className="font-medium">{m.serviceType}</td>
                                <td>{m.description}</td>
                                <td>₹{m.cost.toLocaleString()}</td>
                                <td>{new Date(m.date).toLocaleDateString()}</td>
                                <td>
                                    <span className={`status-pill ${m.isCompleted ? 'status-completed' : 'status-amber'}`}>
                                        {m.isCompleted ? 'Completed' : 'In Progress'}
                                    </span>
                                </td>
                                {canWrite && <td>
                                    <div className="action-buttons">
                                        {!m.isCompleted && <button className="btn-icon" onClick={() => openEdit(m)} title="Edit"><Pencil size={16} /></button>}
                                        {!m.isCompleted && (
                                            <button className="btn-sm btn-success" onClick={() => handleComplete(m.id)}>
                                                <CheckCircle size={14} /> Mark Done
                                            </button>
                                        )}
                                    </div>
                                </td>}
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {showModal && (
                <div className="modal-overlay" onClick={() => setShowModal(false)}>
                    <div className="modal" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>{editing ? 'Edit Maintenance' : 'Log Maintenance'}</h3>
                            <button className="btn-icon" onClick={() => setShowModal(false)}><X size={20} /></button>
                        </div>
                        <form onSubmit={handleSubmit} className="modal-body">
                            <div className="form-grid">
                                <div className="form-group">
                                    <label>Vehicle</label>
                                    <select required value={form.vehicleId} onChange={(e) => setForm(f => ({ ...f, vehicleId: e.target.value }))}>
                                        <option value="">Select vehicle...</option>
                                        {vehicles.filter(v => v.status !== 'OnTrip').map(v => (
                                            <option key={v.id} value={v.id}>{v.name} ({v.licensePlate})</option>
                                        ))}
                                    </select>
                                </div>
                                <div className="form-group">
                                    <label>Service Type</label>
                                    <select required value={form.serviceType} onChange={(e) => setForm(f => ({ ...f, serviceType: e.target.value }))}>
                                        <option value="">Select...</option>
                                        <option value="Oil Change">Oil Change</option>
                                        <option value="Tire Rotation">Tire Rotation</option>
                                        <option value="Brake Repair">Brake Repair</option>
                                        <option value="Engine Tune-up">Engine Tune-up</option>
                                        <option value="Battery Replacement">Battery Replacement</option>
                                        <option value="General Inspection">General Inspection</option>
                                        <option value="Other">Other</option>
                                    </select>
                                </div>
                                <div className="form-group full-width">
                                    <label>Description</label>
                                    <input value={form.description} onChange={(e) => setForm(f => ({ ...f, description: e.target.value }))} placeholder="Details..." />
                                </div>
                                <div className="form-group">
                                    <label>Cost (₹)</label>
                                    <input required type="number" min="1" step="0.01" value={form.cost} onChange={(e) => setForm(f => ({ ...f, cost: e.target.value }))} />
                                </div>
                                <div className="form-group">
                                    <label>Date</label>
                                    <input required type="date" max={new Date().toISOString().split('T')[0]} value={form.date} onChange={(e) => setForm(f => ({ ...f, date: e.target.value }))} />
                                </div>
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancel</button>
                                <button type="submit" className="btn btn-primary">{editing ? 'Update' : 'Log Maintenance'}</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
