import { Package } from "lucide-react";

interface OrderItemLike {
  id: number;
  productListingId: number;
  productName: string;
  quantity: number;
}

// Для карточного вида заказа (в отличие от компактной OrderItemsCell в
// таблице) — здесь достаточно места, чтобы показать фото товара, а не только
// текст. Фото резолвится через уже загруженный публичный каталог
// (Product.id === ProductListing.id, см. data/catalogStore.ts) — честный
// фолбэк-значок, если объявление с тех пор архивировано/удалено и в каталоге
// его больше нет.
export function OrderItemsPhotoList({
  items,
  photoByListingId,
}: {
  items: OrderItemLike[];
  photoByListingId: Map<number, string | undefined>;
}) {
  if (items.length === 0) {
    return <span className="text-sm text-stone-300 dark:text-stone-600">—</span>;
  }

  return (
    <div className="flex flex-col gap-2">
      {items.map((item) => {
        const photo = photoByListingId.get(item.productListingId);
        return (
          <div key={item.id} className="flex items-center gap-2.5">
            {photo ? (
              <img src={photo} alt="" className="h-10 w-10 shrink-0 rounded-lg object-cover" loading="lazy" />
            ) : (
              <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500">
                <Package size={16} />
              </span>
            )}
            <span className="min-w-0 flex-1 truncate text-sm text-stone-700 dark:text-stone-200">
              {item.productName} × {item.quantity}
            </span>
          </div>
        );
      })}
    </div>
  );
}
