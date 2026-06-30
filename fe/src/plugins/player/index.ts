import type { ListenarrPlugin } from '@/plugins'
import { PhHeadphones } from '@phosphor-icons/vue'
import AudioPlayer from './AudioPlayer.vue'
import PlayerLibraryView from './PlayerLibraryView.vue'
import PlayerBookView from './PlayerBookView.vue'

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
    {
      path: '/listen/:id',
      name: 'listen-book',
      component: PlayerBookView,
      meta: { requiresAuth: true },
    },
  ],
  nav: [{ label: 'Listen', to: '/listening', icon: PhHeadphones }],
}
