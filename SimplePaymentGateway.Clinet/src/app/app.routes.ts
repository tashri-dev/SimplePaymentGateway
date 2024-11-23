import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component')
      .then(m => m.LoginComponent)
  },
  {
    path: 'payment',
    loadComponent: () => import('./features/payment/payment-form/payment-form.component')
      .then(m => m.PaymentFormComponent),
    canActivate: [AuthGuard]
  },
  {
    path: '',
    redirectTo: 'payment',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: 'payment'
  }
];
