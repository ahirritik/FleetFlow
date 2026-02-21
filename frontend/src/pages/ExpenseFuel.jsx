import { useState, useEffect } from 'react';
import api from '../services/api';
import toast from 'react-hot-toast';
import { Plus, Trash2, X, Pencil } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export default function ExpenseFuel() {
    const { user } = useAuth();
    const canWrite = ['Manager', 'Dispatcher'].includes(user?.role);
    const canEdit = user?.role === 'Manager';
    const [expenses, setExpenses] = useState([]);
    const [vehicles, setVehicles] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showModal, setShowModal] = useState(false);
    const [editing, setEditing] = useState(null);
    const [vehicleFilter, setVehicleFilter] = useState('');
    const [form, setForm] = useState({ vehicleId: '', tripId: '', category: 'Fuel', liters: '', cost: '', date: '', notes: '' });

    useEffect(() => { loadData(); }, [vehicleFilter]);

    const loadData = async () => {
        try {
            const params = vehicleFilter ? { vehicleId: vehicleFilter } : {};
            const [expRes, vehRes] = await Promise.all([
                api.get('/expenses', { params }),
                api.get('/vehicles'),
            ]);
            setExpenses(expRes.data);
            setVehicles(vehRes.data);
        } catch (err) { console.error(err); }
        finally { setLoading(false); }
    };

    const openCreate = () => { setEditing(null); setForm({ vehicleId: '', tripId: '', category: 'Fuel', liters: '', cost: '', date: '', notes: '' }); setShowModal(true); };
    const openEdit = (e) => {
        setEditing(e.id);
        setForm({ vehicleId: String(e.vehicleId), tripId: e.tripId ? String(e.tripId) : '', category: e.category, liters: e.liters ? String(e.liters) : '', cost: String(e.cost), date: e.date.split('T')[0], notes: e.notes || '' });
        setShowModal(true);
    };

    const handleSubmit = async (ev) => {
        ev.preventDefault();
        const payload = { vehicleId: +form.vehicleId, tripId: form.tripId ? +form.tripId : null, category: form.category, liters: form.liters ? +form.liters : null, cost: +form.cost, date: form.date, notes: form.notes };
        try {
            if (editing) { await api.put(`/expenses/${editing}`, payload); toast.success('Expense updated'); }
            else { await api.post('/expenses', payload); toast.success('Expense logged'); }
            setShowModal(false); setEditing(null);
            setForm({ vehicleId: '', tripId: '', category: 'Fuel', liters: '', cost: '', date: '', notes: '' });
            loadData();
        } catch (err) { toast.error(err.response?.data?.message || 'Error'); }
    };

    const handleDelete = async (id) => {
        if (!confirm('Delete this expense?')) return;
        try { await api.delete(`/expenses/${id}`); toast.success('Deleted'); loadData(); }
        catch (err) { toast.error('Error deleting'); }
    };

    // Compute totals per vehicle
    const vehicleTotals = {};
    expenses.forEach(e => {
        if (!vehicleTotals[e.vehicleName]) vehicleTotals[e.vehicleName] = { fuel: 0, other: 0, total: 0 };
        if (e.category === 'Fuel') vehicleTotals[e.vehicleName].fuel += e.cost;
        else vehicleTotals[e.vehicleName].other += e.cost;
        vehicleTotals[e.vehicleName].total += e.cost;
    });

    if (loading) return <div className="loading-spinner"><div className="spinner"></div></div>;

    return (
        <div className="page-content">
            <div className="page-header">
                <div>
                    <h2>Expense & Fuel Logging</h2>
                    <p>Track operational costs per vehicle</p>
                </div>
                {canWrite && <button className="btn btn-primary" onClick={openCreate}><Plus size={18} /> Log Expense</button>}
            </div>

            <div className="filters-bar">
                <select value={vehicleFilter} onChange={(e) => setVehicleFilter(e.target.value)}>
                    <option value="">All Vehicles</option>
                    {vehicles.map(v => <option key={v.id} value={v.id}>{v.name} ({v.licensePlate})</option>)}
                </select>
            </div>

            {Object.keys(vehicleTotals).length > 0 && (
                <div className="stats-row">
                    {Object.entries(vehicleTotals).map(([name, t]) => (
                        <div key={name} className="stat-card">
                            <span className="stat-label">{name}</span>
                            <span className="stat-value">₹{t.total.toLocaleString()}</span>
                            <span className="stat-sub">Fuel: ₹{t.fuel.toLocaleString()} | Other: ₹{t.other.toLocaleString()}</span>
                        </div>
                    ))}
                </div>
            )}

            <div className="table-container">
                <table className="data-table">
                    <thead>
                        <tr>
                            <th>Vehicle</th><th>Category</th><th>Liters</th>
                            <th>Cost</th><th>Date</th><th>Notes</th>{canWrite && <th>Actions</th>}
                        </tr>
                    </thead>
                    <tbody>
                        {expenses.length === 0 ? (
                            <tr><td colSpan="7" className="empty-cell">No expenses recorded</td></tr>
                        ) : expenses.map((e) => (
                            <tr key={e.id}>
                                <td>{e.vehicleName}</td>
                                <td><span className={`status-pill ${e.category === 'Fuel' ? 'status-dispatched' : 'status-draft'}`}>{e.category}</span></td>
                                <td>{e.liters ? `${e.liters} L` : '—'}</td>
                                <td className="font-medium">₹{e.cost.toLocaleString()}</td>
                                <td>{new Date(e.date).toLocaleDateString()}</td>
                                <td>{e.notes || '—'}</td>
                                {canWrite && <td>
                                    <div className="action-buttons">
                                        {canEdit && <button className="btn-icon" onClick={() => openEdit(e)} title="Edit"><Pencil size={16} /></button>}
                                        <button className="btn-icon btn-danger" onClick={() => handleDelete(e.id)}><Trash2 size={16} /></button>
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
                            <h3>{editing ? 'Edit Expense' : 'Log Expense'}</h3>
                            <button className="btn-icon" onClick={() => setShowModal(false)}><X size={20} /></button>
                        </div>
                        <form onSubmit={handleSubmit} className="modal-body">
                            <div className="form-grid">
                                <div className="form-group">
                                    <label>Vehicle</label>
                                    <select required value={form.vehicleId} onChange={(e) => setForm(f => ({ ...f, vehicleId: e.target.value }))}>
                                        <option value="">Select vehicle...</option>
                                        {vehicles.map(v => <option key={v.id} value={v.id}>{v.name} ({v.licensePlate})</option>)}
                                    </select>
                                </div>
                                <div className="form-group">
                                    <label>Category</label>
                                    <select value={form.category} onChange={(e) => setForm(f => ({ ...f, category: e.target.value }))}>
                                        <option value="Fuel">Fuel</option>
                                        <option value="Toll">Toll</option>
                                        <option value="Other">Other</option>
                                    </select>
                                </div>
                                {form.category === 'Fuel' && (
                                    <div className="form-group">
                                        <label>Liters</label>
                                        <input type="number" min="0" step="0.1" value={form.liters} onChange={(e) => setForm(f => ({ ...f, liters: e.target.value }))} />
                                    </div>
                                )}
                                <div className="form-group">
                                    <label>Cost (₹)</label>
                                    <input required type="number" min="1" step="0.01" value={form.cost} onChange={(e) => setForm(f => ({ ...f, cost: e.target.value }))} />
                                </div>
                                <div className="form-group">
                                    <label>Date</label>
                                    <input required type="date" max={new Date().toISOString().split('T')[0]} value={form.date} onChange={(e) => setForm(f => ({ ...f, date: e.target.value }))} />
                                </div>
                                <div className="form-group">
                                    <label>Notes</label>
                                    <input value={form.notes} onChange={(e) => setForm(f => ({ ...f, notes: e.target.value }))} placeholder="Optional notes..." />
                                </div>
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancel</button>
                                <button type="submit" className="btn btn-primary">{editing ? 'Update' : 'Log Expense'}</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
