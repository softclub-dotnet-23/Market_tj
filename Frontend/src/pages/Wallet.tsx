import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useForm } from "react-hook-form";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { CreditCard, Plus, Wallet as WalletIcon } from "lucide-react";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Field";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { WalletCard, CardBrandMark, CARD_GRADIENTS } from "@/components/customer/WalletCard";
import { WalletPinGate } from "@/components/customer/WalletPinGate";
import {
  CardType,
  MAX_CARDS_PER_USER,
  MAX_TOPUP_AMOUNT,
  WalletTransactionType,
  createWallet,
  detectCardType,
  formatCardNumberInput,
  formatExpiryInput,
  passesLuhnCheck,
  topUpWallet,
  useMyWallets,
  useWalletTransactions,
  type WalletDto,
  type WalletTransactionDto,
} from "@/data/wallet";
import { ApiError } from "@/lib/api";
import { formatDate, formatSomoni, cn } from "@/lib/utils";

const QUICK_TOPUP_AMOUNTS = [100, 500, 1000];

const TX_TYPE_LABEL_KEY: Record<number, string> = {
  [WalletTransactionType.TopUp]: "topUp",
  [WalletTransactionType.Purchase]: "purchase",
  [WalletTransactionType.Refund]: "refund",
  [WalletTransactionType.FarmerCredit]: "farmerCredit",
};

interface CreateCardFormValues {
  cardHolderFirstName: string;
  cardHolderLastName: string;
  cardNumber: string;
  cvv: string;
  expiry: string;
  bankName: string;
}

function CreateCardSection({ onCreated, onCancel, cancelable }: { onCreated: (wallet: WalletDto) => void; onCancel?: () => void; cancelable?: boolean }) {
  const { t } = useTranslation("wallet");
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<CreateCardFormValues>({
    defaultValues: { cardHolderFirstName: "", cardHolderLastName: "", cardNumber: "", cvv: "", expiry: "", bankName: "" },
  });
  const values = watch();
  const digitsOnly = values.cardNumber.replace(/\D/g, "");
  const previewCardType = detectCardType(digitsOnly) ?? CardType.Visa;
  const [expiryMonth, expiryYear] = values.expiry.split("/");

  const validateExpiry = (value: string): string | true => {
    const [mm, yy] = value.split("/");
    if (!mm || !yy || mm.length !== 2 || yy.length !== 2) return t("createCard.expiryFormat");
    const month = Number(mm);
    const year = 2000 + Number(yy);
    if (month < 1 || month > 12) return t("createCard.expiryFormat");
    const now = new Date();
    if (year < now.getFullYear() || (year === now.getFullYear() && month < now.getMonth() + 1)) {
      return t("createCard.expiryPast");
    }
    return true;
  };

  const onSubmit = async (formValues: CreateCardFormValues) => {
    try {
      const [mm, yy] = formValues.expiry.split("/");
      const wallet = await createWallet({
        cardHolderFirstName: formValues.cardHolderFirstName,
        cardHolderLastName: formValues.cardHolderLastName,
        cardNumber: formValues.cardNumber.replace(/\D/g, ""),
        cvv: formValues.cvv,
        expiryMonth: Number(mm),
        expiryYear: 2000 + Number(yy),
        bankName: formValues.bankName,
      });
      onCreated(wallet);
      toast.success(t("createCard.success"));
    } catch (err) {
      toast.error(t("createCard.error"), { description: err instanceof ApiError ? err.message : undefined });
    }
  };

  return (
    <div className="grid grid-cols-1 items-start gap-8 lg:grid-cols-[1fr_1fr]">
      <div className="flex flex-col items-center gap-4 lg:sticky lg:top-6">
        <WalletCard
          cardType={previewCardType}
          firstName={values.cardHolderFirstName}
          lastName={values.cardHolderLastName}
          last4={digitsOnly.length >= 4 ? digitsOnly.slice(-4) : null}
          createdAt={null}
          expiryMonth={expiryMonth ? Number(expiryMonth) : null}
          expiryYear={expiryYear ? 2000 + Number(expiryYear) : null}
          bankName={values.bankName || null}
          className="max-w-full"
        />
      </div>

      <Card>
        <div className="flex items-center justify-between">
          <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("createCard.title")}</h2>
          {cancelable && (
            <button type="button" onClick={onCancel} className="text-sm text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
              {t("createCard.cancel")}
            </button>
          )}
        </div>
        <p className="mt-1.5 text-sm text-stone-500 dark:text-stone-400">{t("createCard.description")}</p>

        <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <Input
              label={t("createCard.firstNameLabel")}
              placeholder={t("createCard.firstNamePlaceholder")}
              error={errors.cardHolderFirstName?.message}
              {...register("cardHolderFirstName", {
                required: t("createCard.firstNameRequired"),
                maxLength: { value: 100, message: t("createCard.firstNameRequired") },
              })}
            />
            <Input
              label={t("createCard.lastNameLabel")}
              placeholder={t("createCard.lastNamePlaceholder")}
              error={errors.cardHolderLastName?.message}
              {...register("cardHolderLastName", {
                required: t("createCard.lastNameRequired"),
                maxLength: { value: 100, message: t("createCard.lastNameRequired") },
              })}
            />
          </div>

          <div>
            <Input
              label={t("createCard.cardNumberLabel")}
              placeholder="0000 0000 0000 0000"
              inputMode="numeric"
              rightSlot={<CardBrandMarkSmall cardType={digitsOnly ? previewCardType : null} />}
              error={errors.cardNumber?.message}
              {...register("cardNumber", {
                required: t("createCard.cardNumberRequired"),
                onChange: (e) => setValue("cardNumber", formatCardNumberInput(e.target.value)),
                validate: (value) => {
                  const digits = value.replace(/\D/g, "");
                  if (digits.length !== 16) return t("createCard.cardNumberLength");
                  if (!passesLuhnCheck(digits)) return t("createCard.cardNumberInvalid");
                  if (!detectCardType(digits)) return t("createCard.cardNumberUnsupported");
                  return true;
                },
              })}
            />
          </div>

          <div className="grid grid-cols-2 gap-5">
            <Input
              label={t("createCard.expiryLabel")}
              placeholder="MM/YY"
              inputMode="numeric"
              error={errors.expiry?.message}
              {...register("expiry", {
                required: t("createCard.expiryFormat"),
                onChange: (e) => setValue("expiry", formatExpiryInput(e.target.value)),
                validate: validateExpiry,
              })}
            />
            <Input
              label={t("createCard.cvvLabel")}
              placeholder="***"
              type="password"
              inputMode="numeric"
              maxLength={3}
              error={errors.cvv?.message}
              {...register("cvv", {
                required: t("createCard.cvvRequired"),
                onChange: (e) => setValue("cvv", e.target.value.replace(/\D/g, "").slice(0, 3)),
                validate: (value) => value.length === 3 || t("createCard.cvvRequired"),
              })}
            />
          </div>

          <Input
            label={t("createCard.bankNameLabel")}
            placeholder={t("createCard.bankNamePlaceholder")}
            error={errors.bankName?.message}
            {...register("bankName", {
              required: t("createCard.bankNameRequired"),
              maxLength: { value: 200, message: t("createCard.bankNameRequired") },
            })}
          />

          <Button type="submit" loading={isSubmitting} className="mt-2">
            {t("createCard.submit")}
          </Button>
        </form>
      </Card>
    </div>
  );
}

