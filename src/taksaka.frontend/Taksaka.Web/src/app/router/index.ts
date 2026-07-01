import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: () => import('../../modules/dashboard/pages/DashboardPage.vue')
    },
    {
      path: '/queue',
      name: 'queue',
      component: () => import('../../modules/queue/pages/QueuePage.vue')
    },
    {
      path: '/workers',
      name: 'workers',
      component: () => import('../../modules/workers/pages/WorkersPage.vue')
    },
    {
      path: '/scheduler',
      name: 'scheduler',
      component: () => import('../../modules/scheduler/pages/SchedulerPage.vue')
    },
    {
      path: '/alerts',
      name: 'alerts',
      component: () => import('../../modules/alerts/pages/AlertsPage.vue')
    },
    {
      path: '/health',
      name: 'health',
      component: () => import('../../modules/health/pages/HealthPage.vue')
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('../../modules/settings/pages/SettingsPage.vue')
    }
  ]
})

export default router
