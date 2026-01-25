<template>
  <div class="admin-container">
    <h1>Admin Settings</h1>
    
    <div class="tabs">
      <button 
        v-for="tab in tabs" 
        :key="tab.id"
        :class="['tab-btn', { active: activeTab === tab.id }]"
        @click="activeTab = tab.id"
      >
        {{ tab.label }}
      </button>
    </div>

    <div class="tab-content">
      <!-- Price Update Settings Tab -->
      <div v-if="activeTab === 'settings'" class="settings-panel">
        <div class="card">
          <h2>Price Update Configuration</h2>
          <div class="form-group">
            <label>Update Interval (minutes)</label>
            <div class="input-group">
              <input 
                v-model.number="priceSettings.updateIntervalMinutes" 
                type="number" 
                min="1" 
                max="1440"
                class="input"
              />
              <button @click="savePriceSettings" class="btn btn-primary">Save Interval</button>
            </div>
          </div>

          <div class="form-group">
            <label>Last Update</label>
            <p class="last-update-text">{{ lastUpdateDisplay }}</p>
          </div>

          <button @click="triggerRefresh" :disabled="refreshing" class="btn btn-success">
            <span v-if="!refreshing">🔄 Refresh Prices Now</span>
            <span v-else>Refreshing...</span>
          </button>
        </div>
      </div>

      <!-- Price Source Priority Tab -->
      <div v-if="activeTab === 'sources'" class="sources-panel">
        <div class="card">
          <h2>Price Source Priority</h2>
          <p class="help-text">Drag to reorder priority (top = first tried)</p>
          
            <div class="sources-list" @dragover.prevent @drop="onDrop">
              <div 
                v-for="(source, index) in priceSources" 
                :key="source.id"
                class="source-item"
                draggable="true"
                @dragstart="onDragStart(index)"
                @dragenter="onDragEnter(index)"
              >
                <div class="source-header">
                  <span class="drag-handle">≡</span>
                  <span class="priority-badge">{{ index + 1 }}</span>
                <strong>{{ source.name }}</strong>
                <span class="toggle">
                  <label class="switch">
                    <input type="checkbox" v-model="source.isEnabled" @change="toggleSource(source)">
                    <span class="slider"></span>
                  </label>
                </span>
              </div>
              
              <div class="source-details">
                <span class="code">{{ source.code }}</span>
                <span>Rate: {{ source.rateLimitPerMinute }}/min</span>
              </div>

              <div class="source-actions">
                <div v-if="source.code === 'YAHOO'" class="text-muted text-sm">
                  No API key required for Yahoo Finance.
                </div>
                <div v-else-if="source.hasApiKey && !editingApiKeys[source.id]" class="api-key-status">
                  <span class="key-indicator">Saved Key: ••••••••••••••••</span>
                  <button @click="editingApiKeys[source.id] = true" class="btn-sm btn-outline">Change</button>
                </div>
                <div v-else class="api-key-input-group">
                  <input 
                    v-model="apiKeys[source.id]" 
                    type="password" 
                    placeholder="Enter API Key"
                    class="input-sm"
                  />
                  <div class="btn-group">
                    <button @click="saveApiKey(source)" class="btn-sm btn-save-key">Save Key</button>
                    <button v-if="source.hasApiKey" @click="cancelEditKey(source.id)" class="btn-sm btn-outline">Cancel</button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="priority-actions" v-if="priorityModified">
            <button @click="savePriorities" class="btn btn-primary">Save New Priority Order</button>
          </div>
        </div>
      </div>

      <!-- Asset Price Sources Tab -->
      <div v-if="activeTab === 'assets'" class="assets-panel">
        <div class="card">
          <h2>Asset Price Symbol Mapping</h2>
          <p class="help-text">Manually configure pricing symbols and sources for each asset</p>

          <div class="table-container">
            <table class="assets-table">
              <thead>
                <tr>
                  <th>Asset Code</th>
                  <th>Name</th>
                  <th>Pricing Code</th>
                  <th>Source</th>
                  <th>Current Price</th>
                  <th>Last Updated</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                <template v-for="asset in assetPriceSources" :key="asset.assetId">
                  <tr v-if="editableAssets[asset.assetId]">
                    <td class="font-medium text-main">{{ asset.symbol }}</td>
                    <td class="text-sm text-muted hidden-sm">{{ asset.name }}</td>
                    <td>
                      <div class="pricing-code-cell">
                        <input 
                          v-model="editableAssets[asset.assetId].customSymbol" 
                          type="text" 
                          :placeholder="asset.symbol"
                          class="input-premium"
                          @input="markAssetModified(asset.assetId)"
                        />
                        <div v-if="!editableAssets[asset.assetId].customSymbol" class="text-xs text-muted mt-1 flex items-center gap-1">
                           <span class="status-dot"></span> Uses Default
                        </div>
                        <div v-else class="text-xs text-primary mt-1 flex items-center gap-1">
                           <span class="status-dot active"></span> Custom Override
                        </div>
                      </div>
                    </td>
                    <td>
                      <div class="select-wrapper">
                        <select 
                          v-model="editableAssets[asset.assetId].sourceId" 
                          @change="markAssetModified(asset.assetId)"
                          class="select-premium"
                        >
                          <option value="">Default (Priority Order)</option>
                          <option v-for="source in priceSources.filter(s => s.isEnabled)" :key="source.id" :value="source.id">
                            {{ source.name }}
                          </option>
                        </select>
                      </div>
                    </td>
                    <td>
                      <div class="price-display">
                        <div v-if="asset.currentPrice" class="price-tag">
                          ${{ asset.currentPrice.toFixed(2) }}
                        </div>
                        <div v-else class="price-error">
                          <span class="error-icon">⚠️</span> No Data
                        </div>
                        
                        <span v-if="asset.currentSourceUsed" class="source-badge">
                          {{ asset.currentSourceUsed }}
                        </span>
                      </div>
                    </td>
                    <td>
                      <div class="flex flex-col">
                        <span class="text-sm font-medium">{{ formatDate(asset.lastUpdated) }}</span>
                        <span class="text-xs text-muted" v-if="!isOld(asset.lastUpdated)">Just now</span>
                        <span class="text-xs text-danger" v-else>Outdated</span>
                      </div>
                    </td>
                    <td>
                      <div class="actions-row">
                        <button 
                          @click="syncAssetPrice(asset.assetId)"
                          class="btn-icon btn-sync"
                          :class="{ 'spinning': syncingAssets[asset.assetId] }"
                          :disabled="syncingAssets[asset.assetId]"
                          title="Sync Price"
                        >
                          <span v-if="syncingAssets[asset.assetId]">⌛</span>
                          <span v-else>🔄</span>
                        </button>

                        <button 
                          v-if="editableAssets[asset.assetId].modified" 
                          @click="saveAssetMapping(asset.assetId)"
                          class="btn-icon btn-save"
                          title="Save Changes"
                        >
                          💾
                        </button>
                        
                        <button 
                          v-if="asset.overrideSourceId || asset.customSymbol" 
                          @click="resetAssetMapping(asset.assetId)"
                          class="btn-icon btn-reset"
                          title="Reset to Default"
                        >
                          ✕
                        </button>
                      </div>
                    </td>
                  </tr>
                </template>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue';
