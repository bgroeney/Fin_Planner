<template>
  <div class="money-flow card p-6">
    <h3 class="font-bold mb-6 flex items-center gap-2">
      <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>
      Annual Cashflow (Sankey)
    </h3>
    
    <div class="sankey-container">
      <svg width="100%" height="300" viewBox="0 0 600 300">
        <!-- Income Nodes -->
        <g class="nodes">
          <rect x="50" y="50" width="100" height="40" rx="4" fill="var(--color-success)" opacity="0.8" />
          <text x="100" y="75" text-anchor="middle" fill="white" font-size="12" font-weight="bold">Salary</text>
          
          <rect x="50" y="110" width="100" height="40" rx="4" fill="var(--color-success)" opacity="0.6" />
          <text x="100" y="135" text-anchor="middle" fill="white" font-size="12" font-weight="bold">Dividends</text>

          <rect x="50" y="170" width="100" height="40" rx="4" fill="var(--color-success)" opacity="0.4" />
          <text x="100" y="195" text-anchor="middle" fill="white" font-size="12" font-weight="bold">Rental</text>
        </g>

        <!-- Center Node -->
        <rect x="250" y="100" width="100" height="100" rx="8" fill="var(--color-accent)" opacity="0.2" stroke="var(--color-accent)" stroke-width="2" />
        <text x="300" y="150" text-anchor="middle" fill="var(--color-text-primary)" font-size="14" font-weight="bold">Total Income</text>

        <!-- Destination Nodes -->
        <g class="nodes">
          <rect x="450" y="50" width="100" height="40" rx="4" fill="var(--color-danger)" opacity="0.8" />
          <text x="500" y="75" text-anchor="middle" fill="white" font-size="12" font-weight="bold">Expenses</text>
          
          <rect x="450" y="110" width="100" height="40" rx="4" fill="var(--color-danger)" opacity="0.6" />
          <text x="500" y="135" text-anchor="middle" fill="white" font-size="12" font-weight="bold">Tax</text>

          <rect x="450" y="170" width="100" height="40" rx="4" fill="var(--color-accent)" opacity="0.8" />
          <text x="500" y="195" text-anchor="middle" fill="white" font-size="12" font-weight="bold">Invested</text>
        </g>

        <!-- Paths (Static for now, but stylized) -->
        <path d="M 150 70 C 200 70, 200 120, 250 120" stroke="var(--color-success)" stroke-width="15" fill="none" opacity="0.2" />
        <path d="M 150 130 C 200 130, 200 150, 250 150" stroke="var(--color-success)" stroke-width="10" fill="none" opacity="0.2" />
        <path d="M 150 190 C 200 190, 200 180, 250 180" stroke="var(--color-success)" stroke-width="8" fill="none" opacity="0.2" />

        <path d="M 350 120 C 400 120, 400 70, 450 70" stroke="var(--color-danger)" stroke-width="15" fill="none" opacity="0.2" />
        <path d="M 350 150 C 400 150, 400 130, 450 130" stroke="var(--color-danger)" stroke-width="12" fill="none" opacity="0.2" />
        <path d="M 350 180 C 400 180, 400 190, 450 190" stroke="var(--color-accent)" stroke-width="10" fill="none" opacity="0.2" />
      </svg>
    </div>
    
    <div class="mt-6 p-4 bg-muted rounded-lg text-xs text-muted flex items-center gap-2">
      <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>
      This diagram visualizes how your total annual income is distributed between consumption, taxation, and wealth creation.
    </div>
  </div>
</template>

<style scoped>
.sankey-container {
  padding: 20px 0;
}
.nodes rect {
  transition: opacity 0.3s;
  cursor: pointer;
}
.nodes rect:hover {
  opacity: 1;
}
path {
  transition: opacity 0.3s;
}
path:hover {
  opacity: 0.5;
}
</style>
