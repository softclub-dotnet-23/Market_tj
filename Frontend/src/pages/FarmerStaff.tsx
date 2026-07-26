import { useTranslation } from "react-i18next";
import { Users } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { formatDate } from "@/lib/utils";
import { StaffPermissions, useFarmerProfile, useFarmerStaff } from "@/data/farmer";

export function FarmerStaff() {
  const { t } = useTranslation("farmer");
  const { profile, loading: profileLoading, error: profileError } = useFarmerProfile();
  const { staff, loading: staffLoading, error: staffError } = useFarmerStaff(profile?.id ?? null);

  if (profileLoading || (profile && staffLoading)) return <PageLoader />;

  if (profileError || staffError || !profile || !staff) {
    return (
      <EmptyState
        icon={<Users size={26} />}
        title={t("staff.errorTitle")}
        description={profileError ?? staffError ?? t("staff.errorDescription")}
      />
    );
  }

  const permissionLabels = (permissions: number) => {
    const labels: string[] = [];
    if (permissions & StaffPermissions.ManageProducts) labels.push(t("staff.permissions.manageProducts"));
    if (permissions & StaffPermissions.ManageStock) labels.push(t("staff.permissions.manageStock"));
    return labels.length > 0 ? labels : [t("staff.permissions.none")];
  };

  return (
    <div className="flex flex-col gap-5">
      {staff.length === 0 ? (
        <EmptyState icon={<Users size={26} />} title={t("staff.emptyTitle")} description={t("staff.emptyDescription")} />
      ) : (
        <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="px-6 py-4 font-medium">{t("staff.columns.user")}</th>
                  <th className="px-6 py-4 font-medium">{t("staff.columns.permissions")}</th>
                  <th className="px-6 py-4 font-medium">{t("staff.columns.status")}</th>
                  <th className="px-6 py-4 font-medium">{t("staff.columns.createdAt")}</th>
                </tr>
              </thead>
              <tbody>
                {staff.map((member) => (
                  <tr key={member.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{t("staff.userLabel", { id: member.userId })}</td>
                    <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{permissionLabels(member.permissions).join(", ")}</td>
                    <td className="px-6 py-4">
                      <span
                        className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                          member.isActive
                            ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
                            : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
                        }`}
                      >
                        {t(member.isActive ? "staff.status.active" : "staff.status.inactive")}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(member.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <p className="rounded-2xl bg-stone-50 px-4 py-3 text-xs text-stone-500 dark:bg-stone-900 dark:text-stone-400">{t("staff.addHint")}</p>
    </div>
  );
}
