<template>
  <div class="import-view animate-fade-in content-container">
    <div class="page-header mb-lg">
      <div class="header-content">
        <h1>Import Data</h1>
        <p class="text-muted">Upload Netwealth CSV files to update your portfolio.</p>
      </div>
    </div>

    <!-- Upload Card -->
    <div class="card mb-xl">
      <div v-if="!preview">
        <div class="form-group mb-lg">
          <label class="label-text mb-sm block">Select Portfolio</label>
          <div class="select-wrapper">
            <select v-model="selectedPortfolioId" class="form-select">
              <option disabled value="">Choose a portfolio...</option>
              <option v-for="p in portfolios" :key="p.id" :value="p.id">{{ p.name }}</option>
            </select>
          </div>
        </div>

        <div class="upload-area mb-lg" @dragover.prevent @drop.prevent="handleDrop">
          <input type="file" id="fileInput" @change="handleFileChange" accept=".csv" class="hidden">
          <label for="fileInput" class="upload-label">
            <div class="upload-icon">
              <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
            </div>
            <div class="upload-text">
              <span v-if="file" class="font-medium text-primary">{{ file.name }}</span>
              <span v-else>Click to upload or drag and drop CSV</span>
            </div>
            <div class="text-xs text-muted mt-xs">Supported: Portfolio Valuation, Transaction Listing</div>
          </label>
        </div>

        <div class="flex-end">
          <button @click="uploadFile" class="btn btn-primary" :disabled="!file || !selectedPortfolioId">
            Next: Preview Import
          </button>
        </div>
      </div>

      <!-- Preview Mode -->
      <div v-else class="preview-mode">
        <div v-if="importing" class="loading-state p-xl flex-center flex-column">
          <MoneyBoxLoader size="lg" text="Processing Import..." />
        </div>
        
        <div v-else class="preview-content">
          <div class="flex-between mb-lg items-center">
             <h2 class="text-xl font-bold">Preview Import</h2>
             <div class="text-sm text-muted">
               Review the data before adding it to your portfolio.
             </div>
          </div>

          <!-- Stats Grid -->
          <div class="stats-grid mb-lg">
             <div class="stat-box">
                <div class="stat-label">File Type</div>
                <div class="stat-value text-primary">{{ preview.fileType }}</div>
             </div>
             <div class="stat-box">
                <div class="stat-label">Account</div>
                <div class="stat-value truncate" :title="preview.accountName">{{ preview.accountName }}</div>
                <div class="stat-sub">{{ preview.accountNumber }}</div>
             </div>
             <div class="stat-box">
                <div class="stat-label">Records</div>
                <div class="stat-value">{{ preview.totalRecords }}</div>
             </div>
             <div class="stat-box">
                 <div class="stat-label">Duplicates</div>
                 <div class="stat-value" :class="duplicateCount > 0 ? 'text-warning' : 'text-success'">{{ duplicateCount }}</div>
             </div>
          </div>
          
          <div class="table-container preview-table mb-lg">
            <table class="data-table">
              <thead>
                <tr>
                  <th class="sticky-header">Date</th>
                  <th class="sticky-header">Type</th>
                  <th class="sticky-header">Asset</th>
                  <th class="sticky-header text-right">Units</th>
                  <th class="sticky-header text-right">Amount</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(rec, i) in preview.previewRecords" :key="i" :class="{ 'tr-duplicate': rec.isDuplicate }">
                  <td class="whitespace-nowrap">{{ rec.date ? new Date(rec.date).toLocaleDateString() : '-' }}</td>
                  <td><span class="badge" :class="getTypeBadge(rec.type)">{{ rec.type }}</span></td>
                  <td>
                    <div class="font-medium text-sm">{{ rec.asset }}</div>
                    <div class="text-xs text-muted">{{ rec.code }}</div>
                    <div v-if="rec.isDuplicate" class="text-xs text-warning mt-1">Duplicate</div>
                  </td>
                  <td class="text-right font-mono">{{ rec.units }}</td>
                  <td class="text-right font-mono">{{ formatCurrency(rec.amount) }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Action Bar -->
          <div class="action-bar card bg-muted-light">
             <label class="checkbox-wrapper">
               <input type="checkbox" v-model="includeDuplicates" class="custom-checkbox">
               <span class="text-sm font-medium">Overwriting existing duplicates</span>
             </label>
             <div class="flex gap-md">
               <button @click="preview = null" class="btn btn-secondary">Cancel</button>
               <button @click="confirmImport" class="btn btn-primary shadow-sm" :disabled="importing">
                 Confirm & Import
               </button>
             </div>
          </div>
        </div>
      </div>
    </div>

    <!-- History Section -->
    <div v-if="!preview" class="history-section animate-fade-in-up">
      <div class="flex-between mb-md items-center">
         <div>
            <h2 class="text-lg font-bold">Import History</h2>
            <p class="text-muted text-sm">Manage previous uploads</p>
         </div>
         <button @click="fetchHistory" class="btn btn-sm btn-outline" title="Refresh list">
           <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.3"/></svg>
           Refresh
         </button>
      </div>

      <div v-if="history.length === 0" class="empty-state card flex-center flex-column p-xl text-muted">
         <svg class="mb-md opacity-50" xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="12" y1="18" x2="12" y2="12"/><line x1="9" y1="15" x2="15" y2="15"/></svg>
         <p>No import history found.</p>
      </div>

      <div v-else class="card p-0 overflow-hidden shadow-sm">
        <div class="table-container history-table">
          <table class="w-full data-table">
            <thead>
              <tr class="bg-muted-lighter">
                <th class="pl-lg">Date</th>
                <th>File Name</th>
                <th>Type</th>
                <th>Records</th>
                <th>Status</th>
                <th class="text-right pr-lg">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(file, i) in history" :key="file.id" :class="{ 'opacity-60': !file.isActive, 'bg-striped': i % 2 === 1 }">
                <td class="whitespace-nowrap pl-lg font-mono text-sm">{{ new Date(file.uploadedAt).toLocaleDateString() }}</td>
                <td>
                  <div class="font-medium text-sm text-primary truncate max-w-xs" :title="file.fileName">{{ file.fileName }}</div>
                  <div class="text-xs text-muted">{{ file.fileType }} • {{ (file.fileSizeBytes / 1024).toFixed(1) }} KB</div>
                </td>
                <td><span class="badge badge-sm">{{ file.fileType === 'PortfolioValuation' ? 'Valuation' : 'Transactions' }}</span></td>
                <td class="font-mono text-sm">{{ file.recordsProcessed }}</td>
                <td>
                  <span class="status-badge" :class="file.isActive ? 'active' : 'inactive'">
                    {{ file.isActive ? 'Active' : 'Unloaded' }}
                  </span>
                </td>
                <td class="text-right pr-lg">
                  <div class="flex-end gap-sm action-buttons">
                    <button @click="downloadFile(file)" class="btn-icon-bg" title="Download Original CSV">
                      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
                    </button>
                    <button @click="toggleStatus(file)" class="btn-icon-bg" :class="file.isActive ? 'text-warning' : 'text-success'" :title="file.isActive ? 'Unload Data (Hide)' : 'Reload Data (Show)'">
                      <svg v-if="file.isActive" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>
                      <svg v-else xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.3"/></svg>
                    </button>
                    <button @click="deleteFile(file)" class="btn-icon-bg text-danger hover-danger" title="Delete Permanently">
                      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/><line x1="10" y1="11" x2="10" y2="17"/><line x1="14" y1="11" x2="14" y2="17"/></svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import api from '../services/api';
import { useRouter } from 'vue-router';
import MoneyBoxLoader from '../components/MoneyBoxLoader.vue';

const portfolios = ref([]);
const selectedPortfolioId = ref('');
const file = ref(null);
const preview = ref(null);
const fileContentBase64 = ref('');
const importing = ref(false);
const includeDuplicates = ref(false);
const history = ref([]);
const router = useRouter();

const duplicateCount = computed(() => {
  return preview.value?.previewRecords?.filter(r => r.isDuplicate).length || 0;
});

const fetchPortfolios = async () => {
  try {
    const res = await api.get('/portfolios');
    portfolios.value = res.data;
    if(portfolios.value.length > 0) {
      selectedPortfolioId.value = portfolios.value[0].id;
    }
  } catch (e) {
    console.error("Failed to fetch portfolios", e);
  }
};

const fetchHistory = async () => {
  if (!selectedPortfolioId.value) return;
  try {
    const res = await api.get(`/import/history/${selectedPortfolioId.value}`);
    history.value = res.data;
  } catch (err) {
    console.error("Failed to load history", err);
  }
};

watch(selectedPortfolioId, (newVal) => {
  if (newVal) fetchHistory();
});

const handleFileChange = (e) => {
  if (e.target.files.length > 0) {
    file.value = e.target.files[0];
  }
};

const handleDrop = (e) => {
  if (e.dataTransfer.files.length > 0) {
    const droppedFile = e.dataTransfer.files[0];
    if (droppedFile.name.endsWith('.csv')) {
      file.value = droppedFile;
    } else {
      alert('Please upload a CSV file.');
    }
  }
};

const uploadFile = () => {
  if(!file.value) return;
  const reader = new FileReader();
  reader.onload = async (e) => {
    fileContentBase64.value = e.target.result.split(',')[1];
    try {
      const res = await api.post('/import/preview', {
        portfolioId: selectedPortfolioId.value,
        fileContentBase64: fileContentBase64.value,
        fileName: file.value.name
      });
      preview.value = res.data;
    } catch (err) {
      alert('Error previewing file: ' + (err.response?.data?.message || err.message));
    }
  };
  reader.readAsDataURL(file.value);
};

const confirmImport = async () => {
  importing.value = true;
  try {
    await api.post('/import/confirm', {
      portfolioId: selectedPortfolioId.value,
      fileContentBase64: fileContentBase64.value,
      fileName: file.value.name,
      includeDuplicates: includeDuplicates.value
    });
    preview.value = null;
    file.value = null;
    fetchHistory();
    // alert('Import successful!'); // Consider a toast instead
  } catch (err) {
    alert('Import failed: ' + err.message);
  } finally {
    importing.value = false;
  }
};

const downloadFile = async (file) => {
  try {
      const response = await api.get(`/import/download/${file.id}`, { responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', file.fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
  } catch (err) {
      alert('Failed to download: ' + err.message);
  }
};

const toggleStatus = async (file) => {
    const action = file.isActive ? 'unload' : 'reload';
    if (!confirm(`Are you sure you want to ${action} this file? This will ${action === 'unload' ? 'remove' : 'restore'} the associated data.`)) return;
    
    try {
        await api.put(`/import/${file.id}/${action}`);
        await fetchHistory();
    } catch (err) {
        alert(`Failed to ${action}: ` + err.message);
    }
};

const deleteFile = async (file) => {
    if (!confirm('Permanently delete this file? This cannot be undone.')) return;
    try {
        await api.delete(`/import/${file.id}`);
        await fetchHistory();
    } catch (err) {
        alert('Failed to delete: ' + err.message);
    }
};

const getTypeBadge = (type) => {
  if (!type) return 'badge-neutral';
  const t = type.toLowerCase();
  if (t === 'buy' || t === 'deposit') return 'text-success bg-success-light';
  if (t === 'sell' || t === 'withdrawal') return 'text-danger bg-danger-light';
  return 'badge-neutral';
};

const formatCurrency = (val) => {
  return new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD' }).format(val);
};

onMounted(fetchPortfolios);
</script>

<style scoped>
.content-container {
  max-width: 1000px;
  margin: 0 auto;
}

.form-select {
  width: 100%;
  padding: var(--spacing-md);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-bg-secondary);
  color: var(--color-text-primary);
  font-size: var(--font-size-base);
}

.upload-area {
  border: 2px dashed var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-xl);
  text-align: center;
  transition: all var(--transition-fast);
  cursor: pointer;
}

.upload-area:hover {
  border-color: var(--color-accent);
  background: var(--color-bg-elevated);
}

.upload-label {
  cursor: pointer;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.upload-icon {
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-md);
}

.upload-text {
  font-size: var(--font-size-base);
  color: var(--color-text-secondary);
}

.loading-state {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 200px;
}

.actions-group {
  display: flex;
  gap: var(--spacing-lg);
  align-items: center;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: var(--font-size-sm);
  cursor: pointer;
}

.btn-group {
  display: flex;
  gap: var(--spacing-sm);
}

/* Table Styles */
.table-container {
  overflow-x: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
}

table {
  width: 100%;
  border-collapse: collapse;
}

th {
  background: var(--color-bg-elevated);
  font-weight: 600;
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: var(--spacing-md);
}

td {
  padding: var(--spacing-md);
  border-top: 1px solid var(--color-border-subtle);
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
}

.badge {
  display: inline-block;
  padding: 2px 8px;
  background: var(--color-bg-elevated);
  font-size: 11px;
  border-radius: var(--radius-full);
  font-weight: 600;
  text-transform: uppercase;
}

.bg-warning-light {
  background-color: rgba(217, 119, 6, 0.1);
}

.btn-icon {
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 4px;
  color: var(--color-text-muted);
  transition: color var(--transition-fast);
}

.btn-icon:hover {
  color: var(--color-text-primary);
}

.text-danger { color: var(--color-danger); }
.text-success { color: var(--color-success); }
.text-warning { color: var(--color-warning); }

.bg-success-light { background-color: #dcfce7; color: #166534; }
.bg-danger-light { background-color: #fee2e2; color: #991b1b; }
.badge-neutral { background-color: var(--color-bg-elevated); color: var(--color-text-muted); }

.status-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  margin-right: 6px;
}
.bg-success { background-color: var(--color-success); }
.bg-muted { background-color: var(--color-text-muted); }

/* Mobile */
@media (max-width: 640px) {
  .wrap-mobile {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-md);
  }
  
  .actions-group {
    width: 100%;
    justify-content: space-between;
    flex-wrap: wrap;
  }
}

/* Utilities */
.hidden { display: none; }
.text-primary { color: var(--color-accent); }
.font-medium { font-weight: 500; }
.p-xl { padding: var(--spacing-xl); }
.text-xl { font-size: var(--font-size-xl); }
.text-lg { font-size: var(--font-size-lg); }
.font-bold { font-weight: 700; }
.font-mono { font-family: var(--font-mono); }
.truncate { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.max-w-xs { max-width: 200px; }
.bg-muted-light { background: var(--color-bg-secondary); }
.bg-muted-lighter { background: #f8fafc; }
.bg-striped { background: #fafafa; }
.opacity-60 { opacity: 0.6; }
.pl-lg { padding-left: var(--spacing-lg); }
.pr-lg { padding-right: var(--spacing-lg); }
.shadow-sm { box-shadow: var(--shadow-sm); }
.flex-column { flex-direction: column; }
.flex-center { display: flex; align-items: center; justify-content: center; }

/* Stats Grid */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: var(--spacing-md);
}

.stat-box {
  background: var(--color-bg-secondary);
  padding: var(--spacing-md);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border-subtle);
}

.stat-label {
  font-size: 0.75rem;
  color: var(--color-text-muted);
  text-transform: uppercase;
  font-weight: 600;
  margin-bottom: 4px;
}

.stat-value {
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--color-text-primary);
}

.stat-sub {
  font-size: 0.75rem;
  color: var(--color-text-muted);
}

/* Data Table */
.data-table th {
  background: var(--color-bg-secondary);
  font-weight: 600;
  color: var(--color-text-muted);
  text-transform: uppercase;
  font-size: 0.7rem;
  padding: var(--spacing-sm) var(--spacing-md);
  white-space: nowrap;
}

.data-table td {
  padding: var(--spacing-sm) var(--spacing-md);
  border-bottom: 1px solid var(--color-border-subtle);
}

.sticky-header {
  position: sticky;
  top: 0;
  z-index: 10;
}

.tr-duplicate {
  background-color: #fffbeb; /* Warning light */
}
.tr-duplicate:hover {
  background-color: #fef3c7;
}

/* Action Bar */
.action-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-md) var(--spacing-lg);
  border-top: 1px solid var(--color-border);
  position: sticky;
  bottom: 0;
  background: white;
  z-index: 20;
}

.checkbox-wrapper {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  cursor: pointer;
}

.custom-checkbox {
  width: 16px;
  height: 16px;
  accent-color: var(--color-primary);
}

/* Status Badge */
.status-badge {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: 99px;
  font-size: 0.7rem;
  font-weight: 600;
  text-transform: uppercase;
}
.status-badge.active { background: #dcfce7; color: #166534; }
.status-badge.inactive { background: #f1f5f9; color: #64748b; }

.badge-sm { font-size: 0.65rem; padding: 1px 6px; }

/* Icon Buttons */
.btn-icon-bg {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-md);
  background: var(--color-bg-secondary);
  color: var(--color-text-muted);
  border: 1px solid transparent;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-icon-bg:hover {
  background: var(--color-bg-elevated);
  color: var(--color-primary);
  border-color: var(--color-border);
}

.hover-danger:hover {
  color: var(--color-danger);
  background: #fee2e2;
}

.action-buttons {
  min-width: 120px; /* Ensure space for 3 buttons */
}
</style>
