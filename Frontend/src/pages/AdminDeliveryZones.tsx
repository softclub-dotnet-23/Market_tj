import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { MapPinned, Pencil, Plus, Trash2 } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Modal } from "@/components/ui/Modal";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { Button } from "@/components/ui/Button";
import { Input, Select } from "@/components/ui/Field";
import { formatSomoni } from "@/lib/utils";
import {
  createDeliveryZone,
  deleteDeliveryZone,
  updateDeliveryZone,
  useAdminDeliveryZones,
  type AdminDeliveryZoneDto,
  type DeliveryZoneFormDto,
} from "@/data/adminEntities";

const PAGE_SIZE = 10;

interface ZoneFormValues {
  region: string;
  district: string;
  basePrice: string;
  pricePerKm: string;
  isActive: string;
}

function toFormValues(z: AdminDeliveryZoneDto): ZoneFormValues {
  return {
    region: z.region,
    district: z.district,
    basePrice: String(z.basePrice),
    pricePerKm: z.pricePerKm != null ? String(z.pricePerKm) : "",
    isActive: String(z.isActive),
  };
}

function ZoneFormModal({
  open,
  onClose,
  editing,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  editing: AdminDeliveryZoneDto | null;
  onSaved: () => void;
}) {
  const { t } = useTranslation("admin");
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ZoneFormValues>({ defaultValues: { isActive: "true" } });

  useEffect(() => {
    if (open) reset(editing ? toFormValues(editing) : { isActive: "true" });
  }, [open, editing, reset]);

  const onSubmit = async (values: ZoneFormValues) => {
    const dto: DeliveryZoneFormDto = {
      region: values.region,
      district: values.district,
      basePrice: Number(values.basePrice),
      pricePerKm: values.pricePerKm ? Number(values.pricePerKm) : null,
      isActive: values.isActive === "true",
    };
    try {
      if (editing) {
        await updateDeliveryZone(editing.id, dto);
        toast.success(t("deliveryZones.updateSuccess"));
      } else {
        await createDeliveryZone(dto);
        toast.success(t("deliveryZones.createSuccess"));
      }
      onSaved();
      onClose();
    } catch (err) {
      toast.error(editing ? t("deliveryZones.updateError") : t("deliveryZones.createError"), {
        description: err instanceof Error ? err.message : undefined,
      });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-md">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{editing ? t("deliveryZones.editModalTitle") : t("deliveryZones.createModalTitle")}</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <Input label={t("deliveryZones.form.region")} error={errors.region?.message} {...register("region", { required: t("deliveryZones.form.required") })} />
          <Input label={t("deliveryZones.form.district")} error={errors.district?.message} {...register("district", { required: t("deliveryZones.form.required") })} />
        </div>
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <Input
            label={t("deliveryZones.form.basePrice")}
            type="number"
            step="0.01"
            min="0"
            error={errors.basePrice?.message}
            {...register("basePrice", { required: t("deliveryZones.form.required"), min: { value: 0, message: t("deliveryZones.form.nonNegative") } })}
          />
          <Input
            label={t("deliveryZones.form.pricePerKm")}
            type="number"
            step="0.01"
            min="0"
            hint={t("deliveryZones.form.optional")}
            error={errors.pricePerKm?.message}
            {...register("pricePerKm", { min: { value: 0, message: t("deliveryZones.form.nonNegative") } })}
          />
        </div>
        <Select label={t("deliveryZones.form.status")} {...register("isActive")}>
          <option value="true">{t("deliveryZones.status.active")}</option>
          <option value="false">{t("deliveryZones.status.inactive")}</option>
        </Select>

        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("deliveryZones.form.cancel")}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {editing ? t("deliveryZones.form.saveChanges") : t("deliveryZones.form.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export function AdminDeliveryZones() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminDeliveryZoneDto | null>(null);
  const [deleting, setDeleting] = useState<AdminDeliveryZoneDto | null>(null);
  const { zones, loading, error } = useAdminDeliveryZones(refreshKey);

  if (loading) return <PageLoader />;

  if (error || !zones) {
    return <EmptyState icon={<MapPinned size={26} />} title={t("deliveryZones.errorTitle")} description={error ?? t("deliveryZones.errorDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(zones.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminDeliveryZoneDto[] = zones.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const openCreate = () => {
    setEditing(null);
    setModalOpen(true);
  };
  const openEdit = (z: AdminDeliveryZoneDto) => {
    setEditing(z);
    setModalOpen(true);
  };

  const handleDelete = async () => {
    if (!deleting) return;
    try {
      await deleteDeliveryZone(deleting.id);
      toast.success(t("deliveryZones.deleteSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("deliveryZones.deleteError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex justify-end">
        <Button leftIcon={<Plus size={16} />} onClick={openCreate}>
          {t("deliveryZones.addButton")}
        </Button>
      </div>

      {zones.length === 0 ? (
        <EmptyState icon={<MapPinned size={26} />} title={t("deliveryZones.emptyTitle")} description={t("deliveryZones.emptyDescription")} />
      ) : (
        <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="px-6 py-4 font-medium">{t("deliveryZones.columns.region")}</th>
                  <th className="px-6 py-4 font-medium">{t("deliveryZones.columns.basePrice")}</th>
                  <th className="px-6 py-4 font-medium">{t("deliveryZones.columns.pricePerKm")}</th>
                  <th className="px-6 py-4 font-medium">{t("deliveryZones.columns.status")}</th>
                  <th className="px-6 py-4 font-medium text-right">{t("deliveryZones.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {pageItems.map((zone) => (
                  <tr key={zone.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">
                      {zone.region}, {zone.district}
                    </td>
                    <td className="px-6 py-4 text-stone-600 dark:text-stone-300">
                      {formatSomoni(zone.basePrice)} {t("common.somoni")}
                    </td>
                    <td className="px-6 py-4 text-stone-600 dark:text-stone-300">
                      {zone.pricePerKm != null ? `${formatSomoni(zone.pricePerKm)} ${t("common.somoni")}/км` : "—"}
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                          zone.isActive
                            ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
                            : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
                        }`}
                      >
                        {t(zone.isActive ? "deliveryZones.status.active" : "deliveryZones.status.inactive")}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-1.5">
                        <button
                          onClick={() => openEdit(zone)}
                          aria-label={t("deliveryZones.editAction")}
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-grove-400"
                        >
                          <Pencil size={15} />
                        </button>
                        <button
                          onClick={() => setDeleting(zone)}
                          aria-label={t("deliveryZones.deleteAction")}
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

      <ZoneFormModal open={modalOpen} onClose={() => setModalOpen(false)} editing={editing} onSaved={() => setRefreshKey((k) => k + 1)} />
      <ConfirmDialog
        open={!!deleting}
        onClose={() => setDeleting(null)}
        onConfirm={handleDelete}
        title={t("deliveryZones.deleteConfirmTitle")}
        description={t("deliveryZones.deleteConfirmDescription")}
        confirmLabel={t("deliveryZones.deleteAction")}
      />
    </div>
  );
}
