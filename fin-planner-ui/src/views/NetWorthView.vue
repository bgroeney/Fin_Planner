<template>
  <div class="net-worth-view space-y-6">
    <!-- Summary Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <div class="card p-6 border-l-4 border-success">
        <span class="text-xs text-muted uppercase font-bold tracking-wider">Total Assets</span>
        <div class="text-3xl font-bold mt-2">${{ formatCurrency(summary.totalAssets) }}</div>
        <div class="text-xs text-success mt-1 flex items-center gap-1">
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="23 6 13.5 15.5 8.5 10.5 1 18"/><polyline points="17 6 23 6 23 12"/></svg>
          Everything you own
        </div>
      </div>

      <div class="card p-6 border-l-4 border-danger">
        <span class="text-xs text-muted uppercase font-bold tracking-wider">Total Liabilities</span>
        <div class="text-3xl font-bold mt-2">${{ formatCurrency(summary.totalLiabilities) }}</div>
        <div class="text-xs text-danger mt-1 flex items-center gap-1">
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="23 18 13.5 8.5 8.5 13.5 1 6"/><polyline points="17 18 23 18 23 12"/></svg>
          Everything you owe
        </div>
      </div>

      <div class="card p-6 bg-accent text-white border-none shadow-accent">
        <span class="text-xs opacity-80 uppercase font-bold tracking-wider">Net Worth</span>
        <div class="text-3xl font-bold mt-2">${{ formatCurrency(summary.netWorth) }}</div>
        <div class="text-xs opacity-80 mt-1">Your real wealth</div>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Liabilities List -->
      <div class="card overflow-hidden">
        <div class="p-6 border-b border-border flex justify-between items-center">
          <h3 class="font-bold flex items-center gap-2">
             <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 1v22M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>
            Liabilities & Loans
          </h3>
          <button @click="showAddLiability = true" class="btn btn-primary btn-sm">Add New</button>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full">
            <thead class="bg-muted text-xs uppercase text-muted">
              <tr>
                <th class="px-6 py-3 text-left">Name</th>
                <th class="px-6 py-3 text-left">Type</th>
                <th class="px-6 py-3 text-right">Interest</th>
                <th class="px-6 py-3 text-right">Balance</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr v-for="l in liabilities" :key="l.id" class="hover:bg-elevated transition-colors">
                <td class="px-6 py-4 font-medium">{{ l.name }}</td>
                <td class="px-6 py-4">
                  <span class="badge badge-outline">{{ l.type }}</span>
                </td>
                <td class="px-6 py-4 text-right text-muted">{{ l.interestRate }}%</td>
                <td class="px-6 py-4 text-right font-bold">${{ formatCurrency(l.principalAmount) }}</td>
              </tr>
              <tr v-if="!liabilities.length">
                <td colspan="4" class="px-6 py-12 text-center text-muted italic">
                  No liabilities tracked. Add one to see your net worth impact.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Allocation Visualization (Simplified Assets) -->
      <div class="card p-6">
        <h3 class="font-bold mb-6">Asset vs Liability Mix</h3>
        <div class="h-64">
           <apexchart v-if="mixSeries.length" type="donut" height="100%" :options="mixOptions" :series="mixSeries"></apexchart>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue';
import api from '../services/api';
import { usePortfolioStore } from '../stores/portfolio';

const portfolioStore = usePortfolioStore();
const summary = ref({ totalAssets: 0, totalLiabilities: 0, netWorth: 0 });
const liabilities = ref([]);
const showAddLiability = ref(false);

const formatCurrency = (val) => {
  if (val === undefined || val === null) return '0';
  return val.toLocaleString();
};

const fetchNetWorth = async () => {
  if (!portfolioStore.currentPortfolio) return;
  try {
    const response = await api.get('/NetWorth', {
      params: { portfolioId: portfolioStore.currentPortfolio.id }
    });
    summary.value = response.data;
  } catch (error) {
    console.error('Failed to fetch net worth', error);
  }
};

const mixSeries = computed(() => [summary.value.totalAssets, summary.value.totalLiabilities]);
const mixOptions = {
  labels: ['Assets', 'Liabilities'],
  colors: ['#10b981', '#ef4444'],
  stroke: { show: false },
  legend: { position: 'bottom', labels: { colors: 'var(--color-text-primary)' } },
  plotOptions: {
    pie: {
      donut: {
        size: '70%',
        labels: {
          show: true,
          total: {
            show: true,
            label: 'Net Worth',
            formatter: () => '$' + formatCurrency(summary.value.netWorth / 1000) + 'k'
          }
        }
      }
    }
  },
  chart: { background: 'transparent' }
};

onMounted(() => {
  fetchNetWorth();
});
</script>

<style scoped>
.card {
  background: var(--glass-bg);
  backdrop-filter: blur(var(--glass-blur));
  -webkit-backdrop-filter: blur(var(--glass-blur));
  border: 1px solid var(--glass-border);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-glass);
}

.shadow-accent {
  box-shadow: 0 10px 30px -5px rgba(59, 130, 246, 0.4);
}

.badge-outline {
  padding: 2px 8px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-full);
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}
</style>
