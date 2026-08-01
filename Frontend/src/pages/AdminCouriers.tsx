import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Bike } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { formatDate } from "@/lib/utils";
import { updateCourierStatus, useAdminCouriers, type AdminCourierDto } from "@/data/adminEntities";

const PAGE_SIZE = 10;

function TogglePill({ active, onClick, activeLabel, inactiveLabel, busy }: { active: boolean; onClick: () => void; activeLabel: string; inactiveLabel: string; busy: boolean }) {
  return (
    <button
      onClick={onClick}
      disabled={busy}
      className={`rounded-full px-2.5 py-1 text-xs font-semibold transition disabled:opacity-50 ${
        active
          ? "bg-grove-100 text-grove-700 hover:bg-grove-200 dark:bg-grove-900 dark:text-grove-300 dark:hover:bg-grove-800"
          : "bg-stone-100 text-stone-500 hover:bg-stone-200 dark:bg-stone-800 dark:text-stone-400 dark:hover:bg-stone-700"
      }`}
    >
      {active ? activeLabel : inactiveLabel}
    </button>
  );
}

export function AdminCouriers() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [busyId, setBusyId] = useState<number | null>(null);
  const { couriers, loading, error } = useAdminCouriers(refreshKey);

  if (loading) return <PageLoader />;

  if (error || !couriers) {
    return <EmptyState icon={<Bike size={26} />} title={t("couriers.errorTitle")} description={error ?? t("couriers.errorDescription")} />;
  }

  if (couriers.length === 0) {
    return <EmptyState icon={<Bike size={26} />} title={t("couriers.emptyTitle")} description={t("couriers.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(couriers.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminCourierDto[] = couriers.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const handleToggle = async (courier: AdminCourierDto, field: "isActive" | "isAvailable") => {
    setBusyId(courier.id);
    try {
      await updateCourierStatus(courier, { [field]: !courier[field] });
      toast.success(t("couriers.updateSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("couriers.updateError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("couriers.columns.transport")}</th>
              <th className="px-6 py-4 font-medium">{t("couriers.columns.region")}</th>
              <th className="px-6 py-4 font-medium">{t("couriers.columns.available")}</th>
              <th className="px-6 py-4 font-medium">{t("couriers.columns.active")}</th>
              <th className="px-6 py-4 font-medium">{t("couriers.columns.createdAt")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((courier) => (
              <tr key={courier.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">
                  {courier.transportType}
                  <span className="ml-2 font-mono text-xs font-normal text-stone-400 dark:text-stone-500">{courier.vehicleNumber}</span>
                </td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                  {courier.region}, {courier.district}
                </td>
                <td className="px-6 py-4">
                  <TogglePill
                    active={courier.isAvailable}
                    busy={busyId === courier.id}
                    onClick={() => handleToggle(courier, "isAvailable")}
                    activeLabel={t("couriers.available")}
                    inactiveLabel={t("couriers.unavailable")}
                  />
                </td>
                <td className="px-6 py-4">
                  <TogglePill
                    active={courier.isActive}
                    busy={busyId === courier.id}
                    onClick={() => handleToggle(courier, "isActive")}
                    activeLabel={t("couriers.active")}
                    inactiveLabel={t("couriers.inactive")}
                  />
                </td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(courier.createdAt)}</td>
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
