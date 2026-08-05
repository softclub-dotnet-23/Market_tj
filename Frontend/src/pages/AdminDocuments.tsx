import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useSearchParams } from "react-router-dom";
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
import { DocumentReviewStatus, FarmerDocumentType } from "@/data/farmer";
import { CourierDocumentType } from "@/data/courier";
import { reviewFarmerDocument, reviewCourierDocument, useAdminFarmerDocuments, useAdminCourierDocuments } from "@/data/adminEntities";

const PAGE_SIZE = 20;
// "Все" грузит оба источника разом и мерджит на клиенте (см. ниже) — единого
// backend-эндпоинта для документов фермеров+курьеров нет и делать его ради
// одной страницы админки избыточно. 200 с каждой стороны с запасом покрывает
// реальный объём очереди модерации.
const ALL_FETCH_SIZE = 200;

type DocKind = "farmer" | "courier";
type DocTypeFilter = "all" | DocKind;

interface UnifiedDoc {
  key: string;
  id: number;
  kind: DocKind;
  primaryName: string;
  secondaryName: string | null;
  documentType: number;
  fileUrl: string;
  status: number;
  uploadedAt: string;
  rejectionReason: string | null;
}

const STATUS_CLASSES: Record<number, string> = {
  [DocumentReviewStatus.Pending]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [DocumentReviewStatus.Approved]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [DocumentReviewStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

const ROLE_BADGE_CLASSES: Record<DocKind, string> = {
  farmer: "bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-400",
  courier: "bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-400",
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
  document: UnifiedDoc | null;
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
      if (document.kind === "farmer") await reviewFarmerDocument(document.id, DocumentReviewStatus.Rejected, values.rejectionReason);
      else await reviewCourierDocument(document.id, DocumentReviewStatus.Rejected, values.rejectionReason);
      toast.success(t("farmerDocuments.rejectSuccess"));
      reset();
      onReviewed();
      onClose();
    } catch (err) {
      toast.error(t("farmerDocuments.rejectError"), { description: err instanceof ApiError ? err.message : undefined });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-md">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("farmerDocuments.rejectModalTitle")}</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
        <Textarea
          label={t("farmerDocuments.rejectionReasonLabel")}
          hint={t(document?.kind === "courier" ? "courierDocuments.rejectionReasonHint" : "farmerDocuments.rejectionReasonHint")}
          error={errors.rejectionReason?.message}
          {...register("rejectionReason", { required: t("farmerDocuments.required") })}
        />
        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("farmerDocuments.cancel")}
          </Button>
          <Button type="submit" variant="danger" loading={isSubmitting}>
            {t("farmerDocuments.rejectAction")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export function AdminDocuments() {
  const { t } = useTranslation("admin");
  const [searchParams] = useSearchParams();
  const initialType = searchParams.get("type");
  const [docType, setDocType] = useState<DocTypeFilter>(initialType === "farmer" || initialType === "courier" ? initialType : "all");
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<string>(String(DocumentReviewStatus.Pending));
  const [refreshKey, setRefreshKey] = useState(0);
  const [rejecting, setRejecting] = useState<UnifiedDoc | null>(null);
  const statusFilter = status === "all" ? null : Number(status);

  const farmerEnabled = docType !== "courier";
  const courierEnabled = docType !== "farmer";
  const farmerPage = docType === "all" ? 1 : page;
  const courierPage = docType === "all" ? 1 : page;
  const farmerPageSize = docType === "all" ? ALL_FETCH_SIZE : PAGE_SIZE;
  const courierPageSize = docType === "all" ? ALL_FETCH_SIZE : PAGE_SIZE;

  const farmer = useAdminFarmerDocuments(farmerPage, farmerPageSize, statusFilter, refreshKey, farmerEnabled);
  const courier = useAdminCourierDocuments(courierPage, courierPageSize, statusFilter, refreshKey, courierEnabled);

  const loading = docType === "farmer" ? farmer.loading : docType === "courier" ? courier.loading : farmer.loading || courier.loading;
  const error = docType === "farmer" ? farmer.error : docType === "courier" ? courier.error : (farmer.error ?? courier.error);
  const ready = docType === "farmer" ? !!farmer.data : docType === "courier" ? !!courier.data : !!farmer.data && !!courier.data;

  const items = useMemo<UnifiedDoc[]>(() => {
    const farmerItems: UnifiedDoc[] = (farmer.data?.items ?? []).map((doc) => ({
      key: `farmer-${doc.id}`,
      id: doc.id,
      kind: "farmer",
      primaryName: doc.farmName,
      secondaryName: doc.farmerFullName,
      documentType: doc.documentType,
      fileUrl: doc.fileUrl,
      status: doc.status,
      uploadedAt: doc.uploadedAt,
      rejectionReason: doc.rejectionReason,
    }));
    const courierItems: UnifiedDoc[] = (courier.data?.items ?? []).map((doc) => ({
      key: `courier-${doc.id}`,
      id: doc.id,
      kind: "courier",
      primaryName: doc.courierFullName ?? "—",
      secondaryName: doc.courierPhoneNumber,
      documentType: doc.documentType,
      fileUrl: doc.fileUrl,
      status: doc.status,
      uploadedAt: doc.uploadedAt,
      rejectionReason: doc.rejectionReason,
    }));

    if (docType === "farmer") return farmerItems;
    if (docType === "courier") return courierItems;
    return [...farmerItems, ...courierItems].sort((a, b) => new Date(b.uploadedAt).getTime() - new Date(a.uploadedAt).getTime());
  }, [docType, farmer.data, courier.data]);

  const displayItems = docType === "all" ? items.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE) : items;
  const totalCount = docType === "all" ? items.length : (docType === "farmer" ? farmer.data?.totalCount : courier.data?.totalCount) ?? 0;

  if (loading) return <PageLoader />;

  if (error || !ready) {
    return <EmptyState icon={<FileText size={26} />} title={t("farmerDocuments.errorTitle")} description={error ?? t("farmerDocuments.errorDescription")} />;
  }

  const typeLabel = (doc: UnifiedDoc) => {
    if (doc.kind === "farmer") {
      return t(
        `farmerDocuments.type.${
          doc.documentType === FarmerDocumentType.PassportFront
            ? "passportFront"
            : doc.documentType === FarmerDocumentType.PassportBack
              ? "passportBack"
              : doc.documentType === FarmerDocumentType.Selfie
                ? "selfie"
                : doc.documentType === FarmerDocumentType.Passport
                  ? "passport"
                  : doc.documentType === FarmerDocumentType.LandDeed
                    ? "landDeed"
                    : "other"
        }`,
      );
    }
    return t(`courierDocuments.type.${doc.documentType === CourierDocumentType.DriverLicense ? "driverLicense" : "vehicleRegistration"}`);
  };
  const statusLabel = (s: number) =>
    t(`farmerDocuments.status.${s === DocumentReviewStatus.Approved ? "approved" : s === DocumentReviewStatus.Rejected ? "rejected" : "pending"}`);
  const nameColumnLabel =
    docType === "farmer" ? t("farmerDocuments.columns.farm") : docType === "courier" ? t("courierDocuments.columns.courier") : t("documents.columns.name");

  const handleApprove = async (doc: UnifiedDoc) => {
    try {
      if (doc.kind === "farmer") await reviewFarmerDocument(doc.id, DocumentReviewStatus.Approved, null);
      else await reviewCourierDocument(doc.id, DocumentReviewStatus.Approved, null);
      toast.success(t("farmerDocuments.approveSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("farmerDocuments.approveError"), { description: err instanceof ApiError ? err.message : undefined });
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col items-stretch gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("documents.title")}</h1>
          <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">{t("documents.subtitle")}</p>
        </div>
        <div className="grid w-full grid-cols-2 gap-3 sm:flex sm:w-auto">
          <div className="sm:w-44">
            <Select
              value={docType}
              onChange={(value) => {
                setDocType(value as DocTypeFilter);
                setPage(1);
              }}
            >
              <option value="all">{t("documents.filterType.all")}</option>
              <option value="farmer">{t("documents.filterType.farmer")}</option>
              <option value="courier">{t("documents.filterType.courier")}</option>
            </Select>
          </div>
          <div className="sm:w-44">
            <Select
              value={status}
              onChange={(value) => {
                setStatus(value);
                setPage(1);
              }}
            >
              <option value="all">{t("farmerDocuments.filterAll")}</option>
              <option value={DocumentReviewStatus.Pending}>{statusLabel(DocumentReviewStatus.Pending)}</option>
              <option value={DocumentReviewStatus.Approved}>{statusLabel(DocumentReviewStatus.Approved)}</option>
              <option value={DocumentReviewStatus.Rejected}>{statusLabel(DocumentReviewStatus.Rejected)}</option>
            </Select>
          </div>
        </div>
      </div>

      {displayItems.length === 0 ? (
        <EmptyState icon={<FileText size={26} />} title={t("farmerDocuments.emptyTitle")} description={t("documents.emptyDescription")} />
      ) : (
        <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
          <div className="flex flex-col gap-3 p-4 lg:hidden">
            {displayItems.map((doc) => (
              <div key={doc.key} className="flex flex-col gap-2 rounded-2xl border border-stone-100 p-4 dark:border-stone-800">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="truncate font-medium text-stone-800 dark:text-stone-100">{doc.primaryName}</p>
                    <p className="truncate text-xs text-stone-400 dark:text-stone-500">{doc.secondaryName ?? "—"}</p>
                  </div>
                  <span className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[doc.status] ?? STATUS_CLASSES[DocumentReviewStatus.Pending]}`}>
                    {statusLabel(doc.status)}
                  </span>
                </div>
                <div className="flex items-center gap-2">
                  {docType === "all" && (
                    <span className={`rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide ${ROLE_BADGE_CLASSES[doc.kind]}`}>
                      {t(`documents.role.${doc.kind}`)}
                    </span>
                  )}
                  <p className="text-sm text-stone-600 dark:text-stone-300">{typeLabel(doc)}</p>
                </div>
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
                    {t("farmerDocuments.viewFile")}
                  </a>
                  {doc.status === DocumentReviewStatus.Pending && (
                    <div className="flex items-center gap-1.5">
                      <button
                        onClick={() => handleApprove(doc)}
                        aria-label={t("farmerDocuments.approveAction")}
                        className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-grove-50 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-grove-950 dark:hover:text-grove-400"
                      >
                        <Check size={15} />
                      </button>
                      <button
                        onClick={() => setRejecting(doc)}
                        aria-label={t("farmerDocuments.rejectAction")}
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
                  <th className="px-6 py-4 font-medium">{nameColumnLabel}</th>
                  {docType === "all" && <th className="px-6 py-4 font-medium">{t("documents.columns.role")}</th>}
                  <th className="px-6 py-4 font-medium">{t("farmerDocuments.columns.type")}</th>
                  <th className="px-6 py-4 font-medium">{t("farmerDocuments.columns.file")}</th>
                  <th className="px-6 py-4 font-medium">{t("farmerDocuments.columns.status")}</th>
                  <th className="px-6 py-4 font-medium">{t("farmerDocuments.columns.uploadedAt")}</th>
                  <th className="px-6 py-4 font-medium text-right">{t("farmerDocuments.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {displayItems.map((doc) => (
                  <tr key={doc.key} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="px-6 py-4">
                      <div className="font-medium text-stone-800 dark:text-stone-100">{doc.primaryName}</div>
                      <div className="text-xs text-stone-400 dark:text-stone-500">{doc.secondaryName ?? "—"}</div>
                    </td>
                    {docType === "all" && (
                      <td className="px-6 py-4">
                        <span className={`rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide ${ROLE_BADGE_CLASSES[doc.kind]}`}>
                          {t(`documents.role.${doc.kind}`)}
                        </span>
                      </td>
                    )}
                    <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{typeLabel(doc)}</td>
                    <td className="px-6 py-4">
                      <a
                        href={resolveMediaUrl(doc.fileUrl)}
                        target="_blank"
                        rel="noreferrer"
                        className="font-medium text-grove-700 underline-offset-4 hover:underline dark:text-grove-400"
                      >
                        {t("farmerDocuments.viewFile")}
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
                            aria-label={t("farmerDocuments.approveAction")}
                            className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-grove-50 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-grove-950 dark:hover:text-grove-400"
                          >
                            <Check size={15} />
                          </button>
                          <button
                            onClick={() => setRejecting(doc)}
                            aria-label={t("farmerDocuments.rejectAction")}
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

          {totalCount > PAGE_SIZE && (
            <div className="border-t border-stone-100 p-4 dark:border-stone-800">
              <Pagination page={page} totalPages={Math.max(1, Math.ceil(totalCount / PAGE_SIZE))} onPageChange={setPage} />
            </div>
          )}
        </div>
      )}

      <RejectModal open={!!rejecting} onClose={() => setRejecting(null)} document={rejecting} onReviewed={() => setRefreshKey((k) => k + 1)} />
    </div>
  );
}
