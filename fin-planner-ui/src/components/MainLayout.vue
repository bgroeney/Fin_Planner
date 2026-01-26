<template>
  <div class="layout">
    <!-- Mobile Backdrop -->
    <div v-if="isMobileMenuOpen" class="sidebar-backdrop" @click="isMobileMenuOpen = false"></div>

    <!-- Sidebar -->
    <aside :class="['sidebar', { collapsed: isCollapsed, 'mobile-open': isMobileMenuOpen }]">
      <!-- ... sidebar content ... -->
      <div class="brand">
        <div class="logo-mark">FP</div>
        <transition name="fade">
          <div class="brand-text" v-if="!isCollapsed">
            <span class="brand-name">FinPlanner</span>
          </div>
        </transition>
        <!-- Mobile Close Button -->
        <button class="mobile-close md:hidden absolute right-4 top-4" @click="isMobileMenuOpen = false" v-if="isMobileMenuOpen">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
        </button>
      </div>

      <nav class="nav">
        <router-link to="/" class="nav-item" exact-active-class="active" title="Dashboard" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Dashboard</span>
        </router-link>

        <router-link to="/portfolios" class="nav-item" active-class="active" title="Portfolios" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21.21 15.89A10 10 0 1 1 8 2.83"/><path d="M22 12A10 10 0 0 0 12 2v10z"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Portfolios</span>
        </router-link>

        <router-link to="/import" class="nav-item" active-class="active" title="Import" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Import</span>
        </router-link>

        <router-link to="/decisions" class="nav-item" active-class="active" title="Decisions" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="9" y1="15" x2="15" y2="15"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Decisions</span>
        </router-link>

        <router-link to="/properties" class="nav-item" active-class="active" title="Properties" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Properties</span>
        </router-link>

        <router-link to="/reports" class="nav-item" active-class="active" title="Reports" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Reports</span>
        </router-link>

        <router-link to="/goals" class="nav-item" active-class="active" title="Goals" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><circle cx="12" cy="12" r="6"/><circle cx="12" cy="12" r="2"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Goals</span>
        </router-link>

        <router-link to="/dividends" class="nav-item" active-class="active" title="Dividends" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/><line x1="8" y1="14" x2="8" y2="14.01"/><line x1="12" y1="14" x2="12" y2="14.01"/><line x1="16" y1="14" x2="16" y2="14.01"/><line x1="8" y1="18" x2="8" y2="18.01"/><line x1="12" y1="18" x2="12" y2="18.01"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Dividends</span>
        </router-link>

        <router-link to="/audit" class="nav-item" active-class="active" title="Audit Log" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><line x1="10" y1="9" x2="8" y2="9"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Audit Log</span>
        </router-link>

        <div class="nav-divider" v-if="!isCollapsed"></div>
        <div class="nav-section-label" v-if="!isCollapsed">TAX PLANNING</div>

        <router-link to="/entities" class="nav-item" active-class="active" title="Entities" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Entities</span>
        </router-link>

        <router-link to="/tax-optimization" class="nav-item" active-class="active" title="Tax Optimizer" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M8 14s1.5 2 4 2 4-2 4-2"/><line x1="9" y1="9" x2="9.01" y2="9"/><line x1="15" y1="9" x2="15.01" y2="9"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Tax Optimizer</span>
        </router-link>

        <div class="nav-divider" v-if="!isCollapsed"></div>
        <div class="nav-section-label" v-if="!isCollapsed">STRATEGY</div>

        <router-link to="/financial-independence" class="nav-item" active-class="active" title="Financial Independence" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">FI Planner</span>
        </router-link>

        <router-link to="/rebalancing" class="nav-item" active-class="active" title="Rebalancing" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M11 19l-7-7 7-7"/><path d="M4 12h16"/><path d="M13 19l7-7-7-7"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Rebalancing</span>
        </router-link>

        <router-link to="/net-worth" class="nav-item" active-class="active" title="Net Worth" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M12 1v22M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Net Worth</span>
        </router-link>
      </nav>

      <div class="sidebar-footer">
        <router-link to="/admin" class="nav-item" active-class="active" title="Settings" @click="isMobileMenuOpen = false">
          <div class="nav-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
          </div>
          <span class="nav-label" v-if="!isCollapsed">Settings</span>
        </router-link>

        <button class="btn-collapse" @click="isCollapsed = !isCollapsed" :title="isCollapsed ? 'Expand' : 'Collapse'">
          <svg v-if="isCollapsed" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="13 17 18 12 13 7"/><polyline points="6 17 11 12 6 7"/></svg>
          <svg v-else xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="11 17 6 12 11 7"/><polyline points="18 17 13 12 18 7"/></svg>
        </button>
      </div>
    </aside>

    <!-- Main Content -->
    <main class="main-content">
      <header class="header">
        <div class="header-left">
          <button class="mobile-menu-toggle" @click="isMobileMenuOpen = !isMobileMenuOpen">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="18" x2="21" y2="18"/></svg>
          </button>
          <h1 class="page-title">{{ pageTitle }}</h1>
        </div>
        <div class="header-right">
          <!-- Theme Toggle -->
          <button class="theme-toggle" @click="toggleTheme" :title="isDark ? 'Switch to Light' : 'Switch to Dark'">
            <svg v-if="isDark" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>
            <svg v-else xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
          </button>

          <!-- User Profile -->
          <div class="user-profile" @click="toggleUserMenu">
            <div class="avatar">
              <span>{{ userInitials }}</span>
            </div>
            <span class="user-name">{{ userName }}</span>
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>

            <transition name="dropdown">
              <div class="user-dropdown" v-if="showUserMenu" @click.stop>
                <div class="dropdown-header">
                  <span class="dropdown-email">{{ authStore.user?.email }}</span>
                </div>
                <div class="dropdown-divider"></div>
                <button @click="handleLogout" class="dropdown-item dropdown-item-danger">
                  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
                  Sign Out
                </button>
              </div>
            </transition>
          </div>
        </div>
      </header>
      <div class="content">
        <router-view></router-view>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useAuthStore } from '../stores/auth';
