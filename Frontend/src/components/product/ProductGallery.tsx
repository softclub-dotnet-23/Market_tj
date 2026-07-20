import { useState } from "react";
import type { MouseEvent } from "react";
import { motion } from "framer-motion";
import { Maximize2, X, ZoomIn } from "lucide-react";
import { Modal } from "@/components/ui/Modal";

export function ProductGallery({ src, title }: { src: string; title: string }) {
  const [zoomStyle, setZoomStyle] = useState<{ x: number; y: number } | null>(null);
  const [lightboxOpen, setLightboxOpen] = useState(false);

  const onMouseMove = (e: MouseEvent<HTMLDivElement>) => {
    const rect = e.currentTarget.getBoundingClientRect();
    const x = ((e.clientX - rect.left) / rect.width) * 100;
    const y = ((e.clientY - rect.top) / rect.height) * 100;
    setZoomStyle({ x, y });
  };

  return (
    <div className="flex flex-col gap-4">
      <div
        onMouseMove={onMouseMove}
        onMouseLeave={() => setZoomStyle(null)}
        onClick={() => setLightboxOpen(true)}
        className="group relative aspect-square cursor-zoom-in overflow-hidden rounded-3xl border border-stone-100 bg-stone-100 dark:border-stone-800 dark:bg-stone-800"
      >
        <motion.img
          src={src}
          alt={title}
          animate={{
            scale: zoomStyle ? 1.6 : 1,
            x: zoomStyle ? `${50 - zoomStyle.x}%` : 0,
            y: zoomStyle ? `${50 - zoomStyle.y}%` : 0,
          }}
          transition={{ duration: 0.15, ease: "linear" }}
          className="h-full w-full object-cover"
        />

        <span className="absolute right-4 top-4 flex h-10 w-10 items-center justify-center rounded-full bg-white/90 text-stone-600 opacity-0 shadow-sm backdrop-blur transition-opacity group-hover:opacity-100 dark:bg-stone-800/90 dark:text-stone-300">
          <ZoomIn size={17} />
        </span>
      </div>

      <Modal open={lightboxOpen} onClose={() => setLightboxOpen(false)} className="max-w-2xl bg-transparent p-0 shadow-none">
        <div className="relative">
          <button
            onClick={() => setLightboxOpen(false)}
            className="absolute -top-14 right-0 flex h-10 w-10 items-center justify-center rounded-full bg-white/90 text-stone-700"
          >
            <X size={18} />
          </button>
          <div className="aspect-square overflow-hidden rounded-3xl bg-white shadow-(--shadow-lifted)">
            <img src={src} alt={title} className="h-full w-full object-cover" />
          </div>
          <p className="mt-4 flex items-center justify-center gap-2 text-sm text-white/80">
            <Maximize2 size={13} />
            {title}
          </p>
        </div>
      </Modal>
    </div>
  );
}
