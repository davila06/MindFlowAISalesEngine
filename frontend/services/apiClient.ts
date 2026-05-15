const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5165";

interface RequestOptions extends RequestInit {
  timeoutMs?: number;
}

function createCorrelationId() {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `corr-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

function buildHeaders(extra?: HeadersInit): HeadersInit {
  const tenantId = process.env.NEXT_PUBLIC_TENANT_ID ?? "default";
  const correlationId = createCorrelationId();

  return {
    "Content-Type": "application/json",
    "X-Tenant-Id": tenantId,
    "X-Correlation-Id": correlationId,
    ...(extra ?? {})
  };
}

async function request<T>(path: string, init?: RequestOptions): Promise<T> {
  const controller = new AbortController();
  const timeoutMs = init?.timeoutMs ?? 12000;
  const timer = setTimeout(() => controller.abort(), timeoutMs);

  const signal = init?.signal
    ? AbortSignal.any([controller.signal, init.signal])
    : controller.signal;

  let response: Response;

  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      signal,
      headers: buildHeaders(init?.headers),
      cache: "no-store"
    });
  } catch (error) {
    if (error instanceof Error && error.name === "AbortError") {
      throw new Error("Request canceled or timed out");
    }

    throw error;
  } finally {
    clearTimeout(timer);
  }

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`${response.status} ${response.statusText}: ${body}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const payload = await response.text();
  if (!payload.trim()) {
    return undefined as T;
  }

  return JSON.parse(payload) as T;
}

export const apiClient = {
  get: <T>(path: string, init?: RequestOptions) => request<T>(path, init),
  post: <T>(path: string, body: unknown, init?: RequestOptions) =>
    request<T>(path, {
      ...init,
      method: "POST",
      body: JSON.stringify(body)
    }),
  put: <T>(path: string, body: unknown, init?: RequestOptions) =>
    request<T>(path, {
      ...init,
      method: "PUT",
      body: JSON.stringify(body)
    }),
  patch: <T>(path: string, body: unknown, init?: RequestOptions) =>
    request<T>(path, {
      ...init,
      method: "PATCH",
      body: JSON.stringify(body)
    })
};
