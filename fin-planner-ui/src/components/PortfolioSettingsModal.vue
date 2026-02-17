<template>
  <Teleport to="body">
  <div v-if="isOpen" class="modal-backdrop" @click.self="close">
    <div class="modal card p-xl slide-in">
        <div class="flex-between mb-lg">
          <h2>Portfolio Settings</h2>
          <button @click="close" class="btn-icon">×</button>
        </div>

        <form @submit.prevent="save">
            <!-- Name -->
            <div class="form-group mb-lg">
                <label class="block mb-sm text-sm font-bold text-muted">Portfolio Name</label>
                <input v-model="form.name" type="text" class="input w-full" required />
            </div>

            <!-- Benchmark -->
            <div class="form-group mb-xl">
                <label class="block mb-sm text-sm font-bold text-muted">Benchmark Index</label>
                <p class="text-xs text-muted mb-sm">Select an asset to compare your portfolio against (e.g. VAS, IVV).</p>
                
                <!-- Search -->
                <div class="relative">
                    <input 
                        v-model="searchQuery" 
                        @input="searchAssets"
                        type="text" 
                        class="input w-full" 
                        placeholder="Search for an index (e.g. VAS)..."
                    />
                    <div v-if="results.length > 0" class="dropdown-list">
                        <div 
                            v-for="asset in results" 
                            :key="asset.id" 
                            class="dropdown-item"
                            @click="selectBenchmark(asset)"
                        >
                            <span class="font-bold">{{ asset.symbol }}</span> - {{ asset.name }}
                        </div>
                    </div>
                </div>

                <!-- Selected -->
                <div v-if="selectedBenchmark" class="mt-sm p-sm bg-gray-50 flex-between border-radius text-sm">
                    <div>
                        <span class="font-bold text-primary">{{ selectedBenchmark.symbol }}</span>
                         <span class="text-muted ml-sm">{{ selectedBenchmark.name }}</span>
                    </div>
                    <button type="button" @click="clearBenchmark" class="text-danger text-xs hover:underline">Remove</button>
                </div>
            </div>

            <div class="flex-end gap-md">
                <button type="button" @click="close" class="btn btn-secondary">Cancel</button>
                <button type="submit" class="btn btn-primary" :disabled="saving">
                    {{ saving ? 'Saving...' : 'Save Changes' }}
                </button>
            </div>
        </form>

        <!-- Target Allocation Section -->
        <div class="allocation-section">
          <div class="section-header-row">
            <h3 class="section-title">Target Allocation</h3>
            <div class="total-indicator" :class="totalClass">
              {{ totalPercentage.toFixed(1) }}%
              <span class="total-label">total</span>
              <span v-if="remainingPercentage > 0.05" class="remaining-label">({{ remainingPercentage.toFixed(1) }}% remaining)</span>
            </div>
          </div>

          <p class="text-xs text-muted mb-md">Define asset categories and their target allocation percentages for rebalancing. Total must not exceed 100%.</p>

          <!-- Categories Table -->
          <div v-if="categories.length > 0" class="categories-table">
            <div class="cat-header-row">
              <span class="cat-col-name">Category</span>
              <span class="cat-col-code">Code</span>
              <span class="cat-col-pct">Target %</span>
              <span class="cat-col-actions"></span>
            </div>

            <div v-for="cat in categories" :key="cat.id" class="cat-row">
              <template v-if="editingId === cat.id">
                <input v-model="editForm.name" class="cat-input" placeholder="Name" />
                <input v-model="editForm.code" class="cat-input cat-code-input" placeholder="CODE" />
                <input v-model.number="editForm.targetPercentage" type="number" step="0.1" min="0" :max="maxEditPercentage" class="cat-input cat-pct-input" />
                <div class="cat-actions">
                  <button @click="saveEdit(cat.id)" class="action-btn save" title="Save" :disabled="catSaving">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>
                  </button>
                  <button @click="cancelEdit" class="action-btn cancel" title="Cancel">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                  </button>
                </div>
              </template>
              <template v-else>
                <span class="cat-name">{{ cat.name }}</span>
                <span class="cat-code">{{ cat.code }}</span>
                <span class="cat-pct">{{ cat.targetPercentage }}%</span>
                <div class="cat-actions">
                  <button @click="startEdit(cat)" class="action-btn edit" title="Edit">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z"/></svg>
                  </button>
                  <button @click="deleteCategory(cat.id)" class="action-btn delete" title="Delete" :disabled="catSaving">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/></svg>
                  </button>
                </div>
              </template>
            </div>
          </div>

          <div v-else class="empty-categories">
            <p>No categories defined yet.</p>
          </div>

          <!-- Add Category Form -->
          <div v-if="showAddForm" class="add-form">
            <input v-model="addForm.name" class="cat-input" placeholder="Category name" />
            <input v-model="addForm.code" class="cat-input cat-code-input" placeholder="CODE" />
            <input v-model.number="addForm.targetPercentage" type="number" step="0.1" min="0" :max="remainingPercentage" class="cat-input cat-pct-input" placeholder="%" />
            <div class="cat-actions">
              <button @click="addCategory" class="action-btn save" title="Add" :disabled="catSaving || !addForm.name || addForm.targetPercentage > remainingPercentage">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>
              </button>
              <button @click="showAddForm = false" class="action-btn cancel" title="Cancel">
                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
              </button>
            </div>
          </div>

          <button v-if="!showAddForm" @click="openAddForm" class="add-category-btn" type="button">
            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
            Add Category
          </button>
        </div>

        <!-- Danger Zone -->
        <div class="danger-zone">
            <h3 class="danger-title">Danger Zone</h3>
            <div class="danger-content">
                <div class="danger-info">
                    <strong>Delete this portfolio</strong>
                    <p>Once deleted, this portfolio and all its data cannot be recovered.</p>
                </div>
                <button type="button" @click="showDeleteConfirm = true" class="btn btn-danger-outline">
                    Delete Portfolio
                </button>
            </div>
        </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <ConfirmDeleteModal
      :isOpen="showDeleteConfirm"
      :portfolioName="portfolio?.name || ''"
      @cancel="showDeleteConfirm = false"
      @confirm="deletePortfolio"
    />
  </div>
  </Teleport>