import api from '../services/api';

const activeTab = ref('settings');
const tabs = [
  { id: 'settings', label: 'Price Update Settings' },
  { id: 'sources', label: 'Price Source Priority' },
  { id: 'assets', label: 'Asset Price Sources' }
];

const priceSettings = ref({ updateIntervalMinutes: 15 });
const priceSources = ref([]);
const assetPriceSources = ref([]);
const editableAssets = ref({});
const apiKeys = ref({});
const editingApiKeys = ref({});
const refreshing = ref(false);
const syncingAssets = ref({});
const lastUpdate = ref(null);

const lastUpdateDisplay = computed(() => {
  if (!lastUpdate.value) return 'Never';
  const date = new Date(lastUpdate.value);
  // Check if date is valid and not epoch
  if (isNaN(date.getTime()) || date.getFullYear() < 2000) return 'Never';
  return date.toLocaleString();
});

onMounted(async () => {
  await loadPriceSettings();
  await loadPriceSources();
  await loadAssetPriceSources();
});

async function loadPriceSettings() {
  try {
    const { data } = await api.get('/admin/price-settings');
    priceSettings.value.updateIntervalMinutes = data.updateIntervalMinutes;
    lastUpdate.value = data.lastUpdateTime;
  } catch (error) {
    console.error('Failed to load price settings', error);
  }
}

