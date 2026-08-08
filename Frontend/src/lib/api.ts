import { toast } from "sonner";
import i18n from "@/lib/i18n";
import { formatCountdown, parseBlockedUntil } from "@/lib/rateLimit";

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

// Для эндпоинтов вроде /order-items, которые ВСЕГДА отвечают 401 без токена
// (см. catalogStore.ts) — так вызывающий код может пропустить заведомо
// провальный запрос вместо "запросить и поймать ошибку", избавляя консоль
// от красных 401 на каждой гостевой странице.
export function hasAuthToken(): boolean {
  return getStoredToken() !== null;
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

// Блок 3 (2026-08-08) — единый живой countdown-тост для ЛЮБОГО 429 от бэкенда
// (rate-limit спам-кликов или уже активный бан за отмены — оба используют
// один и тот же формат сообщения, см. AccountBlockExtensions.FormatBlockMessage).
// Фиксированный id тоста — повторные 429 (напр. пользователь снова кликнул
// заблокированную кнопку) обновляют ТОТ ЖЕ тост вместо накопления дубликатов.
const RATE_LIMIT_TOAST_ID = "rate-limit-block";
let rateLimitIntervalId: ReturnType<typeof setInterval> | null = null;

function showRateLimitToast(message: string) {
  const blockedUntil = parseBlockedUntil(message);
  if (!blockedUntil) {
    toast.error(message, { id: RATE_LIMIT_TOAST_ID });
    return;
  }

  if (rateLimitIntervalId) clearInterval(rateLimitIntervalId);

  const tick = () => {
    const remainingMs = blockedUntil.getTime() - Date.now();
    if (remainingMs <= 0) {
      toast.dismiss(RATE_LIMIT_TOAST_ID);
      if (rateLimitIntervalId) clearInterval(rateLimitIntervalId);
      rateLimitIntervalId = null;
      return;
    }
    toast.error(message, {
      id: RATE_LIMIT_TOAST_ID,
      duration: Infinity,
      description: i18n.t("common:rateLimit.unblocksIn", { time: formatCountdown(remainingMs) }),
    });
  };

  tick();
  rateLimitIntervalId = setInterval(tick, 1000);
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

  // Тело ответа может оказаться пустым/невалидным JSON не только из-за
  // 401/403 (тот случай уже отдельно обработан выше) — например, если
  // соединение оборвалось на середине потока (см. ExceptionHandlingMiddleware
  // на бэкенде, 2026-08-03: раньше при исключении ПОСЛЕ начала отправки
  // ответа клиент получал обрезанное тело). Без этого try/catch пользователь
  // увидел бы сырой текст браузера вроде "Unexpected end of JSON input"
  // вместо понятного сообщения.
  let body: ApiResponse<T>;
  try {
    body = (await response.json()) as ApiResponse<T>;
  } catch {
    throw new ApiError("Сервер вернул пустой или повреждённый ответ, попробуйте ещё раз", response.status, []);
  }

  if (!body.isSuccess) {
    // Блок 3 (2026-08-08) — 429 (rate-limit/бан аккаунта, см. RateLimitAttribute
    // и AccountBlockService на бэкенде) показывается ЗДЕСЬ же, в одном месте
    // для ВСЕХ вызовов api.*, а не в каждом catch-блоке страниц отдельно —
    // единственный способ гарантированно накрыть "любую чувствительную кнопку"
    // живым таймером до разблокировки, не трогая десятки существующих страниц.
    if (response.status === 429) {
      showRateLimitToast(body.message);
    }
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
  patch: <T>(path: string, payload: unknown) =>
    request<T>(path, { method: "PATCH", body: JSON.stringify(payload) }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
  upload: <T>(path: string, formData: FormData) => request<T>(path, { method: "POST", body: formData }),
};

// Именованные алиасы поверх api.get/post — часть страниц (data/adminEntities.ts,
// data/farmer.ts) написана в этом стиле вызова.
export const apiGet = api.get;
export const apiPost = api.post;
export const apiPut = api.put;
export const apiPatch = api.patch;
export const apiDelete = api.delete;
export const apiUpload = api.upload;
