import { Phone } from "lucide-react";
import { FieldWrapper } from "@/components/ui/Field";
import { cn } from "@/lib/utils";

function formatDigits(digits: string) {
  return [digits.slice(0, 2), digits.slice(2, 5), digits.slice(5, 9)].filter(Boolean).join(" ");
}

// Код страны +992 зафиксирован и не редактируется — покупателю/фермеру
// остаётся ввести только сами 9 цифр номера, а не набирать код вручную.
export function PhoneInput({
  label,
  error,
  value,
  onChange,
}: {
  label?: string;
  error?: string;
  value: string;
  onChange: (value: string) => void;
}) {
  const digits = value.replace(/^\+992/, "").replace(/\D/g, "").slice(0, 9);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value.replace(/\D/g, "").slice(0, 9);
    onChange(raw ? `+992 ${formatDigits(raw)}` : "");
  };

  return (
    <FieldWrapper label={label} error={error}>
      <div
        className={cn(
          "flex h-11 items-center gap-2 rounded-xl border border-stone-200 bg-white pl-3.5 pr-4 transition focus-within:border-grove-500 focus-within:ring-2 focus-within:ring-grove-100 dark:border-stone-700 dark:bg-stone-900 dark:focus-within:ring-grove-900",
          error && "border-danger focus-within:border-danger focus-within:ring-red-100",
        )}
      >
        <Phone size={16} className="shrink-0 text-stone-400 dark:text-stone-500" />
        <span className="shrink-0 text-[15px] font-medium text-stone-500 dark:text-stone-400">+992</span>
        <span className="h-5 w-px shrink-0 bg-stone-200 dark:bg-stone-700" />
        <input
          type="tel"
          inputMode="numeric"
          value={formatDigits(digits)}
          onChange={handleChange}
          placeholder="__ ___ ____"
          className="min-w-0 flex-1 bg-transparent text-[15px] text-stone-900 outline-none placeholder:text-stone-400 dark:text-stone-100 dark:placeholder:text-stone-500"
        />
      </div>
    </FieldWrapper>
  );
}
