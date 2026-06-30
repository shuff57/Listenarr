// Injects a Play button onto the core /audiobooks cards. The ONLY thing the core page
// provides is a `data-audiobook-id` attribute per card (Hybrid B); everything here —
// the button, styling, click handling, and re-injection on virtual-scroll re-renders —
// lives in the plugin. No core component is modified.
import { usePlayerStore } from './store'
import { playerApi } from './api'

const PLAY_SVG =
  '<svg viewBox="0 0 256 256" width="20" height="20" fill="currentColor"><path d="M232.4 114.5 88.4 26.6A16 16 0 0 0 64 40.3v175.4a16 16 0 0 0 24.4 13.7l144-87.9a16 16 0 0 0 0-27.4Z"/></svg>'

let observer: MutationObserver | null = null

function play(id: number): void {
  const player = usePlayerStore()
  void player.load(id).then(() => {
    player.playing = true
  })
}

function injectInto(el: HTMLElement): void {
  const idStr = el.getAttribute('data-audiobook-id')
  const id = Number(idStr)
  if (!idStr || !Number.isFinite(id)) return

  if (el.classList.contains('audiobook-detail')) {
    injectDetail(el, id)
  } else {
    injectCard(el, id)
  }
}

// Small circular play button overlaid on a library card's poster.
function injectCard(card: HTMLElement, id: number): void {
  if (card.querySelector('.lp-inject-play')) return
  const container = (card.querySelector('.audiobook-poster-container') as HTMLElement) ?? card
  if (getComputedStyle(container).position === 'static') {
    container.style.position = 'relative'
  }

  const btn = document.createElement('button')
  btn.className = 'lp-inject-play'
  btn.type = 'button'
  btn.title = 'Play'
  btn.setAttribute('aria-label', 'Play')
  btn.innerHTML = PLAY_SVG
  btn.addEventListener('click', (e) => {
    e.preventDefault()
    e.stopPropagation()
    play(id)
  })
  container.appendChild(btn)
}

// Squircle Play button in the detail toolbar, immediately left of the Edit button
// (matches the other icon-button squircles). Falls back to next-to-title if Edit isn't found.
function injectDetail(root: HTMLElement, id: number): void {
  if (root.querySelector('.lp-detail-play')) return

  const btn = document.createElement('button')
  btn.className = 'lp-detail-play'
  btn.type = 'button'
  btn.title = 'Play'
  btn.setAttribute('aria-label', 'Play')
  btn.innerHTML = PLAY_SVG
  btn.addEventListener('click', (e) => {
    e.preventDefault()
    e.stopPropagation()
    play(id)
  })

  const editBtn = root.querySelector('button[aria-label="Edit Metadata"]') as HTMLElement | null
  if (editBtn?.parentElement) {
    editBtn.parentElement.insertBefore(btn, editBtn)
  } else {
    const title = root.querySelector('h1.title') as HTMLElement | null
    if (!title) return
    title.insertAdjacentElement('afterend', btn)
  }

  // Best-effort: reflect saved progress in the tooltip.
  void playerApi
    .getPlayback(id)
    .then((state) => {
      btn.title = state.finished ? 'Play again' : state.positionSeconds > 0 ? 'Resume' : 'Play'
      btn.setAttribute('aria-label', btn.title)
    })
    .catch(() => {})
}

function scan(root: ParentNode): void {
  root.querySelectorAll?.('[data-audiobook-id]').forEach((el) => injectInto(el as HTMLElement))
}

function ensureStyles(): void {
  if (document.getElementById('lp-inject-style')) return
  const s = document.createElement('style')
  s.id = 'lp-inject-style'
  s.textContent = [
    '.lp-inject-play{position:absolute;top:8px;right:8px;z-index:103;width:40px;height:40px;',
    'border:none;border-radius:50%;background:rgba(43,125,233,.92);color:#fff;display:inline-flex;',
    'align-items:center;justify-content:center;cursor:pointer;opacity:0;transform:scale(.9);',
    'transition:opacity .15s ease,transform .15s ease;padding:0}',
    '.audiobook-item:hover .lp-inject-play,.audiobook-poster-container:hover .lp-inject-play{opacity:1;transform:scale(1)}',
    '.lp-inject-play:hover{filter:brightness(1.1)}',
    '.lp-detail-play{display:inline-flex;align-items:center;justify-content:center;width:36px;height:36px;',
    'padding:0;background:var(--brand-500,#2b7de9);color:#fff;border:none;border-radius:10px;cursor:pointer}',
    '.lp-detail-play:hover{filter:brightness(1.1)}',
  ].join('')
  document.head.appendChild(s)
}

/** Start watching the DOM for core book cards and inject Play buttons. Idempotent. */
export function startBooksPageInjector(): void {
  if (observer) return
  ensureStyles()
  scan(document)
  observer = new MutationObserver((mutations) => {
    for (const m of mutations) {
      m.addedNodes.forEach((n) => {
        if (!(n instanceof HTMLElement)) return
        if (n.matches?.('[data-audiobook-id]')) injectInto(n)
        scan(n)
      })
    }
  })
  observer.observe(document.body, { childList: true, subtree: true })
}
