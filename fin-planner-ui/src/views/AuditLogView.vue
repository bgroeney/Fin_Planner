<template>
  <div class="audit-view animate-fade-in">
    <div class="header-row">
      <div class="header-content">
        <h1>Audit Log</h1>
        <p class="subtitle">Track all changes to financial data for compliance</p>
      </div>
    </div>

    <!-- Filters -->
    <div class="filters card">
      <div class="filter-group">
        <label>Entity Type</label>
        <select v-model="filters.entityType" @change="loadLogs">
          <option value="">All</option>
          <option value="Portfolio">Portfolio</option>
          <option value="Holding">Holding</option>
          <option value="Transaction">Transaction</option>
          <option value="Goal">Goal</option>
          <option value="Decision">Decision</option>
        </select>
      </div>
      <div class="filter-group">
        <label>Action</label>
        <select v-model="filters.action" @change="loadLogs">
          <option value="">All</option>
          <option value="Create">Create</option>
          <option value="Update">Update</option>
          <option value="Delete">Delete</option>
        </select>
      </div>
      <button class="btn btn-secondary" @click="resetFilters">
        Reset Filters
      </button>
    </div>

    <div v-if="loading" class="loader-container">
      <div class="spinner"></div>
    </div>

    <div v-else-if="logs.length === 0" class="empty-state card">
      <div class="empty-icon">📋</div>
      <h3>No audit logs found</h3>
      <p>Changes to financial data will appear here.</p>
    </div>

    <div v-else class="logs-table card">
      <table class="data-table">
        <thead>
          <tr>
            <th>Timestamp</th>
            <th>User</th>
            <th>Action</th>
            <th>Entity</th>
            <th>Details</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="log in logs" :key="log.id">
            <td>
              <div class="timestamp-cell">
                <span class="date">{{ formatDate(log.timestamp) }}</span>
                <span class="time">{{ formatTime(log.timestamp) }}</span>
              </div>
            </td>
            <td>
              <span class="user-email">{{ log.userEmail }}</span>
            </td>
            <td>
              <span class="action-badge" :class="getActionClass(log.action)">
                {{ log.action }}
              </span>
            </td>
            <td>
              <div class="entity-cell">
                <span class="entity-type">{{ log.entityType }}</span>
                <span class="entity-name">{{ log.entityName }}</span>
              </div>
            </td>
            <td>
              <button class="btn btn-ghost btn-sm" @click="viewDetails(log)">
                View
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <div class="pagination">
        <button 
          class="btn btn-ghost" 
          :disabled="page <= 1" 
          @click="page--; loadLogs()"
        >
          Previous
        </button>
        <span class="page-info">Page {{ page }}</span>
        <button 
          class="btn btn-ghost" 
          :disabled="logs.length < pageSize" 
          @click="page++; loadLogs()"
        >
          Next
        </button>
      </div>
    </div>

    <!-- Detail Modal -->
    <div v-if="showDetailModal" class="modal-overlay" @click.self="showDetailModal = false">
      <div class="modal-card">
        <div class="modal-header">
          <h2>Change Details</h2>
          <button class="btn-close" @click="showDetailModal = false">&times;</button>
        </div>
        
        <div class="detail-content">
          <div class="detail-row">
            <span class="label">Timestamp:</span>
            <span>{{ formatDateTime(selectedLog?.timestamp) }}</span>
          </div>
          <div class="detail-row">
            <span class="label">User:</span>
            <span>{{ selectedLog?.userEmail }}</span>
          </div>
          <div class="detail-row">
            <span class="label">Action:</span>
            <span class="action-badge" :class="getActionClass(selectedLog?.action)">
              {{ selectedLog?.action }}
            </span>
          </div>
          <div class="detail-row">
            <span class="label">Entity:</span>
            <span>{{ selectedLog?.entityType }} - {{ selectedLog?.entityName }}</span>
          </div>
          <div class="detail-row" v-if="selectedLog?.ipAddress">
            <span class="label">IP Address:</span>
            <span>{{ selectedLog.ipAddress }}</span>
          </div>
          
          <div v-if="selectedLog?.oldValues" class="values-section">
            <h4>Previous Values</h4>
            <pre class="json-display">{{ formatJson(selectedLog.oldValues) }}</pre>
          </div>
          
          <div v-if="selectedLog?.newValues" class="values-section">
            <h4>New Values</h4>
            <pre class="json-display">{{ formatJson(selectedLog.newValues) }}</pre>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue';
import api from '../services/api';

const loading = ref(true);
const logs = ref([]);
const page = ref(1);
const pageSize = 50;
const showDetailModal = ref(false);
const selectedLog = ref(null);

