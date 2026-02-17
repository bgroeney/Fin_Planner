<template>
  <div v-if="!deal" class="deal-workbench card">
    <div class="workbench-loading">
      <div class="spinner"></div>
      <p>Loading deal...</p>
    </div>
  </div>
  <div v-else class="deal-workbench card">
    <!-- Workbench Header -->
    <div class="workbench-header">
      <div class="deal-info">
        <h2 class="deal-title">{{ deal.name }}</h2>
        <div class="deal-meta">
          <span v-if="deal.address" class="deal-address">{{ deal.address }}</span>
          <span v-if="deal.buildingType" class="deal-type badge">{{ deal.buildingType }}</span>
        </div>
      </div>
      <div class="lifecycle-container">
         <LifecycleStepper 
            :current-status="deal.status" 
            :loading="transitioning"
            @request-transition="handleTransitionRequest"
            @request-revert="handleRevertRequest"
            @toggle-history="showHistoryPanel = true"
         />
      </div>

    </div>

    <!-- View Toggle Tabs -->
    <div class="view-tabs">

      <button 
        :class="['view-tab', { active: viewMode === 'summary' }]"
        @click="viewMode = 'summary'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
          <line x1="3" y1="9" x2="21" y2="9"></line>
          <line x1="9" y1="21" x2="9" y2="9"></line>
        </svg>
        Assumptions & Inputs
      </button>

      <button 
        :class="['view-tab', { active: viewMode === 'detailed' }]"
        @click="viewMode = 'detailed'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
          <line x1="3" y1="9" x2="21" y2="9"></line>
          <line x1="3" y1="15" x2="21" y2="15"></line>
          <line x1="9" y1="3" x2="9" y2="21"></line>
          <line x1="15" y1="3" x2="15" y2="21"></line>
        </svg>
        Detailed Model
      </button>
      <button 
        :class="['view-tab', { active: viewMode === 'risk' }]"
        @click="viewMode = 'risk'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M22 12h-4l-3 9L9 3l-3 9H2"/>
        </svg>
        Risk Analysis
      </button>
      <button 
        :class="['view-tab', { active: viewMode === 'documents' }]"
        @click="viewMode = 'documents'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
           <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
           <polyline points="14 2 14 8 20 8"></polyline>
        </svg>
        Documents
      </button>
    </div>

    <!-- Summary View (Edit Mode) -->
    <div v-if="viewMode === 'summary'" class="workbench-grid-container">
       <div class="workbench-toolbar">
          <h3 class="section-title">Deal Assumptions</h3>
          <div class="save-status">
             <span v-if="saving" class="status-saving">
               <span class="spinner-sm"></span> Saving...
             </span>
             <span v-else-if="lastSaved" class="status-saved">
               ✓ Saved
             </span>
          </div>
       </div>

       <div class="bento-grid">
        <!-- Acquisition Costs -->
        <section class="input-card glass-card">
          <h4 class="card-title">Acquisition</h4>
          <div class="card-content">
            <div class="input-row">
              <label>Asking Price</label>
              <div class="input-wrapper">
                <span class="input-prefix">$</span>
                <input type="number" :value="deal.askingPrice" @change="updateField('askingPrice', $event)" />
              </div>
            </div>
            <div class="input-row">
              <label>Stamp Duty</label>
              <div class="input-wrapper">
                <input type="number" step="0.1" :value="deal.stampDutyRate" @change="updateField('stampDutyRate', $event)" />
                <span class="input-suffix">%</span>
              </div>
            </div>
            <div class="input-row">
              <label>Buyers Agent</label>
              <div class="input-wrapper">
                <input type="number" step="0.1" :value="deal.buyersAgentFeeRate" @change="updateField('buyersAgentFeeRate', $event)" />
                <span class="input-suffix">%</span>
              </div>
            </div>
            <div class="input-row">
              <label>Legal Costs</label>
              <div class="input-wrapper">
                <span class="input-prefix">$</span>
                <input type="number" :value="deal.legalCosts" @change="updateField('legalCosts', $event)" />
              </div>
            </div>
            <div class="input-row">
              <label>CapEx Reserve</label>
              <div class="input-wrapper">
                <span class="input-prefix">$</span>
                <input type="number" :value="deal.capExReserve" @change="updateField('capExReserve', $event)" />
              </div>
            </div>
            <div class="summary-row">
              <label>Total Cost</label>
              <span class="summary-value">{{ formatCurrency(totalAcquisitionCost) }}</span>
            </div>
          </div>
        </section>

        <!-- Income & Expenses -->
        <section class="input-card glass-card">
          <h4 class="card-title">Income & Expenses</h4>
           <div class="card-header-row">
              <span></span>
              <span class="col-header">Value</span>
              <span class="col-header">Growth %</span>
           </div>
           <div class="card-content">
              <!-- Gross Rent -->
              <div class="input-grid-row">
                <label>Gross Rent</label>
                <div class="input-wrapper">
                  <span class="input-prefix">$</span>
                  <input type="number" :value="deal.estimatedGrossRent" @change="updateField('estimatedGrossRent', $event)" />
                </div>
                <div class="input-wrapper">
                  <input type="number" step="0.5" :value="deal.rentalGrowthPercent" @change="updateField('rentalGrowthPercent', $event)" />
                </div>
              </div>

              <!-- Vacancy -->
              <div class="input-grid-row">
                <label>Vacancy
                   <button class="btn-xs-link" @click="toggleLeaseMode">{{ deal.leaseDetailsJson ? '(Detailed)' : '(Simple)' }}</button>
                </label>
                <template v-if="!leaseDetails">
                  <div class="input-wrapper">
                    <input type="number" step="0.5" :value="deal.vacancyRatePercent" @change="updateField('vacancyRatePercent', $event)" />
                    <span class="input-suffix">%</span>
                  </div>
                  <div class="input-wrapper">
                    <input type="number" step="0.5" :value="deal.vacancyGrowthPercent ?? 0" @change="updateField('vacancyGrowthPercent', $event)" />
                  </div>
                </template>
                <template v-else>
                    <div class="col-span-2 detailed-label">Configured via Details</div>
                </template>
              </div>

              <!-- Detailed Lease Mode -->
               <div v-if="leaseDetails" class="detailed-subgroup">
                  <div class="input-row compact">
                     <label>Term</label>
                     <div class="input-wrapper xs"><input type="number" :value="leaseDetails.remainingTerm" @change="updateLeaseField('remainingTerm', $event)" /><span class="input-suffix">yrs</span></div>
                  </div>
                  <div class="input-row compact">
                     <label>Review</label>
                     <select :value="leaseDetails.reviewType" @change="updateLeaseField('reviewType', $event)" class="select-xs">
                        <option value="fixed">Fixed</option>
                        <option value="cpi">CPI</option>
                     </select>
                  </div>
                  <div v-if="leaseDetails.reviewType === 'fixed'" class="input-row compact">
                     <label>Rate</label>
                     <div class="input-wrapper xs"><input type="number" step="0.1" :value="leaseDetails.reviewValue" @change="updateLeaseField('reviewValue', $event)" /><span class="input-suffix">%</span></div>
                  </div>
                  <div class="input-row compact">
                     <label>Vac. Shift</label>
                     <div class="input-wrapper xs"><input type="number" :value="leaseDetails.vacancyDurationMonths" @change="updateLeaseField('vacancyDurationMonths', $event)" /><span class="input-suffix">m</span></div>
                  </div>
               </div>

              <!-- Outgoings -->
              <div class="input-grid-row">
                <label>Outgoings</label>
                <div class="input-wrapper">
                  <span class="input-prefix">$</span>
                  <input type="number" :value="deal.outgoingsEstimate" @change="updateField('outgoingsEstimate', $event)" />
                </div>
                <div class="input-wrapper">
                  <input type="number" step="0.5" :value="deal.outgoingsGrowthPercent ?? 0" @change="updateField('outgoingsGrowthPercent', $event)" />
                </div>
              </div>

              <!-- Management -->
              <div class="input-grid-row">
                <label>Mgmt Fee</label>
                <div class="input-wrapper">
                  <input type="number" step="0.5" :value="deal.managementFeePercent" @change="updateField('managementFeePercent', $event)" />
                  <span class="input-suffix">%</span>
                </div>
                 <div class="input-wrapper">
                  <input type="number" step="0.5" :value="deal.managementGrowthPercent ?? 0" @change="updateField('managementGrowthPercent', $event)" />
                </div>
              </div>

              <div class="summary-row">
                <label>Net Operating Income</label>
                <span class="summary-value">{{ formatCurrency(calculatedNOI) }}</span>
              </div>
           </div>
        </section>

        <!-- Financing -->
        <section class="input-card glass-card">
          <div class="card-header-flex">
             <h4 class="card-title">Financing</h4>
             <button class="btn-xs-link" @click="toggleLoanMode">{{ loanDetails ? 'Multi-Tranche' : 'Simple Loan' }}</button>
          </div>
          <div class="card-content">
             <template v-if="!loanDetails || loanDetails.length === 0">
                <div class="input-row">
                  <label>Loan Amount</label>
                  <div class="input-wrapper">
                    <span class="input-prefix">$</span>
                    <input type="number" :value="deal.loanAmount" @change="updateField('loanAmount', $event)" />
                  </div>
                </div>
                <div class="input-row">
                  <label>Interest Rate</label>
                  <div class="input-wrapper">
                    <input type="number" step="0.1" :value="deal.interestRatePercent" @change="updateField('interestRatePercent', $event)" />
                    <span class="input-suffix">%</span>
                  </div>
                </div>
                <div class="input-row">
                   <label>Rate Variance (Risk)</label>
                   <div class="input-wrapper">
                      <span class="input-prefix">±</span>
                      <input type="number" step="0.1" :value="deal.interestVariancePercent" @change="updateField('interestVariancePercent', $event)" />
                      <span class="input-suffix">%</span>
                   </div>
                </div>
             </template>
             
             <!-- Multi Tranche -->
             <div v-else class="detailed-subgroup">
                <div class="multi-loan-header">
                   <span>Tranche</span>
                   <span>Amount</span>
                   <span>Rate</span>
                   <span></span>
                </div>
                 <div v-for="(loan, idx) in loanDetails" :key="idx" class="loan-item-grid">
                    <input type="text" v-model="loan.name" placeholder="Name" class="input-xs" @change="persistLoanDetails"/>
                    <div class="input-wrapper xs"><input type="number" v-model.number="loan.amount" @change="persistLoanDetails" /></div>
                    <div class="input-wrapper xs"><input type="number" step="0.1" v-model.number="loan.rate" @change="persistLoanDetails" /></div>
                    <button class="btn-icon-xs text-danger" @click="removeLoan(idx)">×</button>
                 </div>
                 <button class="btn-xs-secondary full-width mt-sm" @click="addLoan">+ Add Tranche</button>
                 
                 <div class="loan-summary-mini">
                    Avg Rate: {{ weightedAvgRate.toFixed(2) }}%
                 </div>
             </div>

             <div class="summary-row">
                <label>LVR</label>
                <span class="summary-value" :class="{'text-warning': calculatedLVR > 65}">{{ calculatedLVR.toFixed(1) }}%</span>
             </div>
          </div>
        </section>

        <!-- Market & Projections -->
        <section class="input-card glass-card">
           <h4 class="card-title">Market & Projections</h4>
           <div class="card-content">
              <div class="input-row">
                <label>Capital Growth</label>
                <div class="input-wrapper">
                  <input type="number" step="0.5" :value="deal.capitalGrowthPercent" @change="updateField('capitalGrowthPercent', $event)" />
                  <span class="input-suffix">%</span>
                </div>
              </div>
              <div class="input-row">
                  <label>Growth Variance</label>
                  <div class="input-wrapper">
                     <span class="input-prefix">±</span>
                     <input type="number" step="0.5" :value="deal.capitalGrowthVariancePercent" @change="updateField('capitalGrowthVariancePercent', $event)" />
                     <span class="input-suffix">%</span>
                  </div>
              </div>
              <div class="input-row">
                <label>Discount Rate</label>
                <div class="input-wrapper">
                  <input type="number" step="0.1" :value="deal.discountRate || 8" @change="updateField('discountRate', $event)" />
                  <span class="input-suffix">%</span>
                </div>
              </div>
              <div class="input-row">
                <label>Holding Period</label>
                <div class="input-wrapper">
                  <input type="number" min="1" max="30" :value="deal.holdingPeriodYears" @change="updateField('holdingPeriodYears', $event)" />
                  <span class="input-suffix">yrs</span>
                </div>
              </div>
           </div>
        </section>
      </div>

      <!-- Results Summary (Read Only) -->
      <section class="results-bar glass-card">
          <div v-if="simulationResults" class="metrics-flex">
            <div class="metric-item">
               <span class="metric-label">Median NPV</span>
               <span class="metric-value-lg" :class="simulationResults.medianNPV >= 0 ? 'text-success' : 'text-danger'">
                 {{ formatCurrency(simulationResults.medianNPV) }}
               </span>
            </div>
            <div class="vertical-divider"></div>
            <div class="metric-item">
               <span class="metric-label">Median IRR</span>
               <span class="metric-value-lg">{{ simulationResults.medianIRR.toFixed(1) }}%</span>
            </div>
            <div class="vertical-divider"></div>
            <div class="metric-item">
               <span class="metric-label">Rec. Decision</span>
               <span class="metric-tag" :class="getStatusClass(simulationResults.recommendedDecision)">{{ simulationResults.recommendedDecision }}</span>
            </div>
          </div>
          <div v-else class="empty-metrics">
             <span>Run simulation to see results</span>
          </div>
      </section>
    </div>


    <!-- Risk Analysis (Read Only View) -->
    <div v-else-if="viewMode === 'risk'" class="risk-view">
        
        <!-- Unified Simulation Control Bar -->
        <div class="simulation-control-bar" :class="{ 'has-changes': hasUnsavedChanges }">
           <div class="control-status">
              <span v-if="hasUnsavedChanges" class="status-icon warning">⚠️</span>
              <span v-else class="status-icon success">✓</span>
              
              <div class="status-text">
                  <span class="status-title">{{ hasUnsavedChanges ? 'Inputs Changed' : 'Up to Date' }}</span>
                  <span class="status-desc">{{ hasUnsavedChanges ? 'Deal inputs have changed since the last simulation.' : 'Results reflect current inputs.' }}</span>
              </div>
           </div>

           <div class="control-actions">
              <button 
                class="btn" 
                :class="hasUnsavedChanges ? 'btn-primary' : 'btn-secondary'"
                @click="handleRunSimulation"
                :disabled="runningSimulation"
              >
                <span v-if="runningSimulation" class="spinner-sm"></span>
                <span v-else-if="hasUnsavedChanges">Update & Run Analysis</span>
                <span v-else>Re-run Simulation</span>
              </button>
           </div>
        </div>

        <PropertyRiskAnalyzer
            :initial-data="simulationInputSource"
            :initial-results="simulationResults"
            :detailed-cashflow="spreadsheetData"
            :hide-run-button="true"
            @update:inputs="handleInputsUpdate"
        />
    </div>

    <!-- Detailed Spreadsheet View -->
    <div v-else-if="viewMode === 'detailed'" class="spreadsheet-view">
      <CashflowSpreadsheet 
        :deal="deal"
        :holding-period="deal.holdingPeriodYears"
        :initial-overrides-json="deal.spreadsheetOverridesJson"
        @apply-changes="handleSpreadsheetChanges"
        @update:overrides="handleOverridesUpdate"
      />
    </div>

    <!-- Documents View -->
    <div v-else-if="viewMode === 'documents'" class="documents-view p-lg">
        <DealDocuments :deal-id="deal.id" />
    </div>

    <!-- Status History Modal -->
    <StatusHistoryModal 
       v-if="showHistoryPanel" 
       :deal-id="deal.id"
       @close="showHistoryPanel = false"
       @restored="emit('refresh')"
    />

    <!-- Revert Status Modal -->
    <div v-if="showRevertModal" class="modal-backdrop" @click.self="showRevertModal = false">
      <div class="modal revert-modal glass-card">
        <div class="modal-header">
          <h2>Revert Status</h2>
          <button class="btn-close" @click="showRevertModal = false">×</button>
        </div>
        <div class="modal-body">
          <p>Are you sure you want to revert the status to <strong>{{ revertTargetStatus }}</strong>?</p>
          <div class="form-group">
            <label>Reason for reverting (optional)</label>
            <textarea 
              v-model="transitionComment" 
              class="form-control" 
              rows="3" 
              placeholder="e.g., Lease details were incorrect, need to re-evaluate..."
            ></textarea>
          </div>
        </div>
        <div class="modal-actions">
          <button class="btn btn-secondary" @click="showRevertModal = false">Cancel</button>
          <button class="btn btn-warning" @click="confirmStatusChange" :disabled="transitioning">
            <span v-if="transitioning" class="spinner-sm"></span>
            {{ transitioning ? 'Reverting...' : 'Confirm Revert' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Advance Status Modal -->
    <div v-if="showTransitionModal" class="modal-backdrop" @click.self="showTransitionModal = false">
      <div class="modal transition-modal glass-card">
        <div class="modal-header">
          <h2>Update Status</h2>
          <button class="btn-close" @click="showTransitionModal = false">×</button>
        </div>
        <div class="modal-body">
          <p>Move stage to <strong>{{ transitionTargetStatus }}</strong>?</p>
          <div class="form-group">
            <label>Comment (optional)</label>
            <textarea 
              v-model="transitionComment" 
              class="form-control" 
              rows="3" 
              placeholder="e.g., Offer accepted by vendor..."
            ></textarea>
          </div>
        </div>
        <div class="modal-actions">
          <button class="btn btn-secondary" @click="showTransitionModal = false">Cancel</button>
          <button class="btn btn-primary" @click="confirmStatusChange" :disabled="transitioning">
            <span v-if="transitioning" class="spinner-sm"></span>
            {{ transitioning ? 'Updating...' : 'Confirm Update' }}
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { formatCurrency as formatCurrencyUtil, calculateNOI as calculateNOIUtil } from '../../services/monteCarloEngine';
import api from '../../services/api';
import MonteCarloChart from './MonteCarloChart.vue';
import DecisionMatrix from './DecisionMatrix.vue';
import CashflowSpreadsheet from './CashflowSpreadsheet.vue';
import PropertyRiskAnalyzer from './PropertyRiskAnalyzer.vue';
import LifecycleStepper from './LifecycleStepper.vue';
import DealDocuments from './DealDocuments.vue';
import StatusHistoryModal from './StatusHistoryModal.vue';

const viewMode = ref('summary'); 
const spreadsheetData = ref(null);

// Status Management
const transitioning = ref(false);
const showRevertModal = ref(false);
const showTransitionModal = ref(false);
const revertTargetStatus = ref('');
const transitionTargetStatus = ref('');
const transitionComment = ref('');

// History Management
const showHistoryPanel = ref(false);
const statusHistory = ref([]);
const historyLoading = ref(false);

const props = defineProps({
  deal: { type: Object, required: true },
  simulationResults: { type: Object, default: null },
  runningSimulation: { type: Boolean, default: false }
});

const emit = defineEmits(['update', 'run-simulation', 'record-decision', 'refresh']);

// Save state tracking
const saving = ref(false);
const lastSaved = ref(false);
let saveTimeout = null;

function triggerSaveFeedback() {
   saving.value = true;
   lastSaved.value = false;
   if (saveTimeout) clearTimeout(saveTimeout);
   setTimeout(() => {
     saving.value = false;
     lastSaved.value = true;
     saveTimeout = setTimeout(() => lastSaved.value = false, 3000);
   }, 800);
}

// History 
async function toggleHistory() {
  showHistoryPanel.value = !showHistoryPanel.value;
  if (showHistoryPanel.value) {
    await fetchHistory();
  }
}

async function fetchHistory() {
  if (!props.deal) return;
  try {
    historyLoading.value = true;
    const response = await api.get(`/propertydeals/${props.deal.id}/status-history`);
    statusHistory.value = response.data;
  } catch (error) {
    console.error('Failed to fetch history:', error);
    // Fallback mock if endpoint missing yet
    statusHistory.value = [];
  } finally {
    historyLoading.value = false;
  }
}

// Lifecycle Handlers
function handleTransitionRequest(targetStatus) {
   transitionTargetStatus.value = targetStatus;
   transitionComment.value = '';
   showTransitionModal.value = true;
}

function handleRevertRequest(targetStatus) {
    revertTargetStatus.value = targetStatus;
    transitionTargetStatus.value = targetStatus; // Shared handling
    transitionComment.value = '';
    showRevertModal.value = true;
}

async function confirmStatusChange() {
    if (!props.deal) return;
    
    try {
        transitioning.value = true;
        
        await api.post(`/propertydeals/${props.deal.id}/status`, {
            status: transitionTargetStatus.value,
            comment: transitionComment.value
        });
        
        // Success
        showRevertModal.value = false;
        showTransitionModal.value = false;
        
        // Refresh parent to get updated deal state
        emit('refresh');
        
        // Also refresh history if open
        if (showHistoryPanel.value) {
           await fetchHistory();
        }
        
    } catch (error) {
        console.error('Failed to update status:', error);
        alert('Failed to update status. Please try again.');
    } finally {
        transitioning.value = false;
    }
}


function updateField(field, event) {
  triggerSaveFeedback();
  const value = parseFloat(event.target.value) || 0;
  emit('update', { [field]: value });
}

function handleOverridesUpdate(json) {
  // Persist spreadsheet overrides
  triggerSaveFeedback();
  emit('update', { spreadsheetOverridesJson: json });
}

function handleDecision(decision, rationale) {
  emit('record-decision', decision, rationale);
}

function handleSpreadsheetChanges(cashflowData) {
  // Store custom cashflow data for simulation
  spreadsheetData.value = cashflowData;
  
  // If we have detailed cashflows (e.g. from the spreadsheet), ensuring they are passed
  // effectively happens via the :detailed-cashflow propbinding above if we are in Risk View.
  // But if we are in Spreadsheet view and click "Apply", we might want to update the deal state 
  // or trigger a simulation run if applicable.
  
  // Update deal with time variance settings if present
  if (cashflowData.timeVariance) {
    emit('update', {
      timeVarianceEarlyMonths: cashflowData.timeVariance.early,
      timeVarianceLateMonths: cashflowData.timeVariance.late
    });
  }
}

// Watch for deal changes to sync baseline to spreadsheet (if logic existed inside CashflowSpreadsheet)
// Since CashflowSpreadsheet takes :deal prop, it reacts automatically. 
// However, we want to ensure any internal overrides are preserved relative to the new base.
// CashflowSpreadsheet handles this internally via its computed properties (baseValue from prop, override from state).

function handleRunSimulation() {
  emit('run-simulation', spreadsheetData.value);
}

// Parsing Helpers
const leaseDetails = computed(() => {
    try {
        return props.deal.leaseDetailsJson ? JSON.parse(props.deal.leaseDetailsJson) : null;
    } catch { return null; }
});

const loanDetails = computed(() => {
    try {
       return props.deal.loanDetailsJson ? JSON.parse(props.deal.loanDetailsJson) : null;
    } catch { return null; }
});

const totalLoanAmount = computed(() => {
    if (!loanDetails.value) return props.deal.loanAmount;
    return loanDetails.value.reduce((sum, loan) => sum + (loan.amount || 0), 0);
});

const weightedAvgRate = computed(() => {
    if (!loanDetails.value || totalLoanAmount.value === 0) return props.deal.interestRatePercent;
    const weightedSum = loanDetails.value.reduce((sum, loan) => sum + (loan.amount * loan.rate), 0);
    return weightedSum / totalLoanAmount.value;
});

const totalAcquisitionCost = computed(() => {
    if (!props.deal) return 0;
    const askingPrice = props.deal.askingPrice || 0;
    const stampDuty = askingPrice * ((props.deal.stampDutyRate || 0) / 100);
    return askingPrice + stampDuty + (props.deal.legalCosts || 0) + (props.deal.capExReserve || 0);
});

const calculatedNOI = computed(() => {
    if (!props.deal) return 0;
    return calculateNOIUtil(
        props.deal.estimatedGrossRent || 0,
        props.deal.vacancyRatePercent || 0,
        props.deal.managementFeePercent || 0,
        props.deal.outgoingsEstimate || 0
    );
});

const calculatedLVR = computed(() => {
    if (!props.deal || !props.deal.askingPrice) return 0;
    return (totalLoanAmount.value / props.deal.askingPrice) * 100;
});

// Risk Analysis Properties
const simulationInputSource = computed(() => props.deal);
const hasUnsavedChanges = ref(false);

function handleInputsUpdate(newInputs) {
    // Check if critical inputs have changed to show "Unsaved Changes" banner
    // For now, basic implementation:
    hasUnsavedChanges.value = true;
    
    // Auto-save specific risk fields back to deal if they exist in the deal mapping
    // This allows persisting risk preferences (like distribution variances)
    const updates = {};
    if (newInputs.rentVariancePercent !== props.deal.rentVariancePercent) updates.rentVariancePercent = newInputs.rentVariancePercent;
    if (newInputs.vacancyVariancePercent !== props.deal.vacancyVariancePercent) updates.vacancyVariancePercent = newInputs.vacancyVariancePercent;
    if (newInputs.capitalGrowthVariancePercent !== props.deal.capitalGrowthVariancePercent) updates.capitalGrowthVariancePercent = newInputs.capitalGrowthVariancePercent;
    if (newInputs.interestVariancePercent !== props.deal.interestVariancePercent) updates.interestVariancePercent = newInputs.interestVariancePercent;
    
    // Persist distribution settings JSON if provided
    if (newInputs.distributionSettingsJson !== undefined) {
        updates.distributionSettingsJson = newInputs.distributionSettingsJson;
    }
    if (newInputs.correlationMatrixJson !== undefined) {
        updates.correlationMatrixJson = newInputs.correlationMatrixJson;
    }
    
    if (Object.keys(updates).length > 0) {
        emit('update', updates);
    }
}

// Logic Methods
function toggleLeaseMode() {
    triggerSaveFeedback();
    if (leaseDetails.value) {
        // Switch to Simple (Clear details)
        emit('update', { leaseDetailsJson: "" });
    } else {
        // Switch to Detailed (Init default)
        const defaultLease = {
            remainingTerm: 3,
            reviewType: 'fixed',
            reviewValue: 3.0,
            vacancyDurationMonths: 3
        };
        emit('update', { leaseDetailsJson: JSON.stringify(defaultLease) });
    }
}

function updateLeaseField(field, event) {
    triggerSaveFeedback();
    const current = { ...leaseDetails.value };
    
    // Handle select vs input
    let val = event.target.value;
    if (event.target.type === 'number') val = parseFloat(val);
    
    current[field] = val;
    emit('update', { leaseDetailsJson: JSON.stringify(current) });
}

function toggleLoanMode() {
    triggerSaveFeedback();
    if (loanDetails.value) {
        // Clear multi-loan
        emit('update', { loanDetailsJson: "" });
    } else {
        // Init multi-loan with current simple values as first tranche
        const defaultLoans = [
            { name: 'Senior Debt', amount: props.deal.loanAmount, rate: props.deal.interestRatePercent }
        ];
        emit('update', { loanDetailsJson: JSON.stringify(defaultLoans) });
    }
}

function addLoan() {
    const current = [...(loanDetails.value || [])];
    current.push({ name: 'Mezzanine', amount: 0, rate: 8.0 });
    persistLoans(current);
}

function removeLoan(idx) {
    const current = [...loanDetails.value];
    current.splice(idx, 1);
    persistLoans(current);
}

function persistLoanDetails() {
    persistLoans(loanDetails.value);
}

function persistLoans(loans) {
    triggerSaveFeedback();
    const json = JSON.stringify(loans);
    
    // Also update the simple aggregate fields for backward compatibility / high-level display
    const total = loans.reduce((sum, l) => sum + (l.amount || 0), 0);
    let weightedRate = 0;
    if (total > 0) {
        weightedRate = loans.reduce((sum, l) => sum + (l.amount * l.rate), 0) / total;
    }
    
    emit('update', { 
        loanDetailsJson: json,
        loanAmount: total,
        interestRatePercent: parseFloat(weightedRate.toFixed(2))
    });
}


function getStatusClass(status) {
  switch (status) {
    case 'Buy': return 'status-buy';
    case 'Pass': return 'status-pass';
    case 'Uneconomic': return 'status-uneconomic';
    case 'Analyzing': return 'status-analyzing';
    default: return 'status-draft';
  }
}

function formatCurrency(value) {
  return formatCurrencyUtil(value);
}
</script>

<style scoped>
.deal-workbench {
  padding: 0;
  overflow: hidden;
}

.workbench-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg);
  border-bottom: 2px solid var(--color-industrial-copper);
  background: linear-gradient(to right, var(--color-bg-card), rgba(180, 83, 9, 0.03));
}