import { usePortfolioStore } from '../stores/portfolio';
import { useRouter, useRoute } from 'vue-router';

const authStore = useAuthStore();
const portfolioStore = usePortfolioStore();
const router = useRouter();
const route = useRoute();

const isCollapsed = ref(false);
const isMobileMenuOpen = ref(false);
const showUserMenu = ref(false);
const isDark = ref(false);

// Theme management
const toggleTheme = () => {
  isDark.value = !isDark.value;
  document.documentElement.setAttribute('data-theme', isDark.value ? 'dark' : 'light');
  localStorage.setItem('theme', isDark.value ? 'dark' : 'light');
};

// Initialize theme from localStorage
onMounted(() => {
  const savedTheme = localStorage.getItem('theme');
  if (savedTheme === 'dark') {
    isDark.value = true;
    document.documentElement.setAttribute('data-theme', 'dark');
  }
  document.addEventListener('click', handleClickOutside);
  
  if (authStore.isAuthenticated) {
    portfolioStore.fetchPortfolios();
  }
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});

const pageTitle = computed(() => {
  const name = route.name?.toString() || 'Dashboard';
  return name.charAt(0).toUpperCase() + name.slice(1);
});

const userName = computed(() => {
  const email = authStore.user?.email || 'User';
  if (!email.includes('@')) return email;
  const namePart = email.split('@')[0];
  return namePart.split('.').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ');
});

const userInitials = computed(() => {
  const name = userName.value;
  const parts = name.split(' ');
  if (parts.length >= 2) {
    return parts[0][0] + parts[1][0];
  }
  return name.substring(0, 2).toUpperCase();
});

const toggleUserMenu = () => {
  showUserMenu.value = !showUserMenu.value;
};