</template>

<script setup>
import { ref, watch, computed } from 'vue';
import api from '../services/api';
import ConfirmDeleteModal from './ConfirmDeleteModal.vue';

const props = defineProps(['isOpen', 'portfolio']);
const emit = defineEmits(['close', 'updated', 'deleted']);

const form = ref({ name: '', benchmarkAssetId: null });
const searchQuery = ref('');
const results = ref([]);
const selectedBenchmark = ref(null);
const saving = ref(false);
const showDeleteConfirm = ref(false);

// Category state
const categories = ref([]);
const editingId = ref(null);
const editForm = ref({ name: '', code: '', targetPercentage: 0 });
const showAddForm = ref(false);
const addForm = ref({ name: '', code: '', targetPercentage: 0 });
const catSaving = ref(false);

const totalPercentage = computed(() =>
  categories.value.reduce((sum, c) => sum + (c.targetPercentage || 0), 0)
);

const remainingPercentage = computed(() => {
  return Math.max(0, +(100 - totalPercentage.value).toFixed(1));
});

// Max % allowed when editing a specific category (excludes that category's current value)
const maxEditPercentage = computed(() => {
  if (!editingId.value) return 100;
  const othersSum = categories.value
    .filter(c => c.id !== editingId.value)
    .reduce((sum, c) => sum + (c.targetPercentage || 0), 0);
  return Math.max(0, +(100 - othersSum).toFixed(1));
});

