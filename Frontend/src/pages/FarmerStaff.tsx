import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Plus, Trash2, Users } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Modal } from "@/components/ui/Modal";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { Button } from "@/components/ui/Button";
import { Input, Checkbox } from "@/components/ui/Field";
import { ApiError } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import {
  StaffPermissions,
  addFarmerStaffByEmail,
  deleteFarmerStaffMember,
  useFarmerProfile,
  useFarmerStaff,
  type FarmerStaffMemberDto,
} from "@/data/farmer";

interface AddStaffFormValues {
  email: string;
  manageProducts: boolean;
  manageStock: boolean;
}

function AddStaffModal({ open, onClose, farmerProfileId, onSaved }: { open: boolean; onClose: () => void; farmerProfileId: number; onSaved: () => void }) {
  const { t } = useTranslation("farmer");
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AddStaffFormValues>({ defaultValues: { email: "", manageProducts: false, manageStock: false } });

  const onSubmit = async (values: AddStaffFormValues) => {
    const permissions =
      (values.manageProducts ? StaffPermissions.ManageProducts : 0) | (values.manageStock ? StaffPermissions.ManageStock : 0);
    try {
      await addFarmerStaffByEmail(farmerProfileId, values.email, permissions);
      toast.success(t("staff.addSuccess"));
      reset();
      onSaved();
      onClose();
    } catch (err) {
      toast.error(t("staff.addError"), { description: err instanceof ApiError ? err.message : undefined });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-md">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("staff.addModalTitle")}</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
        <Input
          label={t("staff.form.email")}
          type="email"
          hint={t("staff.form.emailHint")}
          error={errors.email?.message}
          {...register("email", { required: t("staff.form.required") })}
        />
        <div className="flex flex-col gap-2.5">
          <span className="text-sm font-medium text-stone-700 dark:text-stone-300">{t("staff.form.permissions")}</span>
          <Checkbox label={t("staff.permissions.manageProducts")} {...register("manageProducts")} />
          <Checkbox label={t("staff.permissions.manageStock")} {...register("manageStock")} />
        </div>
        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("staff.form.cancel")}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {t("staff.form.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export function FarmerStaff() {
  const { t } = useTranslation("farmer");
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const [refreshKey, setRefreshKey] = useState(0);
  const { staff, loading: staffLoading, error: staffError } = useFarmerStaff(profile?.id ?? null, refreshKey);
  const [modalOpen, setModalOpen] = useState(false);
  const [deleting, setDeleting] = useState<FarmerStaffMemberDto | null>(null);

  if (profileLoading || (profile && staffLoading)) return <PageLoader />;

  if (profileError || staffError || !profile || !staff) {
    return (
      <EmptyState
        icon={<Users size={26} />}
        title={t("staff.errorTitle")}
        description={profileError ?? staffError ?? t("staff.errorDescription")}
      />
    );
  }

  const permissionLabels = (permissions: number) => {
    const labels: string[] = [];
    if (permissions & StaffPermissions.ManageProducts) labels.push(t("staff.permissions.manageProducts"));
    if (permissions & StaffPermissions.ManageStock) labels.push(t("staff.permissions.manageStock"));
    return labels.length > 0 ? labels : [t("staff.permissions.none")];
  };

  const handleDelete = async () => {
    if (!deleting) return;
    try {
      await deleteFarmerStaffMember(deleting.id);
      toast.success(t("staff.deleteSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("staff.deleteError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex justify-end">
        <Button leftIcon={<Plus size={16} />} onClick={() => setModalOpen(true)}>
          {t("staff.addButton")}
        </Button>
      </div>

      {staff.length === 0 ? (
        <EmptyState icon={<Users size={26} />} title={t("staff.emptyTitle")} description={t("staff.emptyDescription")} />
      ) : (
        <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
          <div className="flex flex-col gap-3 p-4 lg:hidden">
            {staff.map((member) => (
              <div key={member.id} className="flex flex-col gap-2 rounded-2xl border border-stone-100 p-4 dark:border-stone-800">
                <div className="flex items-start justify-between gap-3">
                  <p className="font-medium text-stone-800 dark:text-stone-100">{t("staff.userLabel", { id: member.userId })}</p>
                  <span
                    className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${
                      member.isActive
                        ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
                        : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
                    }`}
                  >
                    {t(member.isActive ? "staff.status.active" : "staff.status.inactive")}
                  </span>
                </div>
                <p className="text-sm text-stone-600 dark:text-stone-300">{permissionLabels(member.permissions).join(", ")}</p>
                <div className="flex items-center justify-between border-t border-stone-50 pt-2 dark:border-stone-800/60">
                  <p className="text-xs text-stone-400 dark:text-stone-500">{formatDate(member.createdAt)}</p>
                  <button
                    onClick={() => setDeleting(member)}
                    aria-label={t("staff.deleteAction")}
                    className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="hidden overflow-x-auto lg:block">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="px-6 py-4 font-medium">{t("staff.columns.user")}</th>
                  <th className="px-6 py-4 font-medium">{t("staff.columns.permissions")}</th>
                  <th className="px-6 py-4 font-medium">{t("staff.columns.status")}</th>
                  <th className="px-6 py-4 font-medium">{t("staff.columns.createdAt")}</th>
                  <th className="px-6 py-4 font-medium text-right">{t("staff.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {staff.map((member) => (
                  <tr key={member.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{t("staff.userLabel", { id: member.userId })}</td>
                    <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{permissionLabels(member.permissions).join(", ")}</td>
                    <td className="px-6 py-4">
                      <span
                        className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                          member.isActive
                            ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
                            : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
                        }`}
                      >
                        {t(member.isActive ? "staff.status.active" : "staff.status.inactive")}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(member.createdAt)}</td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end">
                        <button
                          onClick={() => setDeleting(member)}
                          aria-label={t("staff.deleteAction")}
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
        </div>
      )}

      <AddStaffModal open={modalOpen} onClose={() => setModalOpen(false)} farmerProfileId={profile.id} onSaved={() => setRefreshKey((k) => k + 1)} />
      <ConfirmDialog
        open={!!deleting}
        onClose={() => setDeleting(null)}
        onConfirm={handleDelete}
        title={t("staff.deleteConfirmTitle")}
        description={t("staff.deleteConfirmDescription")}
        confirmLabel={t("staff.deleteAction")}
      />
    </div>
  );
}
