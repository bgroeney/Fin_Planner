<template>
  <div class="property-risk-analyzer">
    <div class="analyzer-grid">
      <!-- Inputs Column -->
      <div class="inputs-column">
        <h3 class="column-title">Simulation Assumptions</h3>
        
        <!-- Financial Parameters -->
        <div class="input-group">
          <div class="financials-header">
            <h4>Base Financials</h4>
            <div class="toggle-wrapper" :title="inputs.includeAcquisitionCost ? 'Switch to Intrinsic Value (Gross)' : 'Switch to Profit Analysis (Net)'">
              <span class="toggle-label" :class="{ active: !inputs.includeAcquisitionCost }">Gross</span>
              <label class="switch">
                <input type="checkbox" v-model="inputs.includeAcquisitionCost" @change="run">
                <span class="slider round"></span>
              </label>
              <span class="toggle-label" :class="{ active: inputs.includeAcquisitionCost }">Net</span>
            </div>
          </div>
          

          <div class="input-row">
            <label>Property Value</label>
            <div class="input-wrapper" v-if="!readOnly">
              <span class="input-prefix">$</span>
              <input type="number" v-model.number="inputs.propertyValue" @change="run" />
            </div>
            <div class="static-value" v-else>
               {{ formatCurrency(inputs.propertyValue) }}
            </div>
          </div>
        </div>



        <!-- Risks & Distributions -->
        <div class="input-group">
          <div class="group-header">
             <h4>Risk Parameters</h4>
             <button class="btn btn-sm btn-secondary icon-btn" @click="showCorrelationModal = true" v-if="!readOnly">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                   <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                   <line x1="3" y1="9" x2="21" y2="9"></line>
                   <line x1="9" y1="21" x2="9" y2="9"></line>
                </svg>
                Correlation Matrix
             </button>
          </div>
          
          <!-- Rent Distribution -->
          <DistributionCard 
             label="Gross Rent" 
             :base-value="inputs.estimatedGrossRent"
             :is-currency="true"
             :config="distributions.rent"
             @update="updateDistribution('rent', $event)"
             :read-only="readOnly"
          />

          <!-- Vacancy Distribution -->
          <DistributionCard 
             label="Vacancy Rate" 
             :base-value="inputs.vacancyRatePercent"
             suffix="%"
             :config="distributions.vacancy"
             @update="updateDistribution('vacancy', $event)"
             :read-only="readOnly"
          />

          <!-- Growth Distribution -->
          <DistributionCard 
             label="Capital Growth" 
             :base-value="inputs.capitalGrowthPercent"
             suffix="%"
             :config="distributions.growth"
             @update="updateDistribution('growth', $event)"
             :read-only="readOnly"
          />


        </div>
        
        <CorrelationModal 
           v-if="showCorrelationModal" 
           :matrix="correlationMatrix"
           @close="showCorrelationModal = false"
           @save="updateCorrelationMatrix"
        />



        <div class="actions" v-if="!readOnly && !hideRunButton">
          <button class="btn btn-primary full-width" @click="run" :disabled="running">
            <span v-if="running" class="spinner"></span>
            {{ running ? 'Simulating...' : 'Run Analysis' }}
          </button>
        </div>
      </div>

      <!-- Results Column -->
      <div class="results-column">
        <div v-if="!results" class="empty-state">
          <p>Enter assumptions and run simulation to view risk analysis.</p>
        </div>
        <div v-else class="results-content">
          <!-- Key Metrics -->
          <div class="metrics-row">
            <div class="metric">
              <span class="label">Median {{ inputs.includeAcquisitionCost ? 'Net Profit' : 'Value' }}</span>
              <span class="value" :class="results.medianNPV >= 0 ? 'positive' : 'negative'">
                {{ formatCurrency(results.medianNPV) }}
              </span>
            </div>
            <div class="metric">
              <span class="label">Median IRR</span>
              <span class="value">{{ results.medianIRR }}%</span>
            </div>
            <div class="metric">
              <span class="label">Value at Risk (5%)</span>
              <span class="value negative">{{ formatCurrency(results.p10NPV) }}</span>
            </div>
          </div>

          <!-- Value at Risk Chart -->
          <div class="chart-section">
            <ValueAtRiskChart 
              :probability-curve="results.probabilityCurve"
              :total-acquisition-cost="initialEquity"
              :is-net-mode="inputs.includeAcquisitionCost"
            />
          </div>

          <!-- Distribution Chart -->
          <div class="chart-section small">
            <MonteCarloChart
              :histogram="results.npvHistogram"
              :p10="results.p10NPV"
              :median="results.medianNPV"
              :p90="results.p90NPV"
              :title="inputs.includeAcquisitionCost ? 'Net Profit Distribution ($)' : 'Intrinsic Value Distribution ($)'"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { runSimulation, formatCurrency as formatCurrencyUtil, DistributionTypes } from '../../services/monteCarloEngine';
