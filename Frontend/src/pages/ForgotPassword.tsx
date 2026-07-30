import { useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { motion, AnimatePresence } from "framer-motion";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { ArrowLeft, Eye, EyeOff, KeyRound, Lock, Mail } from "lucide-react";
import { Input } from "@/components/ui/Field";
import { Button } from "@/components/ui/Button";
import { AuthPanel } from "@/components/layout/AuthPanel";
import { forgotPassword, resetPassword } from "@/context/AuthContext";
import { ApiError } from "@/lib/api";

type Step = "email" | "reset";

interface EmailForm {
  email: string;
}

interface ResetForm {
  code: string;
  newPassword: string;
  confirmPassword: string;
}

const RESEND_COOLDOWN_SECONDS = 60;

export function ForgotPassword() {
  const { t } = useTranslation(["pages", "common"]);
  const navigate = useNavigate();
  const [step, setStep] = useState<Step>("email");
  const [email, setEmail] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [resendCooldown, setResendCooldown] = useState(0);

  const emailForm = useForm<EmailForm>();
  const resetForm = useForm<ResetForm>();
  const newPassword = resetForm.watch("newPassword");

  const isSubmittingRef = useRef(false);

  const startCooldown = () => {
    setResendCooldown(RESEND_COOLDOWN_SECONDS);
    const timer = setInterval(() => {
      setResendCooldown((s) => {
        if (s <= 1) {
          clearInterval(timer);
          return 0;
        }
        return s - 1;
      });
    }, 1000);
  };

  const onSubmitEmail = async (values: EmailForm) => {
    if (isSubmittingRef.current) return;
    isSubmittingRef.current = true;
    try {
      await forgotPassword(values.email);
      setEmail(values.email);
      startCooldown();
      setStep("reset");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("pages:forgotPassword.sendErrorFallback"));
    } finally {
      isSubmittingRef.current = false;
    }
  };

  const handleResend = async () => {
    if (resendCooldown > 0) return;
    try {
      await forgotPassword(email);
      startCooldown();
      toast.success(t("pages:forgotPassword.codeResent"));
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("pages:forgotPassword.sendErrorFallback"));
    }
  };

  const onSubmitReset = async (values: ResetForm) => {
    if (isSubmittingRef.current) return;
    isSubmittingRef.current = true;
    try {
      await resetPassword(email, values.code, values.newPassword);
      toast.success(t("pages:forgotPassword.resetSuccess"));
      navigate("/login");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("pages:forgotPassword.resetErrorFallback"));
    } finally {
      isSubmittingRef.current = false;
    }
  };

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
      <div className="flex flex-col justify-center px-6 py-14 sm:px-12 lg:px-20">
        <Link to="/login" className="mb-10 flex w-fit items-center gap-1.5 text-sm text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
          <ArrowLeft size={14} />
          {t("pages:forgotPassword.backToLogin")}
        </Link>

        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="mx-auto w-full max-w-sm"
        >
          <h1 className="font-display text-3xl text-stone-900 dark:text-stone-50">{t("pages:forgotPassword.title")}</h1>
          <p className="mt-2 text-stone-500 dark:text-stone-400">
            {step === "email" ? t("pages:forgotPassword.emailStepSubtitle") : t("pages:forgotPassword.resetStepSubtitle", { email })}
          </p>

          <AnimatePresence mode="wait">
            {step === "email" && (
              <motion.form
                key="email"
                initial={{ opacity: 0, x: 12 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -12 }}
                transition={{ duration: 0.2 }}
                onSubmit={emailForm.handleSubmit(onSubmitEmail)}
                className="mt-8 flex flex-col gap-5"
              >
                <Input
                  label={t("pages:login.emailOrPhoneLabel")}
                  placeholder="you@example.com"
                  autoComplete="email"
                  leftIcon={<Mail size={16} />}
                  error={emailForm.formState.errors.email?.message}
                  {...emailForm.register("email", {
                    required: t("pages:register.emailRequired"),
                    pattern: { value: /^\S+@\S+\.\S+$/, message: t("pages:register.emailInvalid") },
                  })}
                />
                <Button type="submit" size="lg" loading={emailForm.formState.isSubmitting} className="mt-1">
                  {t("pages:forgotPassword.sendCodeButton")}
                </Button>
              </motion.form>
            )}

            {step === "reset" && (
              <motion.form
                key="reset"
                initial={{ opacity: 0, x: 12 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -12 }}
                transition={{ duration: 0.2 }}
                onSubmit={resetForm.handleSubmit(onSubmitReset)}
                className="mt-8 flex flex-col gap-5"
              >
                <Input
                  label={t("pages:register.codeLabel")}
                  placeholder={t("pages:register.codePlaceholder")}
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  maxLength={6}
                  leftIcon={<KeyRound size={16} />}
                  error={resetForm.formState.errors.code?.message}
                  {...resetForm.register("code", {
                    required: t("pages:register.codeInvalidLength"),
                    validate: (v) => v.replace(/\D/g, "").length === 6 || t("pages:register.codeInvalidLength"),
                  })}
                />
                <Input
                  label={t("pages:forgotPassword.newPasswordLabel")}
                  type={showPassword ? "text" : "password"}
                  placeholder={t("pages:register.passwordPlaceholder")}
                  autoComplete="new-password"
                  leftIcon={<Lock size={16} />}
                  error={resetForm.formState.errors.newPassword?.message}
                  rightSlot={
                    <button type="button" onClick={() => setShowPassword((s) => !s)} className="text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
                      {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                    </button>
                  }
                  {...resetForm.register("newPassword", {
                    required: t("pages:login.passwordRequired"),
                    minLength: { value: 6, message: t("pages:login.passwordMinLength") },
                  })}
                />
                <Input
                  label={t("pages:register.confirmPasswordLabel")}
                  type={showPassword ? "text" : "password"}
                  placeholder={t("pages:register.confirmPasswordPlaceholder")}
                  autoComplete="new-password"
                  leftIcon={<Lock size={16} />}
                  error={resetForm.formState.errors.confirmPassword?.message}
                  {...resetForm.register("confirmPassword", {
                    required: t("pages:register.confirmPasswordRequired"),
                    validate: (v) => v === newPassword || t("pages:register.passwordsMismatch"),
                  })}
                />

                <Button type="submit" size="lg" loading={resetForm.formState.isSubmitting} className="mt-1">
                  {t("pages:forgotPassword.resetButton")}
                </Button>

                <div className="flex items-center justify-between text-xs">
                  <button
                    type="button"
                    onClick={() => setStep("email")}
                    className="font-medium text-stone-500 hover:text-stone-700 dark:text-stone-400 dark:hover:text-stone-200"
                  >
                    {t("pages:register.changeEmail")}
                  </button>
                  <button
                    type="button"
                    disabled={resendCooldown > 0}
                    onClick={() => void handleResend()}
                    className="font-medium text-grove-700 hover:text-grove-800 disabled:cursor-not-allowed disabled:text-stone-400 dark:text-grove-400 dark:disabled:text-stone-600"
                  >
                    {resendCooldown > 0 ? t("pages:register.resendIn", { seconds: resendCooldown }) : t("pages:register.resendButton")}
                  </button>
                </div>
              </motion.form>
            )}
          </AnimatePresence>

          <p className="mt-8 text-center text-sm text-stone-500 dark:text-stone-400">
            {t("pages:login.noAccount")}{" "}
            <Link to="/register" className="font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400 dark:hover:text-grove-300">
              {t("pages:login.signUp")}
            </Link>
          </p>
        </motion.div>
      </div>

      <AuthPanel />
    </div>
  );
}
