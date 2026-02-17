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
                    path: 'holdings',
                    name: 'holdings',
                    component: () => import('../views/PortfolioDetailView.vue')
                },
                {
                    // Legacy route - redirects to holdings
                    path: 'portfolios',
                    redirect: '/holdings'
                },
                {
                    // Legacy route - redirects to holdings
                    path: 'portfolio/:id',
                    redirect: '/holdings'
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
                    path: 'goals',
                    name: 'goals',
                    component: () => import('../views/GoalsView.vue')
                },
                {
                    path: 'rebalancing',
                    name: 'rebalancing',
                    component: () => import('../views/RebalancingView.vue')
                },
                {
                    path: 'dividends',
                    name: 'dividends',
                    component: () => import('../views/DividendCalendarView.vue')
                },
                {
                    path: 'audit',
                    name: 'audit',
                    component: () => import('../views/AuditLogView.vue')
                },
                {
                    path: 'entities',
                    name: 'entities',
                    component: () => import('../views/EntitiesView.vue')
                },
                {
                    path: 'entities/person/:id',
                    name: 'person-detail',
                    component: () => import('../views/entities/PersonDetailView.vue')
                },
                {
                    path: 'entities/trust/:id',
                    name: 'trust-detail',
                    component: () => import('../views/entities/TrustDetailView.vue')
                },
                {
                    path: 'entities/company/:id',
                    name: 'company-detail',
                    component: () => import('../views/entities/CompanyDetailView.vue')
                },
                {
                    path: 'tax-optimization',
                    name: 'tax-optimization',
                    component: () => import('../views/TaxOptimizerView.vue')
                },
                {
                    path: 'financial-independence',
                    name: 'financial-independence',
                    component: () => import('../views/FinancialIndependenceView.vue')
                },
                {
                    path: 'net-worth',
                    name: 'net-worth',
                    component: () => import('../views/NetWorthView.vue')
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
