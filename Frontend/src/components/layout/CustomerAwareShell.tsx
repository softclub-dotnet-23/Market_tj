import { useAuth } from "@/context/AuthContext";
import { CustomerLayout } from "@/components/layout/CustomerLayout";
import { RootLayout } from "@/components/layout/RootLayout";

// Каталог/товар/профиль фермера/чекаут доступны и гостю, и вошедшему
// покупателю — но покупатель не должен терять сайдбар личного кабинета,
// переходя туда по ссылкам "Каталог"/"Корзина" из своего же сайдбара
// (CustomerLayout уже умеет подсвечивать эти пункты как активные, см.
// isNavItemActive). Для всех остальных — гостя, Фермера/Админа/Курьера,
// которые технически тоже могут открыть /catalog — обычные публичные
// хедер/футер.
export function CustomerAwareShell() {
  const { user } = useAuth();
  return user?.role === "Customer" ? <CustomerLayout /> : <RootLayout />;
}
