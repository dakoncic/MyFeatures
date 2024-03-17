import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/layout.component';

export const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () => import('./home/home.component').then(m => m.HomeComponent)
      },
      // {
      //   path: 'home',
      //   loadComponent: () => import('./home/home.component').then(m => m.HomeComponent)
      // },
      {
        path: '**',
        redirectTo: ''
      }
    ]
  }
];
