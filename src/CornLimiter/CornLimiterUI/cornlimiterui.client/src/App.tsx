import { useEffect, useState } from 'react';
import './App.css';

interface Sale {
    id: number;
    soldOnUtc: string;
}

interface TokenResponse {
    token: string;
}

function App() {
    const [sales, setSales] = useState<Sale[]>();
    const [salesCount, setSalesCount] = useState<number>();
    const [latestSale, setLatestSale] = useState<string>();
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [token, setToken] = useState<string | null>(null);
    const [isAuthenticating, setIsAuthenticating] = useState(true);
    const farmerCode = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

    useEffect(() => {
        initializeAuth();
    }, []);

    useEffect(() => {
        if (token) {
            populateFarmerSellingsData(farmerCode);
        }
    }, [token]);

    const initializeAuth = async () => {
        setIsAuthenticating(true);
        setError(null);

        try {
            const response = await fetch('api/Auth/Token');
            
            if (response.ok) {
                const data: TokenResponse = await response.json();
                setToken(data.token);
            } else {
                setError('Failed to obtain authentication token');
            }
        } catch (err) {
            setError('Error connecting to authentication service');
            console.error('Auth error:', err);
        } finally {
            setIsAuthenticating(false);
        }
    };

    const handleBuyOne = async () => {
        if (!token) {
            setError('No authentication token available');
            return;
        }

        setIsLoading(true);
        setError(null);

        try {
            const response = await fetch('api/v1/Sale/SellOne', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    farmerCode: farmerCode
                })
            });

            if (response.ok) {
                await populateFarmerSellingsData(farmerCode);
            } else if (response.status === 401) {
                setError('Authentication expired. Refreshing token...');
                await initializeAuth();
            } else if (response.status === 429) {
                const errorMessage = await response.text();
                setError(errorMessage || 'Rate limit exceeded. Please try again later.');
            } else {
                setError(`Error: ${response.status} - ${response.statusText}`);
            }
        } catch (err) {
            setError('An error occurred while processing your request.');
            console.error('Buy error:', err);
        } finally {
            setIsLoading(false);
        }
    };

    async function populateFarmerSellingsData(farmerCode: string) {
        if (!token) {
            return;
        }

        try {
            const response = await fetch('api/v1/Sale/ListByFarmer/' + farmerCode, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                const data = await response.json();
                setSales(data.sales);
                setLatestSale(data.latest);
                setSalesCount(data.total);
            } else if (response.status === 401) {
                setError('Authentication expired. Refreshing token...');
                await initializeAuth();
            } else {
                setError(`Failed to load sales: ${response.status}`);
            }
        } catch (err) {
            setError('Error loading sales data');
            console.error('Load error:', err);
        }
    }

    if (isAuthenticating) {
        return (
            <div className="container">
                <div className="row justify-content-center align-items-center min-vh-100">
                    <div className="col-md-6 text-center">
                        <div className="spinner-border text-primary mb-3" role="status" style={{ width: '3rem', height: '3rem' }}>
                            <span className="visually-hidden">Loading...</span>
                        </div>
                        <h2 className="mb-3">Authenticating...</h2>
                        <p className="text-muted">Please wait while we connect to the server.</p>
                    </div>
                </div>
            </div>
        );
    }

    if (!token) {
        return (
            <div className="container">
                <div className="row justify-content-center align-items-center min-vh-100">
                    <div className="col-md-6">
                        <div className="card shadow-lg">
                            <div className="card-body text-center p-5">
                                <i className="bi bi-exclamation-triangle text-danger" style={{ fontSize: '4rem' }}></i>
                                <h2 className="card-title mt-3 mb-3">Authentication Failed</h2>
                                <div className="alert alert-danger" role="alert">
                                    {error || 'Unable to authenticate'}
                                </div>
                                <button 
                                    onClick={initializeAuth} 
                                    className="btn btn-primary btn-lg"
                                >
                                    <i className="bi bi-arrow-clockwise me-2"></i>
                                    Retry
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    const salesContents = sales === undefined
        ? (
            <div className="text-center py-5">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
                <p className="mt-3 text-muted">Loading sales data...</p>
            </div>
        )
        : (
            <div className="row">
                <div className="col-12">
                    <div className="card shadow-sm mb-4">
                        <div className="card-header bg-primary text-white">
                            <h5 className="mb-0">
                                <i className="bi bi-graph-up me-2"></i>
                                Sales Summary
                            </h5>
                        </div>
                        <div className="card-body">
                            <div className="row text-center">
                                <div className="col-md-6 mb-3 mb-md-0">
                                    <div className="border-end">
                                        <h6 className="text-muted">Total Sales</h6>
                                        <h2 className="text-primary">{salesCount}</h2>
                                    </div>
                                </div>
                                <div className="col-md-6">
                                    <h6 className="text-muted">Latest Sale</h6>
                                    <h5 className="text-success">
                                        {latestSale ? new Date(latestSale).toLocaleString() : 'N/A'}
                                    </h5>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="card shadow-sm">
                        <div className="card-header bg-success text-white">
                            <h5 className="mb-0">
                                <i className="bi bi-clock-history me-2"></i>
                                Sales History
                            </h5>
                        </div>
                        <div className="card-body p-0">
                            {sales.length === 0 ? (
                                <div className="text-center py-5">
                                    <i className="bi bi-inbox text-muted" style={{ fontSize: '3rem' }}></i>
                                    <p className="text-muted mt-3">No sales recorded yet</p>
                                </div>
                            ) : (
                                <div className="table-responsive">
                                    <table className="table table-hover table-striped mb-0">
                                        <thead className="table-light">
                                            <tr>
                                                <th scope="col" className="text-center">#</th>
                                                <th scope="col">Sale ID</th>
                                                <th scope="col">Date & Time</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {sales.map((sale, index) =>
                                                <tr key={sale.id}>
                                                    <td className="text-center">{index + 1}</td>
                                                    <td>
                                                        <span className="badge bg-info">{sale.id}</span>
                                                    </td>
                                                    <td>
                                                        <i className="bi bi-calendar-event me-2"></i>
                                                        {new Date(sale.soldOnUtc).toLocaleString()}
                                                    </td>
                                                </tr>
                                            )}
                                        </tbody>
                                    </table>
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        );

    return (
        <div className="min-vh-100 bg-light">
            <nav className="navbar navbar-dark bg-dark shadow-sm mb-4">
                <div className="container">
                    <span className="navbar-brand mb-0 h1">
                        <i className="bi bi-shop me-2"></i>
                        Corn Farmers Sales
                    </span>
                    <span className="badge bg-success">Connected</span>
                </div>
            </nav>

            <div className="container pb-5">
                <div className="row mb-4">
                    <div className="col-12">
                        <div className="d-flex justify-content-between align-items-center">
                            <div>
                                <p className="text-muted mb-0">
                                    <small>Farmer ID: {farmerCode}</small>
                                </p>
                            </div>
                            <button 
                                onClick={handleBuyOne} 
                                disabled={isLoading}
                                className="btn btn-success btn-lg shadow"
                            >
                                {isLoading ? (
                                    <>
                                        <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                                        Processing...
                                    </>
                                ) : (
                                    <>
                                        <i className="bi bi-bag-plus me-2"></i>
                                        Buy One
                                    </>
                                )}
                            </button>
                        </div>
                    </div>
                </div>

                {error && (
                    <div className="alert alert-danger alert-dismissible fade show shadow-sm" role="alert">
                        <i className="bi bi-exclamation-circle me-2"></i>
                        <strong>Error:</strong> {error}
                        <button 
                            type="button" 
                            className="btn-close" 
                            onClick={() => setError(null)}
                            aria-label="Close"
                        ></button>
                    </div>
                )}

                {salesContents}
            </div>
        </div>
    );
}

export default App;