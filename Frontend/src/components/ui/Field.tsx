import { Children, forwardRef, isValidElement, useEffect, useId, useMemo, useRef, useState } from "react";
import type { InputHTMLAttributes, LabelHTMLAttributes, ReactNode, TextareaHTMLAttributes } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Check, ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

interface FieldWrapperProps {
  label?: string;
  hint?: string;
  error?: string;
  required?: boolean;
  children: ReactNode;
  htmlFor?: string;
}

export function FieldWrapper({ label, hint, error, required, children, htmlFor }: FieldWrapperProps) {
  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={htmlFor} className="text-sm font-medium text-stone-700 dark:text-stone-300">
          {label}
          {required && <span className="ml-0.5 text-clay-500">*</span>}
        </label>
      )}
      {children}
      {error ? (
        <span className="text-xs font-medium text-danger">{error}</span>
      ) : hint ? (
        <span className="text-xs text-stone-400 dark:text-stone-500">{hint}</span>
      ) : null}
    </div>
  );
}

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  hint?: string;
  error?: string;
  leftIcon?: ReactNode;
  rightSlot?: ReactNode;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, hint, error, required, leftIcon, rightSlot, className, id, ...props }, ref) => {
    const autoId = useId();
    const inputId = id ?? autoId;
    return (
      <FieldWrapper label={label} hint={hint} error={error} required={required} htmlFor={inputId}>
        <div className="relative flex items-center">
          {leftIcon && <span className="pointer-events-none absolute left-3.5 text-stone-400 dark:text-stone-500">{leftIcon}</span>}
          <input
            ref={ref}
            id={inputId}
            className={cn(
              "h-11 w-full rounded-xl border border-stone-200 bg-white px-4 text-[15px] text-stone-900 placeholder:text-stone-400 transition focus:border-grove-500 focus:ring-2 focus:ring-grove-100 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-100 dark:placeholder:text-stone-500 dark:focus:ring-grove-900",
              leftIcon && "pl-10",
              rightSlot && "pr-11",
              error && "border-danger focus:border-danger focus:ring-red-100",
              className,
            )}
            {...props}
          />
          {rightSlot && <span className="absolute right-3.5">{rightSlot}</span>}
        </div>
      </FieldWrapper>
    );
  },
);
Input.displayName = "Input";

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  hint?: string;
  error?: string;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ label, hint, error, required, className, id, ...props }, ref) => {
    const autoId = useId();
    const inputId = id ?? autoId;
    return (
      <FieldWrapper label={label} hint={hint} error={error} required={required} htmlFor={inputId}>
        <textarea
          ref={ref}
          id={inputId}
          className={cn(
            "min-h-32 w-full resize-y rounded-xl border border-stone-200 bg-white px-4 py-3 text-[15px] text-stone-900 placeholder:text-stone-400 transition focus:border-grove-500 focus:ring-2 focus:ring-grove-100 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-100 dark:placeholder:text-stone-500 dark:focus:ring-grove-900",
            error && "border-danger focus:border-danger focus:ring-red-100",
            className,
          )}
          {...props}
        />
      </FieldWrapper>
    );
  },
);
Textarea.displayName = "Textarea";

interface SelectOptionData {
  value: string;
  label: ReactNode;
  disabled: boolean;
}

interface SelectProps {
  label?: string;
  hint?: string;
  error?: string;
  required?: boolean;
  className?: string;
  id?: string;
  name?: string;
  disabled?: boolean;
  value?: string;
  onChange?: (value: string) => void;
  onBlur?: () => void;
  children: ReactNode;
}

