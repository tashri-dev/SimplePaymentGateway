import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Routes, RouterModule } from '@angular/router';
import { LoginComponent } from '../features/auth/login/login.component';
import { PaymentFormComponent } from '../features/payment/payment-form/payment-form.component';
import { AuthGuard } from '../core/guards/auth.guard';



const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'payment',
    component: PaymentFormComponent,
    canActivate: [AuthGuard]
  },
  {
    path: '',
    redirectTo: 'payment',
    pathMatch: 'full'
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
