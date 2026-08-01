import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Check, MapPin, Search, Sprout, X } from "lucide-react";
import { useAdminSearch } from "@/components/layout/AdminLayout";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Avatar } from "@/components/ui/Avatar";
import { ViewModeToggle, type OrdersViewMode } from "@/components/ui/ViewModeToggle";
import { useAuth } from "@/context/AuthContext";
import { formatDate } from "@/lib/utils";
import { FarmerVerificationStatus, updateFarmerVerification, useAdminFarmers, type AdminFarmerDto } from "@/data/adminEntities";

// Кратно 3 — карточный вид на десктопе всегда 3 колонки (см. xl:grid-cols-3
// ниже), так последняя строка страницы не остаётся неполной/одинокой.
const PAGE_SIZE = 9;

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
  const { user } = useAuth();
  const navigate = useNavigate();
  const searchQuery = useAdminSearch();
  const [page, setPage] = useState(1);
  const [viewMode, setViewMode] = useState<OrdersViewMode>("table");
  const [refreshKey, setRefreshKey] = useState(0);
  const [busyId, setBusyId] = useState<number | null>(null);
  const { farmers, loading, error } = useAdminFarmers(refreshKey);

  useEffect(() => {
    setPage(1);
  }, [searchQuery]);

  if (loading) return <PageLoader />;

  if (error || !farmers) {
    return <EmptyState icon={<Sprout size={26} />} title={t("farmers.errorTitle")} description={error ?? t("farmers.errorDescription")} />;
  }

  if (farmers.length === 0) {
    return <EmptyState icon={<Sprout size={26} />} title={t("farmers.emptyTitle")} description={t("farmers.emptyDescription")} />;
  }

  const query = searchQuery.trim().toLowerCase();
  const filteredFarmers = query
    ? farmers.filter(
        (f) =>
          f.farmName.toLowerCase().includes(query) ||
          `${f.region}, ${f.district}`.toLowerCase().includes(query) ||
          f.village.toLowerCase().includes(query),
      )
    : farmers;

  if (query && filteredFarmers.length === 0) {
    return <EmptyState icon={<Search size={26} />} title={t("common.searchEmptyTitle")} description={t("common.searchEmptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(filteredFarmers.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminFarmerDto[] = filteredFarmers.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const handleSetStatus = async (farmer: AdminFarmerDto, status: number) => {
    setBusyId(farmer.id);
    try {
      await updateFarmerVerification(farmer, status, user?.userId ?? 0);
      toast.success(status === FarmerVerificationStatus.Verified ? t("farmers.verifySuccess") : t("farmers.rejectSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("farmers.actionError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setBusyId(null);
    }
  };

  // stopPropagation — карточка/строка теперь целиком кликабельна (переход на
  // карточку фермера), кнопки подтверждения/отклонения внутри должны
  // отрабатывать сами, не запуская заодно и переход.
  const actionButtons = (farmer: AdminFarmerDto) => (
    <>
      {farmer.verificationStatus !== FarmerVerificationStatus.Verified && (
        <button
          onClick={(e) => {
            e.stopPropagation();
            handleSetStatus(farmer, FarmerVerificationStatus.Verified);
          }}
          disabled={busyId === farmer.id}
          aria-label={t("farmers.verifyAction")}
          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-grove-50 hover:text-grove-700 disabled:opacity-50 dark:text-stone-500 dark:hover:bg-grove-950 dark:hover:text-grove-400"
        >
          <Check size={15} />
        </button>
      )}
      {farmer.verificationStatus !== FarmerVerificationStatus.Rejected && (
        <button
          onClick={(e) => {
            e.stopPropagation();
            handleSetStatus(farmer, FarmerVerificationStatus.Rejected);
          }}
          disabled={busyId === farmer.id}
          aria-label={t("farmers.rejectAction")}
          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 disabled:opacity-50 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
        >
          <X size={15} />
        </button>
      )}
    </>
  );

  const renderCard = (farmer: AdminFarmerDto) => (
    <div
      key={farmer.id}
      onClick={() => navigate(`/admin/farmers/${farmer.id}`)}
      role="button"
      tabIndex={0}
      className="flex cursor-pointer flex-col gap-4 rounded-2xl border border-stone-100 bg-white p-5 shadow-sm transition hover:shadow-md dark:border-stone-800 dark:bg-stone-900"
    >
      <div className="flex items-start gap-3">
        <Avatar name={farmer.farmName} size={44} />
        <div className="min-w-0 flex-1">
          <p className="truncate font-medium text-stone-800 dark:text-stone-100">{farmer.farmName}</p>
          <p className="mt-0.5 flex items-center gap-1 truncate text-xs text-stone-400 dark:text-stone-500">
            <MapPin size={12} className="shrink-0" />
            {farmer.region}, {farmer.district}
          </p>
        </div>
        <span
          className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[farmer.verificationStatus] ?? STATUS_CLASSES[FarmerVerificationStatus.Pending]}`}
        >
          {t(`farmers.status.${STATUS_KEYS[farmer.verificationStatus] ?? "pending"}`)}
        </span>
      </div>
      <div className="flex items-center justify-between border-t border-stone-50 pt-3 dark:border-stone-800/60">
        <span className="text-xs text-stone-400 dark:text-stone-500">{formatDate(farmer.createdAt)}</span>
        <div className="flex items-center gap-1.5">{actionButtons(farmer)}</div>
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
              <th className="px-6 py-4 font-medium">{t("farmers.columns.farmName")}</th>
              <th className="px-6 py-4 font-medium">{t("farmers.columns.region")}</th>
              <th className="px-6 py-4 font-medium">{t("farmers.columns.status")}</th>
              <th className="px-6 py-4 font-medium">{t("farmers.columns.createdAt")}</th>
              <th className="px-6 py-4 font-medium text-right">{t("farmers.columns.actions")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((farmer) => (
              <tr
                key={farmer.id}
                onClick={() => navigate(`/admin/farmers/${farmer.id}`)}
                className="cursor-pointer border-b border-stone-50 transition-colors last:border-0 hover:bg-stone-50/70 dark:border-stone-800/60 dark:hover:bg-stone-800/40"
              >
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
                <td className="px-6 py-4">
                  <div className="flex items-center justify-end gap-1.5">{actionButtons(farmer)}</div>
                </td>
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
