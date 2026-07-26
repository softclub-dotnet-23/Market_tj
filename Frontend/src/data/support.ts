import { apiGet, apiPost } from "@/lib/api";

export const SupportTicketStatus = { Open: 1, InProgress: 2, Closed: 3 } as const;
export const SupportPriority = { Low: 1, Normal: 2, High: 3 } as const;

export interface SupportTicketDto {
  id: number;
  userId: number;
  subject: string;
  status: number;
  priority: number;
  createdAt: string;
  closedAt: string | null;
  assignedToAdminId: number | null;
}

// POST /api/support-tickets не возвращает id созданной записи (та же схема,
// что и у ProductListing/Order на бэкенде) — а без id нельзя прикрепить
// сообщение через POST /api/support-messages. Достаём id только что
// созданного тикета через GET /api/support-tickets (сервер не фильтрует
// список — грузим всё и находим свой тикет на фронте, тот же приём, что и
// везде в проекте), беря самый свежий по createdAt среди тикетов этого
// пользователя с этой темой.
export async function submitSupportRequest(userId: number, subject: string, message: string) {
  await apiPost<string>("/support-tickets", {
    userId,
    subject,
    status: SupportTicketStatus.Open,
    priority: SupportPriority.Normal,
    closedAt: null,
    assignedToAdminId: null,
  });

  const tickets = await apiGet<SupportTicketDto[]>("/support-tickets");
  const myTicket = tickets
    .filter((ticket) => ticket.userId === userId && ticket.subject === subject)
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];

  if (!myTicket) throw new Error("Тикет создан, но не найден для отправки сообщения");

  await apiPost<string>("/support-messages", {
    supportTicketId: myTicket.id,
    senderId: userId,
    message,
  });

  return myTicket;
}
