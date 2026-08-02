import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { useLocation, useNavigate } from "react-router-dom";
import { AnimatePresence, motion, useMotionValue, useReducedMotion, useSpring, useTransform } from "framer-motion";
import { Check, Loader2, Send, X } from "lucide-react";
import { type AssistantActionDto, type AssistantIntent, askAssistant, executeAssistantAction } from "@/data/aiAssistant";
import { getCatalogProductById } from "@/data/catalogStore";
import { useAuth } from "@/context/AuthContext";
import { ApiError, resolveMediaUrl } from "@/lib/api";
import { cn, formatSomoni } from "@/lib/utils";
import { AssistantMascot } from "@/components/assistant/AssistantMascot";

type ActionStatus = "idle" | "loading" | "done" | "cancelled" | "error";

interface AssistantMessage {
  id: number;
  role: "user" | "assistant";
  text: string;
  intent?: AssistantIntent;
  productId?: number | null;
  categoryId?: number | null;
  action?: AssistantActionDto | null;
  actionStatus?: ActionStatus;
}

// Не показываем на страницах входа/регистрации — там нет смысла. В
// админ- и фермер-панелях виджет тоже нужен (роль-осознанные инструменты,
// раздел 38 ТЗ), поэтому в отличие от первой версии их здесь нет.
const HIDDEN_PATH_PREFIXES = ["/login", "/register", "/forgot-password"];

let messageIdCounter = 0;

