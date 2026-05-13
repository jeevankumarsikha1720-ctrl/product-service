import { useState } from "react";
import { Route, Routes } from "react-router-dom";
import { StorePage } from "./store/StorePage";
import { AdminProductsPage } from "./admin/AdminProductsPage";
import { NavBar } from "./shared/NavBar";
import { CartDrawer } from "./cart/CartDrawer";
import { LoginPage } from "./auth/LoginPage";
import { RequireAdmin } from "./auth/RequireAdmin";

export default function App() {
  const [cartOpen, setCartOpen] = useState(false);

  return (
    <div className="flex min-h-screen flex-col">
      <NavBar onOpenCart={() => setCartOpen(true)} />

      <main className="flex-1">
        <Routes>
          <Route path="/" element={<StorePage />} />
          <Route path="/login" element={<LoginPage />} />

          {/* Admin is gated behind RequireAdmin - unauthenticated users get
              bounced to /login and bounced back after a successful login. */}
          <Route
            path="/admin"
            element={
              <RequireAdmin>
                <AdminProductsPage />
              </RequireAdmin>
            }
          />

          <Route
            path="*"
            element={
              <div className="mx-auto max-w-7xl px-6 py-20 text-center">
                <h1 className="text-3xl font-bold text-slate-900">Page not found</h1>
                <p className="mt-2 text-slate-500">
                  The page you were looking for doesn't exist.
                </p>
              </div>
            }
          />
        </Routes>
      </main>

      <CartDrawer open={cartOpen} onClose={() => setCartOpen(false)} />
    </div>
  );
}
