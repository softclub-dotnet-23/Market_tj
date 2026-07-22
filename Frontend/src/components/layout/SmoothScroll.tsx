import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import Lenis from "lenis";

export function SmoothScroll() {
  const { pathname } = useLocation();

  useEffect(() => {
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (prefersReducedMotion) return;

    // Admin/Farmer panels scroll inside their own fixed-shell <main
    // overflow-y-auto>, not the document/window — Lenis controls
    // window-level scroll by default, so there it swallows the wheel event
    // and scrolls nothing, breaking touchpad/mouse scroll entirely.
    if (pathname.startsWith("/admin") || pathname.startsWith("/farmer")) return;

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
  }, [pathname]);

  return null;
}
