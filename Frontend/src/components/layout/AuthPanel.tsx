import { motion } from "framer-motion";
import { BadgeCheck, Leaf, Quote } from "lucide-react";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { Avatar } from "@/components/ui/Avatar";
import { AnimatedCounter } from "@/components/ui/AnimatedCounter";
import { heroPhoto, productPhotos, getCustomerPhoto } from "@/assets/photos";

const floaters = [
  { id: 201, pos: "left-0 top-6" },
  { id: 401, pos: "right-2 top-32" },
  { id: 501, pos: "left-4 bottom-10" },
];

export function AuthPanel() {
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

        <div className="relative mx-auto h-72 w-72">
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.7 }}
            className="absolute inset-6 overflow-hidden rounded-[2rem] shadow-2xl"
          >
            <PhotoTile src={heroPhoto} alt="Фермер с корзиной свежих овощей" className="h-full w-full" />
          </motion.div>
          {floaters.map((f, i) => (
            <motion.div
              key={f.id}
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: 0.3 + i * 0.15 }}
              className={`absolute h-16 w-16 overflow-hidden rounded-2xl border-2 border-white/20 shadow-xl ${f.pos}`}
              style={{ animation: `float 6s ease-in-out ${i * 0.6}s infinite` }}
            >
              <PhotoTile src={productPhotos[f.id]} alt="" className="h-full w-full" />
            </motion.div>
          ))}
        </div>

        <div className="flex flex-col gap-6">
          <div className="rounded-2xl border border-white/10 bg-white/5 p-5 backdrop-blur">
            <Quote size={20} className="mb-3 text-grove-400" fill="currentColor" />
            <p className="text-sm leading-relaxed text-grove-100">
              "Заказываю каждую неделю — разница со свежестью рыночных продуктов колоссальная. Больше не хочу
              покупать иначе."
            </p>
            <div className="mt-4 flex items-center gap-3">
              <Avatar name="Фарида Назарова" src={getCustomerPhoto(1)} size={36} />
              <div>
                <p className="text-sm font-medium text-white">Фарида Назарова</p>
                <p className="text-xs text-grove-300">Покупательница, Душанбе</p>
              </div>
            </div>
          </div>

          <div className="flex items-center justify-between text-white">
            <div>
              <AnimatedCounter value={214} suffix="+" className="font-display text-2xl" />
              <p className="text-xs text-grove-300">фермеров</p>
            </div>
            <div>
              <AnimatedCounter value={32400} suffix="+" className="font-display text-2xl" />
              <p className="text-xs text-grove-300">заказов</p>
            </div>
            <div className="flex items-center gap-1.5">
              <BadgeCheck size={18} className="text-grove-400" />
              <span className="text-xs text-grove-300">
                Проверенные
                <br />
                хозяйства
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
