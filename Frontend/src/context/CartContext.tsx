import { createContext, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import type { CartLine, Product } from "@/types";
import { getProductById, useProducts } from "@/data/products";
import { formatSomoni, getEffectiveMinQuantity, getUnitPrice } from "@/lib/utils";
import { getLocalizedTitle } from "@/lib/productI18n";

const CART_STORAGE_KEY = "market-tj-cart";

// Раньше корзина жила только в памяти React — обновление страницы (F5) её
// полностью стирало, хотя пользователь ничего не оформлял. Читаем/пишем в
// localStorage, чтобы корзина переживала перезагрузку — очищается она только
// осознанно, через clearCart() после реального оформления заказа.
function readStoredCart(): CartLine[] {
  try {
    const raw = localStorage.getItem(CART_STORAGE_KEY);
    return raw ? (JSON.parse(raw) as CartLine[]) : [];
  } catch {
    return [];
  }
}

interface CartContextValue {
  lines: CartLine[];
  totalItems: number;
  totalPrice: number;
  addItem: (product: Product, quantity?: number) => void;
  removeItem: (productId: number) => void;
  setQuantity: (productId: number, quantity: number) => void;
  clearCart: () => void;
  isInCart: (productId: number) => boolean;
}

const CartContext = createContext<CartContextValue | null>(null);

export function CartProvider({ children }: { children: ReactNode }) {
  const { t, i18n } = useTranslation(["common", "product"]);
  const [lines, setLines] = useState<CartLine[]>(readStoredCart);
  // Подписка на каталог нужна не для чтения products напрямую (используем
  // getProductById), а чтобы totalPrice/totalItems пересчитались, когда
  // каталог догрузится — иначе при возврате пользователя с уже непустой
  // корзиной (localStorage) totalPrice считался ДО первой загрузки каталога,
  // "застревал" на 0 и не обновлялся, пока lines не менялись (баг "Итого: 0 c.").
  const products = useProducts();

  useEffect(() => {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(lines));
  }, [lines]);

  const addItem = (product: Product, quantity = getEffectiveMinQuantity(product)) => {
    setLines((prev) => {
      const existing = prev.find((l) => l.productId === product.id);
      if (existing) {
        return prev.map((l) =>
          l.productId === product.id ? { ...l, quantity: l.quantity + quantity } : l,
        );
      }
      return [...prev, { productId: product.id, quantity }];
    });
    toast.success(t("cart.addedToCart", { title: getLocalizedTitle(product, i18n.language) }), {
      description: t("cart.addedToCartDescription", {
        quantity,
        unit: t(`product:units.${product.unit}`),
        price: `${formatSomoni(getUnitPrice(product, quantity))} ${t("currencySomoni")}`,
      }),
    });
  };

  const removeItem = (productId: number) => {
    setLines((prev) => prev.filter((l) => l.productId !== productId));
  };

  const setQuantity = (productId: number, quantity: number) => {
    setLines((prev) => prev.map((l) => (l.productId === productId ? { ...l, quantity } : l)));
  };

  const clearCart = () => setLines([]);

  const isInCart = (productId: number) => lines.some((l) => l.productId === productId);

  const totalItems = useMemo(() => lines.reduce((sum, l) => sum + l.quantity, 0), [lines]);
  const totalPrice = useMemo(
    () =>
      lines.reduce((sum, l) => {
        const product = getProductById(l.productId);
        return sum + (product ? getUnitPrice(product, l.quantity) * l.quantity : 0);
      }, 0),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [lines, products],
  );

  return (
    <CartContext.Provider
      value={{ lines, totalItems, totalPrice, addItem, removeItem, setQuantity, clearCart, isInCart }}
    >
      {children}
    </CartContext.Provider>
  );
}

export function useCart() {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error("useCart must be used within CartProvider");
  return ctx;
}
