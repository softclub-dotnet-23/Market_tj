import { useTranslation } from "react-i18next";
import { Banknote, CreditCard } from "lucide-react";
import { StatusBadge } from "@/components/ui/StatusBadge";

// Общий бейдж способа оплаты заказа — переиспользуется и в CustomerOrders,
// и в FarmerOrders (см. Order.PaymentMethod/IsPaid на бэкенде): Card всегда
// оплачен сразу; CashOnDelivery — либо "ожидает оплаты при получении", либо
// "оплачено наличными" после подтверждения (см. markOrderPaid).
export function PaymentBadge({ paymentMethod, isPaid }: { paymentMethod: number; isPaid: boolean }) {
  const { t } = useTranslation("common");

  if (paymentMethod === 2 /* CashOnDelivery */) {
    return isPaid ? (
      <StatusBadge
        label={t("payment.paidCash")}
        icon={Banknote}
        className="bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-400"
      />
    ) : (
      <StatusBadge
        label={t("payment.awaitingCash")}
        icon={Banknote}
        className="bg-harvest-50 text-harvest-700 dark:bg-harvest-950 dark:text-harvest-400"
      />
    );
  }

  return (
    <StatusBadge
      label={t("payment.paidByCard")}
      icon={CreditCard}
      className="bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-400"
    />
  );
}
