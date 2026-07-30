import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Check, ExternalLink, FileText, X } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Textarea } from "@/components/ui/Field";
import { resolveMediaUrl } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import {
  DocumentReviewStatus,
  FarmerDocumentType,
  updateFarmerDocumentStatus,
  useAdminFarmerDocuments,
  useAdminFarmers,
  type AdminFarmerDocumentDto,
} from "@/data/adminEntities";
import { useAuth } from "@/context/AuthContext";

const PAGE_SIZE = 9;

const STATUS_CLASSES: Record<number, string> = {
  [DocumentReviewStatus.Pending]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [DocumentReviewStatus.Approved]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [DocumentReviewStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

interface RejectFormValues {
  reason: string;
}

function RejectModal({
  document,
  onClose,
  onSubmit,
}: {
  document: AdminFarmerDocumentDto | null;
  onClose: () => void;
  onSubmit: (reason: string) => Promise<void>;
}) {
  const { t } = useTranslation("admin");
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<RejectFormValues>();

  return (
    <Modal
      open={!!document}
      onClose={() => {
        reset();
        onClose();
      }}
      className="max-w-md"
    >
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("farmerDocuments.rejectModalTitle")}</h2>
      <form
        onSubmit={handleSubmit(async (values) => {
          await onSubmit(values.reason);
          reset();
        })}
        className="mt-6 flex flex-col gap-5"
      >
        <Textarea
          label={t("farmerDocuments.form.reason")}
          error={errors.reason?.message}
          {...register("reason", { required: t("farmerDocuments.form.reasonRequired") })}
        />
        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("farmerDocuments.form.cancel")}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {t("farmerDocuments.rejectAction")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export function AdminFarmerDocuments() {
  const { t } = useTranslation("admin");
  const { user } = useAuth();
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [busyId, setBusyId] = useState<number | null>(null);
  const [rejecting, setRejecting] = useState<AdminFarmerDocumentDto | null>(null);
  const { documents, loading, error } = useAdminFarmerDocuments(refreshKey);
  const { farmers } = useAdminFarmers();
  const farmNameById = new Map((farmers ?? []).map((f) => [f.id, f.farmName]));

  if (loading) return <PageLoader />;

  if (error || !documents) {
    return (
      <EmptyState
        icon={<FileText size={26} />}
        title={t("farmerDocuments.errorTitle")}
        description={error ?? t("farmerDocuments.errorDescription")}
      />
    );
  }

  if (documents.length === 0) {
    return <EmptyState icon={<FileText size={26} />} title={t("farmerDocuments.emptyTitle")} description={t("farmerDocuments.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(documents.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminFarmerDocumentDto[] = documents.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const typeLabel = (type: number) =>
    t(`farmerDocuments.type.${type === FarmerDocumentType.Passport ? "passport" : type === FarmerDocumentType.LandDeed ? "landDeed" : "other"}`);
  const statusLabel = (status: number) =>
    t(`farmerDocuments.status.${status === DocumentReviewStatus.Approved ? "approved" : status === DocumentReviewStatus.Rejected ? "rejected" : "pending"}`);

  const handleApprove = async (document: AdminFarmerDocumentDto) => {
    setBusyId(document.id);
    try {
      await updateFarmerDocumentStatus(document, DocumentReviewStatus.Approved, user?.userId ?? 0, null);
      toast.success(t("farmerDocuments.approveSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("farmerDocuments.actionError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setBusyId(null);
    }
  };

  const handleReject = async (reason: string) => {
    if (!rejecting) return;
    setBusyId(rejecting.id);
    try {
      await updateFarmerDocumentStatus(rejecting, DocumentReviewStatus.Rejected, user?.userId ?? 0, reason);
      toast.success(t("farmerDocuments.rejectSuccess"));
      setRefreshKey((k) => k + 1);
      setRejecting(null);
    } catch (err) {
      toast.error(t("farmerDocuments.actionError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                <th className="px-6 py-4 font-medium">{t("farmerDocuments.columns.farmer")}</th>
                <th className="px-6 py-4 font-medium">{t("farmerDocuments.columns.type")}</th>
                <th className="px-6 py-4 font-medium">{t("farmerDocuments.columns.status")}</th>
                <th className="px-6 py-4 font-medium">{t("farmerDocuments.columns.rejectionReason")}</th>
                <th className="px-6 py-4 font-medium">{t("farmerDocuments.columns.uploadedAt")}</th>
                <th className="px-6 py-4 font-medium text-right">{t("farmerDocuments.columns.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {pageItems.map((doc) => (
                <tr key={doc.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                  <td className="max-w-48 truncate px-6 py-4 font-medium text-stone-800 dark:text-stone-100">
                    {farmNameById.get(doc.farmerProfileId) ?? t("farmerDocuments.farmerLabel", { id: doc.farmerProfileId })}
                  </td>
                  <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{typeLabel(doc.documentType)}</td>
                  <td className="px-6 py-4">
                    <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[doc.status] ?? STATUS_CLASSES[DocumentReviewStatus.Pending]}`}>
                      {statusLabel(doc.status)}
                    </span>
                  </td>
                  <td className="max-w-56 truncate px-6 py-4 text-stone-500 dark:text-stone-400">{doc.rejectionReason ?? "—"}</td>
                  <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(doc.uploadedAt)}</td>
                  <td className="px-6 py-4">
                    <div className="flex items-center justify-end gap-1.5">
                      <a
                        href={resolveMediaUrl(doc.fileUrl)}
                        target="_blank"
                        rel="noopener noreferrer"
                        aria-label={t("farmerDocuments.viewAction")}
                        className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-stone-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-stone-200"
                      >
                        <ExternalLink size={15} />
                      </a>
                      {doc.status !== DocumentReviewStatus.Approved && (
                        <button
                          onClick={() => handleApprove(doc)}
                          disabled={busyId === doc.id}
                          aria-label={t("farmerDocuments.approveAction")}
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-grove-50 hover:text-grove-700 disabled:opacity-50 dark:text-stone-500 dark:hover:bg-grove-950 dark:hover:text-grove-400"
                        >
                          <Check size={15} />
                        </button>
                      )}
                      {doc.status !== DocumentReviewStatus.Rejected && (
                        <button
                          onClick={() => setRejecting(doc)}
                          disabled={busyId === doc.id}
                          aria-label={t("farmerDocuments.rejectAction")}
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 disabled:opacity-50 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                        >
                          <X size={15} />
                        </button>
                      )}
                    </div>
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

      <RejectModal document={rejecting} onClose={() => setRejecting(null)} onSubmit={handleReject} />
    </div>
  );
}
