<template>
  <div class="login-container">
    <!-- Decorative background -->
    <div class="login-background">
      <div class="bg-pattern"></div>
    </div>

    <div class="login-card">
      <div class="brand-section">
        <div class="logo-mark">
          <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 2L2 7L12 12L22 7L12 2Z" fill="currentColor" opacity="0.8"/>
            <path d="M2 17L12 22L22 17" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M2 12L12 17L22 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </div>
        <h1 class="brand-title">FinPlanner</h1>
        <p class="brand-tagline">Family Office Wealth Management</p>
      </div>

      <div class="login-content">
        <h2>Welcome</h2>
        <p class="text-muted mb-lg">Sign in to manage your investment portfolios</p>

        <div class="google-btn-wrapper">
          <GoogleLogin :callback="handleGoogleCallback" />
        </div>

        <div v-if="error" class="error-message">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
          {{ error }}
        </div>
      </div>

      <div class="login-footer">
        <p>Secure, private portfolio tracking for discerning investors</p>
      </div>
    <div v-if="loading" class="login-overlay">
      <div class="card p-xl flex-center flex-column">
        <MoneyBoxLoader size="lg" text="Accessing Secure Vault..." />
      </div>
    </div>
  </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';
import MoneyBoxLoader from '../components/MoneyBoxLoader.vue';

const error = ref('');
const loading = ref(false);
const authStore = useAuthStore();
const router = useRouter();

const handleGoogleCallback = async (response) => {
  error.value = '';
  loading.value = true;
  try {
    await authStore.loginWithGoogle(response);
    router.push('/');
  } catch (err) {
    loading.value = false;
    error.value = err.response?.data?.message || 'Authentication failed. Please try again.';
  }
};
</script>

<style scoped>
.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-bg-primary);
  position: relative;
  overflow: hidden;
}

/* Decorative Background */
.login-background {
  position: absolute;
  inset: 0;
  overflow: hidden;
}

.bg-pattern {
  position: absolute;
  top: -50%;
  left: -50%;
  width: 200%;
  height: 200%;
  background: radial-gradient(circle at 30% 20%, rgba(201, 162, 39, 0.08) 0%, transparent 50%),
              radial-gradient(circle at 70% 80%, rgba(201, 162, 39, 0.05) 0%, transparent 50%);
  animation: rotate 60s linear infinite;
}

@keyframes rotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

/* Login Card */
.login-card {
  position: relative;
  width: 100%;
  max-width: 440px;
  background: var(--color-bg-secondary);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-xl);
  padding: var(--spacing-2xl);
  box-shadow: var(--shadow-xl);
  z-index: 1;
}

/* Brand Section */
.brand-section {
  text-align: center;
  margin-bottom: var(--spacing-2xl);
  padding-bottom: var(--spacing-xl);
  border-bottom: 1px solid var(--color-border-subtle);
}

.logo-mark {
  width: 64px;
  height: 64px;
  background: linear-gradient(135deg, var(--color-accent-gold) 0%, var(--color-accent-gold-dark) 100%);
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-bg-primary);
  margin: 0 auto var(--spacing-lg);
  box-shadow: var(--shadow-gold);
}

.brand-title {
  font-family: var(--font-display);
  font-size: var(--font-size-3xl);
  font-weight: 600;
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-xs);
  letter-spacing: var(--letter-spacing-tight);
}

.brand-tagline {
  font-size: var(--font-size-sm);
  color: var(--color-accent-gold);
  text-transform: uppercase;
  letter-spacing: var(--letter-spacing-wider);
}

/* Login Content */
.login-content {
  text-align: center;
}

.login-content h2 {
  font-family: var(--font-display);
  font-size: var(--font-size-xl);
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-sm);
}

.google-btn-wrapper {
  display: flex;
  justify-content: center;
  margin-top: var(--spacing-lg);
  min-height: 44px;
}

/* Error Message */
.error-message {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-sm);
  margin-top: var(--spacing-lg);
  padding: var(--spacing-md);
  background: rgba(248, 113, 113, 0.1);
  border: 1px solid rgba(248, 113, 113, 0.3);
  border-radius: var(--radius-md);
  color: var(--color-danger);
  font-size: var(--font-size-sm);
}

/* Footer */
.login-footer {
  margin-top: var(--spacing-2xl);
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-border-subtle);
  text-align: center;
}

.login-footer p {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.mb-lg { margin-bottom: var(--spacing-lg); }

.login-overlay {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(255, 255, 255, 0.9);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
}
.flex-column { flex-direction: column; }
.flex-center { display: flex; align-items: center; justify-content: center; }
.p-xl { padding: var(--spacing-xl); }
</style>
