import { useState, useEffect } from 'react';
import api from '../services/api';
import toast from 'react-hot-toast';
import { Plus, Trash2, X, Shield } from 'lucide-react';

export default function UserManagement() {
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showModal, setShowModal] = useState(false);
    const [form, setForm] = useState({ fullName: '', email: '', password: '', role: 'Dispatcher' });

    useEffect(() => { loadUsers(); }, []);

    const loadUsers = async () => {
        try {
            const { data } = await api.get('/auth/users');
            setUsers(data);
        } catch (err) { toast.error('Failed to load users'); }
        finally { setLoading(false); }
    };

    const handleCreate = async (e) => {
        e.preventDefault();
        try {
            await api.post('/auth/register', form);
            toast.success('User created successfully');
            setShowModal(false);
            setForm({ fullName: '', email: '', password: '', role: 'Dispatcher' });
            loadUsers();
        } catch (err) { toast.error(err.response?.data?.message || 'Error creating user'); }
    };

    const handleDelete = async (id) => {
        if (!confirm('Delete this user? This action cannot be undone.')) return;
        try {
            await api.delete(`/auth/users/${id}`);
            toast.success('User deleted');
            loadUsers();
        } catch (err) { toast.error(err.response?.data?.message || 'Error deleting user'); }
    };

    const getRoleBadgeClass = (role) => {
        const map = { Manager: 'status-dispatched', Dispatcher: 'status-draft', SafetyOfficer: 'status-amber', Analyst: 'status-completed' };
        return map[role] || '';
    };

    const getRoleLabel = (role) => {
        const map = { Manager: 'Manager', Dispatcher: 'Dispatcher', SafetyOfficer: 'Safety Officer', Analyst: 'Analyst' };
        return map[role] || role;
    };

    if (loading) return <div className="loading-spinner"><div className="spinner"></div></div>;

    return (
        <div className="page-content">
            <div className="page-header">
                <div>
                    <h2>User Management</h2>
                    <p>Manage system users and role assignments</p>
                </div>
                <button className="btn btn-primary" onClick={() => setShowModal(true)}><Plus size={18} /> Add User</button>
            </div>

            <div className="table-container">
                <table className="data-table">
                    <thead>
                        <tr>
                            <th>Name</th><th>Email</th><th>Role</th><th>Created</th><th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.length === 0 ? (
                            <tr><td colSpan="5" className="empty-cell">No users found</td></tr>
                        ) : users.map((u) => (
                            <tr key={u.id}>
                                <td className="font-medium">
                                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                                        <div className="driver-avatar" style={{ width: '32px', height: '32px', fontSize: '13px' }}>
                                            {u.fullName.split(' ').map(n => n[0]).join('')}
                                        </div>
                                        {u.fullName}
                                    </div>
                                </td>
                                <td>{u.email}</td>
                                <td><span className={`status-pill ${getRoleBadgeClass(u.role)}`}>{getRoleLabel(u.role)}</span></td>
                                <td>{new Date(u.createdAt).toLocaleDateString()}</td>
                                <td>
                                    <button className="btn-icon btn-danger" onClick={() => handleDelete(u.id)} title="Delete user">
                                        <Trash2 size={16} />
                                    </button>
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
                            <h3>Create New User</h3>
                            <button className="btn-icon" onClick={() => setShowModal(false)}><X size={20} /></button>
                        </div>
                        <form onSubmit={handleCreate} className="modal-body">
                            <div className="form-grid">
                                <div className="form-group">
                                    <label>Full Name</label>
                                    <input required value={form.fullName} onChange={(e) => setForm(f => ({ ...f, fullName: e.target.value }))} placeholder="e.g. John Smith" />
                                </div>
                                <div className="form-group">
                                    <label>Email</label>
                                    <input required type="email" value={form.email} onChange={(e) => setForm(f => ({ ...f, email: e.target.value }))} placeholder="e.g. john@fleet.com" />
                                </div>
                                <div className="form-group">
                                    <label>Password</label>
                                    <input required type="password" minLength="6" value={form.password} onChange={(e) => setForm(f => ({ ...f, password: e.target.value }))} placeholder="Min 6 characters" />
                                </div>
                                <div className="form-group">
                                    <label>Role</label>
                                    <select value={form.role} onChange={(e) => setForm(f => ({ ...f, role: e.target.value }))}>
                                        <option value="Manager">Manager</option>
                                        <option value="Dispatcher">Dispatcher</option>
                                        <option value="SafetyOfficer">Safety Officer</option>
                                        <option value="Analyst">Analyst</option>
                                    </select>
                                </div>
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancel</button>
                                <button type="submit" className="btn btn-primary">Create User</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
