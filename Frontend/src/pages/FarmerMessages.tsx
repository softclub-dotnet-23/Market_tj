import { useTranslation } from "react-i18next";
import { MessageCircle } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { ConversationsList } from "@/components/chat/ConversationsList";
import { useAuth } from "@/context/AuthContext";
import { useFarmerOrders, useFarmerProfile } from "@/data/farmer";

export function FarmerMessages() {
  const { t } = useTranslation("farmer");
  const { user } = useAuth();
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const { orders } = useFarmerOrders(profile?.id ?? null);

  if (profileLoading) return <PageLoader />;

  if (profileError || !profile || !user) {
    return <EmptyState icon={<MessageCircle size={26} />} title={t("messages.errorTitle")} description={profileError ?? t("messages.errorDescription")} />;
  }

  const orderNumberById = new Map((orders ?? []).map((o) => [o.id, o.orderNumber]));

  return (
    <ConversationsList
      ns="farmer"
      currentUserId={user.userId}
      resolveOtherPartyName={(c) => t("orders.customerLabel", { id: c.customerId })}
      resolveOrderNumber={(orderId) => (orderId ? (orderNumberById.get(orderId) ?? null) : null)}
    />
  );
}
