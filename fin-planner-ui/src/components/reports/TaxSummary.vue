<template>
  <div class="tax-summary">
    <div class="header-row">
      <h3>Tax Summary (FY {{ fiscalYear }})</h3>
      <div class="fy-selector">
        <button @click="fiscalYear--" class="btn-icon">←</button>
        <span>{{ fiscalYear - 1 }} - {{ fiscalYear }}</span>
        <button @click="fiscalYear++" class="btn-icon">→</button>
      </div>
    </div>

    <div class="grid-cards">
      <div class="summary-card">
        <div class="label">Realized Capital Gains</div>
        <div class="value">{{ formatCurrency(data.realizedGains) }}</div>
        <div class="subtext">From asset sales</div>
      </div>
      <div class="summary-card">
        <div class="label">Dividends</div>
        <div class="value">{{ formatCurrency(data.dividends) }}</div>
        <div class="subtext">+ {{ formatCurrency(data.frankingCredits) }} Franking Credits</div>
      </div>
      <div class="summary-card highlight">
        <div class="label">Est. Tax Liability</div>
        <div class="value">{{ formatCurrency((data.realizedGains + data.dividends + data.frankingCredits) * 0.3) }}</div>
        <div class="subtext">@ 30% Marginal Rate</div>
      </div>
       <div class="summary-card info">
        <div class="label">Gross Income</div>
        <div class="value">{{ formatCurrency(data.realizedGains + data.dividends + data.frankingCredits) }}</div>
        <div class="subtext">Total Taxable Income</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue';
import api from '../../services/api';

const props = defineProps({
  portfolioId: String
});

const fiscalYear = ref(new Date().getFullYear() + (new Date().getMonth() > 5 ? 1 : 0));
const data = ref({
  realizedGains: 0,
  dividends: 0,
  frankingCredits: 0
});

const loadData = async () => {
  if (!props.portfolioId) return;
  try {
    const res = await api.get(`/reports/tax-summary/${props.portfolioId}`, {
      params: { fiscalYear: fiscalYear.value }
    });
    data.value = res.data;
  } catch (e) {
    console.error(e);
  }
};

watch(() => props.portfolioId, loadData);
watch(fiscalYear, loadData);

const formatCurrency = (val) => new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD' }).format(val || 0);

onMounted(loadData);
</script>


