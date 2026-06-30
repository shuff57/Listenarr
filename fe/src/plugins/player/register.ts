// IIFE entrypoint for the standalone player bundle. Runs when the host injects this script
// (after the SDK is installed) and self-registers the plugin with the host.
import { playerPlugin } from './index'

const host = (window as unknown as { LISTENARR?: { registerPlugin: (p: unknown) => void } }).LISTENARR
if (host?.registerPlugin) {
  host.registerPlugin(playerPlugin)
} else {
  console.error('[player] window.LISTENARR not present — host SDK missing; cannot register')
}
