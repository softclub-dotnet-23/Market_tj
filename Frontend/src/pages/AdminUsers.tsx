import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Mail, Phone, Search, Users } from "lucide-react";
import { useAdminSearch } from "@/components/layout/AdminLayout";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Avatar } from "@/components/ui/Avatar";
import { ViewModeToggle, type OrdersViewMode } from "@/components/ui/ViewModeToggle";
import { formatDate } from "@/lib/utils";
import { useAdminCustomers, type AdminUserDto } from "@/data/adminEntities";

// Кратно 3 — карточный вид на десктопе всегда 3 колонки (см. xl:grid-cols-3
// ниже), так последняя строка страницы не остаётся неполной/одинокой.
const PAGE_SIZE = 9;

export function AdminUsers() {
  const { t } = useTranslation("admin");
  const searchQuery = useAdminSearch();
  const [page, setPage] = useState(1);
  const [viewMode, setViewMode] = useState<OrdersViewMode>("table");
  const { customers, loading, error } = useAdminCustomers();

  useEffect(() => {
    setPage(1);
  }, [searchQuery]);

  if (loading) return <PageLoader />;

  if (error || !customers) {
    return <EmptyState icon={<Users size={26} />} title={t("users.errorTitle")} description={error ?? t("users.errorDescription")} />;
  }

  if (customers.length === 0) {
    return <EmptyState icon={<Users size={26} />} title={t("users.emptyTitle")} description={t("users.emptyDescription")} />;
  }

  const query = searchQuery.trim().toLowerCase();
  const filteredCustomers = query
    ? customers.filter(
        (c) =>
          c.fullName.toLowerCase().includes(query) ||
          c.email.toLowerCase().includes(query) ||
          c.phoneNumber.toLowerCase().includes(query),
      )
    : customers;

  if (query && filteredCustomers.length === 0) {
    return <EmptyState icon={<Search size={26} />} title={t("common.searchEmptyTitle")} description={t("common.searchEmptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(filteredCustomers.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminUserDto[] = filteredCustomers.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const renderCard = (customer: AdminUserDto) => (
    <div
      key={customer.id}
      className="flex flex-col gap-4 rounded-2xl border border-stone-100 bg-white p-5 shadow-sm transition hover:shadow-md dark:border-stone-800 dark:bg-stone-900"
    >
      <div className="flex items-start gap-3">
        <Avatar name={customer.fullName} size={44} />
        <div className="min-w-0 flex-1">
          <p className="truncate font-medium text-stone-800 dark:text-stone-100">{customer.fullName}</p>
          <p className="mt-0.5 flex items-center gap-1 truncate text-xs text-stone-400 dark:text-stone-500">
            <Mail size={12} className="shrink-0" />
            {customer.email}
          </p>
        </div>
        <span
          className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${
            customer.isActive
              ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
              : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
          }`}
        >
          {t(customer.isActive ? "users.status.active" : "users.status.inactive")}
        </span>
      </div>
      <div className="flex items-center justify-between border-t border-stone-50 pt-3 text-sm dark:border-stone-800/60">
        <span className="flex items-center gap-1.5 text-stone-500 dark:text-stone-400">
          <Phone size={13} className="shrink-0" />
          {customer.phoneNumber}
        </span>
        <span className="text-xs text-stone-400 dark:text-stone-500">{formatDate(customer.createdAt)}</span>
      </div>
    </div>
  );

  return (
    <div className="overflow-hidden rounded-3xl border border-stone-100 bg-linear-to-b from-white to-stone-50/60 shadow-(--shadow-soft) dark:border-stone-800 dark:from-stone-900 dark:to-stone-900">
      <div className="hidden items-center justify-end border-b border-stone-100 p-4 lg:flex dark:border-stone-800">
        <ViewModeToggle value={viewMode} onChange={setViewMode} ns="admin" />
      </div>

      <div className={viewMode === "table" ? "hidden overflow-x-auto lg:block" : "hidden"}>
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("users.columns.fullName")}</th>
              <th className="px-6 py-4 font-medium">{t("users.columns.email")}</th>
              <th className="px-6 py-4 font-medium">{t("users.columns.phone")}</th>
              <th className="px-6 py-4 font-medium">{t("users.columns.status")}</th>
              <th className="px-6 py-4 font-medium">{t("users.columns.createdAt")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((customer) => (
              <tr key={customer.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{customer.fullName}</td>
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{customer.email}</td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{customer.phoneNumber}</td>
                <td className="px-6 py-4">
                  <span
                    className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                      customer.isActive
                        ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
                        : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
                    }`}
                  >
                    {t(customer.isActive ? "users.status.active" : "users.status.inactive")}
                  </span>
                </td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(customer.createdAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {viewMode === "cards" && (
        <div className="hidden grid-cols-2 gap-4 p-5 lg:grid xl:grid-cols-3">{pageItems.map(renderCard)}</div>
      )}

      <div className="grid grid-cols-1 gap-4 p-5 lg:hidden">{pageItems.map(renderCard)}</div>

      {totalPages > 1 && (
        <div className="border-t border-stone-100 p-4 dark:border-stone-800">
          <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
        </div>
      )}
    </div>
  );
}
