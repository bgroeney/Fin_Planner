import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/auth';

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/login',
            name: 'login',
            component: () => import('../views/LoginView.vue')
        },
        {
            path: '/',
            component: () => import('../components/MainLayout.vue'),
            meta: { requiresAuth: true },
            children: [
                {
                    path: '',
                    name: 'dashboard',
                    component: () => import('../views/DashboardView.vue')
                },
                {
                    path: 'portfolios',
                    name: 'portfolios',
                    component: () => import('../views/PortfolioListView.vue')
                },
                {
                    path: 'portfolio/:id',
                    name: 'portfolio-detail',
                    component: () => import('../views/PortfolioDetailView.vue')
                },
                {
                    path: 'import',
                    name: 'import',
                    component: () => import('../views/ImportView.vue')
                },
                {
                    path: 'decisions',
                    name: 'decisions',
                    component: () => import('../views/DecisionsView.vue')
                },
                {
                    path: 'properties',
                    name: 'properties',
                    component: () => import('../views/PropertyHubView.vue')
                },
                {
                    path: 'acquisitions',
                    name: 'acquisitions',
                    component: () => import('../views/AcquisitionPlannerView.vue')
                },
                {
                    path: 'reports',
                    name: 'reports',
                    component: () => import('../views/ReportsView.vue')
                },
                {
                    path: 'admin',
                    name: 'admin',
                    component: () => import('../views/AdminSettings.vue')
                }
            ]
        }
    ]
});

router.beforeEach((to, from, next) => {
    const authStore = useAuthStore();

    if (to.meta.requiresAuth && !authStore.isAuthenticated) {
        next({ name: 'login' });
    } else if (to.name === 'login' && authStore.isAuthenticated) {
        next({ name: 'dashboard' });
    } else {
        next();
    }
});

export default router;
