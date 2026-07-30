import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Pencil, Plus, Settings, Trash2 } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Modal } from "@/components/ui/Modal";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { Button } from "@/components/ui/Button";
import { Input, Textarea } from "@/components/ui/Field";
import { useAuth } from "@/context/AuthContext";
import { formatDate } from "@/lib/utils";
import {
  createAppSetting,
  deleteAppSetting,
  updateSettingValue,
  useAdminSettings,
  type AdminSettingDto,
} from "@/data/adminEntities";

const PAGE_SIZE = 10;

interface SettingFormValues {
  key: string;
  value: string;
  description: string;
}

function SettingFormModal({
  open,
  onClose,
  editing,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  editing: AdminSettingDto | null;
  onSaved: () => void;
}) {
  const { t } = useTranslation("admin");
  const { user } = useAuth();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<SettingFormValues>();

  useEffect(() => {
    if (open) reset(editing ? { key: editing.key, value: editing.value, description: editing.description ?? "" } : { key: "", value: "", description: "" });
  }, [open, editing, reset]);

  const onSubmit = async (values: SettingFormValues) => {
    try {
      if (editing) {
        await updateSettingValue(editing, values.value, user?.userId ?? 0);
        toast.success(t("settings.updateSuccess"));
      } else {
        await createAppSetting({ key: values.key, value: values.value, description: values.description || null }, user?.userId ?? 0);
        toast.success(t("settings.createSuccess"));
      }
      onSaved();
      onClose();
    } catch (err) {
      toast.error(editing ? t("settings.updateError") : t("settings.createError"), {
        description: err instanceof Error ? err.message : undefined,
      });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-md">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{editing ? t("settings.editModalTitle") : t("settings.createModalTitle")}</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
        {editing ? (
          <div>
            <p className="text-xs text-stone-400 dark:text-stone-500">{t("settings.columns.key")}</p>
            <p className="mt-1 font-mono text-sm font-medium text-stone-800 dark:text-stone-100">{editing.key}</p>
          </div>
        ) : (
          <Input
            label={t("settings.form.key")}
            hint={t("settings.form.keyHint")}
            error={errors.key?.message}
            {...register("key", { required: t("settings.form.required") })}
          />
        )}
        <Textarea
          label={t("settings.columns.value")}
          error={errors.value?.message}
          {...register("value", { required: t("settings.form.required") })}
        />
        <Textarea label={t("settings.form.description")} hint={t("settings.form.optional")} {...register("description")} />
        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("settings.form.cancel")}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {editing ? t("settings.form.saveChanges") : t("settings.form.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export function AdminSettings() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminSettingDto | null>(null);
  const [deleting, setDeleting] = useState<AdminSettingDto | null>(null);
  const { settings, loading, error } = useAdminSettings(refreshKey);

  if (loading) return <PageLoader />;

  if (error || !settings) {
    return <EmptyState icon={<Settings size={26} />} title={t("settings.errorTitle")} description={error ?? t("settings.errorDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(settings.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminSettingDto[] = settings.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const openCreate = () => {
    setEditing(null);
    setModalOpen(true);
  };
  const openEdit = (setting: AdminSettingDto) => {
    setEditing(setting);
    setModalOpen(true);
  };

  const handleDelete = async () => {
    if (!deleting) return;
    try {
      await deleteAppSetting(deleting.id);
      toast.success(t("settings.deleteSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("settings.deleteError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex justify-end">
        <Button leftIcon={<Plus size={16} />} onClick={openCreate}>
          {t("settings.addButton")}
        </Button>
      </div>

      {settings.length === 0 ? (
        <EmptyState icon={<Settings size={26} />} title={t("settings.emptyTitle")} description={t("settings.emptyDescription")} />
      ) : (
        <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="px-6 py-4 font-medium">{t("settings.columns.key")}</th>
                  <th className="px-6 py-4 font-medium">{t("settings.columns.value")}</th>
                  <th className="px-6 py-4 font-medium">{t("settings.columns.description")}</th>
                  <th className="px-6 py-4 font-medium">{t("settings.columns.updatedAt")}</th>
                  <th className="px-6 py-4 font-medium text-right">{t("settings.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {pageItems.map((setting) => (
                  <tr key={setting.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="px-6 py-4 font-mono text-xs font-medium text-stone-800 dark:text-stone-100">{setting.key}</td>
                    <td className="max-w-64 truncate px-6 py-4 text-stone-600 dark:text-stone-300">{setting.value}</td>
                    <td className="max-w-80 truncate px-6 py-4 text-stone-500 dark:text-stone-400">{setting.description ?? "—"}</td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(setting.updatedAt)}</td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-1.5">
                        <button
                          onClick={() => openEdit(setting)}
                          aria-label={t("settings.editAction")}
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-grove-400"
                        >
                          <Pencil size={15} />
                        </button>
                        <button
                          onClick={() => setDeleting(setting)}
                          aria-label={t("settings.deleteAction")}
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

      <SettingFormModal open={modalOpen} onClose={() => setModalOpen(false)} editing={editing} onSaved={() => setRefreshKey((k) => k + 1)} />
      <ConfirmDialog
        open={!!deleting}
        onClose={() => setDeleting(null)}
        onConfirm={handleDelete}
        title={t("settings.deleteConfirmTitle")}
        description={t("settings.deleteConfirmDescription")}
        confirmLabel={t("settings.deleteAction")}
      />
    </div>
  );
}
