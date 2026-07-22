import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Package, Plus } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Input, Select, Textarea } from "@/components/ui/Field";
import { formatDate, formatSomoni } from "@/lib/utils";
import {
  ListingStatus,
  createProductListing,
  useFarmerProducts,
  useFarmerProfile,
  useProductCatalog,
  type CreateProductListingDto,
  type FarmerProfileDto,
  type ProductListingDto,
} from "@/data/farmer";

const PAGE_SIZE = 8;

const STATUS_CLASSES: Record<number, string> = {
  [ListingStatus.Draft]: "bg-stone-100 text-stone-600 dark:bg-stone-800 dark:text-stone-300",
  [ListingStatus.Active]: "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300",
  [ListingStatus.OutOfStock]: "bg-harvest-100 text-harvest-800 dark:bg-harvest-900 dark:text-harvest-100",
  [ListingStatus.Archived]: "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500",
};

function StatusBadge({ status, label }: { status: number; label: string }) {
  return (
    <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_CLASSES[status] ?? STATUS_CLASSES[ListingStatus.Draft]}`}>
      {label}
    </span>
  );
}

interface AddProductFormValues {
  productId: string;
  title: string;
  qualityGrade: string;
  retailPricePerKg: string;
  wholesalePricePerKg: string;
  wholesaleMinimumQuantity: string;
  availableQuantity: string;
  minimumOrderQuantity: string;
  harvestDate: string;
  description: string;
  status: string;
}

function AddProductModal({
  open,
  onClose,
  farmerProfile,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  farmerProfile: FarmerProfileDto;
  onCreated: () => void;
}) {
  const { t } = useTranslation("farmer");
  const { catalog, loading: catalogLoading } = useProductCatalog();
  const {
    register,
    handleSubmit,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AddProductFormValues>({
    defaultValues: {
      qualityGrade: "Первый сорт",
      status: String(ListingStatus.Draft),
    },
  });

  const retailPrice = watch("retailPricePerKg");
  const availableQuantity = watch("availableQuantity");
  const wholesalePrice = watch("wholesalePricePerKg");

  const onSubmit = async (values: AddProductFormValues) => {
    const dto: CreateProductListingDto = {
      farmerProfileId: farmerProfile.id,
      productId: Number(values.productId),
      title: values.title,
      description: values.description || null,
      retailPricePerKg: Number(values.retailPricePerKg),
      wholesalePricePerKg: values.wholesalePricePerKg ? Number(values.wholesalePricePerKg) : null,
      wholesaleMinimumQuantity: values.wholesaleMinimumQuantity ? Number(values.wholesaleMinimumQuantity) : null,
      availableQuantity: Number(values.availableQuantity),
      minimumOrderQuantity: Number(values.minimumOrderQuantity),
      harvestDate: values.harvestDate || null,
      qualityGrade: values.qualityGrade,
      region: farmerProfile.region,
      district: farmerProfile.district,
      address: farmerProfile.address,
      status: Number(values.status),
    };

    try {
      await createProductListing(dto);
      toast.success(t("products.createSuccess"));
      reset();
      onCreated();
      onClose();
    } catch (err) {
      toast.error(t("products.createError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-2xl">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("products.modalTitle")}</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
        <Select
          label={t("products.form.product")}
          error={errors.productId?.message}
          disabled={catalogLoading}
          defaultValue=""
          {...register("productId", { required: t("products.form.required") })}
        >
          <option value="" disabled>
            {t("products.form.productPlaceholder")}
          </option>
          {catalog?.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </Select>

        <Input
          label={t("products.form.title")}
          placeholder={t("products.form.titlePlaceholder")}
          error={errors.title?.message}
          {...register("title", {
            required: t("products.form.required"),
            minLength: { value: 3, message: t("products.form.titleLength") },
            maxLength: { value: 150, message: t("products.form.titleLength") },
          })}
        />

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <Select label={t("products.form.qualityGrade")} error={errors.qualityGrade?.message} {...register("qualityGrade", { required: t("products.form.required") })}>
            <option value="Премиум">{t("products.form.qualityOptions.premium")}</option>
            <option value="Первый сорт">{t("products.form.qualityOptions.first")}</option>
            <option value="Стандарт">{t("products.form.qualityOptions.standard")}</option>
          </Select>
          <Select label={t("products.form.status")} {...register("status")}>
            <option value={ListingStatus.Draft}>{t("products.status.draft")}</option>
            <option value={ListingStatus.Active}>{t("products.status.active")}</option>
          </Select>
        </div>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <Input
            label={t("products.form.retailPrice")}
            type="number"
            step="0.01"
            min="0.01"
            error={errors.retailPricePerKg?.message}
            {...register("retailPricePerKg", {
              required: t("products.form.required"),
              min: { value: 0.01, message: t("products.form.mustBePositive") },
            })}
          />
          <Input
            label={t("products.form.availableQuantity")}
            type="number"
            step="0.01"
            min="0.01"
            error={errors.availableQuantity?.message}
            {...register("availableQuantity", {
              required: t("products.form.required"),
              min: { value: 0.01, message: t("products.form.mustBePositive") },
            })}
          />
        </div>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <Input
            label={t("products.form.minimumOrderQuantity")}
            type="number"
            step="0.01"
            min="0.01"
            error={errors.minimumOrderQuantity?.message}
            {...register("minimumOrderQuantity", {
              required: t("products.form.required"),
              min: { value: 0.01, message: t("products.form.mustBePositive") },
              validate: (v) => !availableQuantity || Number(v) <= Number(availableQuantity) || t("products.form.minOrderExceedsAvailable"),
            })}
          />
          <Input label={t("products.form.harvestDate")} type="date" {...register("harvestDate")} />
        </div>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <Input
            label={t("products.form.wholesalePrice")}
            type="number"
            step="0.01"
            min="0.01"
            hint={t("products.form.optional")}
            error={errors.wholesalePricePerKg?.message}
            {...register("wholesalePricePerKg", {
              validate: (v) => !v || !retailPrice || Number(v) <= Number(retailPrice) || t("products.form.wholesaleExceedsRetail"),
            })}
          />
          <Input
            label={t("products.form.wholesaleMinQuantity")}
            type="number"
            step="0.01"
            min="0.01"
            hint={t("products.form.optional")}
            error={errors.wholesaleMinimumQuantity?.message}
            {...register("wholesaleMinimumQuantity", {
              validate: (v) => !wholesalePrice || !!v || t("products.form.wholesaleMinRequired"),
            })}
          />
        </div>

        <Textarea label={t("products.form.description")} hint={t("products.form.optional")} {...register("description", { maxLength: { value: 2000, message: t("products.form.descriptionLength") } })} />

        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("products.form.cancel")}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {t("products.form.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export function FarmerProducts() {
  const { t } = useTranslation("farmer");
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const { products, loading: productsLoading, error: productsError } = useFarmerProducts(profile?.id ?? null, refreshKey);

  if (profileLoading || (profile && productsLoading)) return <PageLoader />;

  if (profileError || productsError || !profile || !products) {
    return (
      <EmptyState
        icon={<Package size={26} />}
        title={t("products.errorTitle")}
        description={profileError ?? productsError ?? t("products.errorDescription")}
      />
    );
  }

  const statusLabel = (status: number) =>
    t(`products.status.${status === ListingStatus.Active ? "active" : status === ListingStatus.OutOfStock ? "outOfStock" : status === ListingStatus.Archived ? "archived" : "draft"}`);

  const totalPages = Math.max(1, Math.ceil(products.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: ProductListingDto[] = products.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  return (
    <div className="flex flex-col gap-5">
      <div className="flex justify-end">
        <Button leftIcon={<Plus size={16} />} onClick={() => setModalOpen(true)}>
          {t("products.addButton")}
        </Button>
      </div>

      {products.length === 0 ? (
        <EmptyState
          icon={<Package size={26} />}
          title={t("products.emptyTitle")}
          description={t("products.emptyDescription")}
        />
      ) : (
        <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="px-6 py-4 font-medium">{t("products.columns.title")}</th>
                  <th className="px-6 py-4 font-medium">{t("products.columns.price")}</th>
                  <th className="px-6 py-4 font-medium">{t("products.columns.quantity")}</th>
                  <th className="px-6 py-4 font-medium">{t("products.columns.status")}</th>
                  <th className="px-6 py-4 font-medium">{t("products.columns.harvestDate")}</th>
                </tr>
              </thead>
              <tbody>
                {pageItems.map((product) => (
                  <tr key={product.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="max-w-64 truncate px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{product.title}</td>
                    <td className="px-6 py-4 text-stone-600 dark:text-stone-300">
                      {formatSomoni(product.retailPricePerKg)} {t("products.perKg")}
                    </td>
                    <td className="px-6 py-4 text-stone-600 dark:text-stone-300">
                      {product.availableQuantity} {t("products.kg")}
                    </td>
                    <td className="px-6 py-4">
                      <StatusBadge status={product.status} label={statusLabel(product.status)} />
                    </td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                      {product.harvestDate ? formatDate(product.harvestDate) : "—"}
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
      )}

      <AddProductModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        farmerProfile={profile}
        onCreated={() => setRefreshKey((k) => k + 1)}
      />
    </div>
  );
}
