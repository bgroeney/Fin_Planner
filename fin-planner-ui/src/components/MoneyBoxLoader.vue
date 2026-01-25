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

<style scoped>
.money-box-loader {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.loader-container {
  position: relative;
  width: 80px;
  height: 80px;
}

.chest-svg {
  position: absolute;
  bottom: 0;
  width: 100%;
  height: 100%;
  z-index: 2;
}

.gold-bar {
  position: absolute;
  left: 50%;
  width: 20px;
  height: 10px;
  background: linear-gradient(45deg, #f1c40f, #f39c12);
  border: 1px solid #d35400;
  border-radius: 2px;
  transform: translateX(-50%);
  opacity: 0;
  z-index: 1; /* Behind front of chest but should look like going inside */
}

/* Size Variants */
.sm .loader-container { width: 40px; height: 40px; }
.sm .gold-bar { width: 10px; height: 5px; }
.lg .loader-container { width: 120px; height: 120px; }
.lg .gold-bar { width: 30px; height: 15px; }

/* Text Styles */
.loader-text {
  color: var(--color-text-muted);
  font-size: 0.9rem;
  font-weight: 500;
  animation: pulse 2s infinite;
}

/* Animations */
.bar-1 { animation: dropIn 2s infinite ease-in-out; animation-delay: 0s; }
.bar-2 { animation: dropIn 2s infinite ease-in-out; animation-delay: 0.6s; }
.bar-3 { animation: dropIn 2s infinite ease-in-out; animation-delay: 1.2s; }

@keyframes dropIn {
  0% {
    top: 0;
    opacity: 0;
    transform: translateX(-50%) rotate(0deg);
  }
  20% {
    opacity: 1;
  }
  50% {
    top: 55%; /* Into the box */
    opacity: 1;
    transform: translateX(-50%) rotate(10deg);
  }
  60% {
    opacity: 0; /* Disappear into the abyss of the box */
  }
  100% {
    top: 60%;
    opacity: 0;
  }
}

@keyframes pulse {
  0%, 100% { opacity: 0.6; }
  50% { opacity: 1; }
}
</style>
