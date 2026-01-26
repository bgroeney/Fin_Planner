<template>
  <div class="distribution-card">
    <div class="card-header">
       <span class="label">{{ label }}</span>
       <span class="base-value">{{ formatBase(baseValue) }}</span>
    </div>

    <!-- Distribution Selector -->
    <div class="dist-selector" v-if="!readOnly">
       <select v-model="localConfig.type" @change="emitUpdate">
          <option value="normal">Normal</option>
          <option value="lognormal" disabled title="Coming soon">Lognormal</option>
          <option value="triangular">Triangular</option>
          <option value="uniform">Flat (Uniform)</option>
       </select>
    </div>
    <div class="dist-type-read" v-else>
       {{ localConfig.type }}
    </div>

    <!-- Params -->
    <div class="params-area" v-if="!readOnly">
       <!-- Normal Params -->
       <div v-if="localConfig.type === 'normal'" class="param-row">
          <label>Variance (±%)</label>
          <div class="input-wrapper">
             <input type="number" v-model.number="localConfig.variancePercent" @change="emitUpdate" />
             <span class="suffix">%</span>
          </div>
       </div>

       <!-- Triangular Params -->
       <div v-if="localConfig.type === 'triangular'" class="param-grid">
          <div class="param-col">
             <label>Min</label>
             <input type="number" v-model.number="localConfig.min" @change="emitUpdate" class="small-input"/>
          </div>
          <div class="param-col">
             <label>Max</label>
             <input type="number" v-model.number="localConfig.max" @change="emitUpdate" class="small-input"/>
          </div>
          <div class="param-col">
             <label>Mode</label>
             <input type="number" v-model.number="localConfig.mode" @change="emitUpdate" class="small-input"/>
          </div>
       </div>

       <!-- Uniform Params -->
       <div v-if="localConfig.type === 'uniform'" class="param-grid">
          <div class="param-col">
             <label>Min</label>
             <input type="number" v-model.number="localConfig.min" @change="emitUpdate" class="small-input"/>
          </div>
          <div class="param-col">
             <label>Max</label>
             <input type="number" v-model.number="localConfig.max" @change="emitUpdate" class="small-input"/>
          </div>
       </div>
    </div>
    <div class="params-read" v-else>
        <div v-if="localConfig.type === 'normal'">±{{ localConfig.variancePercent }}%</div>
        <div v-else-if="localConfig.type === 'uniform'">Range: {{ localConfig.min }} - {{ localConfig.max }}</div>
    </div>

    <!-- Visual Preview (Mini SVG) -->
    <div class="dist-preview">
       <svg width="60" height="20" viewBox="0 0 60 20" class="dist-icon">
          <!-- Normal -->
          <path v-if="localConfig.type === 'normal'" d="M2,18 C15,18 20,2 30,2 C40,2 45,18 58,18" fill="none" class="line-path" />
          <!-- Triangular -->
          <path v-if="localConfig.type === 'triangular'" d="M2,18 L30,2 L58,18" fill="none" class="line-path" />
          <!-- Uniform -->
          <path v-if="localConfig.type === 'uniform'" d="M5,18 L5,5 L55,5 L55,18" fill="none" class="line-path" />
       </svg>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue';

const props = defineProps({
    label: String,
    baseValue: Number,
    isCurrency: Boolean,
    suffix: String,
    config: Object,
    readOnly: Boolean
});

const emit = defineEmits(['update']);

const localConfig = ref({ ...props.config });

watch(() => props.config, (newVal) => {
    localConfig.value = { ...newVal };
}, { deep: true });

// Auto-init params if switching types
watch(() => localConfig.value.type, (newType, oldType) => {
    if (newType !== oldType) {
        if (newType === 'triangular' && (localConfig.value.min === undefined)) {
             // Init defaults based on baseValue
             localConfig.value.min = props.baseValue * 0.9;
             localConfig.value.max = props.baseValue * 1.1;
             localConfig.value.mode = props.baseValue;
        } else if (newType === 'uniform' && (localConfig.value.min === undefined)) {
             localConfig.value.min = props.baseValue * 0.9;
             localConfig.value.max = props.baseValue * 1.1;
        }
        emitUpdate();
    }
});

function emitUpdate() {
    emit('update', localConfig.value);
}

function formatBase(val) {
    if (props.isCurrency) {
        return new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD', maximumFractionDigits: 0 }).format(val);
    }
    if (props.suffix) return `${val}${props.suffix}`;
    return val;
}
</script>


