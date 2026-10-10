import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { jest } from '@jest/globals';

import { TransactionsStatement } from './transactions-statement';
import { AccountsService } from '../../../../core/services/Account/accounts';
import { TransactionsService } from '../../../../core/services/Transaction/transactions';
import { Account } from '../../../../core/models/account.model';
import { TransactionPage } from '../../../../core/services/Transaction/transactions';

describe('TransactionsStatement', () => {
  let component: TransactionsStatement;
  let fixture: ComponentFixture<TransactionsStatement>;

  let accountsService: {
    getAll: jest.Mock;
  };

  let transactionsService: {
    getStatement: jest.Mock;
  };

  let requestedAccountId: string | null;

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
    pageSize: 10,
    totalItems: 1,
    totalPages: 2,
  };

  beforeEach(async () => {
    requestedAccountId = null;

    accountsService = {
      getAll: jest.fn().mockReturnValue(of(mockAccounts)),
    };

    transactionsService = {
      getStatement: jest.fn().mockReturnValue(of(mockStatement)),
    };

    await TestBed.configureTestingModule({
      imports: [TransactionsStatement],
      providers: [
        {
          provide: AccountsService,
          useValue: accountsService,
        },
        {
          provide: TransactionsService,
          useValue: transactionsService,
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              get queryParamMap() {
                return convertToParamMap({
                  accountId: requestedAccountId,
                });
              },
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TransactionsStatement);
    component = fixture.componentInstance;
  });

  it('deve criar o componente', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
  });

  it('deve carregar as contas e selecionar a primeira por padrão', () => {
    fixture.detectChanges();

    expect(accountsService.getAll).toHaveBeenCalledTimes(1);
    expect(component.accounts).toEqual(mockAccounts);
    expect(component.selectedAccountId).toBe('account-1');
    expect(component.loading).toBe(false);

    expect(transactionsService.getStatement).toHaveBeenCalledWith(
      'account-1',
      1,
      10,
    );
  });

  it('deve selecionar a conta informada na URL', () => {
    requestedAccountId = 'account-2';

    fixture.detectChanges();

    expect(component.selectedAccountId).toBe('account-2');
    expect(transactionsService.getStatement).toHaveBeenCalledWith(
      'account-2',
      1,
      10,
    );
  });

  it('deve selecionar a primeira conta se o ID da URL não existir', () => {
    requestedAccountId = 'account-inexistente';

    fixture.detectChanges();

    expect(component.selectedAccountId).toBe('account-1');
  });

  it('deve armazenar o extrato retornado pela API', () => {
    fixture.detectChanges();

    expect(component.statement).toEqual(mockStatement);
    expect(component.transactions).toEqual(mockStatement.items);
    expect(component.loading).toBe(false);
  });

  it('deve calcular o saldo da conta selecionada', () => {
    fixture.detectChanges();

    expect(component.runningBalance).toBe(1500);

    component.selectedAccountId = 'account-2';

    expect(component.runningBalance).toBe(2500);
  });

  it('deve retornar saldo zero quando não houver conta selecionada', () => {
    fixture.detectChanges();

    component.selectedAccountId = 'conta-inexistente';

    expect(component.runningBalance).toBe(0);
  });

  it('deve retornar uma lista vazia quando não houver extrato', () => {
    fixture.detectChanges();

    component.statement = null;

    expect(component.transactions).toEqual([]);
  });

  it('deve recarregar o extrato ao trocar de conta e voltar para a página 1', () => {
    fixture.detectChanges();

    transactionsService.getStatement.mockClear();
    component.page = 2;

    component.onAccountChange('account-2');

    expect(component.selectedAccountId).toBe('account-2');
    expect(component.page).toBe(1);
    expect(transactionsService.getStatement).toHaveBeenCalledWith(
      'account-2',
      1,
      10,
    );
  });

  it('deve mudar para uma página válida', () => {
    fixture.detectChanges();

    transactionsService.getStatement.mockClear();

    component.changePage(2);

    expect(component.page).toBe(2);
    expect(transactionsService.getStatement).toHaveBeenCalledWith(
      'account-1',
      2,
      10,
    );
  });

  it('não deve mudar para uma página menor que 1', () => {
    fixture.detectChanges();

    transactionsService.getStatement.mockClear();

    component.changePage(0);

    expect(component.page).toBe(1);
    expect(transactionsService.getStatement).not.toHaveBeenCalled();
  });

  it('não deve mudar para uma página maior que o total de páginas', () => {
    fixture.detectChanges();

    transactionsService.getStatement.mockClear();

    component.changePage(3);

    expect(component.page).toBe(1);
    expect(transactionsService.getStatement).not.toHaveBeenCalled();
  });

  it('não deve consultar o extrato se não houver conta selecionada', () => {
    fixture.detectChanges();

    transactionsService.getStatement.mockClear();
    component.selectedAccountId = '';

    component.loadStatement();

    expect(transactionsService.getStatement).not.toHaveBeenCalled();
  });

  it('deve tratar erro ao carregar as contas', () => {
    accountsService.getAll.mockReturnValue(
      throwError(() => new Error('Falha ao carregar contas')),
    );

    fixture.detectChanges();

    expect(component.error).toBe('Não foi possível carregar as contas.');
    expect(component.loading).toBe(false);
  });

  it('deve tratar erro ao carregar o extrato', () => {
    fixture.detectChanges();

    transactionsService.getStatement.mockReturnValue(
      throwError(() => new Error('Falha ao carregar extrato')),
    );

    component.loadStatement();

    expect(component.error).toBe('Não foi possível carregar o extrato.');
    expect(component.loading).toBe(false);
  });

  it('deve encerrar o carregamento quando não houver contas', () => {
    accountsService.getAll.mockReturnValue(of([]));

    fixture.detectChanges();

    expect(component.accounts).toEqual([]);
    expect(component.selectedAccountId).toBe('');
    expect(component.loading).toBe(false);
    expect(transactionsService.getStatement).not.toHaveBeenCalled();
  });
});