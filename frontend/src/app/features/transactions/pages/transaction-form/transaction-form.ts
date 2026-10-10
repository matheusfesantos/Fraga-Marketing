import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Account } from '../../../../core/models/account.model';
import { RouterLink } from '@angular/router';
import { AccountsService } from '../../../../core/services/Account/accounts';
import { TransactionsService } from '../../../../core/services/Transaction/transactions';
import { ToastService } from '../../../../core/services/Toast/toast';

@Component({
  selector: 'app-transaction-form',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, RouterLink],
  templateUrl: './transaction-form.html',
  styleUrl: './transaction-form.scss',
})
export class TransactionForm implements OnInit {
  private readonly accountsService = inject(AccountsService);
  private readonly transactionsService = inject(TransactionsService);
  private readonly toastService = inject(ToastService);

  accounts: Account[] = [];
  accountId = '';
  type: 'CREDIT' | 'DEBIT' = 'CREDIT';
  amount: number | null = null;

  loadingAccounts = true;
  submitting = false;

  ngOnInit(): void {
    this.accountsService.getAll().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.accountId = accounts[0]?.id ?? '';
        this.loadingAccounts = false;
      },
      error: () => {
        this.toastService.error('Não foi possível carregar as contas.');
        this.loadingAccounts = false;
      },
    });
  }

  submit(): void {
    if (!this.accountId) {
      this.toastService.warning('Selecione uma conta.');
      return;
    }

    if (this.amount === null || !Number.isFinite(this.amount) || this.amount <= 0) {
      this.toastService.warning('Informe um valor maior que zero.');
      return;
    }

    const transaction = {
      eventId: crypto.randomUUID(),
      accountId: this.accountId,
      type: this.type,
      amount: this.amount,
      occurredAt: new Date().toISOString(),
    };

    this.submitting = true;
    this.transactionsService.create(transaction).subscribe({
      next: () => {
        this.toastService.success('Transação registrada com sucesso!');
        this.amount = null;
        this.submitting = false;
      },
      error: (err) => {
        const apiMessage = err?.error?.detail ?? err?.error?.message;
        this.toastService.error(
          typeof apiMessage === 'string' && apiMessage.trim()
            ? apiMessage
            : 'Não foi possível registrar a transação. Verifique os dados e tente novamente.',
        );
        this.submitting = false;
      },
    });
  }
}