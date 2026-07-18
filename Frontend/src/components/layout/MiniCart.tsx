import { useEffect, useRef } from "react";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { Minus, Plus, ShoppingBag, Trash2 } from "lucide-react";
import { useCart } from "@/context/CartContext";
import { getProductById } from "@/data/products";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { productPhotos } from "@/assets/photos";
import { Button } from "@/components/ui/Button";
import { formatSomoni } from "@/lib/utils";

export function MiniCart({ onClose }: { onClose: () => void }) {
  const { lines, removeItem, setQuantity, totalPrice } = useCart();
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function onClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) onClose();
    }
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, [onClose]);

  return (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, y: -8, scale: 0.98 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      exit={{ opacity: 0, y: -8, scale: 0.98 }}
      transition={{ duration: 0.18 }}
      className="absolute right-0 top-full z-50 mt-3 w-[min(380px,90vw)] rounded-2xl border border-stone-100 bg-white p-4 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900"
    >
      <div className="mb-3 flex items-center justify-between">
        <h3 className="font-display text-base text-stone-900 dark:text-stone-50">Корзина</h3>
        <span className="text-xs text-stone-400 dark:text-stone-500">{lines.length} товар(ов)</span>
      </div>

      {lines.length === 0 ? (
        <div className="flex flex-col items-center gap-2 py-8 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500">
            <ShoppingBag size={20} />
          </div>
          <p className="text-sm text-stone-500 dark:text-stone-400">Корзина пока пуста</p>
          <Link to="/catalog" onClick={onClose}>
            <Button size="sm" variant="outline" className="mt-1">
              Перейти в каталог
            </Button>
          </Link>
        </div>
      ) : (
        <>
          <div className="flex max-h-72 flex-col gap-3 overflow-y-auto pr-1">
            {lines.map((line) => {
              const product = getProductById(line.productId);
              if (!product) return null;
              return (
                <div key={line.productId} className="flex gap-3">
                  <Link
                    to={`/product/${product.slug}`}
                    onClick={onClose}
                    className="h-14 w-14 shrink-0 overflow-hidden rounded-xl"
                  >
                    <PhotoTile src={productPhotos[product.id]} alt={product.title} className="h-full w-full" />
                  </Link>
                  <div className="flex min-w-0 flex-1 flex-col gap-1">
                    <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{product.title}</p>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-1.5 rounded-full bg-stone-100 px-1.5 py-1 dark:bg-stone-800">
                        <button
                          onClick={() =>
                            setQuantity(line.productId, Math.max(product.minimumOrderQuantity, line.quantity - 1))
                          }
                          className="flex h-5 w-5 items-center justify-center rounded-full bg-white text-stone-600 shadow-sm dark:bg-stone-700 dark:text-stone-200"
                        >
                          <Minus size={11} />
                        </button>
                        <span className="min-w-4 text-center text-xs font-medium text-stone-700 dark:text-stone-200">
                          {line.quantity}
                        </span>
                        <button
                          onClick={() => setQuantity(line.productId, line.quantity + 1)}
                          className="flex h-5 w-5 items-center justify-center rounded-full bg-white text-stone-600 shadow-sm dark:bg-stone-700 dark:text-stone-200"
                        >
                          <Plus size={11} />
                        </button>
                      </div>
                      <span className="text-sm font-semibold text-stone-900 dark:text-stone-100">
                        {formatSomoni(product.retailPricePerKg * line.quantity)} с.
                      </span>
                    </div>
                  </div>
                  <button
                    onClick={() => removeItem(line.productId)}
                    aria-label="Удалить"
                    className="self-start text-stone-300 transition hover:text-clay-500"
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              );
            })}
          </div>

          <div className="mt-4 flex items-center justify-between border-t border-stone-100 pt-3 dark:border-stone-800">
            <span className="text-sm text-stone-500 dark:text-stone-400">Итого</span>
            <span className="font-display text-lg text-stone-900 dark:text-stone-50">{formatSomoni(totalPrice)} с.</span>
          </div>
          <Button className="mt-3 w-full" size="md">
            Оформить заказ
          </Button>
          <p className="mt-2 text-center text-[11px] text-stone-400 dark:text-stone-500">
            Оформление заказа появится после подключения к бэкенду
          </p>
        </>
      )}
    </motion.div>
  );
}
