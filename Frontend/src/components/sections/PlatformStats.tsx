import { motion } from "framer-motion";
import { AnimatedCounter } from "@/components/ui/AnimatedCounter";
import { usePlatformStats } from "@/data/site";

export function PlatformStats() {
  const platformStats = usePlatformStats();
  return (
    <section className="relative overflow-hidden bg-grove-900 py-14 text-white sm:py-20">
      <div className="pointer-events-none absolute inset-0 bg-noise opacity-30" />
      <div className="pointer-events-none absolute -left-24 top-0 h-72 w-72 rounded-full bg-grove-700/40 blur-3xl" />
      <div className="pointer-events-none absolute -right-24 bottom-0 h-72 w-72 rounded-full bg-harvest-600/30 blur-3xl" />

      <div className="container-page relative grid grid-cols-2 gap-8 lg:grid-cols-4">
        {platformStats.map((stat, i) => (
          <motion.div
            key={stat.id}
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-40px" }}
            transition={{ duration: 0.5, delay: i * 0.1 }}
            className="flex flex-col items-center gap-2 text-center lg:items-start lg:text-left"
          >
            <AnimatedCounter
              value={stat.value}
              suffix={stat.suffix}
              className="font-display text-4xl text-white sm:text-5xl"
            />
            <p className="text-sm text-grove-200">{stat.label}</p>
          </motion.div>
        ))}
      </div>
    </section>
  );
}
