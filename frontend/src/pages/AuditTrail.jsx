import { useState, useEffect } from 'react';
import api from '../services/api';
import { History, Search, Filter, ChevronLeft, ChevronRight, User, Database, Activity } from 'lucide-react';
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
            case 'Created': return 'text-green-500';
            case 'Deleted': return 'text-red-500';
            case 'Updated': return 'text-blue-500';
            case 'StatusChanged': return 'text-amber-500';
            default: return 'text-muted';
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
                    <div className="search-box">
                        <Filter size={18} />
                        <select
                            value={entityType}
                            onChange={(e) => { setEntityType(e.target.value); setPage(1); }}
                            className="bg-transparent border-none outline-none text-sm ml-2"
                        >
                            <option value="">All Entities</option>
                            <option value="Vehicle">Vehicles</option>
                            <option value="Driver">Drivers</option>
                            <option value="Trip">Trips</option>
                            <option value="MaintenanceLog">Maintenance</option>
                            <option value="Expense">Expenses</option>
                        </select>
                    </div>
                </div>
            </div>

            <div className="audit-timeline">
                {logs.length === 0 ? (
                    <div className="empty-state">
                        <History size={48} className="text-muted mb-4 opacity-20" />
                        <p>No audit logs found matching your criteria</p>
                    </div>
                ) : (
                    <div className="timeline-container">
                        {logs.map((log) => (
                            <div key={log.id} className="timeline-item">
                                <div className="timeline-marker">
                                    <div className="marker-dot"></div>
                                    <div className="marker-line"></div>
                                </div>
                                <div className="timeline-content">
                                    <div className="timeline-header">
                                        <div className="flex items-center gap-2">
                                            <span className={`font-bold ${getActionClass(log.action)}`}>{log.action}</span>
                                            <span className="text-muted">•</span>
                                            <span className="font-medium">{log.entityType} #{log.entityId}</span>
                                        </div>
                                        <span className="timeline-date">{formatDate(log.timestamp)}</span>
                                    </div>
                                    <p className="timeline-details">{log.details}</p>
                                    <div className="timeline-footer">
                                        <User size={14} className="text-muted" />
                                        <span>Performed by: <strong>{log.userName}</strong></span>
                                        <span className="text-muted ml-2">(ID: {log.userId})</span>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {totalPages > 1 && (
                <div className="pagination">
                    <button
                        className="btn btn-secondary btn-sm"
                        disabled={page === 1}
                        onClick={() => setPage(p => p - 1)}
                    >
                        <ChevronLeft size={16} /> Previous
                    </button>
                    <span className="text-sm">Page {page} of {totalPages} ({total} total logs)</span>
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
