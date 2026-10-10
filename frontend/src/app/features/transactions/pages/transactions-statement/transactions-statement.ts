import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Account } from '../../../../core/models/account.model';
import { AccountsService } from '../../../../core/services/Account/accounts';
import { Transaction, TransactionPage, TransactionsService } from '../../../../core/services/Transaction/transactions';

@Component({
  selector: 'app-transactions-statement',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, FormsModule],
  templateUrl: './transactions-statement.html',
  styleUrl: './transactions-statement.scss',
})
export class TransactionsStatement implements OnInit {
  private readonly accountsService = inject(AccountsService);
  private readonly transactionsService = inject(TransactionsService);
  private readonly route = inject(ActivatedRoute);
  private readonly changeDetector = inject(ChangeDetectorRef);

  accounts: Account[] = [];
  selectedAccountId = '';
  statement: TransactionPage | null = null;
  loading = true;
  error = '';
  page = 1;
  readonly pageSize = 10;

  ngOnInit(): void {
    this.accountsService.getAll().subscribe({
      next: (accounts) => {
        this.accounts = accounts;

        if (accounts.length > 0) {
          const requestedAccountId = this.route.snapshot.queryParamMap.get('accountId');
          this.selectedAccountId =
            accounts.find((account) => account.id === requestedAccountId)?.id ??
            accounts[0].id;
          this.loadStatement();
        } else {
          this.loading = false;
        }
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.error = 'Não foi possível carregar as contas.';
        this.loading = false;
        this.changeDetector.markForCheck();
      },
    });
  }

  loadStatement(): void {
    if (!this.selectedAccountId) return;

    this.loading = true;
    this.error = '';

    this.transactionsService
      .getStatement(this.selectedAccountId, this.page, this.pageSize)
      .subscribe({
        next: (result) => {
          this.statement = result;
          this.loading = false;
          this.changeDetector.markForCheck();
        },
        error: () => {
          this.error = 'Não foi possível carregar o extrato.';
          this.loading = false;
          this.changeDetector.markForCheck();
        },
      });
  }

  onAccountChange(accountId: string): void {
    this.selectedAccountId = accountId;
    this.page = 1;
    this.loadStatement();
  }

  changePage(page: number): void {
    if (!this.statement || page < 1 || page > this.statement.totalPages) {
      return;
    }

    this.page = page;
    this.loadStatement();
  }

  get transactions(): Transaction[] {
    return this.statement?.items ?? [];
  }

  get runningBalance(): number {
    const account = this.accounts.find(
      (item) => item.id === this.selectedAccountId,
    );

    return account?.balance ?? 0;
  }
}