const filters = reactive({
  entityType: '',
  action: ''
});

const formatDate = (dateStr) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleDateString('en-AU', { 
    day: '2-digit', 
    month: 'short', 
    year: 'numeric' 
  });
};

const formatTime = (dateStr) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleTimeString('en-AU', { 
    hour: '2-digit', 
    minute: '2-digit' 
  });
};

const formatDateTime = (dateStr) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString('en-AU');
};

const formatJson = (jsonStr) => {
  if (!jsonStr) return '';
  try {
    return JSON.stringify(JSON.parse(jsonStr), null, 2);
  } catch {
    return jsonStr;
  }
};

const getActionClass = (action) => {
  switch(action) {
    case 'Create': return 'action-create';
    case 'Update': return 'action-update';
    case 'Delete': return 'action-delete';
    default: return '';
  }
};

const loadLogs = async () => {
  loading.value = true;
  try {
    const params = new URLSearchParams();
    params.append('page', page.value);
    params.append('pageSize', pageSize);
    if (filters.entityType) params.append('entityType', filters.entityType);
    if (filters.action) params.append('action', filters.action);
    
    const res = await api.get(`/audit?${params.toString()}`);
    logs.value = res.data;
  } catch (e) {
    console.error('Failed to load audit logs', e);
    logs.value = [];
  } finally {
    loading.value = false;
  }
};

const viewDetails = async (log) => {
  try {
    const res = await api.get(`/audit/${log.id}`);
    selectedLog.value = res.data;
    showDetailModal.value = true;
  } catch (e) {
    console.error('Failed to load log details', e);
  }
};

const resetFilters = () => {
  filters.entityType = '';
  filters.action = '';
  page.value = 1;
  loadLogs();
};

onMounted(loadLogs);
</script>

<style scoped>
.audit-view {
  max-width: 1200px;
  margin: 0 auto;
}

.header-row {
  margin-bottom: var(--spacing-xl);
}

.filters {
  display: flex;
  align-items: flex-end;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
  flex-wrap: wrap;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.filter-group label {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.filter-group select {
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
  min-width: 150px;
}

.logs-table {
  padding: var(--spacing-lg);
}

.data-table {
  width: 100%;
  border-collapse: collapse;
}

.data-table th,
.data-table td {
  padding: var(--spacing-md);
  text-align: left;
  border-bottom: 1px solid var(--color-border);
}

.data-table th {
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: var(--letter-spacing-wide);
  color: var(--color-text-muted);
}

.timestamp-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.timestamp-cell .date {
  font-weight: 500;
}

.timestamp-cell .time {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.user-email {
  color: var(--color-text-secondary);
}

.action-badge {
  padding: 4px 8px;
  border-radius: var(--radius-sm);
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
}

.action-create {
  background: rgba(16, 185, 129, 0.15);
  color: #10b981;
}

.action-update {
  background: rgba(59, 130, 246, 0.15);
  color: #3b82f6;
}

.action-delete {
  background: rgba(239, 68, 68, 0.15);
  color: #ef4444;
}

.entity-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.entity-type {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  text-transform: uppercase;
}

.entity-name {
  font-weight: 500;
}

.btn-sm {
  padding: var(--spacing-xs) var(--spacing-sm);
  font-size: var(--font-size-sm);
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: var(--spacing-lg);
  margin-top: var(--spacing-xl);
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-border);
}

.page-info {
  color: var(--color-text-muted);
}

/* Modal */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  backdrop-filter: blur(4px);
}

.modal-card {
  background: var(--color-bg-card);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-xl);
  width: 100%;
  max-width: 600px;
  max-height: 80vh;
  overflow: auto;
  border: 1px solid var(--color-border);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
}

.btn-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  color: var(--color-text-muted);
  cursor: pointer;
}

.detail-content {
  padding: var(--spacing-lg);
}

.detail-row {
  display: flex;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-md);
}

.detail-row .label {
  font-weight: 600;
  color: var(--color-text-muted);
  min-width: 100px;
}

.values-section {
  margin-top: var(--spacing-xl);
}

.values-section h4 {
  margin-bottom: var(--spacing-sm);
  color: var(--color-text-secondary);
}

.json-display {
  background: var(--color-bg-elevated);
  padding: var(--spacing-md);
  border-radius: var(--radius-md);
  font-family: monospace;
  font-size: var(--font-size-sm);
  overflow-x: auto;
  max-height: 200px;
  color: var(--color-text-secondary);
}

.empty-state {
  text-align: center;
  padding: 60px;
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
