<template>
  <div class="goal-widget card">
    <div class="widget-header">
      <h3>Financial Goals</h3>
      <router-link to="/goals" class="btn-text">View All</router-link>
    </div>

    <div v-if="loading" class="loading-state">
      <span class="spinner"></span> Loading goals...
    </div>

    <div v-else-if="goals.length === 0" class="empty-state">
      <p>No active goals.</p>
      <router-link to="/goals" class="btn btn-sm btn-primary">Set a Goal</router-link>
    </div>

    <div v-else class="goals-list">
      <div v-for="goal in topGoals" :key="goal.id" class="goal-item">
        <div class="goal-info">
          <span class="goal-name">{{ goal.name }}</span>
          <span class="goal-amount">{{ formatCurrency(goal.currentAmount) }} / {{ formatCurrency(goal.targetAmount) }}</span>
          <span class="goal-date">{{ formatDate(goal.targetDate) }}</span>
        </div>
        <div class="progress-bar-container">
          <div class="progress-bar" :style="{ width: calculateProgress(goal) + '%' }"></div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import goalService from '../../services/goalService';
import { formatCurrency, formatDate } from '../../utils/formatters';

const goals = ref([]);
const loading = ref(true);

const topGoals = computed(() => {
  return goals.value
    .filter(g => !g.isCompleted)
    .sort((a, b) => new Date(a.targetDate) - new Date(b.targetDate))
    .slice(0, 3);
});

const calculateProgress = (goal) => {
  if (goal.targetAmount <= 0) return 0;
  const pct = (goal.currentAmount / goal.targetAmount) * 100;
  return Math.min(pct, 100);
};

const fetchGoals = async () => {
  try {
    const res = await goalService.getGoals();
    goals.value = res.data;
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
};

onMounted(fetchGoals);
</script>

<style scoped>
.goal-widget {
  padding: var(--spacing-lg);
  display: flex;
  flex-direction: column;
}

.widget-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-md);
}

.widget-header h3 {
  font-size: var(--font-size-md);
  margin: 0;
}

.goals-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.goal-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.goal-info {
  display: flex;
  justify-content: space-between;
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
}

.goal-date {
    color: var(--color-text-muted);
    font-size: var(--font-size-xs);
    margin-left: var(--spacing-sm);
}

.progress-bar-container {
  height: 8px;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-sm);
  overflow: hidden;
}

.progress-bar {
  height: 100%;
  background: var(--color-accent); /* Uses theme primary color (blue) */
  border-radius: var(--radius-sm);
  transition: width 0.5s ease-out;
}

.empty-state {
  text-align: center;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  padding: var(--spacing-md);
}

.btn-text {
  font-size: var(--font-size-sm);
  color: var(--color-primary);
  text-decoration: none;
}

.btn-text:hover {
  text-decoration: underline;
}
</style>
