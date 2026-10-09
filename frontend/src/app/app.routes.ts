import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'zones' },
  { path: 'zones', loadComponent: () => import('./features/zones/zones-page').then(m => m.ZonesPage) },
  { path: 'move', pathMatch: 'full', redirectTo: () => 'move/' + (localStorage.getItem('eurocar.lastZone') ?? 'S1') },
  { path: 'move/:zone', loadComponent: () => import('./features/move/move-page').then(m => m.MovePage) },
  { path: 'keys', pathMatch: 'full', redirectTo: 'keys/1' },
  { path: 'keys/:cabinet', loadComponent: () => import('./features/keys/keys-page').then(m => m.KeysPage) },
  { path: 'log', loadComponent: () => import('./features/log/log-page').then(m => m.LogPage) },
  { path: '**', redirectTo: 'zones' },
];
