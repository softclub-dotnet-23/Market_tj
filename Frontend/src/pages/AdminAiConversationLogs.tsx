import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Bot } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { cn, formatDateTime } from "@/lib/utils";
import { useAdminAiConversationLogs, type AiConversationLogDto } from "@/data/adminEntities";

const PAGE_SIZE = 15;

// Раздел 1.4 плана (2026-08-08, по явному запросу пользователя) — только
// просмотр журнала диалогов AI-ассистента (пишет сам AiAssistantService),
// с фильтром WasError=true, чтобы админ мог быстро найти вопросы, на
// которые ассистент не смог ответить по существу.
export function AdminAiConversationLogs() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [errorOnly, setErrorOnly] = useState(false);
  const { logs, loading, error } = useAdminAiConversationLogs(errorOnly ? true : null);

  useEffect(() => setPage(1), [errorOnly]);

  if (loading) return <PageLoader />;

  if (error || !logs) {
    return <EmptyState icon={<Bot size={26} />} title={t("aiConversationLogs.errorTitle")} description={error ?? t("aiConversationLogs.errorDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(logs.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems = logs.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-center justify-between gap-3">
        <h1 className="font-display text-2xl text-stone-900 dark:text-stone-50">{t("aiConversationLogs.title")}</h1>
        <label className="flex items-center gap-2 text-sm text-stone-600 dark:text-stone-300">
          <input type="checkbox" checked={errorOnly} onChange={(e) => setErrorOnly(e.target.checked)} className="size-4 rounded border-stone-300 accent-grove-600" />
          {t("aiConversationLogs.errorsOnly")}
        </label>
      </div>

      {logs.length === 0 ? (
        <EmptyState icon={<Bot size={26} />} title={t("aiConversationLogs.emptyTitle")} description={t("aiConversationLogs.emptyDescription")} />
      ) : (
        <div className="flex flex-col divide-y divide-stone-100 overflow-hidden rounded-3xl border border-stone-100 bg-white dark:divide-stone-800 dark:border-stone-800 dark:bg-stone-900">
          {pageItems.map((log) => (
            <LogRow key={log.id} log={log} />
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

function LogRow({ log }: { log: AiConversationLogDto }) {
  const { t } = useTranslation("admin");

  return (
    <div className="flex flex-col gap-2 px-5 py-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <span
            className={cn(
              "rounded-full px-2.5 py-0.5 text-xs font-semibold",
              log.wasError
                ? "bg-danger/10 text-danger"
                : "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
            )}
          >
            {log.wasError ? t("aiConversationLogs.error") : log.intent}
          </span>
          <span className="text-xs text-stone-400 dark:text-stone-500">{log.role}</span>
          {log.userFullName && <span className="text-xs text-stone-500 dark:text-stone-400">{log.userFullName}</span>}
        </div>
        <span className="shrink-0 text-xs text-stone-400 dark:text-stone-500">{formatDateTime(log.createdAt)}</span>
      </div>
      <p className="text-sm font-medium text-stone-800 dark:text-stone-100">{log.question}</p>
      <p className="text-sm text-stone-500 dark:text-stone-400">{log.response}</p>
    </div>
  );
}
