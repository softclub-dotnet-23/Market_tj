import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Loader2, MessageCircle, Paperclip, Send } from "lucide-react";
import { Modal } from "@/components/ui/Modal";
import { Avatar } from "@/components/ui/Avatar";
import {
  type ConversationDto,
  getOrCreateConversation,
  markChatMessageRead,
  notifyChatChanged,
  sendChatMessage,
  uploadChatImage,
  useChatMessages,
} from "@/data/chat";
import { resolveMediaUrl } from "@/lib/api";
import { cn, formatChatDate, formatTime } from "@/lib/utils";

const POLL_INTERVAL_MS = 4000;
const MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024;
const ALLOWED_IMAGE_TYPES = ["image/jpeg", "image/png", "image/webp"];

interface ChatModalProps {
  open: boolean;
  onClose: () => void;
  // null — чат ещё не привязан к заказу (вопрос фермеру про товар).
  orderId: number | null;
  orderNumber: string | null;
  customerUserId: number | null;
  farmerUserId: number | null;
  currentUserId: number;
  otherPartyName: string;
  ns: "customer" | "farmer";
}

export function ChatModal({
  open,
  onClose,
  orderId,
  orderNumber,
  customerUserId,
  farmerUserId,
  currentUserId,
  otherPartyName,
  ns,
}: ChatModalProps) {
  const { t } = useTranslation(ns);
  const [conversation, setConversation] = useState<ConversationDto | null>(null);
  const [resolving, setResolving] = useState(false);
  const [resolveError, setResolveError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const [draft, setDraft] = useState("");
  const [sending, setSending] = useState(false);
  const [uploading, setUploading] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const markedIdsRef = useRef(new Set<number>());
  const lastMessageCountRef = useRef(0);

  const { messages } = useChatMessages(conversation?.id ?? null, refreshKey);

  // Резолвим/создаём Conversation заново при каждом открытии — не кэшируем
  // между заказами/товарами, у каждого свой чат.
  useEffect(() => {
    if (!open || customerUserId === null || farmerUserId === null) return;
    let cancelled = false;
    setResolving(true);
    setResolveError(null);
    getOrCreateConversation(orderId, customerUserId, farmerUserId)
      .then((c) => {
        if (!cancelled) setConversation(c);
      })
      .catch((err: unknown) => {
        if (!cancelled) setResolveError(err instanceof Error ? err.message : String(err));
      })
      .finally(() => {
        if (!cancelled) setResolving(false);
      });
    return () => {
      cancelled = true;
    };
  }, [open, orderId, customerUserId, farmerUserId]);

  // Пока модалка открыта — простой поллинг новых сообщений (в проекте нет
  // WebSocket/SignalR, спецификация тоже не требует реального времени, раздел 13.11 ТЗ).
  useEffect(() => {
    if (!open || !conversation) return;
    const interval = setInterval(() => setRefreshKey((k) => k + 1), POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [open, conversation]);

  // Авто-прочтение чужих сообщений — тот же приём "открыл = прочитал", что и
  // в NotificationCenter.
  useEffect(() => {
    if (!messages) return;
    const unread = messages.filter((m) => !m.isRead && m.senderId !== currentUserId && !markedIdsRef.current.has(m.id));
    if (unread.length === 0) return;
    unread.forEach((m) => markedIdsRef.current.add(m.id));
    Promise.all(unread.map((m) => markChatMessageRead(m)))
      .then(() => {
        setRefreshKey((k) => k + 1);
        notifyChatChanged();
      })
      .catch(() => {});
  }, [messages, currentUserId]);

  // Прокручиваем к последнему сообщению только когда реально пришло новое —
  // поллинг каждые несколько секунд иначе дёргал бы скролл вниз при КАЖДОМ
  // тике, даже если ничего не изменилось (и сбивал бы того, кто в этот момент
  // читает историю выше).
  useEffect(() => {
    if (!messages) return;
    if (messages.length > lastMessageCountRef.current) {
      scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
    }
    lastMessageCountRef.current = messages.length;
  }, [messages]);

  // Автофокус поля ввода при открытии — сразу при открытии модалки, не
  // дожидаясь резолва Conversation (запрос к серверу занимает время, а поле
  // теперь разрешено набирать текст ещё до его завершения, см. ниже) — иначе
  // фокус выставлялся слишком поздно после сетевого ответа, и браузер уже не
  // считал это действием пользователя.
  useEffect(() => {
    if (open) textareaRef.current?.focus();
  }, [open]);

  const handleClose = () => {
    setDraft("");
    setConversation(null);
    setResolveError(null);
    markedIdsRef.current = new Set();
    lastMessageCountRef.current = 0;
    onClose();
  };

  const handleSend = async () => {
    const text = draft.trim();
    if (!text || !conversation || sending) return;
    setSending(true);
    try {
      await sendChatMessage(conversation.id, currentUserId, text);
      setDraft("");
      setRefreshKey((k) => k + 1);
      notifyChatChanged();
    } catch (err) {
      toast.error(t("chat.sendError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setSending(false);
    }
  };

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file || !conversation || uploading) return;

    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
      toast.error(t("chat.photoInvalidType"));
      return;
    }
    if (file.size > MAX_IMAGE_SIZE_BYTES) {
      toast.error(t("chat.photoTooLarge"));
      return;
    }

    setUploading(true);
    try {
      await uploadChatImage(conversation.id, currentUserId, draft.trim(), file);
      setDraft("");
      setRefreshKey((k) => k + 1);
      notifyChatChanged();
    } catch (err) {
      toast.error(t("chat.photoUploadError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setUploading(false);
    }
  };

  const busy = sending || uploading;

  // Разделитель дат над первым сообщением каждого нового календарного дня —
  // вместо полной даты в каждом пузыре (было избыточно и загромождало чат).
  let lastDateKey: string | null = null;

  return (
    <Modal open={open} onClose={handleClose} className="flex h-[70vh] max-h-160 w-full flex-col overflow-hidden p-0 sm:max-w-md">
      <div className="flex items-center gap-3 border-b border-stone-100 bg-white px-5 py-4 dark:border-stone-800 dark:bg-stone-900">
        <Avatar name={otherPartyName} size={40} />
        <div className="min-w-0 flex-1">
          <p className="truncate font-display text-base text-stone-900 dark:text-stone-50">{otherPartyName}</p>
          <p className="truncate text-xs text-stone-400 dark:text-stone-500">
            {orderNumber ? t("chat.aboutOrder", { orderNumber }) : t("chat.generalInquiry")}
          </p>
        </div>
      </div>

      <div ref={scrollRef} className="flex-1 space-y-1 overflow-y-auto bg-stone-25 px-5 py-4 dark:bg-stone-950/40">
        {resolving && !conversation && (
          <div className="flex h-full items-center justify-center text-sm text-stone-400 dark:text-stone-500">{t("chat.loading")}</div>
        )}
        {resolveError && <div className="flex h-full items-center justify-center px-4 text-center text-sm text-danger">{resolveError}</div>}
        {conversation && messages && messages.length === 0 && (
          <div className="flex h-full flex-col items-center justify-center gap-2 text-center text-stone-400 dark:text-stone-500">
            <MessageCircle size={28} />
            <p className="text-sm">{t("chat.emptyDescription")}</p>
          </div>
        )}
        {messages?.map((m) => {
          const own = m.senderId === currentUserId;
          const dateKey = m.createdAt.slice(0, 10);
          const showDateSeparator = dateKey !== lastDateKey;
          lastDateKey = dateKey;
          return (
            <div key={m.id}>
              {showDateSeparator && (
                <div className="my-3 flex justify-center">
                  <span className="rounded-full bg-stone-100 px-3 py-1 text-[11px] font-medium text-stone-500 dark:bg-stone-800 dark:text-stone-400">
                    {formatChatDate(m.createdAt)}
                  </span>
                </div>
              )}
              <div className={cn("flex py-1", own ? "justify-end" : "justify-start")}>
                <div
                  className={cn(
                    "max-w-[75%] overflow-hidden rounded-2xl text-sm shadow-sm",
                    own ? "rounded-br-md bg-grove-700 text-white" : "rounded-bl-md bg-white text-stone-800 dark:bg-stone-800 dark:text-stone-100",
                  )}
                >
                  {m.imageUrl && (
                    <img src={resolveMediaUrl(m.imageUrl)} alt="" className="max-h-64 w-full object-cover" loading="lazy" />
                  )}
                  <div className={cn("px-4 py-2.5", !m.message && "pb-2 pt-1.5")}>
                    {m.message && <p className="whitespace-pre-wrap wrap-break-word">{m.message}</p>}
                    <p className={cn("mt-1 text-right text-[10px]", own ? "text-grove-100" : "text-stone-400 dark:text-stone-500")}>
                      {formatTime(m.createdAt)}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      <div className="flex items-end gap-2 border-t border-stone-100 bg-white p-4 dark:border-stone-800 dark:bg-stone-900">
        <input ref={fileInputRef} type="file" accept="image/jpeg,image/png,image/webp" className="hidden" onChange={handleFileSelect} />
        <button
          onClick={() => fileInputRef.current?.click()}
          disabled={!conversation || busy}
          aria-label={t("chat.attachPhoto")}
          className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-stone-200 text-stone-500 transition hover:border-grove-400 hover:text-grove-700 disabled:cursor-not-allowed disabled:opacity-40 dark:border-stone-700 dark:text-stone-400 dark:hover:border-grove-600 dark:hover:text-grove-400"
        >
          {uploading ? <Loader2 size={16} className="animate-spin" /> : <Paperclip size={16} />}
        </button>
        <textarea
          ref={textareaRef}
          autoFocus
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter" && !e.shiftKey) {
              e.preventDefault();
              handleSend();
            }
          }}
          placeholder={t("chat.placeholder")}
          rows={1}
          disabled={busy}
          className="max-h-28 min-h-10 flex-1 resize-none rounded-xl border border-stone-200 bg-stone-25 px-3.5 py-2.5 text-sm outline-none placeholder:text-stone-400 focus:border-grove-400 disabled:opacity-60 dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100 dark:placeholder:text-stone-500"
        />
        <button
          onClick={handleSend}
          disabled={!conversation || busy || !draft.trim()}
          aria-label={t("chat.send")}
          className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-grove-700 text-white transition hover:bg-grove-800 disabled:cursor-not-allowed disabled:opacity-40"
        >
          {sending ? <Loader2 size={16} className="animate-spin" /> : <Send size={16} />}
        </button>
      </div>
    </Modal>
  );
}
