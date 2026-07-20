import type { SVGProps } from "react";

export function InstagramIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" {...props}>
      <rect x="3" y="3" width="18" height="18" rx="5" />
      <circle cx="12" cy="12" r="4.2" />
      <circle cx="17.2" cy="6.8" r="1.1" fill="currentColor" stroke="none" />
    </svg>
  );
}

export function TelegramIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" {...props}>
      <path
        d="M21.5 4.5 3.2 11.6c-1 .4-1 1.8.1 2.1l4.4 1.4 1.7 5.3c.3.9 1.5 1.1 2.1.3l2.5-3.2 4.5 3.3c.8.6 2 .2 2.2-.8l3.1-14.3c.2-1-.8-1.8-1.8-1.5Z"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinejoin="round"
      />
      <path d="M8.7 15.1l9.5-8.4-11 7" stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" />
    </svg>
  );
}

export function FacebookIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" {...props}>
      <path d="M15.5 8.5h-2a1 1 0 0 0-1 1V12h3l-.4 3h-2.6v7h-3v-7H8v-3h2.5V9.2C10.5 6.9 11.9 5.5 14.4 5.5h1.1v3Z" />
    </svg>
  );
}

export function WhatsAppIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" {...props}>
      <path d="M6.5 17.5 4 20l2.6-2.4A8 8 0 1 1 6.5 17.5Z" strokeLinejoin="round" />
      <path
        d="M9.2 9.6c.2-.6.5-.6.8-.6h.5c.2 0 .4 0 .6.4.2.4.6 1.4.6 1.5.1.1.1.3 0 .4-.1.2-.2.3-.3.4-.1.1-.3.3-.4.4-.1.1-.3.3-.1.6.2.3.7 1.1 1.5 1.8 1 .9 1.8 1.2 2.1 1.3.3.1.4.1.6-.1.2-.2.7-.8.9-1.1.2-.3.4-.2.6-.1.2.1 1.5.7 1.7.8.2.1.4.2.4.3 0 .2 0 .9-.3 1.3-.3.5-1.4 1-1.9 1-.5 0-1.6-.1-3.5-1.2-2.4-1.5-3.5-3.3-3.7-3.6-.2-.3-1-1.4-1-2.6 0-1.2.6-1.8.8-2Z"
        fill="currentColor"
      />
    </svg>
  );
}
