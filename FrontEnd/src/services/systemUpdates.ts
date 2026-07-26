import { AppRoutes } from '@/constants/appRoutes'
import { SYSTEM_UPDATES } from '@/constants/systemUpdates'
import type {
  SystemUpdateCategory,
  SystemUpdateRelease,
} from '@/types/systemUpdate'

export const LAST_SEEN_SYSTEM_UPDATE_KEY = 'rinha:last-seen-system-update'

let inMemoryLastSeen: string | null = null

function normalizeSearchText(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLocaleLowerCase()
    .trim()
}

export function getLatestSystemUpdate(
  releases: readonly SystemUpdateRelease[] = SYSTEM_UPDATES,
): SystemUpdateRelease {
  if (!releases.length)
    throw new Error('System update registry cannot be empty')
  return releases[0]!
}

export function filterSystemUpdates(
  releases: readonly SystemUpdateRelease[],
  query: string,
  categories: readonly SystemUpdateCategory[],
  translate: (key: string) => string,
): SystemUpdateRelease[] {
  const normalizedQuery = normalizeSearchText(query)

  return releases.filter((release) => {
    if (
      categories.length &&
      !categories.some((category) => release.categories.includes(category))
    )
      return false
    if (!normalizedQuery) return true

    const keys = [
      release.titleKey,
      release.summaryKey,
      ...release.details.flatMap((detail) => [
        detail.titleKey,
        detail.descriptionKey,
      ]),
    ]
    return keys.some((key) =>
      normalizeSearchText(translate(key)).includes(normalizedQuery),
    )
  })
}

export function getSystemUpdateValidationErrors(
  releases: readonly SystemUpdateRelease[],
  hasTranslation: (key: string) => boolean,
): string[] {
  const errors: string[] = []
  const ids = new Set<string>()
  const versions = new Set<string>()
  const detailIds = new Set<string>()
  const knownPaths = new Set<string>(Object.values(AppRoutes))

  if (releases.filter((release) => release.featured).length !== 1) {
    errors.push('Exactly one release must be featured')
  }

  releases.forEach((release, index) => {
    const parsedDate = new Date(`${release.publishedAt}T00:00:00Z`)

    if (ids.has(release.id)) errors.push(`Duplicate release id: ${release.id}`)
    if (versions.has(release.version))
      errors.push(`Duplicate release version: ${release.version}`)
    if (!/^\d{4}\.\d{2}\.\d+$/.test(release.version))
      errors.push(`Invalid version: ${release.version}`)
    if (
      !/^\d{4}-\d{2}-\d{2}$/.test(release.publishedAt) ||
      Number.isNaN(parsedDate.getTime()) ||
      parsedDate.toISOString().slice(0, 10) !== release.publishedAt
    ) {
      errors.push(`Invalid date: ${release.publishedAt}`)
    }
    if (index > 0 && releases[index - 1]!.publishedAt < release.publishedAt) {
      errors.push('Releases must be newest first')
    }
    if (!release.categories.length)
      errors.push(`Missing categories: ${release.id}`)
    if (!release.areas.length) errors.push(`Missing areas: ${release.id}`)
    if (!release.details.length) errors.push(`Missing details: ${release.id}`)

    for (const key of [release.titleKey, release.summaryKey]) {
      if (!hasTranslation(key)) errors.push(`Missing translation: ${key}`)
    }

    for (const detail of release.details) {
      const scopedDetailId = `${release.id}:${detail.id}`
      if (detailIds.has(scopedDetailId))
        errors.push(`Duplicate detail id: ${scopedDetailId}`)
      detailIds.add(scopedDetailId)

      for (const key of [detail.titleKey, detail.descriptionKey]) {
        if (!hasTranslation(key)) errors.push(`Missing translation: ${key}`)
      }
      if (detail.link && !knownPaths.has(detail.link))
        errors.push(`Unknown internal link: ${detail.link}`)
    }

    ids.add(release.id)
    versions.add(release.version)
  })

  return errors
}

export function readLastSeenSystemUpdate(
  storage?: Pick<Storage, 'getItem' | 'setItem'>,
): string | null {
  if (inMemoryLastSeen !== null) return inMemoryLastSeen

  try {
    const resolvedStorage = storage ?? globalThis.localStorage
    return resolvedStorage?.getItem(LAST_SEEN_SYSTEM_UPDATE_KEY) ?? null
  } catch {
    return inMemoryLastSeen
  }
}

export function markLatestSystemUpdateSeen(
  version: string,
  storage?: Pick<Storage, 'getItem' | 'setItem'>,
): string {
  inMemoryLastSeen = version
  try {
    const resolvedStorage = storage ?? globalThis.localStorage
    resolvedStorage?.setItem(LAST_SEEN_SYSTEM_UPDATE_KEY, version)
  } catch {
    // The session fallback was set before attempting persistent storage.
  }
  return version
}
