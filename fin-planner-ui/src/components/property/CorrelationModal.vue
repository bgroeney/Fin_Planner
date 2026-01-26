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


