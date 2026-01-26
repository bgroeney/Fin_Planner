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

<style scoped>
.status-history {
  padding: var(--spacing-md);
  max-height: 400px;
  overflow-y: auto;
}

.loading-state, .empty-history {
  text-align: center;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  padding: var(--spacing-lg);
}

.history-timeline {
  list-style: none;
  margin: 0;
  padding: 0;
}

.history-item {
  display: flex;
  gap: var(--spacing-md);
  padding-bottom: var(--spacing-lg);
  position: relative;
}

.history-item:last-child {
  padding-bottom: 0;
}

.timeline-line {
  position: absolute;
  left: 5px; /* Center of marker (10px width) */
  top: 10px;
  bottom: 0;
  width: 2px;
  background: var(--color-border);
  z-index: 1;
}

.history-item:last-child .timeline-line {
  display: none;
}

.timeline-marker {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 2px solid white;
  z-index: 2;
  flex-shrink: 0;
  margin-top: 4px; /* Align with header text */
  box-shadow: 0 0 0 1px var(--color-border);
}

.history-content {
  flex: 1;
}

.history-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
}

.status-badge {
    font-size: 10px;
    font-weight: 700;
    text-transform: uppercase;
    padding: 2px 6px;
    border-radius: 4px;
}

.history-date {
  font-size: 10px;
  color: var(--color-text-muted);
}

.history-user {
  font-size: 11px;
  color: var(--color-text-secondary);
  font-style: italic;
  margin-bottom: 4px;
}

.history-comment {
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  background: var(--color-bg-elevated);
  padding: 8px;
  border-radius: 6px;
  border: 1px solid var(--color-border-subtle);
  margin-top: 4px;
  font-style: italic;
}

/* Status Colors (Markers & Badges) */
.status-draft { background: var(--color-text-muted); color: white; }
.timeline-marker.status-draft { background: var(--color-text-muted); }
.status-badge.status-draft { background: var(--color-bg-elevated); color: var(--color-text-muted); }

.status-analyzing { background: var(--color-info); color: white; }
.timeline-marker.status-analyzing { background: var(--color-info); }
.status-badge.status-analyzing { background: rgba(59, 130, 246, 0.1); color: var(--color-info); }

.status-buy { background: var(--color-success); color: white; }
.timeline-marker.status-buy { background: var(--color-success); }
.status-badge.status-buy { background: rgba(16, 185, 129, 0.1); color: var(--color-success); }

.status-pass { background: var(--color-text-secondary); color: white; }
.timeline-marker.status-pass { background: var(--color-text-secondary); }
.status-badge.status-pass { background: var(--color-bg-elevated); color: var(--color-text-secondary); }

.status-uneconomic { background: var(--color-danger); color: white; }
.timeline-marker.status-uneconomic { background: var(--color-danger); }
.status-badge.status-uneconomic { background: rgba(239, 68, 68, 0.1); color: var(--color-danger); }
</style>
