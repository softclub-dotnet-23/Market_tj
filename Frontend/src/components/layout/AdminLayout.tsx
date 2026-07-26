import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  BarChart3,
  Bell,
  Bike,
  ChevronDown,
  ChevronsLeft,
  ChevronsRight,
  CreditCard,
  Leaf,
  LayoutDashboard,
  LogOut,
  MapPinned,
  MessageSquare,
  Package,
  Search,
  Settings,
  ShoppingCart,
  Sprout,
  Users,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Avatar } from "@/components/ui/Avatar";
import { LanguageSwitcher } from "@/components/ui/LanguageSwitcher";
import { ThemeToggle } from "@/components/ui/ThemeToggle";
import { AvatarMenuItem } from "@/components/layout/AvatarMenuItem";
import { useAuth } from "@/context/AuthContext";
import { useAdminOrders } from "@/data/adminEntities";
// Notification.UserId — прямой FK на User.Id, без промежуточного профиля
// (см. комментарий у useFarmerNotifications) — тот же generic-хук годится и
// для админа, несмотря на название.
import { useFarmerNotifications } from "@/data/farmer";
import { resolveMediaUrl } from "@/lib/api";
import { cn } from "@/lib/utils";

interface AdminNavItem {
  labelKey: string;
  path: string;
  icon: LucideIcon;
}

const NAV_ITEMS: AdminNavItem[] = [
  { labelKey: "overview", path: "/admin", icon: LayoutDashboard },
  { labelKey: "analytics", path: "/admin/statistics", icon: BarChart3 },
  { labelKey: "orders", path: "/admin/orders", icon: ShoppingCart },
  { labelKey: "products", path: "/admin/products", icon: Package },
  { labelKey: "farmers", path: "/admin/farmers", icon: Sprout },
  { labelKey: "couriers", path: "/admin/couriers", icon: Bike },
  { labelKey: "delivery", path: "/admin/delivery-zones", icon: MapPinned },
  { labelKey: "customers", path: "/admin/users", icon: Users },
  { labelKey: "payments", path: "/admin/commissions", icon: CreditCard },
  { labelKey: "reviews", path: "/admin/reviews", icon: MessageSquare },
  { labelKey: "notifications", path: "/admin/notifications", icon: Bell },
  { labelKey: "settings", path: "/admin/settings", icon: Settings },
];