import ValueAtRiskChart from './ValueAtRiskChart.vue';
import MonteCarloChart from './MonteCarloChart.vue';
import DistributionCard from './DistributionCard.vue';
import CorrelationModal from './CorrelationModal.vue';

const props = defineProps({
  initialData: { type: Object, required: true },
  initialResults: { type: Object, default: null },
  detailedCashflow: { type: Object, default: null },
  autoRun: { type: Boolean, default: false },
  readOnly: { type: Boolean, default: false },
  hideRunButton: { type: Boolean, default: false }
});

const emit = defineEmits(['update:inputs']);

const inputs = ref({
  propertyValue: 0,
  estimatedGrossRent: 0,
  outgoingsEstimate: 0,
  vacancyRatePercent: 5,
  vacancyVariancePercent: 5,
  managementFeePercent: 7,
  capitalGrowthPercent: 3,
  capitalGrowthVariancePercent: 2,
  discountRate: 8,
  loanAmount: 0,
  interestRatePercent: 6.5,
  interestVariancePercent: 1,
  rentVariancePercent: 10,
  timeVarianceEarly: 0,
  timeVarianceLate: 3,
  holdingPeriodYears: 10,
  // Acquisition specific (can be 0 for existing holdings)
  stampDutyRate: 0,
  legalCosts: 0,
  buyersAgentFeeRate: 0,
  capExReserve: 0,
  includeAcquisitionCost: false // false = Gross (Intrinsic Value), true = Net (Profit)
});

// Distribution Settings (Normal, Lognormal, Triangular, etc)
const distributions = ref({
    rent: { type: 'normal', variancePercent: 10 },
    vacancy: { type: 'normal', variancePercent: 5 },
    growth: { type: 'normal', variancePercent: 2 },
    interest: { type: 'normal', variancePercent: 1 }
});

// 4x4 Matrix: Rent, Vacancy, Interest, Growth
const correlationMatrix = ref([
    [1.0, -0.7, 0.0, 0.7],
    [-0.7, 1.0, 0.3, -0.6],
    [0.0, 0.3, 1.0, -0.5],
    [0.7, -0.6, -0.5, 1.0]
]);

const showCorrelationModal = ref(false);

const results = ref(null);
const running = ref(false);

const initialEquity = computed(() => {
  // For existing holdings: Value - Loan
  // For acquisitions: Total Cost - Loan
  const stampDuty = inputs.value.propertyValue * (inputs.value.stampDutyRate / 100);
  const buyersAgentFee = inputs.value.propertyValue * (inputs.value.buyersAgentFeeRate / 100);
  const acquisitionCosts = stampDuty + buyersAgentFee + inputs.value.legalCosts + inputs.value.capExReserve;
  return (inputs.value.propertyValue + acquisitionCosts) - inputs.value.loanAmount;
});

onMounted(() => {
  // Map initial data to inputs
  mapInputs();
  
  if (props.initialResults) {
      // Parse JSON fields if necessary
      const r = { ...props.initialResults };
      if (typeof r.yearlyDCFJson === 'string') r.yearlyDCF = JSON.parse(r.yearlyDCFJson);
      if (typeof r.npvHistogramJson === 'string') r.npvHistogram = JSON.parse(r.npvHistogramJson);
      if (typeof r.irrHistogramJson === 'string') r.irrHistogram = JSON.parse(r.irrHistogramJson); // Not strictly used yet but good to have
      results.value = r;
  } else if (props.autoRun) {
    run();
  }
});

// Flag to skip inputs watcher during initial mapping
let skipInputsWatch = false;
let inputsDebounceTimer = null;

