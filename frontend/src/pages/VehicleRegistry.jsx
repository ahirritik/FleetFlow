import { useState, useEffect } from 'react';
import api from '../services/api';
import toast from 'react-hot-toast';
import { Plus, Pencil, Trash2, X } from 'lucide-react';

export default function VehicleRegistry() {
    const [vehicles, setVehicles] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showModal, setShowModal] = useState(false);
    const [editing, setEditing] = useState(null);
    const [filters, setFilters] = useState({ type: '', status: '' });
    const [form, setForm] = useState({
        name: '', model: '', licensePlate: '', type: 'Truck',
        maxCapacity: '', odometer: '', region: '', acquisitionCost: ''
    });

    useEffect(() => { loadVehicles(); }, [filters]);

    const loadVehicles = async () => {
        try {
            const params = {};
            if (filters.type) params.type = filters.type;
            if (filters.status) params.status = filters.status;
            const { data } = await api.get('/vehicles', { params });
            setVehicles(data);
        } catch (err) { toast.error('Failed to load vehicles'); }
        finally { setLoading(false); }
    };

    const resetForm = () => {
        setForm({ name: '', model: '', licensePlate: '', type: 'Truck', maxCapacity: '', odometer: '', region: '', acquisitionCost: '' });
        setEditing(null);
    };

    const openCreate = () => { resetForm(); setShowModal(true); };
    const openEdit = (v) => {
        setEditing(v.id);
        setForm({
            name: v.name, model: v.model, licensePlate: v.licensePlate, type: v.type,
            maxCapacity: v.maxCapacity, odometer: v.odometer, region: v.region, acquisitionCost: v.acquisitionCost
        });
        setShowModal(true);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            if (editing) {
                await api.put(`/vehicles/${editing}`, { ...form, maxCapacity: +form.maxCapacity, acquisitionCost: +form.acquisitionCost });
                toast.success('Vehicle updated');
            } else {
                await api.post('/vehicles', { ...form, maxCapacity: +form.maxCapacity, odometer: +form.odometer, acquisitionCost: +form.acquisitionCost });
                toast.success('Vehicle created');
            }
            setShowModal(false);
            loadVehicles();
        } catch (err) { toast.error(err.response?.data?.message || 'Error saving vehicle'); }
    };

    const handleDelete = async (id) => {
        if (!confirm('Delete this vehicle?')) return;
        try { await api.delete(`/vehicles/${id}`); toast.success('Vehicle deleted'); loadVehicles(); }
        catch (err) { toast.error(err.response?.data?.message || 'Error deleting'); }
    };

    const handleStatusChange = async (id, status) => {
        try { await api.patch(`/vehicles/${id}/status`, { status }); toast.success('Status updated'); loadVehicles(); }
        catch (err) { toast.error(err.response?.data?.message || 'Error updating status'); }
    };

    const getStatusClass = (s) => {
        const map = { Available: 'status-available', OnTrip: 'status-dispatched', InShop: 'status-amber', Retired: 'status-cancelled' };
        return map[s] || '';
    };

    if (loading) return <div className="loading-spinner"><div className="spinner"></div></div>;

    return (
        <div className="page-content">
            <div className="page-header">
                <div>
                    <h2>Vehicle Registry</h2>
                    <p>Manage your fleet assets</p>
                </div>
                <button className="btn btn-primary" onClick={openCreate}><Plus size={18} /> Add Vehicle</button>
            </div>

            <div className="filters-bar">
                <select value={filters.type} onChange={(e) => setFilters(f => ({ ...f, type: e.target.value }))}>
                    <option value="">All Types</option>
                    <option value="Truck">Truck</option>
                    <option value="Van">Van</option>
                    <option value="Bike">Bike</option>
                </select>
                <select value={filters.status} onChange={(e) => setFilters(f => ({ ...f, status: e.target.value }))}>
                    <option value="">All Statuses</option>
                    <option value="Available">Available</option>
                    <option value="OnTrip">On Trip</option>
                    <option value="InShop">In Shop</option>
                    <option value="Retired">Retired</option>
                </select>
            </div>

            <div className="table-container">
                <table className="data-table">
                    <thead>
                        <tr>
                            <th>Name</th><th>Model</th><th>Plate</th><th>Type</th>
                            <th>Capacity</th><th>Odometer</th><th>Region</th><th>Status</th><th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {vehicles.length === 0 ? (
                            <tr><td colSpan="9" className="empty-cell">No vehicles found</td></tr>
                        ) : vehicles.map((v) => (
                            <tr key={v.id}>
                                <td className="font-medium">{v.name}</td>
                                <td>{v.model}</td>
                                <td><code>{v.licensePlate}</code></td>
                                <td>{v.type}</td>
                                <td>{v.maxCapacity.toLocaleString()} kg</td>
                                <td>{v.odometer.toLocaleString()} km</td>
                                <td>{v.region}</td>
                                <td>
                                    <select
                                        className={`status-select ${getStatusClass(v.status)}`}
                                        value={v.status}
                                        onChange={(e) => handleStatusChange(v.id, e.target.value)}
                                        disabled={v.status === 'OnTrip'}
                                    >
                                        <option value="Available">Available</option>
                                        <option value="InShop">In Shop</option>
                                        <option value="Retired">Retired</option>
                                        {v.status === 'OnTrip' && <option value="OnTrip">On Trip</option>}
                                    </select>
                                </td>
                                <td>
                                    <div className="action-buttons">
                                        <button className="btn-icon" onClick={() => openEdit(v)} title="Edit"><Pencil size={16} /></button>
                                        <button className="btn-icon btn-danger" onClick={() => handleDelete(v.id)} title="Delete"><Trash2 size={16} /></button>
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {showModal && (
                <div className="modal-overlay" onClick={() => setShowModal(false)}>
                    <div className="modal" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>{editing ? 'Edit Vehicle' : 'Add Vehicle'}</h3>
                            <button className="btn-icon" onClick={() => setShowModal(false)}><X size={20} /></button>
                        </div>
                        <form onSubmit={handleSubmit} className="modal-body">
                            <div className="form-grid">
                                <div className="form-group">
                                    <label>Name</label>
                                    <input required value={form.name} onChange={(e) => setForm(f => ({ ...f, name: e.target.value }))} placeholder="e.g. Truck-01" />
                                </div>
                                <div className="form-group">
                                    <label>Model</label>
                                    <input required value={form.model} onChange={(e) => setForm(f => ({ ...f, model: e.target.value }))} placeholder="e.g. Volvo FH16" />
                                </div>
                                <div className="form-group">
                                    <label>License Plate</label>
                                    <input required value={form.licensePlate} onChange={(e) => setForm(f => ({ ...f, licensePlate: e.target.value }))} placeholder="e.g. TRK-1001" />
                                </div>
                                <div className="form-group">
                                    <label>Type</label>
                                    <select value={form.type} onChange={(e) => setForm(f => ({ ...f, type: e.target.value }))}>
                                        <option value="Truck">Truck</option>
                                        <option value="Van">Van</option>
                                        <option value="Bike">Bike</option>
                                    </select>
                                </div>
                                <div className="form-group">
                                    <label>Max Capacity (kg)</label>
                                    <input required type="number" min="0" value={form.maxCapacity} onChange={(e) => setForm(f => ({ ...f, maxCapacity: e.target.value }))} />
                                </div>
                                {!editing && (
                                    <div className="form-group">
                                        <label>Odometer (km)</label>
                                        <input type="number" min="0" value={form.odometer} onChange={(e) => setForm(f => ({ ...f, odometer: e.target.value }))} />
                                    </div>
                                )}
                                <div className="form-group">
                                    <label>Region</label>
                                    <input value={form.region} onChange={(e) => setForm(f => ({ ...f, region: e.target.value }))} placeholder="e.g. North" />
                                </div>
                                <div className="form-group">
                                    <label>Acquisition Cost ($)</label>
                                    <input type="number" min="0" value={form.acquisitionCost} onChange={(e) => setForm(f => ({ ...f, acquisitionCost: e.target.value }))} />
                                </div>
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancel</button>
                                <button type="submit" className="btn btn-primary">{editing ? 'Update' : 'Create'}</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
