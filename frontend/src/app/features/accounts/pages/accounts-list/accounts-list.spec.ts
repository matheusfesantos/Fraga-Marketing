import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { jest } from '@jest/globals';

import { AccountsList } from './accounts-list';
import { AccountsService } from '../../../../core/services/Account/accounts';
import { TransactionsService } from '../../../../core/services/Transaction/transactions';
import { Account } from '../../../../core/models/account.model';
import { TransactionPage } from '../../../../core/services/Transaction/transactions';

describe('AccountsList', () => {
  let component: AccountsList;
  let fixture: ComponentFixture<AccountsList>;
  let accountsService: {
    getAll: jest.Mock;
  };
  let transactionsService: {
    getStatement: jest.Mock;
  };

  const mockAccounts: Account[] = [
    {
      id: 'account-1',
      name: 'Conta Corrente',
      balance: 1500,
    },
    {
      id: 'account-2',
      name: 'Conta Poupança',
      balance: 2500,
    },
  ];

  const mockStatement: TransactionPage = {
    items: [
      {
        id: 'transaction-1',
        eventId: 'event-1',
        accountId: 'account-1',
        type: 'CREDIT',
        amount: 300,
        occurredAt: '2026-10-10T10:00:00Z',
        balanceAfter: 1800,
      },
    ],
    page: 1,
    pageSize: 3,
    totalItems: 1,
    totalPages: 1,
  };

  beforeEach(async () => {
    accountsService = {
      getAll: jest.fn(),
    };

    transactionsService = {
      getStatement: jest.fn(),
    };

    accountsService.getAll.mockReturnValue(of(mockAccounts));
    transactionsService.getStatement.mockReturnValue(of(mockStatement));

    await TestBed.configureTestingModule({
      imports: [AccountsList],
      providers: [
        provideRouter([]),
        {
          provide: AccountsService,
          useValue: accountsService,
        },
        {
          provide: TransactionsService,
          useValue: transactionsService,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AccountsList);
    component = fixture.componentInstance;
  });

  it('deve criar o componente', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
  });

  it('deve carregar as contas ao inicializar', () => {
    fixture.detectChanges();

    expect(accountsService.getAll).toHaveBeenCalledTimes(1);
    expect(component.accounts).toEqual(mockAccounts);
    expect(component.loading).toBe(false);
  });

  it('deve selecionar a primeira conta e carregar seu extrato', () => {
    fixture.detectChanges();

    expect(component.selectedAccountId).toBe('account-1');
    expect(transactionsService.getStatement).toHaveBeenCalledWith(
      'account-1',
      1,
      3,
    );
    expect(component.recentStatement).toEqual(mockStatement);
    expect(component.loadingRecent).toBe(false);
  });

  it('deve calcular o saldo total das contas', () => {
    fixture.detectChanges();

    expect(component.totalBalance).toBe(4000);
  });

  it('deve retornar a conta selecionada', () => {
    fixture.detectChanges();

    expect(component.selectedAccount).toEqual(mockAccounts[0]);
  });

  it('deve retornar as transações recentes', () => {
    fixture.detectChanges();

    expect(component.recentTransactions).toEqual(mockStatement.items);
  });

  it('deve permitir selecionar outra conta', () => {
    fixture.detectChanges();

    transactionsService.getStatement.mockClear();
    transactionsService.getStatement.mockReturnValue(of(mockStatement));

    component.selectAccount('account-2');

    expect(component.selectedAccountId).toBe('account-2');
    expect(transactionsService.getStatement).toHaveBeenCalledWith(
      'account-2',
      1,
      3,
    );
    expect(component.recentStatement).toEqual(mockStatement);
    expect(component.loadingRecent).toBe(false);
  });

  it('deve tratar erro ao carregar as contas', () => {
    accountsService.getAll.mockReturnValue(
      throwError(() => new Error('Falha na API')),
    );

    fixture.detectChanges();

    expect(component.error).toBe(
      'Não foi possível carregar as contas. Verifique se a API está funcionando.',
    );
    expect(component.loading).toBe(false);
  });

  it('deve tratar lista de contas vazia', () => {
    accountsService.getAll.mockReturnValue(of([]));

    fixture.detectChanges();

    expect(component.accounts).toEqual([]);
    expect(component.selectedAccountId).toBe('');
    expect(transactionsService.getStatement).not.toHaveBeenCalled();
    expect(component.totalBalance).toBe(0);
    expect(component.selectedAccount).toBeUndefined();
    expect(component.recentTransactions).toEqual([]);
  });

  it('deve tratar erro ao carregar as movimentações recentes', () => {
    fixture.detectChanges();

    transactionsService.getStatement.mockReturnValue(
      throwError(() => new Error('Falha no extrato')),
    );

    component.selectAccount('account-1');

    expect(component.recentStatement).toBeNull();
    expect(component.recentError).toBe(
      'Não foi possível carregar as movimentações recentes.',
    );
    expect(component.loadingRecent).toBe(false);
  });
});