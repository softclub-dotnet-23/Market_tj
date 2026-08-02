import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { AlertTriangle, KeyRound, Phone, Star, Truck } from "lucide-react";
import { Modal } from "@/components/ui/Modal";
import { Avatar } from "@/components/ui/Avatar";
import { Button } from "@/components/ui/Button";
import { Textarea } from "@/components/ui/Field";
import { DeliveryTimeline } from "@/components/delivery/DeliveryTimeline";
import { DeliveryStatusBadge } from "@/components/delivery/DeliveryStatusBadge";
import { ApiError, resolveMediaUrl } from "@/lib/api";
import { formatDateTime, formatSomoni } from "@/lib/utils";
import { DeliveryStatus, reportDeliveryProblem, type DeliveryDto } from "@/data/delivery";

function maskVehicleNumber(value: string): string {
  if (value.length <= 4) return value;
  return `${value.slice(0, 2)}••${value.slice(-2)}`;
}

export function CustomerDeliveryModal({
  open,
  onClose,
  delivery,
  orderConfirmed,
  deliveryAddress,
}: {
  open: boolean;
  onClose: () => void;
  delivery: DeliveryDto | null;
  orderConfirmed: boolean;
  deliveryAddress: string;
}) {
  const { t } = useTranslation(["delivery", "common"]);
  const [reportOpen, setReportOpen] = useState(false);
  const [problemText, setProblemText] = useState("");
  const [sending, setSending] = useState(false);

  const handleReport = async () => {
    if (!delivery || !problemText.trim()) return;
    setSending(true);
    try {
      await reportDeliveryProblem(delivery.id, problemText.trim());
      toast.success(t("delivery:problem.success"));
      setReportOpen(false);
      setProblemText("");
    } catch (err) {
      toast.error(t("delivery:problem.error"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setSending(false);
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-md">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("delivery:customer.title")}</h2>

      {!delivery ? (
        <p className="mt-4 text-sm text-stone-500 dark:text-stone-400">{t("delivery:customer.waitingForFarmer")}</p>
      ) : (
        <div className="mt-5 flex flex-col gap-5">
          {delivery.courierId ? (
            <div className="flex items-center gap-3 rounded-2xl border border-stone-100 bg-stone-25 p-4 dark:border-stone-800 dark:bg-stone-950/40">
              <Avatar
                name={delivery.courierFullName ?? "?"}
                src={delivery.courierAvatarUrl ? resolveMediaUrl(delivery.courierAvatarUrl) : undefined}
                size={48}
              />
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium text-stone-900 dark:text-stone-50">
                  {delivery.courierFullName?.split(" ")[0] ?? t("delivery:customer.courierFallback")}
                </p>
                {delivery.courierRating != null && (
                  <span className="flex items-center gap-1 text-xs text-stone-500 dark:text-stone-400">
                    <Star size={11} className="text-harvest-500" fill="currentColor" /> {delivery.courierRating.toFixed(1)}
                  </span>
                )}
                {delivery.courierVehicleNumber && (
                  <span className="flex items-center gap-1 text-xs text-stone-500 dark:text-stone-400">
                    <Truck size={11} /> {delivery.courierTransportType} · {maskVehicleNumber(delivery.courierVehicleNumber)}
                  </span>
                )}
              </div>
              {delivery.courierPhoneNumber && (
                <a
                  href={`tel:${delivery.courierPhoneNumber}`}
                  className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-grove-700 text-white transition hover:bg-grove-800"
                  aria-label={t("delivery:customer.callCourier")}
                >
                  <Phone size={15} />
                </a>
              )}
            </div>
          ) : (
            <p className="text-sm text-stone-400 dark:text-stone-500">{t("delivery:customer.waitingForCourier")}</p>
          )}

          <div className="flex flex-wrap items-center justify-between gap-2">
            <DeliveryStatusBadge status={delivery.status} />
            {delivery.deliveryPrice > 0 && (
              <span className="text-sm font-semibold text-stone-800 dark:text-stone-100">
                {formatSomoni(delivery.deliveryPrice)} {t("common:currencySomoni")}
              </span>
            )}
          </div>

          {delivery.estimatedDeliveryAt && delivery.status !== DeliveryStatus.Delivered && (
            <p className="text-sm text-stone-500 dark:text-stone-400">
              {t("delivery:customer.estimatedArrival")}: <span className="font-medium text-stone-800 dark:text-stone-100">{formatDateTime(delivery.estimatedDeliveryAt)}</span>
            </p>
          )}

          {delivery.status !== DeliveryStatus.Delivered && delivery.status !== DeliveryStatus.Cancelled && delivery.confirmationCode && (
            <div className="flex items-center gap-3 rounded-2xl border border-harvest-200 bg-harvest-50 p-4 dark:border-harvest-900/40 dark:bg-harvest-950/20">
              <KeyRound size={20} className="shrink-0 text-harvest-700 dark:text-harvest-400" />
              <div>
                <p className="text-xs text-harvest-800 dark:text-harvest-200">{t("delivery:customer.confirmationCodeHint")}</p>
                <p className="font-display text-2xl tracking-[0.3em] text-harvest-900 dark:text-harvest-100">{delivery.confirmationCode}</p>
              </div>
            </div>
          )}

          <div className="border-t border-stone-100 pt-4 dark:border-stone-800">
            <DeliveryTimeline orderConfirmed={orderConfirmed} status={delivery.status} />
          </div>

          <p className="text-xs text-stone-400 dark:text-stone-500">
            {t("delivery:customer.deliveryAddress")}: {deliveryAddress}
          </p>
          {delivery.adminNote && <p className="text-xs text-stone-400 dark:text-stone-500">{delivery.adminNote}</p>}

          {delivery.status !== DeliveryStatus.Delivered && delivery.status !== DeliveryStatus.Cancelled && (
            <>
              {!reportOpen ? (
                <Button type="button" variant="outline" size="sm" leftIcon={<AlertTriangle size={14} />} onClick={() => setReportOpen(true)}>
                  {t("delivery:problem.reportAction")}
                </Button>
              ) : (
                <div className="flex flex-col gap-2">
                  <Textarea
                    rows={3}
                    placeholder={t("delivery:problem.placeholder")}
                    value={problemText}
                    onChange={(e) => setProblemText(e.target.value)}
                  />
                  <div className="flex gap-2">
                    <Button type="button" size="sm" loading={sending} disabled={!problemText.trim()} onClick={handleReport}>
                      {t("delivery:problem.submit")}
                    </Button>
                    <Button type="button" size="sm" variant="outline" onClick={() => setReportOpen(false)}>
                      {t("common:actions.cancel")}
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}
    </Modal>
  );
}
