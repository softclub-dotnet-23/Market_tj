import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Package } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { formatSomoni } from "@/lib/utils";
import { ListingStatus, useAdminProducts } from "@/data/adminEntities";

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
  const { data, loading, error } = useAdminProducts(page, PAGE_SIZE);

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

  return (
    <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
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
            </tr>
          </thead>
          <tbody>
            {data.items.map((product) => (
              <tr key={product.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                <td className="max-w-64 truncate px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{product.title}</td>
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{t("products.farmerLabel", { id: product.farmerProfileId })}</td>
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">
                  {formatSomoni(product.retailPricePerKg)} {t("products.perKg")}
                </td>
                <td className="px-6 py-4 text-stone-600 dark:text-stone-300">
                  {product.availableQuantity} {t("products.kg")}
                </td>
                <td className="px-6 py-4">
                  <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[product.status] ?? STATUS_CLASSES[ListingStatus.Draft]}`}>
                    {statusLabel(product.status)}
                  </span>
                </td>
                <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                  {product.region}, {product.district}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="border-t border-stone-100 p-4 dark:border-stone-800">
          <Pagination page={Math.min(page, totalPages)} totalPages={totalPages} onPageChange={setPage} />
        </div>
      )}
    </div>
  );
}
