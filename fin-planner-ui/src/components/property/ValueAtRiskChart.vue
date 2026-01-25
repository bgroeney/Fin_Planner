<template>
  <div class="value-at-risk-chart">
    <div class="chart-header">
      <h4 class="chart-title">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M22 12h-4l-3 9L9 3l-3 9H2"/>
        </svg>
        Value at Risk (Cumulative Probability)
      </h4>
      <div class="chart-legend">
        <span class="legend-item value-curve">{{ isNetMode ? 'Net Profit' : 'Intrinsic Value' }}</span>
        <span v-if="!isNetMode" class="legend-item cost-line">Total Cost</span>
      </div>
    </div>
    
    <div class="chart-container" ref="chartContainer">
      <svg :viewBox="`0 0 ${width} ${height}`" class="chart-svg">
        <!-- Grid lines -->
        <g class="grid-lines">
          <line 
            v-for="i in 5" 
            :key="'h'+i"
            :x1="padding.left"
            :y1="padding.top + ((i-1) * (chartHeight / 4))"
            :x2="width - padding.right"
            :y2="padding.top + ((i-1) * (chartHeight / 4))"
            class="grid-line"
          />
          <line 
            v-for="i in 5" 
            :key="'v'+i"
            :x1="padding.left + ((i-1) * (chartWidth / 4))"
            :y1="padding.top"
            :x2="padding.left + ((i-1) * (chartWidth / 4))"
            :y2="height - padding.bottom"
            class="grid-line"
          />
        </g>

        <!-- Area shading -->
        <defs>
          <linearGradient id="valueGradient" x1="0%" y1="0%" x2="0%" y2="100%">
            <stop offset="0%" stop-color="var(--color-industrial-copper)" stop-opacity="0.3"/>
            <stop offset="100%" stop-color="var(--color-industrial-copper)" stop-opacity="0.05"/>
          </linearGradient>
        </defs>

        <!-- Curve Area -->
        <path 
          :d="areaPath" 
          fill="url(#valueGradient)"
          class="curve-area"
        />

        <!-- Value Curve -->
        <path 
          :d="curvePath" 
          class="value-curve-line" 
        />

        <!-- Cost Line (Only in Gross Mode) -->
        <line
          v-if="!isNetMode"
          :x1="padding.left"
          :y1="scaleY(totalAcquisitionCost)"
          :x2="width - padding.right"
          :y2="scaleY(totalAcquisitionCost)"
          class="cost-benchmark-line"
        />
        <text
          v-if="!isNetMode"
          :x="padding.left + 5"
          :y="scaleY(totalAcquisitionCost) - 5"
          class="benchmark-label"
        >Cost: {{ formatCurrency(totalAcquisitionCost) }}</text>

        <!-- P10/P50/P90 markers -->
        <g class="markers">
           <circle :cx="scaleX(10)" :cy="scaleY(getValueAtPercent(10))" r="4" class="marker-point p10" />
           <circle :cx="scaleX(50)" :cy="scaleY(getValueAtPercent(50))" r="4" class="marker-point median" />
           <circle :cx="scaleX(90)" :cy="scaleY(getValueAtPercent(90))" r="4" class="marker-point p90" />
           
           <!-- Labels for markers -->
           <text :x="scaleX(10)" :y="scaleY(getValueAtPercent(10)) - 10" class="marker-label">P10</text>
           <text :x="scaleX(50)" :y="scaleY(getValueAtPercent(50)) - 10" class="marker-label">Median</text>
           <text :x="scaleX(90)" :y="scaleY(getValueAtPercent(90)) - 10" class="marker-label">P90</text>
        </g>

        <!-- Y-axis labels (Value) -->
        <g class="y-axis">
          <text 
            v-for="(tick, i) in yTicks" 
            :key="'y'+i"
            :x="padding.left - 8"
            :y="scaleY(tick) + 4"
            class="axis-label"
          >{{ formatAxisValue(tick) }}</text>
          
          <!-- Y Axis Title -->
          <text
            :x="padding.left - 45"
            :y="height / 2"
            transform="rotate(-90, 20, 150)"
            class="axis-title"
            style="transform-box: fill-box; transform-origin: center;"
          >{{ isNetMode ? 'Net Profit ($)' : 'Intrinsic Value ($)' }}</text>
        </g>

        <!-- X-axis labels (Probability) -->
        <g class="x-axis">
          <text 
            v-for="p in [0, 25, 50, 75, 100]" 
            :key="'x'+p"
            :x="scaleX(p)"
            :y="height - padding.bottom + 16"
            class="axis-label"
          >{{ p }}%</text>
          <text 
            :x="width / 2"
            :y="height - 5"
            class="axis-title"
          >Cumulative Probability (%)</text>
        </g>
        
        <!-- Hover Overlay -->
        <rect
            :x="padding.left"
            :y="padding.top"
            :width="chartWidth"
            :height="chartHeight"
            fill="transparent"
            @mousemove="handleMouseMove"
            @mouseleave="hideTooltip"
        />
        
        <!-- Interactive Vertical Line -->
        <line
            v-if="tooltip.visible"
            :x1="tooltip.xLine"
            :y1="padding.top"
            :x2="tooltip.xLine"
            :y2="height - padding.bottom"
            class="interactive-line"
        />
        <circle
            v-if="tooltip.visible"
            :cx="tooltip.xLine"
            :cy="scaleY(tooltip.value)"
            r="5"
            class="interactive-point"
        />

      </svg>

      <!-- Tooltip -->
      <div 
        v-if="tooltip.visible" 
        class="chart-tooltip"
        :style="{ left: tooltip.x + 'px', top: tooltip.y + 'px' }"
      >
        <div class="tooltip-header">{{ tooltip.percent.toFixed(0) }}% Probability</div>
        <div class="tooltip-value">{{ formatCurrency(tooltip.value) }}</div>
        <div class="tooltip-sub">Value &le; this amount</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { formatCurrency as formatCurrencyUtil } from '../../services/monteCarloEngine';

