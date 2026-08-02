import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { AlertTriangle, KeyRound, MapPin, Package, Phone, Truck } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import { Input, Textarea } from "@/components/ui/Field";
import { DeliveryStatusBadge } from "@/components/delivery/DeliveryStatusBadge";
import { ApiError } from "@/lib/api";
import { formatDateTime, formatSomoni } from "@/lib/utils";
import {
  DeliveryStatus,
  NEXT_COURIER_STATUS,
  acceptDelivery,
  confirmDelivery,
  reportDeliveryProblem,
  updateCourierDeliveryStatus,
  useMyDeliveries,
  type DeliveryDto,
} from "@/data/delivery";

// Подпись кнопки под каждый следующий шаг курьера — по прямому запросу
// пользователя (2026-08-02). Только один валидный переход с текущего
// статуса показывается сразу (см. NEXT_COURIER_STATUS), остальное недоступно.
const STEP_ACTION_KEY: Partial<Record<number, string>> = {
  [DeliveryStatus.GoingToFarmer]: "goingToFarmer",
  [DeliveryStatus.ArrivedAtFarmer]: "arrivedAtFarmer",
  [DeliveryStatus.PickedUp]: "pickedUp",
  [DeliveryStatus.InTransit]: "inTransit",
  [DeliveryStatus.ArrivedAtClient]: "arrivedAtClient",
};