.deal-title {
  font-size: var(--font-size-xl);
  font-weight: 700;
  margin: 0 0 var(--spacing-xs) 0;
}

.deal-meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.deal-address {
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.run-simulation-btn {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  min-width: 160px;
  justify-content: center;
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid white;
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Grid Layout */
.workbench-grid-container {
  padding: var(--spacing-lg);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.workbench-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-sm);
}

.bento-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--spacing-lg);
}

@media (max-width: 1100px) {
  .bento-grid {
    grid-template-columns: 1fr;
  }
}

/* Glass Cards */
.input-card {
  /* .glass-card class handles background/blur via global CSS */
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  display: flex;
  flex-direction: column;
  height: 100%;
}

.card-title {
  font-size: var(--font-size-sm);
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: var(--letter-spacing-wide);
  color: var(--color-text-muted);
  margin: 0 0 var(--spacing-lg) 0;
  border-bottom: 1px solid var(--color-border-subtle);
  padding-bottom: var(--spacing-sm);
}

.card-header-flex {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid var(--color-border-subtle);
  padding-bottom: var(--spacing-sm);
  margin-bottom: var(--spacing-lg);
}

.card-header-flex .card-title {
  margin: 0;
  border: none;
  padding: 0;
}

.card-header-row {
  display: grid;
  grid-template-columns: 2fr 1.5fr 1fr;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-xs);
  padding: 0 var(--spacing-sm);
}

