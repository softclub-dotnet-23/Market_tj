import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { useLocation, useNavigate } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { Bot, Check, Loader2, Send, Sparkles, X } from "lucide-react";
import { type AssistantActionDto, type AssistantIntent, askAssistant, executeAssistantAction } from "@/data/aiAssistant";
import { getCatalogProductById } from "@/data/catalogStore";
import { useAuth } from "@/context/AuthContext";
import { resolveMediaUrl } from "@/lib/api";
import { cn, formatSomoni } from "@/lib/utils";

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
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<AssistantMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [sending, setSending] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

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

  const handleSend = async () => {
    const text = draft.trim();
    if (!text || sending) return;
    setDraft("");
    pushMessage({ role: "user", text });
    setSending(true);
    try {
      const res = await askAssistant(text);
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
      pushMessage({ role: "assistant", text: err instanceof Error ? err.message : t("error") });
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
      pushMessage({ role: "assistant", text: err instanceof Error ? err.message : t("error") });
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
        aria-label={open ? t("close") : t("open")}
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{ opacity: 1, scale: 1 }}
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.95 }}
        className="fixed bottom-6 right-6 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-grove-700 text-white shadow-(--shadow-lifted) transition hover:bg-grove-800"
      >
        {open ? <X size={22} /> : <Sparkles size={22} />}
      </motion.button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: 16, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 12, scale: 0.97 }}
            transition={{ duration: 0.2, ease: [0.25, 1, 0.5, 1] }}
            className="fixed bottom-24 right-6 z-40 flex h-120 max-h-[70vh] w-88 max-w-[calc(100vw-3rem)] flex-col overflow-hidden rounded-3xl border border-stone-100 bg-white shadow-(--shadow-lifted) dark:border-stone-800 dark:bg-stone-900"
          >
            <div className="flex items-center gap-3 border-b border-stone-100 bg-white px-5 py-4 dark:border-stone-800 dark:bg-stone-900">
              <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-grove-100 text-grove-700 dark:bg-grove-900/40 dark:text-grove-400">
                <Bot size={18} />
              </div>
              <div className="min-w-0 flex-1">
                <p className="truncate font-display text-sm text-stone-900 dark:text-stone-50">{t("title")}</p>
                <p className="truncate text-xs text-stone-400 dark:text-stone-500">{t(`subtitle${roleSuffix}`)}</p>
              </div>
            </div>

            <div ref={scrollRef} className="flex-1 space-y-3 overflow-y-auto bg-stone-25 px-4 py-4 dark:bg-stone-950/40">
              {messages.length === 0 && (
                <div className="flex h-full flex-col items-center justify-center gap-2 px-4 text-center text-stone-400 dark:text-stone-500">
                  <Sparkles size={24} />
                  <p className="text-sm">{t(`greeting${roleSuffix}`)}</p>
                </div>
              )}
              {messages.map((m) => (
                <div key={m.id} className={cn("flex", m.role === "user" ? "justify-end" : "justify-start")}>
                  <div className="max-w-[85%] space-y-2">
                    <div
                      className={cn(
                        "rounded-2xl px-4 py-2.5 text-sm shadow-sm",
                        m.role === "user"
                          ? "rounded-br-md bg-grove-700 text-white"
                          : "rounded-bl-md bg-white text-stone-800 dark:bg-stone-800 dark:text-stone-100",
                      )}
                    >
                      <p className="whitespace-pre-wrap wrap-break-word">{m.text}</p>
                    </div>
                    {m.role === "assistant" && m.intent === "product" && m.productId != null && (
                      <ProductSuggestion productId={m.productId} onOpen={() => goToProduct(m.productId!)} />
                    )}
                    {m.role === "assistant" && m.intent === "category" && m.categoryId != null && (
                      <button
                        onClick={() => goToCategory(m.categoryId!)}
                        className="rounded-xl border border-grove-200 bg-grove-50 px-3.5 py-2 text-xs font-medium text-grove-700 transition hover:bg-grove-100 dark:border-grove-800 dark:bg-grove-900/30 dark:text-grove-400"
                      >
                        {t("viewCategory")}
                      </button>
                    )}
                    {m.role === "assistant" && m.intent === "cart" && (
                      <button
                        onClick={goToCart}
                        className="rounded-xl border border-grove-200 bg-grove-50 px-3.5 py-2 text-xs font-medium text-grove-700 transition hover:bg-grove-100 dark:border-grove-800 dark:bg-grove-900/30 dark:text-grove-400"
                      >
                        {t("viewCart")}
                      </button>
                    )}
                    {m.role === "assistant" && m.intent === "orders" && (
                      <button
                        onClick={goToOrders}
                        className="rounded-xl border border-grove-200 bg-grove-50 px-3.5 py-2 text-xs font-medium text-grove-700 transition hover:bg-grove-100 dark:border-grove-800 dark:bg-grove-900/30 dark:text-grove-400"
                      >
                        {t("viewOrders")}
                      </button>
                    )}
                    {m.role === "assistant" && m.intent === "action_pending" && m.action && (
                      <div className="rounded-xl border border-amber-200 bg-amber-50 p-2.5 dark:border-amber-900/40 dark:bg-amber-950/20">
                        {(!m.actionStatus || m.actionStatus === "idle") && (
                          <div className="flex gap-2">
                            <button
                              onClick={() => handleConfirmAction(m.id, m.action!)}
                              className="flex-1 rounded-lg bg-grove-700 px-3 py-1.5 text-xs font-medium text-white transition hover:bg-grove-800"
                            >
                              {t("confirm")}
                            </button>
                            <button
                              onClick={() => handleCancelAction(m.id)}
                              className="flex-1 rounded-lg border border-stone-200 px-3 py-1.5 text-xs font-medium text-stone-600 transition hover:bg-stone-100 dark:border-stone-700 dark:text-stone-300 dark:hover:bg-stone-800"
                            >
                              {t("cancel")}
                            </button>
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
                </div>
              ))}
              {sending && (
                <div className="flex justify-start">
                  <div className="flex items-center gap-2 rounded-2xl rounded-bl-md bg-white px-4 py-2.5 text-sm text-stone-400 shadow-sm dark:bg-stone-800 dark:text-stone-500">
                    <Loader2 size={14} className="animate-spin" />
                    {t("thinking")}
                  </div>
                </div>
              )}
            </div>

            <div className="flex items-end gap-2 border-t border-stone-100 bg-white p-3 dark:border-stone-800 dark:bg-stone-900">
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
                className="max-h-24 min-h-10 flex-1 resize-none rounded-xl border border-stone-200 bg-stone-25 px-3.5 py-2.5 text-sm outline-none placeholder:text-stone-400 focus:border-grove-400 disabled:opacity-60 dark:border-stone-700 dark:bg-stone-950 dark:text-stone-100 dark:placeholder:text-stone-500"
              />
              <button
                onClick={handleSend}
                disabled={sending || !draft.trim()}
                aria-label={t("send")}
                className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-grove-700 text-white transition hover:bg-grove-800 disabled:cursor-not-allowed disabled:opacity-40"
              >
                {sending ? <Loader2 size={16} className="animate-spin" /> : <Send size={16} />}
              </button>
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
    <button
      onClick={onOpen}
      className="flex w-full items-center gap-3 rounded-xl border border-stone-100 bg-white p-2.5 text-left transition hover:border-grove-300 dark:border-stone-800 dark:bg-stone-900 dark:hover:border-grove-700"
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
    </button>
  );
}
