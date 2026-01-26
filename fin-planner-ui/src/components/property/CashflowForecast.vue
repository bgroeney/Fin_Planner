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


