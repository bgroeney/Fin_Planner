<template>
  <div class="cashflow-spreadsheet">
    <!-- Toolbar -->
    <div class="spreadsheet-toolbar">
      <div class="toolbar-left">
        <h3 class="spreadsheet-title">Cashflow Model</h3>
        <div class="granularity-toggle">
          <button 
            :class="['toggle-btn', { active: granularity === 'years' }]"
            @click="granularity = 'years'"
          >Years</button>
          <button 
            :class="['toggle-btn', { active: granularity === 'months' }]"
            @click="granularity = 'months'"
          >Months</button>
        </div>
      </div>
      <div class="toolbar-right">
        <button class="btn btn-secondary btn-sm" @click="resetAllOverrides" title="Clear all manual locks">
          Unlock All
        </button>
        <!-- Button removed per user request - persistence is now automatic via overrides -->
      </div>
    </div>

    <!-- Spreadsheet Grid -->
    <div class="spreadsheet-container" ref="spreadsheetContainer" @contextmenu.prevent>
      <table class="spreadsheet-table">
        <thead>
          <tr class="header-row">
            <th class="sticky-col row-label-header">Parameter</th>
            <th 
              v-for="col in columns" 
              :key="col.key"
              class="period-header"
              :class="{ 
                'year-start': col.isYearStart,
                'year-summary': col.isYearSummary
              }"
            >
              {{ col.label }}
            </th>
          </tr>
        </thead>
        <tbody>
          <template v-for="row in flattenedRows" :key="row.id">
            <!-- Category Header -->
            <tr v-if="row.isCategory" class="category-row">
              <td :colspan="columns.length + 1" class="category-cell">
                <button class="expand-btn" @click="toggleCategory(row.id)">
                  <svg :class="{ rotated: !expandedCategories.includes(row.id) }" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="6 9 12 15 18 9"></polyline>
                  </svg>
                </button>
                {{ row.label }}
              </td>
            </tr>
            
            <!-- Expandable Parent Row -->
            <tr 
              v-else-if="row.isExpandable && isRowVisible(row)"
              class="data-row expandable-row"
              :class="{ 
                'has-overrides': hasChildOverrides(row),
                'expanded': expandedRows.includes(row.id)
              }"
            >
              <td class="sticky-col row-label">
                <button class="expand-btn" @click="toggleRow(row.id)">
                  <svg :class="{ rotated: !expandedRows.includes(row.id) }" width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="6 9 12 15 18 9"></polyline>
                  </svg>
                </button>
                {{ row.label }}
                <span v-if="hasChildOverrides(row)" class="locked-badge" @click="unlockParent(row)" title="Child values overridden. Click to unlock and reset.">
                  <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                    <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                  </svg>
                </span>
              </td>
              <td 
                v-for="(cell, colIndex) in getRowCells(row)" 
                :key="colIndex"
                class="value-cell parent-cell calculation-cell"
                :class="{ 
                  'locked': hasChildOverridesForPeriod(row, colIndex),
                  'year-summary': columns[colIndex]?.isYearSummary,
                  'negative': cell.value < 0
                }"
              >
                <span class="cell-value">{{ formatCurrency(cell.value) }}</span>
                <span v-if="hasChildOverridesForPeriod(row, colIndex)" class="lock-indicator" title="Monthly values overridden">
                  <svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor">
                    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                    <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                  </svg>
                </span>
              </td>
            </tr>

            <!-- Child Row (sub-item) -->
            <tr 
              v-else-if="row.isChild && expandedRows.includes(row.parentId)"
              class="data-row child-row"
              :class="{ 
                'has-override': hasRowOverrides(row)
              }"
            >
              <td class="sticky-col row-label child-label">
                <span class="child-indent"></span>
                {{ row.label }}
                <button v-if="row.isDeletable" class="delete-row-btn" @click="deleteCustomRow(row.id)" title="Remove">×</button>
              </td>
              <td 
                v-for="(cell, colIndex) in getRowCells(row)" 
                :key="colIndex"
                class="value-cell"
                :class="{ 
                  'editable': !columns[colIndex]?.isYearSummary,
                  'calculation-cell': columns[colIndex]?.isYearSummary,
                  'year-summary': columns[colIndex]?.isYearSummary,
                  'negative': cell.value < 0,
                  'has-override': cell.isOverride
                }"
                @click="!columns[colIndex]?.isYearSummary && startEditing(row, colIndex)"
                @contextmenu.prevent="!columns[colIndex]?.isYearSummary && showContextMenu($event, row, colIndex)"
              >
                <template v-if="editingCell?.rowId === row.id && editingCell?.index === colIndex">
                  <input 
                    ref="editInput"
                    type="number"
                    :value="Math.abs(cell.value)"
                    @blur="finishEditing($event, row, colIndex)"
                    @keydown.enter="finishEditing($event, row, colIndex)"
                    @keydown="handleKeyDown($event, row, colIndex)"
                    class="cell-input"
                  />
                </template>
                <template v-else>
                  <span class="cell-value">{{ formatCurrency(cell.value) }}</span>
                  <span v-if="cell.isOverride" class="override-indicator" title="Manual override">●</span>
                </template>
              </td>
            </tr>

            <!-- Add Custom Row Button -->
            <tr 
              v-else-if="row.isAddButton && expandedRows.includes(row.parentId)"
              class="add-row-row"
            >
              <td :colspan="columns.length + 1" class="add-row-cell">
                <button class="add-row-btn" @click="openAddLineItemModal(row.parentId)">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="5" x2="12" y2="19"></line>
                    <line x1="5" y1="12" x2="19" y2="12"></line>
                  </svg>
                  Add Line Item
                </button>
              </td>
            </tr>

            <!-- Regular Data Row -->
            <tr 
              v-else-if="!row.isChild && !row.isAddButton && isRowVisible(row)"
              class="data-row"
              :class="{ 
                'derived-row': !row.isEditable,
                'summary-row': row.category === 'summary'
              }"
            >
              <td class="sticky-col row-label">
                <span class="row-indent" v-if="row.indent">{{ '  '.repeat(row.indent) }}</span>
                {{ row.label }}
              </td>
              <td 
                v-for="(cell, colIndex) in getRowCells(row)" 
                :key="colIndex"
                class="value-cell"
                :class="{ 
                  'editable': row.isEditable && !columns[colIndex]?.isYearSummary,
                  'calculation-cell': !row.isEditable || columns[colIndex]?.isYearSummary,
                  'year-summary': columns[colIndex]?.isYearSummary,
                  'negative': cell.value < 0,
                  'has-override': cell.isOverride
                }"
                @click="row.isEditable && !columns[colIndex]?.isYearSummary && startEditing(row, colIndex)"
                @contextmenu.prevent="row.isEditable && !columns[colIndex]?.isYearSummary && showContextMenu($event, row, colIndex)"
              >
                <template v-if="editingCell?.rowId === row.id && editingCell?.index === colIndex">
                  <input 
                    ref="editInput"
                    type="number"
                    :value="Math.abs(cell.value)"
                    @blur="finishEditing($event, row, colIndex)"
                    @keydown.enter="finishEditing($event, row, colIndex)"
                    @keydown="handleKeyDown($event, row, colIndex)"
                    class="cell-input"
                  />
                </template>
                <template v-else>
                  <span class="cell-value">{{ formatCurrency(cell.value) }}</span>
                  <span v-if="cell.isOverride" class="override-indicator" title="Manual override">●</span>
                </template>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <!-- Context Menu -->
    <div 
      v-if="contextMenu.visible" 
      class="context-menu"
      :style="{ top: contextMenu.y + 'px', left: contextMenu.x + 'px' }"
      @click.stop
    >
      <button class="context-menu-item" @click="resetCellToCalculated">
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <polyline points="1 4 1 10 7 10"></polyline>
          <path d="M3.51 15a9 9 0 1 0 2.13-9.36L1 10"></path>
        </svg>
        Reset to Calculated
      </button>
      <button class="context-menu-item" @click="copyValue('left')">
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
           <polyline points="15 18 9 12 15 6"></polyline>
        </svg>
        Copy Left
      </button>
      <button class="context-menu-item" @click="copyValue('right')">
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
           <polyline points="9 18 15 12 9 6"></polyline>
        </svg>
        Copy Right
      </button>
      <button class="context-menu-item" @click="openVarianceForCell">
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M12 20V10"></path>
          <path d="M18 20V4"></path>
          <path d="M6 20v-4"></path>
        </svg>
        Set Variance (±%)
      </button>
    </div>
    <div v-if="contextMenu.visible" class="context-menu-backdrop" @click="closeContextMenu"></div>

    <!-- Add Line Item Modal -->
    <div v-if="showAddModal" class="modal-backdrop" @click.self="showAddModal = false">
      <div class="add-line-modal card">
        <div class="modal-header">
          <h4>Add Custom Line Item</h4>
          <button class="btn-close" @click="showAddModal = false">×</button>
        </div>
        <form @submit.prevent="addCustomLineItem" class="modal-body">
          <div class="form-group">
            <label>Label *</label>
            <input v-model="newLineItem.label" type="text" required placeholder="e.g., Security System" />
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Annual Amount *</label>
              <input v-model.number="newLineItem.amount" type="number" min="0" required placeholder="5000" />
            </div>
            <div class="form-group">
              <label>Growth Rate (%)</label>
              <input v-model.number="newLineItem.growthRate" type="number" step="0.5" placeholder="3.0" />
            </div>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" @click="showAddModal = false">Cancel</button>
            <button type="submit" class="btn btn-primary">Add Line Item</button>
          </div>
        </form>
      </div>
    </div>

    <!-- Variance Modal -->
    <div v-if="showVarianceModal" class="modal-backdrop" @click.self="showVarianceModal = false">
      <div class="variance-modal card">
        <div class="modal-header">
          <h4>Set Uncertainty Range</h4>
          <button class="btn-close" @click="showVarianceModal = false">×</button>
        </div>
        <div class="modal-body">
          <p class="variance-context">{{ varianceTarget.label }} - {{ varianceTarget.context }}</p>
          
          <!-- Value Variance -->
          <div class="variance-input-group">
            <label>
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 20V10"></path>
                <path d="M18 20V4"></path>
                <path d="M6 20v-4"></path>
              </svg>
              Value Variance (±%)
            </label>
            <input 
              type="number" 
              v-model.number="varianceValue" 
              min="0" 
              max="100" 
              step="1"
              class="variance-input"
            />
            <input 
              type="range" 
              v-model.number="varianceValue" 
              min="0" 
              max="50" 
              step="1"
              class="variance-slider"
            />
          </div>

          <!-- Time Variance (P10 Early / P90 Late) -->
          <div class="variance-input-group time-variance">
            <label>
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"></circle>
                <polyline points="12 6 12 12 16 14"></polyline>
              </svg>
              Timing Variance (months)
            </label>
            <div class="time-variance-row">
              <div class="time-input">
                <span class="time-label p10-label">P10 (Early)</span>
                <input 
                  type="number" 
                  v-model.number="timeVarianceEarly" 
                  min="0" 
                  max="24"
                  class="variance-input small"
                />
                <span class="time-unit">mo</span>
              </div>
              <span class="time-divider">↔</span>
              <div class="time-input">
                <span class="time-label p90-label">P90 (Late)</span>
                <input 
                  type="number" 
                  v-model.number="timeVarianceLate" 
                  min="0" 
                  max="24"
                  class="variance-input small"
                />
                <span class="time-unit">mo</span>
              </div>
            </div>
            <div class="time-variance-hint">
              Cashflow timing: Receive {{ timeVarianceEarly }}mo early (P10) to {{ timeVarianceLate }}mo late (P90)
            </div>
          </div>
        </div>
        <div class="modal-actions">
          <button class="btn btn-secondary" @click="showVarianceModal = false">Cancel</button>
          <button class="btn btn-primary" @click="applyVariance">Apply</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, nextTick, reactive } from 'vue';

