import { useEffect, useState } from "react";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Lock, ShieldCheck } from "lucide-react";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Field";
import { Skeleton } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { getWalletPinStatus, setWalletPin, verifyWalletPin } from "@/data/wallet";
import { ApiError } from "@/lib/api";

type GateStatus = "loading" | "needsSetup" | "needsEntry" | "unlocked" | "statusError";

// PIN подтверждается только в React state (не localStorage/sessionStorage) —
// по прямому требованию: перезагрузка страницы/новый заход в раздел должны
// снова спросить PIN. Раз состояние живёт в этом компоненте и он размонтируется
// при уходе со страницы Wallet (роут меняется), этого достаточно само по себе —
// отдельный флаг "разблокировано" нигде не персистится.
export function WalletPinGate({ children }: { children: ReactNode }) {
  const { t } = useTranslation("wallet");
  const [status, setStatus] = useState<GateStatus>("loading");

  useEffect(() => {
    let cancelled = false;
    getWalletPinStatus()
      .then((res) => {
        if (!cancelled) setStatus(res.isSet ? "needsEntry" : "needsSetup");
      })
      .catch(() => {
        if (!cancelled) setStatus("statusError");
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (status === "loading") {
    return (
      <div className="flex flex-col gap-6">
        <Skeleton className="aspect-[1.586/1] w-full max-w-sm" />
        <Skeleton className="h-32 w-full max-w-sm" />
      </div>
    );
  }

  if (status === "statusError") {
    return <EmptyState icon={<Lock size={26} />} title={t("pin.statusError")} description="" />;
  }

  if (status === "needsSetup") {
    return <PinSetupForm onSuccess={() => setStatus("unlocked")} />;
  }

  if (status === "needsEntry") {
    return <PinEntryForm onSuccess={() => setStatus("unlocked")} />;
  }

  return <>{children}</>;
}

interface SetupFormValues {
  pin: string;
  confirmPin: string;
  password: string;
}

function PinSetupForm({ onSuccess }: { onSuccess: () => void }) {
  const { t } = useTranslation("wallet");
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<SetupFormValues>({ defaultValues: { pin: "", confirmPin: "", password: "" } });
  const pin = watch("pin");

  const onSubmit = async (values: SetupFormValues) => {
    try {
      await setWalletPin(values.pin, values.password);
      toast.success(t("pin.setupSuccess"));
      onSuccess();
    } catch (err) {
      toast.error(t("pin.setupError"), { description: err instanceof ApiError ? err.message : undefined });
    }
  };

  return (
    <div className="flex justify-center">
      <Card className="w-full max-w-sm">
        <div className="flex flex-col items-center gap-2 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-grove-100 text-grove-700 dark:bg-grove-900/40 dark:text-grove-400">
            <ShieldCheck size={22} />
          </div>
          <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("pin.setupTitle")}</h2>
          <p className="text-sm text-stone-500 dark:text-stone-400">{t("pin.setupDescription")}</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-4">
          <Input
            label={t("pin.pinLabel")}
            type="password"
            inputMode="numeric"
            maxLength={4}
            error={errors.pin?.message}
            {...register("pin", {
              required: t("pin.pinLengthError"),
              onChange: (e) => {
                e.target.value = e.target.value.replace(/\D/g, "").slice(0, 4);
              },
              validate: (value) => value.length === 4 || t("pin.pinLengthError"),
            })}
          />
          <Input
            label={t("pin.confirmPinLabel")}
            type="password"
            inputMode="numeric"
            maxLength={4}
            error={errors.confirmPin?.message}
            {...register("confirmPin", {
              required: t("pin.pinLengthError"),
              onChange: (e) => {
                e.target.value = e.target.value.replace(/\D/g, "").slice(0, 4);
              },
              validate: (value) => value === pin || t("pin.pinMismatchError"),
            })}
          />
          <Input
            label={t("pin.passwordLabel")}
            type="password"
            placeholder={t("pin.passwordPlaceholder")}
            error={errors.password?.message}
            {...register("password", { required: true })}
          />
          <Button type="submit" loading={isSubmitting} className="mt-2">
            {t("pin.setupSubmit")}
          </Button>
        </form>
      </Card>
    </div>
  );
}

interface EntryFormValues {
  pin: string;
}

function PinEntryForm({ onSuccess }: { onSuccess: () => void }) {
  const { t } = useTranslation("wallet");
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<EntryFormValues>({ defaultValues: { pin: "" } });

  const onSubmit = async (values: EntryFormValues) => {
    try {
      await verifyWalletPin(values.pin);
      onSuccess();
    } catch (err) {
      // Бэкенд сам формулирует точный текст ("Неверный PIN, осталось попыток:
      // N" / "Слишком много неверных попыток, попробуйте через N мин.") —
      // показываем его как есть, не подменяем обобщённым t("pin.wrongPin"),
      // чтобы не терять число оставшихся попыток из требования задачи.
      const message = err instanceof ApiError ? err.message : t("pin.wrongPin");
      setError("pin", { message });
    }
  };

  return (
    <div className="flex justify-center">
      <Card className="w-full max-w-sm">
        <div className="flex flex-col items-center gap-2 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-grove-100 text-grove-700 dark:bg-grove-900/40 dark:text-grove-400">
            <Lock size={22} />
          </div>
          <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("pin.entryTitle")}</h2>
          <p className="text-sm text-stone-500 dark:text-stone-400">{t("pin.entryDescription")}</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-4">
          <Input
            label={t("pin.enterPinLabel")}
            type="password"
            inputMode="numeric"
            maxLength={4}
            autoFocus
            error={errors.pin?.message}
            {...register("pin", {
              required: true,
              onChange: (e) => {
                e.target.value = e.target.value.replace(/\D/g, "").slice(0, 4);
              },
            })}
          />
          <Button type="submit" loading={isSubmitting} className="mt-2">
            {t("pin.unlock")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
