import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Pencil, Plus, Tags, Trash2 } from "lucide-react";
import { PageLoader } from "@/components/layout/PageLoader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Modal } from "@/components/ui/Modal";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { Button } from "@/components/ui/Button";
import { Checkbox, Input, Textarea } from "@/components/ui/Field";
import { cn } from "@/lib/utils";
import {
  createCategory,
  deleteCategory,
  updateCategory,
  useAdminCategories,
  type AdminCategoryDto,
} from "@/data/adminEntities";

interface CategoryFormValues {
  name: string;
  nameTj: string;
  nameEn: string;
  description: string;
  imageUrl: string;
  isActive: boolean;
}

function CategoryFormModal({
  open,
  onClose,
  editing,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  editing: AdminCategoryDto | null;
  onSaved: () => void;
}) {
  const { t } = useTranslation("admin");
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CategoryFormValues>({ defaultValues: { isActive: true } });

  useEffect(() => {
    if (open) {
      reset(
        editing
          ? {
              name: editing.name,
              nameTj: editing.nameTj ?? "",
              nameEn: editing.nameEn ?? "",
              description: editing.description ?? "",
              imageUrl: editing.imageUrl ?? "",
              isActive: editing.isActive,
            }
          : { name: "", nameTj: "", nameEn: "", description: "", imageUrl: "", isActive: true },
      );
    }
  }, [open, editing, reset]);

  const onSubmit = async (values: CategoryFormValues) => {
    const dto = {
      name: values.name,
      nameTj: values.nameTj,
      nameEn: values.nameEn,
      description: values.description || null,
      imageUrl: values.imageUrl || null,
      isActive: values.isActive,
    };
    try {
      if (editing) {
        await updateCategory(editing.id, dto);
        toast.success(t("catalog.categoryUpdateSuccess"));
      } else {
        await createCategory(dto);
        toast.success(t("catalog.categoryCreateSuccess"));
      }
      onSaved();
      onClose();
    } catch (err) {
      toast.error(editing ? t("catalog.categoryUpdateError") : t("catalog.categoryCreateError"), {
        description: err instanceof Error ? err.message : undefined,
      });
    }
  };

  return (
    <Modal open={open} onClose={onClose} className="max-w-md">
      <h2 className="font-display text-xl text-stone-900 dark:text-stone-50">
        {editing ? t("catalog.categoryEditModalTitle") : t("catalog.categoryCreateModalTitle")}
      </h2>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-6 flex flex-col gap-5">
        <Input label={t("catalog.form.nameRu")} error={errors.name?.message} {...register("name", { required: t("catalog.form.required") })} />
        <Input label={t("catalog.form.nameTj")} error={errors.nameTj?.message} {...register("nameTj", { required: t("catalog.form.required") })} />
        <Input label={t("catalog.form.nameEn")} error={errors.nameEn?.message} {...register("nameEn", { required: t("catalog.form.required") })} />
        <Textarea label={t("catalog.form.description")} hint={t("catalog.form.optional")} {...register("description")} />
        <Input label={t("catalog.form.imageUrl")} hint={t("catalog.form.optional")} {...register("imageUrl")} />
        <Checkbox label={t("catalog.form.isActive")} {...register("isActive")} />
        <div className="mt-2 flex justify-end gap-3">
          <Button type="button" variant="outline" onClick={onClose}>
            {t("catalog.form.cancel")}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {editing ? t("catalog.form.saveChanges") : t("catalog.form.submit")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

const ACTIVE_CLASSES = "bg-grove-100 text-grove-700 dark:bg-grove-900 dark:text-grove-300";
const INACTIVE_CLASSES = "bg-stone-100 text-stone-400 dark:bg-stone-800 dark:text-stone-500";

// Категории — единственное, чем управляет здесь Admin (раньше был ещё
// справочник базовых "Товаров", но по прямому запросу пользователя название
// товара и единицу измерения теперь вводит сам фермер при создании
// объявления — см. FarmerProducts.tsx, ProductListing.CategoryId/Unit).
// Список показывает название на текущем языке интерфейса админа (та же логика,
// что и в data/categories.ts для публичного каталога) — раньше всегда показывал
// c.name (русский) независимо от выбранного языка, хотя админ обязан вводить
// все 3 варианта именно для этого. Форма редактирования по-прежнему показывает
// все 3 поля явно — их вводит и правит сам админ, а не текущий язык интерфейса.
function displayName(c: AdminCategoryDto, language: string): string {
  if (language === "tj") return c.nameTj ?? c.name;
  if (language === "en") return c.nameEn ?? c.name;
  return c.name;
}

export function AdminCatalog() {
  const { t, i18n } = useTranslation("admin");
  const [refreshKey, setRefreshKey] = useState(0);
  const { categories, loading, error } = useAdminCategories(refreshKey);

  const [categoryModalOpen, setCategoryModalOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<AdminCategoryDto | null>(null);
  const [deletingCategory, setDeletingCategory] = useState<AdminCategoryDto | null>(null);

  const bump = () => setRefreshKey((k) => k + 1);

  const handleDeleteCategory = async () => {
    if (!deletingCategory) return;
    try {
      await deleteCategory(deletingCategory.id);
      toast.success(t("catalog.categoryDeleteSuccess"));
      bump();
    } catch (err) {
      toast.error(t("catalog.categoryDeleteError"), { description: err instanceof Error ? err.message : undefined });
    } finally {
      setDeletingCategory(null);
    }
  };

  return (
    <div className="flex flex-col gap-5">
      <div className="flex justify-end">
        <Button
          leftIcon={<Plus size={16} />}
          onClick={() => {
            setEditingCategory(null);
            setCategoryModalOpen(true);
          }}
        >
          {t("catalog.addCategoryButton")}
        </Button>
      </div>

      {loading ? (
        <PageLoader />
      ) : error || !categories ? (
        <EmptyState icon={<Tags size={26} />} title={t("catalog.categoryErrorTitle")} description={error ?? t("catalog.categoryErrorDescription")} />
      ) : categories.length === 0 ? (
        <EmptyState icon={<Tags size={26} />} title={t("catalog.categoryEmptyTitle")} description={t("catalog.categoryEmptyDescription")} />
      ) : (
        <div className="rounded-3xl border border-stone-100 bg-white dark:border-stone-800 dark:bg-stone-900">
          <div className="flex flex-col gap-3 p-4 lg:hidden">
            {categories.map((c) => (
              <div key={c.id} className="flex flex-col gap-2 rounded-2xl border border-stone-100 p-4 dark:border-stone-800">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="truncate font-medium text-stone-800 dark:text-stone-100">{displayName(c, i18n.language)}</p>
                    {c.description && <p className="mt-0.5 truncate text-sm text-stone-500 dark:text-stone-400">{c.description}</p>}
                  </div>
                  <span className={cn("shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold", c.isActive ? ACTIVE_CLASSES : INACTIVE_CLASSES)}>
                    {c.isActive ? t("catalog.statusActive") : t("catalog.statusInactive")}
                  </span>
                </div>
                <div className="flex items-center justify-end gap-1.5 border-t border-stone-50 pt-2 dark:border-stone-800/60">
                  <button
                    onClick={() => {
                      setEditingCategory(c);
                      setCategoryModalOpen(true);
                    }}
                    aria-label={t("catalog.editAction")}
                    className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-grove-400"
                  >
                    <Pencil size={15} />
                  </button>
                  <button
                    onClick={() => setDeletingCategory(c)}
                    aria-label={t("catalog.deleteAction")}
                    className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-rose-50 hover:text-rose-600 dark:text-stone-500 dark:hover:bg-rose-950 dark:hover:text-rose-400"
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="hidden overflow-x-auto lg:block">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-stone-100 text-xs uppercase tracking-wide text-stone-400 dark:border-stone-800 dark:text-stone-500">
                  <th className="px-6 py-4 font-medium">{t("catalog.columns.name")}</th>
                  <th className="px-6 py-4 font-medium">{t("catalog.columns.description")}</th>
                  <th className="px-6 py-4 font-medium">{t("catalog.columns.status")}</th>
                  <th className="px-6 py-4 font-medium text-right">{t("catalog.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {categories.map((c) => (
                  <tr key={c.id} className="border-b border-stone-50 last:border-0 dark:border-stone-800/60">
                    <td className="px-6 py-4 font-medium text-stone-800 dark:text-stone-100">{displayName(c, i18n.language)}</td>
                    <td className="max-w-80 truncate px-6 py-4 text-stone-500 dark:text-stone-400">{c.description ?? "—"}</td>
                    <td className="px-6 py-4">
                      <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", c.isActive ? ACTIVE_CLASSES : INACTIVE_CLASSES)}>
                        {c.isActive ? t("catalog.statusActive") : t("catalog.statusInactive")}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-1.5">
                        <button
                          onClick={() => {
                            setEditingCategory(c);
                            setCategoryModalOpen(true);
                          }}
                          aria-label={t("catalog.editAction")}
                          className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-400 transition hover:bg-stone-100 hover:text-grove-700 dark:text-stone-500 dark:hover:bg-stone-800 dark:hover:text-grove-400"
                        >
                          <Pencil size={15} />
                        </button>
                        <button
                          onClick={() => setDeletingCategory(c)}
                          aria-label={t("catalog.deleteAction")}
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
        </div>
      )}

      <CategoryFormModal open={categoryModalOpen} onClose={() => setCategoryModalOpen(false)} editing={editingCategory} onSaved={bump} />

      <ConfirmDialog
        open={!!deletingCategory}
        onClose={() => setDeletingCategory(null)}
        onConfirm={handleDeleteCategory}
        title={t("catalog.categoryDeleteConfirmTitle")}
        description={deletingCategory ? t("catalog.categoryDeleteConfirmDescription", { name: deletingCategory.name }) : undefined}
        confirmLabel={t("catalog.deleteAction")}
      />
    </div>
  );
}
