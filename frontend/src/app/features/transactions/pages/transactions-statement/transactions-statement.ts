import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute } from '@angular/router';
import { Account } from '../../../../core/models/account.model';
import { AccountsService } from '../../../../core/services/Account/accounts';
import { Transaction, TransactionPage, TransactionsService } from '../../../../core/services/Transaction/transactions';

const DEFAULT_ERROR_MESSAGE =
  'Não foi possível carregar os dados. Verifique a conexão e tente novamente.';

@Component({
  selector: 'app-transactions-statement',
  standalone: true,
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatPaginatorModule,
    MatSelectModule,
  ],
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
      error: (err) => {
        const apiMessage = err?.error?.detail ?? err?.error?.message;
        this.error =
          typeof apiMessage === 'string' && apiMessage.trim()
            ? apiMessage
            : DEFAULT_ERROR_MESSAGE;
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
        error: (err) => {
          const apiMessage = err?.error?.detail ?? err?.error?.message;
          this.error =
            typeof apiMessage === 'string' && apiMessage.trim()
              ? apiMessage
              : DEFAULT_ERROR_MESSAGE;
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

  onPageChange(event: PageEvent): void {
    this.changePage(event.pageIndex + 1);
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