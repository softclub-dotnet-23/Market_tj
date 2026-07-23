import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { Mail, MapPin, Phone, User as UserIcon } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { formatDate } from "@/lib/utils";
import { useAuth } from "@/context/AuthContext";
import { CustomerType, useCustomerProfile } from "@/data/customer";

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

export function CustomerProfile() {
  const { t } = useTranslation("customer");
  const { user } = useAuth();
  const { profile, loading, error } = useCustomerProfile();

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
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("profile.deliveryTitle")}</h2>
        <div className="mt-2 divide-y divide-stone-50 dark:divide-stone-800/60">
          <Row icon={<MapPin size={16} />} label={t("profile.region")} value={`${profile.region}, ${profile.district}`} />
          <Row icon={<MapPin size={16} />} label={t("profile.defaultAddress")} value={profile.defaultAddress || t("profile.noAddress")} />
          <Row icon={<Phone size={16} />} label={t("profile.memberSince")} value={formatDate(profile.createdAt)} />
        </div>
        <p className="mt-4 text-xs text-stone-400 dark:text-stone-500">{t("profile.editHint")}</p>
      </div>
    </div>
  );
}
