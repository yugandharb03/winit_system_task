const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5099/api';

/** Shape the API returns for any failure (RFC 7807 ProblemDetails). */
interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  /** Field-level messages from ValidationProblemDetails, flattened for display. */
  readonly fieldErrors: string[];

  constructor(status: number, message: string, fieldErrors: string[] = []) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

async function parseError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails | null = null;

  try {
    problem = (await response.json()) as ProblemDetails;
  } catch {
    // Body was not JSON — fall back to the status text below.
  }

  const fieldErrors = problem?.errors ? Object.values(problem.errors).flat() : [];

  const message =
    fieldErrors[0] ??
    problem?.detail ??
    problem?.title ??
    `Request failed with status ${response.status}.`;

  return new ApiError(response.status, message, fieldErrors);
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;

  try {
    response = await fetch(`${BASE_URL}${path}`, {
      headers: { 'Content-Type': 'application/json' },
      ...init,
    });
  } catch (err) {
    // A cancelled request is not a failure — callers check for AbortError, so let it through.
    if (err instanceof DOMException && err.name === 'AbortError') {
      throw err;
    }

    throw new ApiError(0, 'Cannot reach the API. Make sure the backend is running on port 5099.');
  }

  if (!response.ok) {
    throw await parseError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  get: <T>(path: string, signal?: AbortSignal) => request<T>(path, { method: 'GET', signal }),

  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body) }),

  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),

  delete: (path: string) => request<void>(path, { method: 'DELETE' }),
};

/** Builds a query string, dropping empty values so the URL stays clean. */
export function toQuery(params: Record<string, string | number | undefined | null>): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.append(key, String(value));
    }
  }

  const query = search.toString();
  return query ? `?${query}` : '';
}
