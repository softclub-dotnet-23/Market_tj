import { createContext, useContext, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import type { CartLine, Product } from "@/types";
import { getProductById } from "@/data/products";
import { formatSomoni } from "@/lib/utils";

interface CartContextValue {
  lines: CartLine[];
  totalItems: number;
  totalPrice: number;
  addItem: (product: Product, quantity?: number) => void;
  removeItem: (productId: number) => void;
  setQuantity: (productId: number, quantity: number) => void;
  isInCart: (productId: number) => boolean;
}

const CartContext = createContext<CartContextValue | null>(null);

export function CartProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation(["common", "product"]);
  const [lines, setLines] = useState<CartLine[]>([]);

  const addItem = (product: Product, quantity = product.minimumOrderQuantity) => {
    setLines((prev) => {
      const existing = prev.find((l) => l.productId === product.id);
      if (existing) {
        return prev.map((l) =>
          l.productId === product.id ? { ...l, quantity: l.quantity + quantity } : l,
        );
      }
      return [...prev, { productId: product.id, quantity }];
    });
    toast.success(t("cart.addedToCart", { title: product.title }), {
      description: t("cart.addedToCartDescription", {
        quantity,
        unit: t(`product:units.${product.unit}`),
        price: `${formatSomoni(product.retailPricePerKg)} ${t("currencySomoni")}`,
      }),
    });
  };

  const removeItem = (productId: number) => {
    setLines((prev) => prev.filter((l) => l.productId !== productId));
  };

  const setQuantity = (productId: number, quantity: number) => {
    setLines((prev) => prev.map((l) => (l.productId === productId ? { ...l, quantity } : l)));
  };

  const isInCart = (productId: number) => lines.some((l) => l.productId === productId);

  const totalItems = useMemo(() => lines.reduce((sum, l) => sum + l.quantity, 0), [lines]);
  const totalPrice = useMemo(
    () =>
      lines.reduce((sum, l) => {
        const product = getProductById(l.productId);
        return sum + (product ? product.retailPricePerKg * l.quantity : 0);
      }, 0),
    [lines],
  );

  return (
    <CartContext.Provider
      value={{ lines, totalItems, totalPrice, addItem, removeItem, setQuantity, isInCart }}
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
