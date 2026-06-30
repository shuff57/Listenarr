// Standalone build for the player plugin: emits a single IIFE bundle (player.js + player.css)
// that the host loads at runtime. vue / vue-router / pinia are externalized to the host's
// window.LISTENARR_* globals so the plugin shares the host's single framework instance.
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  define: { 'process.env.NODE_ENV': '"production"' },
  build: {
    outDir: 'dist-plugins/player',
    emptyOutDir: true,
    cssCodeSplit: false,
    copyPublicDir: false, // don't drag the host's public/ assets into the plugin package
    lib: {
      entry: fileURLToPath(new URL('./src/plugins/player/register.ts', import.meta.url)),
      name: 'ListenarrPlayerPlugin',
      formats: ['iife'],
      fileName: () => 'player.js',
    },
    rollupOptions: {
      external: ['vue', 'vue-router', 'pinia'],
      output: {
        globals: {
          vue: 'LISTENARR_VUE',
          'vue-router': 'LISTENARR_VUE_ROUTER',
          pinia: 'LISTENARR_PINIA',
        },
        assetFileNames: 'player.[ext]',
      },
    },
  },
})
