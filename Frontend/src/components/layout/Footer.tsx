import { useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "sonner";
import { useTranslation } from "react-i18next";
import { Leaf, Mail, MapPin, Phone, Send } from "lucide-react";
import { useCategories } from "@/data/categories";
import { useOfficeInfo } from "@/data/site";
import { FacebookIcon, InstagramIcon, TelegramIcon, WhatsAppIcon } from "@/components/ui/SocialIcons";
import { Button } from "@/components/ui/Button";

export function Footer() {
  const { t } = useTranslation(["layout", "common"]);
  const categories = useCategories();
  const officeInfo = useOfficeInfo();
  const [email, setEmail] = useState("");

  const columns = [
    {
      title: t("layout:footer.columnCustomers"),
      links: [
        { label: t("layout:nav.catalog"), to: "/catalog" },
        { label: t("layout:nav.about"), to: "/about" },
        { label: t("layout:nav.contact"), to: "/contact" },
        { label: t("layout:footer.login"), to: "/login" },
      ],
    },
    {
      title: t("layout:footer.categoriesTitle"),
      links: categories.slice(0, 4).map((c) => ({ label: c.name, to: `/catalog?category=${c.slug}` })),
    },
  ];

  const subscribe = (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;
    toast.success(t("layout:footer.toastTitle"), { description: t("layout:footer.toastDescription") });
    setEmail("");
  };

  return (
    <footer className="mt-24 border-t border-stone-100 bg-stone-950 text-stone-300">
      <div className="container-page py-16">
        <div className="grid grid-cols-1 gap-12 lg:grid-cols-[1.4fr_0.8fr_0.8fr_1.2fr]">
          <div className="flex flex-col gap-5">
            <Link to="/" className="flex items-center gap-2">
              <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-grove-600 text-white">
                <Leaf size={18} />
              </span>
              <span className="font-display text-xl text-white">
                Market<span className="text-grove-400">.tj</span>
              </span>
            </Link>
            <p className="max-w-xs text-sm leading-relaxed text-stone-400">{t("layout:footer.description")}</p>
            <div className="flex items-center gap-2.5">
              {[InstagramIcon, TelegramIcon, FacebookIcon, WhatsAppIcon].map((Icon, i) => (
                <a
                  key={i}
                  href="#"
                  onClick={(e) => e.preventDefault()}
                  aria-label={t("common:actions.socialNetwork")}
                  className="flex h-9 w-9 items-center justify-center rounded-full border border-stone-800 text-stone-400 transition hover:border-grove-500 hover:text-grove-400"
                >
                  <Icon className="h-4 w-4" />
                </a>
              ))}
            </div>
          </div>

          {columns.map((col) => (
            <div key={col.title} className="flex flex-col gap-3">
              <h4 className="font-display text-sm text-white">{col.title}</h4>
              <ul className="flex flex-col gap-2.5">
                {col.links.map((link) => (
                  <li key={link.label}>
                    <Link to={link.to} className="text-sm text-stone-400 transition hover:text-grove-400">
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}

          <div className="flex flex-col gap-4">
            <h4 className="font-display text-sm text-white">{t("layout:footer.newsletterTitle")}</h4>
            <p className="text-sm text-stone-400">{t("layout:footer.newsletterSubtitle")}</p>
            <form onSubmit={subscribe} className="flex gap-2">
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder={t("layout:footer.emailPlaceholder")}
                className="h-11 w-full min-w-0 rounded-xl border border-stone-800 bg-stone-900 px-4 text-sm text-white placeholder:text-stone-500 focus:border-grove-500 focus:ring-2 focus:ring-grove-900"
              />
              <Button type="submit" size="icon" aria-label={t("common:actions.subscribe")}>
                <Send size={16} />
              </Button>
            </form>
            <div className="flex flex-col gap-2 pt-1 text-sm text-stone-400">
              <span className="flex items-center gap-2">
                <MapPin size={14} className="shrink-0 text-grove-500" />
                {officeInfo.address}
              </span>
              <a href={`tel:${officeInfo.phone}`} className="flex items-center gap-2 hover:text-grove-400">
                <Phone size={14} className="shrink-0 text-grove-500" />
                {officeInfo.phone}
              </a>
              <a href={`mailto:${officeInfo.email}`} className="flex items-center gap-2 hover:text-grove-400">
                <Mail size={14} className="shrink-0 text-grove-500" />
                {officeInfo.email}
              </a>
            </div>
          </div>
        </div>

        <div className="mt-14 flex flex-col items-center justify-between gap-3 border-t border-stone-900 pt-6 text-xs text-stone-500 sm:flex-row">
          <p>{t("layout:footer.copyright", { year: new Date().getFullYear() })}</p>
          <div className="flex items-center gap-5">
            <Link to="/forbidden" className="hover:text-stone-300">
              {t("layout:footer.terms")}
            </Link>
            <Link to="/forbidden" className="hover:text-stone-300">
              {t("layout:footer.privacy")}
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
}
