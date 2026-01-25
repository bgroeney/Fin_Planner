<template>
  <div class="cashflow-forecast">
    <!-- View Toggle -->
    <div class="view-toggle">
      <button 
        :class="['toggle-btn', { active: viewMode === 'chart' }]"
        @click="viewMode = 'chart'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline>
        </svg>
        Chart
      </button>
      <button 
        :class="['toggle-btn', { active: viewMode === 'table' }]"
        @click="viewMode = 'table'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <line x1="3" y1="6" x2="21" y2="6"></line>
          <line x1="3" y1="12" x2="21" y2="12"></line>
          <line x1="3" y1="18" x2="21" y2="18"></line>
        </svg>
        Monthly
      </button>
    </div>

    <div v-if="data.length === 0" class="empty-state">
      <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
        <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline>
      </svg>
      <p>No cashflow data available</p>
    </div>

    <!-- Chart View -->
    <template v-else-if="viewMode === 'chart'">
      <apexchart
        type="area"
        height="220"
        :options="chartOptions"
        :series="series"
      />
      <div class="summary-row">
        <div class="summary-item">
          <span class="summary-label">Avg Monthly Income</span>
          <span class="summary-value value-positive">{{ formatCurrency(avgIncome) }}</span>
        </div>
        <div class="summary-item">
          <span class="summary-label">Avg Monthly Net</span>
          <span class="summary-value" :class="avgNet >= 0 ? 'value-positive' : 'value-negative'">
            {{ formatCurrency(avgNet) }}
          </span>
        </div>
      </div>
    </template>

    <!-- Table View with Collapsible Years -->
    <template v-else>
      <div class="table-container">
        <table class="cashflow-table">
          <thead>
            <tr>
              <th class="col-period">Period</th>
              <th class="col-value text-right">Income</th>
              <th class="col-value text-right">Expenses</th>
              <th class="col-value text-right">Net Cashflow</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="(yearData, year) in groupedByYear" :key="year">
              <!-- Year Summary Row (Clickable to Expand) -->
              <tr 
                class="year-row"
                :class="{ expanded: expandedYears.has(year) }"
                @click="toggleYear(year)"
              >
                <td class="col-period">
                  <button class="expand-btn">
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <polyline v-if="expandedYears.has(year)" points="6 15 12 9 18 15"></polyline>
                      <polyline v-else points="6 9 12 15 18 9"></polyline>
                    </svg>
                  </button>
                  <span class="year-label">FY {{ year }}</span>
                </td>
                <td class="col-value text-right value-positive">{{ formatCurrency(yearData.totalIncome) }}</td>
                <td class="col-value text-right value-negative">{{ formatCurrency(yearData.totalExpenses) }}</td>
                <td class="col-value text-right" :class="yearData.totalNet >= 0 ? 'value-positive' : 'value-negative'">
                  {{ formatCurrency(yearData.totalNet) }}
                </td>
              </tr>
              
              <!-- Monthly Detail Rows -->
              <template v-if="expandedYears.has(year)">
                <tr 
                  v-for="month in yearData.months" 
                  :key="month.month.toISOString()"
                  class="month-row"
                >
                  <td class="col-period month-label">{{ formatMonth(month.month) }}</td>
                  <td class="col-value text-right">{{ formatCurrency(month.projectedIncome) }}</td>
                  <td class="col-value text-right">{{ formatCurrency(month.projectedExpenses) }}</td>
                  <td class="col-value text-right" :class="month.netCashflow >= 0 ? 'value-positive' : 'value-negative'">
                    {{ formatCurrency(month.netCashflow) }}
                  </td>
                </tr>
              </template>
            </template>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';

const props = defineProps({
  data: {
    type: Array,
    default: () => []
  }
});

const viewMode = ref('chart');
const expandedYears = ref(new Set());

// Group data by fiscal year (July to June in Australia)
const groupedByYear = computed(() => {
  const groups = {};
  
  for (const item of props.data) {
    const date = new Date(item.month);
    // Australian Fiscal Year: July to June. FY2025 = July 2024 - June 2025
    const fiscalYear = date.getMonth() >= 6 ? date.getFullYear() + 1 : date.getFullYear();
    
    if (!groups[fiscalYear]) {
      groups[fiscalYear] = {
        months: [],
        totalIncome: 0,
        totalExpenses: 0,
        totalNet: 0
      };
    }
    
    groups[fiscalYear].months.push({
      ...item,
      month: date
    });
    groups[fiscalYear].totalIncome += item.projectedIncome;
    groups[fiscalYear].totalExpenses += item.projectedExpenses;
    groups[fiscalYear].totalNet += item.netCashflow;
  }
  
  // Sort months within each year
  for (const year in groups) {
    groups[year].months.sort((a, b) => a.month - b.month);
  }
  
  return groups;
});

const series = computed(() => [
  {
    name: 'Income',
    data: props.data.map(d => ({ x: new Date(d.month).getTime(), y: d.projectedIncome }))
  },
  {
    name: 'Expenses',
    data: props.data.map(d => ({ x: new Date(d.month).getTime(), y: d.projectedExpenses }))
  },
  {
    name: 'Net Cashflow',
    data: props.data.map(d => ({ x: new Date(d.month).getTime(), y: d.netCashflow }))
  }
]);

const avgIncome = computed(() => {
  if (props.data.length === 0) return 0;
  return props.data.reduce((sum, d) => sum + d.projectedIncome, 0) / props.data.length;
});

