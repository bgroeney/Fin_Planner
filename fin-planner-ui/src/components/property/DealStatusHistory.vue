<template>
  <div class="status-history">
    <div v-if="loading" class="loading-state">
      <span class="spinner-sm"></span> Loading history...
    </div>
    <div v-else-if="history.length === 0" class="empty-history">
      <p>No status history recorded.</p>
    </div>
    <ul v-else class="history-timeline">
      <li v-for="(entry, index) in history" :key="index" class="history-item">
        <div class="timeline-line"></div>
        <div class="timeline-marker" :class="getStatusClass(entry.status)"></div>
        <div class="history-content">
          <div class="history-header">
            <span class="status-badge" :class="getStatusClass(entry.status)">{{ entry.status }}</span>
            <span class="history-date">{{ formatDate(entry.timestamp) }}</span>
          </div>
          <div class="history-user" v-if="entry.userName">
             by {{ entry.userName }}
          </div>
          <div class="history-comment" v-if="entry.comment">
            "{{ entry.comment }}"
          </div>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  history: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false }
});

function getStatusClass(status) {
  switch (status) {
    case 'Buy': return 'status-buy';
    case 'Pass': return 'status-pass';
    case 'Uneconomic': return 'status-uneconomic';
    case 'Analyzing': return 'status-analyzing';
    case 'Acquired': return 'status-buy'; // Reuse buy color
    default: return 'status-draft';
  }
}

function formatDate(isoString) {
  if (!isoString) return '';
  return new Date(isoString).toLocaleString('en-AU', {
    day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit'
  });
}
</script>


