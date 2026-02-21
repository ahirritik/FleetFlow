import { useState, useEffect } from 'react';
import api from '../services/api';
import toast from 'react-hot-toast';
import { Plus, Pencil, X, AlertTriangle, Shield } from 'lucide-react';

export default function DriverProfiles() {
    const [drivers, setDrivers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showModal, setShowModal] = useState(false);
    const [editing, setEditing] = useState(null);
    const [statusFilter, setStatusFilter] = useState('');
    const [form, setForm] = useState({ fullName: '', licenseNumber: '', licenseExpiry: '', licenseCategory: 'Truck', phone: '' });

    useEffect(() => { loadDrivers(); }, [statusFilter]);

    const loadDrivers = async () => {
        try {
            const params = statusFilter ? { status: statusFilter } : {};
            const { data } = await api.get('/drivers', { params });
            setDrivers(data);
        } catch (err) { console.error(err); }
        finally { setLoading(false); }
    };

    const openCreate = () => { setForm({ fullName: '', licenseNumber: '', licenseExpiry: '', licenseCategory: 'Truck', phone: '' }); setEditing(null); setShowModal(true); };
    const openEdit = (d) => {
        setEditing(d.id);
        setForm({ fullName: d.fullName, licenseNumber: d.licenseNumber, licenseExpiry: d.licenseExpiry.split('T')[0], licenseCategory: d.licenseCategory, phone: d.phone });
        setShowModal(true);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            if (editing) { await api.put(`/drivers/${editing}`, form); toast.success('Driver updated'); }
            else { await api.post('/drivers', form); toast.success('Driver added'); }
            setShowModal(false); loadDrivers();
        } catch (err) { toast.error(err.response?.data?.message || 'Error'); }
    };

    const handleStatusChange = async (id, status) => {
        try { await api.patch(`/drivers/${id}/status`, { status }); toast.success('Status updated'); loadDrivers(); }
        catch (err) { toast.error(err.response?.data?.message || 'Error'); }
    };

    const isExpired = (date) => new Date(date) < new Date();
    const completionRate = (d) => d.tripCount > 0 ? Math.round(d.completedTrips / d.tripCount * 100) : 0;

    if (loading) return <div className="loading-spinner"><div className="spinner"></div></div>;

    return (
        <div className="page-content">
            <div className="page-header">
                <div><h2>Driver Profiles & Safety</h2><p>Manage driver compliance and performance</p></div>
                <button className="btn btn-primary" onClick={openCreate}><Plus size={18} /> Add Driver</button>
            </div>
            <div className="filters-bar">
                <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                    <option value="">All Statuses</option>
                    <option value="OnDuty">On Duty</option>
                    <option value="OffDuty">Off Duty</option>
                    <option value="Suspended">Suspended</option>
                    <option value="OnTrip">On Trip</option>
                </select>
            </div>
            <div className="driver-grid">
                {drivers.length === 0 ? <div className="empty-state">No drivers found</div> : drivers.map((d) => (
                    <div key={d.id} className="driver-card">
                        <div className="driver-card-header">
                            <div className="driver-avatar">{d.fullName.split(' ').map(n => n[0]).join('')}</div>
                            <div className="driver-info"><h4>{d.fullName}</h4><span className="text-muted">{d.licenseCategory} License</span></div>
                            <button className="btn-icon" onClick={() => openEdit(d)}><Pencil size={16} /></button>
                        </div>
                        <div className="driver-details">
                            <div className="detail-row"><span>License #</span><span><code>{d.licenseNumber}</code></span></div>
                            <div className="detail-row"><span>Expiry</span>
                                <span className={isExpired(d.licenseExpiry) ? 'text-danger' : ''}>
                                    {isExpired(d.licenseExpiry) && <AlertTriangle size={14} />}
                                    {new Date(d.licenseExpiry).toLocaleDateString()}
                                    {isExpired(d.licenseExpiry) && ' (EXPIRED)'}
                                </span>
                            </div>
                            <div className="detail-row"><span>Trips</span><span>{d.completedTrips}/{d.tripCount} ({completionRate(d)}%)</span></div>
                        </div>
                        <div className="safety-score">
                            <div className="score-header"><Shield size={16} /><span>Safety Score</span><span className="score-value">{d.safetyScore}</span></div>
                            <div className="score-bar"><div className="score-fill" style={{ width: `${d.safetyScore}%`, backgroundColor: d.safetyScore >= 90 ? '#22c55e' : d.safetyScore >= 70 ? '#f59e0b' : '#ef4444' }}></div></div>
                        </div>
                        <div className="driver-card-footer">
                            <select className={`status-select status-${d.status === 'OnDuty' ? 'available' : d.status === 'OnTrip' ? 'dispatched' : d.status === 'Suspended' ? 'cancelled' : 'draft'}`}
                                value={d.status} onChange={(e) => handleStatusChange(d.id, e.target.value)} disabled={d.status === 'OnTrip'}>
                                <option value="OnDuty">On Duty</option><option value="OffDuty">Off Duty</option><option value="Suspended">Suspended</option>
                                {d.status === 'OnTrip' && <option value="OnTrip">On Trip</option>}
                            </select>
                        </div>
                    </div>
                ))}
            </div>
            {showModal && (
                <div className="modal-overlay" onClick={() => setShowModal(false)}>
                    <div className="modal" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header"><h3>{editing ? 'Edit Driver' : 'Add Driver'}</h3><button className="btn-icon" onClick={() => setShowModal(false)}><X size={20} /></button></div>
                        <form onSubmit={handleSubmit} className="modal-body">
                            <div className="form-grid">
                                <div className="form-group"><label>Full Name</label><input required value={form.fullName} onChange={(e) => setForm(f => ({ ...f, fullName: e.target.value }))} /></div>
                                <div className="form-group"><label>License Number</label><input required value={form.licenseNumber} onChange={(e) => setForm(f => ({ ...f, licenseNumber: e.target.value }))} /></div>
                                <div className="form-group"><label>License Expiry</label><input required type="date" value={form.licenseExpiry} onChange={(e) => setForm(f => ({ ...f, licenseExpiry: e.target.value }))} /></div>
                                <div className="form-group"><label>License Category</label>
                                    <select value={form.licenseCategory} onChange={(e) => setForm(f => ({ ...f, licenseCategory: e.target.value }))}>
                                        <option value="Truck">Truck</option><option value="Van">Van</option><option value="Bike">Bike</option>
                                    </select>
                                </div>
                                <div className="form-group"><label>Phone</label><input value={form.phone} onChange={(e) => setForm(f => ({ ...f, phone: e.target.value }))} /></div>
                            </div>
                            <div className="modal-footer"><button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancel</button><button type="submit" className="btn btn-primary">{editing ? 'Update' : 'Add Driver'}</button></div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