.col-header {
  font-size: 10px;
  text-transform: uppercase;
  color: var(--color-text-muted);
  font-weight: 600;
  text-align: center;
}

.card-content {
  display: flex;
  flex-direction: column;
  gap: 10px; /* Consistent dense spacing */
  flex: 1;
}

/* Input Rows */
.input-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  min-height: 36px;
}

.input-grid-row {
  display: grid;
  grid-template-columns: 2fr 1.5fr 1fr;
  gap: var(--spacing-md);
  align-items: center;
  min-height: 36px;
}

.input-row label, .input-grid-row label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
}

.input-wrapper input {
  width: 100%;
  text-align: right;
  padding: 6px 8px; /* Compact padding */
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-family: var(--font-mono);
  font-size: var(--font-size-sm);
  background: var(--color-bg-primary); /* Slightly distinct from card bg */
  transition: all var(--transition-fast);
}

.input-wrapper input:focus {
  border-color: var(--color-industrial-copper);
  background: var(--color-bg-elevated);
  outline: none;
  box-shadow: 0 0 0 2px rgba(180, 83, 9, 0.1);
}

.input-prefix,
.input-suffix {
  position: absolute;
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  pointer-events: none;
}

.input-prefix { left: 8px; }
.input-wrapper:has(.input-prefix) input { padding-left: 20px; }

