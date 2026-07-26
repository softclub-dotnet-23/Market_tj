import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { CreditCard, Pencil, Plus, Trash2 } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Modal } from "@/components/ui/Modal";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Field";
import { dateInputToIso, formatDate } from "@/lib/utils";
import {
  createCommission,
  deleteCommission,
  updateCommission,
  useAdminCommissions,
  type AdminCommissionDto,
  type CommissionFormDto,
} from "@/data/adminEntities";

const PAGE_SIZE = 10;

function isCurrentlyActive(commission: AdminCommissionDto) {
  return !commission.effectiveTo || new Date(commission.effectiveTo).getTime() > Date.now();
}

interface CommissionFormValues {
  categoryId: string;
  percentage: string;
  effectiveFrom: string;
  effectiveTo: string;
}

function toFormValues(c: AdminCommissionDto): CommissionFormValues {
  return {
    categoryId: c.categoryId != null ? String(c.categoryId) : "",
    percentage: String(c.percentage),
    effectiveFrom: c.effectiveFrom.slice(0, 10),
    effectiveTo: c.effectiveTo ? c.effectiveTo.slice(0, 10) : "",
  };
}

function CommissionFormModal({
  open,
  onClose,
  editing,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  editing: AdminCommissionDto | null;
  onSaved: () => void;
}) {
  const { t } = useTranslation("admin");
  const {
    register,
    handleSubmit,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CommissionFormValues>({ defaultValues: { effectiveFrom: new Date().toISOString().slice(0, 10) } });

  useEffect(() => {
    if (open) reset(editing ? toFormValues(editing) : { effectiveFrom: new Date().toISOString().slice(0, 10) });
  }, [open, editing, reset]);

  const effectiveFrom = watch("effectiveFrom");

  const onSubmit = async (values: CommissionFormValues) => {
    const dto: CommissionFormDto = {
      categoryId: values.categoryId ? Number(values.categoryId) : null,
      percentage: Number(values.percentage),
      effectiveFrom: dateInputToIso(values.effectiveFrom)!,
      effectiveTo: dateInputToIso(values.effectiveTo),
    };
    try {
      if (editing) {
        await updateCommission(editing.id, dto);
        toast.success(t("commissions.updateSuccess"));
      } else {
        await createCommission(dto);
        toast.success(t("commissions.createSuccess"));
      }
      onSaved();
      onClose();
    } catch (err) {
      toast.error(editing ? t("commissions.updateError") : t("commissions.createError"), {
        description: err instanceof Error ? err.message : undefined,
      });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-md">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{editing ? t("commissions.editModalTitle") : t("commissions.createModalTitle")}</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
        <Input
          label={t("commissions.form.categoryId")}
          type="number"
          min="1"
          hint={t("commissions.form.categoryIdHint")}
          {...register("categoryId")}
        />
        <Input
          label={t("commissions.form.percentage")}
          type="number"
          step="0.01"
          min="0"
          max="100"
          error={errors.percentage?.message}
          {...register("percentage", {
            required: t("commissions.form.required"),
            min: { value: 0, message: t("commissions.form.percentageRange") },
            max: { value: 100, message: t("commissions.form.percentageRange") },
          })}
        />
        <Input
          label={t("commissions.form.effectiveFrom")}
          type="date"
          error={errors.effectiveFrom?.message}
          {...register("effectiveFrom", { required: t("commissions.form.required") })}
        />
        <Input
          label={t("commissions.form.effectiveTo")}
          type="date"
          hint={t("commissions.form.optional")}
          error={errors.effectiveTo?.message}
          {...register("effectiveTo", {
            validate: (v) => !v || !effectiveFrom || v > effectiveFrom || t("commissions.form.effectiveToBeforeFrom"),
          })}
        />

        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("commissions.form.cancel")}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {editing ? t("commissions.form.saveChanges") : t("commissions.form.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export function AdminCommissions() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminCommissionDto | null>(null);
  const [deleting, setDeleting] = useState<AdminCommissionDto | null>(null);
  const { commissions, loading, error } = useAdminCommissions(refreshKey);

  if (loading) return <PageLoader />;

  if (error || !commissions) {
    return <EmptyState icon={<CreditCard size={26} />} title={t("commissions.errorTitle")} description={error ?? t("commissions.errorDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(commissions.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminCommissionDto[] = commissions.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const openCreate = () => {
    setEditing(null);
    setModalOpen(true);
  };
  const openEdit = (c: AdminCommissionDto) => {
    setEditing(c);
    setModalOpen(true);
  };

  const handleDelete = async () => {
    if (!deleting) return;
    try {
      await deleteCommission(deleting.id);
      toast.success(t("commissions.deleteSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("commissions.deleteError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex justify-end">
        <Button leftIcon={<Plus size={16} />} onClick={openCreate}>
          {t("commissions.addButton")}
        </Button>
      </div>

      {commissions.length === 0 ? (
        <EmptyState icon={<CreditCard size={26} />} title={t("commissions.emptyTitle")} description={t("commissions.emptyDescription")} />
      ) : (
        <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="px-6 py-4 font-medium">{t("commissions.columns.category")}</th>
                  <th className="px-6 py-4 font-medium">{t("commissions.columns.percentage")}</th>
                  <th className="px-6 py-4 font-medium">{t("commissions.columns.effectiveFrom")}</th>
                  <th className="px-6 py-4 font-medium">{t("commissions.columns.effectiveTo")}</th>
                  <th className="px-6 py-4 font-medium">{t("commissions.columns.status")}</th>
                  <th className="px-6 py-4 font-medium text-right">{t("commissions.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {pageItems.map((commission) => (
                  <tr key={commission.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">
                      {commission.categoryId === null ? t("commissions.allCategories") : t("commissions.categoryLabel", { id: commission.categoryId })}
                    </td>
                    <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{commission.percentage}%</td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(commission.effectiveFrom)}</td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                      {commission.effectiveTo ? formatDate(commission.effectiveTo) : "—"}
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                          isCurrentlyActive(commission)
                            ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
                            : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
                        }`}
                      >
                        {t(isCurrentlyActive(commission) ? "commissions.status.active" : "commissions.status.expired")}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-1.5">
                        <button
                          onClick={() => openEdit(commission)}
                          aria-label={t("commissions.editAction")}
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-grove-400"
                        >
                          <Pencil size={15} />
                        </button>
                        <button
                          onClick={() => setDeleting(commission)}
                          aria-label={t("commissions.deleteAction")}
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

      <CommissionFormModal open={modalOpen} onClose={() => setModalOpen(false)} editing={editing} onSaved={() => setRefreshKey((k) => k + 1)} />
      <ConfirmDialog
        open={!!deleting}
        onClose={() => setDeleting(null)}
        onConfirm={handleDelete}
        title={t("commissions.deleteConfirmTitle")}
        description={t("commissions.deleteConfirmDescription")}
        confirmLabel={t("commissions.deleteAction")}
      />
    </div>
  );
}
