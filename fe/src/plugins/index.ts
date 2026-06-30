/*
 * Listenarr - Audiobook Management System
 * Copyright (C) 2024-2026 Listenarr Contributors
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */
import type { Component } from 'vue'
import type { RouteRecordRaw } from 'vue-router'

/**
 * A frontend plugin contributes an optional always-mounted root component (e.g. a floating
 * player bar) and/or routes. Generic and plugin-agnostic; the core knows nothing about any
 * specific plugin.
 */
export interface ListenarrPlugin {
  id: string
  /** Persistent component mounted app-wide, outside the router view. */
  root?: Component
  /** Routes contributed to the app router. */
  routes?: RouteRecordRaw[]
}

// Empty upstream. Fork builds register their plugins here (the only file a fork edits to
// add one).
import { playerPlugin } from './player'

export const plugins: ListenarrPlugin[] = [playerPlugin]
