// Player plugin API client. Reuses core's request plumbing (auth + CSRF + retry) via the
// generic apiService.pluginRequest passthrough, and buildApiPath for the raw stream URL.
// Does NOT modify core's api.ts.
import { apiService } from '@/services/api'
import { buildApiPath } from '@/services/apiBase'
import type { Bookmark, ContinueListeningItem, PlaybackState } from './types'

export const playerApi = {
  getContinueListening(): Promise<ContinueListeningItem[]> {
    return apiService.pluginRequest<ContinueListeningItem[]>('/audiobooks/continue-listening')
  },

  getPlayback(id: number): Promise<PlaybackState> {
    return apiService.pluginRequest<PlaybackState>(`/audiobooks/${id}/playback`)
  },

  savePlayback(
    id: number,
    body: { fileIndex: number; positionSeconds: number; finished: boolean },
  ): Promise<void> {
    return apiService.pluginRequest<void>(`/audiobooks/${id}/playback`, {
      method: 'PUT',
      body: JSON.stringify(body),
    })
  },

  streamUrl(id: number, fileIndex: number): string {
    return buildApiPath(`/audiobooks/${id}/files/${fileIndex}/stream`)
  },

  getBookmarks(id: number): Promise<Bookmark[]> {
    return apiService.pluginRequest<Bookmark[]>(`/audiobooks/${id}/bookmarks`)
  },

  addBookmark(
    id: number,
    body: { fileIndex: number; positionSeconds: number; label?: string | null },
  ): Promise<Bookmark> {
    return apiService.pluginRequest<Bookmark>(`/audiobooks/${id}/bookmarks`, {
      method: 'POST',
      body: JSON.stringify(body),
    })
  },

  deleteBookmark(id: number, bookmarkId: number): Promise<void> {
    return apiService.pluginRequest<void>(`/audiobooks/${id}/bookmarks/${bookmarkId}`, {
      method: 'DELETE',
    })
  },
}
