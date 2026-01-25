<template>
  <div class="allocation-section" v-if="series.length > 0">
    <div class="chart-wrapper">
      <apexchart 
        type="donut" 
        height="200"
        :options="chartOptions" 
        :series="series"
      ></apexchart>
    </div>
    <div class="legend-wrapper">
      <div v-for="(label, i) in labels" :key="label" class="legend-item">
        <span class="legend-dot" :style="{ background: colors[i % colors.length] }"></span>
        <span class="legend-label">{{ label }}</span>
        <span class="legend-value">{{ formatCurrency(seriesValues[i]) }}</span>
        <span class="legend-percent">{{ getPercent(i) }}%</span>
      </div>
    </div>
  </div>
  <div v-else class="empty-state text-muted text-center">
    No allocation data
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  holdings: { type: Array, required: true }
});

const colors = ['#1e40af', '#3b82f6', '#60a5fa', '#93c5fd', '#2563eb', '#1d4ed8', '#dbeafe'];

const seriesValues = computed(() => {
  const map = {};
  props.holdings.forEach(h => {
    const cat = h.categoryName || 'Uncategorized';
    map[cat] = (map[cat] || 0) + h.currentValue;
  });
  return Object.values(map);
});

const series = computed(() => seriesValues.value);

const labels = computed(() => {
  const map = {};
  props.holdings.forEach(h => {
    const cat = h.categoryName || 'Uncategorized';
    map[cat] = (map[cat] || 0) + h.currentValue;
  });
  return Object.keys(map);
});

const total = computed(() => seriesValues.value.reduce((a, b) => a + b, 0));

const getPercent = (i) => {
  if (total.value === 0) return '0';
  return ((seriesValues.value[i] / total.value) * 100).toFixed(1);
};

const formatCurrency = (val) => new Intl.NumberFormat('en-AU', { 
  style: 'currency', 
  currency: 'AUD',
  maximumFractionDigits: 0 
}).format(val);

const chartOptions = computed(() => ({
  labels: labels.value,
  colors: colors,
  chart: {
    type: 'donut',
    fontFamily: 'inherit'
  },
  plotOptions: {
    pie: {
      donut: {
        size: '75%',
        labels: {
          show: true,
          name: { show: false },
          value: { show: false },
          total: {
            show: true,
            label: 'Total',
            fontSize: '12px',
            fontWeight: 600,
            color: 'var(--color-text-muted)',
            formatter: () => formatCurrency(total.value)
          }
        }
      }
    }
  },
  dataLabels: { enabled: false },
  legend: { show: false },
  stroke: { width: 2, colors: ['var(--color-bg-secondary)'] },
  tooltip: {
    y: {
      formatter: (val) => formatCurrency(val)
    }
  }
}));
</script>

<style scoped>
.allocation-section {
  display: flex;
  gap: var(--spacing-lg);
  align-items: center;
  padding: var(--spacing-lg);
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  flex-wrap: wrap; /* Allow wrapping */
}

@media (max-width: 600px) {
  .allocation-section {
    flex-direction: column;
    align-items: stretch;
    gap: var(--spacing-md);
  }

  .chart-wrapper {
    width: 100%;
    display: flex;
    justify-content: center;
  }
}


.chart-wrapper {
  flex-shrink: 0;
  width: 200px;
}

.legend-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.legend-item {
  display: grid;
  grid-template-columns: 12px 1fr auto auto;
  gap: var(--spacing-sm);
  align-items: center;
  font-size: var(--font-size-sm);
}

.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
}

.legend-label {
  color: var(--color-text-primary);
  font-weight: 500;
}

.legend-value {
  color: var(--color-text-secondary);
  text-align: right;
}

.legend-percent {
  color: var(--color-text-muted);
  text-align: right;
  width: 50px;
}

.empty-state {
  padding: var(--spacing-xl);
}
</style>
