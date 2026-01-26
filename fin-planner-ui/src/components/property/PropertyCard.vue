<template>
  <div class="property-card" @click="$emit('click')">
    <div class="property-header">
      <div class="property-type-badge" :class="typeClass">
        {{ property.buildingType }}
      </div>
      <div class="property-yield" :class="yieldClass">
        {{ property.netYieldPercent.toFixed(1) }}%
      </div>
    </div>
    
    <div class="property-body">
      <h4 class="property-address">{{ property.address }}</h4>
      <div class="property-value">
        {{ formatCurrency(property.currentValue) }}
      </div>
    </div>

    <div class="property-footer">
      <div class="footer-item">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
          <polyline points="14 2 14 8 20 8"></polyline>
        </svg>
        <span>{{ property.leaseCount }} {{ property.leaseCount === 1 ? 'lease' : 'leases' }}</span>
      </div>
      <div class="footer-item" v-if="property.nextLeaseExpiry" :class="expiryClass">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10"></circle>
          <polyline points="12 6 12 12 16 14"></polyline>
        </svg>
        <span>{{ formatExpiry(property.nextLeaseExpiry) }}</span>
      </div>
    </div>

    <div class="property-arrow">
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <polyline points="9 18 15 12 9 6"></polyline>
      </svg>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  property: {
    type: Object,
    required: true
  }
});

defineEmits(['click']);

const typeClass = computed(() => ({
  'type-office': props.property.buildingType === 'Office',
  'type-retail': props.property.buildingType === 'Retail',
  'type-industrial': props.property.buildingType === 'Industrial',
  'type-mixed': props.property.buildingType === 'Mixed'
}));

const yieldClass = computed(() => {
  const y = props.property.netYieldPercent;
  if (y >= 6) return 'yield-good';
  if (y >= 4) return 'yield-medium';
  return 'yield-low';
});

const expiryClass = computed(() => {
  if (!props.property.nextLeaseExpiry) return '';
  const monthsAway = monthsDiff(new Date(), new Date(props.property.nextLeaseExpiry));
  if (monthsAway <= 6) return 'expiry-urgent';
  if (monthsAway <= 12) return 'expiry-warning';
  return '';
});

function formatCurrency(value) {
  return new Intl.NumberFormat('en-AU', {
    style: 'currency',
    currency: 'AUD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value || 0);
}

function formatExpiry(date) {
  const d = new Date(date);
  const now = new Date();
  const months = monthsDiff(now, d);
  
  if (months <= 0) return 'Expired';
  if (months === 1) return 'Exp. 1mo';
  if (months < 12) return `Exp. ${months}mo`;
  return d.toLocaleDateString('en-AU', { month: 'short', year: '2-digit' });
}

function monthsDiff(from, to) {
  return (to.getFullYear() - from.getFullYear()) * 12 
    + (to.getMonth() - from.getMonth());
}
</script>


