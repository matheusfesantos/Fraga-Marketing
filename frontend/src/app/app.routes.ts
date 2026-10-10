
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'contas',
    pathMatch: 'full',
  },
  {
    path: 'contas',
    loadComponent: () =>
      import('./features/accounts/pages/accounts-list/accounts-list')
        .then((m) => m.AccountsList),
  },
  {
    path: 'transacoes',
    loadComponent: () =>
      import('./features/transactions/pages/transactions-statement/transactions-statement')
        .then((m) => m.TransactionsStatement),
  },
  {
    path: 'transacoes/nova',
    loadComponent: () =>
      import('./features/transactions/pages/transaction-form/transaction-form')
        .then((m) => m.TransactionForm),
  },
  {
    path: '**',
    redirectTo: 'contas',
  },
];
