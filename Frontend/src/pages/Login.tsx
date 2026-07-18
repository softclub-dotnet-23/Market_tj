import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { ArrowLeft, Eye, EyeOff, Lock, Mail } from "lucide-react";
import { Input, Checkbox } from "@/components/ui/Field";
import { Button } from "@/components/ui/Button";
import { AuthPanel } from "@/components/layout/AuthPanel";

interface LoginForm {
  email: string;
  password: string;
  remember: boolean;
}

export function Login() {
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>();

  const onSubmit = async () => {
    await new Promise((r) => setTimeout(r, 700));
    toast.info("Вход появится после подключения к серверу", {
      description: "Сейчас интерфейс работает на демонстрационных данных.",
    });
  };

  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
      <div className="flex flex-col justify-center px-6 py-14 sm:px-12 lg:px-20">
        <Link to="/" className="mb-10 flex w-fit items-center gap-1.5 text-sm text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
          <ArrowLeft size={14} />
          На главную
        </Link>

        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="mx-auto w-full max-w-sm"
        >
          <h1 className="font-display text-3xl text-stone-900 dark:text-stone-50">С возвращением</h1>
          <p className="mt-2 text-stone-500 dark:text-stone-400">Войдите, чтобы продолжить покупки на Market.tj</p>

          <form onSubmit={handleSubmit(onSubmit)} className="mt-8 flex flex-col gap-5">
            <Input
              label="Email или телефон"
              placeholder="you@example.com"
              leftIcon={<Mail size={16} />}
              error={errors.email?.message}
              {...register("email", { required: "Укажите email или телефон" })}
            />
            <Input
              label="Пароль"
              type={showPassword ? "text" : "password"}
              placeholder="••••••••"
              leftIcon={<Lock size={16} />}
              error={errors.password?.message}
              rightSlot={
                <button type="button" onClick={() => setShowPassword((s) => !s)} className="text-stone-400 hover:text-stone-600 dark:text-stone-500 dark:hover:text-stone-300">
                  {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              }
              {...register("password", { required: "Введите пароль", minLength: { value: 6, message: "Минимум 6 символов" } })}
            />

            <div className="flex items-center justify-between">
              <Checkbox label="Запомнить меня" {...register("remember")} />
              <button type="button" onClick={() => toast("Восстановление пароля скоро появится")} className="text-sm font-medium text-grove-700 hover:text-grove-800 dark:text-grove-400 dark:hover:text-grove-300">
                Забыли пароль?
              </button>
            </div>

            <Button type="submit" size="lg" loading={isSubmitting} className="mt-1">
              Войти
            </Button>
          </form>

          <p className="mt-8 text-center text-sm text-stone-500 dark:text-stone-400">
            Ещё нет аккаунта?{" "}
            <Link to="/register" className="font-semibold text-grove-700 hover:text-grove-800 dark:text-grove-400 dark:hover:text-grove-300">
              Зарегистрироваться
            </Link>
          </p>

          <div className="mt-8 flex items-center justify-center gap-3 text-xs text-stone-400 dark:text-stone-500">
            <button onClick={() => navigate("/forbidden")} className="hover:text-stone-600 dark:hover:text-stone-300">
              Условия использования
            </button>
            <span>·</span>
            <button onClick={() => navigate("/forbidden")} className="hover:text-stone-600 dark:hover:text-stone-300">
              Конфиденциальность
            </button>
          </div>
        </motion.div>
      </div>

      <AuthPanel />
    </div>
  );
}
