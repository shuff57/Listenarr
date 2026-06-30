<template>
  <div class="book-view">
    <div v-if="loading" class="bv-empty">Loading…</div>
    <div v-else-if="!book" class="bv-empty">
      Book not found. <RouterLink to="/listening">Back to Listen</RouterLink>.
    </div>

    <template v-else>
      <div class="bv-header">
        <img :src="cover" :alt="book.title || ''" class="bv-cover" />
        <div class="bv-info">
          <h1 class="bv-title">{{ book.title || 'Unknown title' }}</h1>
          <div v-if="book.subtitle" class="bv-subtitle">{{ book.subtitle }}</div>
          <div class="bv-author">{{ authorText }}</div>
          <div v-if="narratorText" class="bv-narrator">Narrated by {{ narratorText }}</div>

          <div class="bv-actions">
            <button class="bv-play" type="button" @click="play">
              <PhPlay weight="fill" /> {{ playLabel }}
            </button>
            <span v-if="state && state.positionSeconds > 0 && !state.finished" class="bv-resume">
              at {{ formatTime(state.positionSeconds) }}
            </span>
            <span v-if="state?.finished" class="bv-done"><PhCheckCircle weight="fill" /> Finished</span>
          </div>

          <RouterLink :to="`/audiobooks/${id}`" class="bv-manage">Full details &amp; manage →</RouterLink>
        </div>
      </div>

      <p v-if="book.description" class="bv-desc">{{ book.description }}</p>

      <section v-if="chapters.length" class="bv-section">
        <h2 class="bv-h2">Chapters <span class="bv-count">{{ chapters.length }}</span></h2>
        <ol class="bv-chapters">
          <li v-for="ch in chapters" :key="ch.index">
            <button class="bv-chapter" type="button" @click="playAt(ch.fileIndex, ch.startSeconds)">
              <span class="bv-chapter-title">{{ ch.title }}</span>
              <span class="bv-chapter-time">{{ formatTime(ch.startSeconds) }}</span>
            </button>
          </li>
        </ol>
      </section>

      <section v-if="bookmarks.length" class="bv-section">
        <h2 class="bv-h2">Bookmarks <span class="bv-count">{{ bookmarks.length }}</span></h2>
        <ul class="bv-bookmarks">
          <li v-for="bm in bookmarks" :key="bm.id">
            <button class="bv-chapter" type="button" @click="playAt(bm.fileIndex, bm.positionSeconds)">
              <span class="bv-chapter-title">{{ bm.label || 'Bookmark' }}</span>
              <span class="bv-chapter-time">{{ formatTime(bm.positionSeconds) }}</span>
            </button>
          </li>
        </ul>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { PhPlay, PhCheckCircle } from '@phosphor-icons/vue'
import { useLibraryStore, getPlaceholderUrl, useProtectedImages } from './sdk'
import { playerApi } from './api'
import { usePlayerStore } from './store'
import type { Bookmark, Chapter, PlaybackState } from './types'

const route = useRoute()
const library = useLibraryStore()
const player = usePlayerStore()
const { getProtectedImageSrc } = useProtectedImages()

const id = computed(() => Number(route.params.id))
const loading = ref(true)
const state = ref<PlaybackState | null>(null)
const chapters = ref<Chapter[]>([])
const bookmarks = ref<Bookmark[]>([])

const book = computed(() => library.audiobooks.find((b) => b.id === id.value))
const cover = computed(() =>
  book.value?.imageUrl ? getProtectedImageSrc(book.value.imageUrl, getPlaceholderUrl()) : getPlaceholderUrl(),
)
const authorText = computed(() => book.value?.authors?.join(', ') || 'Unknown Author')
const narratorText = computed(() => book.value?.narrators?.join(', ') || '')
const playLabel = computed(() =>
  state.value && state.value.positionSeconds > 0 && !state.value.finished ? 'Resume' : 'Play',
)

function formatTime(sec: number): string {
  const s = Math.max(0, Math.floor(sec))
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  const ss = s % 60
  return `${h}:${String(m).padStart(2, '0')}:${String(ss).padStart(2, '0')}`
}

async function play(): Promise<void> {
  await player.load(id.value)
  player.playing = true
}

async function playAt(fileIndex: number, position: number): Promise<void> {
  await player.load(id.value)
  player.fileIndex = fileIndex
  player.positionSeconds = position
  player.playing = true
}

onMounted(async () => {
  try {
    if (library.audiobooks.length === 0) await library.fetchLibrary()
    state.value = await playerApi.getPlayback(id.value)
    chapters.value = state.value?.chapters ?? []
    bookmarks.value = await playerApi.getBookmarks(id.value)
  } catch {
    // best-effort; book metadata still renders from the library store
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.book-view {
  padding: 1.5rem;
  max-width: 900px;
  margin: 0 auto;
}
.bv-empty {
  color: #9aa0a6;
}
.bv-header {
  display: flex;
  gap: 1.5rem;
  align-items: flex-start;
}
.bv-cover {
  width: 200px;
  height: 200px;
  object-fit: cover;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.5);
  flex-shrink: 0;
}
.bv-info {
  min-width: 0;
}
.bv-title {
  font-size: 1.6rem;
  font-weight: 600;
  margin: 0 0 0.25rem;
}
.bv-subtitle {
  color: #cfd3d7;
  margin-bottom: 0.25rem;
}
.bv-author {
  color: #b9bec3;
}
.bv-narrator {
  color: #9aa0a6;
  font-size: 0.9rem;
  margin-top: 0.15rem;
}
.bv-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin: 1rem 0 0.5rem;
}
.bv-play {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 1.1rem;
  background: var(--brand-500, #2b7de9);
  color: #fff;
  border: none;
  border-radius: 6px;
  font-size: 1rem;
  cursor: pointer;
}
.bv-play:hover {
  filter: brightness(1.1);
}
.bv-resume {
  color: #9aa0a6;
}
.bv-done {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  color: #3498db;
}
.bv-manage {
  display: inline-block;
  margin-top: 0.5rem;
  color: #9aa0a6;
  font-size: 0.9rem;
}
.bv-desc {
  margin: 1.25rem 0;
  color: #cfd3d7;
  line-height: 1.5;
}
.bv-section {
  margin-top: 1.5rem;
}
.bv-h2 {
  font-size: 1.05rem;
  font-weight: 600;
  margin: 0 0 0.5rem;
}
.bv-count {
  color: #9aa0a6;
  font-weight: 400;
  font-size: 0.85rem;
}
.bv-chapters,
.bv-bookmarks {
  list-style: none;
  margin: 0;
  padding: 0;
}
.bv-chapter {
  width: 100%;
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.5rem 0.75rem;
  background: #2a2a2a;
  border: 1px solid #3a3a3a;
  border-radius: 6px;
  color: #ddd;
  cursor: pointer;
  margin-bottom: 0.35rem;
  text-align: left;
}
.bv-chapter:hover {
  background: #333;
}
.bv-chapter-title {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.bv-chapter-time {
  color: #9aa0a6;
  flex-shrink: 0;
}
</style>
