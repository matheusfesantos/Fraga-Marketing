import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Account } from '../../../../core/models/account.model';
import { AccountsService } from '../../../../core/services/Account/accounts';
import { Transaction, TransactionPage, TransactionsService } from '../../../../core/services/Transaction/transactions';

@Component({
  selector: 'app-accounts-list',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, RouterLink],
  templateUrl: './accounts-list.html',
  styleUrl: './accounts-list.scss',
})
export class AccountsList implements OnInit {
  private readonly accountsService = inject(AccountsService);
  private readonly transactionsService = inject(TransactionsService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  accounts: Account[] = [];
  selectedAccountId = '';
  recentStatement: TransactionPage | null = null;
  loading = true;
  loadingRecent = false;
  error = '';
  recentError = '';

  ngOnInit(): void {
    this.accountsService.getAll().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.loading = false;

        if (accounts.length > 0) {
          this.selectAccount(accounts[0].id);
        }

        this.changeDetector.markForCheck();
      },
      error: () => {
        this.error =
          'Não foi possível carregar as contas. Verifique se a API está funcionando.';
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  selectAccount(accountId: string): void {
    this.selectedAccountId = accountId;
    this.loadingRecent = true;
    this.recentError = '';

    this.transactionsService.getStatement(accountId, 1, 3).subscribe({
      next: (statement) => {
        this.recentStatement = statement;
        this.loadingRecent = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.recentStatement = null;
        this.recentError = 'Não foi possível carregar as movimentações recentes.';
        this.loadingRecent = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  get totalBalance(): number {
    return this.accounts.reduce(
      (total, account) => total + account.balance,
      0,
    );
  }

  get selectedAccount(): Account | undefined {
    return this.accounts.find((account) => account.id === this.selectedAccountId);
  }

  get recentTransactions(): Transaction[] {
    return this.recentStatement?.items ?? [];
  }
}