const props = defineProps({
  deal: { type: Object, required: true },
  holdingPeriod: { type: Number, default: 10 },
  initialOverridesJson: { type: String, default: null } // New prop for loading persisted overrides
});

const emit = defineEmits(['apply-changes', 'update:overrides']);

// State
const granularity = ref('years');
const expandedCategories = ref(['income', 'expenses', 'financing', 'summary']);
const expandedRows = ref(['outgoings']); // Start with outgoings expanded
const editingCell = ref(null);
const editInput = ref(null);

// Custom line items
const customLineItems = ref([]);
const showAddModal = ref(false);
const addModalParentId = ref('');
const newLineItem = ref({ label: '', amount: 0, growthRate: 3 });

// Cell overrides stored by "rowId-periodIndex" (always stored as monthly index)
const cellOverrides = reactive({});

// Context menu
const contextMenu = reactive({ visible: false, x: 0, y: 0, rowId: '', cellIndex: -1 });

// Variance modal
const showVarianceModal = ref(false);
const varianceTarget = ref({ label: '', context: '', rowId: '', cellIndex: -1 });
const varianceValue = ref(10);
const timeVarianceEarly = ref(0); // P10 - months early
const timeVarianceLate = ref(3);  // P90 - months late

// Helper to check if we're in monthly view
const isMonthly = computed(() => granularity.value === 'months');