function CardBrandMarkSmall({ cardType }: { cardType: number | null }) {
  if (!cardType) return null;
  return (
    <span className="scale-75 opacity-80">
      <CardBrandMark cardType={cardType} />
    </span>
  );
}

function TopUpForm({ wallet, onTopUp }: { wallet: WalletDto; onTopUp: (wallet: WalletDto) => void }) {
  const { t } = useTranslation("wallet");
  const [amount, setAmount] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Дублирует правила WalletValidator.ValidateTopUp на бэкенде — не
  // полагаемся только на серверную проверку, пользователь должен увидеть
  // ошибку мгновенно, а не после round-trip к API.
  const validate = (value: number): string | null => {
    if (!value || value <= 0) return t("topUpForm.amountPositive");
    if (Math.round(value * 100) / 100 !== value) return t("topUpForm.amountDecimals");
    if (value > MAX_TOPUP_AMOUNT) return t("topUpForm.amountMax", { max: MAX_TOPUP_AMOUNT });
    return null;
  };

  const submit = async (value: number) => {
    const validationError = validate(value);
    if (validationError) {
      setError(validationError);
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      const updated = await topUpWallet(wallet.id, value);
      onTopUp(updated);
      setAmount("");
      toast.success(t("topUpForm.success", { amount: formatSomoni(value) }));
    } catch (err) {
      toast.error(t("topUpForm.error"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card>
      <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("topUpForm.title")}</h2>
      <form
        onSubmit={(e) => {
          e.preventDefault();
          void submit(Number(amount));
        }}
        className="mt-4 flex flex-col gap-4"
      >
        <Input
          type="number"
          step="0.01"
          min="0"
          inputMode="decimal"
          label={t("topUpForm.amountLabel")}
          placeholder={t("topUpForm.amountPlaceholder")}
          value={amount}
          onChange={(e) => {
            setAmount(e.target.value);
            setError(null);
          }}
          error={error ?? undefined}
        />
        <div className="flex flex-wrap gap-2">
          {QUICK_TOPUP_AMOUNTS.map((quick) => (
            <button
              key={quick}
              type="button"
              onClick={() => {
                setAmount(String(quick));
                setError(null);
              }}
              className="rounded-full border border-stone-200 px-3.5 py-1.5 text-sm font-medium text-stone-600 transition hover:border-grove-500 hover:text-grove-700 dark:border-stone-700 dark:text-stone-300 dark:hover:border-grove-500 dark:hover:text-grove-400"
            >
              +{quick}
            </button>
          ))}
        </div>
        <Button type="submit" loading={submitting} disabled={!amount}>
          {t("topUpForm.submit")}
        </Button>
      </form>
    </Card>
  );
}

function TransactionHistory({ transactions, loading }: { transactions: WalletTransactionDto[] | null; loading: boolean }) {
  const { t } = useTranslation(["wallet", "common"]);

  if (loading) {
    return (
      <div className="mt-4 flex flex-col gap-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-14 w-full" />
        ))}
      </div>
    );
  }

  if (!transactions || transactions.length === 0) {
    return <p className="mt-8 py-8 text-center text-sm text-stone-400 dark:text-stone-500">{t("wallet:history.empty")}</p>;
  }

  return (
    <ul className="mt-4 flex flex-col divide-y divide-stone-100 dark:divide-stone-800">
      {transactions.map((tx) => {
        const positive = tx.amount >= 0;
        return (
          <li key={tx.id} className="flex items-center justify-between py-3.5">
            <div>
              <p className="text-sm font-medium text-stone-800 dark:text-stone-100">
                {t(`wallet:history.types.${TX_TYPE_LABEL_KEY[tx.type]}`)}
              </p>
              <p className="text-xs text-stone-400 dark:text-stone-500">
                {formatDate(tx.createdAt)}
                {tx.relatedOrderId ? ` · ${t("wallet:history.orderLabel", { id: tx.relatedOrderId })}` : ""}
              </p>
            </div>
            <span className={cn("font-display text-base", positive ? "text-success" : "text-stone-700 dark:text-stone-300")}>
              {positive ? "+" : "−"}
              {formatSomoni(Math.abs(tx.amount))} {t("common:currencySomoni")}
            </span>
          </li>
        );
      })}
    </ul>
  );
}

