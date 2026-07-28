import { useEffect, useState } from "react";
import { apiGet, apiPost, apiPut, apiUpload } from "@/lib/api";

export interface ConversationDto {
  id: number;
  // null — чат ещё не привязан к заказу (вопрос фермеру про товар до покупки,
  // раздел 8.15 ТЗ: OrderId у Conversation необязателен).
  orderId: number | null;
  // Conversation.CustomerId/FarmerId — прямой FK на User.Id (раздел 8.15 ТЗ),
  // в отличие от Order.CustomerId/FarmerId, которые указывают на
  // CustomerProfile/FarmerProfile — см. useCustomerUserIdMap/useFarmerUserIdMap ниже.
  customerId: number;
  farmerId: number;
  // Резолвится на бэкенде через прямой User.Id (Conversation.CustomerId — FK
  // на User напрямую, без CustomerProfile-индирекции, см. комментарий выше) —
  // null, если пользователь почему-то не найден (например, удалён).
  customerFullName: string | null;
  isClosed: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ChatMessageDto {
  id: number;
  conversationId: number;
  senderId: number;
  message: string;
  imageUrl: string | null;
  isRead: boolean;
  createdAt: string;
}

interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

function useAsync<T>(fetcher: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [state, setState] = useState<AsyncState<T>>({ data: null, loading: true, error: null });

  useEffect(() => {
    let cancelled = false;
    // Чат поллит каждые несколько секунд (см. ChatModal) — фоновое обновление
    // не должно гасить уже показанные сообщения до null перед каждым тиком,
    // иначе список на долю секунды пустеет и появляется заново, выглядит как
    // "чат перезагружается". loading=true показываем только на самой первой
    // загрузке (пока данных ещё вообще не было) — дальше старые данные тихо
    // остаются на экране, пока не придут новые.
    setState((prev) => ({ data: prev.data, loading: prev.data === null, error: null }));

    fetcher()
      .then((data) => {
        if (!cancelled) setState({ data, loading: false, error: null });
      })
      .catch((err: unknown) => {
        if (!cancelled) setState((prev) => ({ data: prev.data, loading: false, error: err instanceof Error ? err.message : String(err) }));
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return state;
}

interface ProfileIdentity {
  id: number;
  userId: number;
}

// Order.FarmerId — это FarmerProfile.Id, а Conversation.FarmerId — User.Id
// (та же нестыковка, что уже решалась для уведомлений — см. комментарии в
// data/customer.ts/submitCustomerOrder и data/adminEntities.ts/
// notifyCustomerAboutOrderStatus). Грузим весь список профилей один раз и
// сопоставляем на фронте.
export function useFarmerUserIdMap() {
  const { data } = useAsync(() => apiGet<ProfileIdentity[]>("/farmer-profiles"), []);
  const map = new Map<number, number>();
  data?.forEach((p) => map.set(p.id, p.userId));
  return map;
}

export function useCustomerUserIdMap() {
  const { data } = useAsync(() => apiGet<ProfileIdentity[]>("/customer-profiles"), []);
  const map = new Map<number, number>();
  data?.forEach((p) => map.set(p.id, p.userId));
  return map;
}

// FarmerLayout/CustomerLayout (бейдж "Сообщения" в сайдбаре) и ChatModal —
// разные компоненты, у каждого свой локальный refreshKey. Без этого моста
// прочтение/отправка сообщений внутри открытого чата не отражались бы на
// бейдже в сайдбаре, пока весь layout не перемонтируется — тот же приём, что
// и у notifyFarmerOrdersChanged (data/farmer.ts).
const chatListeners = new Set<() => void>();

export function notifyChatChanged() {
  chatListeners.forEach((listener) => listener());
}

function useChatExternalRefresh(): number {
  const [externalRefresh, setExternalRefresh] = useState(0);
  useEffect(() => {
    const listener = () => setExternalRefresh((k) => k + 1);
    chatListeners.add(listener);
    return () => {
      chatListeners.delete(listener);
    };
  }, []);
  return externalRefresh;
}

// /api/conversations не фильтрует на бэкенде — грузим всё и сопоставляем на
// фронте, как и везде в проекте.
export function useConversations(refreshKey = 0) {
  const externalRefresh = useChatExternalRefresh();
  const { data, loading, error } = useAsync(() => apiGet<ConversationDto[]>("/conversations"), [refreshKey, externalRefresh]);
  return { conversations: data, loading, error };
}

export function useChatMessagesAll(refreshKey = 0) {
  const externalRefresh = useChatExternalRefresh();
  const { data, loading, error } = useAsync(() => apiGet<ChatMessageDto[]>("/chat-messages"), [refreshKey, externalRefresh]);
  return { messages: data, loading, error };
}

export function useChatMessages(conversationId: number | null, refreshKey = 0) {
  const externalRefresh = useChatExternalRefresh();
  const { data, loading, error } = useAsync(
    () => (conversationId ? apiGet<ChatMessageDto[]>("/chat-messages") : Promise.resolve(null as never)),
    [conversationId, refreshKey, externalRefresh],
  );
  const messages =
    data
      ?.filter((m) => m.conversationId === conversationId)
      .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()) ?? null;
  return { messages, loading, error };
}

// Один Conversation на пару customerUserId/farmerUserId — независимо от
// заказов (единая уникальность на бэкенде, ConversationService.ValidatePairFree)
// — если у этой пары уже есть чат (по любому поводу — вопрос о товаре или
// один из заказов), переиспользуем его, а не плодим новый под каждый заказ.
// orderId передаётся только затем, чтобы ПЕРВОЕ сообщение новой (ещё не
// существующей) переписки сохранило, с чего она началась. POST
// /api/conversations не возвращает id созданной записи (та же схема, что и у
// Order/ProductListing) — находим её через повторный GET.
export async function getOrCreateConversation(
  orderId: number | null,
  customerUserId: number,
  farmerUserId: number,
): Promise<ConversationDto> {
  const existing = await apiGet<ConversationDto[]>("/conversations");
  const matches = (c: ConversationDto) => c.customerId === customerUserId && c.farmerId === farmerUserId;

  const found = existing.find(matches);
  if (found) return found;

  await apiPost<string>("/conversations", {
    orderId,
    customerId: customerUserId,
    farmerId: farmerUserId,
    isClosed: false,
  });

  const refreshed = await apiGet<ConversationDto[]>("/conversations");
  const created = refreshed.find(matches);
  if (!created) throw new Error("Чат создан, но не найден");
  return created;
}

export function sendChatMessage(conversationId: number, senderId: number, message: string) {
  return apiPost<string>("/chat-messages", { conversationId, senderId, message, imageUrl: null, isRead: false });
}

// Фото к сообщению — тот же multipart-приём, что и uploadProductImage/
// uploadAvatar (data/farmer.ts, AuthContext.tsx).
export function uploadChatImage(conversationId: number, senderId: number, caption: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("conversationId", String(conversationId));
  formData.append("senderId", String(senderId));
  if (caption) formData.append("caption", caption);
  return apiUpload<ChatMessageDto>("/chat-messages/upload", formData);
}

export function markChatMessageRead(message: ChatMessageDto) {
  return apiPut<string>(`/chat-messages/${message.id}`, {
    id: message.id,
    conversationId: message.conversationId,
    senderId: message.senderId,
    message: message.message,
    imageUrl: message.imageUrl,
    isRead: true,
  });
}
