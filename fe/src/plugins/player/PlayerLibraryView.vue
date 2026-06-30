<template>
  <div class="listening-view">
    <h1 class="listening-title">Listen</h1>

    <div v-if="loading" class="listening-empty">Loading…</div>

    <template v-else>
      <!-- Continue Listening shelf -->
      <section v-if="continueItems.length > 0" class="listening-section">
        <h2 class="listening-subtitle">Continue Listening</h2>
        <div class="listening-grid">
          <article v-for="item in continueItems" :key="'c' + item.audiobookId" class="listening-card">
            <img v-if="coverFor(item.audiobookId)" :src="coverFor(item.audiobookId)" alt="" class="listening-cover" loading="lazy" />
            <div v-else class="listening-cover listening-cover--ph" aria-hidden="true" />
            <div class="listening-meta">
              <div class="listening-name" :title="titleFor(item.audiobookId)">{{ titleFor(item.audiobookId) }}</div>
              <div class="listening-pos">at {{ formatTime(item.positionSeconds) }}</div>
            </div>
            <button class="listening-play" type="button" @click="play(item.audiobookId)">
              <PhPlay weight="fill" /> Resume
            </button>
          </article>
        </div>
      </section>

      <!-- Full library -->
      <section class="listening-section">
        <h2 class="listening-subtitle">Library</h2>
        <div v-if="books.length === 0" class="listening-empty">
          No audiobooks yet. Add a root folder and scan from your
          <RouterLink to="/audiobooks">library</RouterLink>.
        </div>
        <div v-else class="listening-grid">
          <article v-for="book in books" :key="book.id" class="listening-card">
            <img v-if="coverFor(book.id)" :src="coverFor(book.id)" alt="" class="listening-cover" loading="lazy" />
            <div v-else class="listening-cover listening-cover--ph" aria-hidden="true" />
            <div class="listening-meta">
              <div class="listening-name" :title="book.title || 'Unknown'">{{ book.title || 'Unknown' }}</div>
            </div>
            <button class="listening-play" type="button" @click="play(book.id)">
              <PhPlay weight="fill" /> Play
            </button>
          </article>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { PhPlay } from '@phosphor-icons/vue'
import { useLibraryStore, getPlaceholderUrl, useProtectedImages } from './sdk'
import { playerApi } from './api'
import { usePlayerStore } from './store'
import type { ContinueListeningItem } from './types'

const library = useLibraryStore()
const player = usePlayerStore()
const { getProtectedImageSrc } = useProtectedImages()

const continueItems = ref<ContinueListeningItem[]>([])
const loading = ref(true)

const books = computed(() => library.audiobooks)

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
async function play(id: number): Promise<void> {
  await player.load(id)
  player.playing = true
}

onMounted(async () => {
  try {
    if (library.audiobooks.length === 0) await library.fetchLibrary()
    continueItems.value = await playerApi.getContinueListening()
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
.listening-section {
  margin-bottom: 2rem;
}
.listening-subtitle {
  font-size: 1.05rem;
  font-weight: 600;
  color: #cfd3d7;
  margin: 0 0 0.75rem;
}
.listening-empty {
  color: #9aa0a6;
}
.listening-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
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
.listening-cover--ph {
  background: #333;
}
.listening-meta {
  padding: 0.6rem 0.75rem 0.25rem;
  min-width: 0;
}
.listening-name {
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.listening-pos {
  font-size: 0.8rem;
  color: #9aa0a6;
}
.listening-play {
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
.listening-play:hover {
  filter: brightness(1.1);
}
</style>
