<template>
  <div class="transaction-ledger card">
    <div class="toolbar">
      <div class="filters">
        <select v-model="filterType" class="form-select">
          <option value="">All Types</option>
          <option value="Buy">Buy</option>
          <option value="Sell">Sell</option>
          <option value="Dividend">Dividend</option>
          <option value="Deposit">Deposit</option>
          <option value="Withdrawal">Withdrawal</option>
        </select>
        <input 
          v-model="searchQuery" 
          type="text" 
          placeholder="Search items..." 
          class="form-input"
        />
      </div>
      <button class="btn btn-secondary" @click="exportData">
        <span class="icon">↓</span> Export CSV
      </button>
    </div>

    <table class="data-table">
      <thead>
        <tr>
          <th>Date</th>
          <th>Type</th>
          <th>Asset</th>
          <th class="text-right">Units</th>
          <th class="text-right">Amount</th>
          <th>Narration</th>
        </tr>
      </thead>
      <tbody v-if="!loading && transactions.length > 0">
        <tr v-for="tx in transactions" :key="tx.id">
          <td>{{ formatDate(tx.effectiveDate) }}</td>
          <td>
            <span :class="['badge', getBadgeClass(tx.type)]">{{ tx.type }}</span>
          </td>
          <td>
            <div class="asset-cell">
              <span class="symbol">{{ tx.assetCode }}</span>
              <span class="name text-muted">{{ tx.assetName }}</span>
            </div>
          </td>
          <td class="text-right">{{ tx.units !== 0 ? tx.units.toLocaleString() : '-' }}</td>
          <td class="text-right font-mono">{{ formatCurrency(tx.amount) }}</td>
          <td class="small-text">{{ tx.narration }}</td>
        </tr>
      </tbody>
      <tbody v-else-if="loading">
        <tr><td colspan="6" class="text-center">Loading...</td></tr>
      </tbody>
      <tbody v-else>
        <tr><td colspan="6" class="text-center text-muted">No transactions found</td></tr>
      </tbody>
    </table>
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue';
import api from '../../services/api';

const props = defineProps({
  portfolioId: String
});

const transactions = ref([]);
const loading = ref(true);
const filterType = ref('');
const searchQuery = ref('');

const loadData = async () => {
  if (!props.portfolioId) return;
  loading.value = true;
  try {
    const params = {};
    if (filterType.value) params.type = filterType.value;
    if (searchQuery.value) params.search = searchQuery.value;
    
    const res = await api.get(`/reports/transactions/${props.portfolioId}`, { params });
    transactions.value = res.data;
  } catch (e) {
    console.error('Failed to load transactions', e);
  } finally {
    loading.value = false;
  }
};

watch(() => props.portfolioId, loadData);
watch([filterType, searchQuery], () => {
  // Debounce search could be added here
  loadData();
});

const formatDate = (d) => new Date(d).toLocaleDateString('en-AU');
const formatCurrency = (val) => new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD' }).format(val);

const getBadgeClass = (type) => {
  switch (type) {
    case 'Buy': return 'badge-info';
    case 'Sell': return 'badge-warning';
    case 'Dividend': return 'badge-success';
    default: return 'badge-neutral';
  }
};

const exportData = () => {
  // Simple CSV export implementation
  const headers = ['Date', 'Type', 'Symbol', 'Units', 'Amount', 'Narration'];
  const rows = transactions.value.map(t => [
    new Date(t.effectiveDate).toLocaleDateString(),
    t.type,
    t.assetCode,
    t.units,
    t.amount,
    `"${t.narration}"`
  ]);
  
  const csvContent = [
    headers.join(','),
    ...rows.map(r => r.join(','))
  ].join('\n');
  
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = `transactions_${props.portfolioId}.csv`;
  link.click();
};

onMounted(loadData);
</script>


