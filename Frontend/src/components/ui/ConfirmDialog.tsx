import { useState } from "react";
import { useTranslation } from "react-i18next";
import { AlertTriangle } from "lucide-react";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";

export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  title,
  description,
  confirmLabel,
  danger = true,
}: {
  open: boolean;
  onClose: () => void;
  onConfirm: () => Promise<void> | void;
  title: string;
  description?: string;
  confirmLabel?: string;
  danger?: boolean;
}) {
  const { t } = useTranslation("common");
  const [busy, setBusy] = useState(false);

  const handleConfirm = async () => {
    setBusy(true);
    try {
      await onConfirm();
      onClose();
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-md">
      <div className="flex flex-col items-center gap-4 text-center">
        <span
          className={`flex h-14 w-14 items-center justify-center rounded-2xl ${
            danger
              ? "bg-rose-100 text-rose-600 dark:bg-rose-950 dark:text-rose-400"
              : "bg-harvest-100 text-harvest-700 dark:bg-harvest-950 dark:text-harvest-300"
          }`}
        >
          <AlertTriangle size={24} />
        </span>
        <div className="flex flex-col gap-1.5">
          <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{title}</h2>
          {description && <p className="text-sm text-stone-500 dark:text-stone-400">{description}</p>}
        </div>
        <div className="mt-2 flex w-full gap-3">
          <Button type="button" variant="outline" className="flex-1" onClick={onClose} disabled={busy}>
            {t("actions.cancel")}
          </Button>
          <Button type="button" variant={danger ? "danger" : "primary"} className="flex-1" loading={busy} onClick={handleConfirm}>
            {confirmLabel ?? t("actions.confirm")}
          </Button>
        </div>
      </div>
    </Modal>
  );
}
