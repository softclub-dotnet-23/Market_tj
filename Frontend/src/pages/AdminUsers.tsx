import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Mail, Pencil, Phone, Plus, Search, Trash2, Users } from "lucide-react";
import { useAdminSearch } from "@/components/layout/AdminLayout";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Pagination } from "@/components/ui/Pagination";
import { Avatar } from "@/components/ui/Avatar";
import { ViewModeToggle, type OrdersViewMode } from "@/components/ui/ViewModeToggle";
import { Modal } from "@/components/ui/Modal";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { Button } from "@/components/ui/Button";
import { Checkbox, Input } from "@/components/ui/Field";
import { formatDate } from "@/lib/utils";
import { createCustomer, deleteCustomer, updateCustomer, useAdminCustomers, type AdminUserDto } from "@/data/adminEntities";

// Кратно 3 — карточный вид на десктопе всегда 3 колонки (см. xl:grid-cols-3
// ниже), так последняя строка страницы не остаётся неполной/одинокой.
const PAGE_SIZE = 9;

interface CustomerFormValues {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
  isActive: boolean;
}

function CustomerFormModal({
  open,
  onClose,
  editing,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  editing: AdminUserDto | null;
  onSaved: () => void;
}) {
  const { t } = useTranslation("admin");
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CustomerFormValues>({ defaultValues: { isActive: true } });

  useEffect(() => {
    if (open) {
      reset(
        editing
          ? { fullName: editing.fullName, email: editing.email, phoneNumber: editing.phoneNumber, password: "", isActive: editing.isActive }
          : { fullName: "", email: "", phoneNumber: "", password: "", isActive: true },
      );
    }
  }, [open, editing, reset]);

  const onSubmit = async (values: CustomerFormValues) => {
    const dto = {
      fullName: values.fullName,
      email: values.email,
      phoneNumber: values.phoneNumber,
      password: values.password || null,
      isActive: values.isActive,
    };
    try {
      if (editing) {
        await updateCustomer(editing.id, dto);
        toast.success(t("users.updateSuccess"));
      } else {
        await createCustomer(dto);
        toast.success(t("users.createSuccess"));
      }
      onSaved();
      onClose();
    } catch (err) {
      toast.error(editing ? t("users.updateError") : t("users.createError"), {
        description: err instanceof Error ? err.message : undefined,
      });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-md">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">
        {editing ? t("users.editModalTitle") : t("users.createModalTitle")}
      </h2>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
        <Input label={t("users.form.fullName")} error={errors.fullName?.message} {...register("fullName", { required: t("users.form.required") })} />
        <Input
          type="email"
          label={t("users.form.email")}
          error={errors.email?.message}
          {...register("email", { required: t("users.form.required") })}
        />
        <Input
          label={t("users.form.phone")}
          error={errors.phoneNumber?.message}
          {...register("phoneNumber", { required: t("users.form.required") })}
        />
        <Input
          type="password"
          label={t("users.form.password")}
          hint={editing ? t("users.form.passwordEditHint") : t("users.form.passwordCreateHint")}
          error={errors.password?.message}
          {...register("password", {
            required: editing ? false : t("users.form.required"),
            minLength: { value: 6, message: t("users.form.passwordTooShort") },
          })}
        />
        <Checkbox label={t("users.form.isActive")} {...register("isActive")} />
        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("users.form.cancel")}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {editing ? t("users.form.saveChanges") : t("users.form.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

export function AdminUsers() {
  const { t } = useTranslation("admin");
  const searchQuery = useAdminSearch();
  const [page, setPage] = useState(1);
  const [viewMode, setViewMode] = useState<OrdersViewMode>("table");
  const [refreshKey, setRefreshKey] = useState(0);
  const { customers, loading, error } = useAdminCustomers(refreshKey);

  const [formOpen, setFormOpen] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<AdminUserDto | null>(null);
  const [deletingCustomer, setDeletingCustomer] = useState<AdminUserDto | null>(null);

  const bump = () => setRefreshKey((k) => k + 1);

  useEffect(() => {
    setPage(1);
  }, [searchQuery]);

  const openCreate = () => {
    setEditingCustomer(null);
    setFormOpen(true);
  };

  const openEdit = (customer: AdminUserDto) => {
    setEditingCustomer(customer);
    setFormOpen(true);
  };

  const handleDelete = async () => {
    if (!deletingCustomer) return;
    try {
      await deleteCustomer(deletingCustomer.id);
      toast.success(t("users.deleteSuccess"));
      bump();
    } catch (err) {
      toast.error(t("users.deleteError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setDeletingCustomer(null);
    }
  };

  if (loading) return <PageLoader />;

  if (error || !customers) {
    return <EmptyState icon={<Users size={26} />} title={t("users.errorTitle")} description={error ?? t("users.errorDescription")} />;
  }

  const query = searchQuery.trim().toLowerCase();
  const filteredCustomers = query
    ? customers.filter(
        (c) =>
          c.fullName.toLowerCase().includes(query) ||
          c.email.toLowerCase().includes(query) ||
          c.phoneNumber.toLowerCase().includes(query),
      )
    : customers;

  const totalPages = Math.max(1, Math.ceil(filteredCustomers.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems: AdminUserDto[] = filteredCustomers.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const renderCard = (customer: AdminUserDto) => (
    <div
      key={customer.id}
      className="flex flex-col gap-4 rounded-2xl border border-stone-100 bg-white p-5 shadow-sm transition hover:shadow-md dark:border-stone-800 dark:bg-stone-900"
    >
      <div className="flex items-start gap-3">
        <Avatar name={customer.fullName} size={44} />
        <div className="min-w-0 flex-1">
          <p className="truncate font-medium text-stone-800 dark:text-stone-100">{customer.fullName}</p>
          <p className="mt-0.5 flex items-center gap-1 truncate text-xs text-stone-400 dark:text-stone-500">
            <Mail size={12} className="shrink-0" />
            {customer.email}
          </p>
        </div>
        <span
          className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${
            customer.isActive
              ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
              : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
          }`}
        >
          {t(customer.isActive ? "users.status.active" : "users.status.inactive")}
        </span>
      </div>
      <div className="flex items-center justify-between border-t border-stone-50 pt-3 text-sm dark:border-stone-800/60">
        <span className="flex items-center gap-1.5 text-stone-500 dark:text-stone-400">
          <Phone size={13} className="shrink-0" />
          {customer.phoneNumber}
        </span>
        <span className="text-xs text-stone-400 dark:text-stone-500">{formatDate(customer.createdAt)}</span>
      </div>
      <div className="flex items-center justify-end gap-1.5 border-t border-stone-50 pt-3 dark:border-stone-800/60">
        <button
          onClick={() => openEdit(customer)}
          aria-label={t("users.editAction")}
          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-grove-400"
        >
          <Pencil size={15} />
        </button>
        <button
          onClick={() => setDeletingCustomer(customer)}
          aria-label={t("users.deleteAction")}
          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
        >
          <Trash2 size={15} />
        </button>
      </div>
    </div>
  );

  return (
    <div className="flex flex-col gap-5">
      <div className="flex justify-end">
        <Button leftIcon={<Plus size={16} />} onClick={openCreate}>
          {t("users.addButton")}
        </Button>
      </div>

      {customers.length === 0 ? (
        <EmptyState icon={<Users size={26} />} title={t("users.emptyTitle")} description={t("users.emptyDescription")} />
      ) : query && filteredCustomers.length === 0 ? (
        <EmptyState icon={<Search size={26} />} title={t("common.searchEmptyTitle")} description={t("common.searchEmptyDescription")} />
      ) : (
        <div className="overflow-hidden rounded-3xl border border-stone-100 bg-linear-to-b from-white to-stone-50/60 shadow-(--shadow-soft) dark:border-stone-800 dark:from-stone-900 dark:to-stone-900">
          <div className="hidden items-center justify-end border-b border-stone-100 p-4 lg:flex dark:border-stone-800">
            <ViewModeToggle value={viewMode} onChange={setViewMode} ns="admin" />
          </div>

          <div className={viewMode === "table" ? "hidden overflow-x-auto lg:block" : "hidden"}>
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="px-6 py-4 font-medium">{t("users.columns.fullName")}</th>
                  <th className="px-6 py-4 font-medium">{t("users.columns.email")}</th>
                  <th className="px-6 py-4 font-medium">{t("users.columns.phone")}</th>
                  <th className="px-6 py-4 font-medium">{t("users.columns.status")}</th>
                  <th className="px-6 py-4 font-medium">{t("users.columns.createdAt")}</th>
                  <th className="px-6 py-4 font-medium text-right">{t("users.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {pageItems.map((customer) => (
                  <tr key={customer.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{customer.fullName}</td>
                    <td className="px-6 py-4 text-stone-600 dark:text-stone-300">{customer.email}</td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{customer.phoneNumber}</td>
                    <td className="px-6 py-4">
                      <span
                        className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                          customer.isActive
                            ? "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300"
                            : "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500"
                        }`}
                      >
                        {t(customer.isActive ? "users.status.active" : "users.status.inactive")}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-stone-500 dark:text-stone-400">{formatDate(customer.createdAt)}</td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-1.5">
                        <button
                          onClick={() => openEdit(customer)}
                          aria-label={t("users.editAction")}
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-grove-400"
                        >
                          <Pencil size={15} />
                        </button>
                        <button
                          onClick={() => setDeletingCustomer(customer)}
                          aria-label={t("users.deleteAction")}
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                        >
                          <Trash2 size={15} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {viewMode === "cards" && (
            <div className="hidden grid-cols-2 gap-4 p-5 lg:grid xl:grid-cols-3">{pageItems.map(renderCard)}</div>
          )}

          <div className="grid grid-cols-1 gap-4 p-5 lg:hidden">{pageItems.map(renderCard)}</div>

          {totalPages > 1 && (
            <div className="border-t border-stone-100 p-4 dark:border-stone-800">
              <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
            </div>
          )}
        </div>
      )}

      <CustomerFormModal open={formOpen} onClose={() => setFormOpen(false)} editing={editingCustomer} onSaved={bump} />

      <ConfirmDialog
        open={!!deletingCustomer}
        onClose={() => setDeletingCustomer(null)}
        onConfirm={handleDelete}
        title={t("users.deleteConfirmTitle")}
        description={deletingCustomer ? t("users.deleteConfirmDescription", { name: deletingCustomer.fullName }) : undefined}
        confirmLabel={t("users.deleteAction")}
      />
    </div>
  );
}
