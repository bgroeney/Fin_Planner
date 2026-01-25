<template>
  <div class="decisions-page animate-fade-in">
    <!-- Header -->
    <div class="flex-between mb-lg items-start">
      <div>
        <h1>Decisions</h1>
        <p class="text-muted">Track, approve, and implement portfolio decisions</p>
      </div>
      
      <div class="flex gap-md">
        <!-- Portfolio Selector -->
        <div class="portfolio-select-wrapper">
          <select 
            v-model="selectedPortfolioId" 
            @change="handlePortfolioChange"
            class="portfolio-select"
          >
            <option :value="null" disabled>Select Portfolio</option>
            <option v-for="p in portfolios" :key="p.id" :value="p.id">
              {{ p.name }}
            </option>
          </select>
          <svg class="select-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m6 9 6 6 6-6"/></svg>
        </div>

        <button @click="openNewDecisionModal" class="btn btn-primary" :disabled="!selectedPortfolioId">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 5v14M5 12h14"/></svg>
          New Decision
        </button>
      </div>
    </div>

    <div v-if="!selectedPortfolioId" class="empty-state-large card flex-center flex-col p-xl">
      <div class="empty-icon-bg mb-md">
        <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/></svg>
      </div>
      <h3>Select a Portfolio</h3>
      <p>Please select a portfolio to view and manage decisions.</p>
    </div>

    <div v-else>
      <!-- Stats Row -->
      <div class="stats-row mb-lg">
        <div class="stat-badge pending" :class="{ active: activeTab === 'Pending' }" @click="activeTab = 'Pending'">
          <span class="count">{{ stats.pending }}</span>
          <span class="label">Pending</span>
        </div>
        <div class="stat-badge draft" :class="{ active: activeTab === 'Draft' }" @click="activeTab = 'Draft'">
          <span class="count">{{ stats.draft }}</span>
          <span class="label">Drafts</span>
        </div>
        <div class="stat-badge to-be-implemented" :class="{ active: activeTab === 'To be implemented' }" @click="activeTab = 'To be implemented'">
          <span class="count">{{ stats.toBeImplemented }}</span>
          <span class="label">Ready to Implement</span>
        </div>
        <div class="stat-badge implemented" :class="{ active: activeTab === 'Implemented' }" @click="activeTab = 'Implemented'">
          <span class="count">{{ stats.implemented }}</span>
          <span class="label">Implemented</span>
        </div>
      </div>

      <!-- Tabs & Filters -->
      <div class="flex-between mb-md border-bottom pb-sm">
        <div class="tabs">
          <button 
            v-for="tab in ['All', 'Pending', 'Draft', 'To be implemented', 'Implemented', 'Rejected', 'Deleted']" 
            :key="tab"
            :class="['tab', { active: activeTab === tab }]"
            @click="activeTab = tab"
          >
            {{ tab }}
          </button>
        </div>
        
        <div class="flex gap-sm">
            <button 
              @click="backfillDecisions" 
              class="btn btn-ghost btn-sm" 
              :disabled="backfilling"
            >
              <span v-if="backfilling" class="spinner-sm mr-sm"></span>
              Sync History
            </button>

            <button 
              @click="generateAIRecommendations" 
              class="btn btn-secondary btn-sm" 
              :disabled="generatingAI"
            >
              <span v-if="generatingAI" class="spinner-sm mr-sm"></span>
              <svg v-else class="mr-sm text-accent" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83"/></svg>
              AI Recommendations
            </button>
        </div>
      </div>

      <!-- Decisions List -->
      <div v-if="loading" class="flex-center p-xl">
        <MoneyBoxLoader size="lg" text="Loading Decisions..." />
      </div>

      <!-- AI Generation Overlay -->
      <div v-if="generatingAI" class="modal-backdrop">
         <div class="card p-xl">
             <MoneyBoxLoader size="lg" text="AI is Analyzing Market Opportunities..." />
         </div>
      </div>

      <div v-else-if="filteredDecisions.length === 0" class="empty-state card flex-center flex-col p-lg">
        <p>No decisions found for this filter.</p>
        <div class="flex gap-md mt-md">
            <button v-if="activeTab === 'Draft' || activeTab === 'All'" @click="openNewDecisionModal" class="btn btn-primary">Create First Draft</button>
            <button v-if="activeTab === 'All' || activeTab === 'Implemented'" @click="backfillDecisions" class="btn btn-secondary" :disabled="backfilling">
                <span v-if="backfilling" class="spinner-sm mr-sm"></span>
                Generate from History
            </button>
        </div>
      </div>

      <div v-else class="decisions-table-container card">
        <table class="table">
          <thead>
            <tr>
              <th>Date</th>
              <th>Decision</th>
              <th>Type</th>
              <th>Status</th>
              <th class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr 
              v-for="decision in filteredDecisions" 
              :key="decision.id" 
              @click="viewDecision(decision)" 
              class="clickable-row"
            >
              <td>{{ formatDate(decision.createdAt) }}</td>
              <td>
                <div class="font-medium">{{ decision.title || 'Untitled' }}</div>
                <div class="text-xs text-muted truncate max-w-md">{{ truncate(decision.rationale, 80) }}</div>
              </td>
              <td>
                <span class="flex items-center gap-xs">
                    <svg v-if="decision.type === 'AI'" class="text-accent" xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m12 3-1.912 5.813a2 2 0 0 1-1.275 1.275L3 12l5.813 1.912a2 2 0 0 1 1.275 1.275L12 21l1.912-5.813a2 2 0 0 1 1.275-1.275L21 12l-5.813-1.912a2 2 0 0 1-1.275-1.275L12 3Z"/></svg>
                    {{ decision.type }}
                </span>
              </td>
              <td>
                <span class="badge" :class="getStatusClass(decision.status)">{{ decision.status }}</span>
              </td>
              <td class="text-right">
                <div class="flex-end gap-xs" @click.stop>
                  <button @click="viewDecision(decision)" class="btn btn-ghost btn-xs">View</button>
                  
                  <template v-if="decision.status === 'Draft'">
                    <button @click="editDecision(decision)" class="btn btn-secondary btn-xs">Edit</button>
                    <button @click="submitDecision(decision.id)" class="btn btn-primary btn-xs">Submit</button>
                  </template>

                  <template v-if="decision.status === 'Pending'">
                    <button @click="approveDecision(decision.id)" class="btn btn-success btn-xs" title="Approve">✓</button>
                    <button @click="rejectDecision(decision.id)" class="btn btn-danger-outline btn-xs" title="Reject">✗</button>
                  </template>

                  <template v-if="decision.status === 'To be implemented'">
                    <button @click="implementDecision(decision.id)" class="btn btn-primary btn-xs">Mark Implemented</button>
                  </template>

                  <button v-if="decision.status !== 'Deleted'" @click="deleteDecision(decision.id)" class="btn btn-danger-outline btn-xs" title="Soft Delete">Delete</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Edit/View Modal -->
    <Teleport to="body">
    <div v-if="showModal" class="modal-backdrop" @click="closeModal">
      <div class="modal card" @click.stop>
        <div class="modal-header flex-between mb-lg">
          <h2>{{ modalmode === 'create' ? 'New Decision' : (modalmode === 'edit' ? 'Edit Decision' : 'Decision Details') }}</h2>
          <button @click="closeModal" class="btn-close">×</button>
        </div>
        
        <div class="modal-body mb-lg">
          <div v-if="modalmode === 'view'" class="view-mode">
             <div class="flex-between mb-md">
                <div class="flex items-center gap-md">
                    <span class="badge" :class="getStatusClass(currentDecision.status)">{{ currentDecision.status }}</span>
                    <span class="text-muted text-sm">{{ formatDate(currentDecision.createdAt) }}</span>
                </div>
                <button @click="showHistoryView = !showHistoryView" class="btn btn-ghost btn-xs">
                    {{ showHistoryView ? 'View Details' : 'View History' }}
                </button>
             </div>

             <div v-if="showHistoryView" class="history-view animate-fade-in">
                <h4 class="mb-sm">Decision History</h4>
                <div v-if="loadingHistory" class="flex-center p-md">
                    <MoneyBoxLoader size="sm" />
                </div>
                <div v-else-if="decisionHistory.length === 0" class="text-muted text-sm p-md italic">
                    No history recorded for this decision.
                </div>
                <div v-else class="history-list">
                    <div v-for="log in decisionHistory" :key="log.id" class="history-item mb-sm border-bottom-subtle pb-xs">
                        <div class="flex-between text-xs mb-xs">
                            <span class="font-bold uppercase text-accent">{{ log.action }}</span>
                            <span class="text-muted">{{ formatDate(log.timestamp) }}</span>
                        </div>
                        <p class="text-sm m-0">{{ log.details }}</p>
                        <div class="text-xs text-muted mt-xs italic">By: {{ log.userEmail }}</div>
                    </div>
                </div>
             </div>

             <div v-else class="details-view">
                 <h3 class="mb-md">{{ currentDecision.title }}</h3>
                 <div class="info-block bg-gray-50 p-md radius-md mb-md">
                    <label>Rationale / Analysis</label>
                    <div class="markdown-text">{{ currentDecision.rationale }}</div>
                 </div>
                 
                 <span v-if="currentDecision.type === 'AI'" class="text-accent text-sm font-bold flex items-center gap-xs">
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m12 3-1.912 5.813a2 2 0 0 1-1.275 1.275L3 12l5.813 1.912a2 2 0 0 1 1.275 1.275L12 21l1.912-5.813a2 2 0 0 1 1.275-1.275L21 12l-5.813-1.912a2 2 0 0 1-1.275-1.275L12 3Z"/></svg>
                    AI Generated Recommendation
                 </span>
             </div>
          </div>

          <div v-else class="edit-mode">
             <div class="form-group">
                <label>Title</label>
                <input v-model="form.title" placeholder="e.g. Rebalance Defensive Assets" />
             </div>
             <div class="form-group">
                <label>Rationale</label>
                <textarea v-model="form.rationale" rows="6" placeholder="Explain the reasoning behind this decision..."></textarea>
             </div>
          </div>
        </div>

        <div class="modal-footer flex-end gap-md">
          <button @click="closeModal" class="btn btn-secondary">Close</button>
          
          <template v-if="modalmode === 'create' || modalmode === 'edit'">
             <button @click="saveDecision(true)" class="btn btn-secondary">Save Draft</button>
             <button @click="saveDecision(false)" class="btn btn-primary">Submit for Approval</button>
          </template>
        </div>
      </div>
    </div>
    </Teleport>

  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import api from '../services/api';
