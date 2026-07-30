import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api";
import { SupportPriority, SupportTicketStatus, type SupportMessageDto, type SupportTicketDto } from "@/data/support";

export { SupportPriority, SupportTicketStatus };
export type { SupportMessageDto, SupportTicketDto };

interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

function useAsync<T>(fetcher: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [state, setState] = useState<AsyncState<T>>({ data: null, loading: true, error: null });

  useEffect(() => {
    let cancelled = false;
    setState({ data: null, loading: true, error: null });

    fetcher()
      .then((data) => {
        if (!cancelled) setState({ data, loading: false, error: null });
      })
      .catch((err: unknown) => {
        if (!cancelled) setState({ data: null, loading: false, error: err instanceof Error ? err.message : String(err) });
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return state;
}

interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// Список тикетов поддержки для админа — сервер не фильтрует/не ищет, грузим
// одним запросом с большим pageSize и пагинируем/ищем на фронте, как и все
// остальные Admin-списки в проекте (AdminOrders/AdminFarmers и т.д.).
export function useAdminSupportTickets(refreshKey = 0) {
  const { data, loading, error } = useAsync(
    () => apiGet<PagedResultDto<SupportTicketDto>>("/admin/support-tickets?pageNumber=1&pageSize=500"),
    [refreshKey],
  );
  return { tickets: data?.items ?? null, loading, error };
}

export function useAdminSupportMessages(ticketId: number | null, refreshKey = 0) {
  const { data, loading, error } = useAsync(
    () => (ticketId ? apiGet<SupportMessageDto[]>(`/admin/support-tickets/${ticketId}/messages`) : Promise.resolve(null as never)),
    [ticketId, refreshKey],
  );
  return { messages: data, loading, error };
}

export function replyToSupportTicket(ticketId: number, message: string) {
  return apiPost<string>(`/admin/support-tickets/${ticketId}/messages`, { message });
}

export interface AdminUserLiteDto {
  id: number;
  fullName: string;
  email: string;
}

// Чтобы показать имя автора тикета (для зарегистрированных — не гостей,
// у гостя уже есть GuestName на самом тикете) — берём общий /api/users
// (Admin-only), не тянем сюда весь AdminUserDto ради двух полей.
export function useAllUsers(refreshKey = 0) {
  const { data, loading, error } = useAsync(() => apiGet<AdminUserLiteDto[]>("/users"), [refreshKey]);
  return { users: data, loading, error };
}
