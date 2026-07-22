import { useState } from "react";
import { useTranslation } from "react-i18next";
import { CreditCard } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { formatDate } from "@/lib/utils";
import { useAdminCommissions, type AdminCommissionDto } from "@/data/adminEntities";

const PAGE_SIZE = 10;

function isCurrentlyActive(commission: AdminCommissionDto) {
  return !commission.effectiveTo || new Date(commission.effectiveTo).getTime() > Date.now();
}

export function AdminCommissions() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const { commissions, loading, error } = useAdminCommissions();

  if (loading) return <PageLoader />;

  if (error || !commissions) {
    return <EmptyState icon={<CreditCard size={26} />} title={t("commissions.errorTitle")} description={error ?? t("commissions.errorDescription")} />;
  }

  if (commissions.length === 0) {
    return <EmptyState icon={<CreditCard size={26} />} title={t("commissions.emptyTitle")} description={t("commissions.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(commissions.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminCommissionDto[] = commissions.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("commissions.columns.category")}</th>
              <th className="px-6 py-4 font-medium">{t("commissions.columns.percentage")}</th>
              <th className="px-6 py-4 font-medium">{t("commissions.columns.effectiveFrom")}</th>
              <th className="px-6 py-4 font-medium">{t("commissions.columns.effectiveTo")}</th>
              <th className="px-6 py-4 font-medium">{t("commissions.columns.status")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((commission) => (
              <tr key={commission.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">
                  {commission.categoryId === null ? t("commissions.allCategories") : t("commissions.categoryLabel", { id: commission.categoryId })}
                </td>
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{commission.percentage}%</td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(commission.effectiveFrom)}</td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                  {commission.effectiveTo ? formatDate(commission.effectiveTo) : "—"}
                </td>
                <td className="px-6 py-4">
                  <span
                    className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                      isCurrentlyActive(commission)
                        ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
                        : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
                    }`}
                  >
                    {t(isCurrentlyActive(commission) ? "commissions.status.active" : "commissions.status.expired")}
                  </span>
                </td>
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