import MoneyBoxLoader from '../components/MoneyBoxLoader.vue';

const portfolios = ref([]);
const selectedPortfolioId = ref(null);
const decisions = ref([]);
const loading = ref(false);
const generatingAI = ref(false);
const backfilling = ref(false);
const activeTab = ref('All');
const decisionHistory = ref([]);
const loadingHistory = ref(false);
const showHistoryView = ref(true); // Default expanded for timeline view at bottom

// Modal State
const showModal = ref(false);
const modalmode = ref('view'); // view, edit, create
const currentDecision = ref(null);
const form = ref({
    title: '',
    rationale: ''
});

// Stats
const stats = computed(() => {
    return {
        pending: decisions.value.filter(d => d.status === 'Pending').length,
        draft: decisions.value.filter(d => d.status === 'Draft').length,
        toBeImplemented: decisions.value.filter(d => d.status === 'To be implemented').length,
        implemented: decisions.value.filter(d => d.status === 'Implemented').length,
        deleted: decisions.value.filter(d => d.status === 'Deleted').length,
    }
});

const filteredDecisions = computed(() => {
    if (activeTab.value === 'All') return decisions.value.filter(d => d.status !== 'Deleted');
    return decisions.value.filter(d => d.status === activeTab.value);
});

onMounted(async () => {
    await fetchPortfolios();
});

