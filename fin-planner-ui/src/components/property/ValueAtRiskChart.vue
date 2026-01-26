<template>
  <div class="value-at-risk-chart">
    <div class="chart-header">
      <h4 class="chart-title">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M22 12h-4l-3 9L9 3l-3 9H2"/>
        </svg>
        Value at Risk (Cumulative Distribution)
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

        <!-- Cost Line (Only in Gross Mode when visible) -->
        <line
          v-if="!isNetMode && isBenchmarkVisible"
          :x1="scaleX(totalAcquisitionCost)"
          :y1="padding.top"
          :x2="scaleX(totalAcquisitionCost)"
          :y2="height - padding.bottom"
          class="cost-benchmark-line"
        />
        <text
          v-if="!isNetMode && isBenchmarkVisible"
          :x="scaleX(totalAcquisitionCost) + 5"
          :y="padding.top + 10"
          class="benchmark-label"
          style="text-anchor: start;"
        >Cost: {{ formatCurrency(totalAcquisitionCost) }}</text>
        
        <!-- Off-screen benchmark indicator (arrow pointing right) -->
        <g v-if="!isNetMode && !isBenchmarkVisible && totalAcquisitionCost > maxValue">
          <polygon 
            :points="`${width - padding.right - 15},${padding.top + 20} ${width - padding.right},${padding.top + 25} ${width - padding.right - 15},${padding.top + 30}`"
            fill="var(--color-text-muted)"
          />
          <text
            :x="width - padding.right - 20"
            :y="padding.top + 28"
            class="benchmark-label"
            style="text-anchor: end; font-size: 9px;"
          >Cost: {{ formatCurrency(totalAcquisitionCost) }} →</text>
        </g>

        <!-- P10/P50/P90 markers -->
        <g class="markers">
           <circle :cx="scaleX(getValueAtPercent(10))" :cy="scaleY(10)" r="4" class="marker-point p10" />
           <circle :cx="scaleX(getValueAtPercent(50))" :cy="scaleY(50)" r="4" class="marker-point median" />
           <circle :cx="scaleX(getValueAtPercent(90))" :cy="scaleY(90)" r="4" class="marker-point p90" />
           
           <!-- Labels for markers -->
           <text :x="scaleX(getValueAtPercent(10))" :y="scaleY(10) - 10" class="marker-label">P10</text>
           <text :x="scaleX(getValueAtPercent(50))" :y="scaleY(50) - 10" class="marker-label">Median</text>
           <text :x="scaleX(getValueAtPercent(90))" :y="scaleY(90) - 10" class="marker-label">P90</text>
        </g>

        <!-- Y-axis labels (Probability) -->
        <g class="y-axis">
          <text 
            v-for="p in [0, 25, 50, 75, 100]" 
            :key="'y'+p"
            :x="padding.left - 8"
            :y="scaleY(p) + 4"
            class="axis-label"
          >{{ p }}%</text>
          
          <!-- Y Axis Title -->
          <text
            :x="padding.left - 45"
            :y="height / 2"
            transform="rotate(-90, 20, 150)"
            class="axis-title"
            style="transform-box: fill-box; transform-origin: center;"
          >Cumulative Probability</text>
        </g>

        <!-- X-axis labels (Value) -->
        <g class="x-axis">
          <text 
            v-for="(tick, i) in xTicks" 
            :key="'x'+i"
            :x="scaleX(tick)"
            :y="height - padding.bottom + 16"
            class="axis-label"
          >{{ formatAxisValue(tick) }}</text>
          <text 
            :x="width / 2"
            :y="height - 5"
            class="axis-title"
          >{{ isNetMode ? 'Net Profit ($)' : 'Intrinsic Value ($)' }}</text>
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
            :cy="scaleY(tooltip.percent)"
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

// Min/Max for scaling - Focus on DATA range, not benchmark
const minValue = computed(() => {
  if (!props.probabilityCurve.length) {
    return 0;
  }
  
  const values = props.probabilityCurve.map(p => p.value);
  const dataMin = Math.min(...values);
  const dataMax = Math.max(...values);
  const dataRange = dataMax - dataMin;
  
  // Handle edge case where all values are the same (no variance)
  if (dataRange === 0) {
    return dataMin - Math.abs(dataMin) * 0.1 - 1000; // Add some padding
  }
  
  // Add 10% padding below the data minimum
  let min = dataMin - (dataRange * 0.1);
  
  // In Net mode, ensure 0 is visible if data crosses it
  if (props.isNetMode && dataMax > 0 && dataMin < 0) {
    min = Math.min(min, dataMin * 1.1);
  } else if (props.isNetMode) {
    min = Math.min(min, 0);
  }
  
  return min;
});

