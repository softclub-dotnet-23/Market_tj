import { createContext, useContext, useEffect, useState } from "react";
import type { ReactNode } from "react";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { useProducts } from "@/data/products";

const FAVORITES_STORAGE_KEY = "market-tj-favorites";

// Как и с корзиной (CartContext) — раньше избранное жило только в памяти и
// пропадало при обновлении страницы. Храним в localStorage, чтобы переживало
// перезагрузку.
function readStoredFavorites(): number[] {
  try {
    const raw = localStorage.getItem(FAVORITES_STORAGE_KEY);
    return raw ? (JSON.parse(raw) as number[]) : [];
  } catch {
    return [];
  }
}

interface FavoritesContextValue {
  favoriteIds: number[];
  toggleFavorite: (productId: number) => void;
  isFavorite: (productId: number) => boolean;
}

const FavoritesContext = createContext<FavoritesContextValue | null>(null);

export function FavoritesProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation("common");
  const products = useProducts();
  const [favoriteIds, setFavoriteIds] = useState<number[]>(readStoredFavorites);

  useEffect(() => {
    localStorage.setItem(FAVORITES_STORAGE_KEY, JSON.stringify(favoriteIds));
  }, [favoriteIds]);

  const toggleFavorite = (productId: number) => {
    setFavoriteIds((prev) => {
      const exists = prev.includes(productId);
      const product = products.find((p) => p.id === productId);
      const title = product?.title ?? t("favorites.defaultItemName");
      if (exists) {
        toast(t("favorites.removedItem", { title }));
        return prev.filter((id) => id !== productId);
      }
      toast.success(t("favorites.addedItem", { title }));
      return [...prev, productId];
    });
  };

  const isFavorite = (productId: number) => favoriteIds.includes(productId);

  return (
    <FavoritesContext.Provider value={{ favoriteIds, toggleFavorite, isFavorite }}>
      {children}
    </FavoritesContext.Provider>
  );
}

export function useFavorites() {
  const ctx = useContext(FavoritesContext);
  if (!ctx) throw new Error("useFavorites must be used within FavoritesProvider");
  return ctx;
}
