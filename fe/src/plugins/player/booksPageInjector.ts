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

// Play button as the first squircle in the card's native hover action cluster,
// styled to match the sibling edit/delete buttons. Falls back to a poster overlay.
function injectCard(card: HTMLElement, id: number): void {
  if (card.querySelector('.lp-inject-play')) return

  const btn = document.createElement('button')
  btn.className = 'action-btn resume-btn-small lp-inject-play'
  btn.type = 'button'
  btn.title = 'Play'
  btn.setAttribute('aria-label', 'Play')
  btn.innerHTML = PLAY_SVG
  btn.addEventListener('click', (e) => {
    e.preventDefault()
    e.stopPropagation()
    play(id)
  })

  const actions = card.querySelector('.action-buttons') as HTMLElement | null
  if (actions) {
    actions.insertBefore(btn, actions.firstChild)
    return
  }
  // Fallback: overlay on the poster (still a squircle, not a circle).
  const container = (card.querySelector('.audiobook-poster-container') as HTMLElement) ?? card
  if (getComputedStyle(container).position === 'static') {
    container.style.position = 'relative'
  }
  btn.classList.add('lp-inject-play-overlay')
  container.appendChild(btn)
}

// Play button as the first action in the detail toolbar's .primary-actions cluster
// (leftmost, native icon-button shape). Falls back to before Edit, then next-to-title.
function injectDetail(root: HTMLElement, id: number): void {
  if (root.querySelector('.lp-detail-play')) return

  const btn = document.createElement('button')
  // `primary` opts out of the core's `:not(.primary)` gray-background !important rule.
  btn.className = 'nav-btn icon-button primary lp-detail-play'
  btn.type = 'button'
  btn.title = 'Play'
  btn.setAttribute('aria-label', 'Play')
  btn.innerHTML = PLAY_SVG
  btn.addEventListener('click', (e) => {
    e.preventDefault()
    e.stopPropagation()
    play(id)
  })

  const primary = root.querySelector('.primary-actions') as HTMLElement | null
  const editBtn = root.querySelector('button[aria-label="Edit Metadata"]') as HTMLElement | null
  if (primary) {
    primary.insertBefore(btn, primary.firstChild)
  } else if (editBtn?.parentElement) {
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
    // Thumbnail: blue squircle matching the native .action-btn siblings.
    // Two-class selectors beat core's single-class rules regardless of <style> order.
    '.action-btn.resume-btn-small{background-color:rgba(33,150,243,.9);border-color:rgba(33,150,243,.5)}',
    '.action-btn.resume-btn-small:hover{background-color:rgba(33,150,243,1)}',
    '.lp-inject-play{display:inline-flex;align-items:center;justify-content:center}',
    '.lp-inject-play svg{width:14px;height:14px}',
    // Fallback overlay (when a card has no .action-buttons cluster).
    '.lp-inject-play-overlay{position:absolute;top:8px;right:8px;z-index:31;opacity:0;transition:opacity .2s}',
    '.audiobook-item:hover .lp-inject-play-overlay,.audiobook-poster-container:hover .lp-inject-play-overlay{opacity:1}',
    // Detail: blue accent over the native .nav-btn.icon-button shape.
    '.nav-btn.lp-detail-play{background-color:rgb(33,150,243);border-color:rgb(33,150,243)}',
    '.nav-btn.lp-detail-play:hover{background-color:rgb(30,136,229);border-color:rgb(30,136,229)}',
    '.lp-detail-play svg{width:20px;height:20px}',
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
