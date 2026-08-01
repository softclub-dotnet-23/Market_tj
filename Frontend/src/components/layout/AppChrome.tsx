import { useEffect } from "react";
import { Outlet, useLocation, useNavigationType } from "react-router-dom";
import { Toaster } from "sonner";
import { SmoothScroll } from "@/components/layout/SmoothScroll";
import { AiAssistantWidget } from "@/components/assistant/AiAssistantWidget";
import { useTheme } from "@/context/ThemeContext";

function ScrollToTop() {
  const { pathname } = useLocation();
  const navigationType = useNavigationType();
  useEffect(() => {
    // On back/forward (POP) navigation, leave scroll position alone — the
    // browser restores it natively, which feels smooth and unnoticeable.
    // Forcing scroll-to-top there is what made "back" feel like a jump.
    if (navigationType === "POP") return;
    window.scrollTo({ top: 0, behavior: "instant" as ScrollBehavior });
  }, [pathname, navigationType]);
  return null;
}

export function AppChrome() {
  const { theme } = useTheme();
  return (
    <>
      <ScrollToTop />
      <SmoothScroll />
      <Outlet />
      <AiAssistantWidget />
      <Toaster
        position="bottom-right"
        // Смещение от нижнего края — иначе тосты накладывались бы на плавающую
        // кнопку AI-ассистента (тоже bottom-right, см. AiAssistantWidget).
        offset={{ bottom: "6.5rem" }}
        theme={theme}
        toastOptions={{
          classNames: {
            toast:
              "rounded-2xl! border! border-stone-100! shadow-(--shadow-lifted)! font-sans! dark:border-stone-800! dark:bg-stone-900!",
            title: "text-stone-900! font-medium! dark:text-stone-50!",
            description: "text-stone-500! dark:text-stone-400!",
          },
        }}
      />
    </>
  );
}