.input-suffix { right: 8px; }
.input-wrapper:has(.input-suffix) input { padding-right: 24px; }

/* Summary Rows */
.summary-row {
  margin-top: auto; /* Push to bottom */
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-border-subtle);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.summary-row label {
  font-weight: 600;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
}

.summary-value {
  font-family: var(--font-mono);
  font-weight: 700;
  font-size: var(--font-size-base);
  color: var(--color-industrial-copper);
}

/* Detailed Subgroups */
.detailed-subgroup {
  grid-column: 1 / -1;
  background: rgba(0, 0, 0, 0.02);
  border-radius: var(--radius-md);
  padding: var(--spacing-md);
  border: 1px solid var(--color-border-subtle);
}

.detailed-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  font-style: italic;
  text-align: right;
}

.input-row.compact {
  min-height: 28px;
}

.select-xs {
  font-size: 11px;
  padding: 2px 4px;
  border-radius: 4px;
  border: 1px solid var(--color-border);
}

/* Results Bar */
.results-bar {
  margin-top: var(--spacing-lg);
  padding: var(--spacing-lg);
  border-radius: var(--radius-lg);
  /* glass-card applies style */
}

.metrics-flex {
  display: flex;
  justify-content: space-around;
  align-items: center;
}

.metric-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

.vertical-divider {
  width: 1px;
  height: 40px;
  background: var(--color-border);
}

