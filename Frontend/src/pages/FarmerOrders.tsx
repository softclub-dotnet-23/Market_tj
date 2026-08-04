import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Banknote, CheckCircle2, Phone, ShoppingCart, Truck } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Button } from "@/components/ui/Button";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { ViewModeToggle, type OrdersViewMode } from "@/components/ui/ViewModeToggle";
import { StatusMenu } from "@/components/ui/StatusMenu";
import { OrderItemsCell } from "@/components/ui/OrderItemsCell";
import { OrderItemsPhotoList } from "@/components/ui/OrderItemsPhotoList";
import { PaymentBadge } from "@/components/ui/PaymentBadge";
import { DeliveryStatusBadge } from "@/components/delivery/DeliveryStatusBadge";
import { AssignCourierDrawer } from "@/components/delivery/AssignCourierDrawer";
import { ApiError } from "@/lib/api";
import { markReadyForPickup, useDeliveryByOrder } from "@/data/delivery";
import { formatDateTime, formatSomoni } from "@/lib/utils";
import {
  ORDER_STATUS_CLASSES,
  ORDER_STATUS_ICONS,
  ORDER_STATUS_KEYS,
  OrderStatus,
  getFarmerNextStatuses,
  resolveReceivedAt,
} from "@/lib/orderStatus";
import {
  OrderPaymentMethod,
  markOrderPaid,
  notifyFarmerOrdersChanged,
  updateFarmerOrderStatus,
  useDeliveriesByOrder,
  useFarmerOrders,
  useFarmerProfile,
  useOrderItems,
  type DeliveryDto,
  type FarmerOrderDto,
} from "@/data/farmer";
import { useProducts } from "@/data/products";
import { useOrderSequenceMap } from "@/data/orderSequence";

// Кратно 3 — карточный вид на десктопе всегда 3 колонки (см. xl:grid-cols-3
// ниже), так последняя строка страницы не остаётся неполной/одинокой.
const PAGE_SIZE = 9;

// Тот же цветной акцент карточки заказа по статусу, что и в админке/у
// покупателя (см. AdminOrders.tsx/CustomerOrders.tsx).
const ORDER_STATUS_ACCENT: Record<number, string> = {
  [OrderStatus.Pending]: "border-l-stone-300 bg-stone-50/70 dark:border-l-stone-600 dark:bg-stone-800/40",
  [OrderStatus.Accepted]: "border-l-blue-400 bg-blue-50/70 dark:border-l-blue-600 dark:bg-blue-950/30",
  [OrderStatus.Rejected]: "border-l-rose-400 bg-rose-50/60 dark:border-l-rose-600 dark:bg-rose-950/30",
  [OrderStatus.Preparing]: "border-l-harvest-400 bg-harvest-50/70 dark:border-l-harvest-600 dark:bg-harvest-900/30",
  [OrderStatus.ReadyForPickup]: "border-l-harvest-400 bg-harvest-50/70 dark:border-l-harvest-600 dark:bg-harvest-900/30",
  [OrderStatus.CourierAssigned]: "border-l-blue-400 bg-blue-50/70 dark:border-l-blue-600 dark:bg-blue-950/30",
  [OrderStatus.PickedUp]: "border-l-blue-400 bg-blue-50/70 dark:border-l-blue-600 dark:bg-blue-950/30",
  [OrderStatus.InDelivery]: "border-l-blue-400 bg-blue-50/70 dark:border-l-blue-600 dark:bg-blue-950/30",
  [OrderStatus.Delivered]: "border-l-grove-400 bg-grove-50/70 dark:border-l-grove-600 dark:bg-grove-950/30",
  [OrderStatus.Completed]: "border-l-grove-400 bg-grove-50/70 dark:border-l-grove-600 dark:bg-grove-950/30",
  [OrderStatus.Cancelled]: "border-l-rose-400 bg-rose-50/60 dark:border-l-rose-600 dark:bg-rose-950/30",
};