// Маленькая плитка карты в сетке слева — компактный вариант WalletCard, без
// полной иллюстрации, чтобы несколько карт помещались в ряд.
function CardTile({ wallet, active, onClick }: { wallet: WalletDto; active: boolean; onClick: () => void }) {
  const { t } = useTranslation("wallet");
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "flex flex-col gap-3 rounded-2xl bg-linear-to-br p-4 text-left text-white shadow-sm transition",
        CARD_GRADIENTS[wallet.cardType] ?? CARD_GRADIENTS[CardType.Visa],
        active ? "ring-2 ring-grove-500 ring-offset-2 dark:ring-offset-stone-950" : "opacity-80 hover:opacity-100",
      )}
    >
      <div className="flex items-center justify-between">
        <span className="text-[9px] font-semibold tracking-[0.15em] text-white/60 uppercase">{wallet.bankName}</span>
        <CardBrandMarkSmall cardType={wallet.cardType} />
      </div>
      <span className="font-mono text-sm tracking-[0.1em] text-white/95">•••• {wallet.cardNumberLast4}</span>
      <span className="font-display text-lg text-white/95">
        {formatSomoni(wallet.balance)} <span className="text-xs text-white/60">{t("balance.currency")}</span>
      </span>
    </button>
  );
}

function AddCardTile({ onClick }: { onClick: () => void }) {
  const { t } = useTranslation("wallet");
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex min-h-[132px] flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-stone-200 text-stone-400 transition hover:border-grove-400 hover:text-grove-600 dark:border-stone-700 dark:text-stone-500 dark:hover:border-grove-500 dark:hover:text-grove-400"
    >
      <Plus size={20} />
      <span className="text-xs font-semibold">{t("cardsList.addCard")}</span>
    </button>
  );
}