async function savePriceSettings() {
  try {
    await api.put('/admin/price-settings/interval', {
      intervalMinutes: priceSettings.value.updateIntervalMinutes
    });
    alert('Price update interval saved!');
  } catch (error) {
    alert('Failed to save settings');
  }
}

async function triggerRefresh() {
  refreshing.value = true;
  try {
    await api.post('/admin/price-refresh');
    alert('Price refresh triggered! This may take a few moments.');
    setTimeout(() => loadPriceSettings(), 3000);
  } catch (error) {
    alert('Failed to trigger refresh');
  } finally {
    refreshing.value = false;
  }
}

async function loadPriceSources() {
  try {
    const { data } = await api.get('/admin/price-sources');
    priceSources.value = data;
    priorityModified.value = false;
  } catch (error) {
    console.error('Failed to load price sources', error);
  }
}

const draggedItemIndex = ref(null);
const priorityModified = ref(false);

function onDragStart(index) {
  draggedItemIndex.value = index;
}

function onDragEnter(index) {
  if (draggedItemIndex.value === index) return;
  
  const items = [...priceSources.value];
  const draggedItem = items[draggedItemIndex.value];
  
  items.splice(draggedItemIndex.value, 1);
  items.splice(index, 0, draggedItem);
  
  priceSources.value = items;
  draggedItemIndex.value = index;
  priorityModified.value = true;
}

function onDrop() {
  draggedItemIndex.value = null;
}

async function savePriorities() {
  try {
    const updates = priceSources.value.map((source, index) => ({
      sourceId: source.id,
      newPriority: index + 1
    }));
    
    await api.put('/admin/price-sources/priority', updates);
    alert('Priority order saved!');
    priorityModified.value = false;
  } catch (error) {
    alert('Failed to save priority order');
  }
}

async function toggleSource(source) {
  try {
    await api.put(`/admin/price-sources/${source.id}/toggle`);
  } catch (error) {
    alert('Failed to toggle source');
    source.isEnabled = !source.isEnabled; // Revert
  }
}

async function saveApiKey(source) {
  if (!apiKeys.value[source.id]) {
    alert('Please enter an API key');
    return;
  }
  
  try {
    await api.put(`/admin/price-sources/${source.id}/apikey`, {
      apiKey: apiKeys.value[source.id]
    });
    alert(`API key saved for ${source.name}`);
    source.hasApiKey = true;
    apiKeys.value[source.id] = '';
    editingApiKeys.value[source.id] = false;
  } catch (error) {
    alert('Failed to save API key');
  }
}

function cancelEditKey(sourceId) {
  editingApiKeys.value[sourceId] = false;
  apiKeys.value[sourceId] = '';
}

async function loadAssetPriceSources() {
  try {
    const { data } = await api.get('/admin/assets/price-sources');
    assetPriceSources.value = data;
    
    // Initialize editable assets state
    editableAssets.value = {};
    data.forEach(asset => {
      editableAssets.value[asset.assetId] = {
        customSymbol: asset.customSymbol || '',
        sourceId: asset.overrideSourceId || '',
        modified: false
      };
    });
  } catch (error) {
    console.error('Failed to load asset price sources', error);
  }
}

function markAssetModified(assetId) {
  editableAssets.value[assetId].modified = true;
}

