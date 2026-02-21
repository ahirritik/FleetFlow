import { useState, useEffect } from 'react';
import api from '../services/api';
import toast from 'react-hot-toast';
import { Plus, Play, CheckCircle, XCircle, X } from 'lucide-react';

export default function TripDispatcher() {
    const [trips, setTrips] = useState([]);
    const [vehicles, setVehicles] = useState([]);
    const [drivers, setDrivers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showCreate, setShowCreate] = useState(false);
    const [showComplete, setShowComplete] = useState(null);
    const [statusFilter, setStatusFilter] = useState('');
    const [form, setForm] = useState({ vehicleId: '', driverId: '', origin: '', destination: '', cargoWeight: '', cargoDescription: '' });
    const [endOdometer, setEndOdometer] = useState('');

    useEffect(() => { loadData(); }, [statusFilter]);

    const loadData = async () => {
        try {
            const params = statusFilter ? { status: statusFilter } : {};
            const [tripsRes, vehRes, drRes] = await Promise.all([
                api.get('/trips', { params }),
                api.get('/vehicles', { params: { status: 'Available' } }),
                api.get('/drivers', { params: { status: 'OnDuty' } }),
            ]);
            setTrips(tripsRes.data);
            setVehicles(vehRes.data);
            setDrivers(drRes.data);
        } catch (err) { console.error(err); }
        finally { setLoading(false); }
    };

    const handleCreate = async (e) => {
        e.preventDefault();
        try {
            await api.post('/trips', { ...form, vehicleId: +form.vehicleId, driverId: +form.driverId, cargoWeight: +form.cargoWeight });
            toast.success('Trip created as Draft');
            setShowCreate(false);
            setForm({ vehicleId: '', driverId: '', origin: '', destination: '', cargoWeight: '', cargoDescription: '' });
            loadData();
        } catch (err) { toast.error(err.response?.data?.message || 'Error creating trip'); }
    };

    const handleDispatch = async (id) => {
        try { await api.post(`/trips/${id}/dispatch`); toast.success('Trip dispatched!'); loadData(); }
        catch (err) { toast.error(err.response?.data?.message || 'Error dispatching'); }
    };

    const handleComplete = async (id) => {
        if (!endOdometer) { toast.error('Enter final odometer reading'); return; }
        try { await api.post(`/trips/${id}/complete`, { endOdometer: +endOdometer }); toast.success('Trip completed!'); setShowComplete(null); setEndOdometer(''); loadData(); }
        catch (err) { toast.error(err.response?.data?.message || 'Error completing'); }
    };

    const handleCancel = async (id) => {
        if (!confirm('Cancel this trip?')) return;
        try { await api.post(`/trips/${id}/cancel`); toast.success('Trip cancelled'); loadData(); }
        catch (err) { toast.error(err.response?.data?.message || 'Error cancelling'); }
    };

    const getStatusClass = (s) => {
        const map = { Draft: 'status-draft', Dispatched: 'status-dispatched', Completed: 'status-completed', Cancelled: 'status-cancelled' };
        return map[s] || '';
    };

    if (loading) return <div className="loading-spinner"><div className="spinner"></div></div>;

    return (
        <div className="page-content">
            <div className="page-header">
                <div>
                    <h2>Trip Dispatcher</h2>
                    <p>Create, dispatch, and manage trips</p>
                </div>
                <button className="btn btn-primary" onClick={() => setShowCreate(true)}><Plus size={18} /> New Trip</button>
            </div>

            <div className="filters-bar">
                <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                    <option value="">All Statuses</option>
                    <option value="Draft">Draft</option>
                    <option value="Dispatched">Dispatched</option>
                    <option value="Completed">Completed</option>
                    <option value="Cancelled">Cancelled</option>
                </select>
            </div>

            <div className="table-container">
                <table className="data-table">
                    <thead>
                        <tr>
                            <th>#</th><th>Vehicle</th><th>Driver</th><th>Origin</th><th>Destination</th>
                            <th>Cargo</th><th>Status</th><th>Created</th><th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {trips.length === 0 ? (
                            <tr><td colSpan="9" className="empty-cell">No trips found</td></tr>
                        ) : trips.map((t) => (
                            <tr key={t.id}>
                                <td>{t.id}</td>
                                <td>{t.vehicleName} <span className="text-muted">({t.vehiclePlate})</span></td>
                                <td>{t.driverName}</td>
                                <td>{t.origin}</td>
                                <td>{t.destination}</td>
                                <td>{t.cargoWeight.toLocaleString()} kg</td>
                                <td><span className={`status-pill ${getStatusClass(t.status)}`}>{t.status}</span></td>
                                <td>{new Date(t.createdAt).toLocaleDateString()}</td>
                                <td>
                                    <div className="action-buttons">
                                        {t.status === 'Draft' && (
                                            <>
                                                <button className="btn-sm btn-success" onClick={() => handleDispatch(t.id)} title="Dispatch"><Play size={14} /> Dispatch</button>
                                                <button className="btn-sm btn-danger" onClick={() => handleCancel(t.id)} title="Cancel"><XCircle size={14} /></button>
                                            </>
                                        )}
                                        {t.status === 'Dispatched' && (
                                            <>
                                                <button className="btn-sm btn-success" onClick={() => setShowComplete(t.id)} title="Complete"><CheckCircle size={14} /> Complete</button>
                                                <button className="btn-sm btn-danger" onClick={() => handleCancel(t.id)} title="Cancel"><XCircle size={14} /></button>
                                            </>
                                        )}
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {showCreate && (
                <div className="modal-overlay" onClick={() => setShowCreate(false)}>
                    <div className="modal" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>Create New Trip</h3>
                            <button className="btn-icon" onClick={() => setShowCreate(false)}><X size={20} /></button>
                        </div>
                        <form onSubmit={handleCreate} className="modal-body">
                            <div className="form-grid">
                                <div className="form-group">
                                    <label>Vehicle (Available)</label>
                                    <select required value={form.vehicleId} onChange={(e) => setForm(f => ({ ...f, vehicleId: e.target.value }))}>
                                        <option value="">Select vehicle...</option>
                                        {vehicles.map(v => <option key={v.id} value={v.id}>{v.name} ({v.licensePlate}) — Max: {v.maxCapacity}kg</option>)}
                                    </select>
                                </div>
                                <div className="form-group">
                                    <label>Driver (On Duty)</label>
                                    <select required value={form.driverId} onChange={(e) => setForm(f => ({ ...f, driverId: e.target.value }))}>
                                        <option value="">Select driver...</option>
                                        {drivers.map(d => <option key={d.id} value={d.id}>{d.fullName} ({d.licenseCategory})</option>)}
                                    </select>
                                </div>
                                <div className="form-group">
                                    <label>Origin</label>
                                    <input required value={form.origin} onChange={(e) => setForm(f => ({ ...f, origin: e.target.value }))} placeholder="e.g. Warehouse A" />
                                </div>
                                <div className="form-group">
                                    <label>Destination</label>
                                    <input required value={form.destination} onChange={(e) => setForm(f => ({ ...f, destination: e.target.value }))} placeholder="e.g. Distribution Center B" />
                                </div>
                                <div className="form-group">
                                    <label>Cargo Weight (kg)</label>
                                    <input required type="number" min="1" value={form.cargoWeight} onChange={(e) => setForm(f => ({ ...f, cargoWeight: e.target.value }))} />
                                </div>
                                <div className="form-group">
                                    <label>Cargo Description</label>
                                    <input value={form.cargoDescription} onChange={(e) => setForm(f => ({ ...f, cargoDescription: e.target.value }))} placeholder="e.g. Electronics" />
                                </div>
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-secondary" onClick={() => setShowCreate(false)}>Cancel</button>
                                <button type="submit" className="btn btn-primary">Create Trip</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {showComplete && (
                <div className="modal-overlay" onClick={() => setShowComplete(null)}>
                    <div className="modal modal-sm" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>Complete Trip #{showComplete}</h3>
                            <button className="btn-icon" onClick={() => setShowComplete(null)}><X size={20} /></button>
                        </div>
                        <div className="modal-body">
                            <div className="form-group">
                                <label>Final Odometer Reading (km)</label>
                                <input type="number" min="0" value={endOdometer} onChange={(e) => setEndOdometer(e.target.value)} placeholder="Enter final odometer" />
                            </div>
                            <div className="modal-footer">
                                <button className="btn btn-secondary" onClick={() => setShowComplete(null)}>Cancel</button>
                                <button className="btn btn-primary" onClick={() => handleComplete(showComplete)}>Complete</button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
