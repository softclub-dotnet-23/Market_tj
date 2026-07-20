import { cn } from "@/lib/utils";

const PALETTES = [
  ["#3ba85a", "#1f5731"],
  ["#eaab38", "#9c581d"],
  ["#d1663d", "#7f471e"],
  ["#4d8fb0", "#2a5a73"],
  ["#8a6fb0", "#4f3d73"],
  ["#5fa88f", "#296e56"],
];

function hashString(value: string) {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

function getInitials(name: string) {
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[1][0]).toUpperCase();
}

interface AvatarProps {
  name: string;
  src?: string;
  size?: number;
  className?: string;
  ring?: boolean;
}

export function Avatar({ name, src, size = 44, className, ring = false }: AvatarProps) {
  if (src) {
    return (
      <img
        src={src}
        alt={name}
        loading="lazy"
        className={cn("shrink-0 rounded-full object-cover", ring && "ring-4 ring-white dark:ring-stone-900", className)}
        style={{ width: size, height: size }}
      />
    );
  }

  const idx = hashString(name) % PALETTES.length;
  const [from, to] = PALETTES[idx];

  return (
    <div
      className={cn(
        "flex shrink-0 items-center justify-center rounded-full font-display font-medium text-white",
        ring && "ring-4 ring-white dark:ring-stone-900",
        className,
      )}
      style={{
        width: size,
        height: size,
        fontSize: size * 0.38,
        background: `linear-gradient(135deg, ${from}, ${to})`,
      }}
    >
      {getInitials(name)}
    </div>
  );
}
