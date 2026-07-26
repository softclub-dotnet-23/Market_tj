import { useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { FieldWrapper } from "@/components/ui/Field";
import { cn } from "@/lib/utils";

export function Autocomplete({
  label,
  placeholder,
  leftIcon,
  error,
  hint,
  value,
  onChange,
  options,
}: {
  label?: string;
  placeholder?: string;
  leftIcon?: ReactNode;
  error?: string;
  hint?: string;
  value: string;
  onChange: (value: string) => void;
  options: string[];
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function onClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  const query = value.trim().toLowerCase();
  const filtered = (query ? options.filter((o) => o.toLowerCase().includes(query)) : options).slice(0, 8);

  return (
    <FieldWrapper label={label} error={error} hint={hint}>
      <div ref={ref} className="relative">
        <div className="relative flex items-center">
          {leftIcon && <span className="pointer-events-none absolute left-3.5 text-stone-400 dark:text-stone-500">{leftIcon}</span>}
          <input
            type="text"
            value={value}
            onChange={(e) => {
              onChange(e.target.value);
              setOpen(true);
            }}
            onFocus={() => setOpen(true)}
            placeholder={placeholder}
            className={cn(
              "h-11 w-full rounded-xl border border-stone-200 bg-white px-4 text-[15px] text-stone-900 placeholder:text-stone-400 transition focus:border-grove-500 focus:ring-2 focus:ring-grove-100 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-100 dark:placeholder:text-stone-500 dark:focus:ring-grove-900",
              leftIcon && "pl-10",
              error && "border-danger focus:border-danger focus:ring-red-100",
            )}
          />
        </div>
        <AnimatePresence>
          {open && filtered.length > 0 && (
            <motion.ul
              initial={{ opacity: 0, y: -6, scale: 0.98 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: -6, scale: 0.98 }}
              transition={{ duration: 0.15 }}
              className="absolute z-30 mt-2 max-h-56 w-full overflow-auto rounded-xl border border-stone-100 bg-white p-1.5 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900"
            >
              {filtered.map((opt) => (
                <li key={opt}>
                  <button
                    type="button"
                    onClick={() => {
                      onChange(opt);
                      setOpen(false);
                    }}
                    className="flex w-full items-center rounded-lg px-3 py-2 text-left text-sm text-stone-600 transition hover:bg-stone-50 dark:text-stone-300 dark:hover:bg-stone-800"
                  >
                    {opt}
                  </button>
                </li>
              ))}
            </motion.ul>
          )}
        </AnimatePresence>
      </div>
    </FieldWrapper>
  );
}