const fetchPortfolios = async () => {
    try {
        const res = await api.get('/portfolios');
        portfolios.value = res.data;
        if (portfolios.value.length > 0) {
            selectedPortfolioId.value = portfolios.value[0].id;
            fetchDecisions();
        }
    } catch (e) {
        console.error("Failed to fetch portfolios", e);
    }
}

const handlePortfolioChange = () => {
    fetchDecisions();
};

const fetchDecisions = async () => {
    if (!selectedPortfolioId.value) return;
    loading.value = true;
    try {
        const res = await api.get(`/decisions/portfolio/${selectedPortfolioId.value}`);
        decisions.value = res.data;
    } catch (e) {
        console.error(e);
    } finally {
        loading.value = false;
    }
};

const generateAIRecommendations = async () => {
    if (!selectedPortfolioId.value) return;
    generatingAI.value = true;
    try {
        await api.post(`/decisions/ai-recommendations/${selectedPortfolioId.value}`);
        await fetchDecisions();
        activeTab.value = 'Pending'; // Switch to show the new recommendation
    } catch (e) {
        alert("Failed to generate recommendations: " + (e.response?.data || e.message));
    } finally {
        generatingAI.value = false;
    }
};

const backfillDecisions = async () => {
    if (!selectedPortfolioId.value) return;
    if(!confirm("Generate retroactive decisions for existing history? This will create 'Implemented' decisions for past Buy/Sell transactions.")) return;
    backfilling.value = true;
    try {
        const res = await api.post(`/decisions/backfill/${selectedPortfolioId.value}`);
        alert(res.data.message); // Should return "Backfilled X decisions"
        fetchDecisions();
    } catch (e) {
        console.error(e);
        alert("Backfill failed: " + (e.response?.data?.message || e.message));
    } finally {
        backfilling.value = false;
    }
};