.metric-value-lg {
  font-size: var(--font-size-2xl);
  font-weight: 800;
  font-family: var(--font-display);
  line-height: 1;
}

.metric-tag {
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
  padding: 4px 10px;
  border-radius: var(--radius-full);
}

/* Helper Buttons */
.btn-xs-link {
  background: none;
  border: none;
  color: var(--color-accent);
  font-size: 11px;
  cursor: pointer;
  text-decoration: underline;
  padding: 0;
}

.btn-xs-link:hover {
  color: var(--color-accent-dark);
}

.btn-xs-secondary {
  font-size: 11px;
  padding: 4px;
  background: var(--color-bg-elevated);
  border: 1px solid var(--color-border);
  border-radius: 4px;
  cursor: pointer;
  color: var(--color-text-secondary);
}

.btn-xs-secondary:hover {
  border-color: var(--color-accent);
  color: var(--color-accent);
}

.metric-value-lg.text-success { color: var(--color-success); }
.metric-value-lg.text-danger { color: var(--color-danger); }
.text-warning { color: var(--color-warning); }
.full-width { width: 100%; }
.mt-sm { margin-top: var(--spacing-sm); }

/* Multi-loan specific */
.multi-loan-header {
  display: grid;
  grid-template-columns: 2fr 1.5fr 1fr 20px;
  gap: 8px;
  font-size: 10px;
  text-transform: uppercase;
  color: var(--color-text-muted);
  font-weight: 600;
  margin-bottom: 6px;
}

