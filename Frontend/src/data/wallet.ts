import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api";

export const CardType = { Visa: 1, Mastercard: 2, UnionPay: 3 } as const;

export const WalletTransactionType = { TopUp: 1, Purchase: 2, Refund: 3, FarmerCredit: 4 } as const;

// Верхний предел за одну операцию пополнения — то же значение, что и на
// бэкенде (WalletValidator.MaxTopUpAmount) — фронтенд не полагается только
// на сервер, дублирует проверку для мгновенной обратной связи в форме.
export const MAX_TOPUP_AMOUNT = 50_000;

// До 5 карт на пользователя — то же значение, что и WalletValidator.MaxCardsPerUser.
export const MAX_CARDS_PER_USER = 5;

export interface WalletDto {
  id: number;
  userId: number;
  cardHolderFirstName: string;
  cardHolderLastName: string;
  cardType: number;
  cardNumberLast4: string;
  expiryMonth: number;
  expiryYear: number;
  bankName: string;
  balance: number;
  createdAt: string;
  updatedAt: string;
}

export interface WalletTransactionDto {
  id: number;
  type: number;
  amount: number;
  balanceAfter: number;
  relatedOrderId: number | null;
  createdAt: string;
}

export interface FarmerPaymentCardDto {
  cardType: number;
  cardNumberLast4: string;
  bankName: string;
}

// PIN защищает вход в раздел "Кошелёк" на клиенте (один PIN на пользователя,
// не на карту) — см. backend WalletPinService/WalletController (pin/*).
export interface WalletPinStatusDto {
  isSet: boolean;
}

export function getWalletPinStatus() {
  return apiGet<WalletPinStatusDto>("/wallet/pin/status");
}

export function setWalletPin(pin: string, password: string) {
  return apiPost<string>("/wallet/pin/set", { pin, password });
}

export function verifyWalletPin(pin: string) {
  return apiPost<string>("/wallet/pin/verify", { pin });
}

interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

function useAsync<T>(fetcher: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [state, setState] = useState<AsyncState<T>>({ data: null, loading: true, error: null });

  useEffect(() => {
    let cancelled = false;
    setState({ data: null, loading: true, error: null });

    fetcher()
      .then((data) => {
        if (!cancelled) setState({ data, loading: false, error: null });
      })
      .catch((err: unknown) => {
        if (!cancelled) setState({ data: null, loading: false, error: err instanceof Error ? err.message : String(err) });
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return state;
}

// Список ВСЕХ карт пользователя (до 5), не одна — см. WalletService.GetMyWalletsAsync.
export function useMyWallets(refreshKey = 0) {
  return useAsync(() => apiGet<WalletDto[]>("/wallet"), [refreshKey]);
}

export function useWalletTransactions(walletId: number | null, refreshKey = 0) {
  return useAsync(
    () => (walletId ? apiGet<WalletTransactionDto[]>(`/wallet/${walletId}/transactions`) : Promise.resolve([] as WalletTransactionDto[])),
    [walletId, refreshKey],
  );
}

// Публичная карта фермера (без баланса/имени) — для его публичного профиля,
// доступна анонимно, поэтому не завязана на useAuth/токен.
export function useFarmerPaymentCard(farmerUserId: number | null) {
  return useAsync(
    () => (farmerUserId ? apiGet<FarmerPaymentCardDto | null>(`/wallet/farmer/${farmerUserId}/payment-card`) : Promise.resolve(null)),
    [farmerUserId],
  );
}

export interface CreateWalletPayload {
  cardHolderFirstName: string;
  cardHolderLastName: string;
  cardNumber: string;
  cvv: string;
  expiryMonth: number;
  expiryYear: number;
  bankName: string;
}

export function createWallet(payload: CreateWalletPayload) {
  return apiPost<WalletDto>("/wallet", payload);
}

// Возвращает уже обновлённый WalletDto с новым балансом — страница кошелька
// использует его напрямую для оптимистичного обновления UI, без отдельного
// повторного GET /wallet.
export function topUpWallet(walletId: number, amount: number) {
  return apiPost<WalletDto>(`/wallet/${walletId}/topup`, { amount });
}

// === Валидация — дублирует правила WalletValidator на бэкенде для мгновенной
// обратной связи в форме, а не только после round-trip к API. ===

// Алгоритм Луна: удваиваем каждую вторую цифру справа, если результат > 9 —
// вычитаем 9, суммируем все цифры, номер валиден, если сумма кратна 10.
export function passesLuhnCheck(digitsOnly: string): boolean {
  let sum = 0;
  let shouldDouble = false;
  for (let i = digitsOnly.length - 1; i >= 0; i--) {
    let digit = digitsOnly.charCodeAt(i) - 48;
    if (shouldDouble) {
      digit *= 2;
      if (digit > 9) digit -= 9;
    }
    sum += digit;
    shouldDouble = !shouldDouble;
  }
  return sum % 10 === 0;
}

// Тот же упрощённый BIN-порядок, что и WalletValidator.DetectCardType на
// бэкенде: 62 → UnionPay (проверяем раньше "5"/"4" — более специфичный
// префикс), 4 → Visa, 5 → Mastercard.
export function detectCardType(digitsOnly: string): number | null {
  if (digitsOnly.startsWith("62")) return CardType.UnionPay;
  if (digitsOnly.startsWith("4")) return CardType.Visa;
  if (digitsOnly.startsWith("5")) return CardType.Mastercard;
  return null;
}

export function formatCardNumberInput(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 16);
  return digits.replace(/(\d{4})(?=\d)/g, "$1 ");
}

export function formatExpiryInput(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 4);
  if (digits.length <= 2) return digits;
  return `${digits.slice(0, 2)}/${digits.slice(2)}`;
}
