import { useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useForm } from "react-hook-form";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { ArrowLeft, Eye, EyeOff, Lock, Mail, Phone, Sprout, User, UserRound } from "lucide-react";
import { Input, Checkbox } from "@/components/ui/Field";
import { Button } from "@/components/ui/Button";
import { AuthPanel } from "@/components/layout/AuthPanel";
import { cn } from "@/lib/utils";

type Role = "customer" | "farmer";

interface RegisterForm {
  fullName: string;
  email: string;
  phone: string;
  password: string;
  confirmPassword: string;
  agree: boolean;
}

export function Register() {
  const { t } = useTranslation(["pages", "common"]);
  const ROLES: { id: Role; title: string; description: string; icon: typeof UserRound }[] = [
    { id: "customer", title: t("pages:register.roleCustomerTitle"), description: t("pages:register.roleCustomerDescription"), icon: UserRound },
    { id: "farmer", title: t("pages:register.roleFarmerTitle"), description: t("pages:register.roleFarmerDescription"), icon: Sprout },
  ];

  const [role, setRole] = useState<Role>("customer");
  const [showPassword, setShowPassword] = useState(false);
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>();

  const password = watch("password");

  const isSubmittingRef = useRef(false);

  const onSubmit = async () => {
    if (isSubmittingRef.current) return;
    isSubmittingRef.current = true;
    try {
      await new Promise((r) => setTimeout(r, 800));
      toast.info(t("pages:register.toastTitle"), {
        id: "register-toast",
        description:
          role === "farmer"
            ? t("pages:register.toastDescriptionFarmer")
            : t("pages:register.toastDescriptionCustomer"),
      });
    } finally {
      isSubmittingRef.current = false;
    }
  };

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
      <div className="flex flex-col justify-center px-6 py-14 sm:px-12 lg:px-20">
        <Link to="/" className="mb-8 flex w-fit items-center gap-1.5 text-sm text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
          <ArrowLeft size={14} />
          {t("pages:goHome")}
        </Link>

        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="mx-auto w-full max-w-md"
        >
          <h1 className="font-display text-3xl text-stone-900 dark:text-stone-50">{t("pages:register.title")}</h1>
          <p className="mt-2 text-stone-500 dark:text-stone-400">{t("pages:register.subtitle")}</p>

          <div className="mt-7 grid grid-cols-2 gap-3">
            {ROLES.map((r) => (
              <button
                key={r.id}
                type="button"
                onClick={() => setRole(r.id)}
                className={cn(
                  "flex flex-col items-start gap-2 rounded-2xl border-2 p-4 text-left transition",
                  role === r.id ? "border-grove-600 bg-grove-50 dark:border-grove-500 dark:bg-grove-950" : "border-stone-200 bg-white hover:border-stone-300 dark:border-stone-700 dark:bg-stone-900 dark:hover:border-stone-600",
                )}
              >
                <span
                  className={cn(
                    "flex h-9 w-9 items-center justify-center rounded-xl",
                    role === r.id ? "bg-grove-700 text-white" : "bg-stone-100 text-stone-500 dark:bg-stone-800 dark:text-stone-400",
                  )}
                >
                  <r.icon size={17} />
                </span>
                <span className="text-sm font-semibold text-stone-800 dark:text-stone-100">{r.title}</span>
                <span className="text-xs text-stone-500 dark:text-stone-400">{r.description}</span>
              </button>
            ))}
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
            <Input
              label={t("pages:register.fullNameLabel")}
              placeholder={t("pages:register.fullNamePlaceholder")}
              leftIcon={<User size={16} />}
              error={errors.fullName?.message}
              {...register("fullName", {
                required: t("pages:register.fullNameRequired"),
                minLength: { value: 3, message: t("pages:register.fullNameMinLength") },
              })}
            />
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
              <Input
                label={t("pages:register.emailLabel")}
                type="email"
                placeholder="you@example.com"
                leftIcon={<Mail size={16} />}
                error={errors.email?.message}
                {...register("email", {
                  required: t("pages:register.emailRequired"),
                  pattern: { value: /^\S+@\S+\.\S+$/, message: t("pages:register.emailInvalid") },
                })}
              />
              <Input
                label={t("pages:register.phoneLabel")}
                type="tel"
                placeholder="+992 __ ___ ____"
                leftIcon={<Phone size={16} />}
                error={errors.phone?.message}
                {...register("phone", { required: t("pages:register.phoneRequired") })}
              />
            </div>
            <Input
              label={t("pages:login.passwordLabel")}
              type={showPassword ? "text" : "password"}
              placeholder={t("pages:register.passwordPlaceholder")}
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
            <Input
              label={t("pages:register.confirmPasswordLabel")}
              type={showPassword ? "text" : "password"}
              placeholder={t("pages:register.confirmPasswordPlaceholder")}
              leftIcon={<Lock size={16} />}
              error={errors.confirmPassword?.message}
              {...register("confirmPassword", {
                required: t("pages:register.confirmPasswordRequired"),
                validate: (v) => v === password || t("pages:register.passwordsMismatch"),
              })}
            />

            <Checkbox
              label={
                <span>
                  {t("pages:register.agreePrefix")}{" "}
                  <Link to="/forbidden" className="font-medium text-grove-700 hover:underline">
                    {t("pages:register.agreeTerms")}
                  </Link>{" "}
                  {t("pages:register.agreeSuffix")}
                </span>
              }
              {...register("agree", { required: true })}
            />

            <Button type="submit" size="lg" loading={isSubmitting} className="mt-1">
              {role === "farmer" ? t("pages:register.submitFarmer") : t("common:auth.register")}
            </Button>
          </form>

          <p className="mt-8 text-center text-sm text-stone-500 dark:text-stone-400">
            {t("pages:register.hasAccount")}{" "}
            <Link to="/login" className="font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400 dark:hover:text-grove-300">
              {t("common:auth.login")}
            </Link>
          </p>
        </motion.div>
      </div>

      <AuthPanel />
    </div>
  );
}
