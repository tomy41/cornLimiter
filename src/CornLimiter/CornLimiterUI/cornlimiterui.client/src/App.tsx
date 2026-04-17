import { useEffect, useState } from 'react';
import './App.css';

interface Sale {
    id: number;
    soldOnUtc: string;
}

function App() {
    const [sales, setSales] = useState<Sale[]>();
    const [salesCount, setSalesCount] = useState<number>();
    const [latestSale, setLatestSale] = useState<string>();
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const farmerCode = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

    useEffect(() => {
        populateFarmerSellingsData(farmerCode);
    }, []);

    const handleBuyOne = async () => {
        setIsLoading(true);
        setError(null);

        try {
            const response = await fetch('api/v1/Sale/SellOne', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    farmerCode: farmerCode
                })
            });

            if (response.ok) {
                await populateFarmerSellingsData(farmerCode);
            } else if (response.status === 429) {
                const errorMessage = await response.text();
                setError(errorMessage || 'Rate limit exceeded. Please try again later.');
            } else {
                setError(`Error: ${response.status} - ${response.statusText}`);
            }
        } catch (err) {
            setError('An error occurred while processing your request.');
        } finally {
            setIsLoading(false);
        }
    };

    const salesContents = sales === undefined
        ? <p>Sales loading</p>
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

    async function populateFarmerSellingsData(farmerCode: string) {
        const response = await fetch('api/v1/Sale/ListByFarmer/' + farmerCode);
        if (response.ok) {
            const data = await response.json();
            setSales(data.sales);
            setLatestSale(data.latest);
            setSalesCount(data.total);
        }
    }
}

export default App;