// Раздел "Кошелёк" защищён PIN-кодом (2026-08-03) — WalletPinGate спрашивает
// PIN при каждом заходе (первый раз просит установить) и рендерит содержимое
// кошелька только после успешной проверки в этой же сессии. Сам компонент
// с балансом/картами не изменился внутри — просто обёрнут снаружи.
export function Wallet() {
  return (
    <WalletPinGate>
      <WalletContent />
    </WalletPinGate>
  );
}

function WalletContent() {
  const { t } = useTranslation(["wallet", "common"]);
  const { data: fetchedWallets, loading: walletsLoading, error: walletsError } = useMyWallets();
  const [wallets, setWallets] = useState<WalletDto[] | null>(null);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [showAddCard, setShowAddCard] = useState(false);
  const [txRefreshKey, setTxRefreshKey] = useState(0);

  // Локальная копия списка карт — после создания/пополнения баланс
  // обновляется сразу из ответа API (оптимистичный UI), без повторного
  // GET /wallet и без "прыжка" интерфейса.
  useEffect(() => {
    if (fetchedWallets) {
      setWallets(fetchedWallets);
      setSelectedId((prev) => prev ?? fetchedWallets[0]?.id ?? null);
    }
  }, [fetchedWallets]);

  const selectedWallet = wallets?.find((w) => w.id === selectedId) ?? null;
  const { data: transactions, loading: txLoading } = useWalletTransactions(selectedWallet?.id ?? null, txRefreshKey);

  const handleCreated = (created: WalletDto) => {
    setWallets((prev) => [...(prev ?? []), created]);
    setSelectedId(created.id);
    setShowAddCard(false);
  };

  const handleToppedUp = (updated: WalletDto) => {
    setWallets((prev) => prev?.map((w) => (w.id === updated.id ? updated : w)) ?? [updated]);
    setTxRefreshKey((k) => k + 1);
  };

  if (walletsLoading || wallets === null) {
    return (
      <div className="flex flex-col gap-6">
        <Skeleton className="aspect-[1.586/1] w-full max-w-sm" />
        <Skeleton className="h-32 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  if (walletsError) {
    return (
      <EmptyState icon={<WalletIcon size={26} />} title={t("wallet:loadError.title")} description={t("wallet:loadError.description")} />
    );
  }

  if (wallets.length === 0 || showAddCard) {
    return <CreateCardSection onCreated={handleCreated} onCancel={() => setShowAddCard(false)} cancelable={wallets.length > 0} />;
  }

  return (
    <div className="grid grid-cols-1 items-start gap-8 lg:grid-cols-[1fr_1.15fr]">
      <div className="flex flex-col gap-6">
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-2">
          {wallets.map((w) => (
            <CardTile key={w.id} wallet={w} active={w.id === selectedId} onClick={() => setSelectedId(w.id)} />
          ))}
          {wallets.length < MAX_CARDS_PER_USER && <AddCardTile onClick={() => setShowAddCard(true)} />}
        </div>
        {wallets.length >= MAX_CARDS_PER_USER && (
          <p className="text-center text-xs text-stone-400 dark:text-stone-500">{t("cardsList.maxReached", { max: MAX_CARDS_PER_USER })}</p>
        )}

        {selectedWallet && (
          <motion.div key={selectedWallet.id} initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.4 }}>
            <WalletCard
              cardType={selectedWallet.cardType}
              firstName={selectedWallet.cardHolderFirstName}
              lastName={selectedWallet.cardHolderLastName}
              last4={selectedWallet.cardNumberLast4}
              createdAt={selectedWallet.createdAt}
              expiryMonth={selectedWallet.expiryMonth}
              expiryYear={selectedWallet.expiryYear}
              bankName={selectedWallet.bankName}
              className="max-w-full"
            />
          </motion.div>
        )}

        {selectedWallet && (
          <Card>
            <p className="text-sm text-stone-400 dark:text-stone-500">{t("wallet:balance.title")}</p>
            <p className="mt-1 font-display text-4xl text-stone-900 dark:text-stone-50">
              {formatSomoni(selectedWallet.balance)}{" "}
              <span className="text-xl text-stone-400 dark:text-stone-500">{t("common:currencySomoni")}</span>
            </p>
          </Card>
        )}

        {selectedWallet && <TopUpForm wallet={selectedWallet} onTopUp={handleToppedUp} />}
      </div>

      <Card>
        <h2 className="flex items-center gap-2 font-display text-lg text-stone-900 dark:text-stone-50">
          <CreditCard size={17} className="text-grove-600 dark:text-grove-400" />
          {t("wallet:history.title")}
        </h2>
        <TransactionHistory transactions={transactions} loading={txLoading} />
      </Card>
    </div>
  );
}