const handleLogout = () => {
  authStore.logout();
  router.push('/login');
};

const handleClickOutside = (e) => {
  if (!e.target.closest('.user-profile')) {
    showUserMenu.value = false;
  }
};
</script>

<style scoped>
.layout {
  display: flex;
  height: 100vh;
  overflow: hidden;
  background: var(--color-bg-primary);
}

/* Sidebar */
.sidebar {
  width: 260px; /* Slightly wider for better typography */
  background: var(--glass-bg);
  backdrop-filter: blur(var(--glass-blur));
  -webkit-backdrop-filter: blur(var(--glass-blur));
  border-right: 1px solid var(--glass-border);
  display: flex;
  flex-direction: column;
  padding: var(--spacing-lg);
  transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  height: 100vh;
  z-index: var(--z-sticky);
}

.sidebar.collapsed {
  width: 80px;
  padding: var(--spacing-lg) var(--spacing-sm);
  align-items: center;
}

/* Brand */
.brand {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-xl);
  padding-bottom: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border-subtle);
  height: 48px;
}

.logo-mark {
  width: 40px;
  height: 40px;
  background: linear-gradient(135deg, var(--color-accent) 0%, var(--color-accent-dark) 100%);
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: 800;
  font-size: var(--font-size-base);
  box-shadow: var(--shadow-sm);
  flex-shrink: 0;
}

.brand-name {
  font-family: var(--font-display);
  font-size: var(--font-size-xl);
  font-weight: 800;
  color: var(--color-text-primary);
  letter-spacing: -0.02em;
}

/* Navigation */
.nav {
  display: flex;
  flex-direction: column;
  gap: 6px; /* Slightly more breathing room */
  flex: 1;
  overflow-y: auto;
  padding-right: 4px; /* Space for scrollbar */
}

.nav-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: 12px 16px;
  border-radius: var(--radius-md);
  color: var(--color-text-secondary);
  text-decoration: none;
  transition: all var(--transition-base);
  cursor: pointer;
  font-weight: 500;
  border: 1px solid transparent;
}

.sidebar.collapsed .nav-item {
  justify-content: center;
  padding: 12px;
  width: 48px;
  height: 48px;
}

.nav-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: var(--color-text-muted);
  transition: color var(--transition-fast);
}

.nav-label {
  font-size: var(--font-size-sm);
  font-weight: 500;
  white-space: nowrap;
}

.nav-divider {
  height: 1px;
  background: var(--color-border-subtle);
  margin: var(--spacing-md) 0;
}

.nav-section-label {
  font-size: var(--font-size-xs);
  font-weight: 600;
  color: var(--color-text-muted);
  letter-spacing: var(--letter-spacing-wide);
  padding: 0 var(--spacing-md);
  margin-bottom: var(--spacing-xs);
}

.nav-item:hover {
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
  border-color: var(--color-border-subtle);
}

.nav-item:hover .nav-icon {
  color: var(--color-accent);
}

.nav-item.active {
  background: rgba(59, 130, 246, 0.1); /* Subtle blue tint */
  color: var(--color-accent);
  border-color: rgba(59, 130, 246, 0.2);
}

.nav-item.active .nav-icon {
  color: var(--color-accent);
}

/* Sidebar Footer */
.sidebar-footer {
  margin-top: auto;
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-border-subtle);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.btn-collapse {
  width: 100%;
  padding: var(--spacing-sm);
  background: transparent;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-muted);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all var(--transition-fast);
}

.btn-collapse:hover {
  border-color: var(--color-accent);
  color: var(--color-accent);
  background: var(--color-bg-elevated);
}

/* Main Content */
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: var(--color-bg-primary); /* Slate 50 background */
  overflow: hidden;
  position: relative;
}

