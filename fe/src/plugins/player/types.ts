// Player plugin types — self-contained (not added to core's @/types).

export interface PlaybackFile {
  index: number
  durationSeconds: number | null
  contentType: string
}

export interface Chapter {
  index: number
  fileIndex: number
  startSeconds: number
  endSeconds: number
  title: string
}

export interface Bookmark {
  id: number
  fileIndex: number
  positionSeconds: number
  label: string | null
  createdUtc: string
}

export interface PlaybackState {
  audiobookId: number
  title: string | null
  asin: string | null
  files: PlaybackFile[]
  fileIndex: number
  positionSeconds: number
  finished: boolean
  chapters: Chapter[]
}

/** Lightweight row from the continue-listening endpoint; merged with library data by id. */
export interface ContinueListeningItem {
  audiobookId: number
  fileIndex: number
  positionSeconds: number
  finished: boolean
  updatedUtc: string | null
}