const totalClass = computed(() => {
  const t = totalPercentage.value;
  if (Math.abs(t - 100) < 0.1) return 'total-ok';
  if (t > 100) return 'total-over';
  return 'total-under';
});

// Debounce helper
let timeout = null;

watch(() => props.isOpen, (val) => {
    if (val && props.portfolio) {
        form.value.name = props.portfolio.name;
        form.value.benchmarkAssetId = props.portfolio.benchmarkAssetId;
        
        if (props.portfolio.benchmarkAssetId && props.portfolio.benchmarkSymbol) {
            selectedBenchmark.value = { 
                id: props.portfolio.benchmarkAssetId, 
                symbol: props.portfolio.benchmarkSymbol,
                name: 'Benchmark Asset'
            };
        } else {
            selectedBenchmark.value = null;
        }
        searchQuery.value = '';
        results.value = [];

        // Load categories from portfolio data
        categories.value = (props.portfolio.targetAllocation || []).map(c => ({ ...c }));
        editingId.value = null;
        showAddForm.value = false;
    }
});

const searchAssets = async () => {
    if (!searchQuery.value) {
        results.value = [];
        return;
    }
    clearTimeout(timeout);
    timeout = setTimeout(async () => {
        try {
            const res = await api.get(`/assets?query=${searchQuery.value}`);
            results.value = res.data;
        } catch(e) {
            console.error(e);
        }
    }, 300);
};

const selectBenchmark = (asset) => {
    selectedBenchmark.value = asset;
    form.value.benchmarkAssetId = asset.id;
    results.value = [];
    searchQuery.value = '';
};

const clearBenchmark = () => {
    selectedBenchmark.value = null;
    form.value.benchmarkAssetId = null;
};

const close = () => emit('close');

const save = async () => {
    saving.value = true;
    try {
        await api.put(`/portfolios/${props.portfolio.id}`, {
            name: form.value.name,
            benchmarkAssetId: form.value.benchmarkAssetId
        });
        emit('updated');
        close();
    } catch (e) {
        console.error(e);
        alert('Failed to save settings');
    } finally {
        saving.value = false;
    }
};

// Category CRUD
const startEdit = (cat) => {
  editingId.value = cat.id;
  editForm.value = { name: cat.name, code: cat.code, targetPercentage: cat.targetPercentage };
};

const cancelEdit = () => {
  editingId.value = null;
};

const saveEdit = async (categoryId) => {
  // Clamp to max allowed
  editForm.value.targetPercentage = Math.min(editForm.value.targetPercentage, maxEditPercentage.value);
  catSaving.value = true;
  try {
    await api.put(`/portfolios/${props.portfolio.id}/categories/${categoryId}`, editForm.value);
    const cat = categories.value.find(c => c.id === categoryId);
    if (cat) {
      cat.name = editForm.value.name;
      cat.code = editForm.value.code;
      cat.targetPercentage = editForm.value.targetPercentage;
    }
    editingId.value = null;
  } catch (e) {
    console.error(e);
    alert('Failed to update category');
  } finally {
    catSaving.value = false;
  }
};

const openAddForm = () => {
  addForm.value = { name: '', code: '', targetPercentage: remainingPercentage.value };
  showAddForm.value = true;
};

const addCategory = async () => {
  if (!addForm.value.name) return;
  // Clamp to remaining
  addForm.value.targetPercentage = Math.min(addForm.value.targetPercentage, remainingPercentage.value);
  catSaving.value = true;
  try {
    const res = await api.post(`/portfolios/${props.portfolio.id}/categories`, addForm.value);
    categories.value.push(res.data);
    showAddForm.value = false;
  } catch (e) {
    console.error(e);
    alert('Failed to add category');
  } finally {
    catSaving.value = false;
  }
};

const deleteCategory = async (categoryId) => {
  if (!confirm('Remove this category? Holdings assigned to it will become uncategorized.')) return;
  catSaving.value = true;
  try {
    await api.delete(`/portfolios/${props.portfolio.id}/categories/${categoryId}`);
    categories.value = categories.value.filter(c => c.id !== categoryId);
  } catch (e) {
    console.error(e);
    alert('Failed to delete category');
  } finally {
    catSaving.value = false;
  }
};

