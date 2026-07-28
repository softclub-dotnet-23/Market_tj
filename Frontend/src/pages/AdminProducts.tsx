import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Package, Trash2 } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { ViewModeToggle, type OrdersViewMode } from "@/components/ui/ViewModeToggle";
import { formatSomoni } from "@/lib/utils";
import {
  ListingStatus,
  OrderStatus,
  deleteAdminProductListing,
  useAdminOrderItems,
  useAdminOrders,
  useAdminProducts,
  type AdminProductListingDto,
} from "@/data/adminEntities";
// Фото объявления — общий "грузим всё" хук, уже используемый для менеджмента
// собственных фото на панели фермера (data/farmer.ts), просто ещё один
// потребитель того же общего списка /product-images.
import { useProductPhotoMap } from "@/data/farmer";

const PAGE_SIZE = 10;

const STATUS_CLASSES: Record<number, string> = {
  [ListingStatus.Draft]: "bg-stone-100 text-stone-600 dark:bg-stone-800 dark:text-stone-300",
  [ListingStatus.Active]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [ListingStatus.OutOfStock]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [ListingStatus.Archived]: "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500",
};

export function AdminProducts() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [viewMode, setViewMode] = useState<OrdersViewMode>("table");
  const [refreshKey, setRefreshKey] = useState(0);
  const [deleting, setDeleting] = useState<AdminProductListingDto | null>(null);
  const { data, loading, error } = useAdminProducts(page, PAGE_SIZE, refreshKey);
  const { orders } = useAdminOrders();
  const { orderItems } = useAdminOrderItems();
  const photoByListingId = useProductPhotoMap(data?.items);

  if (loading) return <PageLoader />;

  if (error || !data) {
    return <EmptyState icon={<Package size={26} />} title={t("products.errorTitle")} description={error ?? t("products.errorDescription")} />;
  }

  if (data.items.length === 0) {
    return <EmptyState icon={<Package size={26} />} title={t("products.emptyTitle")} description={t("products.emptyDescription")} />;
  }

  const statusLabel = (status: number) =>
    t(`products.status.${status === ListingStatus.Active ? "active" : status === ListingStatus.OutOfStock ? "outOfStock" : status === ListingStatus.Archived ? "archived" : "draft"}`);

  const totalPages = Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE));

  // "Изначально" = остаток сейчас + всё, что реально списано активными
  // (не отклонёнными/отменёнными) заказами — см. тот же расчёт в FarmerProducts.tsx.
  const activeOrderIds = new Set(
    (orders ?? []).filter((o) => o.status !== OrderStatus.Rejected && o.status !== OrderStatus.Cancelled).map((o) => o.id),
  );
  const soldByListingId = new Map<number, number>();
  (orderItems ?? []).forEach((item) => {
    if (!activeOrderIds.has(item.orderId)) return;
    soldByListingId.set(item.productListingId, (soldByListingId.get(item.productListingId) ?? 0) + item.quantity);
  });

  const handleDelete = async () => {
    if (!deleting) return;
    try {
      await deleteAdminProductListing(deleting.id);
      toast.success(t("products.deleteSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("products.deleteError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  const renderCard = (product: AdminProductListingDto) => {
    const photo = photoByListingId.get(product.id);
    const sold = soldByListingId.get(product.id) ?? 0;
    return (
      <div key={product.id} className="flex flex-col gap-2.5 rounded-2xl border border-stone-100 bg-white p-3 shadow-sm transition hover:shadow-md dark:border-stone-800 dark:bg-stone-900">
        <div className="relative aspect-4/3 w-full overflow-hidden rounded-xl bg-grove-50 dark:bg-stone-800">
          {photo ? (
            <img src={photo} alt="" className="h-full w-full object-cover" loading="lazy" />
          ) : (
            <span className="flex h-full w-full items-center justify-center text-grove-300 dark:text-stone-600">
              <Package size={24} />
            </span>
          )}
          <span
            className={`absolute top-1.5 right-1.5 rounded-full px-2 py-0.5 text-xs font-semibold shadow-sm ${STATUS_CLASSES[product.status] ?? STATUS_CLASSES[ListingStatus.Draft]}`}
          >
            {statusLabel(product.status)}
          </span>
        </div>
        <div>
          <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{product.title}</p>
          <p className="truncate text-xs text-stone-400 dark:text-stone-500">{t("products.farmerLabel", { id: product.farmerProfileId })}</p>
        </div>
        <div className="flex items-center justify-between text-sm">
          <span className="font-semibold text-stone-800 dark:text-stone-100">
            {formatSomoni(product.retailPricePerKg)} {t("products.perKg")}
          </span>
          <div className="flex flex-col items-end">
            <span className="text-xs text-stone-500 dark:text-stone-400">
              {product.availableQuantity} {t("products.kg")}
            </span>
            {sold > 0 && (
              <span className="text-xs text-stone-400 dark:text-stone-500">
                {t("products.originallyOf", { count: product.availableQuantity + sold })}
              </span>
            )}
          </div>
        </div>
        <div className="truncate text-xs text-stone-400 dark:text-stone-500">
          {product.region}, {product.district}
        </div>
        <div className="flex justify-end border-t border-stone-50 pt-2 dark:border-stone-800/60">
          <button
            onClick={() => setDeleting(product)}
            aria-label={t("products.deleteAction")}
            className="flex h-7 w-7 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
          >
            <Trash2 size={14} />
          </button>
        </div>
      </div>
    );
  };

  return (
    <div className="overflow-hidden rounded-3xl border border-stone-100 bg-linear-to-b from-white to-stone-50/60 shadow-(--shadow-soft) dark:border-stone-800 dark:from-stone-900 dark:to-stone-900">
      <div className="flex items-center justify-end border-b border-stone-100 p-4 dark:border-stone-800">
        <ViewModeToggle value={viewMode} onChange={setViewMode} ns="admin" />
      </div>

      {viewMode === "cards" ? (
        <div className="grid grid-cols-2 gap-3 p-5 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">{data.items.map(renderCard)}</div>
      ) : (
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("products.columns.title")}</th>
              <th className="px-6 py-4 font-medium">{t("products.columns.farmer")}</th>
              <th className="px-6 py-4 font-medium">{t("products.columns.price")}</th>
              <th className="px-6 py-4 font-medium">{t("products.columns.quantity")}</th>
              <th className="px-6 py-4 font-medium">{t("products.columns.status")}</th>
              <th className="px-6 py-4 font-medium">{t("products.columns.region")}</th>
              <th className="px-6 py-4 font-medium text-right">{t("products.columns.actions")}</th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((product) => {
              const photo = photoByListingId.get(product.id);
              return (
                <tr
                  key={product.id}
                  className="border-b border-stone-50 transition-colors last:border-0 hover:bg-white dark:border-stone-800/60 dark:hover:bg-stone-800/40"
                >
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-3">
                      {photo ? (
                        <img src={photo} alt="" className="h-14 w-14 shrink-0 rounded-2xl object-cover shadow-sm" loading="lazy" />
                      ) : (
                        <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-grove-50 text-grove-400 dark:bg-stone-800 dark:text-stone-500">
                          <Package size={20} />
                        </span>
                      )}
                      <span className="max-w-48 truncate font-medium text-stone-800 dark:text-stone-100">{product.title}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{t("products.farmerLabel", { id: product.farmerProfileId })}</td>
                  <td className="px-6 py-4 font-semibold text-stone-800 dark:text-stone-100">
                    {formatSomoni(product.retailPricePerKg)} {t("products.perKg")}
                  </td>
                  <td className="px-6 py-4 text-stone-600 dark:text-stone-300">
                    {(() => {
                      const sold = soldByListingId.get(product.id) ?? 0;
                      return (
                        <div className="flex flex-col">
                          <span>
                            {product.availableQuantity} {t("products.kg")}
                          </span>
                          {sold > 0 && (
                            <span className="text-xs text-stone-400 dark:text-stone-500">
                              {t("products.originallyOf", { count: product.availableQuantity + sold })}
                            </span>
                          )}
                        </div>
                      );
                    })()}
                  </td>
                  <td className="px-6 py-4">
                    <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[product.status] ?? STATUS_CLASSES[ListingStatus.Draft]}`}>
                      {statusLabel(product.status)}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                    {product.region}, {product.district}
                  </td>
                  <td className="px-6 py-4">
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

      {totalPages > 1 && (
        <div className="border-t border-stone-100 p-4 dark:border-stone-800">
          <Pagination page={Math.min(page, totalPages)} totalPages={totalPages} onPageChange={setPage} />
        </div>
      )}

      <ConfirmDialog
        open={!!deleting}
        onClose={() => setDeleting(null)}
        onConfirm={handleDelete}
        title={t("products.deleteConfirmTitle")}
        description={deleting ? t("products.deleteConfirmDescription", { title: deleting.title }) : undefined}
        confirmLabel={t("products.deleteAction")}
      />
    </div>
  );
}
