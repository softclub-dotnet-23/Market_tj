const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5193/api";

// Файлы (аватарки, фото объявлений) отдаются статикой бэкенда как
// относительные пути вида "/uploads/avatars/3/x.jpg" (см. LocalFileStorageService
// на бэкенде) — без origin'а бэкенда браузер резолвил бы их относительно
// фронтенда (localhost:5173) и получал 404.
const API_ORIGIN = API_BASE_URL.replace(/\/api\/?$/, "");

export function resolveMediaUrl(path: string) {
  return path.startsWith("http") ? path : `${API_ORIGIN}${path}`;
}

// Та же схема хранения, что и в AuthContext (AUTH_STORAGE_KEY = "market-tj-auth") —
// не импортируем контекст напрямую (это не React-компонент/хук), а читаем то же
// хранилище, куда login() кладёт токен. "Запомнить меня" решает localStorage/sessionStorage.
const AUTH_STORAGE_KEY = "market-tj-auth";

function getStoredToken(): string | null {
  try {
    const raw = localStorage.getItem(AUTH_STORAGE_KEY) ?? sessionStorage.getItem(AUTH_STORAGE_KEY);
    if (!raw) return null;
    return (JSON.parse(raw) as { token?: string }).token ?? null;
  } catch {
    return null;
  }
}

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
  const token = getStoredToken();
  // FormData (загрузка файлов) не должна получить Content-Type: application/json —
  // fetch сам проставит multipart/form-data с правильным boundary, если не
  // трогать заголовок вручную.
  const isFormData = init?.body instanceof FormData;
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      ...(isFormData ? {} : { "Content-Type": "application/json" }),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  // [Authorize] на бэкенде отвечает пустым телом на 401/403 (стандартный
  // ASP.NET challenge, не в конверте {isSuccess, message, errors}) — просто
  // response.json() на пустом теле упал бы с SyntaxError вместо понятной ошибки.
  if (response.status === 401 || response.status === 403) {
    throw new ApiError("Требуется авторизация", response.status, []);
  }

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
  upload: <T>(path: string, formData: FormData) => request<T>(path, { method: "POST", body: formData }),
};

// Именованные алиасы поверх api.get/post — часть страниц (data/adminEntities.ts,
// data/farmer.ts) написана в этом стиле вызова.
export const apiGet = api.get;
export const apiPost = api.post;
export const apiPut = api.put;
export const apiDelete = api.delete;
export const apiUpload = api.upload;