async function saveAssetMapping(assetId) {
  const mapping = editableAssets.value[assetId];
  try {
    await api.put(`/admin/assets/${assetId}/price-source`, {
      sourceId: mapping.sourceId || null,
      customSymbol: mapping.customSymbol || null
    });
    alert('Asset mapping saved!');
    mapping.modified = false;
    // Refresh to show updated data
    await loadAssetPriceSources();
  } catch (error) {
    alert('Failed to save asset mapping');
  }
}

async function resetAssetMapping(assetId) {
  try {
    await api.put(`/admin/assets/${assetId}/price-source`, {
      sourceId: null,
      customSymbol: null
    });
    alert('Asset mapping reset to default!');
    editableAssets.value[assetId].customSymbol = '';
    editableAssets.value[assetId].sourceId = '';
    editableAssets.value[assetId].modified = false;
    // Refresh to show updated data
    await loadAssetPriceSources();
  } catch (error) {
    alert('Failed to reset asset mapping');
  }
}

async function syncAssetPrice(assetId) {
  syncingAssets.value[assetId] = true;
  try {
    const { data } = await api.post(`/admin/assets/${assetId}/sync-price`);
    // Update the asset in the list
    const index = assetPriceSources.value.findIndex(a => a.assetId === assetId);
    if (index !== -1) {
      assetPriceSources.value[index] = data;
    }
  } catch (error) {
    console.error('Failed to sync asset price', error);
    alert('Failed to sync price for this asset');
  } finally {
    syncingAssets.value[assetId] = false;
  }
}

async function setAssetSource(assetId, sourceId) {
  try {
    await api.put(`/admin/assets/${assetId}/price-source`, {
      sourceId: sourceId || null
    });
    alert('Asset price source updated!');
  } catch (error) {
    alert('Failed to update asset source');
  }
}

function formatDate(date) {
  if (!date) return 'Never';
  const d = new Date(date);
  if (isNaN(d.getTime()) || d.getFullYear() < 2000) return 'Never';
  return d.toLocaleString();
}

function isOld(date) {
  if (!date) return true;
  const lastUpdate = new Date(date);
  const now = new Date();
  return (now - lastUpdate) > 24 * 60 * 60 * 1000;
}
</script>

<style scoped>
.admin-container {
  max-width: 1400px; /* Increased max width for tabular data */
  margin: 0 auto;
}

h1 {
  margin-bottom: var(--spacing-lg);
  color: var(--color-text);
  font-weight: 700;
  letter-spacing: -0.5px;
}

/* Premium Tabs */
.tabs {
  display: flex;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-xl);
  border-bottom: 1px solid var(--color-border);
  padding-bottom: 0;
}

.tab-btn {
  padding: 12px 16px;
  background: none;
  border: none;
  cursor: pointer;
  font-weight: 600;
  color: var(--color-text-muted);
  transition: all 0.2s ease;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  font-size: 0.95rem;
}

.tab-btn:hover {
  color: var(--color-primary);
  background: rgba(var(--color-primary-rgb), 0.02);
}

.tab-btn.active {
  color: var(--color-primary);
  border-bottom-color: var(--color-primary);
}

.card {
  background: white;
  border-radius: 16px; /* Larger radius for modern feel */
  padding: var(--spacing-xl);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.02), 0 2px 4px -1px rgba(0, 0, 0, 0.02);
  border: 1px solid var(--color-border);
}

.card h2 {
  margin-bottom: var(--spacing-md);
  font-size: 1.25rem;
  font-weight: 700;
}

.help-text {
  color: var(--color-text-muted);
  font-size: 0.9rem;
  margin-bottom: var(--spacing-xl);
}

/* Table Refinements */
.assets-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
}

.assets-table th {
  text-align: left;
  padding: 16px;
  color: var(--color-text-muted);
  font-weight: 600;
  font-size: 0.85rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border-bottom: 1px solid var(--color-border);
  background: #fcfcfc;
}

.assets-table td {
  padding: 16px;
  border-bottom: 1px solid #f3f4f6;
  vertical-align: middle;
  transition: background-color 0.2s;
}

.assets-table tr:hover td {
  background-color: #fafafa;
}