watch(() => props.initialResults, (newResults, oldResults) => {
    if (newResults) {
      const r = { ...newResults };
      if (typeof r.yearlyDCFJson === 'string') r.yearlyDCF = JSON.parse(r.yearlyDCFJson);
      if (typeof r.npvHistogramJson === 'string') r.npvHistogram = JSON.parse(r.npvHistogramJson);
      if (typeof r.irrHistogramJson === 'string') r.irrHistogram = JSON.parse(r.irrHistogramJson);
      
      // Preserve probability curve if new results don't have one but values match
      // This prevents "unloading" effect when backend syncs partial results
      if (!r.probabilityCurve && results.value?.probabilityCurve && r.medianNPV === results.value.medianNPV) {
          r.probabilityCurve = results.value.probabilityCurve;
      }

      // Always update results - this ensures UI reflects latest simulation
      results.value = r;

      // NOTE: Removed automatic run() trigger - user must click "Run Analysis" to generate probabilityCurve
      // This prevents infinite loop when results sync back from parent
    } else if (newResults === null && oldResults !== null) {
      // Explicitly clear results when reset
      results.value = null;
    }
}, { deep: true, immediate: true });

// Sync Distributions from inputs if available
watch(() => props.initialData, () => {
  skipInputsWatch = true;
  mapInputs();
  // Reset skip flag after Vue processes the reactivity
  setTimeout(() => { skipInputsWatch = false; }, 0);
}, { deep: true });

watch(inputs, (newVal) => {
  // Skip during initial mapping to prevent cascading runs
  if (skipInputsWatch) return;
  
  // Emit update for parent to persist changes
  emit('update:inputs', newVal);
  
  // Debounce auto-run to prevent rapid successive simulation runs (300ms)
  if (props.autoRun) {
    if (inputsDebounceTimer) clearTimeout(inputsDebounceTimer);
    inputsDebounceTimer = setTimeout(() => {
      if (!running.value) run();
    }, 300);
  }
}, { deep: true });

function mapInputs() {
  const d = props.initialData;
  inputs.value = {
    ...inputs.value,
    propertyValue: d.askingPrice || d.currentValue || 0,
    estimatedGrossRent: d.estimatedGrossRent || d.annualRent || 0,
    outgoingsEstimate: d.outgoingsEstimate || d.annualOutgoings || 0,
    loanAmount: d.loanAmount || 0,
    // Add other mappings as needed, falling back to defaults
    ...d
  };
  
  // Ensure askingPrice maps to propertyValue calculation base
  if (d.askingPrice) {
      inputs.value.askingPrice = d.askingPrice; 
      // runSimulation uses 'askingPrice' for acquisition cost logic
  } else {
      // Map legacy variance inputs to Distribution Objects for backward compat
      // If props.initialData has `distributionSettingsJson`, parse it.
      if (d.distributionSettingsJson) {
           try {
             const saved = JSON.parse(d.distributionSettingsJson);
             distributions.value = { ...distributions.value, ...saved };
           } catch(e) {}
      } else {
           // Default map from flat variances
           distributions.value.rent.variancePercent = d.rentVariancePercent || 10;
           distributions.value.vacancy.variancePercent = d.vacancyVariancePercent || 5;
           distributions.value.growth.variancePercent = d.capitalGrowthVariancePercent || 2;
           distributions.value.interest.variancePercent = d.interestVariancePercent || 1;
      }
      
      if (d.correlationMatrixJson) {
          try {
              correlationMatrix.value = JSON.parse(d.correlationMatrixJson);
          } catch(e) {}
      }
  }
}

function updateDistribution(key, config) {
    distributions.value[key] = config;
    run();
    // Persist distribution settings
    emitDistributionSettings();
}

function updateCorrelationMatrix(newMatrix) {
    correlationMatrix.value = newMatrix;
    showCorrelationModal.value = false;
    run();
    // Persist correlation matrix
    emit('update:inputs', { 
      ...inputs.value, 
      correlationMatrixJson: JSON.stringify(newMatrix) 
    });
}

// Debounced emit for distribution settings
let distUpdateTimer = null;
function emitDistributionSettings() {
    if (distUpdateTimer) clearTimeout(distUpdateTimer);
    distUpdateTimer = setTimeout(() => {
        emit('update:inputs', { 
            ...inputs.value, 
            distributionSettingsJson: JSON.stringify(distributions.value)
        });
    }, 500);
}

