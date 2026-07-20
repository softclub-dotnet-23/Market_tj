import { useTranslation } from "react-i18next";

const valueIds = [1, 2, 3, 4];

export function useAboutValues() {
  const { t } = useTranslation("data");
  return valueIds.map((id) => ({
    title: t(`aboutValues.${id}.title`),
    description: t(`aboutValues.${id}.description`),
  }));
}

const impactStatsBase = [
  { id: 1, value: 34, suffix: "%" },
  { id: 2, value: 214, suffix: "+" },
  { id: 3, value: 5, suffix: "" },
  { id: 4, value: 18 },
];

export function useImpactStats() {
  const { t } = useTranslation("data");
  return impactStatsBase.map((s) => ({
    ...s,
    suffix: s.id === 4 ? t("impactStatsHourSuffix") : s.suffix,
    label: t(`impactStats.${s.id}`),
  }));
}

const timelineYears = ["2024", "2025", "2026"];

export function useTimeline() {
  const { t } = useTranslation("data");
  return timelineYears.map((year) => ({
    year,
    title: t(`timeline.${year}.title`),
    description: t(`timeline.${year}.description`),
  }));
}

const teamValueIds = [1, 2, 3];

export function useTeamValues() {
  const { t } = useTranslation("data");
  return teamValueIds.map((id) => ({
    name: t(`teamValues.${id}.name`),
    focus: t(`teamValues.${id}.focus`),
  }));
}
