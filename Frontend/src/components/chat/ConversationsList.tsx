import { useState } from "react";
import { useTranslation } from "react-i18next";
import { MessageCircle } from "lucide-react";
import { Avatar } from "@/components/ui/Avatar";
import { EmptyState } from "@/components/ui/EmptyState";
import { PageLoader } from "@/components/layout/PageLoader";
import { ChatModal } from "@/components/chat/ChatModal";
import { type ConversationDto, useChatMessagesAll, useConversations } from "@/data/chat";
import { cn, formatDateTime } from "@/lib/utils";

interface ConversationsListProps {
  ns: "customer" | "farmer";
  currentUserId: number;
  resolveOtherPartyName: (conversation: ConversationDto) => string;
  resolveOrderNumber: (orderId: number | null) => string | null;
}

// Общий список переписок для покупателя и фермера — единственная точка
// входа в чат (раньше отдельная кнопка была ещё и в каждой строке таблицы
// заказов, но два разных места для одного и того же чата только путали).
export function ConversationsList({ ns, currentUserId, resolveOtherPartyName, resolveOrderNumber }: ConversationsListProps) {
  const { t } = useTranslation(ns);
  const [openConversation, setOpenConversation] = useState<ConversationDto | null>(null);
  const { conversations, loading: conversationsLoading, error } = useConversations();
  const { messages: allMessages, loading: messagesLoading } = useChatMessagesAll();

  if (conversationsLoading || messagesLoading) return <PageLoader />;

  if (error || !conversations) {
    return <EmptyState icon={<MessageCircle size={26} />} title={t("messages.errorTitle")} description={error ?? t("messages.errorDescription")} />;
  }

  const mine = conversations.filter((c) => (ns === "customer" ? c.customerId === currentUserId : c.farmerId === currentUserId));

  const rows = mine
    .map((conversation) => {
      const msgs = (allMessages ?? [])
        .filter((m) => m.conversationId === conversation.id)
        .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
      const last = msgs[msgs.length - 1];
      const unreadCount = msgs.filter((m) => !m.isRead && m.senderId !== currentUserId).length;
      return { conversation, last, unreadCount };
    })
    .sort((a, b) => new Date(b.conversation.updatedAt).getTime() - new Date(a.conversation.updatedAt).getTime());

  if (rows.length === 0) {
    return <EmptyState icon={<MessageCircle size={26} />} title={t("messages.emptyTitle")} description={t("messages.emptyDescription")} />;
  }

  return (
    <>
      <div className="flex flex-col divide-y divide-stone-100 overflow-hidden rounded-3xl border border-stone-100 bg-white dark:divide-stone-800 dark:border-stone-800 dark:bg-stone-900">
        {rows.map(({ conversation, last, unreadCount }) => {
          const name = resolveOtherPartyName(conversation);
          const orderNumber = resolveOrderNumber(conversation.orderId);
          const preview = last ? (last.message || (last.imageUrl ? t("messages.photoPreview") : "")) : t("messages.noMessagesYet");
          return (
            <button
              key={conversation.id}
              onClick={() => setOpenConversation(conversation)}
              className="flex items-center gap-4 px-5 py-4 text-left transition hover:bg-stone-50 dark:hover:bg-stone-800/60"
            >
              <Avatar name={name} size={44} />
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <p className={cn("truncate text-sm", unreadCount > 0 ? "font-semibold text-stone-900 dark:text-stone-50" : "font-medium text-stone-800 dark:text-stone-100")}>
                    {name}
                  </p>
                  {last && <span className="shrink-0 text-xs text-stone-400 dark:text-stone-500">{formatDateTime(last.createdAt)}</span>}
                </div>
                <p className="truncate text-xs text-stone-400 dark:text-stone-500">
                  {orderNumber ? t("chat.aboutOrder", { orderNumber }) : t("chat.generalInquiry")}
                </p>
                <p className={cn("truncate text-sm", unreadCount > 0 ? "font-medium text-stone-700 dark:text-stone-200" : "text-stone-500 dark:text-stone-400")}>
                  {preview}
                </p>
              </div>
              {unreadCount > 0 && (
                <span className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-clay-500 px-1.5 text-[11px] font-bold text-white">
                  {unreadCount > 9 ? "9+" : unreadCount}
                </span>
              )}
            </button>
          );
        })}
      </div>

      <ChatModal
        open={openConversation !== null}
        onClose={() => setOpenConversation(null)}
        orderId={openConversation?.orderId ?? null}
        orderNumber={openConversation ? resolveOrderNumber(openConversation.orderId) : null}
        customerUserId={openConversation?.customerId ?? null}
        farmerUserId={openConversation?.farmerId ?? null}
        currentUserId={currentUserId}
        otherPartyName={openConversation ? resolveOtherPartyName(openConversation) : ""}
        ns={ns}
      />
    </>
  );
}
