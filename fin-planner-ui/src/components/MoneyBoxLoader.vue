<template>
  <div class="money-box-loader" :class="size">
    <div class="loader-container">
      <!-- Gold Bars Animation -->
      <div class="gold-bar bar-1"></div>
      <div class="gold-bar bar-2"></div>
      <div class="gold-bar bar-3"></div>
      
      <!-- Money Box SVG -->
      <svg viewBox="0 0 100 100" class="chest-svg">
        <!-- Back of chest -->
        <path d="M20 40 L80 40 L90 50 L90 90 L10 90 L10 50 Z" fill="#2c3e50" stroke="#34495e" stroke-width="2"/>
        <!-- Front of chest (lower) -->
        <path d="M10 50 L90 50 L90 90 L10 90 Z" fill="#34495e" stroke="#2c3e50" stroke-width="2"/>
        <!-- Lid (Open) -->
        <path d="M10 50 L20 20 L80 20 L90 50" fill="none" stroke="#2c3e50" stroke-width="3" stroke-linecap="round"/>
        <!-- Lock -->
        <circle cx="50" cy="60" r="4" fill="#f1c40f"/>
        <path d="M50 60 L50 68" stroke="#f1c40f" stroke-width="2"/>
      </svg>
    </div>
    <div v-if="text" class="loader-text">{{ text }}</div>
  </div>
</template>

<script setup>
defineProps({
  text: {
    type: String,
    default: 'Processing...'
  },
  size: {
    type: String, // 'sm', 'md', 'lg'
    default: 'md'
  }
});
</script>