const props = defineProps({
  probabilityCurve: {
    type: Array, // { percent: number, value: number }
    required: true,
    default: () => []
  },
  totalAcquisitionCost: {
    type: Number,
    required: true
  },
  isNetMode: {
    type: Boolean,
    default: false
  }
});

// Chart dimensions
const width = 600;
const height = 300;
const padding = { top: 20, right: 30, bottom: 50, left: 80 };
const chartWidth = width - padding.left - padding.right;
const chartHeight = height - padding.top - padding.bottom;

const chartContainer = ref(null);
const tooltip = ref({ visible: false, x: 0, y: 0, xLine: 0, value: 0, percent: 0 });

// Min/Max for scaling
const minValue = computed(() => {
  if (!props.probabilityCurve.length) return 0;
  return Math.min(
    ...props.probabilityCurve.map(p => p.value),
    props.isNetMode ? 0 : props.totalAcquisitionCost * 0.9 // Ensure cost line is visible in Gross Mode
  );
});

const maxValue = computed(() => {
  if (!props.probabilityCurve.length) return 1000000;
  return Math.max(
    ...props.probabilityCurve.map(p => p.value),
    props.isNetMode ? 0 : props.totalAcquisitionCost * 1.1
  );
});

const yTicks = computed(() => {
  const range = maxValue.value - minValue.value;
  const step = range / 4;
  return [
    maxValue.value,
    maxValue.value - step,
    maxValue.value - step * 2,
    maxValue.value - step * 3,
    minValue.value
  ];
});

// Scales
function scaleX(percent) {
  return padding.left + (percent / 100) * chartWidth;
}

function scaleY(value) {
  const range = maxValue.value - minValue.value || 1;
  return padding.top + (1 - (value - minValue.value) / range) * chartHeight;
}

// Helpers
function getValueAtPercent(p) {
    const pt = props.probabilityCurve.find(item => item.percent === p);
    return pt ? pt.value : 0;
}

// Paths
const curvePath = computed(() => {
  if (!props.probabilityCurve.length) return '';
  return props.probabilityCurve.map((pt, i) => {
    const cmd = i === 0 ? 'M' : 'L';
    return `${cmd} ${scaleX(pt.percent)} ${scaleY(pt.value)}`;
  }).join(' ');
});

const areaPath = computed(() => {
    if (!props.probabilityCurve.length) return '';
    const first = props.probabilityCurve[0];
    const last = props.probabilityCurve[props.probabilityCurve.length - 1];
    
    let path = `M ${scaleX(first.percent)} ${height - padding.bottom} `;
    path += curvePath.value.substring(1); // Remove leading M
    path += ` L ${scaleX(last.percent)} ${height - padding.bottom} Z`;
    return path;
});

