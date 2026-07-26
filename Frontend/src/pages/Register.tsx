import { useRef, useState } from "react";
import { Link } from "react-router-dom";
import { Controller, useForm } from "react-hook-form";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { ArrowLeft, Eye, EyeOff, Lock, Mail, MapPin, Sprout, User, UserRound } from "lucide-react";
import { Input, Checkbox } from "@/components/ui/Field";
import { Autocomplete } from "@/components/ui/Autocomplete";
import { PhoneInput } from "@/components/ui/PhoneInput";
import { Button } from "@/components/ui/Button";
import { AuthPanel } from "@/components/layout/AuthPanel";
import { useAuth } from "@/context/AuthContext";
import { createFarmerProfile } from "@/data/farmer";
import { createCustomerProfile } from "@/data/customer";
import { TAJIKISTAN_REGION_SUGGESTIONS, getDistrictsForRegion } from "@/data/tajikistanGeo";
import { ApiError } from "@/lib/api";
import { cn } from "@/lib/utils";

type Role = "customer" | "farmer";

// UserRole на бэкенде сериализуется числом (нет JsonStringEnumConverter) —
// Admin: 1, Farmer: 2, Customer: 3, Courier: 4 (см. data/adminEntities.ts).
const ROLE_VALUES: Record<Role, number> = { farmer: 2, customer: 3 };

interface RegisterForm {
  fullName: string;
  email: string;
  phone: string;
  region: string;
  district: string;
  farmName: string;
  village: string;
  address: string;
  password: string;
  confirmPassword: string;
  agree: boolean;
}