.font-medium { font-weight: 500; }
.text-main { color: var(--color-text-main); }
.text-sm { font-size: 0.875rem; }
.text-xs { font-size: 0.75rem; }
.text-muted { color: var(--color-text-muted); }
.text-primary { color: var(--color-primary); }
.text-danger { color: #ef4444; }

/* Input & Select Restyling */
.input-premium, .select-premium {
  padding: 8px 12px;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  font-size: 0.9rem;
  width: 100%;
  transition: all 0.2s border-color;
  background-color: white;
}

.input-premium:focus, .select-premium:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(var(--color-primary-rgb), 0.1);
}

.select-wrapper {
  position: relative;
  width: 200px;
}

/* Pricing Code Cell */
.pricing-code-cell {
  width: 140px;
}

.status-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background-color: #d1d5db;
  display: inline-block;
}

.status-dot.active {
  background-color: var(--color-primary);
}

/* Badges */
.price-display {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.price-tag {
  font-feature-settings: "tnum";
  font-variant-numeric: tabular-nums;
  font-weight: 600;
  color: #059669; /* Emerald 600 */
}

.price-error {
  color: #ef4444;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  gap: 4px;
}

.source-badge {
  display: inline-flex;
  font-size: 0.7rem;
  padding: 2px 8px;
  background-color: #f3f4f6;
  color: #6b7280;
  border-radius: 12px;
  width: fit-content;
  font-weight: 500;
  border: 1px solid #e5e7eb;
}

/* Action Buttons */
.actions-row {
  display: flex;
  gap: 8px;
  justify-content: flex-start;
  align-items: center;
}

.btn-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  cursor: pointer;
  transition: all 0.2s;
  background: transparent;
  font-size: 1rem;
}

.btn-icon:hover {
  background-color: #f3f4f6;
  transform: translateY(-1px);
}

.btn-sync {
  color: #d97706; /* Amber 600 */
}

.btn-sync:hover {
  background-color: #fffbeb;
  color: #b45309;
}

.btn-save {
  color: #059669; /* Emerald 600 */
}

.btn-save:hover {
  background-color: #ecfdf5;
  color: #047857;
}

.btn-reset {
  color: #9ca3af;
}

.btn-reset:hover {
  background-color: #fef2f2;
  color: #ef4444;
}

.spinning span {
  display: inline-block;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

@media (max-width: 768px) {
  .hidden-sm { display: none; }
}

/* Keep previous basic utility classes minimally */
.flex { display: flex; }
.flex-col { flex-direction: column; }
.mt-1 { margin-top: 4px; }
.gap-1 { gap: 4px; }
.items-center { align-items: center; }

.form-group {
  margin-bottom: var(--spacing-lg);
}

.form-group label {
  display: block;
  font-weight: 500;
  margin-bottom: var(--spacing-sm);
}

.input, .input-sm, .select-sm {
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: 1rem;
}

.input {
  width: 100%;
  max-width: 200px;
}

.input-sm {
  padding: 4px 8px;
  font-size: 0.9rem;
  width: 100%;
}

.select-sm {
  padding: 4px 8px;
  font-size: 0.9rem;
  width: auto;
}

.input-group {
  display: flex;
  gap: var(--spacing-sm);
  align-items: center;
}

.last-update-text {
  font-size: 0.95rem;
  color: var(--color-text);
  margin: 0;
}

.btn {
  padding: var(--spacing-sm) var(--spacing-lg);
  border: none;
  border-radius: var(--radius-md);
  cursor: pointer;
  font-weight: 500;
  transition: all 0.2s;
}

.btn-primary {
  background: var(--color-primary);
  color: white;
}

.btn-success {
  background: #10b981;
  color: white;
  margin-top: var(--spacing-lg);
}

.btn:hover {
  opacity: 0.9;
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-sm {
  padding: 6px 12px;
  font-size: 0.85rem;
  background: #3b82f6; /* Clear blue background */
  color: white;
  border: none;
  border-radius: var(--radius-md);
  cursor: pointer;
  white-space: nowrap;
  flex-shrink: 0;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.btn-sm:hover:not(:disabled) {
  background: #2563eb;
}

.btn-sm:disabled {
  background: #cbd5e1;
  cursor: not-allowed;
  opacity: 0.7;
}

.sources-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.source-item {
  padding: var(--spacing-md);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: #f9fafb;
}

.source-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-sm);
}

.priority-badge {
  background: var(--color-primary);
  color: white;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8rem;
  font-weight: bold;
}

.drag-handle {
  cursor: grab;
  color: var(--color-text-muted);
  font-size: 1.2rem;
  margin-right: var(--spacing-sm);
  user-select: none;
}

.source-item[draggable="true"]:active {
  cursor: grabbing;
}

.source-item {
  transition: transform 0.2s ease;
}

.priority-actions {
  margin-top: var(--spacing-lg);
  display: flex;
  justify-content: flex-end;
}

.toggle {
  margin-left: auto;
}

.switch {
  position: relative;
  display: inline-block;
  width: 40px;
  height: 20px;
}

.switch input {
  opacity: 0;
  width: 0;
  height: 0;
}

.slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #ccc;
  transition: .4s;
  border-radius: 20px;
}

