import { motion, useReducedMotion } from "framer-motion";

interface AssistantMascotProps {
  size?: number;
  className?: string;
  /** Отключает бесконечные циклы (моргание/покачивание листика) — например, внутри маленькой иконки в шапке. */
  animated?: boolean;
}

// Минималистичный оригинальный маскот Market.tj для AI-помощника: округлое
// слоновая кость + зелёное тело, тёмно-зелёное "лицо"-визор с двумя простыми
// глазами, маленький лист вместо антенны и такой же лист-бейдж на груди —
// только визуальный элемент, без какой-либо логики/состояния виджета.
export function AssistantMascot({ size = 28, className, animated = true }: AssistantMascotProps) {
  const prefersReducedMotion = useReducedMotion();
  const shouldAnimate = animated && !prefersReducedMotion;

  const svg = (
    <motion.svg
      width={size}
      height={size}
      viewBox="0 0 48 48"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      aria-hidden="true"
    >
      {/* Лист-антенна */}
      <motion.g
        style={{ transformOrigin: "24px 10px" }}
        animate={shouldAnimate ? { rotate: [-6, 6, -6] } : undefined}
        transition={shouldAnimate ? { duration: 3.2, repeat: Infinity, ease: "easeInOut" } : undefined}
      >
        <path d="M24 10 L24 4" stroke="var(--color-grove-500)" strokeWidth="1.6" strokeLinecap="round" />
        <path
          d="M24 4c-3.2-2.4-7 0-6.4 3.6C21 8.6 25 7.6 24 4Z"
          fill="var(--color-grove-400)"
        />
      </motion.g>

      {/* Тело: слоновая кость с мягкой зелёной обводкой */}
      <rect x="6" y="10" width="36" height="34" rx="14" fill="var(--color-stone-50)" stroke="var(--color-grove-700)" strokeWidth="1.5" />
      <rect x="6" y="10" width="36" height="34" rx="14" fill="url(#assistant-mascot-sheen)" />

      {/* Лицо-визор: тёмно-зелёный */}
      <rect x="12.5" y="17" width="23" height="15" rx="7.5" fill="var(--color-grove-900)" />

      {/* Глаза */}
      <motion.g
        animate={
          shouldAnimate
            ? { scaleY: [1, 1, 1, 0.12, 1, 1, 1, 1, 0.12, 1] }
            : undefined
        }
        transition={
          shouldAnimate
            ? { duration: 5, repeat: Infinity, ease: "easeInOut", times: [0, 0.4, 0.42, 0.44, 0.46, 0.7, 0.72, 0.74, 0.76, 1] }
            : undefined
        }
        style={{ transformOrigin: "24px 24.5px" }}
      >
        <circle cx="19.5" cy="24.5" r="2.4" fill="var(--color-stone-50)" />
        <circle cx="28.5" cy="24.5" r="2.4" fill="var(--color-stone-50)" />
      </motion.g>

      {/* Лист-бейдж на груди */}
      <path
        d="M24 34.5c-4-1-6.4 1.3-5.6 4.7 3.6.9 6.6-1.3 5.6-4.7Z"
        fill="var(--color-grove-500)"
      />

      <defs>
        <linearGradient id="assistant-mascot-sheen" x1="6" y1="10" x2="42" y2="44" gradientUnits="userSpaceOnUse">
          <stop offset="0" stopColor="var(--color-grove-100)" stopOpacity="0.55" />
          <stop offset="1" stopColor="var(--color-stone-50)" stopOpacity="0" />
        </linearGradient>
      </defs>
    </motion.svg>
  );

  if (!shouldAnimate) return svg;

  // Настоящий 3D (не плоский SVG rotate) — оболочка с perspective/preserve-3d,
  // маскот мягко "дышит" покачиванием по X/Y, как будто это маленькая
  // объёмная фигурка, а не иконка. Только визуальный эффект.
  return (
    <div style={{ perspective: 300 }}>
      <motion.div
        style={{ transformStyle: "preserve-3d" }}
        animate={{ rotateY: [-10, 10, -10], rotateX: [4, -4, 4] }}
        transition={{ duration: 5.5, repeat: Infinity, ease: "easeInOut" }}
      >
        {svg}
      </motion.div>
    </div>
  );
}
