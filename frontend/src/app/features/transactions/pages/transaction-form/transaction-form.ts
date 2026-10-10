import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AccountsService } from '../../../../core/services/accounts';
import { Account } from '../../../../core/models/account.model';
import { TransactionsService } from '../../../../core/services/transactions';
import { RouterLink } from '@angular/router';

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

  accounts: Account[] = [];
  accountId = '';
  type: 'CREDIT' | 'DEBIT' = 'CREDIT';
  amount: number | null = null;

  loadingAccounts = true;
  submitting = false;
  error = '';
  success = '';

  ngOnInit(): void {
    this.accountsService.getAll().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.accountId = accounts[0]?.id ?? '';
        this.loadingAccounts = false;
      },
      error: () => {
        this.error = 'Não foi possível carregar as contas.';
        this.loadingAccounts = false;
      },
    });
  }

  submit(): void {
    this.error = '';
    this.success = '';

    if (!this.accountId) {
      this.error = 'Selecione uma conta.';
      return;
    }

    if (this.amount === null || !Number.isFinite(this.amount) || this.amount <= 0) {
      this.error = 'Informe um valor maior que zero.';
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
        this.success = 'Transação enviada com sucesso!';
        this.amount = null;
        this.submitting = false;
      },
      error: (err) => {
        const message = err?.error?.message;

        this.error =
          typeof message === 'string'
            ? message
            : 'Não foi possível registrar a transação. Verifique os dados e tente novamente.';

        this.submitting = false;
      },
    });
  }
}