import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Truck, ArrowLeft, Mail, KeyRound, CheckCircle } from 'lucide-react';
import axios from 'axios';
import toast from 'react-hot-toast';

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export default function ForgotPassword() {
    const [step, setStep] = useState('email'); // email | token | done
    const [email, setEmail] = useState('');
    const [token, setToken] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [loading, setLoading] = useState(false);
    const [resetToken, setResetToken] = useState('');

    const handleForgotPassword = async (e) => {
        e.preventDefault();
        setLoading(true);
        try {
            const { data } = await axios.post(`${API_BASE}/auth/forgot-password`, { email });
            if (data.token) {
                setResetToken(data.token);
                setToken(data.token);
            }
            toast.success('Reset token generated!');
            setStep('token');
        } catch (err) {
            toast.error(err.response?.data?.message || 'Something went wrong');
        } finally { setLoading(false); }
    };

    const handleResetPassword = async (e) => {
        e.preventDefault();
        if (newPassword !== confirmPassword) {
            toast.error('Passwords do not match');
            return;
        }
        setLoading(true);
        try {
            await axios.post(`${API_BASE}/auth/reset-password`, { token, newPassword });
            toast.success('Password reset successfully!');
            setStep('done');
        } catch (err) {
            toast.error(err.response?.data?.message || 'Invalid or expired token');
        } finally { setLoading(false); }
    };

    return (
        <div className="login-page">
            <div className="login-container">
                <div className="login-card">
                    <div className="login-header">
                        <div className="login-logo"><Truck size={32} /></div>
                        <h1>FleetFlow</h1>
                        <p>Password Recovery</p>
                    </div>

                    {step === 'email' && (
                        <form onSubmit={handleForgotPassword} className="login-form">
                            <div className="form-group">
                                <label><Mail size={16} /> Email Address</label>
                                <input
                                    type="email" required
                                    placeholder="Enter your registered email"
                                    value={email} onChange={(e) => setEmail(e.target.value)}
                                />
                            </div>
                            <button type="submit" className="btn btn-primary btn-full" disabled={loading}>
                                {loading ? 'Sending...' : 'Generate Reset Token'}
                            </button>
                        </form>
                    )}

                    {step === 'token' && (
                        <>
                            {resetToken && (
                                <div className="info-banner" style={{
                                    background: 'rgba(59, 130, 246, 0.1)', borderRadius: '10px',
                                    padding: '12px 16px', margin: '0 0 20px', border: '1px solid rgba(59, 130, 246, 0.3)'
                                }}>
                                    <p style={{ fontSize: '12px', color: '#94a3b8', margin: '0 0 6px' }}>
                                        🔑 Demo Mode — Token (auto-filled):
                                    </p>
                                    <code style={{ fontSize: '11px', color: '#3b82f6', wordBreak: 'break-all' }}>{resetToken}</code>
                                </div>
                            )}
                            <form onSubmit={handleResetPassword} className="login-form">
                                <div className="form-group">
                                    <label><KeyRound size={16} /> Reset Token</label>
                                    <input
                                        required placeholder="Paste your reset token"
                                        value={token} onChange={(e) => setToken(e.target.value)}
                                    />
                                </div>
                                <div className="form-group">
                                    <label>New Password</label>
                                    <input
                                        type="password" required minLength="6"
                                        placeholder="Min 6 characters"
                                        value={newPassword} onChange={(e) => setNewPassword(e.target.value)}
                                    />
                                </div>
                                <div className="form-group">
                                    <label>Confirm Password</label>
                                    <input
                                        type="password" required minLength="6"
                                        placeholder="Re-enter new password"
                                        value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)}
                                    />
                                </div>
                                <button type="submit" className="btn btn-primary btn-full" disabled={loading}>
                                    {loading ? 'Resetting...' : 'Reset Password'}
                                </button>
                            </form>
                        </>
                    )}

                    {step === 'done' && (
                        <div style={{ textAlign: 'center', padding: '20px 0' }}>
                            <CheckCircle size={48} style={{ color: '#22c55e', marginBottom: '16px' }} />
                            <h3 style={{ margin: '0 0 8px', color: '#e2e8f0' }}>Password Reset!</h3>
                            <p style={{ color: '#94a3b8', margin: '0 0 20px' }}>
                                Your password has been changed. You can now log in with your new password.
                            </p>
                            <Link to="/login" className="btn btn-primary">Go to Login</Link>
                        </div>
                    )}

                    {step !== 'done' && (
                        <div className="login-footer" style={{ marginTop: '16px' }}>
                            <Link to="/login" style={{ color: '#3b82f6', textDecoration: 'none', display: 'flex', alignItems: 'center', gap: '6px', justifyContent: 'center' }}>
                                <ArrowLeft size={16} /> Back to Login
                            </Link>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