// Columns (with year summaries after each year's months)
const columns = computed(() => {
  const result = [];
  if (granularity.value === 'years') {
    for (let y = 1; y <= props.holdingPeriod; y++) {
      result.push({ key: `Y${y}`, label: `Y${y}`, year: y, isYearStart: true, isYearSummary: false, colType: 'year' });
    }
  } else {
    // Monthly view: months first, then year summary at the end of each year
    for (let y = 1; y <= props.holdingPeriod; y++) {
      for (let m = 1; m <= 12; m++) {
        result.push({
          key: `Y${y}M${m}`,
          label: m === 1 ? `M1` : `M${m}`,
          year: y,
          month: m,
          isYearStart: m === 1,
          isYearSummary: false,
          colType: 'month'
        });
      }
      // Add year summary column AFTER the 12 months
      result.push({
        key: `Y${y}Total`,
        label: `Y${y}`,
        year: y,
        isYearStart: false,
        isYearSummary: true,
        colType: 'yearTotal'
      });
    }
  }
  return result;
});

// Calculate growth-adjusted value
const withGrowth = (base, yearIndex, growthRate = 0.025) => {
  return base * Math.pow(1 + growthRate, yearIndex);
};

// Get monthly index from column index (for storing/retrieving overrides)
const getMonthlyIndex = (colIndex) => {
  if (!isMonthly.value) {
    // In yearly view, map year index to first month of that year
    return colIndex * 12;
  }
  const col = columns.value[colIndex];
  if (col?.isYearSummary) {
    return -1; // Year summary columns don't have overrides
  }
  // In monthly view, account for year summary columns
  // Count how many year summary columns come before this index
  let monthCount = 0;
  for (let i = 0; i < colIndex; i++) {
    if (!columns.value[i].isYearSummary) {
      monthCount++;
    }
  }
  return monthCount;
};

// Get all monthly indices for a year (used for year summary calculation)
const getMonthlyIndicesForYear = (year) => {
  const startMonth = (year - 1) * 12;
  return Array.from({ length: 12 }, (_, i) => startMonth + i);
};

// Create cell values for a row
const createCellValues = (baseValue, rowId, options = {}) => {
  const { growth = 0, invert = false } = options;
  const cells = [];
  
  if (granularity.value === 'years') {
    // Yearly view: each column is a year, sum 12 months
    for (let y = 0; y < props.holdingPeriod; y++) {
      let yearTotal = 0;
      for (let m = 0; m < 12; m++) {
        const monthlyIndex = y * 12 + m;
        const overrideKey = `${rowId}-${monthlyIndex}`;
        if (cellOverrides[overrideKey]) {
          yearTotal += cellOverrides[overrideKey].value;
        } else {
          let value = withGrowth(baseValue / 12, y, growth);
          if (invert) value = -value;
          yearTotal += value;
        }
      }
      cells.push({ value: yearTotal, isOverride: false });
    }
  } else {
    // Monthly view: months + year summary columns
    for (let y = 1; y <= props.holdingPeriod; y++) {
      let yearTotal = 0;
      // Add 12 months
      for (let m = 1; m <= 12; m++) {
        const monthlyIndex = (y - 1) * 12 + (m - 1);
        const overrideKey = `${rowId}-${monthlyIndex}`;
        if (cellOverrides[overrideKey]) {
          const overrideValue = cellOverrides[overrideKey].value;
          cells.push({ value: overrideValue, isOverride: true });
          yearTotal += overrideValue;
        } else {
          let value = withGrowth(baseValue / 12, y - 1, growth);
          if (invert) value = -value;
          cells.push({ value, isOverride: false });
          yearTotal += value;
        }
      }
      // Add year summary (auto-summed from months)
      const hasAnyOverride = getMonthlyIndicesForYear(y).some(idx => cellOverrides[`${rowId}-${idx}`]);
      cells.push({ value: yearTotal, isOverride: false, isYearSummary: true, hasMonthlyOverrides: hasAnyOverride });
    }
  }
  
  return cells;
};

