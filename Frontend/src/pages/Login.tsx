import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { ArrowLeft, Eye, EyeOff, Lock, Mail } from "lucide-react";
import { Input, Checkbox } from "@/components/ui/Field";
import { Button } from "@/components/ui/Button";
import { AuthPanel } from "@/components/layout/AuthPanel";
import { useAuth } from "@/context/AuthContext";
import { ApiError } from "@/lib/api";

interface LoginForm {
  email: string;
  password: string;
  remember: boolean;
}

export function Login() {
  const { t } = useTranslation(["pages", "common", "layout"]);
  const navigate = useNavigate();
  const { login } = useAuth();
  const [showPassword, setShowPassword] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>();

  const onSubmit = async (data: LoginForm) => {
    try {
      const user = await login(data.email, data.password);
      toast.success(`Добро пожаловать, ${user.fullName}`);

      if (user.role === "Admin") {
        navigate("/admin");
      } else {
        toast.info("Личный кабинет для этой роли ещё в разработке");
        navigate("/");
      }
    } catch (err) {
      const message = err instanceof ApiError ? err.message : "Не удалось подключиться к серверу";
      toast.error(message);
    }
  };

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
      <div className="flex flex-col justify-center px-6 py-14 sm:px-12 lg:px-20">
        <Link to="/" className="mb-10 flex w-fit items-center gap-1.5 text-sm text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
          <ArrowLeft size={14} />
          {t("pages:goHome")}
        </Link>

        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="mx-auto w-full max-w-sm"
        >
          <h1 className="font-display text-3xl text-stone-900 dark:text-stone-50">{t("pages:login.title")}</h1>
          <p className="mt-2 text-stone-500 dark:text-stone-400">{t("pages:login.subtitle")}</p>

          <form onSubmit={handleSubmit(onSubmit)} className="mt-8 flex flex-col gap-5">
            <Input
              label={t("pages:login.emailOrPhoneLabel")}
              placeholder="you@example.com"
              leftIcon={<Mail size={16} />}
              error={errors.email?.message}
              {...register("email", { required: t("pages:login.emailOrPhoneRequired") })}
            />
            <Input
              label={t("pages:login.passwordLabel")}
              type={showPassword ? "text" : "password"}
              placeholder="••••••••"
              leftIcon={<Lock size={16} />}
              error={errors.password?.message}
              rightSlot={
                <button type="button" onClick={() => setShowPassword((s) => !s)} className="text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
                  {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              }
              {...register("password", {
                required: t("pages:login.passwordRequired"),
                minLength: { value: 6, message: t("pages:login.passwordMinLength") },
              })}
            />

            <div className="flex items-center justify-between">
              <Checkbox label={t("pages:login.rememberMe")} {...register("remember")} />
              <button type="button" onClick={() => toast(t("pages:login.forgotPasswordToast"))} className="text-sm font-medium text-grove-700 hover:text-grove-800 dark:text-grove-400 dark:hover:text-grove-300">
                {t("pages:login.forgotPassword")}
              </button>
            </div>

            <Button type="submit" size="lg" loading={isSubmitting} className="mt-1">
              {t("common:auth.login")}
            </Button>
          </form>

          <p className="mt-8 text-center text-sm text-stone-500 dark:text-stone-400">
            {t("pages:login.noAccount")}{" "}
            <Link to="/register" className="font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400 dark:hover:text-grove-300">
              {t("pages:login.signUp")}
            </Link>
          </p>

          <div className="mt-8 flex items-center justify-center gap-3 text-xs text-stone-400 dark:text-stone-500">
            <button onClick={() => navigate("/forbidden")} className="hover:text-stone-600 dark:hover:text-stone-300">
              {t("layout:footer.terms")}
            </button>
            <span>·</span>
            <button onClick={() => navigate("/forbidden")} className="hover:text-stone-600 dark:hover:text-stone-300">
              {t("pages:login.privacy")}
            </button>
          </div>
        </motion.div>
      </div>

      <AuthPanel />
    </div>
  );
}
