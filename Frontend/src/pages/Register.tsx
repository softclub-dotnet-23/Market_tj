import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { Controller, useForm } from "react-hook-form";
import { motion, AnimatePresence } from "framer-motion";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { ArrowLeft, Eye, EyeOff, IdCard, KeyRound, Lock, Mail, MapPin, Sprout, Truck, Upload, User, UserRound } from "lucide-react";
import { Input, Checkbox, Select } from "@/components/ui/Field";
import { Autocomplete } from "@/components/ui/Autocomplete";
import { PhoneInput } from "@/components/ui/PhoneInput";
import { Button } from "@/components/ui/Button";
import { AuthPanel } from "@/components/layout/AuthPanel";
import { useAuth, sendVerificationCode, verifyEmailCode } from "@/context/AuthContext";
import { createFarmerProfile } from "@/data/farmer";
import { createCustomerProfile } from "@/data/customer";
import { createCourierProfile, uploadCourierDocument, CourierDocumentType } from "@/data/courier";
import { TAJIKISTAN_REGION_SUGGESTIONS, getDistrictsForRegion } from "@/data/tajikistanGeo";
import { ApiError } from "@/lib/api";
import { cn } from "@/lib/utils";

type Role = "customer" | "farmer" | "courier";
type Step = "info" | "code" | "details";

// UserRole на бэкенде сериализуется числом (нет JsonStringEnumConverter) —
// Admin: 1, Farmer: 2, Customer: 3, Courier: 4 (см. data/adminEntities.ts).
const ROLE_VALUES: Record<Role, number> = { farmer: 2, customer: 3, courier: 4 };

// Значения хранятся как канонические русские строки независимо от локали
// интерфейса (та же схема, что qualityGrade в FarmerProducts.tsx) — метка
// переводится через t(), значение в БД — нет.
// По прямому запросу пользователя (2026-08-05): курьер может доставлять
// только на одном из этих 3 типов транспорта — пешком/мотоцикл/велосипед
// больше не допускаются (см. CourierProfileValidator на бэкенде — та же
// проверка продублирована там как настоящий гейт).
const TRANSPORT_TYPES = ["Автомобиль", "Портер", "КамАЗ"] as const;

const RESEND_COOLDOWN_SECONDS = 60;

// Обязательные документы курьера при регистрации — по прямому запросу
// пользователя (2026-08-04): без обоих фото курьер не сможет впоследствии
// стать "доступным" (см. CourierProfileService.UpdateAsync), но саму
// загрузку делаем обязательной уже на этапе регистрации, а не откладываем.
const ALLOWED_DOCUMENT_TYPES = ["image/jpeg", "image/png", "image/webp", "application/pdf"];
const MAX_DOCUMENT_SIZE_BYTES = 10 * 1024 * 1024;

