import { Routes } from '@angular/router';

// Cada pantalla se carga solo cuando el usuario entra a ella.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  {
    path: 'tasks',
    title: 'Tareas · Gestor de tareas',
    loadComponent: () => import('./features/tasks/pages/tasks-page/tasks-page').then((m) => m.TasksPage),
  },
  {
    path: 'users',
    title: 'Usuarios · Gestor de tareas',
    loadComponent: () => import('./features/users/pages/users-page/users-page').then((m) => m.UsersPage),
  },
  { path: '**', redirectTo: 'tasks' },
];
