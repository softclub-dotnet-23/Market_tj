import { useTranslation } from "react-i18next";
import { MessageCircle } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { ConversationsList } from "@/components/chat/ConversationsList";
import { useAuth } from "@/context/AuthContext";
import { useCustomerOrders, useCustomerProfile } from "@/data/customer";
import { useFarmers } from "@/data/farmers";

export function CustomerMessages() {
  const { t } = useTranslation("customer");
  const { user } = useAuth();
  const { profile, loading: profileLoading, error: profileError } = useCustomerProfile();
  const { orders } = useCustomerOrders(profile?.id ?? null);
  const farmers = useFarmers();

  if (profileLoading) return <PageLoader />;

  if (profileError || !profile || !user) {
    return <EmptyState icon={<MessageCircle size={26} />} title={t("messages.errorTitle")} description={profileError ?? t("messages.errorDescription")} />;
  }

  const orderNumberById = new Map((orders ?? []).map((o) => [o.id, o.orderNumber]));
  const farmerByUserId = new Map(farmers.map((f) => [f.userId, f]));

  return (
    <ConversationsList
      ns="customer"
      currentUserId={user.userId}
      resolveOtherPartyName={(c) => farmerByUserId.get(c.farmerId)?.farmName ?? t("orders.farmerLabel", { id: c.farmerId })}
      resolveOrderNumber={(orderId) => (orderId ? (orderNumberById.get(orderId) ?? null) : null)}
    />
  );
}
