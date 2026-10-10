import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { jest } from '@jest/globals';

import { TransactionForm } from './transaction-form';
import { AccountsService } from '../../../../core/services/Account/accounts';
import { TransactionsService } from '../../../../core/services/Transaction/transactions';
import { ToastService } from '../../../../core/services/Toast/toast';
import { Account } from '../../../../core/models/account.model';

describe('TransactionForm', () => {
    let component: TransactionForm;
    let fixture: ComponentFixture<TransactionForm>;

    let accountsService: {
        getAll: jest.Mock;
    };

    let transactionsService: {
        create: jest.Mock;
    };

    let toastService: {
        success: jest.Mock;
        error: jest.Mock;
        warning: jest.Mock;
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

    beforeEach(async () => {
        accountsService = {
            getAll: jest.fn().mockReturnValue(of(mockAccounts)),
        };

        transactionsService = {
            create: jest.fn().mockReturnValue(of({})),
        };

        toastService = {
            success: jest.fn(),
            error: jest.fn(),
            warning: jest.fn(),
        };

        await TestBed.configureTestingModule({
            imports: [TransactionForm],
            providers: [
                provideRouter([]),
                { provide: AccountsService, useValue: accountsService },
                { provide: TransactionsService, useValue: transactionsService },
                { provide: ToastService, useValue: toastService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(TransactionForm);
        component = fixture.componentInstance;
    });

    it('deve criar o componente', () => {
        fixture.detectChanges();

        expect(component).toBeTruthy();
    });

    it('deve carregar as contas e selecionar a primeira', () => {
        fixture.detectChanges();

        expect(accountsService.getAll).toHaveBeenCalledTimes(1);
        expect(component.accounts).toEqual(mockAccounts);
        expect(component.accountId).toBe('account-1');
        expect(component.loadingAccounts).toBe(false);
    });

    it('deve tratar erro ao carregar as contas', () => {
        accountsService.getAll.mockReturnValue(
            throwError(() => new Error('Falha ao carregar contas')),
        );

        fixture.detectChanges();

        expect(component.loadingAccounts).toBe(false);
        expect(toastService.error).toHaveBeenCalledWith(
            'Não foi possível carregar as contas.',
        );
    });

    it('deve avisar quando nenhuma conta estiver selecionada', () => {
        fixture.detectChanges();
        component.accountId = '';

        component.submit();

        expect(toastService.warning).toHaveBeenCalledWith(
            'Selecione uma conta.',
        );
        expect(transactionsService.create).not.toHaveBeenCalled();
    });

    it.each([null, 0, -10, Number.NaN, Number.POSITIVE_INFINITY])(
        'deve rejeitar valor inválido: %s',
        (amount) => {
            fixture.detectChanges();
            component.accountId = 'account-1';
            component.amount = amount;

            component.submit();

            expect(toastService.warning).toHaveBeenCalledWith(
                'Informe um valor maior que zero.',
            );
            expect(transactionsService.create).not.toHaveBeenCalled();
        },
    );

    it('deve registrar uma transação com sucesso', () => {
        fixture.detectChanges();

        component.accountId = 'account-1';
        component.type = 'CREDIT';
        component.amount = 250;

        component.submit();

        expect(transactionsService.create).toHaveBeenCalledTimes(1);

        const transaction = transactionsService.create.mock.calls[0][0] as {
            eventId: string;
            accountId: string;
            type: 'CREDIT' | 'DEBIT';
            amount: number;
            occurredAt: string;
        };

        expect(transaction).toEqual(
            expect.objectContaining({
                accountId: 'account-1',
                type: 'CREDIT',
                amount: 250,
            }),
        );
        expect(transaction.eventId).toEqual(expect.any(String));
        expect(Number.isNaN(Date.parse(transaction.occurredAt))).toBe(false);

        expect(toastService.success).toHaveBeenCalledWith(
            'Transação registrada com sucesso!',
        );
        expect(component.amount).toBeNull();
        expect(component.submitting).toBe(false);
    });

    it('deve registrar uma transação de débito', () => {
        fixture.detectChanges();

        component.accountId = 'account-2';
        component.type = 'DEBIT';
        component.amount = 100;

        component.submit();

        expect(transactionsService.create).toHaveBeenCalledWith(
            expect.objectContaining({
                accountId: 'account-2',
                type: 'DEBIT',
                amount: 100,
            }),
        );
    });

    it('deve exibir a mensagem de erro retornada pela API em detail', () => {
        fixture.detectChanges();

        transactionsService.create.mockReturnValue(
            throwError(() => ({
                error: { detail: 'Saldo insuficiente' },
            })),
        );

        component.accountId = 'account-1';
        component.amount = 200;

        component.submit();

        expect(toastService.error).toHaveBeenCalledWith(
            'Saldo insuficiente',
        );
        expect(component.submitting).toBe(false);
    });

    it('deve utilizar a mensagem message quando detail não estiver disponível', () => {
        fixture.detectChanges();

        transactionsService.create.mockReturnValue(
            throwError(() => ({
                error: { message: 'Conta bloqueada' },
            })),
        );

        component.accountId = 'account-1';
        component.amount = 200;

        component.submit();

        expect(toastService.error).toHaveBeenCalledWith(
            'Conta bloqueada',
        );
        expect(component.submitting).toBe(false);
    });

    it('deve exibir mensagem padrão quando a API não fornecer mensagem válida', () => {
        fixture.detectChanges();

        transactionsService.create.mockReturnValue(
            throwError(() => ({
                error: { detail: '   ', message: 123 },
            })),
        );

        component.accountId = 'account-1';
        component.amount = 200;

        component.submit();

        expect(toastService.error).toHaveBeenCalledWith(
            'Não foi possível registrar a transação. Verifique os dados e tente novamente.',
        );
        expect(component.submitting).toBe(false);
    });
});