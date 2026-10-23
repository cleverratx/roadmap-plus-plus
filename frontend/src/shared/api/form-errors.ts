import { HttpError } from './http-error'

export type FormErrors = {
  fields: Record<string, string>
  message?: string
}

export function toFormErrors(error: unknown): FormErrors {
  if (!error) {
    return { fields: {} }
  }

  if (!(error instanceof HttpError)) {
    return { fields: {}, message: 'Не удалось связаться с сервером. Проверьте интернет и попробуйте ещё раз.' }
  }

  const fields = Object.fromEntries(
    Object.entries(error.fieldErrors).map(([field, messages]) => [field, messages[0] ?? '']),
  )

  return Object.keys(fields).length > 0 ? { fields } : { fields, message: error.message }
}