export function AiAssistantWidget() {
  const { t } = useTranslation("aiAssistant");
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const prefersReducedMotion = useReducedMotion();
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<AssistantMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [sending, setSending] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Настоящая 3D-анимация кнопки, посчитанная в JS: позиция курсора внутри
  // кнопки превращается в наклон по X/Y (perspective + rotateX/rotateY),
  // сглаженный пружиной — кнопка "наклоняется" к курсору. Чисто визуальный
  // эффект, к open/setOpen не относится.
  const pointerX = useMotionValue(0);
  const pointerY = useMotionValue(0);
  const springConfig = { stiffness: 300, damping: 22, mass: 0.6 };
  const buttonRotateX = useSpring(useTransform(pointerY, [-0.5, 0.5], [16, -16]), springConfig);
  const buttonRotateY = useSpring(useTransform(pointerX, [-0.5, 0.5], [-16, 16]), springConfig);

  const handleButtonPointerMove = (e: React.MouseEvent<HTMLButtonElement>) => {
    if (prefersReducedMotion) return;
    const rect = e.currentTarget.getBoundingClientRect();
    pointerX.set((e.clientX - rect.left) / rect.width - 0.5);
    pointerY.set((e.clientY - rect.top) / rect.height - 0.5);
  };
  const resetButtonTilt = () => {
    pointerX.set(0);
    pointerY.set(0);
  };

  useEffect(() => {
    if (open) textareaRef.current?.focus();
  }, [open]);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [messages]);

  if (HIDDEN_PATH_PREFIXES.some((p) => location.pathname.startsWith(p))) return null;

  const roleSuffix = user?.role === "Farmer" ? "Farmer" : user?.role === "Admin" ? "Admin" : "";

  const pushMessage = (msg: Omit<AssistantMessage, "id">) => {
    setMessages((prev) => [...prev, { ...msg, id: ++messageIdCounter }]);
  };

  // Раньше здесь показывался "сырой" err.message — это всегда было сообщение
  // с бэкенда (ApiError extends Error, поэтому `instanceof Error` истинно
  // почти для любой ошибки, включая сетевые), то есть не локализованный
  // фолбэк t("error") практически никогда не срабатывал, а не-русскоязычный
  // пользователь видел русский текст бэкенда. 429 (см. ErrorType.TooManyRequests
  // на бэкенде — AiAssistantService.AskAsync, GroqRateLimitedException) —
  // единственный случай, который стоит показывать отдельно от общего
  // "недоступен", т.к. у него есть понятная причина и временный характер.
  const resolveAssistantErrorText = (err: unknown): string => {
    if (err instanceof ApiError && err.status === 429) return t("rateLimited");
    return t("error");
  };

  const handleSend = async () => {
    const text = draft.trim();
    if (!text || sending) return;
    setDraft("");
    // История берётся ДО того, как в messages добавится текущий вопрос —
    // бэкенд сам добавляет message последним (см. AiAssistantService.AskAsync).
    const history = messages.map((m) => ({ role: m.role, text: m.text }));
    pushMessage({ role: "user", text });
    setSending(true);
    try {
      const res = await askAssistant(text, history);
      pushMessage({
        role: "assistant",
        text: res.message,
        intent: res.intent,
        productId: res.productId,
        categoryId: res.categoryId,
        action: res.action,
        actionStatus: res.action ? "idle" : undefined,
      });
    } catch (err) {
      pushMessage({ role: "assistant", text: resolveAssistantErrorText(err) });
    } finally {
      setSending(false);
    }
  };

  const handleConfirmAction = async (messageId: number, action: AssistantActionDto) => {
    setMessages((prev) => prev.map((m) => (m.id === messageId ? { ...m, actionStatus: "loading" } : m)));
    try {
      const resultMessage = await executeAssistantAction({ type: action.type, params: action.params });
      setMessages((prev) => prev.map((m) => (m.id === messageId ? { ...m, actionStatus: "done" } : m)));
      pushMessage({ role: "assistant", text: resultMessage });
    } catch (err) {
      setMessages((prev) => prev.map((m) => (m.id === messageId ? { ...m, actionStatus: "error" } : m)));
      pushMessage({ role: "assistant", text: resolveAssistantErrorText(err) });
    }
  };

  const handleCancelAction = (messageId: number) => {
    setMessages((prev) => prev.map((m) => (m.id === messageId ? { ...m, actionStatus: "cancelled" } : m)));
  };

  const goToProduct = (productId: number) => {
    const product = getCatalogProductById(productId);
    if (!product) return;
    setOpen(false);
    navigate(`/product/${product.slug}`);
  };

  const goToCategory = (categoryId: number) => {
    setOpen(false);
    navigate(`/catalog?category=${categoryId}`);
  };

  const goToCart = () => {
    setOpen(false);
    navigate("/checkout");
  };

  const goToOrders = () => {
    setOpen(false);
    navigate("/customer/orders");
  };

  return (
    <>
      <motion.button
        type="button"
        onClick={() => setOpen((v) => !v)}
        onMouseMove={handleButtonPointerMove}
        onMouseLeave={resetButtonTilt}
        aria-label={open ? t("close") : t("open")}
        initial={{ opacity: 0, scale: 0.8 }}
        animate={
          prefersReducedMotion
            ? { opacity: 1, scale: 1, y: 0 }
            : { opacity: 1, scale: 1, y: open ? 0 : [0, -5, 0] }
        }
        transition={
          prefersReducedMotion
            ? { duration: 0.2 }
            : { opacity: { duration: 0.2 }, scale: { duration: 0.2 }, y: { duration: 3.4, repeat: open ? 0 : Infinity, ease: "easeInOut" } }
        }
        style={
          prefersReducedMotion
            ? undefined
            : { rotateX: buttonRotateX, rotateY: buttonRotateY, transformPerspective: 500 }
        }
        whileHover={{ scale: 1.06, y: 0 }}
        whileTap={{ scale: 0.94 }}
        className={cn(
          "fixed right-5 bottom-9 z-40 flex size-16 items-center justify-center rounded-full",
          "border border-grove-500/40 bg-linear-to-br from-grove-600 via-grove-700 to-grove-900",
          "text-white shadow-(--shadow-lifted) shadow-grove-950/30 ring-1 ring-white/10",
          "transition-shadow duration-300 hover:shadow-(--shadow-glow-grove) sm:right-6 sm:bottom-10",
        )}
      >
        <AnimatePresence mode="wait" initial={false}>
          {open ? (
            <motion.span
              key="close-icon"
              initial={{ opacity: 0, rotate: -45, scale: 0.7 }}
              animate={{ opacity: 1, rotate: 0, scale: 1 }}
              exit={{ opacity: 0, rotate: 45, scale: 0.7 }}
              transition={{ duration: 0.18 }}
            >
              <X size={22} />
            </motion.span>
          ) : (
            <motion.span
              key="mascot-icon"
              initial={{ opacity: 0, scale: 0.7 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.7 }}
              transition={{ duration: 0.18 }}
              className="flex items-center justify-center"
            >
              <AssistantMascot size={36} />
            </motion.span>
          )}
        </AnimatePresence>
      </motion.button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={prefersReducedMotion ? { opacity: 0 } : { opacity: 0, y: 24, scale: 0.92, rotateX: -14 }}
            animate={prefersReducedMotion ? { opacity: 1 } : { opacity: 1, y: 0, scale: 1, rotateX: 0 }}
            exit={
              prefersReducedMotion
                ? { opacity: 0, transition: { duration: 0.12 } }
                : { opacity: 0, y: 8, scale: 0.95, rotateX: -8, transition: { duration: 0.18, ease: "easeIn" } }
            }
            transition={
              prefersReducedMotion
                ? { duration: 0.15 }
                : { type: "spring", stiffness: 340, damping: 28, mass: 0.9 }
            }
            style={{ transformPerspective: 1400, transformOrigin: "bottom right" }}
            className={cn(
              "fixed right-5 bottom-28 z-40 flex h-120 max-h-[70vh] w-88 max-w-[calc(100vw-2.5rem)] flex-col overflow-hidden",
              "rounded-[28px] border border-stone-200/70 bg-stone-25 shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-950",
              "sm:right-6",
            )}
          >
            <div className="relative flex shrink-0 items-center gap-3 overflow-hidden bg-linear-to-br from-grove-700 via-grove-800 to-grove-900 px-5 py-4">
              <div className="pointer-events-none absolute inset-0 bg-noise opacity-40" />
              <div className="relative flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl border border-white/15 bg-white/10 backdrop-blur-sm">
                <AssistantMascot size={30} />
              </div>
              <div className="relative min-w-0 flex-1">
                <p className="truncate font-display text-[15px] text-white">{t("title")}</p>
                <p className="truncate text-xs text-grove-100/80">{t(`subtitle${roleSuffix}`)}</p>
              </div>
            </div>

            <div ref={scrollRef} className="flex-1 space-y-3 overflow-y-auto bg-stone-25 px-4 py-4 dark:bg-stone-950">
              {messages.length === 0 && (
                <div className="flex h-full flex-col items-center justify-center gap-3 px-4 text-center">
                  <AssistantMascot size={44} />
                  <p className="text-sm text-stone-500 dark:text-stone-400">{t(`greeting${roleSuffix}`)}</p>
                </div>
              )}
              {messages.map((m) => (
                <motion.div
                  key={m.id}
                  initial={prefersReducedMotion ? undefined : { opacity: 0, y: 8 }}
                  animate={prefersReducedMotion ? undefined : { opacity: 1, y: 0 }}
                  transition={{ duration: 0.22, ease: "easeOut" }}
                  className={cn("flex", m.role === "user" ? "justify-end" : "justify-start")}
                >
                  <div className="max-w-[85%] space-y-2">
                    <div
                      className={cn(
                        "rounded-2xl px-4 py-2.5 text-sm shadow-sm",
                        m.role === "user"
                          ? "rounded-br-md bg-linear-to-br from-grove-600 to-grove-700 text-white"
                          : "rounded-bl-md border border-stone-100 bg-white text-stone-800 dark:border-stone-800 dark:bg-stone-900 dark:text-stone-100",
                      )}
                    >
                      <p className="whitespace-pre-wrap wrap-break-word">{m.text}</p>
                    </div>
                    {m.role === "assistant" && m.intent === "product" && m.productId != null && (
                      <ProductSuggestion productId={m.productId} onOpen={() => goToProduct(m.productId!)} />
                    )}
                    {m.role === "assistant" && m.intent === "category" && m.categoryId != null && (
                      <motion.button
                        whileHover={{ scale: 1.02 }}
                        whileTap={{ scale: 0.98 }}
                        onClick={() => goToCategory(m.categoryId!)}
                        className="rounded-xl border border-grove-200 bg-grove-50 px-3.5 py-2 text-xs font-medium text-grove-700 transition-colors hover:bg-grove-100 dark:border-grove-800 dark:bg-grove-900/30 dark:text-grove-400"
                      >
                        {t("viewCategory")}
                      </motion.button>
                    )}
                    {m.role === "assistant" && m.intent === "cart" && (
                      <motion.button
                        whileHover={{ scale: 1.02 }}
                        whileTap={{ scale: 0.98 }}
                        onClick={goToCart}
                        className="rounded-xl border border-grove-200 bg-grove-50 px-3.5 py-2 text-xs font-medium text-grove-700 transition-colors hover:bg-grove-100 dark:border-grove-800 dark:bg-grove-900/30 dark:text-grove-400"
                      >
                        {t("viewCart")}
                      </motion.button>
                    )}
                    {m.role === "assistant" && m.intent === "orders" && (
                      <motion.button
                        whileHover={{ scale: 1.02 }}
                        whileTap={{ scale: 0.98 }}
                        onClick={goToOrders}
                        className="rounded-xl border border-grove-200 bg-grove-50 px-3.5 py-2 text-xs font-medium text-grove-700 transition-colors hover:bg-grove-100 dark:border-grove-800 dark:bg-grove-900/30 dark:text-grove-400"
                      >
                        {t("viewOrders")}
                      </motion.button>
                    )}
                    {m.role === "assistant" && m.intent === "action_pending" && m.action && (
                      <div className="rounded-xl border border-harvest-200 bg-harvest-50 p-2.5 dark:border-harvest-900/40 dark:bg-harvest-950/20">
                        {(!m.actionStatus || m.actionStatus === "idle") && (
                          <div className="flex gap-2">
                            <motion.button
                              whileHover={{ scale: 1.03 }}
                              whileTap={{ scale: 0.97 }}
                              onClick={() => handleConfirmAction(m.id, m.action!)}
                              className="flex-1 rounded-lg bg-grove-700 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-grove-800"
                            >
                              {t("confirm")}
                            </motion.button>
                            <motion.button
                              whileHover={{ scale: 1.03 }}
                              whileTap={{ scale: 0.97 }}
                              onClick={() => handleCancelAction(m.id)}
                              className="flex-1 rounded-lg border border-stone-200 px-3 py-1.5 text-xs font-medium text-stone-600 transition-colors hover:bg-stone-100 dark:border-stone-700 dark:text-stone-300 dark:hover:bg-stone-800"
                            >
                              {t("cancel")}
                            </motion.button>
                          </div>
                        )}
                        {m.actionStatus === "loading" && (
                          <div className="flex items-center gap-2 text-xs text-stone-500 dark:text-stone-400">
                            <Loader2 size={12} className="animate-spin" />
                            {t("executing")}
                          </div>
                        )}
                        {m.actionStatus === "cancelled" && (
                          <p className="text-xs text-stone-400 dark:text-stone-500">{t("actionCancelled")}</p>
                        )}
                        {m.actionStatus === "done" && (
                          <p className="flex items-center gap-1 text-xs text-grove-700 dark:text-grove-400">
                            <Check size={12} /> {t("actionDone")}
                          </p>
                        )}
                        {m.actionStatus === "error" && <p className="text-xs text-danger">{t("error")}</p>}
                      </div>
                    )}
                  </div>
                </motion.div>
              ))}
              {sending && (
                <div className="flex justify-start">
                  <div className="flex items-center gap-2 rounded-2xl rounded-bl-md border border-stone-100 bg-white px-4 py-2.5 text-sm text-stone-400 shadow-sm dark:border-stone-800 dark:bg-stone-900 dark:text-stone-500">
                    <Loader2 size={14} className="animate-spin" />
                    {t("thinking")}
                  </div>
                </div>
              )}
            </div>

            <div className="flex shrink-0 items-end gap-2 border-t border-stone-100 bg-white p-3 dark:border-stone-800 dark:bg-stone-900">
              <textarea
                ref={textareaRef}
                value={draft}
                onChange={(e) => setDraft(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && !e.shiftKey) {
                    e.preventDefault();
                    handleSend();
                  }
                }}
                placeholder={t("placeholder")}
                rows={1}
                disabled={sending}
                className="max-h-24 min-h-10 flex-1 resize-none rounded-xl border border-stone-200 bg-stone-25 px-3.5 py-2.5 text-sm text-stone-800 outline-none placeholder:text-stone-400 focus:border-grove-400 focus:ring-2 focus:ring-grove-100 disabled:opacity-60 dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100 dark:placeholder:text-stone-500 dark:focus:ring-grove-900/40"
              />
              <motion.button
                whileHover={{ scale: 1.06 }}
                whileTap={{ scale: 0.94 }}
                onClick={handleSend}
                disabled={sending || !draft.trim()}
                aria-label={t("send")}
                className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-linear-to-br from-grove-600 to-grove-700 text-white shadow-sm transition-colors hover:from-grove-700 hover:to-grove-800 disabled:cursor-not-allowed disabled:opacity-40"
              >
                {sending ? <Loader2 size={16} className="animate-spin" /> : <Send size={16} />}
              </motion.button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </>
  );
}