// Generate cashflow rows
const flattenedRows = computed(() => {
  const rows = [];
  const deal = props.deal;

  // Income Category
  rows.push({ id: 'income', label: 'Income', isCategory: true, category: 'income' });
  
  rows.push({
    id: 'grossRent',
    label: 'Gross Rent',
    category: 'income',
    parentCategory: 'income',
    isEditable: true,
    baseValue: deal.estimatedGrossRent,
    growth: 0.025
  });

  const vacancyAmount = deal.estimatedGrossRent * (deal.vacancyRatePercent / 100);
  rows.push({
    id: 'vacancyLoss',
    label: 'Less: Vacancy',
    category: 'income',
    parentCategory: 'income',
    isEditable: true,
    baseValue: vacancyAmount,
    growth: 0.025,
    invert: true
  });

  const netRentalBase = deal.estimatedGrossRent * (1 - deal.vacancyRatePercent / 100);
  rows.push({
    id: 'netRentalIncome',
    label: 'Net Rental Income',
    category: 'income',
    parentCategory: 'income',
    isEditable: false,
    componentIds: ['grossRent', 'vacancyLoss']
  });

  // Expenses Category
  rows.push({ id: 'expenses', label: 'Operating Expenses', isCategory: true, category: 'expenses' });

  // Outgoings - Expandable with children
  const outgoingsChildren = [
    { id: 'councilRates', label: 'Council Rates', baseValue: deal.outgoingsEstimate * 0.35, growth: 0.03 },
    { id: 'waterRates', label: 'Water Rates', baseValue: deal.outgoingsEstimate * 0.10, growth: 0.03 },
    { id: 'insurance', label: 'Insurance', baseValue: deal.outgoingsEstimate * 0.20, growth: 0.05 },
    { id: 'maintenance', label: 'Repairs & Maintenance', baseValue: deal.outgoingsEstimate * 0.25, growth: 0.03 },
    { id: 'other', label: 'Other', baseValue: deal.outgoingsEstimate * 0.10, growth: 0.02 },
    // Add custom items
    ...customLineItems.value.filter(c => c.parentId === 'outgoings').map(c => ({
      id: c.id,
      label: c.label,
      baseValue: c.amount,
      growth: c.growthRate / 100,
      isCustom: true
    }))
  ];

  rows.push({
    id: 'outgoings',
    label: 'Outgoings',
    category: 'expenses',
    parentCategory: 'expenses',
    isExpandable: true,
    childIds: outgoingsChildren.map(c => c.id)
  });

  // Add child rows
  outgoingsChildren.forEach(child => {
    rows.push({
      id: child.id,
      label: child.label,
      category: 'expenses',
      parentCategory: 'expenses',
      parentId: 'outgoings',
      isChild: true,
      isDeletable: child.isCustom,
      baseValue: child.baseValue,
      growth: child.growth,
      invert: true
    });
  });

  // Add button row
  rows.push({
    id: 'addOutgoingsItem',
    isAddButton: true,
    parentId: 'outgoings',
    parentCategory: 'expenses'
  });

  // Management Fee
  const managementCost = netRentalBase * (deal.managementFeePercent / 100);
  rows.push({
    id: 'managementFee',
    label: 'Management Fee',
    category: 'expenses',
    parentCategory: 'expenses',
    isEditable: true,
    baseValue: managementCost,
    growth: 0.025,
    invert: true
  });

  // NOI
  const outgoingsTotal = deal.outgoingsEstimate;
  const noiBase = netRentalBase - outgoingsTotal - managementCost;
  rows.push({
    id: 'noi',
    label: 'Net Operating Income',
    category: 'expenses',
    parentCategory: 'expenses',
    isEditable: false,
    componentIds: ['netRentalIncome', 'outgoings', 'managementFee']
  });

  // Financing Category
  rows.push({ id: 'financing', label: 'Financing', isCategory: true, category: 'financing' });

  const debtService = deal.loanAmount * (deal.interestRatePercent / 100);
  rows.push({
    id: 'debtService',
    label: 'Debt Service',
    category: 'financing',
    parentCategory: 'financing',
    isEditable: true,
    baseValue: debtService,
    invert: true
  });

  const netCashflowBase = noiBase - debtService;
  rows.push({
    id: 'netCashflow',
    label: 'Net Cashflow',
    category: 'financing',
    parentCategory: 'financing',
    isEditable: false,
    componentIds: ['noi', 'debtService']
  });

  // Summary
  rows.push({ id: 'summary', label: 'Summary', isCategory: true, category: 'summary' });

  const terminalValue = deal.askingPrice * Math.pow(1 + (deal.capitalGrowthPercent / 100), props.holdingPeriod);
  rows.push({
    id: 'terminalValue',
    label: 'Terminal Value',
    category: 'summary',
    parentCategory: 'summary',
    isEditable: true,
    isTerminalValue: true,
    terminalValue
  });

  return rows;
});

// Memoization for row cells to avoid redundant calculations in nested rows
const rowCellsCache = new Map();

// Clear cache when any reactive dependency changes
watch([() => props.deal, () => granularity.value, cellOverrides, customLineItems], () => {
  rowCellsCache.clear();
}, { deep: true });

// Get cells for a row (calculates values based on row config)
function getRowCells(row) {
  if (rowCellsCache.has(row.id)) {
    return rowCellsCache.get(row.id);
  }

  let cells;
  if (row.isExpandable || row.componentIds) {
    // Parent or Calculation row: sum components
    cells = getComponentSumCells(row);
  } else if (row.isTerminalValue) {
    // Terminal value: only show value in final period
    cells = columns.value.map((col, idx) => {
      const isLastColumn = idx === columns.value.length - 1;
      return { value: isLastColumn ? row.terminalValue : 0, isOverride: false };
    });
  } else {
    // Standard data row
    cells = createCellValues(row.baseValue, row.id, { growth: row.growth || 0, invert: row.invert });
  }

  rowCellsCache.set(row.id, cells);
  return cells;
}

