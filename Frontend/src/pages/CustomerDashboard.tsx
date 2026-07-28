import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ArrowRight, Bell, Heart, MessageCircle, PackageCheck, ShoppingBag } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { Card } from "@/components/ui/Card";
import { Avatar } from "@/components/ui/Avatar";
import { EmptyState } from "@/components/ui/EmptyState";
import { useCustomerAccountStatus } from "@/components/layout/AccountMenu";
import { useAuth } from "@/context/AuthContext";
import { useFavorites } from "@/context/FavoritesContext";
import { resolveMediaUrl } from "@/lib/api";
import { formatNumber, formatSomoni, formatDateTime } from "@/lib/utils";
import { OrderStatus, useCustomerOrders, useCustomerProfile } from "@/data/customer";

const ACTIVE_STATUSES = new Set<number>([
  OrderStatus.Pending,
  OrderStatus.Accepted,
  OrderStatus.Preparing,
  OrderStatus.ReadyForPickup,
  OrderStatus.CourierAssigned,
  OrderStatus.PickedUp,
  OrderStatus.InDelivery,
]);
const DONE_STATUSES = new Set<number>([OrderStatus.Delivered, OrderStatus.Completed]);

const STATUS_CLASSES: Record<number, string> = {
  [OrderStatus.Pending]: "bg-stone-100 text-stone-600 dark:bg-stone-800 dark:text-stone-300",
  [OrderStatus.Accepted]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
  [OrderStatus.Preparing]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [OrderStatus.ReadyForPickup]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [OrderStatus.CourierAssigned]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.PickedUp]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.InDelivery]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [OrderStatus.Delivered]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [OrderStatus.Completed]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [OrderStatus.Cancelled]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

const STATUS_KEYS: Record<number, string> = {
  [OrderStatus.Pending]: "pending",
  [OrderStatus.Accepted]: "accepted",
  [OrderStatus.Rejected]: "rejected",
  [OrderStatus.Preparing]: "preparing",
  [OrderStatus.ReadyForPickup]: "readyForPickup",
  [OrderStatus.CourierAssigned]: "courierAssigned",
  [OrderStatus.PickedUp]: "pickedUp",
  [OrderStatus.InDelivery]: "inDelivery",
  [OrderStatus.Delivered]: "delivered",
  [OrderStatus.Completed]: "completed",
  [OrderStatus.Cancelled]: "cancelled",
};


// Плитка быстрого действия в hero-блоке — иконка + подпись, тихий счётчик
// справа только если есть что показать (тот же принцип, что и в дропдауне
// шапки, см. components/layout/AccountMenu.tsx).
function QuickTile({ to, icon, label, badge }: { to: string; icon: ReactNode; label: string; badge?: number }) {
  return (
    <Link
      to={to}
      className="flex items-center gap-3 rounded-2xl border border-stone-100 bg-white p-4 shadow-(--shadow-soft) transition hover:-translate-y-0.5 hover:shadow-(--shadow-card) dark:border-stone-800 dark:bg-stone-900"
    >
      <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-400">
        {icon}
      </span>
      <span className="min-w-0 flex-1 truncate text-sm font-medium text-stone-800 dark:text-stone-100">{label}</span>
      {!!badge && badge > 0 && (
        <span className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-clay-500 px-1.5 text-[11px] font-bold text-white">
          {badge > 9 ? "9+" : badge}
        </span>
      )}
    </Link>
  );
}

