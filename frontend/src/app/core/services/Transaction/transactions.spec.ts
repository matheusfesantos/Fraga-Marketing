import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';

import { TransactionPage, TransactionsService } from './transactions';

describe('TransactionsService', () => {
  let service: TransactionsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });

    service = TestBed.inject(TransactionsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve consultar o extrato com paginação padrão', () => {
    const accountId = 'account-123';

    const mockResponse: TransactionPage = {
      items: [],
      page: 1,
      pageSize: 10,
      totalItems: 0,
      totalPages: 0,
    };

    service.getStatement(accountId).subscribe((response) => {
      expect(response).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(
      (request) =>
        request.url === `/api/accounts/${accountId}/transactions`,
    );

    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('10');

    req.flush(mockResponse);
  });

  it('deve consultar o extrato com a paginação informada', () => {
    const accountId = 'account-456';
    const page = 2;
    const pageSize = 5;

    const mockResponse: TransactionPage = {
      items: [],
      page,
      pageSize,
      totalItems: 0,
      totalPages: 0,
    };

    service.getStatement(accountId, page, pageSize).subscribe((response) => {
      expect(response.page).toBe(page);
      expect(response.pageSize).toBe(pageSize);
    });

    const req = httpMock.expectOne(
      (request) =>
        request.url === `/api/accounts/${accountId}/transactions`,
    );

    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('5');

    req.flush(mockResponse);
  });

  it('deve criar uma transação', () => {
    const transaction = {
      eventId: 'event-123',
      accountId: 'account-123',
      type: 'CREDIT' as const,
      amount: 150,
      occurredAt: '2026-10-10T10:00:00Z',
    };

    const mockResponse = { id: 'transaction-123' };

    service.create(transaction).subscribe((response) => {
      expect(response).toEqual(mockResponse);
    });

    const req = httpMock.expectOne('/api/transactions');

    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(transaction);

    req.flush(mockResponse);
  });

  it('deve propagar erros HTTP ao consultar o extrato', () => {
    let receivedError: unknown;

    service.getStatement('account-123').subscribe({
      error: (error: unknown) => {
        receivedError = error;
      },
    });

    const req = httpMock.expectOne(
      (request) =>
        request.url === '/api/accounts/account-123/transactions',
    );

    req.flush(
      { message: 'Erro interno do servidor' },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(receivedError).toBeTruthy();
  });
});