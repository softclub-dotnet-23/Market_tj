import { useEffect, useMemo, useState } from "react";
import { apiGet } from "@/lib/api";

interface OrderSequenceSource {
  id: number;
  createdAt: string;
}

// Технический код заказа (MTJ-XXXXXXXX-N) непонятен покупателю/фермеру/
// админу с первого взгляда — простой порядковый номер читается сразу.
// Считается ОДИН РАЗ, ГЛОБАЛЬНО по всем заказам платформы (не только "своим"
// — GET /api/orders и так уже отдаёт полный список без фильтра по ролям,
// каждая панель просто отфильтровывает его на фронте, см. data/{customer,
// farmer,adminEntities}.ts), по хронологии создания (createdAt) — поэтому
// "Заказ №47" означает один и тот же заказ везде, у всех трёх ролей, а не
// какой-то локальный индекс страницы, который был бы разным. Настоящий код
// (orderNumber) никуда не делся — используется как менее заметная подпись
// рядом (например, чтобы сослаться на заказ в поддержке).
export function useOrderSequenceMap(): Map<number, number> {
  const [orders, setOrders] = useState<OrderSequenceSource[]>([]);

  useEffect(() => {
    let cancelled = false;
    apiGet<OrderSequenceSource[]>("/orders")
      .then((data) => {
        if (!cancelled) setOrders(data);
      })
      .catch(() => {
        if (!cancelled) setOrders([]);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  // useMemo — без него на каждый рендер пересоздавались бы и sort, и Map
  // (используется в AdminOrders/CustomerOrders/FarmerOrders на каждой
  // строке таблицы) — лишняя работа на ровном месте, а не бесконечный цикл,
  // но именно такие места и создают ощущение "тормозит".
  return useMemo(() => {
    const sorted = [...orders].sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
    return new Map(sorted.map((o, i) => [o.id, i + 1]));
  }, [orders]);
}