// Sum components (children or listed component IDs)
function getComponentSumCells(row) {
  let components = [];
  if (row.componentIds) {
    components = flattenedRows.value.filter(r => row.componentIds.includes(r.id));
  } else if (row.isExpandable) {
    components = flattenedRows.value.filter(r => r.parentId === row.id && r.isChild);
  }

  return columns.value.map((col, colIndex) => {
    let total = 0;
    components.forEach(comp => {
      const compCells = getRowCells(comp);
      total += compCells[colIndex]?.value || 0;
    });
    return { value: total, isOverride: false };
  });
}

// Deprecated in favor of generic getComponentSumCells
function getParentRowCells(parentRow) {
  return getComponentSumCells(parentRow);
}

// Methods
function toggleCategory(categoryId) {
  const index = expandedCategories.value.indexOf(categoryId);
  if (index > -1) expandedCategories.value.splice(index, 1);
  else expandedCategories.value.push(categoryId);
}

function toggleRow(rowId) {
  const index = expandedRows.value.indexOf(rowId);
  if (index > -1) expandedRows.value.splice(index, 1);
  else expandedRows.value.push(rowId);
}

function isRowVisible(row) {
  if (row.isCategory) return true;
  return expandedCategories.value.includes(row.parentCategory);
}

function hasChildOverrides(parentRow) {
  if (!parentRow.isExpandable) return false;
  const childRows = flattenedRows.value.filter(r => r.parentId === parentRow.id && r.isChild);
  return childRows.some(child => 
    Object.keys(cellOverrides).some(key => key.startsWith(`${child.id}-`) && cellOverrides[key]?.isOverride)
  );
}

function hasChildOverridesForPeriod(parentRow, colIndex) {
  if (!parentRow.isExpandable) return false;
  const col = columns.value[colIndex];
  const childRows = flattenedRows.value.filter(r => r.parentId === parentRow.id && r.isChild);
  
  if (col?.isYearSummary) {
    // Check all months in this year
    const monthlyIndices = getMonthlyIndicesForYear(col.year);
    return childRows.some(child => 
      monthlyIndices.some(idx => cellOverrides[`${child.id}-${idx}`]?.isOverride)
    );
  }
  
  const monthlyIndex = getMonthlyIndex(colIndex);
  if (monthlyIndex < 0) return false;
  
  return childRows.some(child => cellOverrides[`${child.id}-${monthlyIndex}`]?.isOverride);
}

function hasRowOverrides(row) {
  return Object.keys(cellOverrides).some(key => key.startsWith(`${row.id}-`) && cellOverrides[key]?.isOverride);
}

function unlockParent(parentRow) {
  // Remove all child overrides
  const childRows = flattenedRows.value.filter(r => r.parentId === parentRow.id && r.isChild);
  childRows.forEach(child => {
    Object.keys(cellOverrides).forEach(key => {
      if (key.startsWith(`${child.id}-`)) {
        delete cellOverrides[key];
      }
    });
  });
}

function startEditing(row, colIndex) {
  const col = columns.value[colIndex];
  
  // Smart Yearly Edit Logic:
  // If editing a year summary (or standard year col in year view), check for child overrides
  if (col?.isYearSummary || (!isMonthly.value && col?.colType === 'year')) {
      if (hasChildOverridesForPeriod(row, colIndex)) {
          // Locked - ideally show toast/tooltip? For now just return
          return;
      }
      // If allowed, we proceed to edit. The finishEditing will handle distribution.
  }
  
  editingCell.value = { rowId: row.id, index: colIndex };
  nextTick(() => {
    const input = document.querySelector('.cell-input');
    if (input) {
      input.focus();
      input.select();
    }
  });
}

// Keyboard Navigation
function handleKeyDown(event, row, colIndex) {
  if (event.key === 'ArrowRight' || event.key === 'ArrowLeft' || event.key === 'ArrowUp' || event.key === 'ArrowDown') {
      // If modifying text (selection not at edge), don't navigate
      // Simple heuristic: if input active, only navigate if modifier pressed?
      // Or standard Excel behavior: if edit mode, arrows move cursor. Enter commits.
      // But requirement said "arrows move between cells". Usually this implies *navigation mode* vs *edit mode*.
      // User said "move between cells". Likely means when NOT editing or when navigation.
      // Current implementation: One click enters edit mode immediately.
      // So inside input, arrows should probably move cursor unless at boundary?
      // Let's implement Excel-style: Arrows navigate if not editing, or if editing and holding Ctrl?
      // User request implies simple nav. Let's make arrows navigate if we treat single-click as selection?
      // But we have click->edit. 
      // Compromise: If Shift+Arrow, or if valid cell.
      
      // Actually, standard behavior for web grids:
      // If we are in edit mode of a number input, Up/Down increments (we disabled arrows UI but keys might work).
      // Left/Right moves caret.
      // If user wants to move between cells, they usually press Tab/Enter.
      // But user requested "left/right arrows move between cells".
      // Let's assume they want to finish edit and move.
      
      if (event.key === 'ArrowRight') {
          finishEditing(event, row, colIndex);
          navigateWithDelay(row, colIndex, 1, 0);
      } else if (event.key === 'ArrowLeft') {
          finishEditing(event, row, colIndex);
          navigateWithDelay(row, colIndex, -1, 0);
      } else if (event.key === 'ArrowUp') {
          finishEditing(event, row, colIndex);
          navigateWithDelay(row, colIndex, 0, -1);
      } else if (event.key === 'ArrowDown') {
          finishEditing(event, row, colIndex);
          navigateWithDelay(row, colIndex, 0, 1);
      }
  }
}

