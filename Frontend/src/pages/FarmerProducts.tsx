import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Loader2, Package, Pencil, Plus, Trash2 } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Modal } from "@/components/ui/Modal";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { Button } from "@/components/ui/Button";
import { Input, Select, Textarea } from "@/components/ui/Field";
import { ApiError, resolveMediaUrl } from "@/lib/api";
import { dateInputToIso, formatDate, formatSomoni } from "@/lib/utils";
import {
  ListingStatus,
  OrderStatus,
  createProductListing,
  deleteProductImage,
  deleteProductListing,
  updateProductListing,
  uploadProductImage,
  useFarmerOrders,
  useFarmerProducts,
  useFarmerProfile,
  useOrderItems,
  useProductCatalog,
  useProductImages,
  type CreateProductListingDto,
  type FarmerProfileDto,
  type ProductListingDto,
} from "@/data/farmer";

const MAX_IMAGES = 5;
const ALLOWED_IMAGE_TYPES = ["image/jpeg", "image/png", "image/webp"];
const MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024;

function ProductImagesField({ listingId }: { listingId: number }) {
  const { t } = useTranslation("farmer");
  const [refreshKey, setRefreshKey] = useState(0);
  const { images, loading } = useProductImages(listingId, refreshKey);
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  const onFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;

    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
      toast.error(t("products.form.photoInvalidType"));
      return;
    }
    if (file.size > MAX_IMAGE_SIZE_BYTES) {
      toast.error(t("products.form.photoTooLarge"));
      return;
    }

    setUploading(true);
    try {
      await uploadProductImage(listingId, !images?.length, file);
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("products.form.photoUploadError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setUploading(false);
    }
  };

  const onDelete = async (imageId: number) => {
    setDeletingId(imageId);
    try {
      await deleteProductImage(imageId);
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("products.form.photoDeleteError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <div className="flex flex-col gap-2">
      <span className="text-sm font-medium text-stone-700 dark:text-stone-300">{t("products.form.photos")}</span>
      <div className="flex flex-wrap gap-3">
        {images?.map((image) => (
          <div key={image.id} className="group relative h-20 w-20 shrink-0 overflow-hidden rounded-xl border border-stone-200 dark:border-stone-700">
            <img src={resolveMediaUrl(image.imageUrl)} alt="" className="h-full w-full object-cover" />
            <button
              type="button"
              onClick={() => onDelete(image.id)}
              disabled={deletingId === image.id}
              aria-label={t("products.form.photoDelete")}
              className="absolute inset-0 flex items-center justify-center bg-black/50 text-white opacity-0 transition group-hover:opacity-100 disabled:opacity-100"
            >
              {deletingId === image.id ? <Loader2 size={16} className="animate-spin" /> : <Trash2 size={16} />}
            </button>
          </div>
        ))}
        {(images?.length ?? 0) < MAX_IMAGES && (
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            disabled={uploading || loading}
            className="flex h-20 w-20 shrink-0 items-center justify-center rounded-xl border-2 border-dashed border-stone-300 text-stone-400 transition hover:border-grove-500 hover:text-grove-600 disabled:opacity-60 dark:border-stone-600 dark:text-stone-500 dark:hover:border-grove-500 dark:hover:text-grove-400"
          >
            {uploading ? <Loader2 size={18} className="animate-spin" /> : <Plus size={18} />}
          </button>
        )}
      </div>
      <input ref={inputRef} type="file" accept="image/jpeg,image/png,image/webp" className="hidden" onChange={onFileChange} />
      <p className="text-xs text-stone-400 dark:text-stone-500">{t("products.form.photosHint")}</p>
    </div>
  );
}

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

interface ProductFormValues {
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

function toFormValues(product: ProductListingDto): ProductFormValues {
  return {
    productId: String(product.productId),
    title: product.title,
    qualityGrade: product.qualityGrade,
    retailPricePerKg: String(product.retailPricePerKg),
    wholesalePricePerKg: product.wholesalePricePerKg != null ? String(product.wholesalePricePerKg) : "",
    wholesaleMinimumQuantity: product.wholesaleMinimumQuantity != null ? String(product.wholesaleMinimumQuantity) : "",
    availableQuantity: String(product.availableQuantity),
    minimumOrderQuantity: String(product.minimumOrderQuantity),
    harvestDate: product.harvestDate ? product.harvestDate.slice(0, 10) : "",
    description: product.description ?? "",
    status: String(product.status),
  };
}

function ProductFormModal({
  open,
  onClose,
  farmerProfile,
  editing,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  farmerProfile: FarmerProfileDto;
  editing: ProductListingDto | null;
  onSaved: () => void;
}) {
  const { t } = useTranslation("farmer");
  const { catalog, loading: catalogLoading } = useProductCatalog();
  const {
    register,
    handleSubmit,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ProductFormValues>({
    defaultValues: { qualityGrade: "Первый сорт", status: String(ListingStatus.Draft) },
  });

  useEffect(() => {
    if (open) reset(editing ? toFormValues(editing) : { qualityGrade: "Первый сорт", status: String(ListingStatus.Draft) });
  }, [open, editing, reset]);

  const retailPrice = watch("retailPricePerKg");
  const availableQuantity = watch("availableQuantity");
  const wholesalePrice = watch("wholesalePricePerKg");

  const onSubmit = async (values: ProductFormValues) => {
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
      harvestDate: dateInputToIso(values.harvestDate),
      qualityGrade: values.qualityGrade,
      region: farmerProfile.region,
      district: farmerProfile.district,
      address: farmerProfile.address,
      status: Number(values.status),
    };

    try {
      if (editing) {
        await updateProductListing(editing.id, dto);
        toast.success(t("products.updateSuccess"));
      } else {
        await createProductListing(dto);
        toast.success(t("products.createSuccess"));
      }
      onSaved();
      onClose();
    } catch (err) {
      toast.error(editing ? t("products.updateError") : t("products.createError"), {
        description: err instanceof Error ? err.message : undefined,
      });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-2xl">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{editing ? t("products.editModalTitle") : t("products.modalTitle")}</h2>
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
            {editing && <option value={ListingStatus.Archived}>{t("products.status.archived")}</option>}
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
            min="0"
            error={errors.availableQuantity?.message}
            {...register("availableQuantity", {
              required: t("products.form.required"),
              min: { value: editing ? 0 : 0.01, message: t("products.form.mustBePositive") },
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

        {editing ? (
          <ProductImagesField listingId={editing.id} />
        ) : (
          <p className="text-xs text-stone-400 dark:text-stone-500">{t("products.form.photosAfterCreate")}</p>
        )}

        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("products.form.cancel")}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {editing ? t("products.form.saveChanges") : t("products.form.submit")}
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
  const [editing, setEditing] = useState<ProductListingDto | null>(null);
  const [deleting, setDeleting] = useState<ProductListingDto | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const { products, loading: productsLoading, error: productsError } = useFarmerProducts(profile?.id ?? null, refreshKey);
  const { orders } = useFarmerOrders(profile?.id ?? null);
  const { orderItems } = useOrderItems();

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

  // "Изначально" = остаток сейчас + всё, что реально списано активными
  // (не отклонёнными/отменёнными) заказами — тот же расчёт, что и на бэкенде
  // при списании/возврате остатка (OrderItemService/OrderService), просто
  // восстановленный на фронте без нового поля в базе.
  const activeOrderIds = new Set(
    (orders ?? []).filter((o) => o.status !== OrderStatus.Rejected && o.status !== OrderStatus.Cancelled).map((o) => o.id),
  );
  const soldByListingId = new Map<number, number>();
  (orderItems ?? []).forEach((item) => {
    if (!activeOrderIds.has(item.orderId)) return;
    soldByListingId.set(item.productListingId, (soldByListingId.get(item.productListingId) ?? 0) + item.quantity);
  });

  const openCreate = () => {
    setEditing(null);
    setModalOpen(true);
  };
  const openEdit = (product: ProductListingDto) => {
    setEditing(product);
    setModalOpen(true);
  };

  const handleDelete = async () => {
    if (!deleting) return;
    try {
      await deleteProductListing(deleting.id);
      toast.success(t("products.deleteSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("products.deleteError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex justify-end">
        <Button leftIcon={<Plus size={16} />} onClick={openCreate}>
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
                  <th className="px-6 py-4 font-medium text-right">{t("products.columns.actions")}</th>
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
                      <StatusBadge status={product.status} label={statusLabel(product.status)} />
                    </td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                      {product.harvestDate ? formatDate(product.harvestDate) : "—"}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-1.5">
                        <button
                          onClick={() => openEdit(product)}
                          aria-label={t("products.editAction")}
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-grove-400"
                        >
                          <Pencil size={15} />
                        </button>
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

      <ProductFormModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        farmerProfile={profile}
        editing={editing}
        onSaved={() => setRefreshKey((k) => k + 1)}
      />

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
