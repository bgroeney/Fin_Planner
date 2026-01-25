<template>
  <div class="yield-gauge">
    <div class="gauge-container">
      <apexchart
        type="radialBar"
        height="280"
        :options="chartOptions"
        :series="series"
      />
    </div>
    <div class="gauge-metrics">
      <div class="metric">
        <span class="metric-label">Gross Income</span>
        <span class="metric-value value-positive">{{ formatCurrency(grossIncome) }}</span>
      </div>
      <div class="metric-divider"></div>
      <div class="metric">
        <span class="metric-label">Expenses</span>
        <span class="metric-value value-negative">-{{ formatCurrency(expenses) }}</span>
      </div>
      <div class="metric-divider"></div>
      <div class="metric">
        <span class="metric-label">Net Income</span>
        <span class="metric-value" :class="netIncome >= 0 ? 'value-positive' : 'value-negative'">
          {{ formatCurrency(netIncome) }}
        </span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import VueApexCharts from 'vue3-apexcharts';

const props = defineProps({
  yieldPercent: {
    type: Number,
    default: 0
  },
  targetYield: {
    type: Number,
    default: 6
  },
  grossIncome: {
    type: Number,
    default: 0
  },
  expenses: {
    type: Number,
    default: 0
  }
});

const netIncome = computed(() => props.grossIncome - props.expenses);

const series = computed(() => [Math.min(props.yieldPercent, 12)]); // Cap at 12% for display

const chartOptions = computed(() => {
  const yieldColor = props.yieldPercent >= props.targetYield 
    ? '#059669' // green
    : props.yieldPercent >= 4 
      ? '#d97706' // yellow
      : '#dc2626'; // red

  return {
    chart: {
      type: 'radialBar',
      sparkline: { enabled: false },
      fontFamily: 'Plus Jakarta Sans, Inter, sans-serif'
    },
    plotOptions: {
      radialBar: {
        startAngle: -135,
        endAngle: 135,
        hollow: {
          size: '65%',
          background: 'transparent'
        },
        track: {
          background: 'var(--color-border)',
          strokeWidth: '100%',
          margin: 0
        },
        dataLabels: {
          name: {
            show: true,
            fontSize: '14px',
            fontWeight: 500,
            color: 'var(--color-text-muted)',
            offsetY: -15
          },
          value: {
            show: true,
            fontSize: '32px',
            fontWeight: 700,
            color: yieldColor,
            offsetY: 5,
            formatter: () => `${props.yieldPercent.toFixed(1)}%`
          }
        }
      }
    },
    labels: ['Net Yield'],
    colors: [yieldColor],
    stroke: {
      lineCap: 'round'
    },
    annotations: {
      position: 'front',
      yaxis: [{
        y: props.targetYield,
        borderColor: '#94a3b8',
        strokeDashArray: 4,
        label: {
          text: `Target: ${props.targetYield}%`,
          style: {
            color: 'var(--color-text-muted)',
            fontSize: '12px'
          }
        }
      }]
    }
  };
});

function formatCurrency(value) {
  return new Intl.NumberFormat('en-AU', {
    style: 'currency',
    currency: 'AUD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value || 0);
}
</script>

<style scoped>
.yield-gauge {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-lg);
}

.gauge-container {
  width: 100%;
  max-width: 300px;
}

.gauge-metrics {
  display: flex;
  justify-content: center;
  gap: var(--spacing-xl);
  padding: var(--spacing-md);
  background: var(--color-bg-elevated);
  border-radius: var(--radius-lg);
  width: 100%;
}

.metric {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-xs);
}

.metric-label {
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: var(--letter-spacing-wide);
  color: var(--color-text-muted);
}

.metric-value {
  font-size: var(--font-size-lg);
  font-weight: 700;
  font-family: var(--font-display);
}

.metric-divider {
  width: 1px;
  background: var(--color-border);
}
</style>