function navigateWithDelay(row, colIndex, dCol, dRow) {
    // Determine next target
    const allVisibleRows = flattenedRows.value.filter(r => 
        (r.isEditable || r.isChild) && isRowVisible(r) && !r.isCategory && !r.isAddButton
    );
    const currentRowIdx = allVisibleRows.findIndex(r => r.id === row.id);
    let nextRowIdx = currentRowIdx + dRow;
    let nextColIdx = colIndex + dCol;
    
    // Bounds check
    if (nextRowIdx < 0) nextRowIdx = 0;
    if (nextRowIdx >= allVisibleRows.length) nextRowIdx = allVisibleRows.length - 1;
    
    if (nextColIdx < 0) nextColIdx = 0;
    if (nextColIdx >= columns.value.length) nextColIdx = columns.value.length - 1;
    
    const targetRow = allVisibleRows[nextRowIdx];
    
    // Start editing new cell
    nextTick(() => {
        startEditing(targetRow, nextColIdx);
    });
}

function finishEditing(event, row, colIndex) {
  const newValue = parseFloat(event.target.value) || 0;
  const isExpense = row.category === 'expenses' || row.category === 'financing';
  const col = columns.value[colIndex];

  // Smart Yearly Edit Logic (Distribute to months)
  if (col?.isYearSummary || (!isMonthly.value && col?.colType === 'year')) {
      const year = col.year;
      const monthlyIndices = getMonthlyIndicesForYear(year);
      
      // Calculate monthly amount (distribute evenly)
      // Note: newValue here is the YEARLY total they input.
      const monthlyAmount = newValue / 12;
      const valToSet = isExpense ? -Math.abs(monthlyAmount) : Math.abs(monthlyAmount);

      monthlyIndices.forEach(idx => {
          const key = `${row.id}-${idx}`;
          cellOverrides[key] = {
              value: valToSet,
              isOverride: true
          };
      });
      editingCell.value = null;
      emitChanges();
      return;
  }

  // Normal Monthly Edit
  const monthlyIndex = getMonthlyIndex(colIndex);
  
  if (monthlyIndex < 0) {
    editingCell.value = null;
    return;
  }
  
  const overrideKey = `${row.id}-${monthlyIndex}`;
  
  cellOverrides[overrideKey] = {
    value: isExpense ? -Math.abs(newValue) : newValue,
    isOverride: true
  };
  
  editingCell.value = null;
  emitChanges();
}

function cancelEditing() {
  editingCell.value = null;
}

function showContextMenu(event, row, colIndex) {
  const col = columns.value[colIndex];
  if (col?.isYearSummary) return;
  
  contextMenu.visible = true;
  contextMenu.x = event.clientX;
  contextMenu.y = event.clientY;
  contextMenu.rowId = row.id;
  contextMenu.cellIndex = colIndex;
}

function closeContextMenu() {
  contextMenu.visible = false;
}

function resetCellToCalculated() {
  const monthlyIndex = getMonthlyIndex(contextMenu.cellIndex);
  if (monthlyIndex >= 0) {
    const key = `${contextMenu.rowId}-${monthlyIndex}`;
    delete cellOverrides[key];
  } else {
     // If yearly locked cell (distribute reset?)
     // For now just allow reset on Monthly cells
     
     // Handle Unlock Year
     if (columns.value[contextMenu.cellIndex]?.isYearSummary) {
         const year = columns.value[contextMenu.cellIndex].year;
         const indices = getMonthlyIndicesForYear(year);
         indices.forEach(idx => {
             const key = `${contextMenu.rowId}-${idx}`;
             delete cellOverrides[key];
         });
     }
  }
  closeContextMenu();
  emitChanges();
}

function copyValue(direction) {
    const { rowId, cellIndex } = contextMenu;
    const monthlyIndex = getMonthlyIndex(cellIndex);
    
    // Simplification: Only support monthly cells for copy currently for safety
    if (monthlyIndex < 0) return closeContextMenu();
    
    // Get source value
    // We need to access the rendered value or override value. 
    // Easier to check overrides or calculated base.
    // Let's grab the value from the cellOverrides or recalculate?
    // We already have `flattenedRows` computed.
    const row = flattenedRows.value.find(r => r.id === rowId);
    if (!row) return closeContextMenu();
    
    // Find value from row.cells would be best but row.cells is generated in render loop in template (getRowCells)
    // We can re-call getRowCells(row)
    const cells = getRowCells(row);
    const sourceVal = cells[cellIndex]?.value || 0;
    
    // Determine target range
    const targetIndices = []; 
    // Copy Right: all subsequent months
    // Copy Left: all previous months
    // Limitation: Should this copy across years? Yes.
    
    // We iterate through all periods in current view?
    // No, we operate on ALL periods (years * 12).
    // The visual cellIndex maps to `columns`.
    
    const maxMonths = props.holdingPeriod * 12;
    
    if (direction === 'right') {
        for (let i = monthlyIndex + 1; i < maxMonths; i++) {
             targetIndices.push(i);
        }
    } else { // left
        for (let i = 0; i < monthlyIndex; i++) {
             targetIndices.push(i);
        }
    }
    
    targetIndices.forEach(idx => {
        const key = `${rowId}-${idx}`;
        cellOverrides[key] = {
            value: sourceVal, // Copy exact value
            isOverride: true
        };
    });
    
    closeContextMenu();
    emitChanges();
}

function openVarianceForCell() {
  const row = flattenedRows.value.find(r => r.id === contextMenu.rowId);
  varianceTarget.value = {
    label: row?.label || '',
    context: columns.value[contextMenu.cellIndex]?.label || '',
    rowId: contextMenu.rowId,
    cellIndex: contextMenu.cellIndex
  };
  varianceValue.value = 10;
  // Reset time variance inputs
  timeVarianceEarly.value = props.deal.timeVarianceEarlyMonths || 0;
  timeVarianceLate.value = props.deal.timeVarianceLateMonths || 3;
  
  showVarianceModal.value = true;
  closeContextMenu();
}

