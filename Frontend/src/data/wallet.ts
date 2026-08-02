import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api";

export const CardType = { Visa: 1, Mastercard: 2, UnionPay: 3 } as const;

export const WalletTransactionType = { TopUp: 1, Purchase: 2, Refund: 3, FarmerCredit: 4 } as const;

// Верхний предел за одну операцию пополнения — то же значение, что и на
// бэкенде (WalletValidator.MaxTopUpAmount) — фронтенд не полагается только
// на сервер, дублирует проверку для мгновенной обратной связи в форме.
export const MAX_TOPUP_AMOUNT = 50_000;

export interface WalletDto {
  id: number;
  userId: number;
  cardHolderFirstName: string;
  cardHolderLastName: string;
  cardType: number;
  cardNumberLast4: string;
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

// GetMyWalletAsync отдаёт data: null (не 404), если карты ещё нет — так
// компонент однозначно различает "загружается" / "карты нет" / "вот карта".
export function useMyWallet(refreshKey = 0) {
  return useAsync(() => apiGet<WalletDto | null>("/wallet"), [refreshKey]);
}

export function useMyWalletTransactions(refreshKey = 0) {
  return useAsync(() => apiGet<WalletTransactionDto[]>("/wallet/transactions"), [refreshKey]);
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
  cardType: number;
}

export function createWallet(payload: CreateWalletPayload) {
  return apiPost<WalletDto>("/wallet", payload);
}

// Возвращает уже обновлённый WalletDto с новым балансом — страница кошелька
// использует его напрямую для оптимистичного обновления UI, без отдельного
// повторного GET /wallet.
export function topUpWallet(amount: number) {
  return apiPost<WalletDto>("/wallet/topup", { amount });
}
