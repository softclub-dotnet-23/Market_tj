import { apiPost } from "@/lib/api";

export type AssistantIntent = "product" | "category" | "cart" | "orders" | "none" | "info" | "action_pending";

export interface AssistantActionDto {
  type: string;
  params: Record<string, string>;
  confirmLabel: string;
}

export interface AssistantResponseDto {
  intent: AssistantIntent;
  productId: number | null;
  categoryId: number | null;
  message: string;
  action: AssistantActionDto | null;
}

export interface AssistantHistoryMessageDto {
  role: "user" | "assistant";
  text: string;
}

// POST /api/ai-assistant/ask (раздел 38 ТЗ) — доступен и без авторизации
// (гость получает только поиск по каталогу); фермер/админ получают
// дополнительные инструменты по своей роли, если токен передан. history —
// предыдущие реплики ЭТОГО диалога (без нового вопроса) — без неё каждый
// запрос был изолирован, и ассистент не понимал уточняющих вопросов вроде
// "а когда?" после "статус заказа ORD-123" (см. AiAssistantWidget.tsx).
export function askAssistant(message: string, history?: AssistantHistoryMessageDto[]) {
  return apiPost<AssistantResponseDto>("/ai-assistant/ask", { message, history });
}

// POST /api/ai-assistant/execute-action — требует авторизации. Выполняет
// действие, которое ассистент только ПРЕДЛОЖИЛ (intent="action_pending") и
// которое пользователь подтвердил в UI — права проверяются заново на сервере.
export function executeAssistantAction(action: Pick<AssistantActionDto, "type" | "params">) {
  return apiPost<string>("/ai-assistant/execute-action", action);
}