.slider:before {
  position: absolute;
  content: "";
  height: 14px;
  width: 14px;
  left: 3px;
  bottom: 3px;
  background-color: white;
  transition: .4s;
  border-radius: 50%;
}

input:checked + .slider {
  background-color: var(--color-primary);
}

input:checked + .slider:before {
  transform: translateX(20px);
}

.source-details {
  display: flex;
  gap: var(--spacing-md);
  font-size: 0.9rem;
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-sm);
}

.code {
  font-family: monospace;
  background: white;
  padding: 2px 6px;
  border-radius: 4px;
}

.source-actions {
  display: flex;
  gap: var(--spacing-sm);
  margin-top: var(--spacing-sm);
}

.table-container {
  overflow-x: auto;
}

.badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 0.8rem;
  background: #e0e7ff;
  color: #4f46e5;
}

.has-key {
  color: #10b981;
}

.btn-save {
  background: #10b981 !important;
  color: white !important;
  margin-right: 4px;
}

.btn-reset {
  background: #ef4444 !important;
  color: white !important;
}

.btn-sync {
  background: #f59e0b !important; /* Amber for sync */
  color: white !important;
}

.gap-xs {
  gap: 4px;
}

.help-text-sm {
  font-size: 0.75rem;
  color: var(--color-text-muted);
  margin-left: 4px;
}

.price-value {
  font-weight: 600;
  color: #10b981;
}

.api-key-status {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  background: #f0fdf4;
  padding: 8px 12px;
  border-radius: var(--radius-md);
  border: 1px solid #dcfce7;
}

.key-indicator {
  color: #166534;
  font-family: monospace;
  font-size: 0.9rem;
}

.api-key-input-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  width: 100%;
}

.btn-group {
  display: flex;
  gap: var(--spacing-sm);
}

.btn-outline {
  background: transparent !important;
  border: 1px solid var(--color-border) !important;
  color: var(--color-text) !important;
}

.btn-outline:hover {
  background: #f3f4f6 !important;
}

.btn-save-key {
  background: #3b82f6 !important;
}

.timestamp {
  font-size: 0.85rem;
  color: var(--color-text-muted);
}

.text-muted {
  color: var(--color-text-muted);
}

.price-error {
  display: flex;
  align-items: center;
  gap: 4px;
  color: #ef4444;
  font-weight: 500;
}

.error-icon {
  font-size: 1.1rem;
}

.source-badge {
  background: #f3f4f6;
  color: #6b7280;
  font-size: 0.7rem;
  padding: 2px 6px;
  border-radius: 4px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.025em;
}

.timestamp-container {
  display: flex;
  flex-direction: column;
}

.timestamp-old {
  color: #ef4444;
  font-weight: 500;
}

.space-between {
  justify-content: space-between;
}

.w-full { width: 100%; }
.h-fit { height: fit-content; }
</style>
