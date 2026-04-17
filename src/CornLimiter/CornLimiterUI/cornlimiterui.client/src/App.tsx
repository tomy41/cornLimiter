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
            <div style={{ textAlign: 'center', padding: '50px' }}>
                <h2>Authenticating...</h2>
                <p>Please wait while we connect to the server.</p>
            </div>
        );
    }

    if (!token) {
        return (
            <div style={{ textAlign: 'center', padding: '50px' }}>
                <h2>Authentication Failed</h2>
                <p style={{ color: 'red' }}>{error || 'Unable to authenticate'}</p>
                <button 
                    onClick={initializeAuth} 
                    className="btn btn-primary"
                    style={{ marginTop: '20px' }}
                >
                    Retry
                </button>
            </div>
        );
    }

    const salesContents = sales === undefined
        ? <p>Sales loading...</p>
        : <div>
            <table className="table table-striped">
                <tbody>
                    <tr>
                        <td>Total count</td>
                        <td>{salesCount}</td>
                    </tr>
                    <tr>
                        <td>Latest sale on</td>
                        <td>{latestSale}</td>
                    </tr>
                </tbody>
            </table>
            <p>Sales History</p>
            <table className="table table-striped" aria-labelledby="tableLabel">
                <thead>
                    <tr>
                        <th>Id</th>
                        <th>Date</th>
                    </tr>
                </thead>
                <tbody>
                    {sales.map(sale =>
                        <tr key={sale.id}>
                            <td>{sale.id}</td>
                            <td>{sale.soldOnUtc}</td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>;

    return (
        <div>
            <h1 id="tableLabel">Farmers corn sales</h1>
            <div style={{ marginBottom: '20px' }}>
                <button 
                    onClick={handleBuyOne} 
                    disabled={isLoading}
                    className="btn btn-primary"
                >
                    {isLoading ? 'Processing...' : 'Buy one'}
                </button>
                {error && (
                    <div style={{ color: 'red', marginTop: '10px' }}>
                        {error}
                    </div>
                )}
            </div>
            {salesContents}
        </div>
    );
}

export default App;