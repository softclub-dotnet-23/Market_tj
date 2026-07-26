import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Controller, useForm } from "react-hook-form";
import { motion } from "framer-motion";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { CheckCircle2, Info, LogIn, MapPin, Minus, Plus, ShoppingBag, Sprout, Trash2, User } from "lucide-react";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";
import { Input, Textarea } from "@/components/ui/Field";
import { Autocomplete } from "@/components/ui/Autocomplete";
import { PhoneInput } from "@/components/ui/PhoneInput";
import { Button } from "@/components/ui/Button";
import { PhotoTile } from "@/components/ui/PhotoTile";
import { PageLoader } from "@/components/layout/PageLoader";
import { useCart } from "@/context/CartContext";
import { useAuth } from "@/context/AuthContext";
import { useProducts } from "@/data/products";
import { useFarmers } from "@/data/farmers";
import { useCustomerProfile, submitCustomerOrder } from "@/data/customer";
import { TAJIKISTAN_REGION_SUGGESTIONS, getDistrictsForRegion } from "@/data/tajikistanGeo";
import { ApiError } from "@/lib/api";
import { formatSomoni, getUnitPrice } from "@/lib/utils";

interface CheckoutForm {
  fullName: string;
  phone: string;
  region: string;
  district: string;
  address: string;
  comment: string;
}

interface PlacedOrder {
  orderNumbers: string[];
  total: number;
  phone: string;
  address: string;
}