// Interaction
function handleMouseMove(event) {
    const rect = chartContainer.value.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const chartX = x - padding.left;
    
    // Convert X to Percent
    let percent = (chartX / chartWidth) * 100;
    percent = Math.max(0, Math.min(100, percent));
    
    // Find closest point
    const closest = props.probabilityCurve.reduce((prev, curr) => {
        return (Math.abs(curr.percent - percent) < Math.abs(prev.percent - percent) ? curr : prev);
    });
    
    const xLine = scaleX(closest.percent);
    
    tooltip.value = {
        visible: true,
        x: xLine + 10,
        y: scaleY(closest.value) - 40,
        xLine,
        value: closest.value,
        percent: closest.percent
    };
}

function hideTooltip() {
    tooltip.value.visible = false;
}

// Formatters
function formatCurrency(val) {
  return formatCurrencyUtil(val);
}

function formatAxisValue(value) {
  if (Math.abs(value) >= 1000000) {
    return `$${(value / 1000000).toFixed(1)}M`;
  }
  if (Math.abs(value) >= 1000) {
    return `$${(value / 1000).toFixed(0)}K`;
  }
  return `$${value.toFixed(0)}`;
}
</script>

<style scoped>
.value-at-risk-chart {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  margin: var(--spacing-lg) 0;
}

.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-md);
}

.chart-title {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: var(--font-size-base);
  font-weight: 600;
  margin: 0;
  color: var(--color-text-primary);
}

.chart-title svg {
  color: var(--color-industrial-copper);
}

.chart-legend {
    display: flex;
    gap: var(--spacing-md);
}

.legend-item {
    font-size: var(--font-size-xs);
    display: flex;
    align-items: center;
    gap: var(--spacing-xs);
}

.legend-item::before {
    content: '';
    width: 12px;
    height: 3px;
    border-radius: 2px;
}

.legend-item.value-curve::before { background: var(--color-industrial-copper); }
.legend-item.cost-line::before { background: var(--color-text-muted); border-bottom: 1px dashed; height: 0; border-width: 2px; }

.chart-container {
  position: relative;
  width: 100%;
  aspect-ratio: 2 / 1;
}

.chart-svg {
  width: 100%;
  height: 100%;
}

.grid-line {
    stroke: var(--color-border-subtle);
    stroke-dasharray: 4 4;
}

.value-curve-line {
    fill: none;
    stroke: var(--color-industrial-copper);
    stroke-width: 3;
    stroke-linecap: round;
}

.cost-benchmark-line {
    stroke: var(--color-text-muted);
    stroke-width: 1.5;
    stroke-dasharray: 6 3;
}

.benchmark-label {
    fill: var(--color-text-muted);
    font-size: 10px;
    font-weight: 600;
}

.axis-label {
    fill: var(--color-text-muted);
    font-size: 10px;
}

.y-axis .axis-label {
    text-anchor: end;
}

.x-axis .axis-label {
    text-anchor: middle;
}

.axis-title {
    fill: var(--color-text-secondary);
    font-size: 11px;
    font-weight: 500;
    text-anchor: middle;
}

.marker-point {
    fill: var(--color-bg-card);
    stroke: var(--color-industrial-copper);
    stroke-width: 2;
}

.marker-label {
    font-size: 10px;
    text-anchor: middle;
    fill: var(--color-text-muted);
}

.interactive-line {
    stroke: var(--color-primary);
    stroke-width: 1;
    stroke-dasharray: 4 2;
    pointer-events: none;
}

.interactive-point {
    fill: var(--color-primary);
    stroke: #fff;
    stroke-width: 2;
    pointer-events: none;
}

.chart-tooltip {
  position: absolute;
  background: var(--color-bg-elevated);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--spacing-sm);
  pointer-events: none;
  z-index: 10;
  box-shadow: var(--shadow-lg);
  transform: translateX(-50%);
}

.tooltip-header {
    font-size: var(--font-size-xs);
    color: var(--color-text-muted);
    text-align: center;
}

.tooltip-value {
    font-size: var(--font-size-lg);
    font-weight: 700;
    font-family: var(--font-mono);
    color: var(--color-industrial-copper);
    text-align: center;
}

.tooltip-sub {
    font-size: 10px;
    color: var(--color-text-muted);
    text-align: center;
}
</style>