const deletePortfolio = async () => {
    try {
        await api.delete(`/portfolios/${props.portfolio.id}`);
        showDeleteConfirm.value = false;
        emit('deleted');
        close();
    } catch (e) {
        console.error('Failed to delete portfolio:', e);
        alert('Failed to delete portfolio. Please try again.');
        showDeleteConfirm.value = false;
    }
};
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal-backdrop, 9999);
  backdrop-filter: blur(2px);
}
.modal {
  width: 600px;
  max-width: 95vw;
  max-height: 85vh;
  overflow-y: auto;
  background: var(--color-bg-card, #fff);
  border: 1px solid var(--color-border, #e5e7eb);
  border-radius: var(--radius-lg, 12px);
  box-shadow: var(--shadow-xl, 0 20px 60px rgba(0,0,0,0.15));
}
.dropdown-list {
    position: absolute; top: 100%; left: 0; width: 100%; background: white;
    border: 1px solid var(--color-border); border-radius: 0 0 8px 8px;
    box-shadow: 0 4px 12px rgba(0,0,0,0.1); max-height: 200px; overflow-y: auto; z-index: 10;
}
.dropdown-item { padding: 10px; cursor: pointer; border-bottom: 1px solid #f0f0f0; }
.dropdown-item:hover { background: #f9fafb; }

/* Target Allocation Section */
.allocation-section {
    margin-top: var(--spacing-xl, 24px);
    padding-top: var(--spacing-xl, 24px);
    border-top: 1px solid var(--color-border, #e5e7eb);
}

.section-header-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: var(--spacing-xs, 4px);
}

.section-title {
    margin: 0;
    font-size: var(--font-size-base, 16px);
    font-weight: 600;
    color: var(--color-text-primary, #111827);
}

.total-indicator {
    font-size: var(--font-size-sm, 14px);
    font-weight: 700;
    padding: 4px 12px;
    border-radius: var(--radius-full, 9999px);
}

.total-ok { background: rgba(5, 150, 105, 0.1); color: var(--color-success, #059669); }
.total-under { background: rgba(245, 158, 11, 0.1); color: #d97706; }
.total-over { background: rgba(220, 38, 38, 0.1); color: var(--color-danger, #dc2626); }

.total-label {
    font-weight: 400;
    font-size: var(--font-size-xs, 12px);
    opacity: 0.7;
    margin-left: 2px;
}

.remaining-label {
    font-weight: 400;
    font-size: var(--font-size-xs, 11px);
    opacity: 0.6;
    margin-left: 4px;
}

/* Categories Table */
.categories-table {
    border: 1px solid var(--color-border, #e5e7eb);
    border-radius: 8px;
    overflow: hidden;
    margin-bottom: var(--spacing-sm, 8px);
}

.cat-header-row {
    display: grid;
    grid-template-columns: 1fr 80px 80px 64px;
    gap: 8px;
    padding: 8px 12px;
    background: var(--color-bg-elevated, #f9fafb);
    font-size: var(--font-size-xs, 12px);
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--color-text-muted, #6b7280);
}

.cat-row {
    display: grid;
    grid-template-columns: 1fr 80px 80px 64px;
    gap: 8px;
    padding: 8px 12px;
    align-items: center;
    border-top: 1px solid var(--color-border-subtle, #f0f0f0);
    font-size: var(--font-size-sm, 14px);
}

.cat-row:hover { background: var(--color-bg-elevated, #f9fafb); }

.cat-name { font-weight: 500; color: var(--color-text-primary, #111827); }
.cat-code { color: var(--color-text-muted, #6b7280); font-size: var(--font-size-xs, 12px); text-transform: uppercase; }
.cat-pct { font-weight: 600; text-align: right; color: var(--color-text-primary, #111827); }

.cat-input {
    width: 100%;
    padding: 4px 8px;
    border: 1px solid var(--color-border, #e5e7eb);
    border-radius: 4px;
    font-size: var(--font-size-sm, 14px);
    background: var(--color-bg-primary, #fff);
    color: var(--color-text-primary, #111827);
}

.cat-code-input { text-transform: uppercase; }
.cat-pct-input { text-align: right; }

.cat-actions {
    display: flex;
    gap: 4px;
    justify-content: flex-end;
}

.action-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 28px;
    height: 28px;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    transition: all 0.15s ease;
    background: transparent;
    color: var(--color-text-muted, #6b7280);
}

.action-btn:hover { background: var(--color-bg-elevated, #f0f0f0); }
.action-btn.save { color: var(--color-success, #059669); }
.action-btn.save:hover { background: rgba(5, 150, 105, 0.1); }
.action-btn.delete { color: var(--color-danger, #dc2626); }
.action-btn.delete:hover { background: rgba(220, 38, 38, 0.1); }
.action-btn.cancel { color: var(--color-text-muted); }
.action-btn:disabled { opacity: 0.4; cursor: not-allowed; }

.empty-categories {
    padding: var(--spacing-lg, 16px);
    text-align: center;
    color: var(--color-text-muted, #6b7280);
    font-size: var(--font-size-sm, 14px);
    background: var(--color-bg-elevated, #f9fafb);
    border-radius: 8px;
    margin-bottom: var(--spacing-sm, 8px);
}

/* Add Form */
.add-form {
    display: grid;
    grid-template-columns: 1fr 80px 80px 64px;
    gap: 8px;
    padding: 8px 12px;
    align-items: center;
    border: 1px dashed var(--color-border, #e5e7eb);
    border-radius: 8px;
    margin-bottom: var(--spacing-sm, 8px);
}

.add-category-btn {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 8px 14px;
    background: transparent;
    border: 1px dashed var(--color-border, #d1d5db);
    border-radius: 8px;
    color: var(--color-text-muted, #6b7280);
    font-size: var(--font-size-sm, 14px);
    cursor: pointer;
    transition: all 0.15s ease;
    width: 100%;
    justify-content: center;
}

.add-category-btn:hover {
    border-color: var(--color-accent, #1e40af);
    color: var(--color-accent, #1e40af);
    background: rgba(30, 64, 175, 0.04);
}

/* Danger Zone */
.danger-zone {
    margin-top: var(--spacing-xl, 24px);
    padding-top: var(--spacing-xl, 24px);
    border-top: 1px solid var(--color-border, #e5e7eb);
}

.danger-title {
    margin: 0 0 var(--spacing-md, 12px) 0;
    font-size: var(--font-size-sm, 14px);
    font-weight: 600;
    color: var(--color-danger, #ef4444);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.danger-content {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: var(--spacing-md, 16px);
    background: rgba(239, 68, 68, 0.05);
    border: 1px solid rgba(239, 68, 68, 0.2);
    border-radius: 8px;
}

.danger-info strong {
    display: block;
    font-size: var(--font-size-sm, 14px);
    color: var(--color-text-primary, #111827);
    margin-bottom: 2px;
}

.danger-info p {
    margin: 0;
    font-size: var(--font-size-xs, 12px);
    color: var(--color-text-muted, #6b7280);
}

.btn-danger-outline {
    padding: 8px 16px;
    background: transparent;
    color: var(--color-danger, #ef4444);
    border: 1px solid var(--color-danger, #ef4444);
    border-radius: 6px;
    font-weight: 500;
    font-size: var(--font-size-sm, 14px);
    cursor: pointer;
    transition: all 0.2s ease;
    white-space: nowrap;
}

.btn-danger-outline:hover {
    background: var(--color-danger, #ef4444);
    color: white;
}
</style>