// Кастомный анимированный listbox вместо голого <select> — открытую панель
// нативного select нельзя стилизовать/анимировать ни в одном браузере (её
// рисует ОС, а не CSS), поэтому по прямой просьбе пользователя ("везде где
// можно выбрать сделай красиво, анимационным") заменили на полностью свою
// разметку. Контролируемый компонент (value/onChange(value: string)) — со
// стороны React Hook Form подключается через Controller, а не register()
// (официально рекомендуемый RHF способ для не-нативных полей).
export const Select = forwardRef<HTMLButtonElement, SelectProps>(
  ({ label, hint, error, required, className, id, name, disabled, value, onChange, onBlur, children }, ref) => {
    const autoId = useId();
    const selectId = id ?? autoId;
    const [open, setOpen] = useState(false);
    const containerRef = useRef<HTMLDivElement>(null);

    const options = useMemo<SelectOptionData[]>(
      () =>
        Children.toArray(children)
          .filter(isValidElement)
          .map((el) => {
            const optionProps = el.props as { value?: string | number; children?: ReactNode; disabled?: boolean };
            return { value: String(optionProps.value ?? ""), label: optionProps.children, disabled: !!optionProps.disabled };
          }),
      [children],
    );

    useEffect(() => {
      if (!open) return;
      const onPointerDown = (e: MouseEvent) => {
        if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
          setOpen(false);
          onBlur?.();
        }
      };
      const onKeyDown = (e: KeyboardEvent) => {
        if (e.key === "Escape") setOpen(false);
      };
      document.addEventListener("mousedown", onPointerDown);
      document.addEventListener("keydown", onKeyDown);
      return () => {
        document.removeEventListener("mousedown", onPointerDown);
        document.removeEventListener("keydown", onKeyDown);
      };
    }, [open, onBlur]);

    const selected = options.find((o) => o.value === value);

    return (
      <FieldWrapper label={label} hint={hint} error={error} required={required} htmlFor={selectId}>
        <div ref={containerRef} className="relative">
          <button
            ref={ref}
            type="button"
            id={selectId}
            name={name}
            disabled={disabled}
            onClick={() => setOpen((o) => !o)}
            className={cn(
              "flex h-11 w-full items-center justify-between gap-2 rounded-xl border border-stone-200 bg-white px-4 text-left text-[15px] text-stone-900 transition focus:border-grove-500 focus:ring-2 focus:ring-grove-100 disabled:cursor-not-allowed disabled:opacity-60 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-100 dark:focus:ring-grove-900",
              error && "border-danger focus:border-danger focus:ring-red-100",
              className,
            )}
          >
            <span className={cn("truncate", (!selected || selected.disabled) && "text-stone-400 dark:text-stone-500")}>
              {selected ? selected.label : ""}
            </span>
            <motion.span
              animate={{ rotate: open ? 180 : 0 }}
              transition={{ duration: 0.18 }}
              className="shrink-0 text-stone-400 dark:text-stone-500"
            >
              <ChevronDown size={16} />
            </motion.span>
          </button>

          <AnimatePresence>
            {open && (
              <motion.ul
                role="listbox"
                initial={{ opacity: 0, y: -6, scale: 0.98 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                exit={{ opacity: 0, y: -6, scale: 0.98 }}
                transition={{ duration: 0.15, ease: "easeOut" }}
                className="absolute z-50 mt-1.5 max-h-60 w-full overflow-auto rounded-xl border border-stone-100 bg-white p-1.5 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900"
              >
                {options.map((o) => (
                  <li
                    key={o.value}
                    role="option"
                    aria-selected={!o.disabled && o.value === value}
                    onClick={() => {
                      if (o.disabled) return;
                      onChange?.(o.value);
                      setOpen(false);
                    }}
                    className={cn(
                      "flex items-center justify-between gap-2 rounded-lg px-3 py-2 text-[15px] transition-colors",
                      o.disabled
                        ? "cursor-not-allowed text-stone-300 dark:text-stone-600"
                        : cn(
                            "cursor-pointer",
                            o.value === value
                              ? "bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-300"
                              : "text-stone-700 hover:bg-stone-50 dark:text-stone-200 dark:hover:bg-stone-800",
                          ),
                    )}
                  >
                    <span className="truncate">{o.label}</span>
                    {!o.disabled && o.value === value && <Check size={15} className="shrink-0" />}
                  </li>
                ))}
              </motion.ul>
            )}
          </AnimatePresence>
        </div>
      </FieldWrapper>
    );
  },
);
Select.displayName = "Select";

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  label?: ReactNode;
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  ({ label, className, id, ...props }, ref) => {
    const autoId = useId();
    const inputId = id ?? autoId;
    return (
      <label htmlFor={inputId} className="flex cursor-pointer items-start gap-2.5 text-sm text-stone-600 dark:text-stone-300">
        <input
          ref={ref}
          id={inputId}
          type="checkbox"
          className={cn(
            "mt-0.5 h-4.5 w-4.5 shrink-0 cursor-pointer rounded-[5px] border-stone-300 text-grove-600 focus:ring-2 focus:ring-grove-200 dark:border-stone-600 dark:bg-stone-800",
            className,
          )}
          {...props}
        />
        {label}
      </label>
    );
  },
);
Checkbox.displayName = "Checkbox";

export function Label({ className, ...props }: LabelHTMLAttributes<HTMLLabelElement>) {
  return <label className={cn("text-sm font-medium text-stone-700 dark:text-stone-300", className)} {...props} />;
}
