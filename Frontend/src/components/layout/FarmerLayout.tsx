import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  Bell,
  ChevronDown,
  ChevronsLeft,
  ChevronsRight,
  FileText,
  LayoutDashboard,
  Leaf,
  LogOut,
  MessageSquare,
  Package,
  Search,
  ShoppingCart,
  Users,
  UserRound,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Avatar } from "@/components/ui/Avatar";
import { LanguageSwitcher } from "@/components/ui/LanguageSwitcher";
import { useAuth } from "@/context/AuthContext";
import { cn } from "@/lib/utils";

interface FarmerNavItem {
  labelKey: string;
  path: string;
  icon: LucideIcon;
}

const NAV_ITEMS: FarmerNavItem[] = [
  { labelKey: "overview", path: "/farmer", icon: LayoutDashboard },
  { labelKey: "products", path: "/farmer/products", icon: Package },
  { labelKey: "orders", path: "/farmer/orders", icon: ShoppingCart },
  { labelKey: "reviews", path: "/farmer/reviews", icon: MessageSquare },
  { labelKey: "profile", path: "/farmer/profile", icon: UserRound },
  { labelKey: "documents", path: "/farmer/documents", icon: FileText },
  { labelKey: "staff", path: "/farmer/staff", icon: Users },
  { labelKey: "notifications", path: "/farmer/notifications", icon: Bell },
];

export function FarmerLayout() {
  const { t } = useTranslation("farmer");
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [collapsed, setCollapsed] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  const currentItem = NAV_ITEMS.find((item) =>
    item.path === "/farmer" ? location.pathname === "/farmer" : location.pathname.startsWith(item.path),
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
                item.path === "/farmer" ? location.pathname === "/farmer" : location.pathname.startsWith(item.path);
              return (
                <li key={item.path}>
                  <NavLink
                    to={item.path}
                    className={cn(
                      "flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition",
                      isActive
                        ? "bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-300"
                        : "text-stone-600 hover:bg-stone-50 dark:text-stone-300 dark:hover:bg-stone-800",
                      collapsed && "justify-center px-0",
                    )}
                  >
                    <item.icon size={18} className="shrink-0" />
                    {!collapsed && <span className="flex-1 truncate">{t(`nav.${item.labelKey}`)}</span>}
                  </NavLink>
                </li>
              );
            })}
          </ul>
        </nav>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-18 shrink-0 items-center justify-between gap-4 border-b border-stone-100 bg-white px-6 dark:border-stone-800 dark:bg-stone-900">
          <div>
            <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">
              {currentItem?.path === "/farmer" ? t("dashboard.greeting") : t(`nav.${currentItem?.labelKey ?? "overview"}`)}
            </h1>
            {currentItem?.path === "/farmer" && (
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
            <LanguageSwitcher />
            <button
              aria-label={t("notifications")}
              className="flex h-10 w-10 items-center justify-center rounded-full text-stone-500 transition hover:bg-stone-100 dark:text-stone-400 dark:hover:bg-stone-800"
            >
              <Bell size={18} />
            </button>
            <div ref={menuRef} className="relative">
              <button
                onClick={() => setMenuOpen((o) => !o)}
                className="flex items-center gap-2 rounded-full py-1 pl-1 pr-2 transition hover:bg-stone-100 dark:hover:bg-stone-800"
              >
                <Avatar name={user?.fullName ?? t("farmerName")} size={36} />
                <ChevronDown
                  size={14}
                  className={cn("text-stone-400 transition-transform dark:text-stone-500", menuOpen && "rotate-180")}
                />
              </button>

              {menuOpen && (
                <div className="absolute right-0 top-full z-50 mt-2 w-52 overflow-hidden rounded-2xl border border-stone-100 bg-white p-1.5 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900">
                  <div className="px-2.5 py-2">
                    <p className="truncate text-sm font-semibold text-stone-800 dark:text-stone-100">
                      {user?.fullName ?? t("farmerName")}
                    </p>
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

        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