const avgNet = computed(() => {
  if (props.data.length === 0) return 0;
  return props.data.reduce((sum, d) => sum + d.netCashflow, 0) / props.data.length;
});

const chartOptions = computed(() => ({
  chart: {
    type: 'area',
    toolbar: { show: false },
    fontFamily: 'Plus Jakarta Sans, Inter, sans-serif',
    stacked: false,
    zoom: { enabled: false }
  },
  colors: ['#059669', '#dc2626', '#1e40af'],
  fill: {
    type: 'gradient',
    gradient: {
      shadeIntensity: 1,
      opacityFrom: 0.4,
      opacityTo: 0.1,
      stops: [0, 90, 100]
    }
  },
  stroke: {
    curve: 'smooth',
    width: [2, 2, 3]
  },
  dataLabels: { enabled: false },
  xaxis: {
    type: 'datetime',
    labels: {
      style: {
        colors: 'var(--color-text-muted)',
        fontSize: '11px'
      },
      format: 'MMM yy'
    },
    axisBorder: { show: false },
    axisTicks: { show: false }
  },
  yaxis: {
    labels: {
      style: {
        colors: 'var(--color-text-muted)',
        fontSize: '12px'
      },
      formatter: (val) => `$${(val / 1000).toFixed(0)}k`
    }
  },
  legend: {
    position: 'top',
    horizontalAlign: 'right',
    fontSize: '12px',
    labels: {
      colors: 'var(--color-text-secondary)'
    },
    markers: {
      width: 10,
      height: 10,
      radius: 2
    }
  },
  tooltip: {
    x: {
      format: 'MMM yyyy'
    },
    y: {
      formatter: (val) => `$${val.toLocaleString()}`
    }
  },
  grid: {
    borderColor: 'var(--color-border-subtle)',
    strokeDashArray: 4,
    xaxis: { lines: { show: false } }
  }
}));

function toggleYear(year) {
  if (expandedYears.value.has(year)) {
    expandedYears.value.delete(year);
  } else {
    expandedYears.value.add(year);
  }
  // Trigger reactivity
  expandedYears.value = new Set(expandedYears.value);
}

function formatCurrency(value) {
  return new Intl.NumberFormat('en-AU', {
    style: 'currency',
    currency: 'AUD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value || 0);
}

function formatMonth(date) {
  return date.toLocaleDateString('en-AU', { month: 'short', year: 'numeric' });
}
</script>

<style scoped>
.cashflow-forecast {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

/* View Toggle */
.view-toggle {
  display: flex;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-md);
  padding: 2px;
  width: fit-content;
  align-self: flex-end;
}

.toggle-btn {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-xs) var(--spacing-md);
  background: transparent;
  border: none;
  font-size: var(--font-size-xs);
  font-weight: 500;
  color: var(--color-text-muted);
  cursor: pointer;
  border-radius: var(--radius-sm);
  transition: all 0.15s;
}

.toggle-btn:hover {
  color: var(--color-text-primary);
}

.toggle-btn.active {
  background: var(--color-bg-primary);
  color: var(--color-accent);
  box-shadow: 0 1px 2px rgba(0,0,0,0.1);
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2xl);
  text-align: center;
  color: var(--color-text-muted);
  gap: var(--spacing-md);
}

.empty-state svg {
  opacity: 0.5;
}

/* Summary Row */
.summary-row {
  display: flex;
  justify-content: center;
  gap: var(--spacing-2xl);
  padding-top: var(--spacing-sm);
  border-top: 1px solid var(--color-border-subtle);
}

.summary-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-xs);
}

.summary-label {
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: var(--letter-spacing-wide);
  color: var(--color-text-muted);
}

.summary-value {
  font-size: var(--font-size-lg);
  font-weight: 700;
  font-family: var(--font-display);
}

/* Table View */
.table-container {
  overflow-x: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
}

.cashflow-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}

.cashflow-table th {
  background: var(--color-bg-elevated);
  font-weight: 600;
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--color-text-muted);
  padding: var(--spacing-sm) var(--spacing-md);
  text-align: left;
  border-bottom: 1px solid var(--color-border);
}

.cashflow-table td {
  padding: var(--spacing-sm) var(--spacing-md);
  border-bottom: 1px solid var(--color-border-subtle);
}

.col-period {
  width: 140px;
}

.col-value {
  width: 120px;
  font-family: var(--font-mono);
}

.text-right {
  text-align: right;
}

/* Year Row (Summary) */
.year-row {
  background: var(--color-bg-elevated);
  cursor: pointer;
  transition: background 0.15s;
}

.year-row:hover {
  background: rgba(180, 83, 9, 0.05);
}

.year-row.expanded {
  background: rgba(180, 83, 9, 0.08);
  border-left: 3px solid var(--color-industrial-copper);
}

.year-row td {
  font-weight: 600;
  border-bottom: 1px solid var(--color-border);
}

.expand-btn {
  background: none;
  border: none;
  padding: 2px;
  margin-right: var(--spacing-xs);
  cursor: pointer;
  color: var(--color-text-muted);
  display: inline-flex;
  align-items: center;
  vertical-align: middle;
}

.year-label {
  font-weight: 700;
  letter-spacing: 0.02em;
}

/* Month Row (Detail) */
.month-row {
  background: var(--color-bg-primary);
}

.month-row:hover {
  background: var(--color-bg-elevated);
}

.month-label {
  padding-left: var(--spacing-xl) !important;
  color: var(--color-text-secondary);
}

/* Value Colors */
.value-positive {
  color: var(--color-success);
}

.value-negative {
  color: var(--color-danger);
}
</style>