async function run() {
  running.value = true;
  // Small delay to allow UI to show spinner
  await new Promise(r => setTimeout(r, 50));
  
  try {
    // Always use detailed cashflows if available (Assumptions -> Detailed -> Risk workflow)
    const simulationInputs = {
        ...inputs.value,
        distributions: distributions.value,
        correlationMatrix: correlationMatrix.value,
        detailedCashflows: props.detailedCashflow?.rows?.length ? transformGridToCashflows(props.detailedCashflow) : null
    };
    const res = runSimulation(simulationInputs, 5000, inputs.value.includeAcquisitionCost);
    results.value = res;
  } catch (e) {
    console.error(e);
  } finally {
    running.value = false;
  }
}

function transformGridToCashflows(gridData) {
    if (!gridData || !gridData.rows) return null;
    
    // We need to extract the "Net Cashflow" row for each year
    // The gridData structure from CashflowSpreadsheet is complex (rows array with cells)
    // Actually, CashflowSpreadsheet emits `getCashflowData()` which returns { rows: [...], timeVariance: ... }
    // We need to find the 'netCashflow' row and extract yearly totals
    
    const netRow = gridData.rows.find(r => r.id === 'netCashflow');
    if (!netRow) return null;
    
    // Assuming 'netCashflow' row has yearly summary cells or we can sum monthly
    // In CashflowSpreadsheet, getCashflowData() returns rows with `cells` array. 
    // We need to know which cells correspond to which year.
    // Simplifying: If granularity is years, it's direct. If months, we need to sum.
    // BUT the simulation engine wants yearly totals usually.
    // Let's assume we map the "year total" columns if present, or sum 12 months.
    
    // Better strategy: The spreadsheet should probably do the summation logic and emit clean yearly cashflows?
    // Doing it here:
    const yearlyCashflows = [];
    
    // Check granularity based on cells length approx?
    // Or just look for cells with isYearSummary if available? 
    // The emitted data structure mirrors the internal flattenedRows. 
    // Let's iterate cells.
    
    let currentYearTotal = 0;
    let monthCount = 0;
    
    netRow.cells.forEach(cell => {
        if (cell.isYearSummary) {
            yearlyCashflows.push({ netCashflow: cell.value });
            currentYearTotal = 0;
            monthCount = 0;
        } else {
            // Note: In yearly view, every cell is a year (no isYearSummary flag likely for single year cols, or maybe isYearStart?)
            // Actually in yearly view, all columns are years.
            // In monthly view, we have 12 months then summary.
            
            // Heuristic: If we see `isYearSummary`, use it.
            // If not, and we have many cells (e.g. > 10 * 12), we might be in monthly without summaries?
            // Wait, CashflowSpreadsheet computed columns logic includes summaries.
            
            if (gridData.granularity === 'years') {
                yearlyCashflows.push({ netCashflow: cell.value });
            } else {
                // In monthly view, if we don't have isYearSummary cells for some reason,
                // we sum 12 cells. However, CashflowSpreadsheet usually provides summary cells.
                currentYearTotal += cell.value;
                monthCount++;
                if (monthCount === 12) {
                    yearlyCashflows.push({ netCashflow: currentYearTotal });
                    currentYearTotal = 0;
                    monthCount = 0;
                }
            }
        }
    });

    return yearlyCashflows;
}

function formatCurrency(val) {
  return formatCurrencyUtil(val);
}
</script>

<style scoped>
.property-risk-analyzer {
  height: 100%;
  min-height: 400px;
}

.analyzer-grid {
  display: grid;
  grid-template-columns: 320px 1fr;
  gap: var(--spacing-xl);
  height: 100%;
}

@media (max-width: 900px) {
  .analyzer-grid {
    grid-template-columns: 1fr;
  }
}

