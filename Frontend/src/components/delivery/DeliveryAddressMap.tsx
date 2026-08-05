import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";

// Google Maps JS API + Geocoding API — по прямому запросу пользователя
// (2026-08-04), лёгкая v1: один пин адреса доставки (не координаты каждого
// курьера — в схеме их вообще нет, полноценный "ближайший курьер" на карте
// потребовал бы хранить lat/lng, это отдельная задача на будущее).
//
// Ключ ОБЯЗАН быть клиентским (VITE_GOOGLE_MAPS_API_KEY, вшивается в бандл
// при сборке — тот же приём, что VITE_API_BASE_URL в Frontend/Dockerfile):
// HTTP-referrer restriction, которым по инструкции ограничивается ключ,
// работает только для запросов из браузера.
//
// Пока ключ не задан — компонент рендерит null (тихий no-op), список
// курьеров рядом работает независимо от карты.

declare global {
  interface Window {
    google?: typeof google;
  }
}

let mapsLoaderPromise: Promise<void> | null = null;

function loadGoogleMaps(apiKey: string): Promise<void> {
  if (window.google?.maps) return Promise.resolve();
  if (mapsLoaderPromise) return mapsLoaderPromise;

  mapsLoaderPromise = new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=geocoding`;
    script.async = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error("Google Maps failed to load"));
    document.head.appendChild(script);
  });

  return mapsLoaderPromise;
}

export function DeliveryAddressMap({ address, region, district }: { address: string; region: string; district: string }) {
  const { t } = useTranslation("delivery");
  const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY as string | undefined;
  const containerRef = useRef<HTMLDivElement>(null);
  const [status, setStatus] = useState<"idle" | "loading" | "ready" | "error">("idle");

  useEffect(() => {
    if (!apiKey || !containerRef.current) return;
    let cancelled = false;
    setStatus("loading");

    loadGoogleMaps(apiKey)
      .then(() => {
        if (cancelled || !containerRef.current) return;
        const geocoder = new google.maps.Geocoder();
        const query = [address, district, region, "Таджикистан"].filter(Boolean).join(", ");
        geocoder.geocode({ address: query }, (results, geoStatus) => {
          if (cancelled) return;
          if (geoStatus !== "OK" || !results?.[0] || !containerRef.current) {
            setStatus("error");
            return;
          }
          const location = results[0].geometry.location;
          const map = new google.maps.Map(containerRef.current, {
            center: location,
            zoom: 14,
            disableDefaultUI: true,
            zoomControl: true,
          });
          new google.maps.Marker({ position: location, map });
          setStatus("ready");
        });
      })
      .catch(() => {
        if (!cancelled) setStatus("error");
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiKey, address, region, district]);

  if (!apiKey) return null;

  return (
    <div className="overflow-hidden rounded-2xl border border-stone-100 dark:border-stone-800">
      <div ref={containerRef} className="h-48 w-full bg-stone-100 dark:bg-stone-800" />
      {status === "error" && (
        <p className="border-t border-stone-100 px-3 py-2 text-xs text-stone-400 dark:border-stone-800 dark:text-stone-500">
          {t("assign.mapError")}
        </p>
      )}
    </div>
  );
}