export function CustomerDashboard() {
  const { t } = useTranslation(["customer", "common"]);
  const { user } = useAuth();
  const { profile, loading: profileLoading, error: profileError } = useCustomerProfile();
  const { orders, loading: ordersLoading, error: ordersError } = useCustomerOrders(profile?.id ?? null);
  const { favoriteIds } = useFavorites();
  const { unreadMessagesCount, unreadNotificationsCount } = useCustomerAccountStatus();

  if (profileLoading || (profile && ordersLoading)) return <PageLoader />;

  if (profileError || ordersError || !profile || !orders) {
    return (
      <EmptyState
        icon={<ShoppingBag size={26} />}
        title={t("dashboard.errorTitle")}
        description={profileError ?? ordersError ?? t("dashboard.errorDescription")}
      />
    );
  }

  const activeCount = orders.filter((o) => ACTIVE_STATUSES.has(o.status)).length;
  const totalSpent = orders.filter((o) => DONE_STATUSES.has(o.status)).reduce((sum, o) => sum + o.totalAmount, 0);
  const recent = orders.slice(0, 5);

  return (
    <div className="flex flex-col gap-6">
      {/* Hero — аватар, имя и ключевые цифры одним взглядом, без захода в
          отдельные разделы (вдохновлено разделом "профиль" у крупных
          e-commerce, но только реальными данными — без выдуманных бонусов/
          уровней, которых у Market.tj просто нет). */}
      <div className="overflow-hidden rounded-3xl bg-linear-to-br from-grove-700 via-grove-700 to-grove-900 p-6 text-white shadow-(--shadow-soft) sm:p-8">
        <div className="flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-4">
            <Avatar
              name={user?.fullName ?? ""}
              src={user?.avatarUrl ? resolveMediaUrl(user.avatarUrl) : undefined}
              size={72}
              ring
            />
            <div className="min-w-0">
              <p className="truncate font-display text-2xl">{user?.fullName}</p>
              <p className="mt-1 truncate text-sm text-grove-100">
                {profile.region}, {profile.district}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-5 sm:gap-8">
            <div>
              <p className="font-display text-2xl sm:text-3xl">{formatNumber(orders.length)}</p>
              <p className="text-xs text-grove-100">{t("dashboard.stats.totalOrders")}</p>
            </div>
            <div className="h-10 w-px bg-white/20" />
            <div>
              <p className="font-display text-2xl sm:text-3xl">{formatNumber(activeCount)}</p>
              <p className="text-xs text-grove-100">{t("dashboard.stats.activeOrders")}</p>
            </div>
            <div className="h-10 w-px bg-white/20" />
            <div>
              <p className="font-display text-2xl sm:text-3xl">
                {formatSomoni(totalSpent)} {t("common.somoni")}
              </p>
              <p className="text-xs text-grove-100">{t("dashboard.stats.totalSpent")}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Быстрые действия — прямые ссылки на разделы, которые чаще всего
          нужны покупателю, с теми же живыми счётчиками, что и в шапке. */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <QuickTile to="/catalog" icon={<ShoppingBag size={18} />} label={t("dashboard.goToCatalog")} />
        <QuickTile
          to="/customer/messages"
          icon={<MessageCircle size={18} />}
          label={t("common:account.messages")}
          badge={unreadMessagesCount}
        />
        <QuickTile
          to="/customer/notifications"
          icon={<Bell size={18} />}
          label={t("common:account.notifications")}
          badge={unreadNotificationsCount}
        />
        <QuickTile
          to="/catalog?favorites=1"
          icon={<Heart size={18} />}
          label={t("common:account.favorites")}
          badge={favoriteIds.length}
        />
      </div>

      <Card>
        <div className="flex items-center justify-between">
          <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("dashboard.recentOrders")}</h2>
          <Link to="/customer/orders" className="flex items-center gap-1 text-xs font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400">
            {t("dashboard.viewAll")}
            <ArrowRight size={13} />
          </Link>
        </div>

        {recent.length === 0 ? (
          <div className="mt-6 flex flex-col items-center gap-3 py-8 text-center">
            <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500">
              <PackageCheck size={22} />
            </span>
            <p className="text-sm text-stone-500 dark:text-stone-400">{t("dashboard.noOrdersYet")}</p>
            <Link to="/catalog" className="mt-1 inline-flex items-center gap-2 rounded-xl bg-grove-700 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-grove-800">
              {t("dashboard.goToCatalog")}
              <ArrowRight size={14} />
            </Link>
          </div>
        ) : (
          <ul className="mt-4 flex flex-col divide-y divide-stone-50 dark:divide-stone-800/60">
            {recent.map((order) => (
              <li key={order.id} className="flex items-center justify-between gap-3 py-3.5">
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{order.orderNumber}</p>
                  <p className="text-xs text-stone-400 dark:text-stone-500">{formatDateTime(order.createdAt)}</p>
                </div>
                <div className="flex shrink-0 items-center gap-4">
                  <span className="text-sm font-semibold text-stone-800 dark:text-stone-100">
                    {formatSomoni(order.totalAmount)} {t("common.somoni")}
                  </span>
                  <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[order.status] ?? STATUS_CLASSES[OrderStatus.Pending]}`}>
                    {t(`orders.status.${STATUS_KEYS[order.status] ?? "pending"}`)}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
