import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Loader2, MapPin, Package, Phone, Search, Star, Truck } from "lucide-react";
import { Drawer } from "@/components/ui/Drawer";
import { Button } from "@/components/ui/Button";
import { Input, Select, Textarea, Checkbox } from "@/components/ui/Field";
import { Avatar } from "@/components/ui/Avatar";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { EmptyState } from "@/components/ui/EmptyState";
import { ApiError, resolveMediaUrl } from "@/lib/api";
import { cn } from "@/lib/utils";
import {
  assignCourier,
  updateDeliveryAdminDetails,
  useAvailableCouriers,
  type AvailableCourierDto,
  type DeliveryDto,
} from "@/data/delivery";

interface OrderSummary {
  id: number;
  orderNumber: string;
  farmerName: string;
  customerName: string;
  pickupAddress: string;
  deliveryAddress: string;
  itemCount: number;
}

function toDatetimeLocal(iso: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function fromDatetimeLocal(value: string): string | null {
  if (!value) return null;
  return new Date(value).toISOString();
}

export function AssignCourierDrawer({
  open,
  onClose,
  order,
  existingDelivery,
  onAssigned,
}: {
  open: boolean;
  onClose: () => void;
  order: OrderSummary;
  existingDelivery: DeliveryDto | null;
  onAssigned: () => void;
}) {
  const { t } = useTranslation(["delivery", "common"]);

  const [onlyAvailable, setOnlyAvailable] = useState(true);
  const [region, setRegion] = useState("");
  const [transportType, setTransportType] = useState("");
  const [minRating, setMinRating] = useState("");
  const [selectedCourierId, setSelectedCourierId] = useState<number | null>(existingDelivery?.courierId ?? null);
  const [deliveryFee, setDeliveryFee] = useState(existingDelivery ? String(existingDelivery.deliveryPrice) : "");
  const [estimatedPickupAt, setEstimatedPickupAt] = useState(toDatetimeLocal(existingDelivery?.estimatedPickupAt ?? null));
  const [estimatedDeliveryAt, setEstimatedDeliveryAt] = useState(toDatetimeLocal(existingDelivery?.estimatedDeliveryAt ?? null));
  const [adminNote, setAdminNote] = useState(existingDelivery?.adminNote ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [confirmReplace, setConfirmReplace] = useState(false);

  useEffect(() => {
    if (!open) return;
    setSelectedCourierId(existingDelivery?.courierId ?? null);
    setDeliveryFee(existingDelivery ? String(existingDelivery.deliveryPrice) : "");
    setEstimatedPickupAt(toDatetimeLocal(existingDelivery?.estimatedPickupAt ?? null));
    setEstimatedDeliveryAt(toDatetimeLocal(existingDelivery?.estimatedDeliveryAt ?? null));
    setAdminNote(existingDelivery?.adminNote ?? "");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, existingDelivery?.id]);

  const { couriers, loading } = useAvailableCouriers(
    { onlyAvailable, region: region || undefined, transportType: transportType || undefined, minRating: minRating ? Number(minRating) : undefined },
    open,
  );

  const regions = Array.from(new Set((couriers ?? []).map((c) => c.region))).sort();
  const transportTypes = Array.from(new Set((couriers ?? []).map((c) => c.transportType))).sort();

  const doSubmit = async () => {
    if (!selectedCourierId) return;
    setSubmitting(true);
    try {
      if (existingDelivery) {
        if (existingDelivery.courierId !== selectedCourierId) {
          await assignCourier(order.id, {
            courierId: selectedCourierId,
            deliveryFee: Number(deliveryFee) || 0,
            estimatedPickupAt: fromDatetimeLocal(estimatedPickupAt),
            estimatedDeliveryAt: fromDatetimeLocal(estimatedDeliveryAt),
            adminNote: adminNote || null,
          });
        } else {
          await updateDeliveryAdminDetails(existingDelivery.id, {
            deliveryFee: Number(deliveryFee) || 0,
            estimatedPickupAt: fromDatetimeLocal(estimatedPickupAt),
            estimatedDeliveryAt: fromDatetimeLocal(estimatedDeliveryAt),
            adminNote: adminNote || null,
          });
        }
      } else {
        await assignCourier(order.id, {
          courierId: selectedCourierId,
          deliveryFee: Number(deliveryFee) || 0,
          estimatedPickupAt: fromDatetimeLocal(estimatedPickupAt),
          estimatedDeliveryAt: fromDatetimeLocal(estimatedDeliveryAt),
          adminNote: adminNote || null,
        });
      }
      toast.success(t("delivery:assign.success"));
      onAssigned();
      onClose();
    } catch (err) {
      toast.error(t("delivery:assign.error"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setSubmitting(false);
    }
  };

  const handleSubmitClick = () => {
    if (!selectedCourierId) return;
    if (existingDelivery?.courierId && existingDelivery.courierId !== selectedCourierId) {
      setConfirmReplace(true);
      return;
    }
    void doSubmit();
  };

  return (
    <>
      <Drawer open={open} onClose={onClose} title={t("delivery:assign.title", { orderNumber: order.orderNumber })}>
        <div className="flex flex-col gap-6 p-6">
          <div className="grid grid-cols-1 gap-3 rounded-2xl border border-stone-100 bg-stone-25 p-4 text-sm dark:border-stone-800 dark:bg-stone-950/40">
            <div className="flex justify-between gap-3">
              <span className="text-stone-400 dark:text-stone-500">{t("delivery:assign.farmer")}</span>
              <span className="font-medium text-stone-800 dark:text-stone-100">{order.farmerName}</span>
            </div>
            <div className="flex justify-between gap-3">
              <span className="text-stone-400 dark:text-stone-500">{t("delivery:assign.customer")}</span>
              <span className="font-medium text-stone-800 dark:text-stone-100">{order.customerName}</span>
            </div>
            <div className="flex justify-between gap-3">
              <span className="text-stone-400 dark:text-stone-500">{t("delivery:assign.pickupAddress")}</span>
              <span className="text-right font-medium text-stone-800 dark:text-stone-100">{order.pickupAddress}</span>
            </div>
            <div className="flex justify-between gap-3">
              <span className="text-stone-400 dark:text-stone-500">{t("delivery:assign.deliveryAddress")}</span>
              <span className="text-right font-medium text-stone-800 dark:text-stone-100">{order.deliveryAddress}</span>
            </div>
            <div className="flex justify-between gap-3">
              <span className="text-stone-400 dark:text-stone-500">{t("delivery:assign.itemCount")}</span>
              <span className="flex items-center gap-1 font-medium text-stone-800 dark:text-stone-100">
                <Package size={13} /> {order.itemCount}
              </span>
            </div>
          </div>

          <div className="flex flex-col gap-3">
            <div className="flex items-center justify-between">
              <h3 className="font-display text-base text-stone-900 dark:text-stone-50">{t("delivery:assign.availableCouriers")}</h3>
              <Checkbox label={t("delivery:assign.onlyAvailable")} checked={onlyAvailable} onChange={(e) => setOnlyAvailable(e.target.checked)} />
            </div>

            <div className="grid grid-cols-2 gap-2.5 sm:grid-cols-3">
              <Select value={region} onChange={setRegion}>
                <option value="">{t("delivery:assign.allRegions")}</option>
                {regions.map((r) => (
                  <option key={r} value={r}>{r}</option>
                ))}
              </Select>
              <Select value={transportType} onChange={setTransportType}>
                <option value="">{t("delivery:assign.allTransportTypes")}</option>
                {transportTypes.map((tt) => (
                  <option key={tt} value={tt}>{tt}</option>
                ))}
              </Select>
              <Select value={minRating} onChange={setMinRating}>
                <option value="">{t("delivery:assign.anyRating")}</option>
                <option value="3">3+</option>
                <option value="4">4+</option>
                <option value="4.5">4.5+</option>
              </Select>
            </div>

            {loading ? (
              <div className="flex justify-center py-8">
                <Loader2 size={22} className="animate-spin text-grove-600" />
              </div>
            ) : !couriers || couriers.length === 0 ? (
              <EmptyState icon={<Search size={22} />} title={t("delivery:assign.noCouriers")} description={t("delivery:assign.noCouriersHint")} />
            ) : (
              <div className="flex flex-col gap-2.5">
                {couriers.map((courier) => (
                  <CourierCard
                    key={courier.id}
                    courier={courier}
                    selected={selectedCourierId === courier.id}
                    onSelect={() => setSelectedCourierId(courier.id)}
                  />
                ))}
              </div>
            )}
          </div>

          {selectedCourierId && (
            <div className="flex flex-col gap-4 border-t border-stone-100 pt-5 dark:border-stone-800">
              <Input
                label={t("delivery:assign.deliveryFee")}
                type="number"
                min="0"
                step="0.01"
                value={deliveryFee}
                onChange={(e) => setDeliveryFee(e.target.value)}
              />
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <Input
                  label={t("delivery:assign.estimatedPickup")}
                  type="datetime-local"
                  value={estimatedPickupAt}
                  onChange={(e) => setEstimatedPickupAt(e.target.value)}
                />
                <Input
                  label={t("delivery:assign.estimatedDelivery")}
                  type="datetime-local"
                  value={estimatedDeliveryAt}
                  onChange={(e) => setEstimatedDeliveryAt(e.target.value)}
                />
              </div>
              <Textarea
                label={t("delivery:assign.adminNote")}
                hint={t("delivery:assign.adminNoteHint")}
                rows={3}
                value={adminNote}
                onChange={(e) => setAdminNote(e.target.value)}
              />
            </div>
          )}
        </div>

        <div className="sticky bottom-0 flex items-center justify-end gap-3 border-t border-stone-100 bg-white p-4 dark:border-stone-800 dark:bg-stone-900">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("common:actions.cancel")}
          </Button>
          <Button type="button" loading={submitting} disabled={!selectedCourierId} onClick={handleSubmitClick} leftIcon={<Truck size={16} />}>
            {t("delivery:assign.submit")}
          </Button>
        </div>
      </Drawer>

      <ConfirmDialog
        open={confirmReplace}
        onClose={() => setConfirmReplace(false)}
        onConfirm={doSubmit}
        title={t("delivery:assign.replaceConfirmTitle")}
        description={t("delivery:assign.replaceConfirmDescription")}
        confirmLabel={t("delivery:assign.replaceConfirmAction")}
      />
    </>
  );
}

function CourierCard({ courier, selected, onSelect }: { courier: AvailableCourierDto; selected: boolean; onSelect: () => void }) {
  const { t } = useTranslation("delivery");
  return (
    <button
      type="button"
      onClick={onSelect}
      className={cn(
        "flex flex-col gap-3 rounded-2xl border-2 p-4 text-left transition",
        selected
          ? "border-grove-500 bg-grove-50 dark:border-grove-600 dark:bg-grove-950/30"
          : "border-stone-100 bg-white hover:border-stone-200 dark:border-stone-800 dark:bg-stone-900 dark:hover:border-stone-700",
      )}
    >
      <div className="flex items-center gap-3">
        <Avatar name={courier.fullName} src={courier.avatarUrl ? resolveMediaUrl(courier.avatarUrl) : undefined} size={44} />
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-semibold text-stone-900 dark:text-stone-50">{courier.fullName}</p>
          <p className="flex items-center gap-1 text-xs text-stone-400 dark:text-stone-500">
            <Phone size={11} /> {courier.phoneNumber}
          </p>
        </div>
        <span className={cn("shrink-0 rounded-full px-2 py-0.5 text-[11px] font-semibold", courier.isAvailable ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300" : "bg-stone-100 text-stone-500 dark:bg-stone-800 dark:text-stone-400")}>
          {courier.isAvailable ? t("assign.courierAvailable") : t("assign.courierBusy")}
        </span>
      </div>
      <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs text-stone-500 dark:text-stone-400">
        <span className="flex items-center gap-1">
          <Truck size={12} /> {courier.transportType} {courier.vehicleNumber}
        </span>
        <span className="flex items-center gap-1">
          <MapPin size={12} /> {courier.region}, {courier.district}
        </span>
        <span className="flex items-center gap-1">
          <Star size={12} className="text-harvest-500" fill="currentColor" /> {courier.rating.toFixed(1)}
        </span>
      </div>
      <div className="flex items-center gap-4 text-xs text-stone-400 dark:text-stone-500">
        <span>{t("assign.activeDeliveries", { count: courier.activeDeliveries })}</span>
        <span>{t("assign.completedDeliveries", { count: courier.completedDeliveries })}</span>
      </div>
    </button>
  );
}