interface RegisterForm {
  fullName: string;
  email: string;
  phone: string;
  region: string;
  district: string;
  farmName: string;
  village: string;
  address: string;
  transportType: string;
  vehicleNumber: string;
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
    { id: "courier", title: t("pages:register.roleCourierTitle"), icon: Truck },
  ];

  const [role, setRole] = useState<Role>("customer");
  const [showPassword, setShowPassword] = useState(false);
  const [step, setStep] = useState<Step>("info");
  const [sendingCode, setSendingCode] = useState(false);
  const [verifyingCode, setVerifyingCode] = useState(false);
  const [code, setCode] = useState("");
  const [codeError, setCodeError] = useState<string | null>(null);
  const [resendCooldown, setResendCooldown] = useState(0);
  const [licenseFile, setLicenseFile] = useState<File | null>(null);
  const [vehicleRegFile, setVehicleRegFile] = useState<File | null>(null);
  const [documentsError, setDocumentsError] = useState<string | null>(null);
  const licenseInputRef = useRef<HTMLInputElement>(null);
  const vehicleRegInputRef = useRef<HTMLInputElement>(null);

  const {
    register,
    handleSubmit,
    watch,
    trigger,
    getValues,
    control,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({ defaultValues: { phone: "", region: "", district: "" } });

  const password = watch("password");
  const region = watch("region");
  const districtOptions = getDistrictsForRegion(region ?? "");

  const isSubmittingRef = useRef(false);

  // Обратный отсчёт до повторной отправки кода — чисто визуальный (реальный
  // лимит проверяется на бэкенде), просто чтобы не казалось, что кнопка
  // "Отправить ещё раз" ничего не делает.
  useEffect(() => {
    if (resendCooldown <= 0) return;
    const timer = setInterval(() => setResendCooldown((s) => Math.max(0, s - 1)), 1000);
    return () => clearInterval(timer);
  }, [resendCooldown]);

  const requestCode = async () => {
    setSendingCode(true);
    try {
      await sendVerificationCode(getValues("email"));
      setResendCooldown(RESEND_COOLDOWN_SECONDS);
      return true;
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : t("pages:register.sendCodeErrorFallback"));
      return false;
    } finally {
      setSendingCode(false);
    }
  };

  const handleInfoNext = async () => {
    const valid = await trigger(["fullName", "email", "phone"]);
    if (!valid) return;
    if (await requestCode()) setStep("code");
  };

  const handleResend = async () => {
    if (resendCooldown > 0) return;
    await requestCode();
  };

  const handleVerifyCode = async () => {
    if (code.replace(/\D/g, "").length !== 6) {
      setCodeError(t("pages:register.codeInvalidLength"));
      return;
    }
    setVerifyingCode(true);
    setCodeError(null);
    try {
      await verifyEmailCode(getValues("email"), code);
      setStep("details");
    } catch (err) {
      setCodeError(err instanceof ApiError ? err.message : t("pages:register.verifyCodeErrorFallback"));
    } finally {
      setVerifyingCode(false);
    }
  };

  const onDocumentFileChange = (setter: (file: File | null) => void) => (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    if (!ALLOWED_DOCUMENT_TYPES.includes(file.type)) {
      toast.error(t("common:avatar.invalidType"));
      return;
    }
    if (file.size > MAX_DOCUMENT_SIZE_BYTES) {
      toast.error(t("pages:register.documentTooLarge"));
      return;
    }
    setDocumentsError(null);
    setter(file);
  };

  const onSubmit = async (values: RegisterForm) => {
    if (isSubmittingRef.current) return;
    if (role === "courier" && (!licenseFile || !vehicleRegFile)) {
      setDocumentsError(t("pages:register.documentsRequired"));
      return;
    }
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
      } else if (role === "courier") {
        const courierProfileId = await createCourierProfile({
          userId: user.userId,
          transportType: values.transportType,
          vehicleNumber: values.vehicleNumber,
          region: values.region,
          district: values.district,
          address: values.address,
        });
        // Документы — сразу после создания профиля, тем же токеном (уже
        // выдан на этом шаге). Оба обязательны — проверено выше, до
        // registerAccount, чтобы не создавать аккаунт впустую при их отсутствии.
        await uploadCourierDocument(courierProfileId, CourierDocumentType.DriverLicense, licenseFile!);
        await uploadCourierDocument(courierProfileId, CourierDocumentType.VehicleRegistration, vehicleRegFile!);
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
      window.location.href = role === "farmer" ? "/farmer" : role === "courier" ? "/courier" : "/customer";
    } catch (err) {
      const message = err instanceof ApiError ? err.message : t("pages:register.registerErrorFallback");
      toast.error(message, { id: "register-toast" });
    } finally {
      isSubmittingRef.current = false;
    }
  };

  const stepIndex = step === "info" ? 1 : step === "code" ? 2 : 3;

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
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
          <div className="flex items-center justify-between">
            <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("pages:register.title")}</h1>
            <span className="text-xs font-medium text-stone-400 dark:text-stone-500">
              {t("pages:register.stepIndicator", { step: stepIndex, total: 3 })}
            </span>
          </div>
          <p className="mt-0.5 text-xs text-stone-500 dark:text-stone-400">{t("pages:register.subtitle")}</p>

          {step === "info" && (
            <div className="mt-3 grid grid-cols-3 gap-1.5 rounded-xl bg-stone-100 p-1 dark:bg-stone-800">
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
          )}

          <AnimatePresence mode="wait">
            {step === "info" && (
              <motion.form
                key="info"
                initial={{ opacity: 0, x: 12 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -12 }}
                transition={{ duration: 0.2 }}
                onSubmit={(e) => {
                  e.preventDefault();
                  void handleInfoNext();
                }}
                className="mt-3 flex flex-col gap-3"
              >
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

                <Button type="submit" size="md" loading={sendingCode}>
                  {t("pages:register.nextButton")}
                </Button>
              </motion.form>
            )}

            {step === "code" && (
              <motion.div
                key="code"
                initial={{ opacity: 0, x: 12 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -12 }}
                transition={{ duration: 0.2 }}
                className="mt-3 flex flex-col gap-3"
              >
                <p className="text-sm text-stone-600 dark:text-stone-300">
                  {t("pages:register.codeStepSubtitle", { email: getValues("email") })}
                </p>
                <Input
                  label={t("pages:register.codeLabel")}
                  placeholder={t("pages:register.codePlaceholder")}
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  maxLength={6}
                  leftIcon={<KeyRound size={16} />}
                  error={codeError ?? undefined}
                  value={code}
                  onChange={(e) => {
                    setCode(e.target.value.replace(/\D/g, "").slice(0, 6));
                    setCodeError(null);
                  }}
                />
                <Button type="button" size="md" loading={verifyingCode} onClick={() => void handleVerifyCode()}>
                  {t("pages:register.verifyButton")}
                </Button>
                <div className="flex items-center justify-between text-xs">
                  <button
                    type="button"
                    onClick={() => setStep("info")}
                    className="font-medium text-stone-500 hover:text-stone-700 dark:text-stone-400 dark:hover:text-stone-200"
                  >
                    {t("pages:register.changeEmail")}
                  </button>
                  <button
                    type="button"
                    disabled={resendCooldown > 0 || sendingCode}
                    onClick={() => void handleResend()}
                    className="font-medium text-grove-700 hover:text-grove-800 disabled:cursor-not-allowed disabled:text-stone-400 dark:text-grove-400 dark:disabled:text-stone-600"
                  >
                    {resendCooldown > 0 ? t("pages:register.resendIn", { seconds: resendCooldown }) : t("pages:register.resendButton")}
                  </button>
                </div>
              </motion.div>
            )}

            {step === "details" && (
              <motion.form
                key="details"
                initial={{ opacity: 0, x: 12 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -12 }}
                transition={{ duration: 0.2 }}
                onSubmit={handleSubmit(onSubmit)}
                className="mt-3 flex flex-col gap-3"
              >
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

                {role === "courier" && (
                  <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                    <Controller
                      name="transportType"
                      control={control}
                      rules={{ required: t("pages:register.transportTypeRequired") }}
                      render={({ field, fieldState }) => (
                        <Select
                          label={t("pages:register.transportTypeLabel")}
                          error={fieldState.error?.message}
                          {...field}
                        >
                          <option value="" disabled>
                            {t("pages:register.transportTypePlaceholder")}
                          </option>
                          {TRANSPORT_TYPES.map((type) => (
                            <option key={type} value={type}>
                              {t(`pages:register.transportTypeOptions.${type}`)}
                            </option>
                          ))}
                        </Select>
                      )}
                    />
                    <Input
                      label={t("pages:register.vehicleNumberLabel")}
                      placeholder={t("pages:register.vehicleNumberPlaceholder")}
                      leftIcon={<Truck size={16} />}
                      error={errors.vehicleNumber?.message}
                      {...register("vehicleNumber", { required: t("pages:register.vehicleNumberRequired") })}
                    />
                    <Input
                      label={t("pages:register.addressLabel")}
                      placeholder={t("pages:register.addressPlaceholder")}
                      leftIcon={<MapPin size={16} />}
                      error={errors.address?.message}
                      {...register("address", { required: t("pages:register.addressRequired") })}
                    />
                  </div>
                )}

                {role === "courier" && (
                  <div className="flex flex-col gap-2">
                    <p className="text-xs font-medium text-stone-500 dark:text-stone-400">{t("pages:register.documentsLabel")}</p>
                    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                      <button
                        type="button"
                        onClick={() => licenseInputRef.current?.click()}
                        className={cn(
                          "flex items-center gap-2.5 rounded-xl border-2 border-dashed px-3 py-2.5 text-left text-xs transition",
                          licenseFile
                            ? "border-grove-300 bg-grove-50 text-grove-700 dark:border-grove-800 dark:bg-grove-950/40 dark:text-grove-300"
                            : "border-stone-200 text-stone-500 hover:border-stone-300 dark:border-stone-700 dark:text-stone-400",
                        )}
                      >
                        <IdCard size={16} className="shrink-0" />
                        <span className="min-w-0 flex-1 truncate">{licenseFile ? licenseFile.name : t("pages:register.driverLicenseLabel")}</span>
                        <Upload size={14} className="shrink-0" />
                      </button>
                      <button
                        type="button"
                        onClick={() => vehicleRegInputRef.current?.click()}
                        className={cn(
                          "flex items-center gap-2.5 rounded-xl border-2 border-dashed px-3 py-2.5 text-left text-xs transition",
                          vehicleRegFile
                            ? "border-grove-300 bg-grove-50 text-grove-700 dark:border-grove-800 dark:bg-grove-950/40 dark:text-grove-300"
                            : "border-stone-200 text-stone-500 hover:border-stone-300 dark:border-stone-700 dark:text-stone-400",
                        )}
                      >
                        <Truck size={16} className="shrink-0" />
                        <span className="min-w-0 flex-1 truncate">{vehicleRegFile ? vehicleRegFile.name : t("pages:register.vehicleRegistrationLabel")}</span>
                        <Upload size={14} className="shrink-0" />
                      </button>
                    </div>
                    <input
                      ref={licenseInputRef}
                      type="file"
                      accept="image/jpeg,image/png,image/webp,application/pdf"
                      className="hidden"
                      onChange={onDocumentFileChange(setLicenseFile)}
                    />
                    <input
                      ref={vehicleRegInputRef}
                      type="file"
                      accept="image/jpeg,image/png,image/webp,application/pdf"
                      className="hidden"
                      onChange={onDocumentFileChange(setVehicleRegFile)}
                    />
                    {documentsError && <p className="text-xs text-danger">{documentsError}</p>}
                    <p className="text-xs text-stone-400 dark:text-stone-500">{t("pages:register.documentsHint")}</p>
                  </div>
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
                      <Link to="/terms" className="font-medium text-grove-700 hover:underline">
                        {t("pages:register.agreeTerms")}
                      </Link>{" "}
                      {t("pages:register.agreeSuffix")}
                    </span>
                  }
                  {...register("agree", { required: true })}
                />

                <Button type="submit" size="md" loading={isSubmitting}>
                  {role === "farmer"
                    ? t("pages:register.submitFarmer")
                    : role === "courier"
                      ? t("pages:register.submitCourier")
                      : t("common:auth.register")}
                </Button>
              </motion.form>
            )}
          </AnimatePresence>

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
