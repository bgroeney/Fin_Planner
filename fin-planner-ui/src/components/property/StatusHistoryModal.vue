<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="history-modal">
      <div class="modal-header">
        <div class="header-title">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/>
            <polyline points="12 6 12 12 16 14"/>
          </svg>
          <h2>Status History</h2>
        </div>
        <button class="close-btn" @click="$emit('close')">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="6" x2="6" y2="18"/>
            <line x1="6" y1="6" x2="18" y2="18"/>
          </svg>
        </button>
      </div>
      
      <div class="modal-body">
        <div v-if="loading" class="loading-state">
          <span class="spinner"></span>
          <span>Loading history...</span>
        </div>
        
        <div v-else-if="history.length === 0" class="empty-state">
          <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="12" cy="12" r="10"/>
            <polyline points="12 6 12 12 16 14"/>
          </svg>
          <p>No status changes recorded yet.</p>
        </div>
        
        <div v-else class="history-feed">
          <article 
            v-for="(entry, index) in history" 
            :key="entry.id" 
            class="history-card"
            :class="{ 'is-first': index === 0 }"
          >
            <header class="card-header">
              <span class="status-pill" :class="getStatusClass(entry.status)">
                {{ entry.status }}
              </span>
              <time class="timestamp">{{ formatDate(entry.timestamp) }}</time>
            </header>
            
            <div class="card-meta">
              <span v-if="entry.previousStatus" class="transition-info">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="9 18 15 12 9 6"/>
                </svg>
                from <strong>{{ entry.previousStatus }}</strong>
              </span>
              <span class="user-info">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                  <circle cx="12" cy="7" r="4"/>
                </svg>
                {{ entry.userName }}
              </span>
            </div>
            
            <p v-if="entry.comment" class="comment-text">
              "{{ entry.comment }}"
            </p>
            
            <footer v-if="entry.hasSnapshot" class="card-actions">
              <button class="action-btn" @click="viewSnapshot(entry)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                  <circle cx="12" cy="12" r="3"/>
                </svg>
                View Snapshot
              </button>
              <button class="action-btn action-btn--warning" @click="confirmRestore(entry)" :disabled="restoring">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="1 4 1 10 7 10"/>
                  <path d="M3.51 15a9 9 0 1 0 2.13-9.36L1 10"/>
                </svg>
                Restore
              </button>
            </footer>
          </article>
        </div>
      </div>
      
      <!-- Snapshot Viewer Panel -->
      <div v-if="selectedSnapshot" class="snapshot-panel">
        <div class="snapshot-header">
          <h3>Snapshot at {{ formatDate(selectedSnapshot.timestamp) }}</h3>
          <button class="close-btn close-btn--small" @click="selectedSnapshot = null">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18"/>
              <line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>
        </div>
        <div class="snapshot-grid" v-if="parsedInputs">
          <div class="snapshot-item">
            <span class="snapshot-label">Asking Price</span>
            <span class="snapshot-value">{{ formatCurrency(parsedInputs.AskingPrice) }}</span>
          </div>
          <div class="snapshot-item">
            <span class="snapshot-label">Gross Rent</span>
            <span class="snapshot-value">{{ formatCurrency(parsedInputs.EstimatedGrossRent) }}</span>
          </div>
          <div class="snapshot-item">
            <span class="snapshot-label">Vacancy</span>
            <span class="snapshot-value">{{ parsedInputs.VacancyRatePercent }}%</span>
          </div>
          <div class="snapshot-item">
            <span class="snapshot-label">Interest Rate</span>
            <span class="snapshot-value">{{ parsedInputs.InterestRatePercent }}%</span>
          </div>
          <div class="snapshot-item">
            <span class="snapshot-label">Discount Rate</span>
            <span class="snapshot-value">{{ parsedInputs.DiscountRate }}%</span>
          </div>
          <div class="snapshot-item">
            <span class="snapshot-label">Hold Period</span>
            <span class="snapshot-value">{{ parsedInputs.HoldingPeriodYears }} yrs</span>
          </div>
        </div>
      </div>
    </div>
    
    <!-- Restore Confirmation Modal -->
    <div v-if="showRestoreConfirm" class="confirm-overlay" @click.self="showRestoreConfirm = false">
      <div class="confirm-dialog">
        <h3>Restore to snapshot?</h3>
        <p>This will overwrite current inputs with the values from {{ formatDate(restoreEntry?.timestamp) }}.</p>
        <div class="confirm-actions">
          <button class="btn btn-secondary" @click="showRestoreConfirm = false">Cancel</button>
          <button class="btn btn-warning" @click="executeRestore" :disabled="restoring">
            {{ restoring ? 'Restoring...' : 'Restore' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '../../services/api'

const props = defineProps({
  dealId: { type: String, required: true }
})

const emit = defineEmits(['close', 'restored'])

const history = ref([])
const loading = ref(true)
const selectedSnapshot = ref(null)
const showRestoreConfirm = ref(false)
const restoreEntry = ref(null)
const restoring = ref(false)

const parsedInputs = computed(() => {
  if (!selectedSnapshot.value?.inputsSnapshotJson) return null
  try {
    return JSON.parse(selectedSnapshot.value.inputsSnapshotJson)
  } catch {
    return null
  }
})

onMounted(async () => {
  try {
    const response = await api.get(`/propertydeals/${props.dealId}/status-history`)
    history.value = response.data
  } catch (error) {
    console.error('Failed to fetch status history:', error)
  } finally {
    loading.value = false
  }
})

function viewSnapshot(entry) {
  selectedSnapshot.value = entry
}

function confirmRestore(entry) {
  restoreEntry.value = entry
  showRestoreConfirm.value = true
}

async function executeRestore() {
  if (!restoreEntry.value) return
  
  try {
    restoring.value = true
    await api.post(`/propertydeals/${props.dealId}/restore-snapshot/${restoreEntry.value.id}`)
    showRestoreConfirm.value = false
    emit('restored')
    emit('close')
  } catch (error) {
    console.error('Failed to restore snapshot:', error)
    alert('Failed to restore snapshot. Please try again.')
  } finally {
    restoring.value = false
  }
}

function formatCurrency(value) {
  return new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD', maximumFractionDigits: 0 }).format(value || 0)
}

function getStatusClass(status) {
  const statusMap = {
    'Draft': 'status--draft',
    'Under Review': 'status--review',
    'Proceed to Acquire': 'status--proceed',
    'Offer Made': 'status--offer',
    'Under Due Diligence': 'status--dd',
    'Waiting Settlement': 'status--settlement',
    'Acquired': 'status--acquired',
    'Pass': 'status--pass'
  }
  return statusMap[status] || 'status--default'
}

function formatDate(isoString) {
  if (!isoString) return ''
  const date = new Date(isoString)
  return date.toLocaleString('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}
</script>

<style scoped>
/* Modal Backdrop */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal-backdrop, 900);
  animation: fadeIn 0.2s ease;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes slideUp {
  from { opacity: 0; transform: translateY(16px); }
  to { opacity: 1; transform: translateY(0); }
}

/* Modal Container */
.history-modal {
  width: 100%;
  max-width: 480px;
  max-height: 80vh;
  display: flex;
  flex-direction: column;
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl, 16px);
  box-shadow: var(--shadow-xl);
  animation: slideUp 0.25s ease;
  overflow: hidden;
}

/* Header */
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-md) var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
  background: var(--color-bg-secondary);
}

