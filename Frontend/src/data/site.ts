import { useTranslation } from "react-i18next";
import { useProducts } from "@/data/products";
import { useFarmers } from "@/data/farmers";

// Раньше все 4 числа были захардкожены (214/1860/32400/5) — теперь настоящие
// счётчики из каталога. "Отзывов покупателей" вместо "Выполненных заказов" —
// /api/orders и /api/order-items требуют входа (см. data/catalogStore.ts),
// гостю недоступны, поэтому для главной страницы взят реальный публичный
// сигнал (отзывы уже посчитаны на фермера в useFarmers()), а не подделан ноль.
export function usePlatformStats() {
  const { t } = useTranslation("data");
  const products = useProducts();
  const farmers = useFarmers();
  const reviewsCount = farmers.reduce((sum, f) => sum + f.reviewCount, 0);
  const regionsCount = new Set(products.map((p) => p.region)).size;

  return [
    { id: 1, value: farmers.length, suffix: "+", label: t("platformStats.1") },
    { id: 2, value: products.length, suffix: "+", label: t("platformStats.2") },
    { id: 3, value: reviewsCount, suffix: "+", label: t("platformStats.3") },
    { id: 4, value: regionsCount, suffix: "", label: t("platformStats.4") },
  ];
}

const deliveryStepsBase = [{ id: 1 }, { id: 2 }, { id: 3 }, { id: 4 }];

export function useDeliverySteps() {
  const { t } = useTranslation("data");
  return deliveryStepsBase.map((s) => ({
    ...s,
    title: t(`deliverySteps.${s.id}.title`),
    description: t(`deliverySteps.${s.id}.description`),
  }));
}

const whyChooseUsBase = [{ id: 1 }, { id: 2 }, { id: 3 }, { id: 4 }];

export function useWhyChooseUs() {
  const { t } = useTranslation("data");
  return whyChooseUsBase.map((s) => ({
    ...s,
    title: t(`whyChooseUs.${s.id}.title`),
    description: t(`whyChooseUs.${s.id}.description`),
  }));
}

export function useOfficeInfo() {
  const { t } = useTranslation("data");
  return {
    address: t("officeInfo.address"),
    phone: "+992 37 221 04 15",
    phoneAlt: "+992 90 000 12 34",
    email: "hello@market.tj",
    hours: t("officeInfo.hours"),
  };
}