.loan-item-grid {
  display: grid;
  grid-template-columns: 2fr 1.5fr 1fr 20px;
  gap: 8px;
  align-items: center;
  margin-bottom: 6px;
}

.loan-summary-mini {
  text-align: right;
  font-size: 10px;
  color: var(--color-text-muted);
  margin-top: 4px;
}

/* Save Status */
.save-status {
  font-size: var(--font-size-xs);
}

.status-saving {
  color: var(--color-text-muted);
  display: flex;
  align-items: center;
  gap: 6px;
}

.status-saved {
  color: var(--color-success);
  font-weight: 500;
}

.spinner-sm {
  width: 12px;
  height: 12px;
  border: 2px solid var(--color-text-muted);
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

/* View Tabs (Retained) */
.view-tabs {
  display: flex;
  border-bottom: 1px solid var(--color-border);
  padding: 0 var(--spacing-lg);
  background: transparent;
}

.view-tab {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-md) var(--spacing-lg);
  border: none;
  background: none;
  font-size: var(--font-size-sm);
  font-weight: 500;
  color: var(--color-text-muted);
  cursor: pointer;
  border-bottom: 2px solid transparent;
  transition: all var(--transition-fast);
}

.view-tab:hover {
  color: var(--color-text-primary);
}

.view-tab.active {
  color: var(--color-accent);
  border-bottom-color: var(--color-accent);
}