const maxValue = computed(() => {
  if (!props.probabilityCurve.length) return 100000;
  
  const values = props.probabilityCurve.map(p => p.value);
  const dataMin = Math.min(...values);
  const dataMax = Math.max(...values);
  const dataRange = dataMax - dataMin;
  
  // Handle edge case where all values are the same (no variance)
  if (dataRange === 0) {
    return dataMax + Math.abs(dataMax) * 0.1 + 1000; // Add some padding
  }
  
  // Add 10% padding above the data maximum
  let max = dataMax + (dataRange * 0.1);
  
  // In Net mode, ensure 0 is visible
  if (props.isNetMode) {
    max = Math.max(max, 0);
  }
  
  return max;
});

// Check if benchmark (Cost line) is visible within chart bounds
const isBenchmarkVisible = computed(() => {
  if (props.isNetMode) return false; // No benchmark in Net mode
  return props.totalAcquisitionCost >= minValue.value && props.totalAcquisitionCost <= maxValue.value;
});

const xTicks = computed(() => {
  const range = maxValue.value - minValue.value;
  const step = range / 4;
  return [
    minValue.value,
    minValue.value + step,
    minValue.value + step * 2,
    minValue.value + step * 3,
    maxValue.value
  ];
});

// Scales
function scaleX(value) {
  const range = maxValue.value - minValue.value || 1;
  return padding.left + ((value - minValue.value) / range) * chartWidth;
}

function scaleY(percent) {
  // Invert Y so 100% is top
  return padding.top + (1 - (percent / 100)) * chartHeight;
}

// Helpers
function getValueAtPercent(p) {
    const pt = props.probabilityCurve.find(item => item.percent === p);
    return pt && isFinite(pt.value) ? pt.value : 0;
}

// Paths
const curvePath = computed(() => {
  if (!props.probabilityCurve.length) return '';
  
  // Filter for valid points only
  const validPoints = props.probabilityCurve.filter(pt => isFinite(pt.value));
  if (!validPoints.length) return '';

  const pathData = validPoints.map((pt, i) => {
    const cmd = i === 0 ? 'M' : 'L';
    const x = scaleX(pt.value);
    const y = scaleY(pt.percent);
    
    // Safety check for NaN coords
    if (!isFinite(x) || !isFinite(y)) return '';
    return `${cmd} ${x.toFixed(2)} ${y.toFixed(2)}`;
  }).filter(s => s).join(' '); // Filter out empty strings from bad coords
  
  return pathData;
});

const areaPath = computed(() => {
    if (!props.probabilityCurve.length) return '';
    
    // Filter for valid points only
    const validPoints = props.probabilityCurve.filter(pt => isFinite(pt.value));
    if (!validPoints.length) return '';
    
    const first = validPoints[0];
    const last = validPoints[validPoints.length - 1];
    
    let path = `M ${scaleX(first.value)} ${height - padding.bottom} `; // Start bottom-left
    path += curvePath.value.substring(1); // Curve
    path += ` L ${scaleX(last.value)} ${height - padding.bottom} Z`; // Down to bottom-right
    return path;
});

// Interaction
function handleMouseMove(event) {
    const rect = chartContainer.value.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const chartX = x - padding.left;
    
    // Convert X position to Value
    const range = maxValue.value - minValue.value || 1;
    const ratio = Math.max(0, Math.min(1, chartX / chartWidth));
    const valueAtCursor = minValue.value + (ratio * range);
    
    // Find closest point by Value
    const closest = props.probabilityCurve.reduce((prev, curr) => {
        return (Math.abs(curr.value - valueAtCursor) < Math.abs(prev.value - valueAtCursor) ? curr : prev);
    });
    
    // Snap tooltip to the closest actual data point coordinates
    const xLine = scaleX(closest.value);
    
    tooltip.value = {
        visible: true,
        x: xLine + 10,
        y: scaleY(closest.percent) - 40,
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


