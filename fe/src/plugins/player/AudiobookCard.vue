<template>
  <div class="audiobook-item" tabindex="0" @click="open" @keydown.enter="open">
    <div class="audiobook-poster-container">
      <div class="audiobook-image-placeholder" :class="{ loaded: imgLoaded }">
        <PhBookOpen class="audiobook-placeholder-icon" />
      </div>
      <img
        :src="cover"
        :alt="book.title || 'Audiobook'"
        class="audiobook-poster cover-loading-image"
        :class="{ loaded: imgLoaded }"
        loading="lazy"
        decoding="async"
        @load="imgLoaded = true"
        @error="imgLoaded = true"
      />

      <!-- Play button (one-click from the Listen page; doesn't navigate) -->
      <button class="play-fab" type="button" title="Play" @click.stop="$emit('play', book.id)">
        <PhPlay weight="fill" />
      </button>

      <div class="status-overlay">
        <div class="audiobook-title">{{ book.title || 'Unknown title' }}</div>
        <div class="audiobook-author">{{ authorText }}</div>
        <div class="monitored-badge" :class="{ unmonitored: book.monitored === false }">
          <component :is="book.monitored === false ? PhEyeSlash : PhEye" />
          {{ book.monitored === false ? 'Unmonitored' : 'Monitored' }}
        </div>
        <div v-if="finished" class="monitored-badge playback-finished">
          <PhCheckCircle /> Finished
        </div>
        <div v-else-if="position > 0" class="monitored-badge playback-in-progress">
          <PhPlay /> In progress
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { PhBookOpen, PhPlay, PhCheckCircle, PhEye, PhEyeSlash } from '@phosphor-icons/vue'
import { getPlaceholderUrl, useProtectedImages } from './sdk'
import type { LibraryBook } from './sdk'

const props = defineProps<{ book: LibraryBook; finished?: boolean; position?: number }>()
defineEmits<{ play: [id: number] }>()

const router = useRouter()
const { getProtectedImageSrc } = useProtectedImages()
const imgLoaded = ref(false)

const finished = computed(() => props.finished ?? false)
const position = computed(() => props.position ?? 0)
const cover = computed(() =>
  props.book.imageUrl ? getProtectedImageSrc(props.book.imageUrl, getPlaceholderUrl()) : getPlaceholderUrl(),
)
const authorText = computed(() =>
  props.book.authors && props.book.authors.length ? props.book.authors.join(', ') : 'Unknown Author',
)

function open(): void {
  router.push(`/listen/${props.book.id}`)
}
</script>

<style scoped>
.audiobook-item {
  cursor: pointer;
  transition: transform 0.2s ease;
  position: relative;
}
.audiobook-item:hover {
  transform: scale(1.05);
}
.audiobook-poster-container {
  position: relative;
  aspect-ratio: 1/1;
  border-radius: 6px;
  overflow: hidden;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.5);
}
.audiobook-image-placeholder {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(90deg, #242424 0%, #2f343a 50%, #242424 100%);
  background-size: 200% 100%;
  color: #9aa4b2;
  transition: opacity 0.2s ease;
  z-index: 1;
}
.audiobook-image-placeholder.loaded {
  opacity: 0;
}
.audiobook-placeholder-icon {
  width: 2.75em;
  height: 2.75em;
}
.cover-loading-image {
  position: absolute;
  inset: 0;
  z-index: 2;
  opacity: 0;
  transition: opacity 0.2s ease;
}
.cover-loading-image.loaded {
  opacity: 1;
}
.audiobook-poster {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
/* Squircle Play button matching the native .action-btn / reference resume button. */
.play-fab {
  position: absolute;
  top: 8px;
  right: 8px;
  z-index: 103;
  padding: 6px 8px;
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 6px;
  background-color: rgba(33, 150, 243, 0.9);
  color: #fff;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.15s ease;
}
.play-fab svg {
  width: 14px;
  height: 14px;
}
.audiobook-poster-container:hover .play-fab {
  opacity: 1;
}
.play-fab:hover {
  background-color: rgba(33, 150, 243, 1);
}
.status-overlay {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background: linear-gradient(transparent, rgba(0, 0, 0, 0.9));
  padding: 8px;
  transition:
    padding 0.2s ease,
    opacity 0.2s ease;
  opacity: 0;
  z-index: 101;
  pointer-events: none;
}
.audiobook-poster-container:hover .status-overlay {
  padding: 80px 8px 8px;
  opacity: 1;
}
.audiobook-title {
  font-size: 13px;
  font-weight: 500;
  color: #fff;
  margin-bottom: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.audiobook-author {
  font-size: 11px;
  color: #ccc;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.monitored-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  margin-top: 0.5rem;
  margin-left: 0.25rem;
  padding: 0.25rem 0.5rem;
  background-color: rgba(46, 204, 113, 0.2);
  border: 1px solid rgba(46, 204, 113, 0.4);
  border-radius: 6px;
  font-size: 10px;
  font-weight: 500;
  color: #2ecc71;
  white-space: nowrap;
}
.monitored-badge.unmonitored {
  background-color: rgba(231, 76, 60, 0.2);
  border-color: rgba(231, 76, 60, 0.4);
  color: #e74c3c;
}
.monitored-badge.playback-finished {
  background-color: rgba(52, 152, 219, 0.2);
  border-color: rgba(52, 152, 219, 0.4);
  color: #3498db;
}
.monitored-badge.playback-in-progress {
  background-color: rgba(243, 156, 18, 0.2);
  border-color: rgba(243, 156, 18, 0.4);
  color: #f39c12;
}
</style>
