import { useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { FileText, Loader2, Trash2, Upload } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { Button } from "@/components/ui/Button";
import { Select } from "@/components/ui/Field";
import { ApiError } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import {
  DocumentReviewStatus,
  FarmerDocumentType,
  deleteFarmerDocument,
  uploadFarmerDocument,
  useFarmerDocuments,
  useFarmerProfile,
  type FarmerDocumentDto,
} from "@/data/farmer";

const ALLOWED_DOCUMENT_TYPES = ["image/jpeg", "image/png", "image/webp", "application/pdf"];
const MAX_DOCUMENT_SIZE_BYTES = 10 * 1024 * 1024;

const STATUS_CLASSES: Record<number, string> = {
  [DocumentReviewStatus.Pending]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [DocumentReviewStatus.Approved]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [DocumentReviewStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

export function FarmerDocuments() {
  const { t } = useTranslation("farmer");
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const [refreshKey, setRefreshKey] = useState(0);
  const { documents, loading: docsLoading, error: docsError } = useFarmerDocuments(profile?.id ?? null, refreshKey);
  const [documentType, setDocumentType] = useState<string>(String(FarmerDocumentType.Passport));
  const [uploading, setUploading] = useState(false);
  const [deleting, setDeleting] = useState<FarmerDocumentDto | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  if (profileLoading || (profile && docsLoading)) return <PageLoader />;

  if (profileError || docsError || !profile || !documents) {
    return (
      <EmptyState
        icon={<FileText size={26} />}
        title={t("documents.errorTitle")}
        description={profileError ?? docsError ?? t("documents.errorDescription")}
      />
    );
  }

  const typeLabel = (type: number) =>
    t(`documents.type.${type === FarmerDocumentType.Passport ? "passport" : type === FarmerDocumentType.LandDeed ? "landDeed" : "other"}`);
  const statusLabel = (status: number) =>
    t(`documents.status.${status === DocumentReviewStatus.Approved ? "approved" : status === DocumentReviewStatus.Rejected ? "rejected" : "pending"}`);

  const onFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;

    if (!ALLOWED_DOCUMENT_TYPES.includes(file.type)) {
      toast.error(t("documents.form.fileInvalidType"));
      return;
    }
    if (file.size > MAX_DOCUMENT_SIZE_BYTES) {
      toast.error(t("documents.form.fileTooLarge"));
      return;
    }

    setUploading(true);
    try {
      await uploadFarmerDocument(profile.id, Number(documentType), file);
      toast.success(t("documents.uploadSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("documents.uploadError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setUploading(false);
    }
  };

  const handleDelete = async () => {
    if (!deleting) return;
    try {
      await deleteFarmerDocument(deleting.id);
      toast.success(t("documents.deleteSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("documents.deleteError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col items-stretch gap-3 rounded-3xl border border-stone-100 bg-white p-4 sm:flex-row sm:items-end sm:justify-between dark:border-stone-800 dark:bg-stone-900">
        <div className="w-full sm:max-w-xs">
          <Select label={t("documents.form.documentType")} value={documentType} onChange={(e) => setDocumentType(e.target.value)}>
            <option value={FarmerDocumentType.Passport}>{typeLabel(FarmerDocumentType.Passport)}</option>
            <option value={FarmerDocumentType.LandDeed}>{typeLabel(FarmerDocumentType.LandDeed)}</option>
            <option value={FarmerDocumentType.Other}>{typeLabel(FarmerDocumentType.Other)}</option>
          </Select>
        </div>
        <Button
          type="button"
          leftIcon={uploading ? <Loader2 size={16} className="animate-spin" /> : <Upload size={16} />}
          disabled={uploading}
          onClick={() => inputRef.current?.click()}
        >
          {t("documents.uploadButton")}
        </Button>
        <input ref={inputRef} type="file" accept="image/jpeg,image/png,image/webp,application/pdf" className="hidden" onChange={onFileChange} />
      </div>
      <p className="text-xs text-stone-400 dark:text-stone-500">{t("documents.form.fileHint")}</p>

      {documents.length === 0 ? (
        <EmptyState icon={<FileText size={26} />} title={t("documents.emptyTitle")} description={t("documents.emptyDescription")} />
      ) : (
        <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="px-6 py-4 font-medium">{t("documents.columns.type")}</th>
                  <th className="px-6 py-4 font-medium">{t("documents.columns.status")}</th>
                  <th className="px-6 py-4 font-medium">{t("documents.columns.rejectionReason")}</th>
                  <th className="px-6 py-4 font-medium">{t("documents.columns.uploadedAt")}</th>
                  <th className="px-6 py-4 font-medium text-right">{t("documents.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {documents.map((doc) => (
                  <tr key={doc.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{typeLabel(doc.documentType)}</td>
                    <td className="px-6 py-4">
                      <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[doc.status] ?? STATUS_CLASSES[DocumentReviewStatus.Pending]}`}>
                        {statusLabel(doc.status)}
                      </span>
                    </td>
                    <td className="max-w-64 truncate px-6 py-4 text-stone-500 dark:text-stone-400">{doc.rejectionReason ?? "—"}</td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(doc.uploadedAt)}</td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end">
                        <button
                          onClick={() => setDeleting(doc)}
                          aria-label={t("documents.deleteAction")}
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                        >
                          <Trash2 size={15} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={!!deleting}
        onClose={() => setDeleting(null)}
        onConfirm={handleDelete}
        title={t("documents.deleteConfirmTitle")}
        description={t("documents.deleteConfirmDescription")}
        confirmLabel={t("documents.deleteAction")}
      />
    </div>
  );
}
