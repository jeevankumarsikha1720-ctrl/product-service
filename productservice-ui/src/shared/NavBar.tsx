import { NavLink } from "react-router-dom";
import { useCart } from "../cart/CartContext";

interface Props {
  onOpenCart: () => void;
}

export function NavBar({ onOpenCart }: Props) {
  const { itemCount } = useCart();

  return (
    <nav className="sticky top-0 z-40 border-b border-white/60 bg-white/70 backdrop-blur">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
        <NavLink to="/" className="flex items-center gap-2 text-lg font-bold text-slate-900">
          <span className="flex size-8 items-center justify-center rounded-lg bg-slate-900 text-sm font-black text-white">
            P
          </span>
          ProductService
        </NavLink>

        <div className="flex items-center gap-1">
          <NavLink
            to="/"
            end
            className={({ isActive }) =>
              `rounded-xl px-3 py-2 text-sm font-medium transition ${
                isActive
                  ? "bg-slate-900 text-white"
                  : "text-slate-600 hover:bg-slate-100"
              }`
            }
          >
            Store
          </NavLink>
          <NavLink
            to="/admin"
            className={({ isActive }) =>
              `rounded-xl px-3 py-2 text-sm font-medium transition ${
                isActive
                  ? "bg-slate-900 text-white"
                  : "text-slate-600 hover:bg-slate-100"
              }`
            }
          >
            Admin
          </NavLink>

          <button
            onClick={onOpenCart}
            className="ml-2 relative inline-flex items-center gap-2 rounded-xl bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-slate-800"
          >
            🛒 Cart
            {itemCount > 0 && (
              <span className="inline-flex min-w-5 items-center justify-center rounded-full bg-blue-500 px-1.5 py-0.5 text-xs font-bold">
                {itemCount}
              </span>
            )}
          </button>
        </div>
      </div>
    </nav>
  );
}
