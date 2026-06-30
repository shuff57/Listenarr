import type { ListenarrPlugin } from '@/plugins'
import AudioPlayer from './AudioPlayer.vue'
import PlayerLibraryView from './PlayerLibraryView.vue'

export const playerPlugin: ListenarrPlugin = {
  id: 'player',
  root: AudioPlayer,
  routes: [
    {
      path: '/listening',
      name: 'listening',
      component: PlayerLibraryView,
      meta: { requiresAuth: true },
    },
  ],
}
