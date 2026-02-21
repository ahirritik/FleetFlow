import { useState, useEffect } from 'react';
import api from '../services/api';
import { History, Filter, ChevronLeft, ChevronRight, User } from 'lucide-react';
import toast from 'react-hot-toast';

export default function AuditTrail() {
    const [logs, setLogs] = useState([]);
    const [total, setTotal] = useState(0);
    const [page, setPage] = useState(1);
    const [entityType, setEntityType] = useState('');
    const [loading, setLoading] = useState(true);
    const pageSize = 20;

    useEffect(() => { loadLogs(); }, [page, entityType]);

    const loadLogs = async () => {
        setLoading(true);
        try {
            const params = `?page=${page}&pageSize=${pageSize}${entityType ? `&entityType=${entityType}` : ''}`;
            const res = await api.get(`/audit${params}`);
            setLogs(res.data.logs);
            setTotal(res.data.total);
        } catch (err) {
            console.error(err);
            toast.error('Failed to load audit logs');
        } finally {
            setLoading(false);
        }
    };

    const formatDate = (dateStr) => {
        const d = new Date(dateStr);
        return d.toLocaleString();
    };

    const getActionClass = (action) => {
        switch (action) {
            case 'Created': return 'log-created';
            case 'Deleted': return 'log-deleted';
            case 'Updated': return 'log-updated';
            case 'StatusChanged': return 'log-status';
            default: return 'log-default';
        }
    };

    const totalPages = Math.ceil(total / pageSize);

    if (loading && page === 1) return <div className="loading-spinner"><div className="spinner"></div></div>;

    return (
        <div className="page-content">
            <div className="page-header">
                <div>
                    <h2>Audit Trail</h2>
                    <p>System-wide activity log and transparency</p>
                </div>
                <div className="filters-bar">
                    <div className="filter-group" style={{ display: 'flex', alignItems: 'center', gap: '8px', background: 'var(--bg-white)', padding: '6px 14px', borderRadius: '8px', border: '1px solid var(--border-light)' }}>
                        <Filter size={16} color="var(--text-muted)" />
                        <select
                            value={entityType}
                            onChange={(e) => { setEntityType(e.target.value); setPage(1); }}
                            style={{ border: 'none', background: 'transparent', outline: 'none', color: 'var(--text-dark)', fontSize: '13px', cursor: 'pointer' }}
                        >
                            <option value="">All Entities</option>
                            <option value="Vehicle">Vehicles</option>
                            <option value="Driver">Drivers</option>
                            <option value="Trip">Trips</option>
                            <option value="MaintenanceLog">Maintenance</option>
                            <option value="Expense">Expenses</option>
                            <option value="User">Users</option>
                        </select>
                    </div>
                </div>
            </div>

            <div className="audit-timeline">
                {logs.length === 0 ? (
                    <div className="empty-state">
                        <History size={48} style={{ opacity: 0.2, margin: '0 auto 16px', color: 'var(--text-muted)' }} />
                        <p>No audit logs found matching your criteria</p>
                    </div>
                ) : (
                    <div className="timeline-container">
                        {logs.map((log) => (
                            <div key={log.id} className="timeline-item">
                                <div className="timeline-marker">
                                    <div className={`marker-dot ${getActionClass(log.action)}`}></div>
                                    <div className="marker-line"></div>
                                </div>
                                <div className="timeline-content">
                                    <div className="timeline-header">
                                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                                            <span className={`log-badge ${getActionClass(log.action)}`}>{log.action}</span>
                                            <span style={{ color: 'var(--text-muted)' }}>•</span>
                                            <span style={{ fontWeight: 600, fontSize: '14px' }}>{log.entityType} #{log.entityId}</span>
                                        </div>
                                        <span className="timeline-date">{formatDate(log.timestamp)}</span>
                                    </div>
                                    <p className="timeline-details">{log.details}</p>
                                    <div className="timeline-footer">
                                        <User size={14} color="var(--text-muted)" />
                                        <span>Performed by: <strong style={{ color: 'var(--text-dark)' }}>{log.userName}</strong></span>
                                        <span style={{ color: 'var(--text-muted)', marginLeft: '8px' }}>(ID: {log.userId})</span>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {totalPages > 1 && (
                <div className="pagination" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '16px', marginTop: '24px' }}>
                    <button
                        className="btn btn-secondary btn-sm"
                        disabled={page === 1}
                        onClick={() => setPage(p => p - 1)}
                    >
                        <ChevronLeft size={16} /> Previous
                    </button>
                    <span style={{ fontSize: '13px', color: 'var(--text-muted)' }}>Page {page} of {totalPages} ({total} total logs)</span>
                    <button
                        className="btn btn-secondary btn-sm"
                        disabled={page === totalPages}
                        onClick={() => setPage(p => p + 1)}
                    >
                        Next <ChevronRight size={16} />
                    </button>
                </div>
            )}
        </div>
    );
}
