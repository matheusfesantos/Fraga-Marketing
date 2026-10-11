import { Component, OnInit, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { Account } from '../../../../core/models/account.model';
import { AccountsService } from '../../../../core/services/Account/accounts';
import { TransactionsService } from '../../../../core/services/Transaction/transactions';
import { ToastService } from '../../../../core/services/Toast/toast';

type TransactionKind = 'CREDIT' | 'DEBIT';

const DEFAULT_ERROR_MESSAGE =
  'Não foi possível registrar a transação. Verifique os dados e tente novamente.';

/**
 * O backend persiste valores em numeric(18,2) e rejeita mais de duas casas decimais.
 * Valida o mesmo no front para dar feedback imediato (o backend continua sendo a fonte da verdade).
 */
export function amountFormat(control: AbstractControl): ValidationErrors | null {
  const value = control.value;

  if (typeof value !== 'number') {
    return null;
  }

  if (!Number.isFinite(value)) {
    return { invalidNumber: true };
  }

  const cents = value * 100;

  return Math.abs(cents - Math.round(cents)) < 1e-6 ? null : { maxDecimals: true };
}

@Component({
  selector: 'app-transaction-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './transaction-form.html',
  styleUrl: './transaction-form.scss',
})
export class TransactionForm implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly accountsService = inject(AccountsService);
  private readonly transactionsService = inject(TransactionsService);
  private readonly toastService = inject(ToastService);

  readonly form = this.fb.group({
    accountId: this.fb.control('', Validators.required),
    type: this.fb.control<TransactionKind>('CREDIT', Validators.required),
    amount: this.fb.control<number | null>(null, [
      Validators.required,
      Validators.min(0.01),
      amountFormat,
    ]),
  });

  accounts: Account[] = [];
  loadingAccounts = true;
  submitting = false;

  /**
   * Identificador do evento em andamento. É mantido entre tentativas com os mesmos dados
   * (ex.: falha de rede), para que um reenvio seja idempotente no backend.
   */
  private pendingEventId: string | null = null;

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.pendingEventId = null;
    });
  }

  ngOnInit(): void {
    this.updateFormAvailability();
    this.loadAccounts();
  }

  submit(): void {
    if (this.form.disabled) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toastService.warning('Corrija os campos destacados.');
      return;
    }

    const { accountId, type, amount } = this.form.getRawValue();

    this.pendingEventId ??= crypto.randomUUID();

    const transaction = {
      eventId: this.pendingEventId,
      accountId,
      type,
      amount: amount as number,
      occurredAt: new Date().toISOString(),
    };

    this.submitting = true;
    this.updateFormAvailability();

    this.transactionsService
      .create(transaction)
      .pipe(
        finalize(() => {
          this.submitting = false;
          this.updateFormAvailability();
        }),
      )
      .subscribe({
        next: () => {
          this.pendingEventId = null;
          this.form.controls.amount.reset(null);
          this.toastService.success('Transação registrada com sucesso!');
          this.loadAccounts();
        },
        error: (err) => {
          this.toastService.error(this.resolveErrorMessage(err));
        },
      });
  }

  private loadAccounts(): void {
    this.loadingAccounts = true;
    this.updateFormAvailability();

    this.accountsService.getAll().subscribe({
      next: (accounts) => {
        const current = this.form.controls.accountId.value;
        const selected = accounts.some((account) => account.id === current)
          ? current
          : (accounts[0]?.id ?? '');

        this.accounts = accounts;
        this.form.controls.accountId.setValue(selected, { emitEvent: false });
        this.loadingAccounts = false;
        this.updateFormAvailability();
      },
      error: () => {
        this.toastService.error('Não foi possível carregar as contas.');
        this.loadingAccounts = false;
        this.updateFormAvailability();
      },
    });
  }

  private updateFormAvailability(): void {
    if (this.loadingAccounts || this.submitting || this.accounts.length === 0) {
      this.form.disable({ emitEvent: false });
    } else {
      this.form.enable({ emitEvent: false });
    }
  }

  private resolveErrorMessage(err: unknown): string {
    const body = (err as { error?: { detail?: unknown; message?: unknown } } | null)?.error;
    const apiMessage = body?.detail ?? body?.message;

    return typeof apiMessage === 'string' && apiMessage.trim()
      ? apiMessage
      : DEFAULT_ERROR_MESSAGE;
  }
}