function applyVariance() {
  const monthlyIndex = getMonthlyIndex(varianceTarget.value.cellIndex);
  if (monthlyIndex >= 0) {
    const key = `${varianceTarget.value.rowId}-${monthlyIndex}`;
    // Store value variance in overrides if needed, but primarily we emit the Time Variance
    if (cellOverrides[key]) {
      cellOverrides[key].variance = varianceValue.value;
    }
  }
  
  // Emit time variance updates
  emit('apply-changes', {
    ...getCashflowData(),
    timeVariance: {
      early: timeVarianceEarly.value,
      late: timeVarianceLate.value
    }
  });
  
  showVarianceModal.value = false;
}

function openAddLineItemModal(parentId) {
  addModalParentId.value = parentId;
  newLineItem.value = { label: '', amount: 0, growthRate: 3 };
  showAddModal.value = true;
}

function addCustomLineItem() {
  customLineItems.value.push({
    id: `custom-${Date.now()}`,
    parentId: addModalParentId.value,
    label: newLineItem.value.label,
    amount: newLineItem.value.amount,
    growthRate: newLineItem.value.growthRate || 3
  });
  showAddModal.value = false;
}

function deleteCustomRow(rowId) {
  const index = customLineItems.value.findIndex(c => c.id === rowId);
  if (index > -1) {
    customLineItems.value.splice(index, 1);
    // Clear any overrides for this row
    Object.keys(cellOverrides).forEach(key => {
      if (key.startsWith(`${rowId}-`)) {
        delete cellOverrides[key];
      }
    });
  }
}



function formatCurrency(value) {
  if (value === 0) return '-';
  const absValue = Math.abs(value);
  const formatted = new Intl.NumberFormat('en-AU', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(absValue);
  return value < 0 ? `(${formatted})` : formatted;
}
const emitChanges = () => {
    emit('apply-changes', getCashflowData());
};

function getCashflowData() {
    return {
        rows: flattenedRows.value.map(row => ({
            id: row.id,
            cells: getRowCells(row) // capture current values including overrides
        })),
        granularity: granularity.value,
        timeVariance: {
            early: timeVarianceEarly.value,
            late: timeVarianceLate.value
        }
    };
}

// Initialize overrides from prop
watch(() => props.initialOverridesJson, (newJson) => {
  if (newJson) {
    try {
      const parsed = JSON.parse(newJson);
      // Clear existing first to avoid stale keys
      Object.keys(cellOverrides).forEach(key => delete cellOverrides[key]);
      Object.assign(cellOverrides, parsed);
    } catch (e) {
      console.error('Failed to parse overrides JSON', e);
    }
  }
}, { immediate: true });

// Watch for changes locally and emit (debounced)
let debounceTimer = null;
watch(cellOverrides, (newVal) => {
  if (debounceTimer) clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => {
    emit('update:overrides', JSON.stringify(newVal));
  }, 1000); // 1s debounce to avoid spamming API
}, { deep: true });

// Expose reset method
function resetAllOverrides() {
  if (confirm('Are you sure you want to restore all cells to calculated values? This cannot be undone.')) {
     Object.keys(cellOverrides).forEach(key => delete cellOverrides[key]);
     // This will trigger the watch above and emit empty JSON, clearing backend
  }
}

// Ensure overrides are valid numbers before calculating?
// The UI inputs handle type="number". The logic uses cellOverrides[key].value.
</script>

<style scoped>
.cashflow-spreadsheet {
  border: 2px solid var(--color-industrial-copper);
  border-radius: var(--radius-lg);
  overflow: hidden;
  background: var(--color-bg-card);
}

/* Toolbar */
.spreadsheet-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-md) var(--spacing-lg);
  background: linear-gradient(to right, var(--color-bg-elevated), rgba(180, 83, 9, 0.05));
  border-bottom: 1px solid var(--color-border);
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
}

.spreadsheet-title {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: 600;
}

.granularity-toggle {
  display: flex;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  overflow: hidden;
}

.toggle-btn {
  padding: var(--spacing-xs) var(--spacing-md);
  border: none;
  background: var(--color-bg-card);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.toggle-btn:not(:last-child) {
  border-right: 1px solid var(--color-border);
}

.toggle-btn.active {
  background: var(--color-industrial-copper);
  color: white;
}

.toolbar-right {
  display: flex;
  gap: var(--spacing-sm);
}

/* Spreadsheet */
.spreadsheet-container {
  overflow-x: auto;
  max-height: 600px;
  overflow-y: auto;
}

.spreadsheet-table {
  width: 100%;
  border-collapse: collapse;
  font-family: var(--font-mono);
  font-size: var(--font-size-sm);
}

.header-row {
  position: sticky;
  top: 0;
  z-index: 10;
  background: var(--color-bg-elevated);
}

.header-row th {
  padding: var(--spacing-sm) var(--spacing-md);
  text-align: right;
  font-weight: 600;
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-muted);
  border-bottom: 2px solid var(--color-industrial-copper);
  white-space: nowrap;
}

.row-label-header {
  text-align: left;
  min-width: 200px;
}

.period-header {
  min-width: 70px;
}

.period-header.year-start {
  border-left: 1px solid var(--color-border);
}

.period-header.year-summary {
  background: rgba(180, 83, 9, 0.08);
  font-weight: 700;
  color: var(--color-industrial-copper);
  border-left: 2px solid var(--color-industrial-copper);
  min-width: 85px;
}

.sticky-col {
  position: sticky;
  left: 0;
  z-index: 5;
  background: var(--color-bg-card);
}

/* Category Row */
.category-row {
  background: var(--color-bg-elevated);
}

.category-cell {
  padding: var(--spacing-sm) var(--spacing-md);
  font-weight: 600;
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-industrial-copper);
  border-top: 1px solid var(--color-border);
}

.expand-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: 2px;
  margin-right: var(--spacing-xs);
  color: var(--color-text-muted);
}

.expand-btn svg {
  transition: transform var(--transition-fast);
}

.expand-btn svg.rotated {
  transform: rotate(-90deg);
}