// Actions
const openNewDecisionModal = () => {
    modalmode.value = 'create';
    form.value = { title: '', rationale: '' };
    showModal.value = true;
};

const viewDecision = (decision) => {
    currentDecision.value = decision;
    modalmode.value = 'view';
    showHistoryView.value = true;
    showModal.value = true;
    fetchHistory(decision.id);
};

const fetchHistory = async (id) => {
    loadingHistory.value = true;
    try {
        const res = await api.get(`/decisions/${id}/history`);
        decisionHistory.value = res.data;
    } catch (e) {
        console.error("Failed to fetch history", e);
    } finally {
        loadingHistory.value = false;
    }
};

const editDecision = (decision) => {
    currentDecision.value = decision;
    form.value = { title: decision.title, rationale: decision.rationale };
    modalmode.value = 'edit';
    showModal.value = true;
};

const saveDecision = async (asDraft) => {
    try {
        const payload = {
            portfolioId: selectedPortfolioId.value,
            title: form.value.title,
            rationale: form.value.rationale,
            saveAsDraft: asDraft
        };

        if (modalmode.value === 'create') {
            await api.post('/decisions', payload);
        } else {
            // Edit
            await api.put(`/decisions/${currentDecision.value.id}`, payload); // Simplified DTO usage
        }
        
        closeModal();
        fetchDecisions();
    } catch (e) {
        console.error(e);
        alert("Failed to save");
    }
};

const submitDecision = async (id) => {
    if(!confirm("Submit this decision for approval?")) return;
    await apiAction(`/decisions/${id}/submit`);
};

const approveDecision = async (id) => {
    await apiAction(`/decisions/${id}/approve`);
};

const rejectDecision = async (id) => {
    if(!confirm("Reject this decision?")) return;
    await apiAction(`/decisions/${id}/reject`);
};

const implementDecision = async (id) => {
    if(!confirm("Mark this decision as implemented?")) return;
    await apiAction(`/decisions/${id}/implement`);
};

const deleteDecision = async (id) => {
    if(!confirm("Are you sure you want to delete this decision? It will be moved to the Deleted tab.")) return;
    try {
        await api.delete(`/decisions/${id}`);
        fetchDecisions();
    } catch (e) {
        console.error(e);
        alert("Delete failed");
    }
};

const apiAction = async (endpoint) => {
    try {
        await api.put(endpoint);
        fetchDecisions();
    } catch (e) {
        console.error(e);
        alert("Action failed");
    }
};

