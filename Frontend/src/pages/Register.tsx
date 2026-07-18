import { useState } from "react";
import { Link } from "react-router-dom";
import { useForm } from "react-hook-form";
import { motion } from "framer-motion";
import { toast } from "sonner";
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

const ROLES: { id: Role; title: string; description: string; icon: typeof UserRound }[] = [
  { id: "customer", title: "Я покупатель", description: "Хочу заказывать свежие продукты у фермеров", icon: UserRound },
  { id: "farmer", title: "Я фермер", description: "Хочу продавать свой урожай напрямую покупателям", icon: Sprout },
];

export function Register() {
  const [role, setRole] = useState<Role>("customer");
  const [showPassword, setShowPassword] = useState(false);
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>();

  const password = watch("password");

  const onSubmit = async () => {
    await new Promise((r) => setTimeout(r, 800));
    toast.info("Регистрация появится после подключения к серверу", {
      description:
        role === "farmer"
          ? "После подключения бэкенда вы сможете заполнить профиль хозяйства и отправить его на проверку."
          : "Сейчас интерфейс работает на демонстрационных данных.",
    });
  };

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
      <div className="flex flex-col justify-center px-6 py-14 sm:px-12 lg:px-20">
        <Link to="/" className="mb-8 flex w-fit items-center gap-1.5 text-sm text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
          <ArrowLeft size={14} />
          На главную
        </Link>

        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="mx-auto w-full max-w-md"
        >
          <h1 className="font-display text-3xl text-stone-900 dark:text-stone-50">Создать аккаунт</h1>
          <p className="mt-2 text-stone-500 dark:text-stone-400">Присоединяйтесь к Market.tj за пару минут</p>

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
              label="Полное имя"
              placeholder="Ваше имя и фамилия"
              leftIcon={<User size={16} />}
              error={errors.fullName?.message}
              {...register("fullName", { required: "Укажите имя", minLength: { value: 3, message: "Минимум 3 символа" } })}
            />
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
              <Input
                label="Email"
                type="email"
                placeholder="you@example.com"
                leftIcon={<Mail size={16} />}
                error={errors.email?.message}
                {...register("email", {
                  required: "Укажите email",
                  pattern: { value: /^\S+@\S+\.\S+$/, message: "Некорректный email" },
                })}
              />
              <Input
                label="Телефон"
                type="tel"
                placeholder="+992 __ ___ ____"
                leftIcon={<Phone size={16} />}
                error={errors.phone?.message}
                {...register("phone", { required: "Укажите телефон" })}
              />
            </div>
            <Input
              label="Пароль"
              type={showPassword ? "text" : "password"}
              placeholder="Минимум 6 символов"
              leftIcon={<Lock size={16} />}
              error={errors.password?.message}
              rightSlot={
                <button type="button" onClick={() => setShowPassword((s) => !s)} className="text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
                  {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              }
              {...register("password", { required: "Введите пароль", minLength: { value: 6, message: "Минимум 6 символов" } })}
            />
            <Input
              label="Подтвердите пароль"
              type={showPassword ? "text" : "password"}
              placeholder="Повторите пароль"
              leftIcon={<Lock size={16} />}
              error={errors.confirmPassword?.message}
              {...register("confirmPassword", {
                required: "Повторите пароль",
                validate: (v) => v === password || "Пароли не совпадают",
              })}
            />

            <Checkbox
              label={
                <span>
                  Согласен с{" "}
                  <Link to="/forbidden" className="font-medium text-grove-700 hover:underline">
                    условиями использования
                  </Link>{" "}
                  и политикой конфиденциальности
                </span>
              }
              {...register("agree", { required: true })}
            />

            <Button type="submit" size="lg" loading={isSubmitting} className="mt-1">
              {role === "farmer" ? "Зарегистрироваться как фермер" : "Зарегистрироваться"}
            </Button>
          </form>

          <p className="mt-8 text-center text-sm text-stone-500 dark:text-stone-400">
            Уже есть аккаунт?{" "}
            <Link to="/login" className="font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400 dark:hover:text-grove-300">
              Войти
            </Link>
          </p>
        </motion.div>
      </div>

      <AuthPanel />
    </div>
  );
}
