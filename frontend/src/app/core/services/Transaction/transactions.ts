import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Transaction {
  id: string;
  eventId: string;
  accountId: string;
  type: 'CREDIT' | 'DEBIT';
  amount: number;
  occurredAt: string;
  balanceAfter: number;
}

export interface TransactionPage {
  items: Transaction[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/accounts';

  getStatement(
    accountId: string,
    page = 1,
    pageSize = 10,
  ): Observable<TransactionPage> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    return this.http.get<TransactionPage>(
      `${this.apiUrl}/${accountId}/transactions`,
      { params },
    );
  }

  create(transaction: {
    eventId: string;
    accountId: string;
    type: 'CREDIT' | 'DEBIT';
    amount: number;
    occurredAt: string;
  }): Observable<unknown> {
    return this.http.post('/api/transactions', transaction);
  }
}