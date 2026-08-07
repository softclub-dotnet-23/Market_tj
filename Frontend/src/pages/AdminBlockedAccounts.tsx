import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { ShieldOff } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Button } from "@/components/ui/Button";
import { ApiError } from "@/lib/api";
import { cn, formatDateTime } from "@/lib/utils";
import { unblockAccount, useAdminAccountBlocks, type AccountBlockDto } from "@/data/adminEntities";

const PAGE_SIZE = 15;

// Единая страница "Заблокированные аккаунты" (Блок 2, 2026-08-08, по явному
// запросу пользователя) — показывает баны за частые отмены заказов
// (BlockType="Cancellations", курьеры/фермеры) и, начиная с Блока 3, также
// технические rate-limit баны (BlockType="RateLimit") — один и тот же
// список с одной и той же кнопкой ручной разблокировки для обоих случаев.
export function AdminBlockedAccounts() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [activeOnly, setActiveOnly] = useState(true);
  const [refreshKey, setRefreshKey] = useState(0);
  const [unblockingId, setUnblockingId] = useState<number | null>(null);
  const { blocks, loading, error } = useAdminAccountBlocks(activeOnly ? true : null, refreshKey);

  useEffect(() => setPage(1), [activeOnly]);

  const handleUnblock = async (block: AccountBlockDto) => {
    setUnblockingId(block.id);
    try {
      await unblockAccount(block.id);
      toast.success(t("blockedAccounts.unblockSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("blockedAccounts.unblockError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setUnblockingId(null);
    }
  };

  if (loading) return <PageLoader />;

  if (error || !blocks) {
    return <EmptyState icon={<ShieldOff size={26} />} title={t("blockedAccounts.errorTitle")} description={error ?? t("blockedAccounts.errorDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(blocks.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems = blocks.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-center justify-between gap-3">
        <h1 className="font-display text-2xl text-stone-900 dark:text-stone-50">{t("blockedAccounts.title")}</h1>
        <label className="flex items-center gap-2 text-sm text-stone-600 dark:text-stone-300">
          <input type="checkbox" checked={activeOnly} onChange={(e) => setActiveOnly(e.target.checked)} className="size-4 rounded border-stone-300 accent-grove-600" />
          {t("blockedAccounts.activeOnly")}
        </label>
      </div>

      {blocks.length === 0 ? (
        <EmptyState icon={<ShieldOff size={26} />} title={t("blockedAccounts.emptyTitle")} description={t("blockedAccounts.emptyDescription")} />
      ) : (
        <div className="flex flex-col divide-y divide-stone-100 overflow-hidden rounded-3xl border border-stone-100 bg-white dark:divide-stone-800 dark:border-stone-800 dark:bg-stone-900">
          {pageItems.map((block) => (
            <BlockRow key={block.id} block={block} busy={unblockingId === block.id} onUnblock={() => handleUnblock(block)} />
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <div className="rounded-3xl border border-stone-100 p-4 dark:border-stone-800">
          <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
        </div>
      )}
    </div>
  );
}

const BLOCK_TYPE_LABEL_KEYS: Record<string, string> = {
  Cancellations: "blockedAccounts.blockType.cancellations",
  RateLimit: "blockedAccounts.blockType.rateLimit",
};

function BlockRow({ block, busy, onUnblock }: { block: AccountBlockDto; busy: boolean; onUnblock: () => void }) {
  const { t } = useTranslation("admin");

  return (
    <div className="flex flex-col gap-2 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex flex-col gap-1.5">
        <div className="flex flex-wrap items-center gap-2">
          <span
            className={cn(
              "rounded-full px-2.5 py-0.5 text-xs font-semibold",
              block.isActive
                ? "bg-danger/10 text-danger"
                : "bg-stone-100 text-stone-500 dark:bg-stone-800 dark:text-stone-400",
            )}
          >
            {block.isActive ? t("blockedAccounts.statusActive") : t("blockedAccounts.statusInactive")}
          </span>
          <span className="text-xs text-stone-400 dark:text-stone-500">
            {t(BLOCK_TYPE_LABEL_KEYS[block.blockType] ?? "blockedAccounts.blockType.other")}
          </span>
          <span className="text-xs text-stone-400 dark:text-stone-500">{block.role}</span>
          <span className="text-sm font-medium text-stone-800 dark:text-stone-100">
            {block.userFullName ?? t("blockedAccounts.userLabel", { id: block.userId })}
          </span>
        </div>
        <p className="text-sm text-stone-600 dark:text-stone-300">{block.reason}</p>
        <p className="text-xs text-stone-400 dark:text-stone-500">
          {t("blockedAccounts.blockedUntil")}: {formatDateTime(block.blockedUntil)}
          {block.unblockedAt && ` · ${t("blockedAccounts.unblockedAt")}: ${formatDateTime(block.unblockedAt)}`}
        </p>
      </div>
      {block.isActive && (
        <Button type="button" size="sm" variant="outline" loading={busy} onClick={onUnblock}>
          {t("blockedAccounts.unblockAction")}
        </Button>
      )}
    </div>
  );
}
