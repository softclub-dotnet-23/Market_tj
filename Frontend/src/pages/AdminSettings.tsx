import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Settings } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { formatDate } from "@/lib/utils";
import { useAdminSettings, type AdminSettingDto } from "@/data/adminEntities";

const PAGE_SIZE = 10;

export function AdminSettings() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const { settings, loading, error } = useAdminSettings();

  if (loading) return <PageLoader />;

  if (error || !settings) {
    return <EmptyState icon={<Settings size={26} />} title={t("settings.errorTitle")} description={error ?? t("settings.errorDescription")} />;
  }

  if (settings.length === 0) {
    return <EmptyState icon={<Settings size={26} />} title={t("settings.emptyTitle")} description={t("settings.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(settings.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminSettingDto[] = settings.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("settings.columns.key")}</th>
              <th className="px-6 py-4 font-medium">{t("settings.columns.value")}</th>
              <th className="px-6 py-4 font-medium">{t("settings.columns.description")}</th>
              <th className="px-6 py-4 font-medium">{t("settings.columns.updatedAt")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((setting) => (
              <tr key={setting.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                <td className="px-6 py-4 font-mono text-xs font-medium text-stone-800 dark:text-stone-100">{setting.key}</td>
                <td className="max-w-64 truncate px-6 py-4 text-stone-600 dark:text-stone-300">{setting.value}</td>
                <td className="max-w-80 truncate px-6 py-4 text-stone-500 dark:text-stone-400">{setting.description ?? "—"}</td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(setting.updatedAt)}</td>
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
