interface ApiEnvelope<T> {
  isSuccess: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(`/api${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  const envelope = (await response.json()) as ApiEnvelope<T>;

  if (!response.ok || !envelope.isSuccess) {
    throw new Error(envelope.message || "Не удалось выполнить запрос");
  }

  return envelope.data as T;
}

export async function apiGet<T>(path: string): Promise<T> {
  const response = await fetch(`/api${path}`);
  const envelope = (await response.json()) as ApiEnvelope<T>;

  if (!response.ok || !envelope.isSuccess) {
    throw new Error(envelope.message || "Не удалось выполнить запрос");
  }

  return envelope.data as T;
}
