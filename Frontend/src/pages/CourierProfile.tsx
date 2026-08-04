import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { CalendarDays, CheckCircle2, MapPin, Package, Star, Truck } from "lucide-react";
import { Card } from "@/components/ui/Card";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Switch } from "@/components/ui/Switch";
import { StatCard } from "@/components/ui/StatCard";
import { Avatar } from "@/components/ui/Avatar";
import { useAuth } from "@/context/AuthContext";
import { resolveMediaUrl } from "@/lib/api";
import { useCourierProfile, setCourierAvailability, useCourierDocuments, hasApprovedCourierDocuments } from "@/data/courier";
import { DeliveryStatus, useMyDeliveries } from "@/data/delivery";
import { formatNumber } from "@/lib/utils";

// Начало недели — понедельник (ISO), не воскресенье — тот же принцип, что
// уже используется в дашбордах Admin/Farmer для "на этой неделе".
function startOfWeek(now: Date): Date {
  const d = new Date(now);
  const day = d.getDay();
  const diff = (day === 0 ? -6 : 1) - day;
  d.setDate(d.getDate() + diff);
  d.setHours(0, 0, 0, 0);
  return d;
}

function startOfDay(now: Date): Date {
  const d = new Date(now);
  d.setHours(0, 0, 0, 0);
  return d;
}

export function CourierProfile() {
  const { t } = useTranslation(["courier", "common"]);
  const { user } = useAuth();
  const [refreshKey, setRefreshKey] = useState(0);
  const [togglingAvailability, setTogglingAvailability] = useState(false);
  const { profile, loading: profileLoading, error: profileError } = useCourierProfile(refreshKey);
  const { deliveries, loading: deliveriesLoading } = useMyDeliveries();
  const { documents } = useCourierDocuments(profile?.id ?? null, refreshKey);
  const documentsApproved = hasApprovedCourierDocuments(documents);

  const stats = useMemo(() => {
    if (!deliveries) return { today: 0, week: 0, total: 0, active: 0 };
    const now = new Date();
    const dayStart = startOfDay(now);
    const weekStart = startOfWeek(now);
    const delivered = deliveries.filter((d) => d.status === DeliveryStatus.Delivered && d.deliveredAt);
    const today = delivered.filter((d) => new Date(d.deliveredAt!) >= dayStart).length;
    const week = delivered.filter((d) => new Date(d.deliveredAt!) >= weekStart).length;
    const active = deliveries.filter((d) => d.status !== DeliveryStatus.Delivered && d.status !== DeliveryStatus.Cancelled).length;
    return { today, week, total: delivered.length, active };
  }, [deliveries]);

  const handleToggleAvailability = async () => {
    if (!profile) return;
    // Гейт документов — по прямому запросу пользователя (2026-08-04):
    // курьер не может включить доступность, пока admin не одобрил оба
    // обязательных документа. Бэкенд проверяет то же самое (см.
    // CourierProfileService.UpdateAsync) — это только UX-подсказка, а не
    // единственная защита.
    if (!profile.isAvailable && !documentsApproved) {
      toast.error(t("courier:profile.documentsNotApproved"));
      return;
    }
    setTogglingAvailability(true);
    const next = !profile.isAvailable;
    try {
      await setCourierAvailability(profile, next);
      toast.success(next ? t("courier:profile.availableOnSuccess") : t("courier:profile.availableOffSuccess"));
      setRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("courier:profile.availabilityError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setTogglingAvailability(false);
    }
  };

  if (profileLoading || deliveriesLoading) return <PageLoader />;

  if (profileError || !profile) {
    return (
      <EmptyState
        icon={<Truck size={26} />}
        title={t("courier:profile.errorTitle")}
        description={profileError ?? t("courier:profile.errorDescription")}
      />
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <Card className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <Avatar name={user?.fullName ?? t("courier:courierName")} src={user?.avatarUrl ? resolveMediaUrl(user.avatarUrl) : undefined} size={56} />
          <div>
            <p className="font-display text-lg text-stone-900 dark:text-stone-50">{user?.fullName ?? t("courier:courierName")}</p>
            <p className="flex items-center gap-1 text-sm text-stone-500 dark:text-stone-400">
              <Star size={13} className="fill-current text-harvest-500" />
              {profile.rating.toFixed(1)} / 5
            </p>
          </div>
        </div>

        <div
          className={`flex items-center gap-3 rounded-2xl border p-4 ${
            profile.isAvailable
              ? "border-grove-200 bg-grove-50 dark:border-grove-900 dark:bg-grove-950/40"
              : "border-stone-200 bg-stone-50 dark:border-stone-700 dark:bg-stone-800/60"
          }`}
        >
          <div>
            <p
              className={`text-sm font-semibold ${profile.isAvailable ? "text-grove-700 dark:text-grove-400" : "text-stone-600 dark:text-stone-300"}`}
            >
              {profile.isAvailable ? t("courier:profile.availableOn") : t("courier:profile.availableOff")}
            </p>
            <p className="text-xs text-stone-400 dark:text-stone-500">
              {!profile.isAvailable && !documentsApproved ? t("courier:profile.documentsNotApproved") : t("courier:profile.availabilityHint")}
            </p>
          </div>
          <Switch
            checked={profile.isAvailable}
            onChange={handleToggleAvailability}
            disabled={togglingAvailability || (!profile.isAvailable && !documentsApproved)}
            aria-label={t("courier:profile.availableOn")}
          />
        </div>
      </Card>

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <StatCard icon={CalendarDays} accent="grove" label={t("courier:profile.stats.today")} value={formatNumber(stats.today)} />
        <StatCard icon={Package} accent="blue" label={t("courier:profile.stats.week")} value={formatNumber(stats.week)} />
        <StatCard icon={CheckCircle2} accent="orange" label={t("courier:profile.stats.total")} value={formatNumber(stats.total)} />
        <StatCard icon={Truck} accent="rose" label={t("courier:profile.stats.active")} value={formatNumber(stats.active)} />
      </div>

      <Card className="flex flex-col gap-4">
        <h2 className="font-display text-base text-stone-900 dark:text-stone-50">{t("courier:profile.detailsTitle")}</h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="flex items-start gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-stone-100 text-stone-500 dark:bg-stone-800 dark:text-stone-400">
              <Truck size={16} />
            </span>
            <div>
              <p className="text-xs text-stone-400 dark:text-stone-500">{t("courier:profile.transport")}</p>
              <p className="text-sm font-medium text-stone-800 dark:text-stone-100">
                {profile.transportType} · {profile.vehicleNumber}
              </p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-stone-100 text-stone-500 dark:bg-stone-800 dark:text-stone-400">
              <MapPin size={16} />
            </span>
            <div>
              <p className="text-xs text-stone-400 dark:text-stone-500">{t("courier:profile.region")}</p>
              <p className="text-sm font-medium text-stone-800 dark:text-stone-100">
                {profile.region}, {profile.district}
              </p>
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}