export function AdminLayout() {
  const { t } = useTranslation("admin");
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [collapsed, setCollapsed] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const { orders } = useAdminOrders();
  const { notifications } = useFarmerNotifications(user?.userId ?? null);
  const unreadCount = notifications?.filter((n) => !n.isRead).length ?? 0;

  const currentItem = NAV_ITEMS.find((item) =>
    item.path === "/admin" ? location.pathname === "/admin" : location.pathname.startsWith(item.path),
  );

  useEffect(() => {
    function onClick(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setMenuOpen(false);
    }
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div className="flex h-screen bg-stone-25 dark:bg-stone-950">
      <aside
        className={cn(
          "flex shrink-0 flex-col border-r border-stone-100 bg-white transition-[width] duration-200 dark:border-stone-800 dark:bg-stone-900",
          collapsed ? "w-20" : "w-64",
        )}
      >
        <div className="flex h-18 items-center justify-between border-b border-stone-100 px-5 dark:border-stone-800">
          {!collapsed && (
            <div className="flex items-center gap-2">
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-grove-700 text-white">
                <Leaf size={18} />
              </span>
              <span className="font-display text-lg text-stone-900 dark:text-stone-50">
                Market<span className="text-grove-600 dark:text-grove-400">.tj</span>
              </span>
            </div>
          )}
          <button
            onClick={() => setCollapsed((c) => !c)}
            aria-label={t(collapsed ? "expandSidebar" : "collapseSidebar")}
            className={cn(
              "flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-stone-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-stone-200",
              collapsed && "mx-auto",
            )}
          >
            {collapsed ? <ChevronsRight size={16} /> : <ChevronsLeft size={16} />}
          </button>
        </div>

        <nav className="flex-1 overflow-y-auto p-3">
          <ul className="flex flex-col gap-1">
            {NAV_ITEMS.map((item) => {
              const isActive =
                item.path === "/admin" ? location.pathname === "/admin" : location.pathname.startsWith(item.path);
              const badge = item.labelKey === "orders" ? (orders?.length ?? 0) : item.labelKey === "notifications" ? unreadCount : 0;
              return (
                <li key={item.path}>
                  <NavLink
                    to={item.path}
                    className={cn(
                      "group flex items-center gap-3 rounded-2xl px-2.5 py-2 text-sm font-medium transition-all",
                      isActive
                        ? "bg-grove-50 text-grove-700 dark:bg-grove-950/70 dark:text-grove-300"
                        : "text-stone-600 hover:bg-stone-50 dark:text-stone-300 dark:hover:bg-stone-800",
                      collapsed && "justify-center px-0",
                    )}
                  >
                    <span
                      className={cn(
                        "flex h-8 w-8 shrink-0 items-center justify-center rounded-xl transition-all",
                        isActive
                          ? "bg-linear-to-br from-grove-500 to-grove-700 text-white shadow-[0_6px_14px_-4px_rgba(59,168,90,0.55)]"
                          : "bg-stone-100 text-stone-500 group-hover:bg-stone-200 group-hover:text-stone-700 dark:bg-stone-800 dark:text-stone-400 dark:group-hover:bg-stone-700 dark:group-hover:text-stone-200",
                      )}
                    >
                      <item.icon size={16} />
                    </span>
                    {!collapsed && <span className="flex-1 truncate">{t(`nav.${item.labelKey}`)}</span>}
                    {!collapsed && badge > 0 && (
                      <span
                        className={cn(
                          "flex h-5 min-w-5 items-center justify-center rounded-full px-1.5 text-[11px] font-semibold",
                          item.labelKey === "notifications"
                            ? "bg-clay-500 text-white"
                            : "bg-stone-100 text-stone-600 dark:bg-stone-800 dark:text-stone-300",
                        )}
                      >
                        {badge}
                      </span>
                    )}
                  </NavLink>
                </li>
              );
            })}
          </ul>
        </nav>

        <div className="border-t border-stone-100 p-3 dark:border-stone-800">
          <button
            onClick={handleLogout}
            className={cn(
              "flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-stone-500 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-400 dark:hover:bg-rose-950 dark:hover:text-rose-400",
              collapsed && "justify-center px-0",
            )}
          >
            <LogOut size={18} className="shrink-0" />
            {!collapsed && <span>{t("logout")}</span>}
          </button>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-18 shrink-0 items-center justify-between gap-4 border-b border-stone-100 bg-white px-6 shadow-(--shadow-soft) dark:border-stone-800 dark:bg-stone-900">
          <div>
            <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">
              {currentItem?.path === "/admin" ? t("dashboard.greeting") : t(`nav.${currentItem?.labelKey ?? "overview"}`)}
            </h1>
            {currentItem?.path === "/admin" && (
              <p className="text-sm text-stone-400 dark:text-stone-500">{t("dashboard.greetingSubtitle")}</p>
            )}
          </div>

          <div className="flex items-center gap-3">
            <div className="hidden h-10 items-center gap-2 rounded-xl border border-stone-200 bg-stone-25 px-3.5 sm:flex dark:border-stone-700 dark:bg-stone-950">
              <Search size={15} className="text-stone-400 dark:text-stone-500" />
              <input
                type="text"
                placeholder={t("search")}
                className="w-40 bg-transparent text-sm outline-none placeholder:text-stone-400 dark:text-stone-100 dark:placeholder:text-stone-500"
              />
            </div>
            <ThemeToggle />
            <LanguageSwitcher />
            <button
              aria-label={t("notifications")}
              onClick={() => navigate("/admin/notifications")}
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
                <Avatar name={user?.fullName ?? t("adminName")} src={user?.avatarUrl ? resolveMediaUrl(user.avatarUrl) : undefined} size={36} />
                <ChevronDown
                  size={14}
                  className={cn("text-stone-400 transition-transform dark:text-stone-500", menuOpen && "rotate-180")}
                />
              </button>

              {menuOpen && (
                <div className="absolute right-0 top-full z-50 mt-2 w-52 overflow-hidden rounded-2xl border border-stone-100 bg-white p-1.5 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900">
                  <div className="px-2.5 py-2">
                    <p className="truncate text-sm font-semibold text-stone-800 dark:text-stone-100">
                      {user?.fullName ?? t("adminName")}
                    </p>
                    <p className="truncate text-xs text-stone-400 dark:text-stone-500">{user?.email}</p>
                  </div>
                  <AvatarMenuItem />
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

        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
