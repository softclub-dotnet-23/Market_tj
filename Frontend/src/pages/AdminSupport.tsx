import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { LifeBuoy, Send } from "lucide-react";
import { useAdminSearch } from "@/components/layout/AdminLayout";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Avatar } from "@/components/ui/Avatar";
import { cn, formatDateTime } from "@/lib/utils";
import {
  SupportTicketStatus,
  replyToSupportTicket,
  useAdminSupportMessages,
  useAdminSupportTickets,
  useAllUsers,
  type SupportTicketDto,
} from "@/data/adminSupport";

const PAGE_SIZE = 9;

const STATUS_CLASSES: Record<number, string> = {
  [SupportTicketStatus.Open]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [SupportTicketStatus.InProgress]: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  [SupportTicketStatus.Closed]: "bg-stone-100 text-stone-500 dark:bg-stone-800 dark:text-stone-400",
};

function TicketThreadModal({
  ticket,
  authorName,
  onClose,
  onReplied,
}: {
  ticket: SupportTicketDto | null;
  authorName: string;
  onClose: () => void;
  onReplied: () => void;
}) {
  const { t } = useTranslation("admin");
  const [refreshKey, setRefreshKey] = useState(0);
  const [reply, setReply] = useState("");
  const [sending, setSending] = useState(false);
  const { messages, loading } = useAdminSupportMessages(ticket?.id ?? null, refreshKey);

  useEffect(() => {
    if (ticket) setReply("");
  }, [ticket]);

  const handleSend = async () => {
    if (!ticket || !reply.trim()) return;
    setSending(true);
    try {
      await replyToSupportTicket(ticket.id, reply.trim());
      setReply("");
      setRefreshKey((k) => k + 1);
      onReplied();
    } catch (err) {
      toast.error(t("support.replyError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setSending(false);
    }
  };

  return (
    <Modal open={!!ticket} onClose={onClose} className="max-w-xl">
      {ticket && (
        <div className="flex flex-col gap-4">
          <div>
            <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{ticket.subject}</h2>
            <p className="mt-1 text-sm text-stone-400 dark:text-stone-500">
              {authorName}
              {ticket.guestEmail && ` · ${ticket.guestEmail}`}
            </p>
          </div>

          <div className="flex max-h-96 min-h-40 flex-col gap-2 overflow-y-auto rounded-2xl bg-stone-50 p-4 dark:bg-stone-800/40">
            {loading ? (
              <PageLoader />
            ) : !messages || messages.length === 0 ? (
              <p className="m-auto text-sm text-stone-400 dark:text-stone-500">{t("support.noMessagesYet")}</p>
            ) : (
              messages.map((m) => {
                const fromStaff = m.senderId !== null && m.senderId !== ticket.userId;
                return (
                  <div key={m.id} className={cn("flex", fromStaff ? "justify-end" : "justify-start")}>
                    <div
                      className={cn(
                        "max-w-[80%] rounded-2xl px-4 py-2.5 text-sm shadow-sm",
                        fromStaff ? "rounded-br-md bg-grove-700 text-white" : "rounded-bl-md bg-white text-stone-800 dark:bg-stone-900 dark:text-stone-100",
                      )}
                    >
                      <p className="whitespace-pre-wrap">{m.message}</p>
                      <p className={cn("mt-1 text-[11px]", fromStaff ? "text-grove-100" : "text-stone-400 dark:text-stone-500")}>
                        {formatDateTime(m.createdAt)}
                      </p>
                    </div>
                  </div>
                );
              })
            )}
          </div>

          <div className="flex items-end gap-2">
            <textarea
              value={reply}
              onChange={(e) => setReply(e.target.value)}
              placeholder={t("support.replyPlaceholder")}
              rows={2}
              className="h-auto min-h-11 w-full flex-1 resize-none rounded-xl border border-stone-200 bg-white px-4 py-2.5 text-[15px] text-stone-900 transition focus:border-grove-500 focus:ring-2 focus:ring-grove-100 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-100 dark:focus:ring-grove-900"
            />
            <Button onClick={handleSend} loading={sending} disabled={!reply.trim()} className="h-11 w-11 shrink-0 p-0" aria-label={t("support.sendReply")}>
              <Send size={16} />
            </Button>
          </div>
        </div>
      )}
    </Modal>
  );
}

export function AdminSupport() {
  const { t } = useTranslation("admin");
  const searchQuery = useAdminSearch();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<number | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const [openTicket, setOpenTicket] = useState<SupportTicketDto | null>(null);
  const { tickets, loading, error } = useAdminSupportTickets(refreshKey);
  const { users } = useAllUsers();

  useEffect(() => setPage(1), [searchQuery, statusFilter]);

  if (loading) return <PageLoader />;

  if (error || !tickets) {
    return <EmptyState icon={<LifeBuoy size={26} />} title={t("support.errorTitle")} description={error ?? t("support.errorDescription")} />;
  }

  const nameById = new Map((users ?? []).map((u) => [u.id, u.fullName]));
  const authorName = (ticket: SupportTicketDto) => ticket.guestName ?? (ticket.userId ? (nameById.get(ticket.userId) ?? t("support.unknownUser")) : t("support.unknownUser"));

  const statusLabel = (status: number) =>
    t(`support.status.${status === SupportTicketStatus.Open ? "open" : status === SupportTicketStatus.InProgress ? "inProgress" : "closed"}`);

  const query = searchQuery.trim().toLowerCase();
  let filtered = tickets;
  if (statusFilter !== null) filtered = filtered.filter((tk) => tk.status === statusFilter);
  if (query) filtered = filtered.filter((tk) => tk.subject.toLowerCase().includes(query) || authorName(tk).toLowerCase().includes(query));

  filtered = [...filtered].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

  if (tickets.length === 0) {
    return <EmptyState icon={<LifeBuoy size={26} />} title={t("support.emptyTitle")} description={t("support.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const STATUS_TABS = [
    { value: null, label: t("support.allStatuses") },
    { value: SupportTicketStatus.Open, label: t("support.status.open") },
    { value: SupportTicketStatus.InProgress, label: t("support.status.inProgress") },
    { value: SupportTicketStatus.Closed, label: t("support.status.closed") },
  ];

  return (
    <div className="flex flex-col gap-5">
      <div className="flex gap-2 rounded-xl bg-stone-100 p-1 dark:bg-stone-800">
        {STATUS_TABS.map((tab) => (
          <button
            key={String(tab.value)}
            onClick={() => setStatusFilter(tab.value)}
            className={cn(
              "rounded-lg px-4 py-2 text-sm font-medium transition",
              statusFilter === tab.value ? "bg-white text-stone-900 shadow-sm dark:bg-stone-900 dark:text-stone-50" : "text-stone-500 dark:text-stone-400",
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {filtered.length === 0 ? (
        <EmptyState icon={<LifeBuoy size={26} />} title={t("common.searchEmptyTitle")} description={t("common.searchEmptyDescription")} />
      ) : (
        <div className="flex flex-col divide-y divide-stone-100 overflow-hidden rounded-3xl border border-stone-100 bg-white dark:divide-stone-800 dark:border-stone-800 dark:bg-stone-900">
          {pageItems.map((ticket) => (
            <button
              key={ticket.id}
              onClick={() => setOpenTicket(ticket)}
              className="flex items-center gap-4 px-5 py-4 text-left transition hover:bg-stone-50 dark:hover:bg-stone-800/60"
            >
              <Avatar name={authorName(ticket)} size={40} />
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{authorName(ticket)}</p>
                  <span className="shrink-0 text-xs text-stone-400 dark:text-stone-500">{formatDateTime(ticket.createdAt)}</span>
                </div>
                <p className="truncate text-sm text-stone-600 dark:text-stone-300">{ticket.subject}</p>
                {!ticket.userId && <p className="truncate text-xs text-stone-400 dark:text-stone-500">{t("support.guestBadge")}</p>}
              </div>
              <span className={cn("shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold", STATUS_CLASSES[ticket.status] ?? STATUS_CLASSES[SupportTicketStatus.Open])}>
                {statusLabel(ticket.status)}
              </span>
            </button>
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <div className="rounded-3xl border border-stone-100 p-4 dark:border-stone-800">
          <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
        </div>
      )}

      <TicketThreadModal
        ticket={openTicket}
        authorName={openTicket ? authorName(openTicket) : ""}
        onClose={() => setOpenTicket(null)}
        onReplied={() => setRefreshKey((k) => k + 1)}
      />
    </div>
  );
}
