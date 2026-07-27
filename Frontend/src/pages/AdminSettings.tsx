import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Pencil, Settings } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Textarea } from "@/components/ui/Field";
import { useAuth } from "@/context/AuthContext";
import { formatDate } from "@/lib/utils";
import { updateSettingValue, useAdminSettings, type AdminSettingDto } from "@/data/adminEntities";

const PAGE_SIZE = 10;

interface SettingFormValues {
  value: string;
}

function EditSettingModal({
  setting,
  onClose,
  onSaved,
}: {
  setting: AdminSettingDto | null;
  onClose: () => void;
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
    if (setting) reset({ value: setting.value });
  }, [setting, reset]);

  const onSubmit = async (values: SettingFormValues) => {
    if (!setting) return;
    try {
      await updateSettingValue(setting, values.value, user?.userId ?? 0);
      toast.success(t("settings.updateSuccess"));
      onSaved();
      onClose();
    } catch (err) {
      toast.error(t("settings.updateError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  return (
    <Modal open={!!setting} onClose={onClose} className="max-w-md">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("settings.editModalTitle")}</h2>
      {setting && (
        <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
          <div>
            <p className="text-xs text-stone-400 dark:text-stone-500">{t("settings.columns.key")}</p>
            <p className="mt-1 font-mono text-sm font-medium text-stone-800 dark:text-stone-100">{setting.key}</p>
          </div>
          {setting.description && <p className="text-xs text-stone-400 dark:text-stone-500">{setting.description}</p>}
          <Textarea
            label={t("settings.columns.value")}
            error={errors.value?.message}
            {...register("value", { required: t("settings.form.required") })}
          />
          <div className="mt-2 flex justify-end gap-3">
            <Button type="button" variant="outline" onClick={onClose}>
              {t("settings.form.cancel")}
            </Button>
            <Button type="submit" loading={isSubmitting}>
              {t("settings.form.saveChanges")}
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}

export function AdminSettings() {
  const { t } = useTranslation("admin");
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);
  const [editing, setEditing] = useState<AdminSettingDto | null>(null);
  const { settings, loading, error } = useAdminSettings(refreshKey);

  if (loading) return <PageLoader />;

  if (error || !settings) {
    return <EmptyState icon={<Settings size={26} />} title={t("settings.errorTitle")} description={error ?? t("settings.errorDescription")} />;
  }

  if (settings.length === 0) {
    return <EmptyState icon={<Settings size={26} />} title={t("settings.emptyTitle")} description={t("settings.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(settings.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminSettingDto[] = settings.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  return (
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
                  <div className="flex items-center justify-end">
                    <button
                      onClick={() => setEditing(setting)}
                      aria-label={t("settings.editAction")}
                      className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-grove-400"
                    >
                      <Pencil size={15} />
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

      <EditSettingModal setting={editing} onClose={() => setEditing(null)} onSaved={() => setRefreshKey((k) => k + 1)} />
    </div>
  );
}