/* Inputs Column */
.inputs-column {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.column-title {
  font-size: var(--font-size-sm);
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: var(--letter-spacing-wide);
  color: var(--color-text-muted);
  margin: 0 0 var(--spacing-sm) 0;
  padding-bottom: var(--spacing-xs);
  border-bottom: 1px solid var(--color-border-subtle);
}

.input-group {
  background: var(--color-bg-elevated);
  border: 1px solid var(--color-border-subtle);
  border-radius: var(--radius-md);
  padding: var(--spacing-md);
}

.input-group h4 {
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  color: var(--color-text-muted);
  margin: 0 0 var(--spacing-sm) 0;
}

.financials-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-sm);
}

.financials-header h4 {
  margin: 0;
}

.group-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-sm);
}

.group-header h4 {
  margin: 0;
}

.input-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  min-height: 32px;
  gap: var(--spacing-sm);
}

.input-row label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  flex-shrink: 0;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
  width: 120px;
}

.input-wrapper input {
  width: 100%;
  text-align: right;
  padding: 4px 8px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  font-family: var(--font-mono);
  font-size: var(--font-size-sm);
  background: var(--color-bg-primary);
}

.input-wrapper input:focus {
  border-color: var(--color-industrial-copper);
  outline: none;
}

.input-prefix {
  position: absolute;
  left: 6px;
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  pointer-events: none;
}

.input-wrapper:has(.input-prefix) input {
  padding-left: 18px;
}

.static-value {
  font-family: var(--font-mono);
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  font-weight: 500;
}

/* Toggle Switch */
.toggle-wrapper {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
}

.toggle-label {
  color: var(--color-text-muted);
  transition: color 0.2s;
}

.toggle-label.active {
  color: var(--color-industrial-copper);
  font-weight: 600;
}

.switch {
  position: relative;
  width: 36px;
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
  inset: 0;
  background-color: var(--color-bg-tertiary);
  transition: 0.3s;
}

.slider.round {
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
  transition: 0.3s;
  border-radius: 50%;
}

input:checked + .slider {
  background-color: var(--color-industrial-copper);
}

input:checked + .slider:before {
  transform: translateX(16px);
}

/* Actions */
.actions {
  margin-top: auto;
  padding-top: var(--spacing-md);
}

.full-width {
  width: 100%;
}

.spinner {
  display: inline-block;
  width: 14px;
  height: 14px;
  border: 2px solid white;
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-right: 6px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Results Column */
.results-column {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.empty-state {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  text-align: center;
  padding: var(--spacing-xl);
  background: var(--color-bg-elevated);
  border-radius: var(--radius-lg);
  border: 1px dashed var(--color-border);
}

.results-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
  height: 100%;
}

/* Metrics Row */
.metrics-row {
  display: flex;
  gap: var(--spacing-lg);
  padding: var(--spacing-md);
  background: var(--color-bg-elevated);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border-subtle);
}

.metric {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: var(--spacing-sm);
  border-radius: var(--radius-sm);
  cursor: help;
  transition: background 0.2s;
}

.metric:hover {
  background: var(--color-bg-tertiary);
}

.metric .label {
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-muted);
  margin-bottom: 4px;
}

.metric .value {
  font-size: var(--font-size-xl);
  font-weight: 700;
  font-family: var(--font-display);
  line-height: 1.2;
}

.metric .value.positive {
  color: var(--color-success);
}

.metric .value.negative {
  color: var(--color-danger);
}

/* Tooltip on hover */
.metric[title]:hover::after {
  content: attr(title);
  position: absolute;
  bottom: 100%;
  left: 50%;
  transform: translateX(-50%);
  background: var(--color-bg-primary);
  color: var(--color-text-primary);
  padding: 4px 8px;
  border-radius: var(--radius-sm);
  font-size: 11px;
  white-space: nowrap;
  box-shadow: var(--shadow-md);
  z-index: 10;
}

/* Charts */
.chart-section {
  flex: 1;
  min-height: 200px;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border-subtle);
  padding: var(--spacing-sm);
  overflow: hidden;
}

.chart-section.small {
  flex: 0 0 auto;
  min-height: 150px;
  max-height: 180px;
}

/* Button Styles */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 8px 16px;
  border-radius: var(--radius-md);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
  font-size: var(--font-size-sm);
}

.btn-primary {
  background: var(--color-industrial-copper);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: var(--color-industrial-copper-dark);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-secondary {
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
  border: 1px solid var(--color-border);
}

.btn-sm {
  padding: 4px 10px;
  font-size: 12px;
}

.icon-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}
</style>


