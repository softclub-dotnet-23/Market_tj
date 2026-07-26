import { useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Camera, Loader2, Trash2 } from "lucide-react";
import { useAuth } from "@/context/AuthContext";
import { ApiError } from "@/lib/api";

const MAX_SIZE_BYTES = 5 * 1024 * 1024;
const ALLOWED_TYPES = ["image/jpeg", "image/png", "image/webp"];

// Пункт в выпадающем меню аккаунта (Header/MobileMenu/AdminLayout/FarmerLayout/
// CustomerLayout — везде один и тот же аккаунт-дропдаун с именем/email/выходом)
// для загрузки или удаления аватарки. Один общий компонент вместо копирования
// логики загрузки в 5 разных топбаров.
export function AvatarMenuItem() {
  const { t } = useTranslation("common");
  const { user, uploadAvatar, removeAvatar } = useAuth();
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);

  if (!user) return null;

  const onChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;

    if (!ALLOWED_TYPES.includes(file.type)) {
      toast.error(t("avatar.invalidType"));
      return;
    }
    if (file.size > MAX_SIZE_BYTES) {
      toast.error(t("avatar.tooLarge"));
      return;
    }

    setBusy(true);
    try {
      await uploadAvatar(file);
      toast.success(t("avatar.uploadSuccess"));
    } catch (err) {
      toast.error(t("avatar.uploadError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setBusy(false);
    }
  };

  const onRemove = async () => {
    setBusy(true);
    try {
      await removeAvatar();
      toast.success(t("avatar.removeSuccess"));
    } catch (err) {
      toast.error(t("avatar.removeError"), { description: err instanceof ApiError ? err.message : undefined });
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="flex items-center gap-1">
      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="hidden"
        onChange={onChange}
        disabled={busy}
      />
      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        disabled={busy}
        className="flex flex-1 items-center gap-2.5 rounded-xl px-2.5 py-2 text-left text-sm text-stone-600 transition hover:bg-stone-50 disabled:opacity-60 dark:text-stone-300 dark:hover:bg-stone-800"
      >
        {busy ? <Loader2 size={15} className="animate-spin" /> : <Camera size={15} />}
        {user.avatarUrl ? t("avatar.changePhoto") : t("avatar.addPhoto")}
      </button>
      {user.avatarUrl && (
        <button
          type="button"
          onClick={onRemove}
          disabled={busy}
          aria-label={t("avatar.removePhoto")}
          className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 disabled:opacity-60 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
        >
          <Trash2 size={14} />
        </button>
      )}
    </div>
  );
}
