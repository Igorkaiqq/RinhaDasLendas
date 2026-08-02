export interface ApiFieldError {
  field: string
  messageCode: string
  message: string
}

export interface ParsedApiError {
  message?: string
  messageCode?: string
  errors: string[]
  fieldErrors: ApiFieldError[]
}

export function parseApiError(data: unknown): ParsedApiError {
  if (!isRecord(data)) return { errors: [], fieldErrors: [] }

  const rawErrors = data.errors
  const errors = Array.isArray(rawErrors)
    ? rawErrors.filter((error): error is string => typeof error === 'string')
    : []
  const fieldErrors = Array.isArray(data.fieldErrors)
    ? data.fieldErrors.flatMap(parseFieldError)
    : []

  return {
    message: typeof data.message === 'string' ? data.message : undefined,
    messageCode:
      typeof data.messageCode === 'string' ? data.messageCode : undefined,
    errors,
    fieldErrors,
  }
}

function parseFieldError(value: unknown): ApiFieldError[] {
  if (!isRecord(value)) return []
  return typeof value.field === 'string' &&
    typeof value.messageCode === 'string' &&
    typeof value.message === 'string'
    ? [
        {
          field: value.field,
          messageCode: value.messageCode,
          message: value.message,
        },
      ]
    : []
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}