.header-title {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  color: var(--color-text-primary);
}

.header-title svg {
  color: var(--color-accent);
}

.header-title h2 {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: 600;
}

.close-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  background: transparent;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-muted);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.close-btn:hover {
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
  border-color: var(--color-text-muted);
}

.close-btn--small {
  width: 24px;
  height: 24px;
}

/* Body */
.modal-body {
  flex: 1;
  overflow-y: auto;
  padding: var(--spacing-md);
}

.loading-state,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2xl);
  color: var(--color-text-muted);
  text-align: center;
  gap: var(--spacing-md);
}

.empty-state p {
  margin: 0;
}

/* Spinner */
.spinner {
  width: 24px;
  height: 24px;
  border: 2px solid var(--color-border);
  border-top-color: var(--color-accent);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* History Feed - Card Layout */
.history-feed {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.history-card {
  background: var(--color-bg-secondary);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-md);
  transition: all var(--transition-fast);
  cursor: default;
}

.history-card:hover {
  border-color: var(--color-accent-light);
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.history-card.is-first {
  border-left: 4px solid var(--color-accent);
  background: linear-gradient(to right, rgba(30, 64, 175, 0.03), transparent);
}

[data-theme="dark"] .history-card.is-first {
  background: linear-gradient(to right, rgba(59, 130, 246, 0.05), transparent);
}

/* Card Header */
.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.375rem;
}

.status-pill {
  display: inline-flex;
  align-items: center;
  padding: 0.375rem 0.75rem;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border-radius: 8px;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.1);
}

