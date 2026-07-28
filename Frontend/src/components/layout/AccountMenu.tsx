import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Bell, Heart, MessageCircle, Package, UserRound } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useAuth } from "@/context/AuthContext";
import { useFavorites } from "@/context/FavoritesContext";
import { OrderStatus, useCustomerOrders, useCustomerProfile } from "@/data/customer";
import { useFarmerNotifications } from "@/data/farmer";
import { useChatMessagesAll, useConversations } from "@/data/chat";
import { cn } from "@/lib/utils";

// Публичная шапка/мобильное меню (Header.tsx/MobileMenu.tsx) — единственные
// два места, где авторизованному покупателю нужен один и тот же набор
// быстрых ссылок на личный кабинет с живыми счётчиками, без захода в сам
// кабинет. Общий хук + компонент вместо дублирования во всех местах.
export function useCustomerAccountStatus() {
  const { user } = useAuth();
  const { profile } = useCustomerProfile();
  const { orders } = useCustomerOrders(profile?.id ?? null);
  // "Активный" — заказ ещё не завершился ни хорошо, ни плохо (не Completed/
  // Rejected/Cancelled) — именно такие требуют внимания покупателя.
  const activeOrdersCount = (orders ?? []).filter(
    (o) => o.status !== OrderStatus.Rejected && o.status !== OrderStatus.Cancelled && o.status !== OrderStatus.Completed,
  ).length;

  const { notifications } = useFarmerNotifications(user?.userId ?? null);
  const unreadNotificationsCount = (notifications ?? []).filter((n) => !n.isRead).length;

  const { conversations } = useConversations();
  const { messages: allChatMessages } = useChatMessagesAll();
  const myConversationIds = new Set((conversations ?? []).filter((c) => c.customerId === user?.userId).map((c) => c.id));
  // Считаем непрочитанные ЧАТЫ, а не сумму сообщений в них — тот же принцип,
  // что и в бейдже "Сообщения" панели покупателя (CustomerLayout.tsx).
  const unreadMessagesCount = new Set(
    (allChatMessages ?? [])
      .filter((m) => myConversationIds.has(m.conversationId) && !m.isRead && m.senderId !== user?.userId)
      .map((m) => m.conversationId),
  ).size;

  return { activeOrdersCount, unreadMessagesCount, unreadNotificationsCount };
}

function CountBadge({ count }: { count: number }) {
  if (count <= 0) return null;
  return (
    <span className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-clay-500 px-1.5 text-[11px] font-bold text-white">
      {count > 9 ? "9+" : count}
    </span>
  );
}

interface AccountLinkItem {
  to: string;
  icon: LucideIcon;
  label: string;
  hint: string;
  badge: number;
}

// Компактная строка-ссылка: иконка + заголовок + короткое пояснение слева,
// счётчик справа (если есть что считать) — единый ряд для десктопного
// дропдауна и мобильного дровера.
function AccountLinkRow({ item, onNavigate }: { item: AccountLinkItem; onNavigate: () => void }) {
  return (
    <Link
      to={item.to}
      onClick={onNavigate}
      className="flex items-center gap-2.5 rounded-xl px-2.5 py-2 text-left transition hover:bg-stone-50 dark:hover:bg-stone-800"
    >
      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-stone-100 text-stone-500 dark:bg-stone-800 dark:text-stone-400">
        <item.icon size={15} />
      </span>
      <span className="min-w-0 flex-1">
        <span className="block truncate text-sm font-medium text-stone-800 dark:text-stone-100">{item.label}</span>
        <span className="block truncate text-xs text-stone-400 dark:text-stone-500">{item.hint}</span>
      </span>
      <CountBadge count={item.badge} />
    </Link>
  );
}

// Список быстрых ссылок личного кабинета покупателя — используется и в
// десктопном дропдауне (Header.tsx), и в мобильном дровере (MobileMenu.tsx).
// onNavigate закрывает меню/дровер после клика (тот же коллбэк, что уже
// передаётся туда для остальных пунктов).
export function CustomerAccountLinks({ onNavigate, className }: { onNavigate: () => void; className?: string }) {
  const { t } = useTranslation("common");
  const { favoriteIds } = useFavorites();
  const { activeOrdersCount, unreadMessagesCount, unreadNotificationsCount } = useCustomerAccountStatus();

  const items: AccountLinkItem[] = [
    { to: "/customer/orders", icon: Package, label: t("account.myOrders"), hint: t("account.myOrdersHint"), badge: activeOrdersCount },
    { to: "/customer/messages", icon: MessageCircle, label: t("account.messages"), hint: t("account.messagesHint"), badge: unreadMessagesCount },
    {
      to: "/customer/notifications",
      icon: Bell,
      label: t("account.notifications"),
      hint: t("account.notificationsHint"),
      badge: unreadNotificationsCount,
    },
    { to: "/catalog?favorites=1", icon: Heart, label: t("account.favorites"), hint: t("account.favoritesHint"), badge: favoriteIds.length },
    { to: "/customer/profile", icon: UserRound, label: t("account.profile"), hint: t("account.profileHint"), badge: 0 },
  ];

  return (
    <div className={cn("flex flex-col gap-0.5", className)}>
      {items.map((item) => (
        <AccountLinkRow key={item.to} item={item} onNavigate={onNavigate} />
      ))}
    </div>
  );
}

// Отдельная иконка-ярлык в шапке (рядом с избранным/корзиной) — прямой
// переход к заказам одним кликом, с тихим бейджем только когда есть что
// показать (раздел требования: "может показывать бейдж, если есть активный
// заказ" — не обязательный визуальный шум, если заказов нет).
export function CustomerOrderShortcut({ className }: { className?: string }) {
  const { t } = useTranslation("common");
  const { activeOrdersCount } = useCustomerAccountStatus();

  return (
    <Link
      to="/customer/orders"
      aria-label={t("account.myOrders")}
      className={cn(
        "relative hidden h-10 w-10 items-center justify-center rounded-full text-stone-600 transition hover:bg-stone-100 dark:text-stone-300 dark:hover:bg-stone-800 sm:flex",
        className,
      )}
    >
      <Package size={19} />
      {activeOrdersCount > 0 && (
        <span className="absolute -right-0.5 -top-0.5 flex h-4.5 w-4.5 items-center justify-center rounded-full bg-clay-500 text-[10px] font-bold text-white">
          {activeOrdersCount > 9 ? "9+" : activeOrdersCount}
        </span>
      )}
    </Link>
  );
}
