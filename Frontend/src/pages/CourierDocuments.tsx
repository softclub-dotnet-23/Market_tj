import { useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Car, CheckCircle2, Clock, FileText, IdCard, Loader2, Upload, XCircle } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Button } from "@/components/ui/Button";
import { ApiError } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import { DocumentReviewStatus } from "@/data/farmer";
import {
  CourierDocumentType,
  REQUIRED_COURIER_DOCUMENT_TYPES,
  uploadCourierDocument,
  useCourierDocuments,
  useCourierProfile,
  type CourierDocumentDto,
} from "@/data/courier";

const ALLOWED_DOCUMENT_TYPES = ["image/jpeg", "image/png", "image/webp", "application/pdf"];
const MAX_DOCUMENT_SIZE_BYTES = 10 * 1024 * 1024;

const STATUS_CLASSES: Record<number, string> = {
  [DocumentReviewStatus.Pending]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [DocumentReviewStatus.Approved]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [DocumentReviewStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

const REQUIRED_SLOT_ICON: Record<number, typeof IdCard> = {
  [CourierDocumentType.DriverLicense]: IdCard,
  [CourierDocumentType.VehicleRegistration]: Car,
};

function latestByType(documents: CourierDocumentDto[], type: number): CourierDocumentDto | null {
  const matching = documents.filter((d) => d.documentType === type);
  if (matching.length === 0) return null;
  return matching.reduce((latest, d) => (new Date(d.uploadedAt) > new Date(latest.uploadedAt) ? d : latest));
}

export function CourierDocuments() {
  const { t } = useTranslation("courier");
  const { profile, loading: profileLoading, error: profileError } = useCourierProfile();
  const [refreshKey, setRefreshKey] = useState(0);
  const { documents, loading: docsLoading, error: docsError } = useCourierDocuments(profile?.id ?? null, refreshKey);
  const [uploadingType, setUploadingType] = useState<number | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const pendingType = useRef<number | null>(null);

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

  const typeLabel = (type: number) => t(`documents.type.${type === CourierDocumentType.DriverLicense ? "driverLicense" : "vehicleRegistration"}`);
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
      await uploadCourierDocument(profile.id, documentTypeToUpload, file);
      toast.success(t("documents.uploadSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("documents.uploadError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setUploadingType(null);
    }
  };

  const onFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    const type = pendingType.current;
    e.target.value = "";
    if (!file || type === null) return;
    await doUpload(type, file);
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-3xl border border-stone-100 bg-white p-5 dark:border-stone-800 dark:bg-stone-900">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("documents.required.title")}</h2>
        <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">{t("documents.required.description")}</p>

        <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
          {REQUIRED_COURIER_DOCUMENT_TYPES.map((type) => {
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
                    pendingType.current = type;
                    inputRef.current?.click();
                  }}
                >
                  {doc ? t("documents.required.replaceAction") : t("documents.required.uploadAction")}
                </Button>
              </div>
            );
          })}
        </div>
        <input ref={inputRef} type="file" accept="image/jpeg,image/png,image/webp,application/pdf" className="hidden" onChange={onFileChange} />
        <p className="mt-3 text-xs text-stone-400 dark:text-stone-500">{t("documents.form.fileHint")}</p>
      </div>
    </div>
  );
}
