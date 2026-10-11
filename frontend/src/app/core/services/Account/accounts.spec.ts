import { TestBed } from '@angular/core/testing';
import {
  provideHttpClient,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { AccountsService } from './accounts';
import { Account } from '../../models/account.model';

describe('AccountsService', () => {
  let service: AccountsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(AccountsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve consultar todas as contas', () => {
    const mockAccounts: Account[] = [
      {
        id: 'account-123',
        name: 'Conta Corrente',
        balance: 1500,
      },
    ];

    service.getAll().subscribe((accounts) => {
      expect(accounts).toEqual(mockAccounts);
      expect(accounts.length).toBe(1);
    });

    const req = httpMock.expectOne('/api/accounts');

    expect(req.request.method).toBe('GET');

    req.flush(mockAccounts);
  });

  it('deve retornar uma lista vazia quando não houver contas', () => {
    service.getAll().subscribe((accounts) => {
      expect(accounts).toEqual([]);
      expect(accounts.length).toBe(0);
    });

    const req = httpMock.expectOne('/api/accounts');

    expect(req.request.method).toBe('GET');

    req.flush([]);
  });

  it('deve propagar erros HTTP ao consultar as contas', () => {
    let receivedError: unknown;

    service.getAll().subscribe({
      error: (error: unknown) => {
        receivedError = error;
      },
    });

    const req = httpMock.expectOne('/api/accounts');

    req.flush(
      { message: 'Erro interno do servidor' },
      {
        status: 500,
        statusText: 'Internal Server Error',
      },
    );

    expect(receivedError).toBeTruthy();
  });
});