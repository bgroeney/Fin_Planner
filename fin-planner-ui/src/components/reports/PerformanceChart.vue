<template>
  <div class="performance-chart card">
    <div class="chart-header">
      <h3>Portfolio Performance</h3>
      <div class="controls-row">
        <div class="benchmark-selector">
          <select v-model="selectedBenchmark" class="form-select-sm">
            <option value="">No Benchmark</option>
            <option value="ASX200">ASX 200</option>
            <option value="S&P500">S&P 500</option>
            <option value="ALLORDS">All Ordinaries</option>
          </select>
        </div>
        <div class="range-selector">
          <button v-for="r in ranges" :key="r" 
            :class="{ active: range === r }"
            @click="range = r"
          >{{ r }}</button>
        </div>
      </div>
    </div>
    
    <div class="chart-container">
      <apexchart v-if="hasData"
        type="area" 
        height="350" 
        :options="chartOptions" 
        :series="series"
      ></apexchart>
      <div v-else class="empty-chart">
        <p>No performance data available.</p>
        <button @click="rebuildHistory" class="btn btn-primary btn-sm">Build History</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onMounted, computed } from 'vue';
import api from '../../services/api';

const props = defineProps({
  portfolioId: String,
  benchmark: String
});

const range = ref('1Y');
const selectedBenchmark = ref('');
const ranges = ['1M', '3M', '6M', '1Y', '3Y', '5Y', 'ALL'];
const historyData = ref([]);
const benchmarkData = ref([]);

const hasData = computed(() => historyData.value.length > 0);

const series = computed(() => {
  const s = [{
    name: 'Portfolio Value',
    data: historyData.value.map(d => [new Date(d.date).getTime(), d.totalValue])
  }];

  if (benchmarkData.value && benchmarkData.value.length > 0) {
    const startValue = historyData.value[0]?.totalValue || 0;
    const benchStart = benchmarkData.value[0]?.price || 1;
    const multiplier = startValue / benchStart;

    s.push({
      name: selectedBenchmark.value,
      data: benchmarkData.value.map(d => [new Date(d.date).getTime(), d.price * multiplier])
    });
  }

  return s;
});

const chartOptions = computed(() => ({
  chart: {
    type: 'area',
    toolbar: { show: false },
    animations: { enabled: true }
  },
  dataLabels: { enabled: false },
  stroke: { 
    curve: 'smooth', 
    width: [3, 2],
    dashArray: [0, 5] // Dashed line for benchmark
  },
  fill: {
    type: 'gradient',
    gradient: {
      shadeIntensity: 1,
      opacityFrom: 0.7,
      opacityTo: 0.3,
    }
  },
  xaxis: {
    type: 'datetime',
    tooltip: { enabled: false }
  },
  yaxis: {
    labels: {
      formatter: (val) => '$' + (val / 1000).toFixed(1) + 'k'
    }
  },
  theme: {
    mode: 'dark', 
    palette: 'palette1'
  },
  colors: ['#3b82f6', '#9ca3af'], // Blue for Portfolio, Gray for Benchmark
  grid: {
    borderColor: '#333'
  },
  legend: {
    position: 'top',
    horizontalAlign: 'left'
  }
}));

const loadData = async () => {
  if (!props.portfolioId) return;
  try {
    const res = await api.get(`/reports/performance/${props.portfolioId}`, {
      params: { 
        range: range.value,
        benchmark: selectedBenchmark.value 
      }
    });
    
    if (res.data.portfolio) {
      historyData.value = res.data.portfolio;
      benchmarkData.value = res.data.benchmark || [];
    } else {
      historyData.value = res.data;
      benchmarkData.value = [];
    }
  } catch (e) {
    console.error(e);
  }
};

const rebuildHistory = async () => {
  if (!props.portfolioId) return;
  try {
    await api.post(`/reports/rebuild-history/${props.portfolioId}`);
    await loadData();
  } catch (e) {
    console.error(e);
  }
};

watch(() => props.portfolioId, loadData);
watch(selectedBenchmark, loadData);
watch(range, loadData);
onMounted(loadData);
</script>

<style scoped>
.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-md);
}

.controls-row {
  display: flex;
  gap: var(--spacing-md);
  align-items: center;
}

.form-select-sm {
  padding: 4px 8px;
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
  font-size: var(--font-size-xs);
  cursor: pointer;
}

.range-selector {
  background: var(--color-bg-elevated);
  padding: 4px;
  border-radius: var(--radius-md);
  display: flex;
  gap: 2px;
}

.range-selector button {
  padding: 4px 12px;
  border: none;
  background: transparent;
  color: var(--color-text-muted);
  border-radius: var(--radius-sm);
  cursor: pointer;
  font-size: var(--font-size-xs);
  font-weight: 500;
  transition: all 0.2s;
}

.range-selector button:hover {
  color: var(--color-text-primary);
}

.range-selector button.active {
  background: var(--color-bg-primary);
  color: var(--color-accent);
  box-shadow: 0 1px 2px rgba(0,0,0,0.1);
}

.empty-chart {
  height: 350px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-lg);
  color: var(--color-text-muted);
}
</style>
