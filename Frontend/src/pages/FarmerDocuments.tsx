import { useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { CheckCircle2, Clock, FileText, IdCard, Loader2, Trash2, Upload, UserRound, XCircle } from "lucide-react";
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
  REQUIRED_FARMER_DOCUMENT_TYPES,
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

const REQUIRED_SLOT_ICON: Record<number, typeof IdCard> = {
  [FarmerDocumentType.PassportFront]: IdCard,
  [FarmerDocumentType.PassportBack]: IdCard,
  [FarmerDocumentType.Selfie]: UserRound,
};

function latestByType(documents: FarmerDocumentDto[], type: number): FarmerDocumentDto | null {
  const matching = documents.filter((d) => d.documentType === type);
  if (matching.length === 0) return null;
  return matching.reduce((latest, d) => (new Date(d.uploadedAt) > new Date(latest.uploadedAt) ? d : latest));
}

export function FarmerDocuments() {
  const { t } = useTranslation("farmer");
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const [refreshKey, setRefreshKey] = useState(0);
  const { documents, loading: docsLoading, error: docsError } = useFarmerDocuments(profile?.id ?? null, refreshKey);
  const [documentType, setDocumentType] = useState<string>(String(FarmerDocumentType.LandDeed));
  const [uploadingType, setUploadingType] = useState<number | null>(null);
  const [deleting, setDeleting] = useState<FarmerDocumentDto | null>(null);
  const requiredInputRef = useRef<HTMLInputElement>(null);
  const otherInputRef = useRef<HTMLInputElement>(null);
  const pendingRequiredType = useRef<number | null>(null);

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
    t(
      `documents.type.${
        type === FarmerDocumentType.PassportFront
          ? "passportFront"
          : type === FarmerDocumentType.PassportBack
            ? "passportBack"
            : type === FarmerDocumentType.Selfie
              ? "selfie"
              : type === FarmerDocumentType.Passport
                ? "passport"
                : type === FarmerDocumentType.LandDeed
                  ? "landDeed"
                  : "other"
      }`,
    );
  const statusLabel = (status: number) =>
    t(`documents.status.${status === DocumentReviewStatus.Approved ? "approved" : status === DocumentReviewStatus.Rejected ? "rejected" : "pending"}`);

  const doUpload = async (documentTypeToUpload: number, file: File) => {
    if (!ALLOWED_DOCUMENT_TYPES.includes(file.type)) {
      toast.error(t("documents.form.fileInvalidType"));
      return;
    }
    if (file.size > MAX_DOCUMENT_SIZE_BYTES) {
      toast.error(t("documents.form.fileTooLarge"));
      return;
    }

    setUploadingType(documentTypeToUpload);
    try {
      await uploadFarmerDocument(profile.id, documentTypeToUpload, file);
      toast.success(t("documents.uploadSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("documents.uploadError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setUploadingType(null);
    }
  };

  const onRequiredFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    const type = pendingRequiredType.current;
    e.target.value = "";
    if (!file || type === null) return;
    await doUpload(type, file);
  };

  const onOtherFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    await doUpload(Number(documentType), file);
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

  const otherDocuments = documents.filter((d) => !REQUIRED_FARMER_DOCUMENT_TYPES.includes(d.documentType as (typeof REQUIRED_FARMER_DOCUMENT_TYPES)[number]));

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-3xl border border-stone-100 bg-white p-5 dark:border-stone-800 dark:bg-stone-900">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("documents.required.title")}</h2>
        <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">{t("documents.required.description")}</p>

        <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-3">
          {REQUIRED_FARMER_DOCUMENT_TYPES.map((type) => {
            const doc = latestByType(documents, type);
            const Icon = REQUIRED_SLOT_ICON[type] ?? IdCard;
            const isUploading = uploadingType === type;
            return (
              <div
                key={type}
                className={`flex flex-col gap-3 rounded-2xl border-2 p-4 transition ${
                  !doc
                    ? "border-dashed border-stone-200 dark:border-stone-700"
                    : doc.status === DocumentReviewStatus.Approved
                      ? "border-grove-200 dark:border-grove-800"
                      : doc.status === DocumentReviewStatus.Rejected
                        ? "border-rose-200 dark:border-rose-900"
                        : "border-harvest-200 dark:border-harvest-900"
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-2">
                    <Icon size={18} className="shrink-0 text-stone-400 dark:text-stone-500" />
                    <p className="text-sm font-semibold text-stone-800 dark:text-stone-100">{typeLabel(type)}</p>
                  </div>
                  {doc && (
                    <span className={`flex shrink-0 items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-semibold ${STATUS_CLASSES[doc.status] ?? STATUS_CLASSES[DocumentReviewStatus.Pending]}`}>
                      {doc.status === DocumentReviewStatus.Approved ? (
                        <CheckCircle2 size={11} />
                      ) : doc.status === DocumentReviewStatus.Rejected ? (
                        <XCircle size={11} />
                      ) : (
                        <Clock size={11} />
                      )}
                      {statusLabel(doc.status)}
                    </span>
                  )}
                </div>

                {doc ? (
                  <>
                    <p className="text-xs text-stone-400 dark:text-stone-500">{formatDate(doc.uploadedAt)}</p>
                    {doc.status === DocumentReviewStatus.Rejected && doc.rejectionReason && (
                      <p className="text-xs text-rose-600 dark:text-rose-400">{doc.rejectionReason}</p>
                    )}
                  </>
                ) : (
                  <p className="text-xs text-stone-400 dark:text-stone-500">{t("documents.required.notUploaded")}</p>
                )}

                <Button
                  type="button"
                  size="sm"
                  variant={doc ? "outline" : "primary"}
                  leftIcon={isUploading ? <Loader2 size={14} className="animate-spin" /> : <Upload size={14} />}
                  disabled={isUploading}
                  onClick={() => {
                    pendingRequiredType.current = type;
                    requiredInputRef.current?.click();
                  }}
                >
                  {doc ? t("documents.required.replaceAction") : t("documents.required.uploadAction")}
                </Button>
              </div>
            );
          })}
        </div>
        <input ref={requiredInputRef} type="file" accept="image/jpeg,image/png,image/webp,application/pdf" className="hidden" onChange={onRequiredFileChange} />
        <p className="mt-3 text-xs text-stone-400 dark:text-stone-500">{t("documents.form.fileHint")}</p>
      </div>

      <div className="flex flex-col gap-3">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("documents.otherTitle")}</h2>
        <div className="flex flex-col items-stretch gap-3 rounded-3xl border border-stone-100 bg-white p-4 sm:flex-row sm:items-end sm:justify-between dark:border-stone-800 dark:bg-stone-900">
          <div className="w-full sm:max-w-xs">
            <Select label={t("documents.form.documentType")} value={documentType} onChange={setDocumentType}>
              <option value={FarmerDocumentType.LandDeed}>{typeLabel(FarmerDocumentType.LandDeed)}</option>
              <option value={FarmerDocumentType.Other}>{typeLabel(FarmerDocumentType.Other)}</option>
            </Select>
          </div>
          <Button
            type="button"
            leftIcon={uploadingType === Number(documentType) ? <Loader2 size={16} className="animate-spin" /> : <Upload size={16} />}
            disabled={uploadingType !== null}
            onClick={() => otherInputRef.current?.click()}
          >
            {t("documents.uploadButton")}
          </Button>
          <input ref={otherInputRef} type="file" accept="image/jpeg,image/png,image/webp,application/pdf" className="hidden" onChange={onOtherFileChange} />
        </div>

        {otherDocuments.length === 0 ? (
          <EmptyState icon={<FileText size={26} />} title={t("documents.emptyTitle")} description={t("documents.emptyDescription")} />
        ) : (
          <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
            <div className="flex flex-col gap-3 p-4 lg:hidden">
              {otherDocuments.map((doc) => (
                <div key={doc.id} className="flex flex-col gap-2 rounded-2xl border border-stone-100 p-4 dark:border-stone-800">
                  <div className="flex items-start justify-between gap-3">
                    <p className="font-medium text-stone-800 dark:text-stone-100">{typeLabel(doc.documentType)}</p>
                    <span className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[doc.status] ?? STATUS_CLASSES[DocumentReviewStatus.Pending]}`}>
                      {statusLabel(doc.status)}
                    </span>
                  </div>
                  {doc.rejectionReason && <p className="text-sm text-stone-500 dark:text-stone-400">{doc.rejectionReason}</p>}
                  <div className="flex items-center justify-between border-t border-stone-50 pt-2 dark:border-stone-800/60">
                    <p className="text-xs text-stone-400 dark:text-stone-500">{formatDate(doc.uploadedAt)}</p>
                    <button
                      onClick={() => setDeleting(doc)}
                      aria-label={t("documents.deleteAction")}
                      className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                </div>
              ))}
            </div>

            <div className="hidden overflow-x-auto lg:block">
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
                  {otherDocuments.map((doc) => (
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
      </div>

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
