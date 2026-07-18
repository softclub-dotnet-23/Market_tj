import { useTranslation } from "react-i18next";

const platformStatsBase = [
  { id: 1, value: 214, suffix: "+" },
  { id: 2, value: 1860, suffix: "+" },
  { id: 3, value: 32400, suffix: "+" },
  { id: 4, value: 5, suffix: "" },
];

export function usePlatformStats() {
  const { t } = useTranslation("data");
  return platformStatsBase.map((s) => ({ ...s, label: t(`platformStats.${s.id}`) }));
}

export const regions = [
  "Все регионы",
  "г. Душанбе",
  "Согдийская область",
  "Хатлонская область",
  "Районы республиканского подчинения",
  "ГБАО",
];

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
