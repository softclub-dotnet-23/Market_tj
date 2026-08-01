import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ArrowLeft, Calendar, Mail, MapPin, MessageSquare, Phone, ShoppingCart, Users, Wallet } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Avatar } from "@/components/ui/Avatar";
import { Card } from "@/components/ui/Card";
import { StatCard } from "@/components/ui/StatCard";
import { RatingStars } from "@/components/ui/RatingStars";
import { formatDate, formatDateTime, formatSomoni } from "@/lib/utils";
import { ORDER_STATUS_CLASSES, ORDER_STATUS_KEYS, OrderStatus } from "@/lib/orderStatus";
import { useAdminCustomerProfiles, useAdminOrders, useAdminReviews, useAdminUserById } from "@/data/adminEntities";

// Карточка одного покупателя в админке (/admin/users/:id) — по прямому
// запросу пользователя, тем же принципом, что и AdminFarmerDetail.tsx:
// сколько у него заказов, сколько потратил, какие отзывы оставил — всё в
// одном месте вместо голого списка "имя/email/телефон" на странице
// "Покупатели". Order.CustomerId/Review.CustomerId ссылаются на
// CustomerProfile.Id, а не на User.Id из маршрута — сперва резолвим профиль.
const CUSTOMER_TYPE_KEYS: Record<number, string> = { 1: "retail", 2: "wholesale" };

