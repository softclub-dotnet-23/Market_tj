const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5193/api";

// Ответ бэкенда MarketTJ.WebApi (см. ApiControllerBase.HandleResult):
// успех — { isSuccess: true, message, data }, ошибка — { isSuccess: false, message, errors }.
interface ApiSuccessResponse<T> {
  isSuccess: true;
  message: string;
  data: T;
}

interface ApiErrorResponse {
  isSuccess: false;
  message: string;
  errors: string[];
}

type ApiResponse<T> = ApiSuccessResponse<T> | ApiErrorResponse;

export class ApiError extends Error {
  status: number;
  errors: string[];

  constructor(message: string, status: number, errors: string[]) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errors = errors;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...init?.headers,
    },
  });

  const body = (await response.json()) as ApiResponse<T>;

  if (!body.isSuccess) {
    throw new ApiError(body.message, response.status, body.errors);
  }

  return body.data;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, payload: unknown) =>
    request<T>(path, { method: "POST", body: JSON.stringify(payload) }),
  put: <T>(path: string, payload: unknown) =>
    request<T>(path, { method: "PUT", body: JSON.stringify(payload) }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
};
