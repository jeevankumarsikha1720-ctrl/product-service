import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// API base URL is read from VITE_API_BASE_URL inside src/api.ts.
// Default in code points at the deployed Azure App Service.
// To override locally, create .env.local with: VITE_API_BASE_URL=http://localhost:5080/api
export default defineConfig({
  plugins: [react(), tailwindcss()],
});
