import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { BadgeCheck, CalendarDays, MapPin, MessageCircle, Package, Sprout, Wallet as WalletIcon } from "lucide-react";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { EmptyState } from "@/components/ui/EmptyState";
import { RatingStars } from "@/components/ui/RatingStars";
import { Skeleton } from "@/components/ui/Skeleton";
import { PageLoader } from "@/components/layout/PageLoader";
import { ReviewsSection } from "@/components/product/ReviewsSection";
import { ChatModal } from "@/components/chat/ChatModal";
import { CARD_GRADIENTS, CardBrandMark } from "@/components/customer/WalletCard";
import { useFarmers } from "@/data/farmers";
import { useCatalogLoaded } from "@/data/products";
import { getCatalogReviewsByFarmerId } from "@/data/catalogStore";
import { CardType, useFarmerPaymentCard } from "@/data/wallet";
import { useAuth } from "@/context/AuthContext";
import { resolveMediaUrl } from "@/lib/api";
import { formatDate, cn } from "@/lib/utils";

function StatBlock({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-stone-100 bg-white p-4 dark:border-stone-800 dark:bg-stone-900">
      <p className="font-display text-xl text-stone-900 dark:text-stone-50">{value}</p>
      <p className="mt-0.5 text-xs text-stone-400 dark:text-stone-500">{label}</p>
    </div>
  );
}

// Компактная витрина "способ оплаты фермера" — только тип карты и последние
// 4 цифры (см. GetFarmerPaymentCardDto на бэкенде — намеренно не отдаёт ни
// имя держателя, ни баланс). Переиспользует тот же градиент/логотип, что и
// полноразмерная карта в личном кошельке (WalletCard), просто в свёрнутом виде.
function FarmerPaymentCardSection({ farmerUserId }: { farmerUserId: number }) {
  const { t } = useTranslation(["pages", "wallet"]);
  const { data: card, loading } = useFarmerPaymentCard(farmerUserId);

  return (
    <div className="mt-6 rounded-3xl border border-stone-100 bg-white p-6 dark:border-stone-800 dark:bg-stone-900">
      <h2 className="flex items-center gap-2 font-display text-lg text-stone-900 dark:text-stone-50">
        <WalletIcon size={17} className="text-grove-600 dark:text-grove-400" />
        {t("wallet:paymentCard.title")}
      </h2>

      {loading ? (
        <Skeleton className="mt-4 h-16 w-full max-w-xs" />
      ) : !card ? (
        <p className="mt-3 text-sm text-stone-500 dark:text-stone-400">{t("wallet:paymentCard.empty")}</p>
      ) : (
        <div
          className={cn(
            "mt-4 flex max-w-xs items-center justify-between rounded-2xl bg-linear-to-br p-4 text-white shadow-(--shadow-soft)",
            CARD_GRADIENTS[card.cardType] ?? CARD_GRADIENTS[CardType.Visa],
          )}
        >
          <div className="flex flex-col gap-1">
            <span className="text-[9px] font-semibold tracking-[0.15em] text-white/60 uppercase">{card.bankName}</span>
            <span className="font-mono text-sm tracking-[0.15em] text-white/95">•••• {card.cardNumberLast4}</span>
          </div>
          <CardBrandMark cardType={card.cardType} />
        </div>
      )}
    </div>
  );
}

