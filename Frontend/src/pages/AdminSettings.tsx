import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Bell, Building2, Percent, Save, Settings, TriangleAlert } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input, Textarea, Checkbox } from "@/components/ui/Field";
import { formatDate } from "@/lib/utils";
import { updatePlatformSettings, usePlatformSettings, type PlatformSettingsFormDto } from "@/data/adminEntities";

function SectionCard({
  icon,
  title,
  description,
  children,
}: {
  icon: React.ReactNode;
  title: string;
  description: string;
  children: React.ReactNode;
}) {
  return (
    <Card>
      <div className="flex items-start gap-3">
        <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-400">
          {icon}
        </span>
        <div>
          <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{title}</h2>
          <p className="mt-0.5 text-sm text-stone-500 dark:text-stone-400">{description}</p>
        </div>
      </div>
      <div className="mt-6 flex flex-col gap-5">{children}</div>
    </Card>
  );
}

export function AdminSettings() {
  const { t } = useTranslation("admin");
  const [refreshKey, setRefreshKey] = useState(0);
  const { settings, loading, error } = usePlatformSettings(refreshKey);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<PlatformSettingsFormDto>();

  const maintenanceModeEnabled = watch("maintenanceModeEnabled");

  useEffect(() => {
    if (!settings) return;
    reset({
      siteName: settings.siteName,
      logoUrl: settings.logoUrl ?? "",
      contactEmail: settings.contactEmail,
      contactPhone: settings.contactPhone,
      commissionPercent: settings.commissionPercent,
      currency: settings.currency,
      minimumOrderAmount: settings.minimumOrderAmount,
      maintenanceModeEnabled: settings.maintenanceModeEnabled,
      maintenanceMessage: settings.maintenanceMessage ?? "",
      emailNotificationsEnabled: settings.emailNotificationsEnabled,
      smsNotificationsEnabled: settings.smsNotificationsEnabled,
    });
  }, [settings, reset]);

  if (loading) return <PageLoader />;

  if (error || !settings) {
    return <EmptyState icon={<Settings size={26} />} title={t("platformSettings.errorTitle")} description={error ?? t("platformSettings.errorDescription")} />;
  }

  const onSubmit = async (values: PlatformSettingsFormDto) => {
    try {
      await updatePlatformSettings({
        ...values,
        logoUrl: values.logoUrl || null,
        maintenanceMessage: values.maintenanceMessage || null,
        commissionPercent: Number(values.commissionPercent),
        minimumOrderAmount: Number(values.minimumOrderAmount),
      });
      toast.success(t("platformSettings.saveSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("platformSettings.saveError"), { description: err instanceof Error ? err.message : undefined });
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-6">
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <div>
          <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("platformSettings.title")}</h1>
          <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">{t("platformSettings.subtitle")}</p>
        </div>
        <div className="flex items-center gap-3">
          {settings.updatedAt && (
            <span className="text-xs text-stone-400 dark:text-stone-500">
              {t("platformSettings.lastUpdated", { date: formatDate(settings.updatedAt) })}
            </span>
          )}
          <Button type="submit" leftIcon={<Save size={16} />} loading={isSubmitting}>
            {t("platformSettings.saveButton")}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <SectionCard
          icon={<Building2 size={18} />}
          title={t("platformSettings.sections.general.title")}
          description={t("platformSettings.sections.general.description")}
        >
          <Input
            label={t("platformSettings.sections.general.siteName")}
            error={errors.siteName?.message}
            {...register("siteName", { required: t("platformSettings.required") })}
          />
          <Input
            label={t("platformSettings.sections.general.logoUrl")}
            hint={t("platformSettings.sections.general.logoUrlHint")}
            {...register("logoUrl")}
          />
          <Input
            type="email"
            label={t("platformSettings.sections.general.contactEmail")}
            error={errors.contactEmail?.message}
            {...register("contactEmail", { required: t("platformSettings.required") })}
          />
          <Input
            label={t("platformSettings.sections.general.contactPhone")}
            error={errors.contactPhone?.message}
            {...register("contactPhone", { required: t("platformSettings.required") })}
          />
        </SectionCard>

        <SectionCard
          icon={<Percent size={18} />}
          title={t("platformSettings.sections.commission.title")}
          description={t("platformSettings.sections.commission.description")}
        >
          <Input
            type="number"
            step="0.1"
            min={0}
            max={100}
            label={t("platformSettings.sections.commission.commissionPercent")}
            error={errors.commissionPercent?.message}
            {...register("commissionPercent", { required: t("platformSettings.required"), valueAsNumber: true, min: 0, max: 100 })}
          />
          <Input
            label={t("platformSettings.sections.commission.currency")}
            error={errors.currency?.message}
            {...register("currency", { required: t("platformSettings.required") })}
          />
          <Input
            type="number"
            step="0.01"
            min={0}
            label={t("platformSettings.sections.commission.minimumOrderAmount")}
            error={errors.minimumOrderAmount?.message}
            {...register("minimumOrderAmount", { required: t("platformSettings.required"), valueAsNumber: true, min: 0 })}
          />
        </SectionCard>

        <SectionCard
          icon={<Bell size={18} />}
          title={t("platformSettings.sections.notifications.title")}
          description={t("platformSettings.sections.notifications.description")}
        >
          <Checkbox label={t("platformSettings.sections.notifications.emailEnabled")} {...register("emailNotificationsEnabled")} />
          <Checkbox label={t("platformSettings.sections.notifications.smsEnabled")} {...register("smsNotificationsEnabled")} />
        </SectionCard>

        <SectionCard
          icon={<TriangleAlert size={18} />}
          title={t("platformSettings.sections.maintenance.title")}
          description={t("platformSettings.sections.maintenance.description")}
        >
          <Checkbox label={t("platformSettings.sections.maintenance.enabled")} {...register("maintenanceModeEnabled")} />
          {maintenanceModeEnabled && (
            <Textarea
              label={t("platformSettings.sections.maintenance.message")}
              hint={t("platformSettings.sections.maintenance.messageHint")}
              error={errors.maintenanceMessage?.message}
              {...register("maintenanceMessage", {
                required: maintenanceModeEnabled ? t("platformSettings.sections.maintenance.messageRequired") : false,
              })}
            />
          )}
        </SectionCard>
      </div>
    </form>
  );
}