export function FarmerOrders() {
  const { t } = useTranslation(["farmer", "common"]);
  const [page, setPage] = useState(1);
  const [viewMode, setViewMode] = useState<OrdersViewMode>("table");
  const [busyId, setBusyId] = useState<number | null>(null);
  const [markingPaidId, setMarkingPaidId] = useState<number | null>(null);
  const [readyConfirmOrder, setReadyConfirmOrder] = useState<FarmerOrderDto | null>(null);
  const [assigningOrder, setAssigningOrder] = useState<FarmerOrderDto | null>(null);
  const [assignRefreshKey, setAssignRefreshKey] = useState(0);
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const { orders, loading: ordersLoading, error: ordersError } = useFarmerOrders(profile?.id ?? null);
  const { deliveriesByOrderId, loading: deliveriesLoading } = useDeliveriesByOrder();
  const { delivery: assigningDelivery } = useDeliveryByOrder(assigningOrder?.id ?? null, assignRefreshKey);
  const { orderItems } = useOrderItems();
  const products = useProducts();
  const photoByListingId = new Map(products.map((p) => [p.id, p.photoUrl]));
  const sequenceById = useOrderSequenceMap();

  const handleStatusChange = async (order: FarmerOrderDto, status: number) => {
    if (status === order.status) return;
    setBusyId(order.id);
    try {
      await updateFarmerOrderStatus(order, status);
      toast.success(t("orders.updateSuccess"));
      notifyFarmerOrdersChanged();
    } catch (err) {
      toast.error(t("orders.updateError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setBusyId(null);
    }
  };

  const handleMarkPaid = async (order: FarmerOrderDto) => {
    setMarkingPaidId(order.id);
    try {
      await markOrderPaid(order.id);
      toast.success(t("common:payment.markPaidSuccess"));
      notifyFarmerOrdersChanged();
    } catch (err) {
      toast.error(t("common:payment.markPaidError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setMarkingPaidId(null);
    }
  };

  const handleMarkReady = async () => {
    if (!readyConfirmOrder) return;
    try {
      await markReadyForPickup(readyConfirmOrder.id);
      toast.success(t("orders.delivery.readySuccess"));
      notifyFarmerOrdersChanged();
      setReadyConfirmOrder(null);
    } catch (err) {
      toast.error(t("orders.delivery.readyError"), { description: err instanceof ApiError ? err.message : undefined });
    }
  };

  if (profileLoading || (profile && (ordersLoading || deliveriesLoading))) return <PageLoader />;

  if (profileError || ordersError || !profile || !orders) {
    return (
      <EmptyState
        icon={<ShoppingCart size={26} />}
        title={t("orders.errorTitle")}
        description={profileError ?? ordersError ?? t("orders.errorDescription")}
      />
    );
  }

  if (orders.length === 0) {
    return <EmptyState icon={<ShoppingCart size={26} />} title={t("orders.emptyTitle")} description={t("orders.emptyDescription")} />;
  }

  const totalPages = Math.max(1, Math.ceil(orders.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: FarmerOrderDto[] = orders.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const deliveryInfo = (order: FarmerOrderDto, delivery: DeliveryDto | undefined) => {
    // Фермер назначает курьера сам (2026-08-04) — раньше здесь было только
    // "waitingForAdmin" (ждём, пока админ назначит), теперь активная кнопка.
    // Admin по-прежнему может назначить/переназначить из своей панели как
    // запасной вариант — обе стороны используют один и тот же
    // AssignCourierAsync-эндпоинт с разными правами владения.
    if (!delivery || !delivery.courierId) {
      return (
        <Button type="button" size="sm" variant="outline" leftIcon={<Truck size={14} />} onClick={() => setAssigningOrder(order)}>
          {t("orders.delivery.assignAction")}
        </Button>
      );
    }

    const canMarkReady = order.status === OrderStatus.CourierAssigned;

    return (
      <div className="flex flex-col gap-2 rounded-xl border border-stone-100 bg-stone-25 p-3 text-sm dark:border-stone-800 dark:bg-stone-950/40">
        <div className="flex items-center justify-between gap-2">
          <div className="min-w-0">
            <p className="truncate font-medium text-stone-800 dark:text-stone-100">{delivery.courierFullName ?? t("orders.courierLabel", { id: delivery.courierId })}</p>
            {delivery.courierPhoneNumber && (
              <a href={`tel:${delivery.courierPhoneNumber}`} className="flex items-center gap-1 text-xs text-grove-700 hover:underline dark:text-grove-400">
                <Phone size={11} /> {delivery.courierPhoneNumber}
              </a>
            )}
          </div>
          <DeliveryStatusBadge status={delivery.status} />
        </div>
        {delivery.courierTransportType && (
          <p className="flex items-center gap-1 text-xs text-stone-500 dark:text-stone-400">
            <Truck size={11} /> {delivery.courierTransportType} {delivery.courierVehicleNumber}
          </p>
        )}
        {delivery.adminNote && <p className="text-xs text-stone-500 dark:text-stone-400">{delivery.adminNote}</p>}
        {canMarkReady && (
          <Button type="button" size="sm" leftIcon={<CheckCircle2 size={14} />} onClick={() => setReadyConfirmOrder(order)}>
            {t("orders.delivery.readyAction")}
          </Button>
        )}
      </div>
    );
  };

  const paymentCell = (order: FarmerOrderDto) => (
    <div className="flex flex-col items-start gap-1.5">
      <PaymentBadge paymentMethod={order.paymentMethod} isPaid={order.isPaid} />
      {order.paymentMethod === OrderPaymentMethod.CashOnDelivery && !order.isPaid && (
        <Button
          size="sm"
          variant="outline"
          leftIcon={<Banknote size={13} />}
          loading={markingPaidId === order.id}
          onClick={() => handleMarkPaid(order)}
        >
          {t("common:payment.markPaid")}
        </Button>
      )}
    </div>
  );

  const statusCell = (order: FarmerOrderDto, receivedAt: string | null) => (
    <div className="flex flex-col items-start gap-1.5">
      <StatusMenu
        value={order.status}
        busy={busyId === order.id}
        lockedLabel={t("orders.statusLockedHint")}
        onChange={(status) => handleStatusChange(order, status)}
        options={[order.status, ...getFarmerNextStatuses(order.status)].map((s) => ({
          value: s,
          label: t(`orders.status.${ORDER_STATUS_KEYS[s]}`),
          className: ORDER_STATUS_CLASSES[s],
          icon: ORDER_STATUS_ICONS[s],
        }))}
      />
      <span className="text-xs text-stone-400 dark:text-stone-500">
        {receivedAt ? formatDateTime(receivedAt) : t("orders.notReceivedYet")}
      </span>
    </div>
  );

  const renderCard = (order: FarmerOrderDto) => {
    const delivery = deliveriesByOrderId.get(order.id);
    const receivedAt = resolveReceivedAt(order.status, order.completedAt, delivery?.deliveredAt);
    const items = orderItems?.filter((i) => i.orderId === order.id) ?? [];
    return (
      <div
        key={order.id}
        className={`flex flex-col gap-3 rounded-2xl border border-l-4 border-stone-100 p-5 shadow-sm transition hover:shadow-md dark:border-stone-800 ${ORDER_STATUS_ACCENT[order.status] ?? ""}`}
      >
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-3">
            <span
              className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white font-display text-sm font-semibold text-stone-800 shadow-sm dark:bg-stone-900 dark:text-stone-100"
              title={order.orderNumber}
            >
              {sequenceById.get(order.id) ?? "—"}
            </span>
            <p className="text-xs text-stone-400 dark:text-stone-500">{formatDateTime(order.createdAt)}</p>
          </div>
          <p className="font-display text-lg font-semibold text-grove-700 dark:text-grove-400">
            {formatSomoni(order.totalAmount)} {t("common.somoni")}
          </p>
        </div>
        <p className="truncate text-sm text-stone-600 dark:text-stone-300" title={order.customerFullName ?? undefined}>
          <span className="text-stone-400 dark:text-stone-500">{t("orders.columns.customer")}: </span>
          {order.customerFullName ?? t("orders.customerLabel", { id: order.customerId })}
        </p>
        <div className="border-t border-stone-900/5 pt-3 dark:border-stone-100/5">
          <OrderItemsPhotoList items={items} photoByListingId={photoByListingId} />
        </div>
        <div className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 border-t border-stone-900/5 pt-3 text-sm dark:border-stone-100/5">
          <span className="text-stone-400 dark:text-stone-500">{t("orders.columns.address")}</span>
          <span className="text-stone-700 dark:text-stone-200">
            {order.region}, {order.district}
          </span>
        </div>
        <div className="border-t border-stone-900/5 pt-3 dark:border-stone-100/5">{deliveryInfo(order, delivery)}</div>
        <div className="flex flex-wrap items-center justify-between gap-3 pt-1">
          {statusCell(order, receivedAt)}
          {paymentCell(order)}
        </div>
      </div>
    );
  };

  return (
    <div className="overflow-hidden rounded-3xl border border-stone-100 bg-linear-to-b from-white to-stone-50/60 shadow-(--shadow-soft) dark:border-stone-800 dark:from-stone-900 dark:to-stone-900">
      <div className="hidden items-center justify-end border-b border-stone-100 p-4 lg:flex dark:border-stone-800">
        <ViewModeToggle value={viewMode} onChange={setViewMode} ns="farmer" />
      </div>

      {/* Десктоп/планшет — компактная таблица (6 колонок), без
          горизонтального скролла на типичном ноутбучном экране. */}
      <div className={viewMode === "table" ? "hidden overflow-x-auto lg:block" : "hidden"}>
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
              <th className="px-6 py-4 font-medium">{t("orders.columns.orderNumber")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.customer")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.products")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.address")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.amount")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.status")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.payment")}</th>
              <th className="px-6 py-4 font-medium">{t("orders.columns.delivery")}</th>
            </tr>
          </thead>
          <tbody>
            {pageItems.map((order) => {
              const delivery = deliveriesByOrderId.get(order.id);
              const receivedAt = resolveReceivedAt(order.status, order.completedAt, delivery?.deliveredAt);
              const items = orderItems?.filter((i) => i.orderId === order.id) ?? [];
              return (
                <tr key={order.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                  <td className="px-6 py-4">
                    <span className="font-medium text-stone-800 dark:text-stone-100" title={order.orderNumber}>
                      {sequenceById.get(order.id) ?? "—"}
                    </span>
                    <span className="mt-0.5 block text-xs text-stone-400 dark:text-stone-500">{formatDateTime(order.createdAt)}</span>
                  </td>
                  <td
                    className="max-w-40 truncate px-6 py-4 text-stone-600 dark:text-stone-300"
                    title={order.customerFullName ?? undefined}
                  >
                    {order.customerFullName ?? t("orders.customerLabel", { id: order.customerId })}
                  </td>
                  <td className="px-6 py-4 text-stone-500 dark:text-stone-400">
                    <OrderItemsCell items={items} ns="farmer" />
                  </td>
                  <td className="max-w-40 truncate px-6 py-4 text-stone-500 dark:text-stone-400">
                    {order.region}, {order.district}
                  </td>
                  <td className="px-6 py-4 font-semibold text-stone-800 dark:text-stone-100">
                    {formatSomoni(order.totalAmount)} {t("common.somoni")}
                  </td>
                  <td className="px-6 py-4">{statusCell(order, receivedAt)}</td>
                  <td className="px-6 py-4">{paymentCell(order)}</td>
                  <td className="max-w-56 px-6 py-4">{deliveryInfo(order, delivery)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Десктоп — карточки вместо таблицы, если выбрано в переключателе выше. */}
      {viewMode === "cards" && (
        <div className="hidden grid-cols-2 gap-4 p-5 lg:grid xl:grid-cols-3">{pageItems.map(renderCard)}</div>
      )}

      {/* Мобильный/узкий экран — карточки вместо таблицы всегда, с фото товаров. */}
      <div className="grid grid-cols-1 gap-4 p-5 lg:hidden">{pageItems.map(renderCard)}</div>

      {totalPages > 1 && (
        <div className="border-t border-stone-100 p-4 dark:border-stone-800">
          <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
        </div>
      )}

      <ConfirmDialog
        open={!!readyConfirmOrder}
        onClose={() => setReadyConfirmOrder(null)}
        onConfirm={handleMarkReady}
        danger={false}
        title={t("orders.delivery.readyConfirmTitle")}
        description={t("orders.delivery.readyConfirmDescription")}
        confirmLabel={t("orders.delivery.readyAction")}
      />

      {assigningOrder && (
        <AssignCourierDrawer
          variant="farmer"
          open={!!assigningOrder}
          onClose={() => setAssigningOrder(null)}
          order={{
            id: assigningOrder.id,
            orderNumber: assigningOrder.orderNumber,
            farmerName: profile.farmName,
            customerName: assigningOrder.customerFullName ?? t("orders.customerLabel", { id: assigningOrder.customerId }),
            pickupAddress: profile.address,
            deliveryAddress: assigningOrder.deliveryAddress,
            itemCount: orderItems?.filter((i) => i.orderId === assigningOrder.id).length ?? 0,
            deliveryRegion: assigningOrder.region,
            deliveryDistrict: assigningOrder.district,
            currentDeliveryPrice: assigningOrder.deliveryPrice,
          }}
          existingDelivery={assigningDelivery}
          onAssigned={() => {
            setAssignRefreshKey((k) => k + 1);
            notifyFarmerOrdersChanged();
          }}
        />
      )}
    </div>
  );
}
