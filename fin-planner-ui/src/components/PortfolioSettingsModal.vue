<template>
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
</template>

<script setup>
import { ref, watch } from 'vue';
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

// Debounce helper
let timeout = null;

watch(() => props.isOpen, (val) => {
    if (val && props.portfolio) {
        form.value.name = props.portfolio.name;
        form.value.benchmarkAssetId = props.portfolio.benchmarkAssetId;
        
        // If has benchmark, set display (simple logic: we only have symbol in dto, 
        // to get name we might need to fetch or just display symbol)
        if (props.portfolio.benchmarkAssetId && props.portfolio.benchmarkSymbol) {
            selectedBenchmark.value = { 
                id: props.portfolio.benchmarkAssetId, 
                symbol: props.portfolio.benchmarkSymbol,
                name: 'Benchmark Asset' // We might not have name handy without fetch, acceptable for MVP
            };
        } else {
            selectedBenchmark.value = null;
        }
        searchQuery.value = '';
        results.value = [];
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
  position: fixed; top: 0; left: 0; width: 100%; height: 100%;
  background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000;
}
.modal { width: 500px; max-width: 95vw; background: white; border-radius: 12px; }
.dropdown-list {
    position: absolute; top: 100%; left: 0; width: 100%; background: white;
    border: 1px solid var(--color-border); border-radius: 0 0 8px 8px;
    box-shadow: 0 4px 12px rgba(0,0,0,0.1); max-height: 200px; overflow-y: auto; z-index: 10;
}
.dropdown-item { padding: 10px; cursor: pointer; border-bottom: 1px solid #f0f0f0; }
.dropdown-item:hover { background: #f9fafb; }

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
