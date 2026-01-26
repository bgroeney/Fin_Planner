<template>
  <div class="monte-carlo-chart">
    <h4 class="chart-title">{{ title }}</h4>
    <div class="chart-container">
      <svg :viewBox="`0 0 ${width} ${height}`" class="histogram-svg">
        <!-- Bars -->
        <g class="bars">
          <rect
            v-for="(bar, index) in normalizedBars"
            :key="index"
            :x="bar.x"
            :y="bar.y"
            :width="bar.width"
            :height="bar.height"
            :class="getBarClass(bar)"
            class="bar"
          />
        </g>
        
        <!-- Percentile Lines -->
        <g class="percentile-lines">
          <line
            :x1="p10Position"
            :y1="padding"
            :x2="p10Position"
            :y2="height - padding"
            class="percentile-line p10-line"
          />
          <line
            :x1="medianPosition"
            :y1="padding"
            :x2="medianPosition"
            :y2="height - padding"
            class="percentile-line median-line"
          />
          <line
            :x1="p90Position"
            :y1="padding"
            :x2="p90Position"
            :y2="height - padding"
            class="percentile-line p90-line"
          />
          
          <!-- Zero line if applicable -->
          <line
            v-if="showZeroLine"
            :x1="zeroPosition"
            :y1="padding"
            :x2="zeroPosition"
            :y2="height - padding"
            class="zero-line"
          />
        </g>

        <!-- Labels -->
        <g class="labels">
          <text :x="p10Position" :y="height - 5" class="percentile-label">P10</text>
          <text :x="medianPosition" :y="height - 5" class="percentile-label median-label">P50</text>
          <text :x="p90Position" :y="height - 5" class="percentile-label">P90</text>
        </g>
        
        <!-- Axis Value Labels -->
        <g class="axis-values">
            <text :x="padding" :y="height - 5" class="axis-value start">{{ formatMoney(dataMin) }}</text>
            <text :x="width - padding" :y="height - 5" class="axis-value end">{{ formatMoney(dataMax) }}</text>
        </g>
      </svg>
    </div>
    
    <!-- Legend -->
    <div class="chart-legend">
      <div class="legend-item">
        <span class="legend-color negative"></span>
        <span class="legend-text">Negative NPV</span>
      </div>
      <div class="legend-item">
        <span class="legend-color positive"></span>
        <span class="legend-text">Positive NPV</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { formatCurrency } from '../../services/monteCarloEngine';

const props = defineProps({
  histogram: { type: Array, required: true },
  p10: { type: Number, required: true },
  median: { type: Number, required: true },
  p90: { type: Number, required: true },
  title: { type: String, default: 'Distribution' }
});

// Chart dimensions
const width = 400;
const height = 150;
const padding = 20;
const barGap = 2;

// Computed
const dataMin = computed(() => {
  if (!props.histogram?.length) return 0;
  return Math.min(...props.histogram.map(b => b.min));
});

const dataMax = computed(() => {
  if (!props.histogram?.length) return 0;
  return Math.max(...props.histogram.map(b => b.max));
});

const dataRange = computed(() => dataMax.value - dataMin.value || 1);

const maxCount = computed(() => {
  if (!props.histogram?.length) return 1;
  return Math.max(...props.histogram.map(b => b.count)) || 1;
});

const chartWidth = computed(() => width - padding * 2);
const chartHeight = computed(() => height - padding * 2);

const normalizedBars = computed(() => {
  if (!props.histogram?.length) return [];
  
  const barWidth = (chartWidth.value / props.histogram.length) - barGap;
  
  return props.histogram.map((bar, index) => {
    const normalizedHeight = (bar.count / maxCount.value) * chartHeight.value;
    const midValue = (bar.min + bar.max) / 2;
    
    return {
      x: padding + index * (barWidth + barGap),
      y: height - padding - normalizedHeight,
      width: barWidth,
      height: normalizedHeight,
      value: midValue,
      count: bar.count,
      isNegative: midValue < 0
    };
  });
});

const valueToPosition = (value) => {
  return padding + ((value - dataMin.value) / dataRange.value) * chartWidth.value;
};

const p10Position = computed(() => valueToPosition(props.p10));
const medianPosition = computed(() => valueToPosition(props.median));
const p90Position = computed(() => valueToPosition(props.p90));

const showZeroLine = computed(() => {
  return dataMin.value < 0 && dataMax.value > 0;
});

const zeroPosition = computed(() => valueToPosition(0));

// Methods
function getBarClass(bar) {
  if (bar.isNegative) return 'bar-negative';
  return 'bar-positive';
}

function formatMoney(val) {
    if (Math.abs(val) >= 1000000) return `$${(val / 1000000).toFixed(1)}M`;
    if (Math.abs(val) >= 1000) return `$${(val / 1000).toFixed(0)}K`;
    return `$${val.toFixed(0)}`;
}
</script>


