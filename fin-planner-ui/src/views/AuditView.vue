<template>
  <div class="audit-view animate-fade-in content-container">
    <div class="header-row mb-xl">
      <div class="header-content">
        <h1>Audit Log</h1>
        <p class="subtitle">Track all changes and system activities in your workspace</p>
      </div>
      <div class="actions">
        <button class="btn btn-secondary" @click="loadData">Refresh</button>
      </div>
    </div>

    <div v-if="loading && !logs.length" class="loader-container">
      <div class="spinner"></div>
      <p>Loading activity logs...</p>
    </div>

    <div v-else class="card overflow-hidden">
      <table class="data-table">
        <thead>
          <tr>
            <th>Timestamp</th>
            <th>Entity Type</th>
            <th>Entity ID</th>
            <th>Action</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="log in logs" :key="log.id">
            <td class="timestamp">{{ formatDateTime(log.timestamp) }}</td>
            <td><span class="badge">{{ log.entityType }}</span></td>
            <td class="entity-id">{{ log.entityId.substring(0, 8) }}...</td>
            <td class="action-cell">{{ log.action }}</td>
          </tr>
        </tbody>
      </table>
      
      <div v-if="logs.length === 0" class="empty-state">
        <p>No activity recorded yet.</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import api from '../services/api';

const logs = ref([]);
const loading = ref(true);

const loadData = async () => {
  loading.value = true;
  try {
    const res = await api.get('/audit');
    logs.value = res.data;
  } catch (e) {
    console.error('Failed to load audit logs', e);
  } finally {
    loading.value = false;
  }
};

const formatDateTime = (dateString) => {
  return new Date(dateString).toLocaleString('en-AU', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
};

onMounted(loadData);
</script>

<style scoped>
.content-container {
  max-width: 1000px;
  margin: 0 auto;
}

.timestamp {
  font-family: monospace;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.badge {
  padding: 2px 8px;
  background: var(--color-bg-elevated);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-xs);
  text-transform: uppercase;
}

.entity-id {
  font-family: monospace;
  color: var(--color-text-muted);
}

.action-cell {
  font-weight: 500;
}

.empty-state {
  padding: var(--spacing-2xl);
  text-align: center;
  color: var(--color-text-muted);
}
</style>
