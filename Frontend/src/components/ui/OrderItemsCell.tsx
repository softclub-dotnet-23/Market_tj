import { useState } from "react";
import { useTranslation } from "react-i18next";

interface OrderItemLike {
  id: number;
  productName: string;
  quantity: number;
}

// В свёрнутом виде — ровно одна строка, всегда, независимо от числа позиций:
// первый товар + "ещё N" сразу рядом на той же строке (не под ней), иначе
// заказы с несколькими товарами становились выше однопозиционных и раздували
// высоту всей таблицы. Разворачивается по клику — там уже нормально занимать
// несколько строк, это временное явное действие пользователя, а не дефолт.
export function OrderItemsCell({ items, ns }: { items: OrderItemLike[]; ns: "admin" | "farmer" }) {
  const { t } = useTranslation(ns);
  const [expanded, setExpanded] = useState(false);

  if (items.length === 0) {
    return <span className="text-stone-300 dark:text-stone-600">—</span>;
  }

  const [first, ...rest] = items;

  if (!expanded) {
    return (
      <button
        onClick={() => rest.length > 0 && setExpanded(true)}
        disabled={rest.length === 0}
        className="flex max-w-56 items-center gap-1.5 text-left disabled:cursor-default"
      >
        <span className="truncate">
          {first.productName} × {first.quantity}
        </span>
        {rest.length > 0 && (
          <span className="shrink-0 text-xs font-medium text-grove-700 hover:underline dark:text-grove-400">
            {t("orders.moreItems", { count: rest.length })}
          </span>
        )}
      </button>
    );
  }

  return (
    <div className="flex max-w-56 flex-col gap-1">
      {items.map((item) => (
        <span key={item.id} className="truncate">
          {item.productName} × {item.quantity}
        </span>
      ))}
      <button
        onClick={() => setExpanded(false)}
        className="w-fit text-left text-xs font-medium text-grove-700 hover:underline dark:text-grove-400"
      >
        {t("orders.showLess")}
      </button>
    </div>
  );
}
