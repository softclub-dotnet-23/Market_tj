import { useState } from "react";
import { motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { BadgeCheck, Leaf, Quote } from "lucide-react";
import { Avatar } from "@/components/ui/Avatar";
import { AnimatedCounter } from "@/components/ui/AnimatedCounter";
import { treePhoto, applePhoto, getCustomerPhoto } from "@/assets/photos";

const prefersReducedMotion =
  typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

// Timings/curves match the "Login Tree Animation" design (treeGrow / appleGrow / appleSway / appleFall).
const TREE_GROW_DURATION = 1.8;
const TREE_GROW_EASE = [0.22, 1, 0.36, 1] as const;
const APPLE_GROW_DURATION = 0.7;
const APPLE_GROW_STAGGER = 0.15;
const APPLE_SWAY_DURATION = 4;
const APPLE_SWAY_STAGGER = 0.6;
const APPLE_FALL_DURATION = 1.15;
const APPLE_FALL_EASE = [0.5, 0, 0.85, 0.4] as const;
const APPLE_ASPECT = 39 / 33;

const appleSpots = [
  { left: "30%", top: "14%", size: 34 },
  { left: "52%", top: "10%", size: 36 },
  { left: "68%", top: "20%", size: 33 },
  { left: "18%", top: "30%", size: 35 },
  { left: "44%", top: "26%", size: 32 },
  { left: "60%", top: "38%", size: 38 },
  { left: "80%", top: "34%", size: 33 },
  { left: "26%", top: "46%", size: 34 },
  { left: "48%", top: "48%", size: 35 },
];

function Apple({ left, top, size, index }: { left: string; top: string; size: number; index: number }) {
  const [fallen, setFallen] = useState(false);
  const growDelay = prefersReducedMotion ? 0 : index * APPLE_GROW_STAGGER;
  const swayDelay = growDelay + APPLE_GROW_DURATION + index * APPLE_SWAY_STAGGER;
  const dimensions = { left, top, width: size, height: size * APPLE_ASPECT };

  if (fallen) {
    return (
      <motion.img
        src={applePhoto}
        alt=""
        className="absolute select-none"
        style={dimensions}
        initial={{ y: 0, rotate: 0, opacity: 1 }}
        animate={{ y: 420, rotate: 220, opacity: 0 }}
        transition={{ duration: APPLE_FALL_DURATION, ease: APPLE_FALL_EASE }}
      />
    );
  }

  return (
    <motion.div
      className="absolute cursor-pointer"
      style={dimensions}
      initial={prefersReducedMotion ? false : { scale: 0, opacity: 0 }}
      animate={{ scale: [0, 1.15, 0.95, 1], opacity: [0, 1, 1, 1] }}
      transition={{
        delay: growDelay,
        duration: prefersReducedMotion ? 0 : APPLE_GROW_DURATION,
        times: [0, 0.6, 0.8, 1],
      }}
      onClick={() => setFallen(true)}
    >
      <motion.img
        src={applePhoto}
        alt=""
        className="h-full w-full select-none drop-shadow-[0_3px_5px_rgba(0,0,0,0.35)]"
        style={{ transformOrigin: "50% -10%" }}
        animate={{ rotate: [-3, 3, -3] }}
        transition={{
          delay: swayDelay,
          duration: APPLE_SWAY_DURATION,
          repeat: Infinity,
          ease: "easeInOut",
        }}
      />
    </motion.div>
  );
}

export function AuthPanel() {
  const { t } = useTranslation("layout");
  const [grown, setGrown] = useState(prefersReducedMotion);
  return (
    <div className="relative hidden overflow-hidden bg-grove-900 lg:block">
      <div className="pointer-events-none absolute inset-0 bg-noise opacity-20" />
      <div className="pointer-events-none absolute -left-20 top-10 h-72 w-72 rounded-full bg-grove-700/50 blur-3xl" />
      <div className="pointer-events-none absolute -right-16 bottom-10 h-72 w-72 rounded-full bg-harvest-600/30 blur-3xl" />

      <div className="relative flex h-full flex-col justify-between p-12">
        <div className="flex items-center gap-2 text-white">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-white/10">
            <Leaf size={18} />
          </span>
          <span className="font-display text-xl">
            Market<span className="text-grove-300">.tj</span>
          </span>
        </div>

        <div className="relative mx-auto aspect-630/628 w-full max-w-107.5" style={{ backgroundColor: "#1a482a" }}>
          <motion.img
            src={treePhoto}
            alt={t("authPanel.heroAlt")}
            className="absolute inset-0 h-full w-full object-contain drop-shadow-[0_18px_30px_rgba(0,0,0,0.35)]"
            style={{ transformOrigin: "bottom center" }}
            initial={
              prefersReducedMotion
                ? false
                : { scaleY: 0.05, scaleX: 0.4, opacity: 0.4, clipPath: "inset(100% 0 0 0)" }
            }
            animate={{ scaleY: 1, scaleX: 1, opacity: [0.4, 1, 1], clipPath: "inset(0% 0 0 0)" }}
            transition={{
              duration: prefersReducedMotion ? 0 : TREE_GROW_DURATION,
              ease: TREE_GROW_EASE,
              opacity: {
                duration: prefersReducedMotion ? 0 : TREE_GROW_DURATION,
                times: [0, 0.3, 1],
                ease: TREE_GROW_EASE,
              },
            }}
            onAnimationComplete={() => setGrown(true)}
          />
          {grown && appleSpots.map((spot, i) => <Apple key={i} {...spot} index={i} />)}
        </div>

        <div className="flex flex-col gap-6">
          <div className="rounded-2xl border border-white/10 bg-white/5 p-5 backdrop-blur">
            <Quote size={20} className="mb-3 text-grove-400" fill="currentColor" />
            <p className="text-sm leading-relaxed text-grove-100">{t("authPanel.quote")}</p>
            <div className="mt-4 flex items-center gap-3">
              <Avatar name={t("authPanel.customerName")} src={getCustomerPhoto(1)} size={36} />
              <div>
                <p className="text-sm font-medium text-white">{t("authPanel.customerName")}</p>
                <p className="text-xs text-grove-300">{t("authPanel.customerRole")}</p>
              </div>
            </div>
          </div>

          <div className="flex items-center justify-between text-white">
            <div>
              <AnimatedCounter value={214} suffix="+" className="font-display text-2xl" />
              <p className="text-xs text-grove-300">{t("authPanel.farmersLabel")}</p>
            </div>
            <div>
              <AnimatedCounter value={32400} suffix="+" className="font-display text-2xl" />
              <p className="text-xs text-grove-300">{t("authPanel.ordersLabel")}</p>
            </div>
            <div className="flex items-center gap-1.5">
              <BadgeCheck size={18} className="text-grove-400" />
              <span className="text-xs text-grove-300">
                {t("authPanel.verifiedFarms1")}
                <br />
                {t("authPanel.verifiedFarms2")}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