/* Data Rows */
.data-row {
  border-bottom: 1px solid var(--color-border-subtle);
}

.data-row:hover {
  background: rgba(180, 83, 9, 0.03);
}

.expandable-row {
  background: var(--color-bg-primary);
  font-weight: 500;
}

.expandable-row.has-overrides {
  background: rgba(16, 185, 129, 0.05);
}

.child-row {
  background: var(--color-bg-card);
}

.child-row.has-override {
  background: rgba(16, 185, 129, 0.03);
}

.child-label {
  display: flex;
  align-items: center;
}

.child-indent {
  width: 24px;
  height: 1px;
  background: var(--color-border);
  margin-right: var(--spacing-sm);
}

.row-label {
  padding: var(--spacing-sm) var(--spacing-md);
  text-align: left;
  color: var(--color-text-secondary);
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.locked-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  background: rgba(16, 185, 129, 0.1);
  border-radius: var(--radius-sm);
  color: var(--color-success);
  cursor: pointer;
}

.locked-badge:hover {
  background: rgba(16, 185, 129, 0.2);
}

.delete-row-btn {
  background: none;
  border: none;
  color: var(--color-danger);
  cursor: pointer;
  font-size: 1rem;
  padding: 0 4px;
  opacity: 0.5;
}

.delete-row-btn:hover {
  opacity: 1;
}

/* Value Cells */
.value-cell {
  padding: var(--spacing-sm) var(--spacing-md);
  text-align: right;
  position: relative;
}

.value-cell.editable {
  cursor: cell;
}

.value-cell.calculation-cell {
  background-color: var(--color-bg-primary) !important;
  font-weight: 600;
  cursor: default;
}

[data-theme="dark"] .value-cell.calculation-cell {
  background-color: rgba(255, 255, 255, 0.03) !important;
}

.value-cell.editable:hover {
  background: rgba(180, 83, 9, 0.08);
}

.value-cell.negative .cell-value {
  color: var(--color-danger);
}

.value-cell.has-override {
  background: rgba(16, 185, 129, 0.08);
}

.value-cell.locked {
  cursor: not-allowed;
}

.value-cell.year-summary {
  background: rgba(180, 83, 9, 0.06);
  font-weight: 600;
  border-left: 2px solid var(--color-industrial-copper);
}

.value-cell.year-summary.locked {
  background: rgba(16, 185, 129, 0.1);
}

.parent-cell {
  font-weight: 500;
}

.override-indicator {
  position: absolute;
  top: 2px;
  right: 2px;
  color: var(--color-success);
  font-size: 8px;
}

.lock-indicator {
  position: absolute;
  top: 3px;
  right: 3px;
  color: var(--color-success);
  font-size: 8px;
  opacity: 0.8;
}

.cell-input {
  width: 100%;
  height: 100%;
  border: 1px solid var(--color-industrial-copper);
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
  font-family: var(--font-mono);
  font-size: var(--font-size-xs);
  text-align: right;
  padding: 0 var(--spacing-xs);
  border-radius: var(--radius-sm);
  outline: none;
  /* Hide spin buttons */
  appearance: textfield;
  -moz-appearance: textfield;
}

.cell-input::-webkit-outer-spin-button,
.cell-input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

/* Add Row */
.add-row-row {
  background: var(--color-bg-card);
}

.add-row-cell {
  padding: var(--spacing-xs) var(--spacing-md);
  padding-left: 48px;
}

.add-row-btn {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-xs) var(--spacing-sm);
  background: none;
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.add-row-btn:hover {
  border-color: var(--color-industrial-copper);
  color: var(--color-industrial-copper);
}

/* Context Menu */
.context-menu-backdrop {
  position: fixed;
  inset: 0;
  z-index: 999;
}

.context-menu {
  position: fixed;
  z-index: 1000;
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-lg);
  min-width: 180px;
  padding: var(--spacing-xs);
}

.context-menu-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  width: 100%;
  padding: var(--spacing-sm) var(--spacing-md);
  border: none;
  background: none;
  text-align: left;
  font-size: var(--font-size-sm);
  cursor: pointer;
  border-radius: var(--radius-sm);
}

.context-menu-item:hover {
  background: var(--color-bg-elevated);
}

/* Modals */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  backdrop-filter: blur(4px);
}

.add-line-modal,
.variance-modal {
  width: 400px;
  max-width: 90%;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
}

.modal-header h4 {
  margin: 0;
  font-size: var(--font-size-lg);
}

.btn-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: var(--color-text-muted);
}

.modal-body {
  padding: var(--spacing-lg);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-md);
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-md);
  padding: var(--spacing-lg);
  border-top: 1px solid var(--color-border);
}

.variance-context {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-lg);
}

.variance-input-group label {
  display: block;
  margin-bottom: var(--spacing-xs);
  font-size: var(--font-size-sm);
  font-weight: 500;
}

.variance-input {
  width: 80px;
  padding: var(--spacing-sm);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  text-align: center;
}

.variance-slider {
  width: 100%;
  margin-top: var(--spacing-sm);
}

/* Time Variance Styles */
.time-variance {
  margin-top: var(--spacing-lg);
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-border);
}

.time-variance label {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
}

.time-variance label svg {
  color: var(--color-info);
}

.time-variance-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-md);
  margin-top: var(--spacing-sm);
}

.time-input {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
}

.time-label {
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
  padding: 2px 6px;
  border-radius: var(--radius-sm);
}

.p10-label {
  background: rgba(16, 185, 129, 0.1);
  color: var(--color-success);
}

.p90-label {
  background: rgba(239, 68, 68, 0.1);
  color: var(--color-danger);
}

.time-unit {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.time-divider {
  color: var(--color-text-muted);
  font-size: var(--font-size-lg);
}

.variance-input.small {
  width: 50px;
}

.time-variance-hint {
  margin-top: var(--spacing-sm);
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  text-align: center;
}
</style>
