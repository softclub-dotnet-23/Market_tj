import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Users } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { formatDate } from "@/lib/utils";
import { useAdminCustomers, type AdminUserDto } from "@/data/adminEntities";

const PAGE_SIZE = 10;

export function AdminUsers() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const { customers, loading, error } = useAdminCustomers();

  if (loading) return <PageLoader />;

  if (error || !customers) {
    return <EmptyState icon={<Users size={26} />} title={t("users.errorTitle")} description={error ?? t("users.errorDescription")} />;
  }

  if (customers.length === 0) {
    return <EmptyState icon={<Users size={26} />} title={t("users.emptyTitle")} description={t("users.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(customers.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminUserDto[] = customers.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
      <div className="overflow-x-auto">
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

      {totalPages > 1 && (
        <div className="border-t border-stone-100 p-4 dark:border-stone-800">
          <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
        </div>
      )}
    </div>
  );
}