const closeModal = () => {
    showModal.value = false;
    currentDecision.value = null;
    form.value = { title: '', rationale: '' };
};

// Helpers
const formatDate = (d) => new Date(d).toLocaleDateString('en-AU', { day: 'numeric', month: 'short', year: 'numeric' });
const truncate = (text, length) => text && text.length > length ? text.substring(0, length) + '...' : text;

const getStatusClass = (status) => {
    switch (status) {
        case 'Approved': return 'badge-success';
        case 'To be implemented': return 'badge-primary';
        case 'Implemented': return 'badge-success';
        case 'Rejected': return 'badge-danger';
        case 'Draft': return 'badge-muted';
        case 'Deleted': return 'badge-danger'; // or a new color
        default: return 'badge-warning';
    }
};

const getCardClass = (decision) => {
    if (decision.status === 'Implemented') return 'card-implemented';
    if (decision.type === 'AI') return 'card-ai';
    return '';
};

</script>

<style scoped>
.decisions-page {
    max-width: 1200px;
}

.portfolio-select-wrapper {
    position: relative;
    width: 200px;
}
.portfolio-select {
    width: 100%;
    appearance: none;
    padding: var(--spacing-sm) var(--spacing-md);
    padding-right: 30px;
    border: 1px solid var(--color-border);
    border-radius: var(--radius-md);
    background: var(--color-bg-secondary);
    color: var(--color-text-primary);
    font-size: var(--font-size-sm);
    cursor: pointer;
}
.select-icon {
    position: absolute;
    right: 10px;
    top: 50%;
    transform: translateY(-50%);
    pointer-events: none;
    color: var(--color-text-muted);
}

.stats-row {
    display: flex;
    gap: var(--spacing-md);
    overflow-x: auto;
    padding-bottom: 4px;
    flex-wrap: wrap; /* responsive */
}
.stat-badge {
    flex: 1;
    min-width: 120px;
    background: var(--color-bg-card);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-lg);
    padding: var(--spacing-md);
    display: flex;
    flex-direction: column;
    align-items: center;
    cursor: pointer;
    transition: all var(--transition-fast);
}
.stat-badge:hover {
    border-color: var(--color-accent);
    background: var(--color-bg-elevated);
}
.stat-badge.active {
    background: rgba(37, 99, 235, 0.05); /* accent light */
    border-color: var(--color-accent);
}
.stat-badge .count {
    font-size: var(--font-size-2xl);
    font-weight: 700;
    color: var(--color-text-primary);
}
.stat-badge .label {
    font-size: var(--font-size-xs);
    font-weight: 600;
    text-transform: uppercase;
    color: var(--color-text-muted);
}

/* Tabs */
.tabs { display: flex; gap: 4px; overflow-x: auto; padding-bottom: 2px; }
.tab {
  padding: 8px 16px;
  border: none; background: none; cursor: pointer;
  border-bottom: 2px solid transparent;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  white-space: nowrap;
  transition: all var(--transition-fast);
}
.tab:hover {
    color: var(--color-text-primary);
}
.tab.active {
  color: var(--color-accent);
  border-bottom-color: var(--color-accent);
  font-weight: 500;
}
.border-bottom { border-bottom: 1px solid var(--color-border); }
.pb-sm { padding-bottom: var(--spacing-sm); }

/* Buttons & Utils */
.btn-ghost { color: var(--color-text-muted); }
.btn-ghost:hover { color: var(--color-text-primary); background: var(--color-bg-elevated); }
.text-accent { color: var(--color-accent); }
.spinner-sm { width: 16px; height: 16px; border: 2px solid currentColor; border-top-color: transparent; border-radius: 50%; animation: spin 0.8s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

/* Table Styles */
.decisions-table-container {
  overflow-x: auto;
  padding: 0;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-bg-card);
}
.table {
  width: 100%;
  border-collapse: collapse;
}
.table th, .table td {
  padding: var(--spacing-md);
  text-align: left;
  border-bottom: 1px solid var(--color-border-subtle);
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
}
.table th {
  font-weight: 600;
  color: var(--color-text-muted);
  background: var(--color-bg-elevated);
  text-transform: uppercase;
  font-size: var(--font-size-xs);
  letter-spacing: 0.05em;
}
.table tr:last-child td {
  border-bottom: none;
}
.table tr:hover {
    background: var(--color-bg-primary);
}
.clickable-row {
    cursor: pointer;
}

