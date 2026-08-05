import { motion, useReducedMotion } from "framer-motion";

// Своих фотографий грузовиков в проекте нет (см. комментарий в
// DeliveryFleet.tsx) — вместо стоковой фотографии рисуем собственную SVG-
// иллюстрацию рефрижератора в цветах и стиле сайта (grove/harvest/stone),
// с фирменным листом-бейджем на борту, как у остального брендинга.
export function DeliveryTruckIllustration({ className }: { className?: string }) {
  const prefersReducedMotion = useReducedMotion();

  return (
    <motion.div
      initial={prefersReducedMotion ? undefined : { y: 0 }}
      animate={prefersReducedMotion ? undefined : { y: [0, -8, 0] }}
      transition={{ duration: 5, repeat: Infinity, ease: "easeInOut" }}
      className={className}
    >
      <svg viewBox="0 0 520 300" fill="none" className="h-full w-full" aria-hidden="true">
        {/* Дорога/тень */}
        <ellipse cx="260" cy="256" rx="210" ry="14" className="fill-stone-900/10 dark:fill-black/40" />
        <path d="M20 258 H500" strokeDasharray="14 12" className="stroke-stone-300 dark:stroke-stone-700" strokeWidth="3" />

        {/* Скоростные линии сзади */}
        <g className="stroke-grove-400 dark:stroke-grove-600" strokeWidth="4" strokeLinecap="round" opacity="0.55">
          <path d="M18 150 H62" />
          <path d="M8 172 H48" opacity="0.7" />
          <path d="M26 194 H58" opacity="0.5" />
        </g>

        {/* Кузов-рефрижератор */}
        <rect x="150" y="70" width="230" height="150" rx="14" className="fill-grove-600 dark:fill-grove-700" />
        <rect x="150" y="70" width="230" height="34" rx="14" className="fill-grove-500 dark:fill-grove-600" />
        <rect x="150" y="190" width="230" height="30" rx="10" className="fill-grove-800 dark:fill-grove-950" />

        {/* Рефрижераторный блок на крыше */}
        <rect x="215" y="46" width="100" height="26" rx="7" className="fill-harvest-400 dark:fill-harvest-500" />
        <path
          d="M265 51v16M258 55l14 8M272 55l-14 8"
          className="stroke-harvest-900/70 dark:stroke-harvest-950"
          strokeWidth="2.4"
          strokeLinecap="round"
        />

        {/* Табличка с названием на борту (тот же логотип, что в шапке/футере) */}
        <g transform="translate(265, 140)">
          <rect x="-88" y="-24" width="176" height="48" rx="12" className="fill-stone-25 dark:fill-stone-900" opacity="0.95" />
          <text
            textAnchor="middle"
            dominantBaseline="middle"
            y="2"
            style={{ fontFamily: "var(--font-display)" }}
            fontSize="26"
            fontWeight="600"
          >
            <tspan className="fill-grove-800 dark:fill-grove-200">Market</tspan>
            <tspan className="fill-grove-500 dark:fill-grove-400">.tj</tspan>
          </text>
        </g>

        {/* Кабина */}
        <path
          d="M40 130c0-9 7-16 16-16h58c9 0 16 7 16 16v60c0 9-7 16-16 16H56c-9 0-16-7-16-16Z"
          className="fill-stone-50 dark:fill-stone-200"
        />
        <rect x="54" y="128" width="58" height="34" rx="8" className="fill-grove-900 dark:fill-grove-950" />
        <rect x="60" y="134" width="46" height="22" rx="5" className="fill-grove-700 opacity-70 dark:fill-grove-800" />
        <rect x="40" y="196" width="90" height="10" rx="4" className="fill-stone-300 dark:fill-stone-600" />

        {/* Бампер/фара */}
        <rect x="34" y="176" width="8" height="30" rx="4" className="fill-stone-800 dark:fill-stone-500" />
        <circle cx="46" cy="182" r="5" className="fill-harvest-400" />

        {/* Колёса */}
        <g>
          <circle cx="108" cy="222" r="24" className="fill-stone-900 dark:fill-stone-950" />
          <circle cx="108" cy="222" r="10" className="fill-stone-400 dark:fill-stone-600" />
          <circle cx="330" cy="222" r="24" className="fill-stone-900 dark:fill-stone-950" />
          <circle cx="330" cy="222" r="10" className="fill-stone-400 dark:fill-stone-600" />
        </g>

        {/* Плывущие листья над машиной */}
        <g className="fill-grove-400 dark:fill-grove-600" opacity="0.8">
          <path d="M420 90c-8-6-17 1-15 9 8 3 17-1 15-9Z" />
          <path d="M450 130c-8-6-17 1-15 9 8 3 17-1 15-9Z" opacity="0.6" />
        </g>
      </svg>
    </motion.div>
  );
}
