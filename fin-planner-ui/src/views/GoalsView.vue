<template>
  <div class="goals-view animate-fade-in">
    <div class="header-row">
      <div class="header-content">
        <h1>Financial Goals</h1>
        <p class="subtitle">Track your progress towards financial independence</p>
      </div>
      <button class="btn btn-primary" @click="openCreateModal">
        <span class="icon">+</span> Set New Goal
      </button>
    </div>

    <div v-if="loading" class="loader-container">
      <div class="spinner"></div>
    </div>

    <div v-else-if="goals.length === 0" class="empty-state">
      <div class="empty-icon">🎯</div>
      <h2>No goals set yet</h2>
      <p>Start tracking your savings targets, big purchases, or debt repayments.</p>
      <button class="btn btn-primary" @click="openCreateModal">Create Your First Goal</button>
    </div>

    <div v-else class="goals-grid">
      <div v-for="goal in goals" :key="goal.id" class="goal-card card">
        <div class="goal-card-header">
          <h3>{{ goal.name }}</h3>
          <div class="goal-actions">
            <button class="btn-icon" @click="openEditModal(goal)" title="Edit">✎</button>
            <button class="btn-icon danger" @click="confirmDelete(goal)" title="Delete">🗑</button>
          </div>
        </div>
        
        <div class="goal-progress-section">
          <div class="progress-labels">
            <span class="current">{{ formatCurrency(goal.currentAmount) }}</span>
            <span class="target">of {{ formatCurrency(goal.targetAmount) }}</span>
          </div>
          <div class="progress-bar-bg">
            <div class="progress-bar-fill" :style="{ width: calculateProgress(goal) + '%' }"></div>
          </div>
          <div class="progress-meta">
            <span class="percentage">{{ calculateProgress(goal).toFixed(1) }}% Complete</span>
            <span class="target-date">Target: {{ formatDate(goal.targetDate) }}</span>
          </div>
        </div>

        <div class="goal-status">
            <span class="status-badge" :class="{ completed: goal.isCompleted }">
                {{ goal.isCompleted ? 'Completed' : 'In Progress' }}
            </span>
        </div>
      </div>
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-card">
        <h2>{{ isEditing ? 'Edit Goal' : 'Set New Goal' }}</h2>
        
        <form @submit.prevent="saveGoal">
          <div class="form-group">
            <label>Goal Name</label>
            <input v-model="form.name" type="text" placeholder="e.g. House Deposit" required />
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Target Amount</label>
              <input v-model.number="form.targetAmount" type="number" step="0.01" required />
            </div>
            <div class="form-group">
              <label>Current Amount saved</label>
              <input v-model.number="form.currentAmount" type="number" step="0.01" />
            </div>
          </div>

          <div class="form-group">
            <label>Target Date</label>
            <input v-model="form.targetDate" type="date" required />
          </div>
          
           <div class="form-group checkbox-group" v-if="isEditing">
            <input type="checkbox" id="completed" v-model="form.isCompleted" />
            <label for="completed">Mark as Completed</label>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" @click="closeModal">Cancel</button>
            <button type="submit" class="btn btn-primary" :disabled="saving">
              {{ saving ? 'Saving...' : (isEditing ? 'Update Goal' : 'Create Goal') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import goalService from '../services/goalService';
import { formatCurrency, formatDate } from '../utils/formatters';

const goals = ref([]);
const loading = ref(true);
const showModal = ref(false);
const isEditing = ref(false);
const saving = ref(false);
const editingId = ref(null);

const form = ref({
  name: '',
  targetAmount: 0,
  currentAmount: 0,
  targetDate: '',
  isCompleted: false
});

const calculateProgress = (goal) => {
  if (goal.targetAmount <= 0) return 0;
  const pct = (goal.currentAmount / goal.targetAmount) * 100;
  return Math.min(pct, 100);
};

const fetchGoals = async () => {
  loading.value = true;
  try {
    const res = await goalService.getGoals();
    goals.value = res.data;
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
};

const openCreateModal = () => {
  isEditing.value = false;
  editingId.value = null;
  form.value = {
    name: '',
    targetAmount: 10000,
    currentAmount: 0,
    targetDate: new Date(new Date().setFullYear(new Date().getFullYear() + 1)).toISOString().split('T')[0],
    isCompleted: false
  };
  showModal.value = true;
};

const openEditModal = (goal) => {
  isEditing.value = true;
  editingId.value = goal.id;
  form.value = {
    name: goal.name,
    targetAmount: goal.targetAmount,
    currentAmount: goal.currentAmount,
    targetDate: goal.targetDate.split('T')[0],
    isCompleted: goal.isCompleted
  };
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const saveGoal = async () => {
  // Basic validation
  if (!form.value.targetDate) {
    alert('Please select a target date');
    return;
  }
  
  const year = new Date(form.value.targetDate).getFullYear();
  if (year < 2000 || year > 2100) {
    alert('Please enter a valid year between 2000 and 2100');
    return;
  }

  saving.value = true;
  try {
    if (isEditing.value) {
      await goalService.updateGoal(editingId.value, form.value);
    } else {
      await goalService.createGoal(form.value);
    }
    await fetchGoals();
    closeModal();
  } catch (e) {
    console.error('Failed to save goal:', e);
    const message = e.response?.data?.message || e.response?.data || e.message || 'Unknown error';
    alert(`Failed to save goal: ${message}`);
  } finally {
    saving.value = false;
  }
};

const confirmDelete = async (goal) => {
  if (confirm(`Are you sure you want to delete "${goal.name}"?`)) {
    try {
      await goalService.deleteGoal(goal.id);
      await fetchGoals();
    } catch (e) {
      console.error(e);
      alert('Failed to delete goal');
    }
  }
};

onMounted(fetchGoals);
</script>

<style scoped>
.goals-view {
  max-width: 1200px;
  margin: 0 auto;
}

.header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-xl);
}

.goals-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: var(--spacing-lg);
}

.goal-card {
  padding: var(--spacing-lg);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.goal-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.goal-card-header h3 {
  margin: 0;
  font-size: var(--font-size-lg);
}

.goal-actions {
  display: flex;
  gap: var(--spacing-sm);
}

.btn-icon {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 1.2rem;
  padding: 4px;
  opacity: 0.6;
  transition: opacity 0.2s;
  color: var(--color-text-primary);
}

.btn-icon:hover {
  opacity: 1;
}

.btn-icon.danger:hover {
  color: var(--color-error);
}

.progress-bar-bg {
  height: 10px;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-full);
  margin: 8px 0;
  overflow: hidden;
}

.progress-bar-fill {
  height: 100%;
  background: var(--color-accent);
  border-radius: var(--radius-full);
  transition: width 0.5s ease;
}

.progress-labels {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
}

.progress-labels .current {
  font-size: var(--font-size-xl);
  font-weight: 700;
  color: var(--color-text-primary);
}

.progress-labels .target {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.progress-meta {
  display: flex;
  justify-content: space-between;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.status-badge {
    padding: 2px 8px;
    border-radius: var(--radius-sm);
    background: var(--color-bg-elevated);
    color: var(--color-text-muted);
    font-size: var(--font-size-xs);
    font-weight: 500;
}

.status-badge.completed {
    background: rgba(16, 185, 129, 0.1);
    color: #10b981;
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  backdrop-filter: blur(4px);
}

.modal-card {
  background: var(--color-bg-card);
  padding: var(--spacing-xl);
  border-radius: var(--radius-lg);
  width: 100%;
  max-width: 500px;
  box-shadow: var(--shadow-xl);
  border: 1px solid var(--color-border);
}

.form-group {
  margin-bottom: var(--spacing-md);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-md);
}

.form-group label {
  display: block;
  margin-bottom: var(--spacing-xs);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.form-group input[type="text"],
.form-group input[type="number"],
.form-group input[type="date"] {
  width: 100%;
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-input);
  color: var(--color-text-primary);
  font-size: var(--font-size-base);
}

.form-group.checkbox-group {
    display: flex;
    align-items: center;
    gap: var(--spacing-sm);
}

.form-group.checkbox-group label {
    margin: 0;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-md);
  margin-top: var(--spacing-xl);
}

.empty-state {
  text-align: center;
  padding: 60px;
  background: var(--color-bg-card);
  border-radius: var(--radius-lg);
  border: 1px dashed var(--color-border);
}

.empty-icon {
  font-size: 48px;
  margin-bottom: var(--spacing-md);
}

.loader-container {
    display: flex;
    justify-content: center;
    padding: 60px;
}
.spinner {
    width: 30px;
    height: 30px;
    border: 3px solid rgba(255,255,255,0.1);
    border-radius: 50%;
    border-top-color: var(--color-accent);
    animation: spin 1s ease-in-out infinite;
}
@keyframes spin {
    to { transform: rotate(360deg); }
}
</style>
