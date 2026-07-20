import { forwardRef } from "react";
import type { ButtonHTMLAttributes, ReactNode } from "react";
import { motion, type HTMLMotionProps } from "framer-motion";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

const VARIANTS = {
  primary:
    "bg-grove-700 text-white shadow-(--shadow-glow-grove) hover:bg-grove-800 active:bg-grove-900",
  secondary:
    "bg-stone-900 text-white hover:bg-stone-800 active:bg-stone-950 dark:bg-stone-100 dark:text-stone-900 dark:hover:bg-stone-200 dark:active:bg-stone-300",
  outline:
    "border border-stone-300 bg-white text-stone-800 hover:border-grove-600 hover:text-grove-700 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-100 dark:hover:border-grove-500 dark:hover:text-grove-400",
  ghost: "text-stone-700 hover:bg-stone-100 dark:text-stone-300 dark:hover:bg-stone-800",
  harvest: "bg-harvest-500 text-stone-900 hover:bg-harvest-400 shadow-[0_8px_24px_-6px_rgba(224,146,42,0.45)]",
  danger: "bg-danger text-white hover:bg-red-700",
  link: "text-grove-700 hover:text-grove-800 underline-offset-4 hover:underline p-0 h-auto dark:text-grove-400 dark:hover:text-grove-300",
} as const;

const SIZES = {
  sm: "h-9 px-4 text-sm gap-1.5 rounded-lg",
  md: "h-11 px-5 text-[15px] gap-2 rounded-xl",
  lg: "h-13 px-7 text-base gap-2.5 rounded-xl",
  icon: "h-11 w-11 rounded-xl",
} as const;

export interface ButtonProps
  extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, "onDrag" | "onDragStart" | "onDragEnd" | "onAnimationStart"> {
  variant?: keyof typeof VARIANTS;
  size?: keyof typeof SIZES;
  loading?: boolean;
  leftIcon?: ReactNode;
  rightIcon?: ReactNode;
  motionProps?: HTMLMotionProps<"button">;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      variant = "primary",
      size = "md",
      loading = false,
      leftIcon,
      rightIcon,
      disabled,
      children,
      ...props
    },
    ref,
  ) => {
    return (
      <motion.button
        ref={ref}
        whileTap={{ scale: disabled || loading ? 1 : 0.97 }}
        transition={{ duration: 0.12 }}
        disabled={disabled || loading}
        className={cn(
          "relative inline-flex select-none items-center justify-center whitespace-nowrap font-medium transition-colors duration-200 disabled:cursor-not-allowed disabled:opacity-50",
          VARIANTS[variant],
          size !== "icon" && SIZES[size],
          size === "icon" && SIZES.icon,
          className,
        )}
        {...props}
      >
        {loading ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          leftIcon
        )}
        {children}
        {!loading && rightIcon}
      </motion.button>
    );
  },
);
Button.displayName = "Button";
