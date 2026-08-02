import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Controller, useForm } from "react-hook-form";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { CreditCard, Wallet as WalletIcon } from "lucide-react";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Field";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { WalletCard } from "@/components/customer/WalletCard";
import {
  CardType,
  MAX_TOPUP_AMOUNT,
  WalletTransactionType,
  createWallet,
  topUpWallet,
  useMyWallet,
  useMyWalletTransactions,
  type WalletDto,
  type WalletTransactionDto,
} from "@/data/wallet";
import { ApiError } from "@/lib/api";
import { formatDate, formatSomoni } from "@/lib/utils";
import { cn } from "@/lib/utils";

const QUICK_TOPUP_AMOUNTS = [100, 500, 1000];

const CARD_TYPE_OPTIONS = [
  { value: CardType.Visa, label: "VISA" },
  { value: CardType.Mastercard, label: "Mastercard" },
  { value: CardType.UnionPay, label: "UnionPay" },
];

const TX_TYPE_LABEL_KEY: Record<number, string> = {
  [WalletTransactionType.TopUp]: "topUp",
  [WalletTransactionType.Purchase]: "purchase",
  [WalletTransactionType.Refund]: "refund",
  [WalletTransactionType.FarmerCredit]: "farmerCredit",
};

interface CreateCardFormValues {
  cardHolderFirstName: string;
  cardHolderLastName: string;
  cardType: number;
}

function CreateCardSection({ onCreated }: { onCreated: (wallet: WalletDto) => void }) {
  const { t } = useTranslation("wallet");
  const {
    register,
    handleSubmit,
    watch,
    control,
    formState: { errors, isSubmitting },
  } = useForm<CreateCardFormValues>({
    defaultValues: { cardHolderFirstName: "", cardHolderLastName: "", cardType: CardType.Visa },
  });
  const values = watch();

  const onSubmit = async (formValues: CreateCardFormValues) => {
    try {
      const wallet = await createWallet(formValues);
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
          cardType={values.cardType}
          firstName={values.cardHolderFirstName}
          lastName={values.cardHolderLastName}
          last4={null}
          createdAt={null}
          className="max-w-full"
        />
      </div>

      <Card>
        <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("createCard.title")}</h2>
        <p className="mt-1.5 text-sm text-stone-500 dark:text-stone-400">{t("createCard.description")}</p>

        <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
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

          <div>
            <label className="text-sm font-medium text-stone-700 dark:text-stone-300">{t("createCard.cardTypeLabel")}</label>
            <Controller
              name="cardType"
              control={control}
              render={({ field }) => (
                <div className="mt-2 grid grid-cols-3 gap-3">
                  {CARD_TYPE_OPTIONS.map((opt) => (
                    <button
                      key={opt.value}
                      type="button"
                      onClick={() => field.onChange(opt.value)}
                      className={cn(
                        "flex h-16 flex-col items-center justify-center gap-1.5 rounded-2xl border-2 text-xs font-semibold transition",
                        field.value === opt.value
                          ? "border-grove-600 bg-grove-50 text-grove-700 dark:border-grove-500 dark:bg-grove-950 dark:text-grove-300"
                          : "border-stone-200 text-stone-500 hover:border-stone-300 dark:border-stone-700 dark:text-stone-400 dark:hover:border-stone-600",
                      )}
                    >
                      <CreditCard size={18} />
                      {opt.label}
                    </button>
                  ))}
                </div>
              )}
            />
          </div>

          <Button type="submit" loading={isSubmitting} className="mt-2">
            {t("createCard.submit")}
          </Button>
        </form>
      </Card>
    </div>
  );
}

function TopUpForm({ onTopUp }: { onTopUp: (wallet: WalletDto) => void }) {
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
      const updated = await topUpWallet(value);
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

export function Wallet() {
  const { t } = useTranslation(["wallet", "common"]);
  const { data: fetchedWallet, loading: walletLoading, error: walletError } = useMyWallet();
  const [wallet, setWallet] = useState<WalletDto | null>(null);
  const [walletLoaded, setWalletLoaded] = useState(false);
  const [txRefreshKey, setTxRefreshKey] = useState(0);
  const { data: transactions, loading: txLoading } = useMyWalletTransactions(txRefreshKey);

  // Локальная копия кошелька — после создания карты/пополнения баланс
  // обновляется сразу из ответа API (оптимистичный UI), без повторного
  // GET /wallet и без "прыжка" интерфейса при перезагрузке страницы.
  useEffect(() => {
    if (!walletLoading) {
      setWallet(fetchedWallet);
      setWalletLoaded(true);
    }
  }, [walletLoading, fetchedWallet]);

  const handleCreated = (created: WalletDto) => setWallet(created);

  const handleToppedUp = (updated: WalletDto) => {
    setWallet(updated);
    setTxRefreshKey((k) => k + 1);
  };

  if (walletLoading || !walletLoaded) {
    return (
      <div className="flex flex-col gap-6">
        <Skeleton className="aspect-[1.586/1] w-full max-w-sm" />
        <Skeleton className="h-32 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  if (walletError) {
    return (
      <EmptyState icon={<WalletIcon size={26} />} title={t("wallet:loadError.title")} description={t("wallet:loadError.description")} />
    );
  }

  if (!wallet) {
    return <CreateCardSection onCreated={handleCreated} />;
  }

  return (
    <div className="grid grid-cols-1 items-start gap-8 lg:grid-cols-[1fr_1.15fr]">
      <div className="flex flex-col gap-6">
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.4 }}>
          <WalletCard
            cardType={wallet.cardType}
            firstName={wallet.cardHolderFirstName}
            lastName={wallet.cardHolderLastName}
            last4={wallet.cardNumberLast4}
            createdAt={wallet.createdAt}
            className="max-w-full"
          />
        </motion.div>

        <Card>
          <p className="text-sm text-stone-400 dark:text-stone-500">{t("wallet:balance.title")}</p>
          <p className="mt-1 font-display text-4xl text-stone-900 dark:text-stone-50">
            {formatSomoni(wallet.balance)} <span className="text-xl text-stone-400 dark:text-stone-500">{t("common:currencySomoni")}</span>
          </p>
        </Card>

        <TopUpForm onTopUp={handleToppedUp} />
      </div>

      <Card>
        <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("wallet:history.title")}</h2>
        <TransactionHistory transactions={transactions} loading={txLoading} />
      </Card>
    </div>
  );
}
