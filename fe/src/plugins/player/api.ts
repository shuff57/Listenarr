// Player plugin API client. Uses the host SDK (window.LISTENARR) for request plumbing
// (auth + CSRF + retry) and path building — so the bundle never ships its own apiService copy.
import { request, buildApiPath } from './sdk'
import type { Bookmark, ContinueListeningItem, PlaybackState } from './types'

export const playerApi = {
  getContinueListening(): Promise<ContinueListeningItem[]> {
    return request<ContinueListeningItem[]>('/audiobooks/continue-listening')
  },

  getPlayback(id: number): Promise<PlaybackState> {
    return request<PlaybackState>(`/audiobooks/${id}/playback`)
  },

  savePlayback(
    id: number,
    body: { fileIndex: number; positionSeconds: number; finished: boolean },
  ): Promise<void> {
    return request<void>(`/audiobooks/${id}/playback`, {
      method: 'PUT',
      body: JSON.stringify(body),
    })
  },

  streamUrl(id: number, fileIndex: number): string {
    return buildApiPath(`/audiobooks/${id}/files/${fileIndex}/stream`)
  },

  getBookmarks(id: number): Promise<Bookmark[]> {
    return request<Bookmark[]>(`/audiobooks/${id}/bookmarks`)
  },

  addBookmark(
    id: number,
    body: { fileIndex: number; positionSeconds: number; label?: string | null },
  ): Promise<Bookmark> {
    return request<Bookmark>(`/audiobooks/${id}/bookmarks`, {
      method: 'POST',
      body: JSON.stringify(body),
    })
  },

  deleteBookmark(id: number, bookmarkId: number): Promise<void> {
    return request<void>(`/audiobooks/${id}/bookmarks/${bookmarkId}`, {
      method: 'DELETE',
    })
  },
}
