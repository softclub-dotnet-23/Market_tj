import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import {
  ArrowLeft,
  Calendar,
  Check,
  FileText,
  Mail,
  MapPin,
  Package,
  Phone,
  ShoppingCart,
  Sprout,
  Star,
  Trash2,
  Wallet,
  X,
} from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Avatar } from "@/components/ui/Avatar";
import { Card } from "@/components/ui/Card";
import { StatCard } from "@/components/ui/StatCard";
import { RatingStars } from "@/components/ui/RatingStars";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { resolveMediaUrl } from "@/lib/api";
import { formatDate, formatSomoni } from "@/lib/utils";
import {
  DocumentReviewStatus,
  FarmerVerificationStatus,
  ListingStatus,
  OrderStatus,
  deleteAdminProductListing,
  reviewFarmerDocument,
  updateFarmerVerification,
  useAdminFarmerById,
  useAdminFarmerDashboard,
  useAdminFarmerDocuments,
  useAdminOrderItems,
  useAdminOrders,
  useAdminProducts,
  useAdminUserById,
  type AdminProductListingDto,
} from "@/data/adminEntities";
import { useAuth } from "@/context/AuthContext";
import { useProductPhotoMap } from "@/data/farmer";

// Всё, что нужно админу для управления ОДНИМ фермером в одном месте — по
// прямому запросу пользователя ("чтобы админу было удобно управлять
// фермером и его товарами, чтобы он знал каждый шаг"). Заменяет собой
// отдельную страницу /admin/products (удалена) — там показывались объявления
// ВСЕХ фермеров разом без привязки к остальным данным фермера; здесь товары
// только этого фермера, рядом с его профилем/документами/статистикой.
const ALL_ITEMS_PAGE_SIZE = 10000;