export function Checkout() {
  const { t } = useTranslation(["pages", "common", "layout"]);
  const navigate = useNavigate();
  const products = useProducts();
  const farmers = useFarmers();
  const { lines, totalPrice, removeItem, setQuantity, clearCart } = useCart();
  const { user } = useAuth();
  const { profile, loading: profileLoading } = useCustomerProfile();
  const [placedOrder, setPlacedOrder] = useState<PlacedOrder | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    control,
    formState: { errors, isSubmitting },
  } = useForm<CheckoutForm>({
    defaultValues: { fullName: user?.fullName ?? "", region: "", district: "", address: "", phone: "", comment: "" },
  });

  const region = watch("region");
  const districtOptions = getDistrictsForRegion(region ?? "");

  // Профиль покупателя (region/district/defaultAddress) подгружается отдельным
  // запросом после первого рендера — заполняем форму, как только он придёт.
  useEffect(() => {
    if (!profile) return;
    reset((prev) => ({
      ...prev,
      region: prev.region || profile.region,
      district: prev.district || profile.district,
      address: prev.address || profile.defaultAddress || "",
    }));
  }, [profile, reset]);

  // Каталог теперь на реальных данных (см. data/catalogStore.ts) — можно по-настоящему
  // создавать Order+OrderItem. У Order один FarmerId, поэтому строки корзины
  // группируются по фермеру: если товары от нескольких фермеров, уходит
  // несколько заказов (по одному на каждого).
  const onSubmit = async (values: CheckoutForm) => {
    if (!profile) return;

    try {
      const groups = new Map<number, typeof lines>();
      for (const line of lines) {
        const product = products.find((p) => p.id === line.productId);
        if (!product) continue;
        const group = groups.get(product.farmerId) ?? [];
        group.push(line);
        groups.set(product.farmerId, group);
      }

      const deliveryAddress = `${values.region}, ${values.district}, ${values.address}`;
      const orderNumbers: string[] = [];

      for (const [farmerId, groupLines] of groups) {
        const items = groupLines.map((line) => {
          const product = products.find((p) => p.id === line.productId)!;
          return {
            productListingId: product.id,
            productName: product.title,
            unitPrice: getUnitPrice(product, line.quantity),
            quantity: line.quantity,
          };
        });
        const farmerUserId = farmers.find((f) => f.id === farmerId)?.userId ?? 0;
        const created = await submitCustomerOrder(profile.id, {
          farmerId,
          farmerUserId,
          region: values.region,
          district: values.district,
          deliveryAddress,
          customerComment: values.comment || null,
          items,
        });
        orderNumbers.push(created.orderNumber);
      }

      setPlacedOrder({ orderNumbers, total: totalPrice, phone: values.phone, address: deliveryAddress });
      clearCart();
      toast.success(t("pages:checkout.toastTitle"), { description: t("pages:checkout.toastDescription", { orderNumber: orderNumbers[0] }) });
    } catch (err) {
      toast.error(t("pages:checkout.submitError"), { description: err instanceof ApiError ? err.message : undefined });
    }
  };

  // Реальный заказ создаётся от лица CustomerProfile (не User) — гостю и
  // пользователю без профиля покупателя (например, вошедшему как Farmer/Admin)
  // оформить нечем, поэтому вместо формы честно показываем, что нужно сделать.
  if (!user) {
    return (
      <div className="container-page flex min-h-[60vh] flex-col items-center justify-center gap-4 py-16 text-center">
        <span className="flex h-14 w-14 items-center justify-center rounded-full bg-grove-50 text-grove-700 dark:bg-grove-950 dark:text-grove-400">
          <LogIn size={24} />
        </span>
        <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("pages:checkout.loginRequiredTitle")}</h1>
        <p className="max-w-sm text-stone-500 dark:text-stone-400">{t("pages:checkout.loginRequiredDescription")}</p>
        <div className="mt-2 flex gap-3">
          <Link to="/login">
            <Button variant="outline" leftIcon={<LogIn size={16} />}>
              {t("common:auth.login")}
            </Button>
          </Link>
          <Link to="/register">
            <Button>{t("common:auth.register")}</Button>
          </Link>
        </div>
      </div>
    );
  }

  if (profileLoading) return <PageLoader />;

  if (!profile) {
    return (
      <div className="container-page flex min-h-[60vh] flex-col items-center justify-center gap-3 py-16 text-center">
        <span className="flex h-14 w-14 items-center justify-center rounded-full bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500">
          <Sprout size={24} />
        </span>
        <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("pages:checkout.customerAccountRequiredTitle")}</h1>
        <p className="max-w-sm text-stone-500 dark:text-stone-400">{t("pages:checkout.customerAccountRequiredDescription")}</p>
      </div>
    );
  }

  if (placedOrder) {
    return (
      <div className="container-page flex min-h-[70vh] flex-col items-center justify-center py-16 text-center">
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="flex w-full max-w-md flex-col items-center gap-4"
        >
          <span className="flex h-14 w-14 items-center justify-center rounded-full bg-grove-100 text-grove-700 dark:bg-grove-950 dark:text-grove-400">
            <CheckCircle2 size={28} />
          </span>
          <h1 className="font-display text-2xl text-stone-900 dark:text-stone-50">
            {t("pages:checkout.successTitle", { orderNumber: placedOrder.orderNumbers.join(", ") })}
          </h1>
          <p className="text-stone-500 dark:text-stone-400">
            {t("pages:checkout.successDescription", { phone: placedOrder.phone })}
          </p>

          <div className="mt-2 flex w-full flex-col gap-2 rounded-2xl border border-stone-100 bg-white p-5 text-left dark:border-stone-800 dark:bg-stone-900">
            <div className="flex items-center justify-between text-sm">
              <span className="text-stone-400 dark:text-stone-500">{t("pages:checkout.successDeliveryTitle")}</span>
              <span className="max-w-[60%] text-right font-medium text-stone-800 dark:text-stone-100">{placedOrder.address}</span>
            </div>
            <div className="flex items-center justify-between text-sm">
              <span className="text-stone-400 dark:text-stone-500">{t("pages:checkout.successTotal")}</span>
              <span className="font-semibold text-stone-900 dark:text-stone-50">
                {formatSomoni(placedOrder.total)} {t("common:currencySomoni")}
              </span>
            </div>
          </div>

          <div className="mt-2 flex w-full gap-3">
            <Button variant="outline" className="flex-1" onClick={() => navigate("/")}>
              {t("pages:checkout.backHome")}
            </Button>
            <Button className="flex-1" onClick={() => navigate("/catalog")}>
              {t("pages:checkout.continueShopping")}
            </Button>
          </div>
        </motion.div>
      </div>
    );
  }

  if (lines.length === 0) {
    return (
      <div className="container-page flex min-h-[60vh] flex-col items-center justify-center gap-3 py-16 text-center">
        <span className="flex h-14 w-14 items-center justify-center rounded-full bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500">
          <ShoppingBag size={24} />
        </span>
        <h1 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("pages:checkout.emptyTitle")}</h1>
        <p className="text-stone-500 dark:text-stone-400">{t("pages:checkout.emptyDescription")}</p>
        <Link to="/catalog">
          <Button className="mt-2">{t("pages:checkout.goToCatalog")}</Button>
        </Link>
      </div>
    );
  }

  return (
    <div>
      <div className="container-page pb-4 pt-8">
        <Breadcrumbs items={[{ label: t("layout:nav.catalog"), to: "/catalog" }, { label: t("pages:checkout.title") }]} />
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="container-page grid grid-cols-1 gap-8 pb-24 pt-4 lg:grid-cols-[1fr_0.85fr]">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="flex flex-col gap-5 rounded-3xl border border-stone-100 bg-white p-6 sm:p-8 dark:border-stone-800 dark:bg-stone-900"
        >
          <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">{t("pages:checkout.deliveryTitle")}</h2>

          <Input
            label={t("pages:checkout.fullNameLabel")}
            leftIcon={<User size={16} />}
            error={errors.fullName?.message}
            {...register("fullName", { required: t("pages:checkout.fullNameRequired") })}
          />
          <Controller
            name="phone"
            control={control}
            rules={{
              required: t("pages:checkout.phoneRequired"),
              validate: (v) => v.replace(/\D/g, "").length >= 12 || t("pages:checkout.phoneRequired"),
            }}
            render={({ field }) => (
              <PhoneInput label={t("pages:checkout.phoneLabel")} error={errors.phone?.message} value={field.value} onChange={field.onChange} />
            )}
          />
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <Controller
              name="region"
              control={control}
              rules={{ required: t("pages:checkout.regionRequired") }}
              render={({ field }) => (
                <Autocomplete
                  label={t("pages:checkout.regionLabel")}
                  placeholder={t("pages:checkout.regionPlaceholder")}
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
              rules={{ required: t("pages:checkout.districtRequired") }}
              render={({ field }) => (
                <Autocomplete
                  label={t("pages:checkout.districtLabel")}
                  placeholder={t("pages:checkout.districtPlaceholder")}
                  leftIcon={<MapPin size={16} />}
                  error={errors.district?.message}
                  value={field.value}
                  onChange={field.onChange}
                  options={districtOptions}
                />
              )}
            />
          </div>
          <Input
            label={t("pages:checkout.addressLabel")}
            placeholder={t("pages:checkout.addressPlaceholder")}
            leftIcon={<MapPin size={16} />}
            error={errors.address?.message}
            {...register("address", { required: t("pages:checkout.addressRequired") })}
          />
          <Textarea
            label={t("pages:checkout.commentLabel")}
            placeholder={t("pages:checkout.commentPlaceholder")}
            rows={3}
            {...register("comment")}
          />
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5, delay: 0.1 }}
          className="flex h-fit flex-col gap-4 rounded-3xl border border-stone-100 bg-white p-6 dark:border-stone-800 dark:bg-stone-900"
        >
          <div className="flex items-center justify-between">
            <h2 className="font-display text-lg text-stone-900 dark:text-stone-50">{t("pages:checkout.summaryTitle")}</h2>
            <span className="text-xs text-stone-400 dark:text-stone-500">{t("pages:checkout.itemsCount", { count: lines.length })}</span>
          </div>

          <div className="flex max-h-80 flex-col gap-3 overflow-y-auto pr-1">
            {lines.map((line) => {
              const product = products.find((p) => p.id === line.productId);
              if (!product) return null;
              return (
                <div key={line.productId} className="flex gap-3">
                  <div className="h-14 w-14 shrink-0 overflow-hidden rounded-xl">
                    <PhotoTile src={product.photoUrl} alt={product.title} className="h-full w-full" />
                  </div>
                  <div className="flex min-w-0 flex-1 flex-col gap-1">
                    <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{product.title}</p>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-1.5 rounded-full bg-stone-100 px-1.5 py-1 dark:bg-stone-800">
                        <button
                          type="button"
                          onClick={() => setQuantity(line.productId, Math.max(product.minimumOrderQuantity, line.quantity - 1))}
                          className="flex h-5 w-5 items-center justify-center rounded-full bg-white text-stone-600 shadow-sm dark:bg-stone-700 dark:text-stone-200"
                        >
                          <Minus size={11} />
                        </button>
                        <span className="min-w-4 text-center text-xs font-medium text-stone-700 dark:text-stone-200">
                          {line.quantity}
                        </span>
                        <button
                          type="button"
                          onClick={() => setQuantity(line.productId, line.quantity + 1)}
                          className="flex h-5 w-5 items-center justify-center rounded-full bg-white text-stone-600 shadow-sm dark:bg-stone-700 dark:text-stone-200"
                        >
                          <Plus size={11} />
                        </button>
                      </div>
                      <span className="text-sm font-semibold text-stone-900 dark:text-stone-100">
                        {formatSomoni(getUnitPrice(product, line.quantity) * line.quantity)} {t("common:currencySomoni")}
                      </span>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={() => removeItem(line.productId)}
                    aria-label={t("layout:miniCart.remove")}
                    className="self-start text-stone-300 transition hover:text-clay-500"
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              );
            })}
          </div>

          <div className="flex items-center justify-between border-t border-stone-100 pt-3 dark:border-stone-800">
            <span className="text-sm text-stone-500 dark:text-stone-400">{t("pages:checkout.subtotal")}</span>
            <span className="font-display text-lg text-stone-900 dark:text-stone-50">
              {formatSomoni(totalPrice)} {t("common:currencySomoni")}
            </span>
          </div>

          <p className="flex items-start gap-1.5 text-xs text-stone-400 dark:text-stone-500">
            <Info size={13} className="mt-0.5 shrink-0" />
            {t("pages:checkout.paymentNote")}
          </p>

          <Button type="submit" size="lg" loading={isSubmitting} className="w-full">
            {t("pages:checkout.submit")}
          </Button>
        </motion.div>
      </form>
    </div>
  );
}
