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
    [1, 0, 0, 0],
    [0, 1, 0, 0],
    [0, 0, 1, 0],
    [0, 0, 0, 1]
]);

const showCorrelationModal = ref(false);

const results = ref(null);
const running = ref(false);

const initialEquity = computed(() => {
  // For existing holdings: Value - Loan
  // For acquisitions: Total Cost - Loan
  const acquisitionCosts = (inputs.value.propertyValue * (inputs.value.stampDutyRate / 100)) + inputs.value.legalCosts + inputs.value.capExReserve;
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

watch(() => props.initialResults, (newResults) => {
    if(newResults) {
      const r = { ...newResults };
      if (typeof r.yearlyDCFJson === 'string') r.yearlyDCF = JSON.parse(r.yearlyDCFJson);
      if (typeof r.npvHistogramJson === 'string') r.npvHistogram = JSON.parse(r.npvHistogramJson);
      if (typeof r.irrHistogramJson === 'string') r.irrHistogram = JSON.parse(r.irrHistogramJson);
      results.value = r;
    }
}, { deep: true, immediate: true });

// Sync Distributions from inputs if available
watch(() => props.initialData, () => {
  mapInputs();
}, { deep: true });

watch(inputs, (newVal) => {
  if (props.autoRun) run();
  // Emit update for parent to persist changes if needed
  emit('update:inputs', newVal);
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
}

function updateCorrelationMatrix(newMatrix) {
    correlationMatrix.value = newMatrix;
    showCorrelationModal.value = false;
    run();
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
    results.value = runSimulation(simulationInputs, 5000, inputs.value.includeAcquisitionCost);
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
  background: var(--color-bg-card);
}

.analyzer-grid {
  display: grid;
  grid-template-columns: 350px 1fr;
  gap: var(--spacing-lg);
}

@media (max-width: 1000px) {
  .analyzer-grid {
    grid-template-columns: 1fr;
  }
}

.column-title {
  font-size: var(--font-size-lg);
  font-weight: 600;
  margin-bottom: var(--spacing-lg);
  border-bottom: 2px solid var(--color-industrial-copper);
  padding-bottom: var(--spacing-sm);
}

.input-group {
  margin-bottom: var(--spacing-lg);
  background: var(--color-bg-elevated);
  padding: var(--spacing-md);
  border-radius: var(--radius-md);
}

.input-group h4 {
  margin: 0 0 var(--spacing-sm) 0;
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  color: var(--color-text-muted);
  letter-spacing: 0.05em;
}

.input-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-sm);
}

.input-row label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.input-wrapper {
  display: flex;
  align-items: center;
  gap: 4px;
}

.input-wrapper input {
  width: 100px;
  padding: 4px 8px;
  border: 1px solid var(--color-border);
  border-radius: 4px;
  text-align: right;
  font-family: var(--font-mono);
  font-size: var(--font-size-sm);
}

.icon-inline {
  display: inline-block;
  vertical-align: middle;
  margin-right: 4px;
  color: var(--color-icon-muted);
}

.input-prefix, .input-suffix {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.variance-indicator {
  font-size: 10px;
  padding: 2px 4px;
  background: rgba(59, 130, 246, 0.1);
  color: var(--color-info);
  border-radius: 4px;
  margin-left: 4px;
}

.full-width {
  width: 100%;
}

.results-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.metrics-row {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-md);
}

.metric {
  background: var(--color-bg-elevated);
  padding: var(--spacing-md);
  border-radius: var(--radius-md);
  text-align: center;
}

.metric .label {
  display: block;
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  color: var(--color-text-muted);
  margin-bottom: 4px;
}

.metric .value {
  display: block;
  font-size: var(--font-size-lg);
  font-weight: 700;
  font-family: var(--font-mono);
}

.value.positive { color: var(--color-success); }
.value.negative { color: var(--color-danger); }

.spinner {
  display: inline-block;
  width: 12px;
  height: 12px;
  border: 2px solid currentColor;
  border-right-color: transparent;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-right: 8px;
}

@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }



/* Toggle Switch Styles */
.financials-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 0 0 var(--spacing-sm) 0;
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

.icon-btn {
  display: flex;
  align-items: center;
  gap: 6px;
}

.btn-sm {
  padding: 4px 12px;
  font-size: 11px;
}

.toggle-wrapper {
  display: flex;
  align-items: center;
  gap: 8px;
}

.toggle-label {
  font-size: 10px;
  color: var(--color-text-muted);
  font-weight: 600;
  text-transform: uppercase;
  transition: color 0.2s;
}

.toggle-label.active {
  color: var(--color-industrial-copper);
}

.switch {
  position: relative;
  display: inline-block;
  width: 32px;
  height: 18px;
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
  background-color: var(--color-bg-tertiary);
  transition: .4s;
  border: 1px solid var(--color-border);
}

.slider:before {
  position: absolute;
  content: "";
  height: 12px;
  width: 12px;
  left: 2px;
  bottom: 2px;
  background-color: white;
  transition: .4s;
  box-shadow: 0 1px 2px rgba(0,0,0,0.1);
}

input:checked + .slider {
  background-color: var(--color-industrial-copper);
  border-color: var(--color-industrial-copper);
}

input:checked + .slider:before {
  transform: translateX(14px);
}

.slider.round {
  border-radius: 34px;
}

.slider.round:before {
  border-radius: 50%;
}
</style>