/* Header - Floating Glass Effect */
.header {
  height: 72px;
  margin: var(--spacing-lg) var(--spacing-xl) 0; /* Floating margin */
  background: var(--glass-bg);
  backdrop-filter: blur(var(--glass-blur));
  -webkit-backdrop-filter: blur(var(--glass-blur));
  border: 1px solid var(--glass-border);
  border-radius: var(--radius-lg); /* Rounded header */
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 var(--spacing-lg);
  flex-shrink: 0;
  z-index: 50;
  box-shadow: var(--shadow-glass);
}

.page-title {
  font-size: var(--font-size-xl);
  font-weight: 700;
  margin: 0;
  color: var(--color-text-primary);
}

.header-right {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}

/* Theme Toggle */
.theme-toggle {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: transparent;
  border: 1px solid transparent; /* Cleaner look */
  border-radius: var(--radius-full);
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.theme-toggle:hover {
  background: var(--color-bg-elevated);
  color: var(--color-accent);
}

/* User Profile */
.user-profile {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: 6px 8px 6px 6px;
  border-radius: var(--radius-full);
  cursor: pointer;
  transition: background var(--transition-fast);
  position: relative;
  border: 1px solid transparent;
}

.user-profile:hover {
  background: var(--color-bg-elevated);
  border-color: var(--color-border-subtle);
}

.avatar {
  width: 36px;
  height: 36px;
  background: var(--color-accent);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--font-size-sm);
  font-weight: 600;
  color: white;
  box-shadow: var(--shadow-sm);
}

.user-name {
  font-size: var(--font-size-sm);
  font-weight: 600;
  color: var(--color-text-primary);
}

.user-profile > svg {
  color: var(--color-text-muted);
  width: 14px;
  height: 14px;
}

/* User Dropdown */
.user-dropdown {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  width: 220px;
  background: var(--glass-bg);
  backdrop-filter: blur(12px);
  border: 1px solid var(--glass-border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-xl);
  overflow: hidden;
  z-index: var(--z-dropdown);
  padding: var(--spacing-xs);
}

.dropdown-header {
  padding: var(--spacing-md);
  background: rgba(0,0,0,0.02);
  margin: calc(var(--spacing-xs) * -1) calc(var(--spacing-xs) * -1) var(--spacing-xs);
  border-bottom: 1px solid var(--color-border-subtle);
}

.dropdown-email {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  font-weight: 500;
}

.dropdown-divider {
  display: none; /* Handled by padding/margin */
}

.dropdown-item {
  width: 100%;
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: 10px var(--spacing-md);
  border: none;
  background: transparent;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
  text-align: left;
  border-radius: var(--radius-md);
}

.dropdown-item:hover {
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
}

.dropdown-item-danger {
  color: var(--color-danger);
}

.dropdown-item-danger:hover {
  background: rgba(220, 38, 38, 0.05);
  color: var(--color-danger);
}

/* Content */
.content {
  padding: var(--spacing-xl);
  overflow-y: auto;
  flex: 1;
}

/* Transitions */
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}

.dropdown-enter-active, .dropdown-leave-active {
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
}
.dropdown-enter-from, .dropdown-leave-to {
  opacity: 0;
  transform: translateY(-8px) scale(0.95);
}
</style>

<style scoped>
/* Mobile Responsive Styles */
@media (max-width: 768px) {
  .sidebar {
    position: fixed;
    top: 0;
    bottom: 0;
    left: 0;
    z-index: 1000;
    transform: translateX(-100%);
    background: var(--color-bg-secondary); /* Solid background for mobile */
  }

  .sidebar.mobile-open {
    transform: translateX(0);
  }

  .header {
    margin: 0;
    border-radius: 0;
    border-top: none;
    border-left: none;
    border-right: none;
    height: 64px;
    padding: 0 var(--spacing-md);
  }
}

.mobile-menu-toggle {
  display: none;
  background: transparent;
  border: none;
  color: var(--color-text-primary);
  padding: 8px;
  cursor: pointer;
  margin-right: var(--spacing-sm);
}

@media (max-width: 768px) {
  .mobile-menu-toggle {
    display: flex;
  }
}
</style>
