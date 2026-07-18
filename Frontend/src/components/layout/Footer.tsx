import { useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "sonner";
import { Leaf, Mail, MapPin, Phone, Send } from "lucide-react";
import { categories } from "@/data/categories";
import { officeInfo } from "@/data/site";
import { FacebookIcon, InstagramIcon, TelegramIcon, WhatsAppIcon } from "@/components/ui/SocialIcons";
import { Button } from "@/components/ui/Button";

const columns = [
  {
    title: "Покупателям",
    links: [
      { label: "Каталог", to: "/catalog" },
      { label: "О нас", to: "/about" },
      { label: "Контакты", to: "/contact" },
      { label: "Вход", to: "/login" },
    ],
  },
  {
    title: "Категории",
    links: categories.slice(0, 4).map((c) => ({ label: c.name, to: `/catalog?category=${c.slug}` })),
  },
];

export function Footer() {
  const [email, setEmail] = useState("");

  const subscribe = (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;
    toast.success("Спасибо за подписку!", { description: "Мы будем присылать только полезные новости." });
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
            <p className="max-w-xs text-sm leading-relaxed text-stone-400">
              Маркетплейс, который напрямую соединяет фермеров Таджикистана с покупателями — свежие продукты с поля,
              без посредников.
            </p>
            <div className="flex items-center gap-2.5">
              {[InstagramIcon, TelegramIcon, FacebookIcon, WhatsAppIcon].map((Icon, i) => (
                <a
                  key={i}
                  href="#"
                  onClick={(e) => e.preventDefault()}
                  aria-label="Социальная сеть"
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
            <h4 className="font-display text-sm text-white">Новости и предложения</h4>
            <p className="text-sm text-stone-400">Раз в неделю — сезонные подборки и советы от фермеров.</p>
            <form onSubmit={subscribe} className="flex gap-2">
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Ваш email"
                className="h-11 w-full min-w-0 rounded-xl border border-stone-800 bg-stone-900 px-4 text-sm text-white placeholder:text-stone-500 focus:border-grove-500 focus:ring-2 focus:ring-grove-900"
              />
              <Button type="submit" size="icon" aria-label="Подписаться">
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
          <p>© {new Date().getFullYear()} Market.tj. Все права защищены.</p>
          <div className="flex items-center gap-5">
            <Link to="/forbidden" className="hover:text-stone-300">
              Условия использования
            </Link>
            <Link to="/forbidden" className="hover:text-stone-300">
              Политика конфиденциальности
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
}
