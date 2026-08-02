import { useEffect, useRef } from "react";
import { useTranslation } from "react-i18next";
import { Bell } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { useAuth } from "@/context/AuthContext";
import { formatDate } from "@/lib/utils";
import { markNotificationRead, notifyNotificationsChanged, useFarmerNotifications } from "@/data/farmer";

// Открыл — прочитал, как в Instagram Direct: отдельная кнопка "прочитано" по
// каждому уведомлению неудобна, поэтому при первом открытии страницы всё
// непрочитанное автоматически помечается прочитанным, а бейдж в сайдбаре
// (другой вызов того же хука в Layout) обновляется сразу через
// notifyNotificationsChanged(), без перезагрузки страницы.
export function NotificationCenter({ ns }: { ns: "farmer" | "admin" | "customer" | "courier" }) {
  const { t } = useTranslation(ns);
  const { user } = useAuth();
  const { notifications, loading, error } = useFarmerNotifications(user?.userId ?? null);
  const markedRef = useRef(false);

  useEffect(() => {
    if (loading || !notifications || markedRef.current) return;
    markedRef.current = true;
    const unread = notifications.filter((n) => !n.isRead);
    if (unread.length === 0) return;
    Promise.all(unread.map((n) => markNotificationRead(n).catch(() => undefined))).then(() => {
      notifyNotificationsChanged();
    });
  }, [loading, notifications]);

  if (loading) return <PageLoader />;

  if (error || !notifications) {
    return <EmptyState icon={<Bell size={26} />} title={t("notificationsPage.errorTitle")} description={error ?? t("notificationsPage.errorDescription")} />;
  }

  if (notifications.length === 0) {
    return <EmptyState icon={<Bell size={26} />} title={t("notificationsPage.emptyTitle")} description={t("notificationsPage.emptyDescription")} />;
  }

  return (
    <div className="flex flex-col gap-3">
      {notifications.map((notification) => (
        <div
          key={notification.id}
          className={`flex items-start gap-3 rounded-2xl border p-5 transition ${
            notification.isRead
              ? "border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900"
              : "border-grove-200 bg-grove-50/60 dark:border-grove-800 dark:bg-grove-950/40"
          }`}
        >
          <span
            className={`mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-xl ${
              notification.isRead
                ? "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
                : "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
            }`}
          >
            <Bell size={16} />
          </span>
          <div>
            <p className="text-sm font-semibold text-stone-800 dark:text-stone-100">{notification.title}</p>
            <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">{notification.message}</p>
            <p className="mt-2 text-xs text-stone-400 dark:text-stone-500">{formatDate(notification.createdAt)}</p>
          </div>
        </div>
      ))}
    </div>
  );
}
