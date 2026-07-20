import { useEffect, useState } from "react";
import type { ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import {
  LogOut,
  Package,
  ShoppingCart,
  Sprout,
  Truck,
  Users,
  Wallet,
} from "lucide-react";
import { api, ApiError } from "@/lib/api";
import { useAuth } from "@/context/AuthContext";
import { formatSomoni } from "@/lib/utils";
import { Button } from "@/components/ui/Button";

interface TopSellingProduct {
  productName: string;
  quantitySold: number;
  revenue: number;
}

interface OrderStatusCount {
  status: number;
  count: number;
}

interface MonthlyRevenue {
  year: number;
  month: number;
  revenue: number;
}

interface AdminDashboardData {
  totalUsers: number;
  totalFarmers: number;
  totalCustomers: number;
  totalCouriers: number;
  totalOrders: number;
  ordersToday: number;
  ordersThisMonth: number;
  totalRevenue: number;
  revenueThisMonth: number;
  totalProductListings: number;
  activeProductListings: number;
  topSellingProducts: TopSellingProduct[];
  ordersByStatus: OrderStatusCount[];
  revenueByMonth: MonthlyRevenue[];
}

const ORDER_STATUS_LABELS: Record<number, string> = {
  1: "Ожидает",
  2: "Принят",
  3: "Отклонён",
  4: "Готовится",
  5: "Готов к выдаче",
  6: "Курьер назначен",
  7: "Забран",
  8: "В доставке",
  9: "Доставлен",
  10: "Завершён",
  11: "Отменён",
};

const MONTH_LABELS = [
  "Янв", "Фев", "Мар", "Апр", "Май", "Июн",
  "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек",
];

export function AdminDashboard() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [data, setData] = useState<AdminDashboardData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    api
      .get<AdminDashboardData>("/analytics/admin/dashboard")
      .then((result) => {
        if (!cancelled) setData(result);
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : "Не удалось загрузить данные аналитики");
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div className="min-h-screen bg-stone-50 dark:bg-stone-950">
      <header className="flex items-center justify-between border-b border-stone-200 bg-white px-6 py-4 dark:border-stone-800 dark:bg-stone-900">
        <div>
          <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">Панель администратора</h1>
          <p className="text-sm text-stone-500 dark:text-stone-400">{user?.fullName} · {user?.email}</p>
        </div>
        <Button variant="outline" size="sm" leftIcon={<LogOut size={16} />} onClick={handleLogout}>
          Выйти
        </Button>
      </header>

      <main className="mx-auto max-w-6xl px-6 py-8">
        {isLoading && <p className="text-stone-500 dark:text-stone-400">Загрузка данных...</p>}

        {error && (
          <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">
            {error}
          </div>
        )}

        {data && (
          <div className="flex flex-col gap-8">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <StatCard icon={<Users size={18} />} label="Пользователи" value={data.totalUsers} sub={`${data.totalFarmers} фермеров · ${data.totalCustomers} покупателей · ${data.totalCouriers} курьеров`} />
              <StatCard icon={<ShoppingCart size={18} />} label="Заказы" value={data.totalOrders} sub={`${data.ordersToday} сегодня · ${data.ordersThisMonth} в этом месяце`} />
              <StatCard icon={<Wallet size={18} />} label="Выручка" value={`${formatSomoni(data.totalRevenue)} с.`} sub={`${formatSomoni(data.revenueThisMonth)} с. в этом месяце`} />
              <StatCard icon={<Package size={18} />} label="Объявления" value={data.totalProductListings} sub={`${data.activeProductListings} активных`} />
            </div>

            <section className="grid grid-cols-1 gap-6 lg:grid-cols-2">
              <div className="rounded-2xl border border-stone-200 bg-white p-5 dark:border-stone-800 dark:bg-stone-900">
                <h2 className="mb-4 flex items-center gap-2 font-medium text-stone-900 dark:text-stone-50">
                  <Sprout size={16} /> Топ продаж
                </h2>
                {data.topSellingProducts.length === 0 ? (
                  <p className="text-sm text-stone-500 dark:text-stone-400">Пока нет завершённых заказов</p>
                ) : (
                  <ul className="flex flex-col gap-2">
                    {data.topSellingProducts.map((p) => (
                      <li key={p.productName} className="flex items-center justify-between text-sm">
                        <span className="text-stone-700 dark:text-stone-300">{p.productName}</span>
                        <span className="text-stone-500 dark:text-stone-400">{p.quantitySold} шт · {formatSomoni(p.revenue)} с.</span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              <div className="rounded-2xl border border-stone-200 bg-white p-5 dark:border-stone-800 dark:bg-stone-900">
                <h2 className="mb-4 flex items-center gap-2 font-medium text-stone-900 dark:text-stone-50">
                  <Truck size={16} /> Заказы по статусам
                </h2>
                {data.ordersByStatus.length === 0 ? (
                  <p className="text-sm text-stone-500 dark:text-stone-400">Заказов пока нет</p>
                ) : (
                  <ul className="flex flex-col gap-2">
                    {data.ordersByStatus.map((s) => (
                      <li key={s.status} className="flex items-center justify-between text-sm">
                        <span className="text-stone-700 dark:text-stone-300">{ORDER_STATUS_LABELS[s.status] ?? s.status}</span>
                        <span className="text-stone-500 dark:text-stone-400">{s.count}</span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </section>

            <section className="rounded-2xl border border-stone-200 bg-white p-5 dark:border-stone-800 dark:bg-stone-900">
              <h2 className="mb-4 font-medium text-stone-900 dark:text-stone-50">Выручка по месяцам</h2>
              <div className="flex items-end gap-3 overflow-x-auto pb-2">
                {data.revenueByMonth.map((m) => {
                  const max = Math.max(...data.revenueByMonth.map((r) => r.revenue), 1);
                  const heightPct = Math.max((m.revenue / max) * 100, 2);
                  return (
                    <div key={`${m.year}-${m.month}`} className="flex w-16 flex-col items-center gap-2">
                      <div className="flex h-32 w-full items-end">
                        <div className="w-full rounded-t-md bg-grove-600 dark:bg-grove-500" style={{ height: `${heightPct}%` }} />
                      </div>
                      <span className="text-xs text-stone-500 dark:text-stone-400">{MONTH_LABELS[m.month - 1]}</span>
                    </div>
                  );
                })}
              </div>
            </section>
          </div>
        )}
      </main>
    </div>
  );
}

function StatCard({ icon, label, value, sub }: { icon: ReactNode; label: string; value: string | number; sub: string }) {
  return (
    <div className="rounded-2xl border border-stone-200 bg-white p-5 dark:border-stone-800 dark:bg-stone-900">
      <div className="mb-2 flex items-center gap-2 text-stone-500 dark:text-stone-400">
        {icon}
        <span className="text-sm">{label}</span>
      </div>
      <p className="font-display text-2xl text-stone-900 dark:text-stone-50">{value}</p>
      <p className="mt-1 text-xs text-stone-500 dark:text-stone-400">{sub}</p>
    </div>
  );
}
