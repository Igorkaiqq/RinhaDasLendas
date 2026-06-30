const sensitivePatterns = [
  /Bot\s+[A-Za-z0-9._-]+/gi,
  /Bearer\s+[A-Za-z0-9._-]+/gi,
  /DISCORD_TOKEN=[^\s]+/gi,
  /RINHA_API_INTERNAL_TOKEN=[^\s]+/gi,
]

export const logger = {
  info: (message: string, metadata: Record<string, unknown> = {}) => log('info', message, metadata),
  error: (message: string, error?: unknown, metadata: Record<string, unknown> = {}) => log('error', message, { ...metadata, error: serializeError(error) }),
}

function log(level: 'info' | 'error', message: string, metadata: Record<string, unknown>) {
  const entry = redact(JSON.stringify({ level, message, ...metadata, timestamp: new Date().toISOString() }))
  if (level === 'error') {
    console.error(entry)
    return
  }

  console.log(entry)
}

function serializeError(error: unknown) {
  if (error instanceof Error) {
    return { name: error.name, message: error.message, stack: error.stack }
  }

  return error
}

function redact(value: string) {
  return sensitivePatterns.reduce((current, pattern) => current.replace(pattern, '[REDACTED]'), value)
}
