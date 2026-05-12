import { useEffect, useState } from "react";
import axios from "axios";

type Product = {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  stockQuantity: number;
  onHand: number;
  reserved: number;
  isLowStock: boolean;
  isActive: boolean;
};

const API_URL =
  "https://productservice-api-jk2026.azurewebsites.net/api/products";

function App() {
  const [products, setProducts] = useState<Product[]>([]);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [price, setPrice] = useState(0);

  useEffect(() => {
    getProducts();
  }, []);

  async function getProducts() {
    const response = await axios.get(API_URL);
    setProducts(response.data.items ?? []);
  }

  async function createProduct() {
    await axios.post(API_URL, {
      name,
      description,
      price,
      currency: "USD",
      stockQuantity: 0,
    });

    setName("");
    setDescription("");
    setPrice(0);
    getProducts();
  }

  return (
    <div style={{ padding: "40px", fontFamily: "Arial" }}>
      <h1>Product Dashboard</h1>

      <div style={{ marginBottom: "20px" }}>
        <input placeholder="Name" value={name} onChange={(e) => setName(e.target.value)} />

        <input
          placeholder="Description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          style={{ marginLeft: "10px" }}
        />

        <input
          type="number"
          placeholder="Price"
          value={price}
          onChange={(e) => setPrice(Number(e.target.value))}
          style={{ marginLeft: "10px" }}
        />

        <button onClick={createProduct} style={{ marginLeft: "10px" }}>
          Create Product
        </button>
      </div>

      <table border={1} cellPadding={10} style={{ borderCollapse: "collapse", width: "100%" }}>
        <thead>
          <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Description</th>
            <th>Price</th>
            <th>Stock</th>
          </tr>
        </thead>

        <tbody>
          {products.map((product) => (
            <tr key={product.id}>
              <td>{product.id}</td>
              <td>{product.name}</td>
              <td>{product.description}</td>
              <td>${product.price}</td>
              <td>{product.onHand}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default App;