export function FarmerPublicProfile() {
  const { t } = useTranslation(["pages", "product", "layout"]);
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const farmers = useFarmers();
  const catalogLoaded = useCatalogLoaded();
  const farmer = farmers.find((f) => f.id === Number(id));
  const [askFarmerOpen, setAskFarmerOpen] = useState(false);

  if (!farmer && !catalogLoaded) return <PageLoader />;

  if (!farmer) {
    return (
      <div className="container-page py-16">
        <EmptyState
          icon={<Sprout size={26} />}
          title={t("pages:farmerProfile.notFoundTitle")}
          description={t("pages:farmerProfile.notFoundDescription")}
          action={
            <Link to="/catalog">
              <Button variant="outline">{t("pages:productDetails.backToCatalog")}</Button>
            </Link>
          }
        />
      </div>
    );
  }

  const reviews = getCatalogReviewsByFarmerId(farmer.id);

  const handleAskFarmer = () => {
    if (!user) {
      toast.error(t("pages:productDetails.chatLoginRequired"));
      navigate("/login");
      return;
    }
    if (user.role !== "Customer") {
      toast.error(t("pages:productDetails.chatCustomersOnly"));
      return;
    }
    setAskFarmerOpen(true);
  };

  return (
    <div className="container-page py-8 sm:py-12">
      <Breadcrumbs items={[{ label: t("layout:nav.catalog"), to: "/catalog" }, { label: farmer.farmName }]} className="mb-6" />

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5 }}
        className="flex flex-col gap-5 rounded-3xl border border-stone-100 bg-white p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8 dark:border-stone-800 dark:bg-stone-900"
      >
        <div className="flex items-center gap-4">
          <Avatar name={farmer.farmName} src={farmer.avatarUrl ? resolveMediaUrl(farmer.avatarUrl) : undefined} size={84} ring />
          <div>
            <div className="flex items-center gap-2">
              <h1 className="font-display text-2xl text-stone-900 dark:text-stone-50">{farmer.farmName}</h1>
              {farmer.verified && (
                <span className="flex items-center gap-1 rounded-full bg-grove-50 px-2.5 py-1 text-xs font-semibold text-grove-700 dark:bg-grove-950 dark:text-grove-400">
                  <BadgeCheck size={13} />
                  {t("product:verified")}
                </span>
              )}
            </div>
            <p className="mt-1.5 flex items-center gap-1.5 text-sm text-stone-500 dark:text-stone-400">
              <MapPin size={14} />
              {farmer.district}, {farmer.region}
              {farmer.village ? `, ${farmer.village}` : ""}
            </p>
            <div className="mt-2">
              <RatingStars rating={farmer.rating} size={14} showValue reviewCount={farmer.reviewCount} />
            </div>
            {farmer.tags.length > 0 && (
              <div className="mt-2 flex flex-wrap gap-1.5">
                {farmer.tags.map((tag) => (
                  <Badge key={tag} variant="stone" className="text-[11px]">
                    {tag}
                  </Badge>
                ))}
              </div>
            )}
          </div>
        </div>
        <div className="flex shrink-0 flex-col gap-2 sm:flex-row">
          <Button variant="outline" leftIcon={<MessageCircle size={15} />} onClick={handleAskFarmer}>
            {t("pages:productDetails.askFarmer")}
          </Button>
          <Link to={`/catalog?farmer=${farmer.id}`}>
            <Button leftIcon={<Package size={15} />} className="w-full">
              {t("product:allProducts")}
            </Button>
          </Link>
        </div>
      </motion.div>

      <div className="mt-6 grid grid-cols-2 gap-4 sm:grid-cols-3">
        <StatBlock label={t("product:productsCount", { count: farmer.productCount })} value={String(farmer.productCount)} />
        <StatBlock label={t("pages:farmerProfile.yearsOnPlatform")} value={String(farmer.yearsActive)} />
        <StatBlock label={t("pages:farmerProfile.memberSince")} value={formatDate(farmer.joinedAt)} />
      </div>

      <div className="mt-6 rounded-3xl border border-stone-100 bg-white p-6 dark:border-stone-800 dark:bg-stone-900">
        <h2 className="flex items-center gap-2 font-display text-lg text-stone-900 dark:text-stone-50">
          <CalendarDays size={17} className="text-grove-600 dark:text-grove-400" />
          {t("pages:farmerProfile.aboutTitle")}
        </h2>
        <p className="mt-3 text-[15px] leading-relaxed text-stone-600 dark:text-stone-300">
          {farmer.bio || t("pages:farmerProfile.noDescription")}
        </p>
      </div>

      <FarmerPaymentCardSection farmerUserId={farmer.userId} />

      <div className="mt-10">
        <h2 className="mb-5 font-display text-xl text-stone-900 dark:text-stone-50">{t("pages:farmerProfile.reviewsTitle")}</h2>
        <ReviewsSection reviews={reviews} rating={farmer.rating} count={farmer.reviewCount} />
      </div>

      <ChatModal
        open={askFarmerOpen}
        onClose={() => setAskFarmerOpen(false)}
        orderId={null}
        orderNumber={null}
        customerUserId={user?.userId ?? null}
        farmerUserId={farmer.userId}
        currentUserId={user?.userId ?? 0}
        otherPartyName={farmer.farmName}
        ns="customer"
      />
    </div>
  );
}
