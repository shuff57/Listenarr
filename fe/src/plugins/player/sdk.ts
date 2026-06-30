// Host services provided at runtime via window.LISTENARR (installed by the host before this
// plugin's bundle loads). Using these instead of importing core modules directly means the
// plugin bundle never ships a second copy of apiService, the library store, etc.

export interface LibraryBook {
  id: number
  title?: string
  imageUrl?: string
  authors?: string[]
  narrators?: string[]
  description?: string
  subtitle?: string
  publishYear?: number
  monitored?: boolean
}

interface LibraryStore {
  audiobooks: LibraryBook[]
  fetchLibrary: () => Promise<void>
}

interface HostSdk {
  api: {
    request<T>(endpoint: string, options?: RequestInit): Promise<T>
    buildApiPath(endpoint: string): string
  }
  stores: { useLibraryStore: () => LibraryStore }
  ui: {
    useProtectedImages: () => { getProtectedImageSrc: (url: string, fallback: string) => string }
    getPlaceholderUrl: () => string
  }
}

const sdk = (window as unknown as { LISTENARR: HostSdk }).LISTENARR

export const request = sdk.api.request
export const buildApiPath = sdk.api.buildApiPath
export const useLibraryStore = sdk.stores.useLibraryStore
export const useProtectedImages = sdk.ui.useProtectedImages
export const getPlaceholderUrl = sdk.ui.getPlaceholderUrl