/* Section Header (retained for other views) */
.section-header {
   display: flex;
   justify-content: space-between;
   align-items: center;
   margin-bottom: var(--spacing-sm);
}

.section-title {
   font-size: var(--font-size-base);
   font-weight: 600;
   margin: 0;
}

/* Spreadsheet View */
.spreadsheet-view {
  padding: var(--spacing-lg);
}

/* Risk View */
.risk-view {
    padding: var(--spacing-lg);
}

.risk-actions-bar {
  margin-bottom: var(--spacing-lg);
  display: flex;
  justify-content: flex-end;
}

.validation-error {
    color: var(--color-danger);
    font-size: var(--font-size-xs);
    margin-top: var(--spacing-xs);
    padding: var(--spacing-xs);
    background: rgba(239, 68, 68, 0.1);
    border-radius: var(--radius-sm);
}

.risk-explanation {
    margin-top: var(--spacing-lg);
    background: var(--color-bg-elevated);
    padding: var(--spacing-lg);
    border-radius: var(--radius-md);
}

.risk-explanation h4 {
    margin-top: 0;
}

.risk-explanation ul {
    margin-bottom: 0;
    padding-left: var(--spacing-lg);
}

.text-danger { color: var(--color-danger); }
.text-primary { color: var(--color-industrial-copper); }
.text-success { color: var(--color-success); }

