import { useTranslation } from "react-i18next";
import { FileText } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { formatDate } from "@/lib/utils";
import { DocumentReviewStatus, FarmerDocumentType, useFarmerDocuments, useFarmerProfile } from "@/data/farmer";

const STATUS_CLASSES: Record<number, string> = {
  [DocumentReviewStatus.Pending]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [DocumentReviewStatus.Approved]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [DocumentReviewStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

export function FarmerDocuments() {
  const { t } = useTranslation("farmer");
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const { documents, loading: docsLoading, error: docsError } = useFarmerDocuments(profile?.id ?? null);

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

  return (
    <div className="flex flex-col gap-5">
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
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <p className="rounded-2xl bg-stone-50 px-4 py-3 text-xs text-stone-500 dark:bg-stone-900 dark:text-stone-400">{t("documents.uploadHint")}</p>
    </div>
  );
}
