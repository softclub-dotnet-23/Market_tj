import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import Lenis from "lenis";
import { useAuth } from "@/context/AuthContext";

// Каталог/товар/профиль фермера/чекаут рендерятся внутри того же
// fixed-shell <main overflow-y-auto>, что и Admin/Farmer/Customer, когда их
// открывает вошедший покупатель (см. CustomerAwareShell.tsx) — гостю те же
// пути отдаются через обычный RootLayout с window-скроллом, поэтому для
// гостя Lenis там нужен, а для покупателя — нет.
const CUSTOMER_AWARE_PATHS = ["/catalog", "/product/", "/farmers/", "/checkout"];

export function SmoothScroll() {
  const { pathname } = useLocation();
  const { user } = useAuth();

  useEffect(() => {
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (prefersReducedMotion) return;

    // Admin/Farmer/Customer panels scroll inside their own fixed-shell <main
    // overflow-y-auto>, not the document/window — Lenis controls
    // window-level scroll by default, so there it swallows the wheel event
    // and scrolls nothing, breaking touchpad/mouse scroll entirely.
    const rendersInsideAccountShell =
      pathname.startsWith("/admin") ||
      pathname.startsWith("/farmer") ||
      pathname.startsWith("/customer") ||
      (user?.role === "Customer" && CUSTOMER_AWARE_PATHS.some((p) => pathname === p || pathname.startsWith(p)));
    if (rendersInsideAccountShell) return;

    const lenis = new Lenis({
      duration: 1.05,
      easing: (t) => 1 - Math.pow(1 - t, 3),
      smoothWheel: true,
    });

    let rafId: number;
    function raf(time: number) {
      lenis.raf(time);
      rafId = requestAnimationFrame(raf);
    }
    rafId = requestAnimationFrame(raf);

    return () => {
      cancelAnimationFrame(rafId);
      lenis.destroy();
    };
  }, [pathname, user?.role]);

  return null;
}
