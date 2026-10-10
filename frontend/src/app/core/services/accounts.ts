import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Account } from '../models/account.model';

@Injectable({ providedIn: 'root' })
export class AccountsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/accounts';

  getAll(): Observable<Account[]> {
    return this.http.get<Account[]>(this.apiUrl);
  }
}