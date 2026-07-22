import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Sprout } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { formatDate } from "@/lib/utils";
import { FarmerVerificationStatus, useAdminFarmers, type AdminFarmerDto } from "@/data/adminEntities";

const PAGE_SIZE = 10;

const STATUS_CLASSES: Record<number, string> = {
  [FarmerVerificationStatus.Pending]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [FarmerVerificationStatus.Verified]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [FarmerVerificationStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

const STATUS_KEYS: Record<number, string> = {
  [FarmerVerificationStatus.Pending]: "pending",
  [FarmerVerificationStatus.Verified]: "verified",
  [FarmerVerificationStatus.Rejected]: "rejected",
};

export function AdminFarmers() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const { farmers, loading, error } = useAdminFarmers();

  if (loading) return <PageLoader />;

  if (error || !farmers) {
    return <EmptyState icon={<Sprout size={26} />} title={t("farmers.errorTitle")} description={error ?? t("farmers.errorDescription")} />;
  }

  if (farmers.length === 0) {
    return <EmptyState icon={<Sprout size={26} />} title={t("farmers.emptyTitle")} description={t("farmers.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(farmers.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminFarmerDto[] = farmers.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("farmers.columns.farmName")}</th>
              <th className="px-6 py-4 font-medium">{t("farmers.columns.region")}</th>
              <th className="px-6 py-4 font-medium">{t("farmers.columns.status")}</th>
              <th className="px-6 py-4 font-medium">{t("farmers.columns.createdAt")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((farmer) => (
              <tr key={farmer.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                <td className="max-w-64 truncate px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{farmer.farmName}</td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                  {farmer.region}, {farmer.district}
                </td>
                <td className="px-6 py-4">
                  <span
                    className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[farmer.verificationStatus] ?? STATUS_CLASSES[FarmerVerificationStatus.Pending]}`}
                  >
                    {t(`farmers.status.${STATUS_KEYS[farmer.verificationStatus] ?? "pending"}`)}
                  </span>
                </td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(farmer.createdAt)}</td>
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