export function AdminCustomerDetail() {
  const { t } = useTranslation("admin");
  const { id } = useParams();
  const navigate = useNavigate();
  const userId = id ? Number(id) : null;

  const { account, loading: accountLoading, error: accountError } = useAdminUserById(userId);
  const { profiles, loading: profilesLoading } = useAdminCustomerProfiles();
  const { orders, loading: ordersLoading } = useAdminOrders();
  const { reviews, loading: reviewsLoading } = useAdminReviews();

  const loading = accountLoading || profilesLoading || ordersLoading || reviewsLoading;

  if (loading) return <PageLoader />;

  if (accountError || !account) {
    return <EmptyState icon={<Users size={26} />} title={t("users.errorTitle")} description={accountError ?? t("users.errorDescription")} />;
  }

  const profile = (profiles ?? []).find((p) => p.userId === account.id) ?? null;
  const customerOrders = profile ? (orders ?? []).filter((o) => o.customerId === profile.id) : [];
  const customerReviews = profile ? (reviews ?? []).filter((r) => r.customerId === profile.id) : [];

  const activeOrders = customerOrders.filter((o) => o.status !== OrderStatus.Rejected && o.status !== OrderStatus.Cancelled);
  const totalSpent = activeOrders.reduce((sum, o) => sum + o.totalAmount, 0);
  const now = new Date();
  const ordersThisMonth = customerOrders.filter((o) => {
    const d = new Date(o.createdAt);
    return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth();
  }).length;
  const averageRatingGiven = customerReviews.length
    ? customerReviews.reduce((sum, r) => sum + r.rating, 0) / customerReviews.length
    : null;

  return (
    <div className="flex flex-col gap-6">
      <button
        onClick={() => navigate("/admin/users")}
        className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-stone-500 transition hover:text-grove-700 dark:text-stone-400 dark:hover:text-grove-400"
      >
        <ArrowLeft size={15} />
        {t("customerDetail.back")}
      </button>

      {/* Профиль покупателя */}
      <Card className="flex flex-col gap-5">
        <div className="flex flex-col items-start gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-4">
            <Avatar name={account.fullName} size={64} ring />
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">{account.fullName}</h1>
                <span
                  className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                    account.isActive
                      ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
                      : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
                  }`}
                >
                  {t(account.isActive ? "users.status.active" : "users.status.inactive")}
                </span>
                {profile && (
                  <span className="rounded-full bg-blue-100 px-2.5 py-1 text-xs font-semibold text-blue-700 dark:bg-blue-900 dark:text-blue-300">
                    {t(`customerDetail.type.${CUSTOMER_TYPE_KEYS[profile.customerType] ?? "retail"}`)}
                  </span>
                )}
              </div>
              {profile && (
                <p className="mt-1 flex items-center gap-1 text-sm text-stone-500 dark:text-stone-400">
                  <MapPin size={13} className="shrink-0" />
                  {profile.region}, {profile.district}
                </p>
              )}
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3 border-t border-stone-100 pt-4 text-sm sm:grid-cols-2 lg:grid-cols-4 dark:border-stone-800">
          <div className="flex items-center gap-2 text-stone-500 dark:text-stone-400">
            <Calendar size={14} className="shrink-0" />
            {t("customerDetail.registeredAt", { date: formatDate(account.createdAt) })}
          </div>
          <div className="flex items-center gap-2 text-stone-500 dark:text-stone-400">
            <Mail size={14} className="shrink-0" />
            <span className="truncate">{account.email}</span>
          </div>
          <div className="flex items-center gap-2 text-stone-500 dark:text-stone-400">
            <Phone size={14} className="shrink-0" />
            {account.phoneNumber}
          </div>
          {profile?.defaultAddress && (
            <div className="flex items-center gap-2 text-stone-500 dark:text-stone-400">
              <MapPin size={14} className="shrink-0" />
              <span className="truncate">{profile.defaultAddress}</span>
            </div>
          )}
        </div>
      </Card>

      {/* Статистика: сколько заказов, сколько потратил */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard icon={ShoppingCart} accent="orange" label={t("customerDetail.stats.totalOrders")} value={String(customerOrders.length)} />
        <StatCard icon={ShoppingCart} accent="orange" label={t("customerDetail.stats.ordersThisMonth")} value={String(ordersThisMonth)} />
        <StatCard icon={Wallet} accent="grove" label={t("customerDetail.stats.totalSpent")} value={`${formatSomoni(totalSpent)} ${t("common.somoni")}`} />
        <StatCard icon={MessageSquare} accent="blue" label={t("customerDetail.stats.reviewsWritten")} value={String(customerReviews.length)} />
      </div>

      {/* Заказы */}
      <Card className="flex flex-col gap-4">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("customerDetail.ordersTitle")}</h2>
        {customerOrders.length === 0 ? (
          <p className="text-sm text-stone-400 dark:text-stone-500">{t("orders.emptyDescription")}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="py-3 pr-4 font-medium">{t("orders.columns.orderNumber")}</th>
                  <th className="py-3 pr-4 font-medium">{t("orders.columns.farmer")}</th>
                  <th className="py-3 pr-4 font-medium">{t("orders.columns.amount")}</th>
                  <th className="py-3 pr-4 font-medium">{t("orders.columns.status")}</th>
                </tr>
              </thead>
              <tbody>
                {[...customerOrders]
                  .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
                  .map((order) => (
                    <tr key={order.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                      <td className="py-3 pr-4">
                        <div className="flex flex-col">
                          <span className="font-medium text-stone-800 dark:text-stone-100">{order.orderNumber}</span>
                          <span className="text-xs text-stone-400 dark:text-stone-500">{formatDateTime(order.createdAt)}</span>
                        </div>
                      </td>
                      <td className="py-3 pr-4 text-stone-600 dark:text-stone-300">{t("orders.farmerLabel", { id: order.farmerId })}</td>
                      <td className="py-3 pr-4 font-semibold text-stone-800 dark:text-stone-100">
                        {formatSomoni(order.totalAmount)} {t("common.somoni")}
                      </td>
                      <td className="py-3 pr-4">
                        <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${ORDER_STATUS_CLASSES[order.status]}`}>
                          {t(`orders.status.${ORDER_STATUS_KEYS[order.status]}`)}
                        </span>
                      </td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Отзывы, оставленные покупателем */}
      <Card className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("customerDetail.reviewsTitle")}</h2>
          {averageRatingGiven != null && <RatingStars rating={averageRatingGiven} size={14} showValue />}
        </div>
        {customerReviews.length === 0 ? (
          <p className="text-sm text-stone-400 dark:text-stone-500">{t("reviews.emptyDescription")}</p>
        ) : (
          <div className="flex flex-col gap-2.5">
            {[...customerReviews]
              .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
              .map((review) => (
                <div key={review.id} className="flex flex-col gap-1.5 rounded-xl border border-stone-100 p-3 dark:border-stone-800">
                  <div className="flex items-center justify-between gap-3">
                    <span className="text-sm font-medium text-stone-700 dark:text-stone-200">{t("orders.farmerLabel", { id: review.farmerId })}</span>
                    <div className="flex items-center gap-2">
                      <RatingStars rating={review.rating} size={12} />
                      <span className="text-xs text-stone-400 dark:text-stone-500">{formatDate(review.createdAt)}</span>
                    </div>
                  </div>
                  {review.comment && <p className="text-sm text-stone-600 dark:text-stone-300">{review.comment}</p>}
                </div>
              ))}
          </div>
        )}
      </Card>
    </div>
  );
}
