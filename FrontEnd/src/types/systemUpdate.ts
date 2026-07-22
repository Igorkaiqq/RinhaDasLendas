import type { AppRoutePath } from '@/constants/appRoutes'

export type SystemUpdateCategory =
  | 'feature'
  | 'improvement'
  | 'fix'
  | 'security'
  | 'infrastructure'

export type SystemUpdateArea =
  | 'platform'
  | 'players'
  | 'teams'
  | 'users'
  | 'drafts'
  | 'discord'
  | 'security'
  | 'infrastructure'

export interface SystemUpdateDetail {
  readonly id: string
  readonly category: SystemUpdateCategory
  readonly titleKey: string
  readonly descriptionKey: string
  readonly link?: AppRoutePath
}

export interface SystemUpdateRelease {
  readonly id: string
  readonly version: string
  readonly publishedAt: string
  readonly featured: boolean
  readonly categories: readonly SystemUpdateCategory[]
  readonly areas: readonly SystemUpdateArea[]
  readonly titleKey: string
  readonly summaryKey: string
  readonly details: readonly SystemUpdateDetail[]
}
