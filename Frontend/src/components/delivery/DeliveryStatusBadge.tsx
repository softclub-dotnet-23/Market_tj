import { useTranslation } from "react-i18next";
import { CheckCircle2, Clock, MapPin, Package, Truck, UserCheck, XCircle } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { DeliveryStatus } from "@/data/delivery";

const STATUS_META: Record<number, { key: string; icon: typeof Clock; variant: "stone" | "harvest" | "grove" | "success" | "danger" }> = {
  [DeliveryStatus.Pending]: { key: "notAssigned", icon: Clock, variant: "stone" },
  [DeliveryStatus.Assigned]: { key: "assigned", icon: UserCheck, variant: "harvest" },
  [DeliveryStatus.Accepted]: { key: "accepted", icon: UserCheck, variant: "harvest" },
  [DeliveryStatus.GoingToFarmer]: { key: "goingToFarmer", icon: Truck, variant: "harvest" },
  [DeliveryStatus.ArrivedAtFarmer]: { key: "arrivedAtFarmer", icon: MapPin, variant: "harvest" },
  [DeliveryStatus.PickedUp]: { key: "pickedUp", icon: Package, variant: "grove" },
  [DeliveryStatus.InTransit]: { key: "inTransit", icon: Truck, variant: "grove" },
  [DeliveryStatus.ArrivedAtClient]: { key: "arrivedAtClient", icon: MapPin, variant: "grove" },
  [DeliveryStatus.Delivered]: { key: "delivered", icon: CheckCircle2, variant: "success" },
  [DeliveryStatus.Cancelled]: { key: "cancelled", icon: XCircle, variant: "danger" },
};

export function DeliveryStatusBadge({ status, className }: { status: number | null | undefined; className?: string }) {
  const { t } = useTranslation("delivery");
  const meta = STATUS_META[status ?? DeliveryStatus.Pending] ?? STATUS_META[DeliveryStatus.Pending];
  const Icon = meta.icon;
  return (
    <Badge variant={meta.variant} icon={<Icon size={11} />} className={className}>
      {t(`status.${meta.key}`)}
    </Badge>
  );
}

export function deliveryStatusLabel(t: (key: string) => string, status: number | null | undefined): string {
  const meta = STATUS_META[status ?? DeliveryStatus.Pending] ?? STATUS_META[DeliveryStatus.Pending];
  return t(`delivery:status.${meta.key}`);
}
