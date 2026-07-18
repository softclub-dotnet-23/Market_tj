import { createContext, useContext, useState } from "react";
import type { ReactNode } from "react";
import { toast } from "sonner";
import { getProductById } from "@/data/products";

interface FavoritesContextValue {
  favoriteIds: number[];
  toggleFavorite: (productId: number) => void;
  isFavorite: (productId: number) => boolean;
}

const FavoritesContext = createContext<FavoritesContextValue | null>(null);

export function FavoritesProvider({ children }: { children: ReactNode }) {
  const [favoriteIds, setFavoriteIds] = useState<number[]>([]);

  const toggleFavorite = (productId: number) => {
    setFavoriteIds((prev) => {
      const exists = prev.includes(productId);
      const product = getProductById(productId);
      if (exists) {
        toast(`${product?.title ?? "Товар"} убран из избранного`);
        return prev.filter((id) => id !== productId);
      }
      toast.success(`${product?.title ?? "Товар"} добавлен в избранное`);
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
