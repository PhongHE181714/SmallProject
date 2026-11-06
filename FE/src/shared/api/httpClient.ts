const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

export class ApiRequestError extends Error {
  constructor(
    public readonly status: number,
    public readonly detail?: string,
    public readonly data?: unknown,
  ) {
    super(detail || `HTTP_${status}`)
    this.name = 'ApiRequestError'
  }
}

export async function get<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, { signal })

  if (!response.ok) {
    const errorDetails = await parseErrorResponse(response)
    throw new ApiRequestError(response.status, errorDetails.detail, errorDetails.data)
  }

  return (await response.json()) as T
}

export async function getBlob(path: string, signal?: AbortSignal): Promise<Blob> {
  const response = await fetch(`${API_BASE_URL}${path}`, { signal })

  if (!response.ok) {
    const errorDetails = await parseErrorResponse(response)
    throw new ApiRequestError(response.status, errorDetails.detail, errorDetails.data)
  }

  return response.blob()
}

export async function post<TResponse, TBody = unknown>(
  path: string,
  body: TBody,
  signal?: AbortSignal,
): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(body),
    signal,
  })

  if (!response.ok) {
    const errorDetails = await parseErrorResponse(response)
    throw new ApiRequestError(response.status, errorDetails.detail, errorDetails.data)
  }

  return (await response.json()) as TResponse
}

export async function postForm<TResponse>(
  path: string,
  body: FormData,
  signal?: AbortSignal,
): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    body,
    signal,
  })

  if (!response.ok) {
    const errorDetails = await parseErrorResponse(response)
    throw new ApiRequestError(response.status, errorDetails.detail, errorDetails.data)
  }

  return (await response.json()) as TResponse
}

export async function put<TResponse, TBody = unknown>(
  path: string,
  body: TBody,
  signal?: AbortSignal,
): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(body),
    signal,
  })

  if (!response.ok) {
    const errorDetails = await parseErrorResponse(response)
    throw new ApiRequestError(response.status, errorDetails.detail, errorDetails.data)
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  return (await response.json()) as TResponse
}

export async function del<TResponse = void>(
  path: string,
  signal?: AbortSignal,
): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'DELETE',
    signal,
  })

  if (!response.ok) {
    const errorDetails = await parseErrorResponse(response)
    throw new ApiRequestError(response.status, errorDetails.detail, errorDetails.data)
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  return (await response.json()) as TResponse
}

async function parseErrorResponse(response: Response): Promise<{ detail?: string; data?: unknown }> {
  try {
    const data = await response.json()
    if (data && typeof data === 'object') {
      const detail =
        'detail' in data && typeof (data as { detail: unknown }).detail === 'string'
          ? (data as { detail: string }).detail
          : undefined
      return { detail, data }
    }
    return { data }
  } catch {
    return {}
  }
}

export { API_BASE_URL }
