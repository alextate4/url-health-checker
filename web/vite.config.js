import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    // The ASP.NET Core server serves these files, so one `dotnet run` gives UI and API.
    outDir: '../server/wwwroot',
    // outDir is outside web/, so Vite will not empty it unless we say so.
    emptyOutDir: true,
  },
  server: {
    // During `npm run dev`, API calls go to the running C# server.
    proxy: { '/api': 'http://localhost:5080' },
  },
})
