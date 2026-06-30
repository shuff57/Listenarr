import type { ListenarrPlugin } from '@/plugins'
import { PhHeadphones } from '@phosphor-icons/vue'
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
  nav: [{ label: 'Listen', to: '/listening', icon: PhHeadphones }],
}