/* Status Colors - Refined for "Pro Max" look */
.status--draft { background: rgba(148, 163, 184, 0.1); color: #475569; border: 1px solid rgba(148, 163, 184, 0.2); }
.status--review { background: rgba(59, 130, 246, 0.1); color: #2563eb; border: 1px solid rgba(59, 130, 246, 0.2); }
.status--proceed { background: rgba(34, 197, 94, 0.1); color: #16a34a; border: 1px solid rgba(34, 197, 94, 0.2); }
.status--offer { background: rgba(245, 158, 11, 0.1); color: #d97706; border: 1px solid rgba(245, 158, 11, 0.2); }
.status--dd { background: rgba(168, 85, 247, 0.1); color: #9333ea; border: 1px solid rgba(168, 85, 247, 0.2); }
.status--settlement { background: rgba(6, 182, 212, 0.1); color: #0891b2; border: 1px solid rgba(6, 182, 212, 0.2); }
.status--acquired { background: rgba(16, 185, 129, 0.1); color: #059669; border: 1px solid rgba(16, 185, 129, 0.2); }
.status--pass { background: rgba(239, 68, 68, 0.1); color: #dc2626; border: 1px solid rgba(239, 68, 68, 0.2); }
.status--default { background: rgba(148, 163, 184, 0.1); color: #475569; border: 1px solid rgba(148, 163, 184, 0.2); }

[data-theme="dark"] .status--draft { color: #94a3b8; border-color: rgba(148, 163, 184, 0.3); }
[data-theme="dark"] .status--review { color: #60a5fa; border-color: rgba(59, 130, 246, 0.3); }
[data-theme="dark"] .status--proceed { color: #4ade80; border-color: rgba(34, 197, 94, 0.3); }
[data-theme="dark"] .status--offer { color: #fbbf24; border-color: rgba(245, 158, 11, 0.3); }
[data-theme="dark"] .status--dd { color: #c084fc; border-color: rgba(168, 85, 247, 0.3); }
[data-theme="dark"] .status--settlement { color: #22d3ee; border-color: rgba(6, 182, 212, 0.3); }
[data-theme="dark"] .status--acquired { color: #34d399; border-color: rgba(16, 185, 129, 0.3); }
[data-theme="dark"] .status--pass { color: #f87171; border-color: rgba(239, 68, 68, 0.3); }
[data-theme="dark"] .status--default { color: #94a3b8; border-color: rgba(148, 163, 184, 0.3); }

.timestamp {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

/* Card Meta */
.card-meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-xs);
}

.transition-info,
.user-info {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.transition-info strong {
  color: var(--color-text-secondary);
  font-weight: 500;
}

/* Comment */
.comment-text {
  margin: var(--spacing-sm) 0;
  padding: var(--spacing-sm) var(--spacing-md);
  background: var(--color-bg-elevated);
  border-left: 3px solid var(--color-accent);
  border-radius: 0 var(--radius-sm) var(--radius-sm) 0;
  font-size: var(--font-size-sm);
  font-style: italic;
  color: var(--color-text-secondary);
}

/* Card Actions */
.card-actions {
  display: flex;
  gap: var(--spacing-sm);
  margin-top: var(--spacing-sm);
  padding-top: var(--spacing-sm);
  border-top: 1px solid var(--color-border-subtle);
}

.action-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.375rem 0.75rem;
  font-size: var(--font-size-xs);
  font-weight: 500;
  background: var(--color-bg-elevated);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.action-btn:hover {
  background: var(--color-bg-card);
  border-color: var(--color-accent);
  color: var(--color-accent);
}

.action-btn--warning {
  border-color: var(--color-warning);
  color: var(--color-warning);
}

.action-btn--warning:hover {
  background: rgba(217, 119, 6, 0.1);
}

.action-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Snapshot Panel */
.snapshot-panel {
  border-top: 1px solid var(--color-border);
  padding: var(--spacing-md) var(--spacing-lg);
  background: var(--color-bg-secondary);
}

.snapshot-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-md);
}

.snapshot-header h3 {
  margin: 0;
  font-size: var(--font-size-sm);
  font-weight: 600;
  color: var(--color-text-primary);
}

.snapshot-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-sm);
}

.snapshot-item {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
  padding: var(--spacing-sm);
  background: var(--color-bg-elevated);
  border-radius: var(--radius-md);
}

.snapshot-label {
  font-size: 0.625rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-muted);
}

.snapshot-value {
  font-size: var(--font-size-sm);
  font-weight: 600;
  color: var(--color-text-primary);
}

/* Confirm Dialog */
.confirm-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: calc(var(--z-modal, 1000) + 100);
}

.confirm-dialog {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-xl);
  max-width: 360px;
  text-align: center;
  box-shadow: var(--shadow-xl);
}

.confirm-dialog h3 {
  margin: 0 0 var(--spacing-sm) 0;
  font-size: var(--font-size-lg);
  color: var(--color-text-primary);
}

.confirm-dialog p {
  margin: 0 0 var(--spacing-lg) 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.confirm-actions {
  display: flex;
  justify-content: center;
  gap: var(--spacing-sm);
}

.btn-warning {
  background: var(--color-warning);
  color: white;
  border-color: var(--color-warning);
}

.btn-warning:hover {
  opacity: 0.9;
}
</style>
