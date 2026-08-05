import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Check, FileText, X } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Modal } from "@/components/ui/Modal";
import { Select, Textarea } from "@/components/ui/Field";
import { Button } from "@/components/ui/Button";
import { ApiError, resolveMediaUrl } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import { DocumentReviewStatus } from "@/data/farmer";
import { CourierDocumentType } from "@/data/courier";
import { reviewCourierDocument, useAdminCourierDocuments, type AdminCourierDocumentDto } from "@/data/adminEntities";

const PAGE_SIZE = 20;

const STATUS_CLASSES: Record<number, string> = {
  [DocumentReviewStatus.Pending]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [DocumentReviewStatus.Approved]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [DocumentReviewStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

interface RejectFormValues {
  rejectionReason: string;
}

function RejectModal({
  open,
  onClose,
  document,
  onReviewed,
}: {
  open: boolean;
  onClose: () => void;
  document: AdminCourierDocumentDto | null;
  onReviewed: () => void;
}) {
  const { t } = useTranslation("admin");
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<RejectFormValues>({ defaultValues: { rejectionReason: "" } });

  const onSubmit = async (values: RejectFormValues) => {
    if (!document) return;
    try {
      await reviewCourierDocument(document.id, DocumentReviewStatus.Rejected, values.rejectionReason);
      toast.success(t("courierDocuments.rejectSuccess"));
      reset();
      onReviewed();
      onClose();
    } catch (err) {
      toast.error(t("courierDocuments.rejectError"), { description: err instanceof ApiError ? err.message : undefined });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-md">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("courierDocuments.rejectModalTitle")}</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
        <Textarea
          label={t("courierDocuments.rejectionReasonLabel")}
          hint={t("courierDocuments.rejectionReasonHint")}
          error={errors.rejectionReason?.message}
          {...register("rejectionReason", { required: t("courierDocuments.required") })}
        />
        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("courierDocuments.cancel")}
          </Button>
          <Button type="submit" variant="danger" loading={isSubmitting}>
            {t("courierDocuments.rejectAction")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export function AdminCourierDocuments() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<string>(String(DocumentReviewStatus.Pending));
  const [refreshKey, setRefreshKey] = useState(0);
  const [rejecting, setRejecting] = useState<AdminCourierDocumentDto | null>(null);
  const statusFilter = status === "all" ? null : Number(status);
  const { data, loading, error } = useAdminCourierDocuments(page, PAGE_SIZE, statusFilter, refreshKey);

  if (loading) return <PageLoader />;

  if (error || !data) {
    return <EmptyState icon={<FileText size={26} />} title={t("courierDocuments.errorTitle")} description={error ?? t("courierDocuments.errorDescription")} />;
  }

  const typeLabel = (type: number) => t(`courierDocuments.type.${type === CourierDocumentType.DriverLicense ? "driverLicense" : "vehicleRegistration"}`);
  const statusLabel = (s: number) =>
    t(`courierDocuments.status.${s === DocumentReviewStatus.Approved ? "approved" : s === DocumentReviewStatus.Rejected ? "rejected" : "pending"}`);

  const handleApprove = async (document: AdminCourierDocumentDto) => {
    try {
      await reviewCourierDocument(document.id, DocumentReviewStatus.Approved, null);
      toast.success(t("courierDocuments.approveSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("courierDocuments.approveError"), { description: err instanceof ApiError ? err.message : undefined });
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col items-stretch gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("courierDocuments.title")}</h1>
          <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">{t("courierDocuments.subtitle")}</p>
        </div>
        <div className="w-full sm:max-w-52">
          <Select
            value={status}
            onChange={(value) => {
              setStatus(value);
              setPage(1);
            }}
          >
            <option value="all">{t("courierDocuments.filterAll")}</option>
            <option value={DocumentReviewStatus.Pending}>{statusLabel(DocumentReviewStatus.Pending)}</option>
            <option value={DocumentReviewStatus.Approved}>{statusLabel(DocumentReviewStatus.Approved)}</option>
            <option value={DocumentReviewStatus.Rejected}>{statusLabel(DocumentReviewStatus.Rejected)}</option>
          </Select>
        </div>
      </div>

      {data.items.length === 0 ? (
        <EmptyState icon={<FileText size={26} />} title={t("courierDocuments.emptyTitle")} description={t("courierDocuments.emptyDescription")} />
      ) : (
        <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
          <div className="flex flex-col gap-3 p-4 lg:hidden">
            {data.items.map((doc) => (
              <div key={doc.id} className="flex flex-col gap-2 rounded-2xl border border-stone-100 p-4 dark:border-stone-800">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="truncate font-medium text-stone-800 dark:text-stone-100">{doc.courierFullName ?? "—"}</p>
                    <p className="truncate text-xs text-stone-400 dark:text-stone-500">{doc.courierPhoneNumber ?? "—"}</p>
                  </div>
                  <span className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[doc.status] ?? STATUS_CLASSES[DocumentReviewStatus.Pending]}`}>
                    {statusLabel(doc.status)}
                  </span>
                </div>
                <p className="text-sm text-stone-600 dark:text-stone-300">{typeLabel(doc.documentType)}</p>
                {doc.status === DocumentReviewStatus.Rejected && doc.rejectionReason && (
                  <p className="text-xs text-stone-400 dark:text-stone-500">{doc.rejectionReason}</p>
                )}
                <div className="flex items-center justify-between border-t border-stone-50 pt-2 dark:border-stone-800/60">
                  <a
                    href={resolveMediaUrl(doc.fileUrl)}
                    target="_blank"
                    rel="noreferrer"
                    className="text-sm font-medium text-grove-700 underline-offset-4 hover:underline dark:text-grove-400"
                  >
                    {t("courierDocuments.viewFile")}
                  </a>
                  {doc.status === DocumentReviewStatus.Pending && (
                    <div className="flex items-center gap-1.5">
                      <button
                        onClick={() => handleApprove(doc)}
                        aria-label={t("courierDocuments.approveAction")}
                        className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-grove-50 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-grove-950 dark:hover:text-grove-400"
                      >
                        <Check size={15} />
                      </button>
                      <button
                        onClick={() => setRejecting(doc)}
                        aria-label={t("courierDocuments.rejectAction")}
                        className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                      >
                        <X size={15} />
                      </button>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>

          <div className="hidden overflow-x-auto lg:block">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="px-6 py-4 font-medium">{t("courierDocuments.columns.courier")}</th>
                  <th className="px-6 py-4 font-medium">{t("courierDocuments.columns.type")}</th>
                  <th className="px-6 py-4 font-medium">{t("courierDocuments.columns.file")}</th>
                  <th className="px-6 py-4 font-medium">{t("courierDocuments.columns.status")}</th>
                  <th className="px-6 py-4 font-medium">{t("courierDocuments.columns.uploadedAt")}</th>
                  <th className="px-6 py-4 font-medium text-right">{t("courierDocuments.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((doc) => (
                  <tr key={doc.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="px-6 py-4">
                      <div className="font-medium text-stone-800 dark:text-stone-100">{doc.courierFullName ?? "—"}</div>
                      <div className="text-xs text-stone-400 dark:text-stone-500">{doc.courierPhoneNumber ?? "—"}</div>
                    </td>
                    <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{typeLabel(doc.documentType)}</td>
                    <td className="px-6 py-4">
                      <a
                        href={resolveMediaUrl(doc.fileUrl)}
                        target="_blank"
                        rel="noreferrer"
                        className="font-medium text-grove-700 underline-offset-4 hover:underline dark:text-grove-400"
                      >
                        {t("courierDocuments.viewFile")}
                      </a>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[doc.status] ?? STATUS_CLASSES[DocumentReviewStatus.Pending]}`}>
                        {statusLabel(doc.status)}
                      </span>
                      {doc.status === DocumentReviewStatus.Rejected && doc.rejectionReason && (
                        <p className="mt-1 max-w-56 truncate text-xs text-stone-400 dark:text-stone-500">{doc.rejectionReason}</p>
                      )}
                    </td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(doc.uploadedAt)}</td>
                    <td className="px-6 py-4">
                      {doc.status === DocumentReviewStatus.Pending ? (
                        <div className="flex items-center justify-end gap-1.5">
                          <button
                            onClick={() => handleApprove(doc)}
                            aria-label={t("courierDocuments.approveAction")}
                            className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-grove-50 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-grove-950 dark:hover:text-grove-400"
                          >
                            <Check size={15} />
                          </button>
                          <button
                            onClick={() => setRejecting(doc)}
                            aria-label={t("courierDocuments.rejectAction")}
                            className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                          >
                            <X size={15} />
                          </button>
                        </div>
                      ) : (
                        <span className="block text-right text-xs text-stone-300 dark:text-stone-600">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {data.totalCount > PAGE_SIZE && (
            <div className="border-t border-stone-100 p-4 dark:border-stone-800">
              <Pagination page={data.page} totalPages={Math.max(1, Math.ceil(data.totalCount / data.pageSize))} onPageChange={setPage} />
            </div>
          )}
        </div>
      )}

      <RejectModal open={!!rejecting} onClose={() => setRejecting(null)} document={rejecting} onReviewed={() => setRefreshKey((k) => k + 1)} />
    </div>
  );
}
