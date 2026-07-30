import { useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { motion } from "framer-motion";
import { Camera, Loader2, Mail, MapPin, Pencil, Phone, User as UserIcon } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { Card } from "@/components/ui/Card";
import { EmptyState } from "@/components/ui/EmptyState";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Field";
import { formatDate } from "@/lib/utils";
import { useAuth } from "@/context/AuthContext";
import { ApiError, resolveMediaUrl } from "@/lib/api";
import {
  CustomerType,
  updateCustomerProfile,
  useCustomerProfile,
  type CustomerProfileDto,
  type CustomerProfileEditableFields,
} from "@/data/customer";

const AVATAR_ALLOWED_TYPES = ["image/jpeg", "image/png", "image/webp"];
const AVATAR_MAX_SIZE_BYTES = 5 * 1024 * 1024;

function Row({ icon, label, value }: { icon: React.ReactNode; label: string; value: React.ReactNode }) {
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
  const { t } = useTranslation(["customer", "common"]);
  const { user, uploadAvatar, removeAvatar } = useAuth();
  const [modalOpen, setModalOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [avatarBusy, setAvatarBusy] = useState(false);
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const { profile, loading, error } = useCustomerProfile(refreshKey);

  if (loading) return <PageLoader />;

  if (error || !profile) {
    return <EmptyState icon={<UserIcon size={26} />} title={t("profile.errorTitle")} description={error ?? t("profile.errorDescription")} />;
  }

  const typeLabel = profile.customerType === CustomerType.Wholesale ? t("profile.typeWholesale") : t("profile.typeRetail");

  const onAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;

    if (!AVATAR_ALLOWED_TYPES.includes(file.type)) {
      toast.error(t("common:avatar.invalidType"));
      return;
    }
    if (file.size > AVATAR_MAX_SIZE_BYTES) {
      toast.error(t("common:avatar.tooLarge"));
      return;
    }

    setAvatarBusy(true);
    try {
      await uploadAvatar(file);
      toast.success(t("common:avatar.uploadSuccess"));
    } catch (err) {
      toast.error(t("common:avatar.uploadError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setAvatarBusy(false);
    }
  };

  const onAvatarRemove = async () => {
    setAvatarBusy(true);
    try {
      await removeAvatar();
      toast.success(t("common:avatar.removeSuccess"));
    } catch (err) {
      toast.error(t("common:avatar.removeError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setAvatarBusy(false);
    }
  };

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-[300px_1fr]">
      <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.5 }}>
        <Card className="flex flex-col items-center gap-4 overflow-hidden py-8 text-center">
          <div className="relative flex items-center justify-center">
            <motion.span
              aria-hidden
              className="absolute h-28 w-28 rounded-full bg-grove-400/30 blur-2xl dark:bg-grove-500/20"
              animate={{ scale: [1, 1.12, 1], opacity: [0.5, 0.8, 0.5] }}
              transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
            />
            <span className="relative flex h-24 w-24 items-center justify-center overflow-hidden rounded-full bg-linear-to-br from-grove-500 to-grove-700 text-white shadow-[0_8px_24px_-6px_rgba(59,168,90,0.55)]">
              {avatarBusy ? (
                <Loader2 size={26} className="animate-spin" />
              ) : user?.avatarUrl ? (
                <img src={resolveMediaUrl(user.avatarUrl)} alt="" className="h-full w-full object-cover" />
              ) : (
                <UserIcon size={34} />
              )}
            </span>
            <input
              ref={avatarInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="hidden"
              onChange={onAvatarChange}
              disabled={avatarBusy}
            />
            <motion.button
              type="button"
              onClick={() => avatarInputRef.current?.click()}
              disabled={avatarBusy}
              aria-label={user?.avatarUrl ? t("common:avatar.changePhoto") : t("common:avatar.addPhoto")}
              whileHover={{ scale: 1.12 }}
              whileTap={{ scale: 0.92 }}
              className="absolute right-0 bottom-0 flex h-8 w-8 items-center justify-center rounded-full bg-stone-900 text-white shadow-md ring-2 ring-white transition-colors hover:bg-grove-700 disabled:opacity-60 dark:ring-stone-900"
            >
              <Camera size={14} />
            </motion.button>
          </div>
          {user?.avatarUrl && (
            <button
              type="button"
              onClick={onAvatarRemove}
              disabled={avatarBusy}
              className="-mt-2 text-xs font-medium text-rose-600 transition hover:underline disabled:opacity-60 dark:text-rose-400"
            >
              {t("common:avatar.removePhoto")}
            </button>
          )}
          <div>
            <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{user?.fullName}</h2>
            <p className="mt-1 flex items-center justify-center gap-1.5 text-sm text-stone-500 dark:text-stone-400">
              <Mail size={14} />
              {user?.email}
            </p>
          </div>
          <span className="flex items-center gap-1.5 rounded-full bg-grove-100 px-3 py-1.5 text-xs font-semibold text-grove-700 dark:bg-grove-900 dark:text-grove-300">
            {typeLabel}
          </span>
        </Card>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.5, delay: 0.1 }}>
        <Card>
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
        </Card>
      </motion.div>

      <EditProfileModal open={modalOpen} onClose={() => setModalOpen(false)} profile={profile} onSaved={() => setRefreshKey((k) => k + 1)} />
    </div>
  );
}
