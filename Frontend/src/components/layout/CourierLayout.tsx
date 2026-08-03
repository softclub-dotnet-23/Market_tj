import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Bell, ChevronDown, Leaf, LogOut, Menu, Truck, User } from "lucide-react";
import { Avatar } from "@/components/ui/Avatar";
import { Switch } from "@/components/ui/Switch";
import { PanelMobileDrawer } from "@/components/layout/PanelMobileDrawer";
import { LanguageSwitcher } from "@/components/ui/LanguageSwitcher";
import { ThemeToggle } from "@/components/ui/ThemeToggle";
import { useAuth } from "@/context/AuthContext";
import { resolveMediaUrl } from "@/lib/api";
import { useFarmerNotifications } from "@/data/farmer";
import { useMyDeliveries } from "@/data/delivery";
import { useCourierProfile, setCourierAvailability } from "@/data/courier";
import { cn } from "@/lib/utils";

// Минимальная курьерская панель — по прямому запросу пользователя (2026-08-02):
// без отдельной публичной страницы доставки, без полноценной 4-й панели
// (раздел 4.5/14 ТЗ — "Courier mini-interface", не полноценная панель), всего
// один основной раздел "Мои доставки" + уведомления. Структура — тот же
// паттерн, что FarmerLayout/AdminLayout/CustomerLayout (сайдбар/мобильный
// дровер/шапка), просто с гораздо меньшим числом пунктов меню.
export function CourierLayout() {
  const { t } = useTranslation("courier");
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const { notifications } = useFarmerNotifications(user?.userId ?? null);
  const unreadCount = notifications?.filter((n) => !n.isRead).length ?? 0;
  const { deliveries } = useMyDeliveries();
  const activeDeliveriesCount = deliveries?.filter((d) => d.status !== 9 && d.status !== 10).length ?? 0;
  const [availabilityRefreshKey, setAvailabilityRefreshKey] = useState(0);
  const [togglingAvailability, setTogglingAvailability] = useState(false);
  const { profile } = useCourierProfile(availabilityRefreshKey);

  // Переключатель "доступен для заказов" вынесен прямо в шапку — по
  // прямому запросу пользователя (2026-08-03): курьеру должно быть заметно
  // и легко переключать статус, не заходя отдельно в раздел профиля.
  const handleToggleAvailability = async () => {
    if (!profile) return;
    setTogglingAvailability(true);
    const next = !profile.isAvailable;
    try {
      await setCourierAvailability(profile, next);
      toast.success(next ? t("profile.availableOnSuccess") : t("profile.availableOffSuccess"));
      setAvailabilityRefreshKey((k) => k + 1);
    } catch (err) {
      toast.error(t("profile.availabilityError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setTogglingAvailability(false);
    }
  };

  useEffect(() => {
    function onClick(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setMenuOpen(false);
    }
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  useEffect(() => {
    setMobileNavOpen(false);
  }, [location.pathname]);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  const navList = () => (
    <ul className="flex flex-col gap-1 p-3">
      <li>
        <NavLink
          to="/courier"
          className={cn(
            "group flex items-center gap-3 rounded-2xl px-2.5 py-2 text-sm font-medium transition-all",
            location.pathname === "/courier"
              ? "bg-grove-50 text-grove-700 dark:bg-grove-950/70 dark:text-grove-300"
              : "text-stone-600 hover:bg-stone-50 dark:text-stone-300 dark:hover:bg-stone-800",
          )}
        >
          <span
            className={cn(
              "flex h-8 w-8 shrink-0 items-center justify-center rounded-xl transition-all",
              location.pathname === "/courier"
                ? "bg-linear-to-br from-grove-500 to-grove-700 text-white shadow-[0_6px_14px_-4px_rgba(59,168,90,0.55)]"
                : "bg-stone-100 text-stone-500 group-hover:bg-stone-200 group-hover:text-stone-700 dark:bg-stone-800 dark:text-stone-400 dark:group-hover:bg-stone-700 dark:group-hover:text-stone-200",
            )}
          >
            <Truck size={16} />
          </span>
          <span className="flex-1 truncate">{t("nav.myDeliveries")}</span>
          {activeDeliveriesCount > 0 && (
            <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-stone-100 px-1.5 text-[11px] font-semibold text-stone-600 dark:bg-stone-800 dark:text-stone-300">
              {activeDeliveriesCount}
            </span>
          )}
        </NavLink>
      </li>
      <li>
        <NavLink
          to="/courier/profile"
          className={cn(
            "group flex items-center gap-3 rounded-2xl px-2.5 py-2 text-sm font-medium transition-all",
            location.pathname === "/courier/profile"
              ? "bg-grove-50 text-grove-700 dark:bg-grove-950/70 dark:text-grove-300"
              : "text-stone-600 hover:bg-stone-50 dark:text-stone-300 dark:hover:bg-stone-800",
          )}
        >
          <span
            className={cn(
              "flex h-8 w-8 shrink-0 items-center justify-center rounded-xl transition-all",
              location.pathname === "/courier/profile"
                ? "bg-linear-to-br from-grove-500 to-grove-700 text-white shadow-[0_6px_14px_-4px_rgba(59,168,90,0.55)]"
                : "bg-stone-100 text-stone-500 group-hover:bg-stone-200 group-hover:text-stone-700 dark:bg-stone-800 dark:text-stone-400 dark:group-hover:bg-stone-700 dark:group-hover:text-stone-200",
            )}
          >
            <User size={16} />
          </span>
          <span className="flex-1 truncate">{t("nav.profile")}</span>
        </NavLink>
      </li>
    </ul>
  );

  return (
    <div className="flex h-screen bg-stone-25 dark:bg-stone-950">
      <aside className="relative hidden w-64 shrink-0 flex-col border-r border-stone-100 bg-white lg:flex dark:border-stone-800 dark:bg-stone-900">
        <div className="flex h-18 shrink-0 items-center border-b border-stone-100 px-5 dark:border-stone-800">
          <div className="flex items-center gap-2">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-grove-700 text-white">
              <Leaf size={18} />
            </span>
            <span className="font-display text-lg text-stone-900 dark:text-stone-50">
              Market<span className="text-grove-600 dark:text-grove-400">.tj</span>
            </span>
          </div>
        </div>

        <nav className="flex-1 overflow-y-auto">{navList()}</nav>

        <div className="border-t border-stone-100 p-3 dark:border-stone-800">
          <button
            onClick={handleLogout}
            className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-stone-500 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-400 dark:hover:bg-rose-950 dark:hover:text-rose-400"
          >
            <LogOut size={18} className="shrink-0" />
            <span>{t("logout")}</span>
          </button>
        </div>
      </aside>

      <PanelMobileDrawer open={mobileNavOpen} onClose={() => setMobileNavOpen(false)}>
        <div className="flex h-18 items-center border-b border-stone-100 px-5 dark:border-stone-800">
          <div className="flex items-center gap-2">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-grove-700 text-white">
              <Leaf size={18} />
            </span>
            <span className="font-display text-lg text-stone-900 dark:text-stone-50">
              Market<span className="text-grove-600 dark:text-grove-400">.tj</span>
            </span>
          </div>
        </div>
        <nav className="flex-1 overflow-y-auto">{navList()}</nav>
        <div className="border-t border-stone-100 p-3 dark:border-stone-800">
          <button
            onClick={handleLogout}
            className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-stone-500 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-400 dark:hover:bg-rose-950 dark:hover:text-rose-400"
          >
            <LogOut size={18} className="shrink-0" />
            <span>{t("logout")}</span>
          </button>
        </div>
      </PanelMobileDrawer>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-18 shrink-0 items-center justify-between gap-2 border-b border-stone-100 bg-white px-4 shadow-(--shadow-soft) sm:gap-4 sm:px-6 dark:border-stone-800 dark:bg-stone-900">
          <div className="flex min-w-0 items-center gap-3">
            <button
              onClick={() => setMobileNavOpen(true)}
              aria-label={t("nav.myDeliveries")}
              className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-stone-600 transition hover:bg-stone-100 lg:hidden dark:text-stone-300 dark:hover:bg-stone-800"
            >
              <Menu size={20} />
            </button>
            <h1 className="truncate font-display text-lg text-stone-900 sm:text-xl dark:text-stone-50">{t("nav.myDeliveries")}</h1>
          </div>

          <div className="flex shrink-0 items-center gap-1 sm:gap-3">
            {profile && (
              <div
                className={cn(
                  "hidden items-center gap-2 rounded-full border px-3 py-1.5 sm:flex",
                  profile.isAvailable
                    ? "border-grove-200 bg-grove-50 dark:border-grove-900 dark:bg-grove-950/40"
                    : "border-stone-200 bg-stone-50 dark:border-stone-700 dark:bg-stone-800/60",
                )}
              >
                <span
                  className={cn(
                    "text-xs font-semibold whitespace-nowrap",
                    profile.isAvailable ? "text-grove-700 dark:text-grove-400" : "text-stone-500 dark:text-stone-400",
                  )}
                >
                  {profile.isAvailable ? t("profile.availableOn") : t("profile.availableOff")}
                </span>
                <Switch
                  checked={profile.isAvailable}
                  onChange={handleToggleAvailability}
                  disabled={togglingAvailability}
                  size="sm"
                  aria-label={t("profile.availableOn")}
                />
              </div>
            )}
            <ThemeToggle />
            <LanguageSwitcher />
            <button
              aria-label={t("notifications")}
              onClick={() => navigate("/courier/notifications")}
              className="relative flex h-10 w-10 items-center justify-center rounded-full text-stone-500 transition hover:bg-stone-100 dark:text-stone-400 dark:hover:bg-stone-800"
            >
              <Bell size={18} />
              {unreadCount > 0 && (
                <span className="absolute right-1.5 top-1.5 flex h-4 w-4 items-center justify-center rounded-full bg-clay-500 text-[10px] font-bold text-white">
                  {unreadCount > 9 ? "9+" : unreadCount}
                </span>
              )}
            </button>
            <div ref={menuRef} className="relative">
              <button
                onClick={() => setMenuOpen((o) => !o)}
                className="flex items-center gap-2 rounded-full py-1 pl-1 pr-2 transition hover:bg-stone-100 dark:hover:bg-stone-800"
              >
                <Avatar name={user?.fullName ?? t("courierName")} src={user?.avatarUrl ? resolveMediaUrl(user.avatarUrl) : undefined} size={36} />
                <ChevronDown size={14} className={cn("hidden text-stone-400 transition-transform sm:block dark:text-stone-500", menuOpen && "rotate-180")} />
              </button>

              {menuOpen && (
                <div className="absolute right-0 top-full z-50 mt-2 w-52 overflow-hidden rounded-2xl border border-stone-100 bg-white p-1.5 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900">
                  <div className="px-2.5 py-2">
                    <p className="truncate text-sm font-semibold text-stone-800 dark:text-stone-100">{user?.fullName ?? t("courierName")}</p>
                    <p className="truncate text-xs text-stone-400 dark:text-stone-500">{user?.email}</p>
                  </div>
                  <button
                    onClick={handleLogout}
                    className="flex w-full items-center gap-2.5 rounded-xl px-2.5 py-2 text-left text-sm text-stone-600 transition hover:bg-stone-50 dark:text-stone-300 dark:hover:bg-stone-800"
                  >
                    <LogOut size={15} />
                    {t("logout")}
                  </button>
                </div>
              )}
            </div>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-4 sm:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