.workbench-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2xl);
  min-height: 400px;
  gap: var(--spacing-md);
}

.workbench-loading .spinner {
  width: 48px;
  height: 48px;
  border: 4px solid var(--color-border);
  border-top-color: var(--color-industrial-copper);
}

/* Immersive Mode Overrides */
.immersive-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  height: 100%;
}

.workbench-container {
  flex: 1;
  overflow-y: auto;
  padding-bottom: var(--spacing-2xl);
}

/* Simulation Control Bar */
.simulation-control-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg);
  background: var(--color-bg-elevated);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  margin-bottom: var(--spacing-lg);
  transition: all var(--transition-base);
}

.simulation-control-bar.has-changes {
  background: rgba(245, 158, 11, 0.05); /* Amber tint */
  border-color: rgba(245, 158, 11, 0.3);
}

.control-status {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}

.status-icon {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  font-size: 16px;
  flex-shrink: 0;
}

.status-icon.warning {
  background: rgba(245, 158, 11, 0.2);
  color: var(--color-warning);
}

.status-icon.success {
  background: rgba(16, 185, 129, 0.1);
  color: var(--color-success);
}

.status-text {
  display: flex;
  flex-direction: column;
}

.status-title {
  font-weight: 600;
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
}

.status-desc {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.control-actions {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}
</style>