const STATUS_CLASSES: Record<number, string> = {
  [FarmerVerificationStatus.Pending]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [FarmerVerificationStatus.Verified]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [FarmerVerificationStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

const STATUS_KEYS: Record<number, string> = {
  [FarmerVerificationStatus.Pending]: "pending",
  [FarmerVerificationStatus.Verified]: "verified",
  [FarmerVerificationStatus.Rejected]: "rejected",
};

const LISTING_STATUS_CLASSES: Record<number, string> = {
  [ListingStatus.Draft]: "bg-stone-100 text-stone-600 dark:bg-stone-800 dark:text-stone-300",
  [ListingStatus.Active]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [ListingStatus.OutOfStock]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [ListingStatus.Archived]: "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500",
};

const DOC_STATUS_CLASSES: Record<number, string> = {
  [DocumentReviewStatus.Pending]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [DocumentReviewStatus.Approved]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [DocumentReviewStatus.Rejected]: "bg-rose-100 text-rose-700 dark:bg-rose-900 dark:text-rose-300",
};

const DOC_STATUS_KEYS: Record<number, string> = {
  [DocumentReviewStatus.Pending]: "pending",
  [DocumentReviewStatus.Approved]: "approved",
  [DocumentReviewStatus.Rejected]: "rejected",
};

const DOC_TYPE_KEYS: Record<number, string> = { 1: "passport", 2: "landDeed", 3: "other" };

export function AdminFarmerDetail() {
  const { t } = useTranslation(["admin", "product"]);
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const farmerProfileId = id ? Number(id) : null;

  const [refreshKey, setRefreshKey] = useState(0);
  const [busy, setBusy] = useState(false);
  const [deleting, setDeleting] = useState<AdminProductListingDto | null>(null);

  const { farmer, loading: farmerLoading, error: farmerError } = useAdminFarmerById(farmerProfileId, refreshKey);
  const { account, loading: accountLoading } = useAdminUserById(farmer?.userId ?? null);
  const { dashboard, loading: dashboardLoading } = useAdminFarmerDashboard(farmerProfileId, refreshKey);
  const { data: productsPage, loading: productsLoading } = useAdminProducts(1, ALL_ITEMS_PAGE_SIZE, refreshKey);
  const { data: documentsPage, loading: documentsLoading } = useAdminFarmerDocuments(1, ALL_ITEMS_PAGE_SIZE, null, refreshKey);
  const { orders } = useAdminOrders();
  const { orderItems } = useAdminOrderItems();

  // Фильтруем по farmerProfileId из URL, а не farmer.id — до того как farmer
  // загрузится, farmer.id ещё недоступен, а хуки ниже (особенно
  // useProductPhotoMap) обязаны вызываться безусловно на каждом рендере
  // (Rules of Hooks), то есть до любых ранних return по loading/error.
  const products = (productsPage?.items ?? []).filter((p) => p.farmerProfileId === farmerProfileId);
  const documents = (documentsPage?.items ?? []).filter((d) => d.farmerProfileId === farmerProfileId);
  const photoByListingId = useProductPhotoMap(products);

  const loading = farmerLoading || accountLoading || dashboardLoading || productsLoading || documentsLoading;

  if (loading) return <PageLoader />;

  if (farmerError || !farmer) {
    return <EmptyState icon={<Sprout size={26} />} title={t("farmers.errorTitle")} description={farmerError ?? t("farmers.errorDescription")} />;
  }

  const activeOrderIds = new Set(
    (orders ?? []).filter((o) => o.status !== OrderStatus.Rejected && o.status !== OrderStatus.Cancelled).map((o) => o.id),
  );
  const soldByListingId = new Map<number, number>();
  (orderItems ?? []).forEach((item) => {
    if (!activeOrderIds.has(item.orderId)) return;
    soldByListingId.set(item.productListingId, (soldByListingId.get(item.productListingId) ?? 0) + item.quantity);
  });

  const handleSetStatus = async (status: number) => {
    setBusy(true);
    try {
      await updateFarmerVerification(farmer, status, user?.userId ?? 0);
      toast.success(status === FarmerVerificationStatus.Verified ? t("farmers.verifySuccess") : t("farmers.rejectSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("farmers.actionError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setBusy(false);
    }
  };

  const handleDeleteProduct = async () => {
    if (!deleting) return;
    try {
      await deleteAdminProductListing(deleting.id);
      toast.success(t("products.deleteSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("products.deleteError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  const handleReviewDocument = async (docId: number, status: number) => {
    setBusy(true);
    try {
      await reviewFarmerDocument(docId, status, null);
      toast.success(status === DocumentReviewStatus.Approved ? t("farmerDocuments.approveSuccess") : t("farmerDocuments.rejectSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(
        status === DocumentReviewStatus.Approved ? t("farmerDocuments.approveError") : t("farmerDocuments.rejectError"),
        { description: err instanceof Error ? err.message : undefined },
      );
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <button
        onClick={() => navigate("/admin/farmers")}
        className="inline-flex w-fit items-center gap-1.5 text-sm font-medium text-stone-500 transition hover:text-grove-700 dark:text-stone-400 dark:hover:text-grove-400"
      >
        <ArrowLeft size={15} />
        {t("farmerDetail.back")}
      </button>

      {/* Профиль фермера */}
      <Card className="flex flex-col gap-5">
        <div className="flex flex-col items-start gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-4">
            <Avatar name={farmer.farmName} src={farmer.avatarUrl ? resolveMediaUrl(farmer.avatarUrl) : undefined} size={64} ring />
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">{farmer.farmName}</h1>
                <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[farmer.verificationStatus] ?? STATUS_CLASSES[FarmerVerificationStatus.Pending]}`}>
                  {t(`farmers.status.${STATUS_KEYS[farmer.verificationStatus] ?? "pending"}`)}
                </span>
              </div>
              <p className="mt-1 flex items-center gap-1 text-sm text-stone-500 dark:text-stone-400">
                <MapPin size={13} className="shrink-0" />
                {farmer.region}, {farmer.district}, {farmer.village}
              </p>
              {dashboard?.averageRating != null && (
                <div className="mt-1.5">
                  <RatingStars rating={dashboard.averageRating} size={13} showValue />
                </div>
              )}
            </div>
          </div>

          <div className="flex items-center gap-2">
            {farmer.verificationStatus !== FarmerVerificationStatus.Verified && (
              <button
                onClick={() => handleSetStatus(FarmerVerificationStatus.Verified)}
                disabled={busy}
                className="flex h-10 items-center gap-1.5 rounded-xl bg-grove-700 px-4 text-sm font-medium text-white transition hover:bg-grove-800 disabled:opacity-50"
              >
                <Check size={15} />
                {t("farmers.verifyAction")}
              </button>
            )}
            {farmer.verificationStatus !== FarmerVerificationStatus.Rejected && (
              <button
                onClick={() => handleSetStatus(FarmerVerificationStatus.Rejected)}
                disabled={busy}
                className="flex h-10 items-center gap-1.5 rounded-xl border border-rose-200 px-4 text-sm font-medium text-rose-600 transition hover:bg-rose-50 disabled:opacity-50 dark:border-rose-900 dark:text-rose-400 dark:hover:bg-rose-950"
              >
                <X size={15} />
                {t("farmers.rejectAction")}
              </button>
            )}
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3 border-t border-stone-100 pt-4 text-sm sm:grid-cols-2 lg:grid-cols-4 dark:border-stone-800">
          <div className="flex items-center gap-2 text-stone-500 dark:text-stone-400">
            <Calendar size={14} className="shrink-0" />
            {t("farmerDetail.registeredAt", { date: formatDate(farmer.createdAt) })}
          </div>
          {account && (
            <>
              <div className="flex items-center gap-2 text-stone-500 dark:text-stone-400">
                <Mail size={14} className="shrink-0" />
                <span className="truncate">{account.email}</span>
              </div>
              <div className="flex items-center gap-2 text-stone-500 dark:text-stone-400">
                <Phone size={14} className="shrink-0" />
                {account.phoneNumber}
              </div>
              <div className="flex items-center gap-2 text-stone-500 dark:text-stone-400">
                {t("farmerDetail.ownerName", { name: account.fullName })}
              </div>
            </>
          )}
        </div>

        {farmer.description && <p className="border-t border-stone-100 pt-4 text-sm text-stone-600 dark:border-stone-800 dark:text-stone-300">{farmer.description}</p>}
      </Card>

      {/* Статистика: сколько продал, сколько получил */}
      {dashboard && (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <StatCard icon={Package} accent="grove" label={t("farmerDetail.stats.totalProducts")} value={String(dashboard.totalOwnProducts)} />
          <StatCard icon={Package} accent="blue" label={t("farmerDetail.stats.activeProducts")} value={String(dashboard.activeProducts)} />
          <StatCard icon={ShoppingCart} accent="orange" label={t("farmerDetail.stats.totalOrders")} value={String(dashboard.totalOrdersReceived)} />
          <StatCard icon={ShoppingCart} accent="orange" label={t("farmerDetail.stats.ordersThisMonth")} value={String(dashboard.ordersThisMonth)} />
          <StatCard icon={Wallet} accent="grove" label={t("farmerDetail.stats.totalRevenue")} value={`${formatSomoni(dashboard.totalRevenue)} ${t("common.somoni")}`} />
          <StatCard icon={Wallet} accent="grove" label={t("farmerDetail.stats.revenueThisMonth")} value={`${formatSomoni(dashboard.revenueThisMonth)} ${t("common.somoni")}`} />
          <StatCard icon={Star} accent="rose" label={t("farmerDetail.stats.rating")} value={dashboard.averageRating != null ? dashboard.averageRating.toFixed(1) : "—"} />
          <StatCard icon={FileText} accent="blue" label={t("farmerDetail.stats.documents")} value={String(documents.length)} />
        </div>
      )}

      {/* Документы */}
      <Card className="flex flex-col gap-4">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("farmerDetail.documentsTitle")}</h2>
        {documents.length === 0 ? (
          <p className="text-sm text-stone-400 dark:text-stone-500">{t("farmerDocuments.emptyDescription")}</p>
        ) : (
          <div className="flex flex-col gap-2.5">
            {documents.map((doc) => (
              <div key={doc.id} className="flex flex-wrap items-center gap-3 rounded-xl border border-stone-100 p-3 dark:border-stone-800">
                <FileText size={16} className="shrink-0 text-stone-400 dark:text-stone-500" />
                <span className="text-sm font-medium text-stone-700 dark:text-stone-200">{t(`farmerDocuments.type.${DOC_TYPE_KEYS[doc.documentType] ?? "other"}`)}</span>
                <span className={`rounded-full px-2 py-0.5 text-xs font-semibold ${DOC_STATUS_CLASSES[doc.status] ?? DOC_STATUS_CLASSES[DocumentReviewStatus.Pending]}`}>
                  {t(`farmerDocuments.status.${DOC_STATUS_KEYS[doc.status] ?? "pending"}`)}
                </span>
                <span className="text-xs text-stone-400 dark:text-stone-500">{formatDate(doc.uploadedAt)}</span>
                <a
                  href={resolveMediaUrl(doc.fileUrl)}
                  target="_blank"
                  rel="noreferrer"
                  className="text-xs font-medium text-grove-700 hover:underline dark:text-grove-400"
                >
                  {t("farmerDocuments.viewFile")}
                </a>
                <div className="ml-auto flex items-center gap-1.5">
                  {doc.status !== DocumentReviewStatus.Approved && (
                    <button
                      onClick={() => handleReviewDocument(doc.id, DocumentReviewStatus.Approved)}
                      disabled={busy}
                      aria-label={t("farmerDocuments.approveAction")}
                      className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-grove-50 hover:text-grove-700 disabled:opacity-50 dark:text-stone-500 dark:hover:bg-grove-950 dark:hover:text-grove-400"
                    >
                      <Check size={15} />
                    </button>
                  )}
                  {doc.status !== DocumentReviewStatus.Rejected && (
                    <button
                      onClick={() => handleReviewDocument(doc.id, DocumentReviewStatus.Rejected)}
                      disabled={busy}
                      aria-label={t("farmerDocuments.rejectAction")}
                      className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 disabled:opacity-50 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                    >
                      <X size={15} />
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>

      {/* Товары фермера */}
      <Card className="flex flex-col gap-4">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("farmerDetail.productsTitle")}</h2>
        {products.length === 0 ? (
          <p className="text-sm text-stone-400 dark:text-stone-500">{t("products.emptyDescription")}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="py-3 pr-4 font-medium">{t("products.columns.title")}</th>
                  <th className="py-3 pr-4 font-medium">{t("products.columns.price")}</th>
                  <th className="py-3 pr-4 font-medium">{t("products.columns.quantity")}</th>
                  <th className="py-3 pr-4 font-medium">{t("products.columns.status")}</th>
                  <th className="py-3 pr-4 font-medium text-right">{t("products.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {products.map((product) => {
                  const photo = photoByListingId.get(product.id);
                  const sold = soldByListingId.get(product.id) ?? 0;
                  return (
                    <tr key={product.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                      <td className="py-3 pr-4">
                        <div className="flex items-center gap-3">
                          {photo ? (
                            <img src={photo} alt="" className="h-12 w-12 shrink-0 rounded-xl object-cover" loading="lazy" />
                          ) : (
                            <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-grove-50 text-grove-400 dark:bg-stone-800 dark:text-stone-500">
                              <Package size={18} />
                            </span>
                          )}
                          <span className="max-w-48 truncate font-medium text-stone-800 dark:text-stone-100">{product.title}</span>
                        </div>
                      </td>
                      <td className="py-3 pr-4 font-semibold text-stone-800 dark:text-stone-100">
                        {formatSomoni(product.retailPricePerKg)} {t("products.pricePerUnitSuffix", { unit: t(`product:units.${product.unit}`) })}
                      </td>
                      <td className="py-3 pr-4 text-stone-600 dark:text-stone-300">
                        <div className="flex flex-col">
                          <span>
                            {product.availableQuantity} {t(`product:units.${product.unit}`)}
                          </span>
                          {sold > 0 && (
                            <span className="text-xs text-stone-400 dark:text-stone-500">{t("farmerDetail.soldCount", { count: sold, unit: t(`product:units.${product.unit}`) })}</span>
                          )}
                        </div>
                      </td>
                      <td className="py-3 pr-4">
                        <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${LISTING_STATUS_CLASSES[product.status] ?? LISTING_STATUS_CLASSES[ListingStatus.Draft]}`}>
                          {t(
                            `products.status.${
                              product.status === ListingStatus.Active
                                ? "active"
                                : product.status === ListingStatus.OutOfStock
                                  ? "outOfStock"
                                  : product.status === ListingStatus.Archived
                                    ? "archived"
                                    : "draft"
                            }`,
                          )}
                        </span>
                      </td>
                      <td className="py-3 pr-4">
                        <div className="flex items-center justify-end">
                          <button
                            onClick={() => setDeleting(product)}
                            aria-label={t("products.deleteAction")}
                            className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                          >
                            <Trash2 size={15} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <ConfirmDialog
        open={!!deleting}
        onClose={() => setDeleting(null)}
        onConfirm={handleDeleteProduct}
        title={t("products.deleteConfirmTitle")}
        description={deleting ? t("products.deleteConfirmDescription", { title: deleting.title }) : undefined}
        confirmLabel={t("products.deleteAction")}
      />
    </div>
  );
}
