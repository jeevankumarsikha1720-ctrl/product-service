import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import App from "./App.tsx";
import { CartProvider } from "./cart/CartContext.tsx";
import { AuthProvider } from "./auth/AuthContext.tsx";
import "./index.css";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
});

// Provider order matters:
//   AuthProvider outermost  — login state survives query cache resets
//   QueryClientProvider     — needs to see auth so 401 invalidations can flow
//   CartProvider            — cart calls API; needs auth context if we later
//                             require auth for cart endpoints
//   BrowserRouter           — innermost so all providers are available to routes
createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <AuthProvider>
      <QueryClientProvider client={queryClient}>
        <CartProvider>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </CartProvider>
      </QueryClientProvider>
    </AuthProvider>
  </StrictMode>,
);
