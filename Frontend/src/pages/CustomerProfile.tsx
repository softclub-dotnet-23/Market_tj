import { useState } from "react";
import type { ReactNode } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Mail, MapPin, Pencil, Phone, User as UserIcon } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Field";
import { formatDate } from "@/lib/utils";
import { useAuth } from "@/context/AuthContext";
import {
  CustomerType,
  updateCustomerProfile,
  useCustomerProfile,
  type CustomerProfileDto,
  type CustomerProfileEditableFields,
} from "@/data/customer";

function Row({ icon, label, value }: { icon: ReactNode; label: string; value: ReactNode }) {
  return (
    <div className="flex items-start gap-3 py-3.5">
      <span className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-300">
        {icon}
      </span>
      <div className="min-w-0">
        <p className="text-xs text-stone-400 dark:text-stone-500">{label}</p>
        <p className="text-sm font-medium text-stone-800 dark:text-stone-100">{value}</p>
      </div>
    </div>
  );
}

function EditProfileModal({
  open,
  onClose,
  profile,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  profile: CustomerProfileDto;
  onSaved: () => void;
}) {
  const { t } = useTranslation("customer");
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CustomerProfileEditableFields>({
    defaultValues: {
      region: profile.region,
      district: profile.district,
      defaultAddress: profile.defaultAddress ?? "",
    },
  });

  const onSubmit = async (values: CustomerProfileEditableFields) => {
    try {
      await updateCustomerProfile(profile, { ...values, defaultAddress: values.defaultAddress || null });
      toast.success(t("profile.updateSuccess"));
      onSaved();
      onClose();
    } catch (err) {
      toast.error(t("profile.updateError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-lg">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("profile.editModalTitle")}</h2>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <Input label={t("profile.region")} error={errors.region?.message} {...register("region", { required: t("profile.form.required") })} />
          <Input
            label={t("profile.form.district")}
            error={errors.district?.message}
            {...register("district", { required: t("profile.form.required") })}
          />
        </div>
        <Input label={t("profile.defaultAddress")} hint={t("profile.form.optional")} {...register("defaultAddress")} />

        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("profile.form.cancel")}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {t("profile.form.saveChanges")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export function CustomerProfile() {
  const { t } = useTranslation("customer");
  const { user } = useAuth();
  const [modalOpen, setModalOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const { profile, loading, error } = useCustomerProfile(refreshKey);

  if (loading) return <PageLoader />;

  if (error || !profile) {
    return <EmptyState icon={<UserIcon size={26} />} title={t("profile.errorTitle")} description={error ?? t("profile.errorDescription")} />;
  }

  const typeLabel = profile.customerType === CustomerType.Wholesale ? t("profile.typeWholesale") : t("profile.typeRetail");

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_1.4fr]">
      <div className="rounded-3xl border border-stone-100 bg-white p-6 dark:border-stone-800 dark:bg-stone-900">
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("profile.accountTitle")}</h2>
        <div className="mt-2 divide-y divide-stone-50 dark:divide-stone-800/60">
          <Row icon={<UserIcon size={16} />} label={t("profile.fullName")} value={user?.fullName ?? "—"} />
          <Row icon={<Mail size={16} />} label={t("profile.email")} value={user?.email ?? "—"} />
          <Row icon={<UserIcon size={16} />} label={t("profile.customerType")} value={typeLabel} />
        </div>
      </div>

      <div className="rounded-3xl border border-stone-100 bg-white p-6 dark:border-stone-800 dark:bg-stone-900">
        <div className="flex items-center justify-between">
          <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("profile.deliveryTitle")}</h2>
          <Button size="sm" variant="outline" leftIcon={<Pencil size={14} />} onClick={() => setModalOpen(true)}>
            {t("profile.editAction")}
          </Button>
        </div>
        <div className="mt-2 divide-y divide-stone-50 dark:divide-stone-800/60">
          <Row icon={<MapPin size={16} />} label={t("profile.region")} value={`${profile.region}, ${profile.district}`} />
          <Row icon={<MapPin size={16} />} label={t("profile.defaultAddress")} value={profile.defaultAddress || t("profile.noAddress")} />
          <Row icon={<Phone size={16} />} label={t("profile.memberSince")} value={formatDate(profile.createdAt)} />
        </div>
      </div>

      <EditProfileModal open={modalOpen} onClose={() => setModalOpen(false)} profile={profile} onSaved={() => setRefreshKey((k) => k + 1)} />
    </div>
  );
}