/* Actions & Badges */
.text-right { text-align: right; }
.truncate { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.max-w-md { max-width: 300px; display: inline-block; vertical-align: bottom; }
.font-medium { font-weight: 500; }
.text-xs { font-size: 0.75rem; }
.flex-end { display: flex; justify-content: flex-end; align-items: center; }
.gap-xs { gap: 0.5rem; }
.btn-xs { padding: 4px 8px; font-size: 0.75rem; height: auto; }

/* Status Badges */
.badge { padding: 2px 8px; border-radius: var(--radius-full); font-size: 11px; font-weight: 600; text-transform: uppercase; }
.badge-success { background: rgba(16, 185, 129, 0.1); color: var(--color-success); }
.badge-warning { background: rgba(245, 158, 11, 0.1); color: var(--color-warning); }
.badge-primary { background: rgba(59, 130, 246, 0.1); color: var(--color-accent); }
.badge-danger { background: rgba(239, 68, 68, 0.1); color: var(--color-danger); }
.badge-muted { background: var(--color-bg-elevated); color: var(--color-text-muted); }

/* Modal */
.modal-backdrop {
    position: fixed; top: 0; left: 0; width: 100%; height: 100%;
    background: rgba(0,0,0,0.5);
    z-index: 1000;
    display: flex; align-items: center; justify-content: center;
    backdrop-filter: blur(2px);
}
.modal {
    width: 600px; max-width: 90%; max-height: 90vh; overflow-y: auto;
    background: var(--color-bg-card);
    border-radius: var(--radius-lg);
    border: 1px solid var(--color-border);
    box-shadow: var(--shadow-xl);
}
.modal-header h2 { font-size: var(--font-size-lg); margin: 0; }
.btn-close { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: var(--color-text-muted); }
.btn-success { background: var(--color-success); color: white; border: none; }
.btn-success:hover { background: #059669; } /* Emerald 600 */
.btn-danger-outline { background: transparent; color: var(--color-danger); border: 1px solid var(--color-danger); }
.btn-danger-outline:hover { background: rgba(239, 68, 68, 0.1); }

/* History View */
.history-item {
    border-left: 2px solid var(--color-border);
    padding-left: var(--spacing-md);
}
.history-item:hover {
    border-left-color: var(--color-accent);
}
.border-bottom-subtle {
    border-bottom: 1px solid var(--color-border-subtle);
}

/* Vertical Timeline Styles */
.history-section {
    border-top: 1px solid var(--color-border);
}
.btn-collapse {
    background: none;
    border: none;
    cursor: pointer;
    padding: var(--spacing-sm) 0;
    color: var(--color-text-primary);
    width: 100%;
}
.btn-collapse:hover {
    color: var(--color-accent);
}
.w-full { width: 100%; }
.text-left { text-align: left; }
.flex-between { display: flex; justify-content: space-between; align-items: center; }
.m-0 { margin: 0; }
.transition-transform {
    transition: transform 0.3s ease;
}
.rotate-180 {
    transform: rotate(180deg);
}
.timeline-list {
    position: relative;
    padding-left: 12px;
}
.timeline-item {
    position: relative;
    padding-left: 24px;
    border-left: 2px solid var(--color-border);
    padding-bottom: var(--spacing-md);
}
.timeline-item:last-child {
    border-left-color: transparent;
    padding-bottom: 0;
}
.timeline-marker {
    position: absolute;
    left: -5px;
    top: 4px; /* Align with first line of text */
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: var(--color-accent);
    border: 2px solid var(--color-bg-card);
}
.pb-md { padding-bottom: var(--spacing-md); }
.pt-md { padding-top: var(--spacing-md); }
.mt-md { margin-top: var(--spacing-md); }
</style>
