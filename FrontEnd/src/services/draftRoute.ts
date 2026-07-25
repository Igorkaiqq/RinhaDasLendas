export function resolveInitialDraftId(value: unknown): string | null {
  const rawValue = Array.isArray(value) ? value[0] : value
  return typeof rawValue === 'string' && rawValue.trim() ? rawValue.trim() : null
}