export function Register() {
  const { t } = useTranslation(["pages", "common"]);
  const { register: registerAccount } = useAuth();
  const ROLES: { id: Role; title: string; icon: typeof UserRound }[] = [
    { id: "customer", title: t("pages:register.roleCustomerTitle"), icon: UserRound },
    { id: "farmer", title: t("pages:register.roleFarmerTitle"), icon: Sprout },
  ];

  const [role, setRole] = useState<Role>("customer");
  const [showPassword, setShowPassword] = useState(false);
  const {
    register,
    handleSubmit,
    watch,
    control,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({ defaultValues: { phone: "", region: "", district: "" } });

  const password = watch("password");
  const region = watch("region");
  const districtOptions = getDistrictsForRegion(region ?? "");

  const isSubmittingRef = useRef(false);

  const onSubmit = async (values: RegisterForm) => {
    if (isSubmittingRef.current) return;
    isSubmittingRef.current = true;
    try {
      const user = await registerAccount({
        fullName: values.fullName,
        email: values.email,
        phoneNumber: values.phone,
        password: values.password,
        role: ROLE_VALUES[role],
      });

      if (role === "farmer") {
        await createFarmerProfile({
          userId: user.userId,
          farmName: values.farmName,
          region: values.region,
          district: values.district,
          village: values.village,
          address: values.address,
          description: null,
        });
      } else {
        await createCustomerProfile({
          userId: user.userId,
          region: values.region,
          district: values.district,
          defaultAddress: null,
        });
      }

      // Настоящая перезагрузка страницы — та же причина, что и в Login.tsx:
      // браузер надёжно распознаёт "форма отправлена → успех" и предлагает
      // сохранить пароль только при реальной навигации, не SPA-переходе.
      window.location.href = role === "farmer" ? "/farmer" : "/customer";
    } catch (err) {
      const message = err instanceof ApiError ? err.message : t("pages:register.registerErrorFallback");
      toast.error(message, { id: "register-toast" });
    } finally {
      isSubmittingRef.current = false;
    }
  };

  return (
    <div className="grid h-screen grid-cols-1 overflow-hidden lg:grid-cols-2">
      <div className="flex flex-col justify-center px-6 py-4 sm:px-10 lg:px-14">
        <Link to="/" className="mb-2 flex w-fit items-center gap-1.5 text-xs text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
          <ArrowLeft size={13} />
          {t("pages:goHome")}
        </Link>

        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="mx-auto w-full max-w-md"
        >
          <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("pages:register.title")}</h1>
          <p className="mt-0.5 text-xs text-stone-500 dark:text-stone-400">{t("pages:register.subtitle")}</p>

          <div className="mt-3 grid grid-cols-2 gap-1.5 rounded-xl bg-stone-100 p-1 dark:bg-stone-800">
            {ROLES.map((r) => (
              <button
                key={r.id}
                type="button"
                onClick={() => setRole(r.id)}
                className={cn(
                  "flex items-center justify-center gap-1.5 rounded-lg px-3 py-2 text-xs font-semibold transition",
                  role === r.id
                    ? "bg-white text-grove-700 shadow-sm dark:bg-stone-900 dark:text-grove-400"
                    : "text-stone-500 hover:text-stone-700 dark:text-stone-400 dark:hover:text-stone-200",
                )}
              >
                <r.icon size={15} />
                {r.title}
              </button>
            ))}
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="mt-3 flex flex-col gap-3">
            <Input
              label={t("pages:register.fullNameLabel")}
              placeholder={t("pages:register.fullNamePlaceholder")}
              autoComplete="name"
              leftIcon={<User size={16} />}
              error={errors.fullName?.message}
              {...register("fullName", {
                required: t("pages:register.fullNameRequired"),
                minLength: { value: 3, message: t("pages:register.fullNameMinLength") },
              })}
            />
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Input
                label={t("pages:register.emailLabel")}
                type="email"
                placeholder="you@example.com"
                autoComplete="email"
                leftIcon={<Mail size={16} />}
                error={errors.email?.message}
                {...register("email", {
                  required: t("pages:register.emailRequired"),
                  pattern: { value: /^\S+@\S+\.\S+$/, message: t("pages:register.emailInvalid") },
                })}
              />
              <Controller
                name="phone"
                control={control}
                rules={{
                  required: t("pages:register.phoneRequired"),
                  validate: (v) => v.replace(/\D/g, "").length >= 12 || t("pages:register.phoneRequired"),
                }}
                render={({ field }) => (
                  <PhoneInput label={t("pages:register.phoneLabel")} error={errors.phone?.message} value={field.value} onChange={field.onChange} />
                )}
              />
            </div>

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Controller
                name="region"
                control={control}
                rules={{ required: t("pages:register.regionRequired") }}
                render={({ field }) => (
                  <Autocomplete
                    label={t("pages:register.regionLabel")}
                    placeholder={t("pages:register.regionPlaceholder")}
                    leftIcon={<MapPin size={16} />}
                    error={errors.region?.message}
                    value={field.value}
                    onChange={field.onChange}
                    options={TAJIKISTAN_REGION_SUGGESTIONS}
                  />
                )}
              />
              <Controller
                name="district"
                control={control}
                rules={{ required: t("pages:register.districtRequired") }}
                render={({ field }) => (
                  <Autocomplete
                    label={t("pages:register.districtLabel")}
                    placeholder={t("pages:register.districtPlaceholder")}
                    leftIcon={<MapPin size={16} />}
                    error={errors.district?.message}
                    value={field.value}
                    onChange={field.onChange}
                    options={districtOptions}
                  />
                )}
              />
            </div>

            {role === "farmer" && (
              <>
                <Input
                  label={t("pages:register.farmNameLabel")}
                  placeholder={t("pages:register.farmNamePlaceholder")}
                  leftIcon={<Sprout size={16} />}
                  error={errors.farmName?.message}
                  {...register("farmName", { required: t("pages:register.farmNameRequired") })}
                />
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <Input
                    label={t("pages:register.villageLabel")}
                    placeholder={t("pages:register.villagePlaceholder")}
                    leftIcon={<MapPin size={16} />}
                    error={errors.village?.message}
                    {...register("village", { required: t("pages:register.villageRequired") })}
                  />
                  <Input
                    label={t("pages:register.addressLabel")}
                    placeholder={t("pages:register.addressPlaceholder")}
                    leftIcon={<MapPin size={16} />}
                    error={errors.address?.message}
                    {...register("address", { required: t("pages:register.addressRequired") })}
                  />
                </div>
              </>
            )}

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Input
                label={t("pages:login.passwordLabel")}
                type={showPassword ? "text" : "password"}
                placeholder={t("pages:register.passwordPlaceholder")}
                autoComplete="new-password"
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
                autoComplete="new-password"
                leftIcon={<Lock size={16} />}
                error={errors.confirmPassword?.message}
                {...register("confirmPassword", {
                  required: t("pages:register.confirmPasswordRequired"),
                  validate: (v) => v === password || t("pages:register.passwordsMismatch"),
                })}
              />
            </div>

            <Checkbox
              label={
                <span className="text-xs">
                  {t("pages:register.agreePrefix")}{" "}
                  <Link to="/forbidden" className="font-medium text-grove-700 hover:underline">
                    {t("pages:register.agreeTerms")}
                  </Link>{" "}
                  {t("pages:register.agreeSuffix")}
                </span>
              }
              {...register("agree", { required: true })}
            />

            <Button type="submit" size="md" loading={isSubmitting}>
              {role === "farmer" ? t("pages:register.submitFarmer") : t("common:auth.register")}
            </Button>
          </form>

          <p className="mt-2 text-center text-xs text-stone-500 dark:text-stone-400">
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