export function CourierDeliveries() {
  const { t } = useTranslation(["courier", "delivery", "common"]);
  const [refreshKey, setRefreshKey] = useState(0);
  const [busyId, setBusyId] = useState<number | null>(null);
  const [confirmingDelivery, setConfirmingDelivery] = useState<DeliveryDto | null>(null);
  const [reportingDelivery, setReportingDelivery] = useState<DeliveryDto | null>(null);
  const { deliveries, loading, error } = useMyDeliveries(refreshKey);

  const bump = () => setRefreshKey((k) => k + 1);

  const handleAccept = async (delivery: DeliveryDto) => {
    setBusyId(delivery.id);
    try {
      await acceptDelivery(delivery.id);
      toast.success(t("courier:actions.acceptSuccess"));
      bump();
    } catch (err) {
      toast.error(t("courier:actions.actionError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setBusyId(null);
    }
  };

  const handleAdvance = async (delivery: DeliveryDto) => {
    const next = NEXT_COURIER_STATUS[delivery.status];
    if (!next) return;
    setBusyId(delivery.id);
    try {
      await updateCourierDeliveryStatus(delivery.id, next);
      toast.success(t("courier:actions.actionSuccess"));
      bump();
    } catch (err) {
      toast.error(t("courier:actions.actionError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setBusyId(null);
    }
  };

  if (loading) return <PageLoader />;

  if (error || !deliveries) {
    return <EmptyState icon={<Truck size={26} />} title={t("courier:errorTitle")} description={error ?? t("courier:errorDescription")} />;
  }

  if (deliveries.length === 0) {
    return <EmptyState icon={<Truck size={26} />} title={t("courier:emptyTitle")} description={t("courier:emptyDescription")} />;
  }

  const active = deliveries.filter((d) => d.status !== DeliveryStatus.Delivered && d.status !== DeliveryStatus.Cancelled);
  const finished = deliveries.filter((d) => d.status === DeliveryStatus.Delivered || d.status === DeliveryStatus.Cancelled);

  const renderCard = (delivery: DeliveryDto) => {
    const nextStepKey = STEP_ACTION_KEY[NEXT_COURIER_STATUS[delivery.status] ?? -1];
    const canReportProblem = delivery.status !== DeliveryStatus.Delivered && delivery.status !== DeliveryStatus.Cancelled;
    return (
      <div key={delivery.id} className="flex flex-col gap-3 rounded-2xl border border-stone-100 bg-white p-5 shadow-sm dark:border-stone-800 dark:bg-stone-900">
        <div className="flex items-start justify-between gap-3">
          <p className="font-display text-base text-stone-900 dark:text-stone-50">{delivery.orderNumber ?? `#${delivery.orderId}`}</p>
          <DeliveryStatusBadge status={delivery.status} />
        </div>

        <div className="flex flex-col gap-2 rounded-xl border border-stone-100 bg-stone-25 p-3 text-sm dark:border-stone-800 dark:bg-stone-950/40">
          <div className="flex items-start gap-2">
            <MapPin size={14} className="mt-0.5 shrink-0 text-grove-600" />
            <div className="min-w-0">
              <p className="text-xs text-stone-400 dark:text-stone-500">{t("courier:card.pickupFrom")}</p>
              <p className="font-medium text-stone-800 dark:text-stone-100">{delivery.farmerName}</p>
              <p className="text-xs text-stone-500 dark:text-stone-400">{delivery.pickupAddress}</p>
              {delivery.farmerPhoneNumber && (
                <a href={`tel:${delivery.farmerPhoneNumber}`} className="flex items-center gap-1 text-xs text-grove-700 hover:underline dark:text-grove-400">
                  <Phone size={10} /> {delivery.farmerPhoneNumber}
                </a>
              )}
            </div>
          </div>
          <div className="flex items-start gap-2 border-t border-stone-100 pt-2 dark:border-stone-800">
            <MapPin size={14} className="mt-0.5 shrink-0 text-clay-500" />
            <div className="min-w-0">
              <p className="text-xs text-stone-400 dark:text-stone-500">{t("courier:card.deliverTo")}</p>
              <p className="font-medium text-stone-800 dark:text-stone-100">{delivery.customerName}</p>
              <p className="text-xs text-stone-500 dark:text-stone-400">{delivery.deliveryAddress}</p>
              {delivery.customerPhoneNumber && (
                <a href={`tel:${delivery.customerPhoneNumber}`} className="flex items-center gap-1 text-xs text-grove-700 hover:underline dark:text-grove-400">
                  <Phone size={10} /> {delivery.customerPhoneNumber}
                </a>
              )}
            </div>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-stone-500 dark:text-stone-400">
          <span className="flex items-center gap-1">
            <Package size={12} /> {t("courier:card.itemCount", { count: delivery.itemCount })}
          </span>
          {delivery.deliveryPrice > 0 && (
            <span className="font-semibold text-stone-700 dark:text-stone-200">
              {formatSomoni(delivery.deliveryPrice)} {t("common:currencySomoni")}
            </span>
          )}
          {delivery.estimatedDeliveryAt && <span>{t("courier:card.eta")}: {formatDateTime(delivery.estimatedDeliveryAt)}</span>}
        </div>

        {delivery.adminNote && <p className="text-xs text-stone-500 dark:text-stone-400">{delivery.adminNote}</p>}

        <div className="flex flex-wrap gap-2 border-t border-stone-100 pt-3 dark:border-stone-800">
          {delivery.status === DeliveryStatus.Assigned && (
            <Button type="button" size="sm" loading={busyId === delivery.id} onClick={() => handleAccept(delivery)}>
              {t("courier:actions.accept")}
            </Button>
          )}
          {nextStepKey && (
            <Button type="button" size="sm" loading={busyId === delivery.id} onClick={() => handleAdvance(delivery)}>
              {t(`courier:actions.${nextStepKey}`)}
            </Button>
          )}
          {delivery.status === DeliveryStatus.ArrivedAtClient && (
            <Button type="button" size="sm" leftIcon={<KeyRound size={14} />} onClick={() => setConfirmingDelivery(delivery)}>
              {t("courier:actions.confirmDelivery")}
            </Button>
          )}
          {canReportProblem && (
            <Button type="button" size="sm" variant="outline" leftIcon={<AlertTriangle size={14} />} onClick={() => setReportingDelivery(delivery)}>
              {t("courier:actions.reportProblem")}
            </Button>
          )}
        </div>
      </div>
    );
  };

  return (
    <div className="flex flex-col gap-8">
      {active.length > 0 && (
        <div className="flex flex-col gap-3">
          <h2 className="font-display text-base text-stone-900 dark:text-stone-50">{t("courier:activeSection")}</h2>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">{active.map(renderCard)}</div>
        </div>
      )}
      {finished.length > 0 && (
        <div className="flex flex-col gap-3">
          <h2 className="font-display text-base text-stone-900 dark:text-stone-50">{t("courier:historySection")}</h2>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">{finished.map(renderCard)}</div>
        </div>
      )}

      <ConfirmCodeModal delivery={confirmingDelivery} onClose={() => setConfirmingDelivery(null)} onConfirmed={bump} />
      <ReportProblemModal delivery={reportingDelivery} onClose={() => setReportingDelivery(null)} onReported={bump} />
    </div>
  );
}

function ConfirmCodeModal({ delivery, onClose, onConfirmed }: { delivery: DeliveryDto | null; onClose: () => void; onConfirmed: () => void }) {
  const { t } = useTranslation(["courier", "common"]);
  const [code, setCode] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (!delivery || code.length !== 4) return;
    setSubmitting(true);
    try {
      await confirmDelivery(delivery.id, code);
      toast.success(t("courier:confirmModal.success"));
      setCode("");
      onConfirmed();
      onClose();
    } catch (err) {
      toast.error(t("courier:confirmModal.error"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal open={!!delivery} onClose={() => { setCode(""); onClose(); }} className="max-w-sm">
      <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("courier:confirmModal.title")}</h2>
      <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">{t("courier:confirmModal.description")}</p>
      <Input
        className="mt-4 text-center font-display text-2xl tracking-[0.4em]"
        maxLength={4}
        inputMode="numeric"
        value={code}
        onChange={(e) => setCode(e.target.value.replace(/\D/g, "").slice(0, 4))}
        placeholder="••••"
      />
      <div className="mt-5 flex justify-end gap-3">
        <Button type="button" variant="outline" onClick={onClose}>
          {t("common:actions.cancel")}
        </Button>
        <Button type="button" loading={submitting} disabled={code.length !== 4} onClick={handleSubmit}>
          {t("courier:actions.confirmDelivery")}
        </Button>
      </div>
    </Modal>
  );
}

function ReportProblemModal({ delivery, onClose, onReported }: { delivery: DeliveryDto | null; onClose: () => void; onReported: () => void }) {
  const { t } = useTranslation(["delivery", "common"]);
  const [description, setDescription] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (!delivery || !description.trim()) return;
    setSubmitting(true);
    try {
      await reportDeliveryProblem(delivery.id, description.trim());
      toast.success(t("delivery:problem.success"));
      setDescription("");
      onReported();
      onClose();
    } catch (err) {
      toast.error(t("delivery:problem.error"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal open={!!delivery} onClose={() => { setDescription(""); onClose(); }} className="max-w-sm">
      <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("delivery:problem.title")}</h2>
      <Textarea
        className="mt-4"
        rows={4}
        placeholder={t("delivery:problem.placeholder")}
        value={description}
        onChange={(e) => setDescription(e.target.value)}
      />
      <div className="mt-5 flex justify-end gap-3">
        <Button type="button" variant="outline" onClick={onClose}>
          {t("common:actions.cancel")}
        </Button>
        <Button type="button" loading={submitting} disabled={!description.trim()} onClick={handleSubmit}>
          {t("delivery:problem.submit")}
        </Button>
      </div>
    </Modal>
  );
}
