<template>
  <div class="listening-view">
    <h1 class="listening-title">Continue Listening</h1>

    <div v-if="loading" class="listening-empty">Loading…</div>
    <div v-else-if="items.length === 0" class="listening-empty">
      Nothing in progress yet. Start a book from your
      <RouterLink to="/audiobooks">library</RouterLink>.
    </div>

    <div v-else class="listening-grid">
      <div v-for="item in items" :key="item.audiobookId" class="listening-card">
        <img
          v-if="coverFor(item.audiobookId)"
          :src="coverFor(item.audiobookId)"
          alt=""
          class="listening-cover"
          loading="lazy"
        />
        <div v-else class="listening-cover listening-cover--placeholder" aria-hidden="true" />
        <div class="listening-meta">
          <div class="listening-book-title" :title="titleFor(item.audiobookId)">
            {{ titleFor(item.audiobookId) }}
          </div>
          <div class="listening-pos">at {{ formatTime(item.positionSeconds) }}</div>
        </div>
        <button class="listening-resume" type="button" @click="resume(item.audiobookId)">
          <PhPlay weight="fill" />
          Resume
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { PhPlay } from '@phosphor-icons/vue'
import { useLibraryStore } from '@/stores/library'
import { getPlaceholderUrl } from '@/utils/placeholder'
import { useProtectedImages } from '@/composables/useProtectedImages'
import { playerApi } from './api'
import { usePlayerStore } from './store'
import type { ContinueListeningItem } from './types'

const library = useLibraryStore()
const player = usePlayerStore()
const { getProtectedImageSrc } = useProtectedImages()

const items = ref<ContinueListeningItem[]>([])
const loading = ref(true)

function bookFor(id: number) {
  return library.audiobooks.find((b) => b.id === id)
}
function titleFor(id: number): string {
  return bookFor(id)?.title || 'Unknown title'
}
function coverFor(id: number): string {
  const url = bookFor(id)?.imageUrl
  return url ? getProtectedImageSrc(url, getPlaceholderUrl()) : ''
}

function formatTime(sec: number): string {
  const s = Math.max(0, Math.floor(sec))
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  const ss = s % 60
  return `${h}:${String(m).padStart(2, '0')}:${String(ss).padStart(2, '0')}`
}

async function resume(id: number): Promise<void> {
  await player.load(id)
  player.playing = true
}

onMounted(async () => {
  try {
    if (library.audiobooks.length === 0) await library.fetchLibrary()
    items.value = await playerApi.getContinueListening()
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.listening-view {
  padding: 1.5rem;
}
.listening-title {
  font-size: 1.5rem;
  font-weight: 600;
  margin: 0 0 1rem;
}
.listening-empty {
  color: #9aa0a6;
}
.listening-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 1rem;
}
.listening-card {
  display: flex;
  flex-direction: column;
  background: #2a2a2a;
  border: 1px solid #3a3a3a;
  border-radius: 8px;
  overflow: hidden;
}
.listening-cover {
  width: 100%;
  aspect-ratio: 1 / 1;
  object-fit: cover;
}
.listening-cover--placeholder {
  background: #333;
}
.listening-meta {
  padding: 0.6rem 0.75rem 0.25rem;
  min-width: 0;
}
.listening-book-title {
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.listening-pos {
  font-size: 0.8rem;
  color: #9aa0a6;
}
.listening-resume {
  margin: 0.5rem 0.75rem 0.75rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  padding: 0.4rem 0.75rem;
  background: var(--brand-500, #2b7de9);
  color: #fff;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}
.listening-resume:hover {
  filter: brightness(1.1);
}
</style>
