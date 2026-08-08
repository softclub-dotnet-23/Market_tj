// Блок 3 (2026-08-08) — бэкенд возвращает готовое человекочитаемое сообщение
// вида "Аккаунт временно заблокирован до 10.08.2026 18:09 UTC. Причина: ..."
// (см. AccountBlockExtensions.FormatBlockMessage на бэкенде), а не отдельное
// структурированное поле с датой — формат сообщения фиксирован, поэтому дату
// разблокировки можно надёжно распарсить регуляркой, не меняя контракт API.
const BLOCKED_UNTIL_PATTERN = /до (\d{2})\.(\d{2})\.(\d{4}) (\d{2}):(\d{2}) UTC/;

export function parseBlockedUntil(message: string): Date | null {
  const match = message.match(BLOCKED_UNTIL_PATTERN);
  if (!match) return null;
  const [, dd, mm, yyyy, hh, min] = match;
  return new Date(Date.UTC(Number(yyyy), Number(mm) - 1, Number(dd), Number(hh), Number(min)));
}

export function formatCountdown(msRemaining: number): string {
  if (msRemaining <= 0) return "0:00";
  const totalSeconds = Math.floor(msRemaining / 1000);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  if (hours > 0) return `${hours}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
  return `${minutes}:${String(seconds).padStart(2, "0")}`;
}
