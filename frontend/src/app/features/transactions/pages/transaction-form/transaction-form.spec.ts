import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { jest } from '@jest/globals';

import { TransactionForm } from './transaction-form';
import { AccountsService } from '../../../../core/services/Account/accounts';
import { TransactionsService } from '../../../../core/services/Transaction/transactions';
import { ToastService } from '../../../../core/services/Toast/toast';
import { Account } from '../../../../core/models/account.model';

type SentTransaction = {
    eventId: string;
    accountId: string;
    type: 'CREDIT' | 'DEBIT';
    amount: number;
    occurredAt: string;
};

describe('TransactionForm', () => {
    let component: TransactionForm;
    let fixture: ComponentFixture<TransactionForm>;

    let accountsService: { getAll: jest.Mock };
    let transactionsService: { create: jest.Mock };
    let toastService: {
        success: jest.Mock;
        error: jest.Mock;
        warning: jest.Mock;
    };

    const mockAccounts: Account[] = [
        { id: 'account-1', name: 'Conta Corrente', balance: 1500 },
        { id: 'account-2', name: 'Conta Poupança', balance: 2500 },
    ];

    const sentTransaction = (call: number): SentTransaction =>
        transactionsService.create.mock.calls[call][0] as SentTransaction;

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
        expect(component.form.controls.accountId.value).toBe('account-1');
        expect(component.loadingAccounts).toBe(false);
        expect(component.form.enabled).toBe(true);
    });

    it('deve tratar erro ao carregar as contas', () => {
        accountsService.getAll.mockReturnValue(
            throwError(() => new Error('Falha ao carregar contas')),
        );

        fixture.detectChanges();

        expect(component.loadingAccounts).toBe(false);
        expect(component.form.disabled).toBe(true);
        expect(toastService.error).toHaveBeenCalledWith(
            'Não foi possível carregar as contas.',
        );
    });

    it('não deve enviar quando não houver conta disponível', () => {
        accountsService.getAll.mockReturnValue(of([]));

        fixture.detectChanges();
        component.submit();

        expect(component.form.disabled).toBe(true);
        expect(transactionsService.create).not.toHaveBeenCalled();
    });

    it('deve avisar e não enviar quando nenhuma conta estiver selecionada', () => {
        fixture.detectChanges();
        component.form.controls.accountId.setValue('');
        component.form.controls.amount.setValue(100);

        component.submit();

        expect(component.form.controls.accountId.hasError('required')).toBe(true);
        expect(toastService.warning).toHaveBeenCalledWith(
            'Corrija os campos destacados.',
        );
        expect(transactionsService.create).not.toHaveBeenCalled();
    });

    it.each([
        [null, 'required'],
        [0, 'min'],
        [-10, 'min'],
        [10.555, 'maxDecimals'],
        [Number.NaN, 'invalidNumber'],
        [Number.POSITIVE_INFINITY, 'invalidNumber'],
    ])('deve rejeitar valor inválido %s (%s)', (amount, errorKey) => {
        fixture.detectChanges();
        component.form.controls.amount.setValue(amount);

        component.submit();

        expect(component.form.controls.amount.hasError(errorKey)).toBe(true);
        expect(component.form.controls.amount.touched).toBe(true);
        expect(toastService.warning).toHaveBeenCalledWith(
            'Corrija os campos destacados.',
        );
        expect(transactionsService.create).not.toHaveBeenCalled();
    });

    it('deve aceitar valor com até duas casas decimais', () => {
        fixture.detectChanges();
        component.form.controls.amount.setValue(10.55);

        expect(component.form.controls.amount.valid).toBe(true);
    });

    it('deve registrar uma transação de crédito com sucesso', () => {
        fixture.detectChanges();
        component.form.patchValue({
            accountId: 'account-1',
            type: 'CREDIT',
            amount: 250,
        });

        component.submit();

        expect(transactionsService.create).toHaveBeenCalledTimes(1);

        const transaction = sentTransaction(0);

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
        expect(component.form.controls.amount.value).toBeNull();
        expect(component.submitting).toBe(false);
        expect(component.form.enabled).toBe(true);
    });

    it('deve recarregar as contas após o sucesso para refletir o saldo do backend', () => {
        fixture.detectChanges();
        component.form.controls.amount.setValue(250);

        component.submit();

        expect(accountsService.getAll).toHaveBeenCalledTimes(2);
        expect(component.form.controls.accountId.value).toBe('account-1');
    });

    it('deve registrar uma transação de débito', () => {
        fixture.detectChanges();
        component.form.patchValue({
            accountId: 'account-2',
            type: 'DEBIT',
            amount: 100,
        });

        component.submit();

        expect(transactionsService.create).toHaveBeenCalledWith(
            expect.objectContaining({
                accountId: 'account-2',
                type: 'DEBIT',
                amount: 100,
            }),
        );
    });

    it('deve reutilizar o eventId ao reenviar os mesmos dados após uma falha', () => {
        fixture.detectChanges();
        transactionsService.create.mockReturnValue(
            throwError(() => ({ status: 0 })),
        );
        component.form.controls.amount.setValue(200);

        component.submit();
        component.submit();

        expect(transactionsService.create).toHaveBeenCalledTimes(2);
        expect(sentTransaction(1).eventId).toBe(sentTransaction(0).eventId);
    });

    it('deve gerar um novo eventId quando os dados forem alterados', () => {
        fixture.detectChanges();
        transactionsService.create.mockReturnValue(
            throwError(() => ({ status: 0 })),
        );
        component.form.controls.amount.setValue(200);

        component.submit();
        component.form.controls.amount.setValue(300);
        component.submit();

        expect(sentTransaction(1).eventId).not.toBe(sentTransaction(0).eventId);
    });

    it('deve gerar um novo eventId após um envio bem-sucedido', () => {
        fixture.detectChanges();
        component.form.controls.amount.setValue(200);
        component.submit();

        component.form.controls.amount.setValue(200);
        component.submit();

        expect(sentTransaction(1).eventId).not.toBe(sentTransaction(0).eventId);
    });

    it('deve exibir a mensagem de erro retornada pela API em detail', () => {
        fixture.detectChanges();
        transactionsService.create.mockReturnValue(
            throwError(() => ({ error: { detail: 'Saldo insuficiente' } })),
        );
        component.form.controls.amount.setValue(200);

        component.submit();

        expect(toastService.error).toHaveBeenCalledWith('Saldo insuficiente');
        expect(component.submitting).toBe(false);
        expect(component.form.enabled).toBe(true);
    });

    it('deve utilizar a mensagem message quando detail não estiver disponível', () => {
        fixture.detectChanges();
        transactionsService.create.mockReturnValue(
            throwError(() => ({ error: { message: 'Conta bloqueada' } })),
        );
        component.form.controls.amount.setValue(200);

        component.submit();

        expect(toastService.error).toHaveBeenCalledWith('Conta bloqueada');
        expect(component.submitting).toBe(false);
    });

    it('deve exibir mensagem padrão quando a API não fornecer mensagem válida', () => {
        fixture.detectChanges();
        transactionsService.create.mockReturnValue(
            throwError(() => ({ error: { detail: '   ', message: 123 } })),
        );
        component.form.controls.amount.setValue(200);

        component.submit();

        expect(toastService.error).toHaveBeenCalledWith(
            'Não foi possível registrar a transação. Verifique os dados e tente novamente.',
        );
        expect(component.submitting).toBe(false);
    });

    it('deve exibir erro inline do valor após o campo ser tocado', () => {
        fixture.detectChanges();
        const amount = component.form.controls.amount;

        expect(component.showErrors(amount)).toBe(false);

        amount.markAsTouched();

        expect(component.showErrors(amount)).toBe(true);
    });
});