function ProductSuggestion({ productId, onOpen }: { productId: number; onOpen: () => void }) {
  const { t } = useTranslation(["aiAssistant", "common"]);
  const product = getCatalogProductById(productId);
  if (!product) return null;

  return (
    <motion.button
      whileHover={{ scale: 1.015 }}
      whileTap={{ scale: 0.985 }}
      onClick={onOpen}
      className="flex w-full items-center gap-3 rounded-xl border border-stone-100 bg-white p-2.5 text-left shadow-sm transition-colors hover:border-grove-300 dark:border-stone-800 dark:bg-stone-900 dark:hover:border-grove-700"
    >
      {product.photoUrl && (
        <img
          src={resolveMediaUrl(product.photoUrl)}
          alt=""
          className="h-12 w-12 shrink-0 rounded-lg object-cover"
          loading="lazy"
        />
      )}
      <div className="min-w-0 flex-1">
        <p className="truncate text-xs font-medium text-stone-900 dark:text-stone-50">{product.title}</p>
        <p className="text-xs text-grove-700 dark:text-grove-400">
          {formatSomoni(product.retailPricePerKg)} {t("common:currencySomoni")}
        </p>
      </div>
      <span className="shrink-0 text-[11px] font-medium text-grove-700 dark:text-grove-400">{t("open")}</span>
    </motion.button>
  );
}
