import { useEffect, useRef, useState } from "react";
import { Link, NavLink, useLocation, useNavigate } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { useTranslation } from "react-i18next";
import { ChevronDown, Heart, LayoutDashboard, Leaf, LogOut, Menu, Search, ShoppingBag, User, X } from "lucide-react";
import { MegaMenu } from "@/components/layout/MegaMenu";
import { MobileMenu } from "@/components/layout/MobileMenu";
import { MiniCart } from "@/components/layout/MiniCart";
import { AvatarMenuItem } from "@/components/layout/AvatarMenuItem";
import { Button } from "@/components/ui/Button";
import { Avatar } from "@/components/ui/Avatar";
import { ThemeToggle } from "@/components/ui/ThemeToggle";
import { LanguageSwitcher } from "@/components/ui/LanguageSwitcher";
import { useAuth } from "@/context/AuthContext";
import { useCart } from "@/context/CartContext";
import { useFavorites } from "@/context/FavoritesContext";
import { resolveMediaUrl } from "@/lib/api";
import { cn } from "@/lib/utils";

export function Header() {
  const { t } = useTranslation(["layout", "common"]);
  const NAV_LINKS = [
    { to: "/", label: t("layout:nav.home"), end: true },
    { to: "/catalog", label: t("layout:nav.catalog") },
    { to: "/about", label: t("layout:nav.about") },
    { to: "/contact", label: t("layout:nav.contact") },
  ];
  const [scrolled, setScrolled] = useState(false);
  const [megaOpen, setMegaOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [cartOpen, setCartOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [searchValue, setSearchValue] = useState("");
  const [accountMenuOpen, setAccountMenuOpen] = useState(false);
  const closeTimer = useRef<ReturnType<typeof setTimeout>>(undefined);
  const accountMenuRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const { pathname } = useLocation();

  const { totalItems } = useCart();
  const { favoriteIds } = useFavorites();
  const { user, logout } = useAuth();

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 12);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  useEffect(() => {
    function onClick(e: MouseEvent) {
      if (accountMenuRef.current && !accountMenuRef.current.contains(e.target as Node)) setAccountMenuOpen(false);
    }
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  const handleLogout = () => {
    logout();
    setAccountMenuOpen(false);
    navigate("/");
  };

  const panelPath =
    user?.role === "Admin" ? "/admin" : user?.role === "Farmer" ? "/farmer" : user?.role === "Customer" ? "/customer" : null;

  useEffect(() => {
    clearTimeout(closeTimer.current);
    setMegaOpen(false);
  }, [pathname]);

  const openMega = () => {
    clearTimeout(closeTimer.current);
    setMegaOpen(true);
  };
  const closeMegaDelayed = () => {
    closeTimer.current = setTimeout(() => setMegaOpen(false), 150);
  };

  const submitSearch = (e: React.FormEvent) => {
    e.preventDefault();
    navigate(searchValue ? `/catalog?search=${encodeURIComponent(searchValue)}` : "/catalog");
    setSearchOpen(false);
  };

  return (
    <header
      className={cn(
        "sticky top-0 z-50 w-full border-b transition-all duration-300",
        scrolled
          ? "border-stone-100 bg-white/85 backdrop-blur-lg dark:border-stone-800 dark:bg-stone-950/85"
          : "border-transparent bg-white/60 backdrop-blur-sm dark:bg-stone-950/60",
      )}
    >
      <div className="container-page flex h-18 items-center justify-between gap-4">
        <Link to="/" className="flex shrink-0 items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-grove-700 text-white shadow-(--shadow-glow-grove)">
            <Leaf size={18} />
          </span>
          <span className="font-display text-xl text-stone-900 dark:text-stone-50">
            Market<span className="text-grove-600 dark:text-grove-400">.tj</span>
          </span>
        </Link>

        <nav className="hidden items-center gap-1 lg:flex">
          {NAV_LINKS.map((link) =>
            link.to === "/catalog" ? (
              <div
                key={link.to}
                className="relative"
                onMouseEnter={openMega}
                onMouseLeave={closeMegaDelayed}
              >
                <NavLink
                  to={link.to}
                  onClick={() => setMegaOpen(false)}
                  className={({ isActive }) =>
                    cn(
                      "whitespace-nowrap rounded-lg px-4 py-2 text-sm font-medium text-stone-600 transition hover:bg-stone-100 hover:text-stone-900 dark:text-stone-300 dark:hover:bg-stone-800 dark:hover:text-stone-50",
                      isActive && "text-grove-700 dark:text-grove-400",
                    )
                  }
                >
                  {link.label}
                </NavLink>
                <AnimatePresence>{megaOpen && <MegaMenu onNavigate={() => setMegaOpen(false)} />}</AnimatePresence>
              </div>
            ) : (
              <NavLink
                key={link.to}
                to={link.to}
                end={link.end}
                className={({ isActive }) =>
                  cn(
                    "whitespace-nowrap rounded-lg px-4 py-2 text-sm font-medium text-stone-600 transition hover:bg-stone-100 hover:text-stone-900",
                    isActive && "text-grove-700",
                  )
                }
              >
                {link.label}
              </NavLink>
            ),
          )}
        </nav>

        <div className="flex items-center gap-1.5 sm:gap-2">
          <div className="relative hidden sm:block">
            <AnimatePresence mode="wait">
              {searchOpen ? (
                <motion.form
                  key="input"
                  initial={{ width: 40, opacity: 0 }}
                  animate={{ width: 240, opacity: 1 }}
                  exit={{ width: 40, opacity: 0 }}
                  transition={{ duration: 0.25, ease: [0.25, 1, 0.5, 1] }}
                  onSubmit={submitSearch}
                  className="overflow-hidden"
                >
                  <input
                    autoFocus
                    value={searchValue}
                    onChange={(e) => setSearchValue(e.target.value)}
                    onBlur={() => !searchValue && setSearchOpen(false)}
                    placeholder={t("common:actions.searchPlaceholder")}
                    className="h-10 w-full rounded-full border border-stone-200 bg-white px-4 text-sm focus:border-grove-500 focus:ring-2 focus:ring-grove-100 dark:border-stone-700 dark:bg-stone-900 dark:text-stone-100 dark:placeholder:text-stone-500"
                  />
                </motion.form>
              ) : (
                <motion.button
                  key="icon"
                  onClick={() => setSearchOpen(true)}
                  aria-label={t("common:actions.search")}
                  className="flex h-10 w-10 items-center justify-center rounded-full text-stone-600 transition hover:bg-stone-100 dark:text-stone-300 dark:hover:bg-stone-800"
                >
                  <Search size={19} />
                </motion.button>
              )}
            </AnimatePresence>
          </div>

          <ThemeToggle />
          <LanguageSwitcher />

          <Link
            to="/catalog?favorites=1"
            aria-label={t("common:actions.favorites")}
            className="relative hidden h-10 w-10 items-center justify-center rounded-full text-stone-600 transition hover:bg-stone-100 dark:text-stone-300 dark:hover:bg-stone-800 sm:flex"
          >
            <Heart size={19} />
            {favoriteIds.length > 0 && (
              <span className="absolute -right-0.5 -top-0.5 flex h-4.5 w-4.5 items-center justify-center rounded-full bg-clay-500 text-[10px] font-bold text-white">
                {favoriteIds.length}
              </span>
            )}
          </Link>

          <div className="relative">
            <button
              onClick={() => setCartOpen((o) => !o)}
              aria-label={t("common:actions.cart")}
              className="relative flex h-10 w-10 items-center justify-center rounded-full text-stone-600 transition hover:bg-stone-100 dark:text-stone-300 dark:hover:bg-stone-800"
            >
              <ShoppingBag size={19} />
              {totalItems > 0 && (
                <span className="absolute -right-0.5 -top-0.5 flex h-4.5 w-4.5 items-center justify-center rounded-full bg-grove-700 text-[10px] font-bold text-white">
                  {totalItems}
                </span>
              )}
            </button>
            <AnimatePresence>{cartOpen && <MiniCart onClose={() => setCartOpen(false)} />}</AnimatePresence>
          </div>

          {user ? (
            <div ref={accountMenuRef} className="relative hidden md:block">
              <button
                onClick={() => setAccountMenuOpen((o) => !o)}
                className="flex items-center gap-1.5 rounded-full py-1 pl-1 pr-2 transition hover:bg-stone-100 dark:hover:bg-stone-800"
              >
                <Avatar name={user.fullName} src={user.avatarUrl ? resolveMediaUrl(user.avatarUrl) : undefined} size={32} />
                <ChevronDown
                  size={14}
                  className={cn("text-stone-400 transition-transform dark:text-stone-500", accountMenuOpen && "rotate-180")}
                />
              </button>

              {accountMenuOpen && (
                <div className="absolute right-0 top-full z-50 mt-2 w-56 overflow-hidden rounded-2xl border border-stone-100 bg-white p-1.5 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900">
                  <div className="px-2.5 py-2">
                    <p className="truncate text-sm font-semibold text-stone-800 dark:text-stone-100">{user.fullName}</p>
                    <p className="truncate text-xs text-stone-400 dark:text-stone-500">{user.email}</p>
                  </div>
                  {panelPath && (
                    <Link
                      to={panelPath}
                      onClick={() => setAccountMenuOpen(false)}
                      className="flex w-full items-center gap-2.5 rounded-xl px-2.5 py-2 text-left text-sm text-stone-600 transition hover:bg-stone-50 dark:text-stone-300 dark:hover:bg-stone-800"
                    >
                      <LayoutDashboard size={15} />
                      {t("common:auth.goToPanel")}
                    </Link>
                  )}
                  <AvatarMenuItem />
                  <button
                    onClick={handleLogout}
                    className="flex w-full items-center gap-2.5 rounded-xl px-2.5 py-2 text-left text-sm text-stone-600 transition hover:bg-stone-50 dark:text-stone-300 dark:hover:bg-stone-800"
                  >
                    <LogOut size={15} />
                    {t("common:auth.logout")}
                  </button>
                </div>
              )}
            </div>
          ) : (
            <div className="hidden items-center gap-2 pl-1 md:flex">
              <Button variant="ghost" size="sm" onClick={() => navigate("/login")} leftIcon={<User size={15} />}>
                {t("common:auth.login")}
              </Button>
              <Button size="sm" onClick={() => navigate("/register")}>
                {t("common:auth.register")}
              </Button>
            </div>
          )}

          <button
            onClick={() => setMobileOpen(true)}
            aria-label={t("common:actions.openMenu")}
            className="flex h-10 w-10 items-center justify-center rounded-full text-stone-700 transition hover:bg-stone-100 dark:text-stone-200 dark:hover:bg-stone-800 lg:hidden"
          >
            <Menu size={21} />
          </button>
        </div>
      </div>

      <AnimatePresence>
        {mobileOpen && <MobileMenu onClose={() => setMobileOpen(false)} />}
      </AnimatePresence>
    </header>
  );
}

export function CloseIconButton({ onClick }: { onClick: () => void }) {
  const { t } = useTranslation("common");
  return (
    <button
      onClick={onClick}
      aria-label={t("actions.close")}
      className="flex h-9 w-9 items-center justify-center rounded-full bg-stone-100 text-stone-500 hover:bg-stone-200 dark:bg-stone-800 dark:text-stone-400 dark:hover:bg-stone-700"
    >
      <X size={16} />
    </button>
  );
}
