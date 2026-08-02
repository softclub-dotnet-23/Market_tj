import { useTranslation } from "react-i18next";
import { motion } from "framer-motion";
import { CardType } from "@/data/wallet";
import { cn } from "@/lib/utils";

// Три разных, но родственных градиента — все в тёмный stone-900, к своему
// акцентному цвету из палитры Market.tj (см. index.css), чтобы карта
// выглядела частью общего дизайна, а не вставкой из чужого стиля, и при
// этом три типа карт визуально легко различались. Экспортируется — тот же
// градиент переиспользует компактная карточка "Способ оплаты фермера" на
// публичном профиле (см. FarmerPublicProfile.tsx), чтобы не разъезжаться
// визуально с личным кошельком.
export const CARD_GRADIENTS: Record<number, string> = {
  [CardType.Visa]: "from-stone-900 via-grove-900 to-grove-700",
  [CardType.Mastercard]: "from-stone-900 via-clay-600 to-clay-500",
  [CardType.UnionPay]: "from-stone-900 via-harvest-700 to-harvest-500",
};

export function CardBrandMark({ cardType }: { cardType: number }) {
  if (cardType === CardType.Mastercard) {
    return (
      <div className="flex items-center" aria-hidden>
        <span className="h-8 w-8 rounded-full bg-harvest-400/90" />
        <span className="-ml-3.5 h-8 w-8 rounded-full bg-clay-400/80 mix-blend-plus-lighter" />
      </div>
    );
  }
  if (cardType === CardType.UnionPay) {
    return (
      <span className="font-display text-lg font-semibold tracking-tight text-white/95">
        Union<span className="text-stone-900/70">Pay</span>
      </span>
    );
  }
  return <span className="font-display text-2xl font-semibold italic tracking-tight text-white/95">VISA</span>;
}

interface WalletCardProps {
  cardType: number;
  firstName: string;
  lastName: string;
  last4: string | null;
  createdAt: string | null;
  className?: string;
}

export function WalletCard({ cardType, firstName, lastName, last4, createdAt, className }: WalletCardProps) {
  const { t } = useTranslation("wallet");
  const holderName = `${firstName} ${lastName}`.trim();
  const openedLabel = createdAt
    ? new Date(createdAt).toLocaleDateString(undefined, { month: "2-digit", year: "2-digit" }).replace(/\//g, "/")
    : "--/--";

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.4 }}
      className={cn(
        "bg-noise relative aspect-[1.586/1] w-full max-w-sm overflow-hidden rounded-3xl bg-linear-to-br p-6 text-white shadow-(--shadow-lifted) sm:p-7",
        CARD_GRADIENTS[cardType] ?? CARD_GRADIENTS[CardType.Visa],
        className,
      )}
    >
      <div className="pointer-events-none absolute -right-10 -top-10 h-40 w-40 rounded-full bg-white/10 blur-2xl" />
      <div className="pointer-events-none absolute -bottom-14 -left-10 h-40 w-40 rounded-full bg-black/20 blur-3xl" />

      <div className="relative flex h-full flex-col justify-between">
        <div className="flex items-start justify-between">
          <span className="flex h-9 w-12 items-center justify-center rounded-lg bg-white/15 backdrop-blur-sm">
            <span className="h-5 w-8 rounded-[4px] bg-linear-to-br from-harvest-300/90 to-harvest-500/90" />
          </span>
          <span className="text-[10px] font-semibold tracking-[0.2em] text-white/60 uppercase">Market.tj</span>
        </div>

        <div>
          <p className="font-mono text-lg tracking-[0.15em] text-white/95 sm:text-xl">
            •••• •••• •••• {last4 ?? "····"}
          </p>
        </div>

        <div className="flex items-end justify-between gap-3">
          <div className="min-w-0">
            <p className="text-[9px] font-medium tracking-[0.15em] text-white/50 uppercase">
              {t("card.holderFallback")}
            </p>
            <p className="truncate text-sm font-medium tracking-wide text-white/95 uppercase sm:text-base">
              {holderName || t("card.holderFallback")}
            </p>
            <p className="mt-1 text-[9px] font-medium tracking-[0.15em] text-white/45 uppercase">
              {t("card.validFrom")} {openedLabel}
            </p>
          </div>
          <CardBrandMark cardType={cardType} />
        </div>
      </div>
    </motion.div>
  );
}
