import { useTranslation } from "react-i18next";
import { Leaf } from "lucide-react";

export function PageLoader() {
  const { t } = useTranslation("common");
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-3">
      <span className="flex h-11 w-11 animate-pulse items-center justify-center rounded-xl bg-grove-700 text-white">
        <Leaf size={20} />
      </span>
      <span className="text-sm text-stone-400 dark:text-stone-500">{t("loading")}</span>
    </div>
  );
}
