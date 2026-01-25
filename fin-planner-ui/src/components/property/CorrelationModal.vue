<template>
  <Teleport to="body">
    <div class="modal-backdrop" @click.self="$emit('close')">
      <div class="correlation-modal">
         <div class="modal-header">
           <h3>Correlation Matrix</h3>
           <button class="btn-close" @click="$emit('close')">×</button>
         </div>
         <div class="modal-body">
           <p class="description">
              Define relationships between variables (-1.0 to 1.0). 
              <br>Positive = Variables move together. Negative = Inverse relationship.
           </p>
           
           <div class="matrix-grid" v-if="localMatrix.length === variables.length">
              <!-- Headers -->
              <div class="grid-header-cell"></div>
              <div class="grid-header-cell" v-for="v in variables" :key="v.key">{{ v.label }}</div>
              
              <!-- Rows -->
              <template v-for="(rowVar, rIdx) in variables" :key="rIdx">
                 <div class="grid-row-header">{{ rowVar.label }}</div>
                 <div v-for="(colVar, cIdx) in variables" :key="`${rIdx}-${cIdx}`" class="grid-cell">
                    <input 
                      v-if="rIdx !== cIdx"
                      type="number" 
                      step="0.1" 
                      min="-1" 
                      max="1" 
                      :value="localMatrix[rIdx][cIdx]"
                      @input="updateCell(rIdx, cIdx, $event.target.value)"
                      class="correlation-input"
                      :class="{ 'positive': localMatrix[rIdx][cIdx] > 0, 'negative': localMatrix[rIdx][cIdx] < 0 }"
                    />
                    <span v-else class="diagonal">1.0</span>
                 </div>
              </template>
           </div>
         </div>
         <div class="modal-actions">
            <button class="btn btn-primary" @click="save">Apply Correlations</button>
         </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, onMounted } from 'vue';

const props = defineProps({
    matrix: Array
});

const emit = defineEmits(['close', 'save']);

const variables = [
    { key: 'rent', label: 'Rent' },
    { key: 'vacancy', label: 'Vacancy' },
    { key: 'interest', label: 'Interest' },
    { key: 'growth', label: 'Growth' }
];

const localMatrix = ref([]);

onMounted(() => {
    // Deep copy matrix
    localMatrix.value = props.matrix.map(row => [...row]);
});

function updateCell(r, c, val) {
    let num = parseFloat(val);
    if (isNaN(num)) num = 0;
    if (num > 1) num = 1;
    if (num < -1) num = -1;
    
    // Symmetric update
    localMatrix.value[r][c] = num;
    localMatrix.value[c][r] = num;
}

function save() {
    emit('save', localMatrix.value);
}
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  backdrop-filter: blur(4px);
}

.correlation-modal {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  width: 500px;
  max-width: 90%;
  box-shadow: var(--shadow-lg);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
}

.modal-header h3 {
  margin: 0;
  font-size: var(--font-size-lg);
}

.modal-body {
  padding: var(--spacing-lg);
}

.description {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-lg);
  text-align: center;
}

.matrix-grid {
  display: grid;
  grid-template-columns: 80px repeat(4, 1fr);
  gap: 8px;
  align-items: center;
}

.grid-header-cell, .grid-row-header {
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  color: var(--color-text-muted);
  text-align: center;
}

.grid-row-header {
  text-align: right;
  padding-right: 8px;
}

.grid-cell {
  display: flex;
  justify-content: center;
}

.correlation-input {
  width: 100%;
  text-align: center;
  padding: 8px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  font-family: var(--font-mono);
}

.correlation-input.positive {
  background: rgba(16, 185, 129, 0.1);
  color: var(--color-success);
  border-color: rgba(16, 185, 129, 0.3);
}

.correlation-input.negative {
  background: rgba(239, 68, 68, 0.1);
  color: var(--color-danger);
  border-color: rgba(239, 68, 68, 0.3);
}

.diagonal {
  color: var(--color-text-muted);
  font-family: var(--font-mono);
  font-size: var(--font-size-sm);
}

.modal-actions {
  padding: var(--spacing-lg);
  border-top: 1px solid var(--color-border);
  display: flex;
  justify-content: flex-end;
}

.btn-close {
  background: none;
  border: none;
  font-size: 20px;
  cursor: pointer;
  color: var(--color-text-muted);
}
</style>
