import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { AccountsService } from '../../../../core/services/accounts';
import { Account } from '../../../../core/models/account.model';

@Component({
  selector: 'app-accounts-list',
  standalone: true,
  imports: [CommonModule, CurrencyPipe],
  templateUrl: './accounts-list.html',
  styleUrl: './accounts-list.scss',
})
export class AccountsList implements OnInit {
  private readonly accountsService = inject(AccountsService);

  accounts: Account[] = [];
  loading = true;
  error = '';

  ngOnInit(): void {
  this.accountsService.getAll().subscribe({
    next: (accounts) => {
      console.log('Contas recebidas:', accounts);
      this.accounts = accounts;
      console.log('Quantidade de contas:', this.accounts.length);
      this.loading = false;
    },
    error: (err) => {
      console.error('Erro ao carregar contas:', err);
      this.error = 'Não foi possível carregar as contas. Verifique se a API está funcionando.';
      this.loading = false;
    },
  });
}

  get totalBalance(): number {
    return this.accounts.reduce(
      (total, account) => total + account.balance,
      0,
    );
  }
}