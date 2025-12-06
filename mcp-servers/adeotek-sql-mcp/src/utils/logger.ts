/**
 * Logger utility using Winston
 */

import winston from 'winston';

const logLevel = process.env.LOG_LEVEL || 'info';
const isDevelopment = process.env.NODE_ENV !== 'production';

/**
 * Create a Winston logger instance
 */
export const logger = winston.createLogger({
  level: logLevel,
  format: winston.format.combine(
    winston.format.timestamp({ format: 'YYYY-MM-DD HH:mm:ss' }),
    winston.format.errors({ stack: true }),
    winston.format.splat(),
    winston.format.json()
  ),
  defaultMeta: { service: 'adeotek-sql-mcp' },
  transports: [
    new winston.transports.Console({
      format: isDevelopment
        ? winston.format.combine(
            winston.format.colorize(),
            winston.format.printf(({ timestamp, level, message, ...meta }) => {
              const metaStr = Object.keys(meta).length ? JSON.stringify(meta, null, 2) : '';
              return `${timestamp} [${level}]: ${message} ${metaStr}`;
            })
          )
        : winston.format.json(),
    }),
  ],
});

/**
 * Create a child logger with additional context
 */
export function createLogger(context: Record<string, unknown>): winston.Logger {
  return logger.child(context);
}

/**
 * Sanitize sensitive data before logging
 */
export function sanitize(data: unknown): unknown {
  if (typeof data !== 'object' || data === null) {
    return data;
  }

  const sensitiveKeys = [
    'password',
    'apiKey',
    'token',
    'secret',
    'authorization',
    'connectionString',
  ];

  if (Array.isArray(data)) {
    return data.map((item) => sanitize(item));
  }

  const sanitized: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(data)) {
    if (sensitiveKeys.some((sk) => key.toLowerCase().includes(sk.toLowerCase()))) {
      sanitized[key] = '***REDACTED***';
    } else if (typeof value === 'object' && value !== null) {
      sanitized[key] = sanitize(value);
    } else {
      sanitized[key] = value;
    }
  }
  return sanitized;
}

export default logger;
