<template>
  <div class="listening-view">
    <h1 class="listening-title">Listen</h1>

    <div v-if="loading" class="listening-empty">Loading…</div>

    <template v-else>
      <CollapsibleSection
        v-if="continueBooks.length > 0"
        title="Continue Listening"
        :count="continueBooks.length"
        section-key="continue"
      >
        <div class="audiobooks-grid">
          <AudiobookCard
            v-for="b in continueBooks"
            :key="'c' + b.id"
            :book="b"
            :position="stateFor(b.id)?.positionSeconds ?? 0"
            :finished="false"
            @play="play"
          />
        </div>
      </CollapsibleSection>

      <CollapsibleSection
        v-if="finishedBooks.length > 0"
        title="Finished"
        :count="finishedBooks.length"
        section-key="finished"
      >
        <div class="audiobooks-grid">
          <AudiobookCard
            v-for="b in finishedBooks"
            :key="'f' + b.id"
            :book="b"
            :finished="true"
            @play="play"
          />
        </div>
      </CollapsibleSection>

      <CollapsibleSection title="Library" :count="books.length" section-key="library">
        <div v-if="books.length === 0" class="listening-empty">
          No audiobooks yet. Add a root folder and scan from your
          <RouterLink to="/audiobooks">library</RouterLink>.
        </div>
        <div v-else class="audiobooks-grid">
          <AudiobookCard
            v-for="b in books"
            :key="b.id"
            :book="b"
            :position="stateFor(b.id)?.positionSeconds ?? 0"
            :finished="stateFor(b.id)?.finished ?? false"
            @play="play"
          />
        </div>
      </CollapsibleSection>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { useLibraryStore } from './sdk'
import { playerApi } from './api'
import { usePlayerStore } from './store'
import CollapsibleSection from './CollapsibleSection.vue'
import AudiobookCard from './AudiobookCard.vue'
import type { ContinueListeningItem } from './types'

const library = useLibraryStore()
const player = usePlayerStore()

const states = ref<Map<number, ContinueListeningItem>>(new Map())
const loading = ref(true)

const books = computed(() => library.audiobooks)
function stateFor(id: number): ContinueListeningItem | undefined {
  return states.value.get(id)
}

const continueBooks = computed(() =>
  [...states.value.values()]
    .filter((s) => !s.finished && s.positionSeconds > 0)
    .sort((a, b) => (b.updatedUtc ?? '').localeCompare(a.updatedUtc ?? ''))
    .map((s) => books.value.find((bk) => bk.id === s.audiobookId))
    .filter((b): b is NonNullable<typeof b> => !!b),
)

const finishedBooks = computed(() =>
  books.value.filter((b) => stateFor(b.id)?.finished),
)

async function play(id: number): Promise<void> {
  await player.load(id)
  player.playing = true
}

onMounted(async () => {
  try {
    if (library.audiobooks.length === 0) await library.fetchLibrary()
    const rows = await playerApi.getPlaybackStates()
    states.value = new Map(rows.map((r) => [r.audiobookId, r]))
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
  padding: 0.5rem 0;
}
.audiobooks-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 20px;
  padding: 10px 0;
}
</style>
