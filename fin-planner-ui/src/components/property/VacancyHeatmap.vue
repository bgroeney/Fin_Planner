<template>
  <div class="vacancy-heatmap">
    <div v-if="data.length === 0" class="empty-state">
      <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
        <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
        <line x1="16" y1="2" x2="16" y2="6"></line>
        <line x1="8" y1="2" x2="8" y2="6"></line>
        <line x1="3" y1="10" x2="21" y2="10"></line>
      </svg>
      <p>No lease expirations in the next 24 months</p>
    </div>
    <template v-else>
      <apexchart
        type="bar"
        height="220"
        :options="chartOptions"
        :series="series"
      />
      <div class="legend">
        <div class="legend-item">
          <span class="legend-color legend-danger"></span>
          <span>Expiring &lt; 6mo</span>
        </div>
        <div class="legend-item">
          <span class="legend-color legend-warning"></span>
          <span>Expiring 6-12mo</span>
        </div>
        <div class="legend-item">
          <span class="legend-color legend-safe"></span>
          <span>Expiring &gt; 12mo</span>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import VueApexCharts from 'vue3-apexcharts';

const props = defineProps({
  data: {
    type: Array,
    default: () => []
  }
});

// Group data by month and calculate totals
const groupedData = computed(() => {
  const months = {};
  const now = new Date();

  props.data.forEach(item => {
    const monthKey = new Date(item.month).toISOString().slice(0, 7);
    if (!months[monthKey]) {
      months[monthKey] = {
        month: new Date(item.month),
        total: 0,
        tenants: []
      };
    }
    months[monthKey].total += item.expiringRent;
    months[monthKey].tenants.push(item.tenantName);
  });

  return Object.values(months).sort((a, b) => a.month - b.month);
});

const series = computed(() => [{
  name: 'Expiring Rent',
  data: groupedData.value.map(item => ({
    x: formatMonth(item.month),
    y: item.total,
    tenants: item.tenants
  }))
}]);

const chartOptions = computed(() => {
  const now = new Date();
  
  return {
    chart: {
      type: 'bar',
      toolbar: { show: false },
      fontFamily: 'Plus Jakarta Sans, Inter, sans-serif'
    },
    plotOptions: {
      bar: {
        borderRadius: 4,
        columnWidth: '70%',
        distributed: true
      }
    },
    dataLabels: { enabled: false },
    xaxis: {
      type: 'category',
      labels: {
        style: {
          colors: 'var(--color-text-muted)',
          fontSize: '11px'
        },
        rotate: -45,
        rotateAlways: true
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
    colors: groupedData.value.map(item => {
      const monthsAway = monthsDiff(now, item.month);
      if (monthsAway <= 6) return '#dc2626';
      if (monthsAway <= 12) return '#d97706';
      return '#059669';
    }),
    legend: { show: false },
    tooltip: {
      custom: function({ series, seriesIndex, dataPointIndex, w }) {
        const data = w.config.series[0].data[dataPointIndex];
        return `
          <div style="padding: 12px; font-family: inherit;">
            <div style="font-weight: 600; margin-bottom: 8px;">${data.x}</div>
            <div style="color: var(--color-text-secondary); margin-bottom: 4px;">
              Expiring: $${data.y.toLocaleString()}
            </div>
            <div style="font-size: 12px; color: var(--color-text-muted);">
              ${data.tenants.join(', ')}
            </div>
          </div>
        `;
      }
    },
    grid: {
      borderColor: 'var(--color-border-subtle)',
      strokeDashArray: 4,
      xaxis: { lines: { show: false } }
    }
  };
});

function formatMonth(date) {
  return new Date(date).toLocaleDateString('en-AU', { month: 'short', year: '2-digit' });
}

function monthsDiff(from, to) {
  const fromDate = new Date(from);
  const toDate = new Date(to);
  return (toDate.getFullYear() - fromDate.getFullYear()) * 12 
    + (toDate.getMonth() - fromDate.getMonth());
}
</script>

<style scoped>
.vacancy-heatmap {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

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

.legend {
  display: flex;
  justify-content: center;
  gap: var(--spacing-lg);
  padding-top: var(--spacing-sm);
  border-top: 1px solid var(--color-border-subtle);
}

.legend-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.legend-color {
  width: 12px;
  height: 12px;
  border-radius: 2px;
}

.legend-danger {
  background: #dc2626;
}

.legend-warning {
  background: #d97706;
}

.legend-safe {
  background: #059669;
}
</style>
