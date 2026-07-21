import { defineConfig, loadEnv, type Plugin } from 'vite'
import vue from '@vitejs/plugin-vue'

// SignalR 10 ships misplaced pure annotations that Rolldown rejects (fixed in aspnetcore 11).
function fixSignalrPureAnnotations(): Plugin {
  return {
    name: 'fix-signalr-pure-annotations',
    transform(code, id) {
      if (!id.includes('@microsoft/signalr')) {
        return
      }

      if (!code.includes('/*#__PURE__*/ function')) {
        return
      }

      return {
        code: code.replace(/\/\*#__PURE__\*\/ function/g, 'function'),
        map: null
      }
    }
  }
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const base = env.VITE_BASE_PATH || '/'

  return {
    base,
    plugins: [fixSignalrPureAnnotations(), vue()],
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: 'http://localhost:5000',
          changeOrigin: true
        },
        '/hubs': {
          target: 'http://localhost:5000',
          ws: true,
          changeOrigin: true
        }
      }
    }
  }
})
