<template>
  <div class="detailed-model">
    <!-- Toolbar -->
    <div class="model-toolbar">
      <div class="toolbar-section">
        <div class="view-toggle">
          <button 
            :class="['toggle-btn', { active: viewMode === 'years' }]"
            @click="viewMode = 'years'"
          >Years</button>
          <button 
            :class="['toggle-btn', { active: viewMode === 'months' }]"
            @click="viewMode = 'months'"
          >Months</button>
        </div>
      </div>
      <div class="toolbar-section">
        <button class="btn btn-secondary btn-sm" @click="unlockAll" title="Remove all manual overrides">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
            <path d="M7 11V7a5 5 0 0 1 9.9-1"></path>
          </svg>
          Unlock All
        </button>
      </div>
    </div>

    <!-- Spreadsheet -->
    <div class="spreadsheet-wrapper" ref="spreadsheetWrapper" @keydown="handleKeyDown" tabindex="0">
      <table class="spreadsheet">
        <thead>
          <tr>
            <th class="row-header sticky-col">Line Item</th>
            <th 
              v-for="col in columns" 
              :key="col.key"
              :class="['period-col', { 'year-boundary': col.isYearStart, 'year-total': col.isYearTotal }]"
            >
              {{ col.label }}
            </th>
          </tr>
        </thead>
        <tbody>
          <!-- Income Section -->
          <tr class="section-header">
            <td colspan="100%">
              <button class="section-toggle" @click="toggleSection('income')">
                <svg :class="{ collapsed: !sections.income }" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
                Income
              </button>
            </td>
          </tr>
          <template v-if="sections.income">
            <!-- Editable income rows -->
            <tr v-for="row in incomeEditableRows" :key="row.id" :class="['data-row', { 'calculated-row': row.isCalculated }]">
              <td class="row-header sticky-col">
                <span class="row-label">{{ row.label }}</span>
                <button v-if="hasRowLocks(row.id)" class="unlock-row-btn" @click="unlockRow(row.id)" title="Unlock all cells in this row">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                    <path d="M7 11V7a5 5 0 0 1 9.9-1"></path>
                  </svg>
                </button>
                <button v-if="row.isCustom" class="delete-btn" @click="deleteRevenueRow(row.id)" title="Remove">×</button>
              </td>
              <td 
                v-for="(col, colIdx) in columns" 
                :key="col.key"
                :class="getCellClasses(row, col, colIdx)"
                @click="handleCellClick(row, colIdx)"
                @dblclick="startEdit(row, colIdx)"
                @contextmenu.prevent="showContextMenu($event, row, colIdx)"
              >
                <template v-if="isEditing(row.id, colIdx)">
                  <input 
                    ref="editInput"
                    type="text"
                    class="cell-edit"
                    :value="editValue"
                    @input="editValue = $event.target.value"
                    @blur="finishEdit"
                    @keydown.enter="finishEdit"
                    @keydown.escape="cancelEdit"
                    @keydown.up.prevent="finishEditAndMove('up')"
                    @keydown.down.prevent="finishEditAndMove('down')"
                    @keydown.left="handleInputArrow($event, 'left')"
                    @keydown.right="handleInputArrow($event, 'right')"
                  />
                </template>
                <template v-else>
                  <span class="cell-value">{{ formatValue(getCellValue(row, colIdx)) }}</span>
                  <span v-if="isCellLocked(row.id, colIdx)" class="lock-icon" title="Manually overridden">
                    <svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor">
                      <rect x="3" y="11" width="18" height="11" rx="2"></rect>
                      <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                    </svg>
                  </span>
                  <span v-else-if="hasMixedOverrides(row.id, colIdx)" class="lock-icon mixed" title="Locked due to monthly inputs">
                    <svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor">
                      <rect x="3" y="11" width="18" height="11" rx="2"></rect>
                      <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                    </svg>
                  </span>
                </template>
              </td>
            </tr>
            <!-- Add Revenue button -->
            <tr class="add-row">
              <td colspan="100%">
                <button class="add-btn" @click="showAddRevenueModal = true">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="5" x2="12" y2="19"></line>
                    <line x1="5" y1="12" x2="19" y2="12"></line>
                  </svg>
                  Add Revenue Line
                </button>
              </td>
            </tr>
            <!-- Calculated income rows (Net Rental Income) -->
            <tr v-for="row in incomeCalculatedRows" :key="row.id" class="data-row calculated-row">
              <td class="row-header sticky-col">
                <span class="row-label">{{ row.label }}</span>
              </td>
              <td 
                v-for="(col, colIdx) in columns" 
                :key="col.key"
                :class="['cell', 'calculated', { 'year-total': col.isYearTotal }]"
              >
                <span class="cell-value">{{ formatValue(getCellValue(row, colIdx)) }}</span>
              </td>
            </tr>
          </template>

          <!-- Expenses Section -->
          <tr class="section-header">
            <td colspan="100%">
              <button class="section-toggle" @click="toggleSection('expenses')">
                <svg :class="{ collapsed: !sections.expenses }" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
                Operating Expenses
              </button>
            </td>
          </tr>
          <template v-if="sections.expenses">
            <!-- Editable expense rows -->
            <tr v-for="row in expenseEditableRows" :key="row.id" :class="['data-row', { 'calculated-row': row.isCalculated, 'sub-row': row.isChild }]">
              <td class="row-header sticky-col">
                <span :class="['row-label', { indented: row.isChild }]">{{ row.label }}</span>
                <button v-if="hasRowLocks(row.id)" class="unlock-row-btn" @click="unlockRow(row.id)" title="Unlock all cells in this row">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                    <path d="M7 11V7a5 5 0 0 1 9.9-1"></path>
                  </svg>
                </button>
                <button v-if="row.isCustom" class="delete-btn" @click="deleteExpenseRow(row.id)" title="Remove">×</button>
              </td>
              <td 
                v-for="(col, colIdx) in columns" 
                :key="col.key"
                :class="getCellClasses(row, col, colIdx)"
                @click="handleCellClick(row, colIdx)"
                @dblclick="startEdit(row, colIdx)"
                @contextmenu.prevent="showContextMenu($event, row, colIdx)"
              >
                <template v-if="isEditing(row.id, colIdx)">
                  <input 
                    ref="editInput"
                    type="text"
                    class="cell-edit"
                    :value="editValue"
                    @input="editValue = $event.target.value"
                    @blur="finishEdit"
                    @keydown.enter="finishEdit"
                    @keydown.escape="cancelEdit"
                    @keydown.up.prevent="finishEditAndMove('up')"
                    @keydown.down.prevent="finishEditAndMove('down')"
                    @keydown.left="handleInputArrow($event, 'left')"
                    @keydown.right="handleInputArrow($event, 'right')"
                  />
                </template>
                <template v-else>
                  <span class="cell-value">{{ formatValue(getCellValue(row, colIdx), true) }}</span>
                  <span v-if="isCellLocked(row.id, colIdx)" class="lock-icon" title="Manually overridden">
                    <svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor">
                      <rect x="3" y="11" width="18" height="11" rx="2"></rect>
                      <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                    </svg>
                  </span>
                  <span v-else-if="hasMixedOverrides(row.id, colIdx)" class="lock-icon mixed" title="Locked due to monthly inputs">
                    <svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor">
                      <rect x="3" y="11" width="18" height="11" rx="2"></rect>
                      <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                    </svg>
                  </span>
                </template>
              </td>
            </tr>
            <!-- Add Expense button -->
            <tr class="add-row">
              <td colspan="100%">
                <button class="add-btn" @click="showAddExpenseModal = true">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="5" x2="12" y2="19"></line>
                    <line x1="5" y1="12" x2="19" y2="12"></line>
                  </svg>
                  Add Expense Line
                </button>
              </td>
            </tr>
            <!-- Calculated expense rows (Total Operating Expenses, NOI) -->
            <tr v-for="row in expenseCalculatedRows" :key="row.id" class="data-row calculated-row">
              <td class="row-header sticky-col">
                <span class="row-label">{{ row.label }}</span>
              </td>
              <td 
                v-for="(col, colIdx) in columns" 
                :key="col.key"
                :class="['cell', 'calculated', { 'year-total': col.isYearTotal }]"
              >
                <span class="cell-value">{{ formatValue(getCellValue(row, colIdx), true) }}</span>
              </td>
            </tr>
          </template>

          <!-- Financing Section -->
          <tr class="section-header">
            <td colspan="100%">
              <button class="section-toggle" @click="toggleSection('financing')">
                <svg :class="{ collapsed: !sections.financing }" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
                Financing
              </button>
            </td>
          </tr>
          <template v-if="sections.financing">
            <!-- Editable financing rows -->
            <tr v-for="row in financingEditableRows" :key="row.id" :class="['data-row', { 'calculated-row': row.isCalculated }]">
              <td class="row-header sticky-col">
                <span class="row-label">{{ row.label }}</span>
                <button v-if="hasRowLocks(row.id)" class="unlock-row-btn" @click="unlockRow(row.id)" title="Unlock all cells in this row">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                    <path d="M7 11V7a5 5 0 0 1 9.9-1"></path>
                  </svg>
                </button>
                <button v-if="row.isCustom" class="delete-btn" @click="deleteDebtRow(row.id)" title="Remove">×</button>
              </td>
              <td 
                v-for="(col, colIdx) in columns" 
                :key="col.key"
                :class="getCellClasses(row, col, colIdx)"
                @click="handleCellClick(row, colIdx)"
                @dblclick="startEdit(row, colIdx)"
                @contextmenu.prevent="showContextMenu($event, row, colIdx)"
              >
                <template v-if="isEditing(row.id, colIdx)">
                  <input 
                    ref="editInput"
                    type="text"
                    class="cell-edit"
                    :value="editValue"
                    @input="editValue = $event.target.value"
                    @blur="finishEdit"
                    @keydown.enter="finishEdit"
                    @keydown.escape="cancelEdit"
                    @keydown.up.prevent="finishEditAndMove('up')"
                    @keydown.down.prevent="finishEditAndMove('down')"
                    @keydown.left="handleInputArrow($event, 'left')"
                    @keydown.right="handleInputArrow($event, 'right')"
                  />
                </template>
                <template v-else>
                  <span class="cell-value">{{ formatValue(getCellValue(row, colIdx), true) }}</span>
                  <span v-if="isCellLocked(row.id, colIdx)" class="lock-icon" title="Manually overridden">
                    <svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor">
                      <rect x="3" y="11" width="18" height="11" rx="2"></rect>
                      <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                    </svg>
                  </span>
                  <span v-else-if="hasMixedOverrides(row.id, colIdx)" class="lock-icon mixed" title="Locked due to monthly inputs">
                    <svg width="10" height="10" viewBox="0 0 24 24" fill="currentColor">
                      <rect x="3" y="11" width="18" height="11" rx="2"></rect>
                      <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                    </svg>
                  </span>
                </template>
              </td>
            </tr>
            <!-- Add Debt Line button -->
            <tr class="add-row">
              <td colspan="100%">
                <button class="add-btn" @click="showAddDebtModal = true">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="12" y1="5" x2="12" y2="19"></line>
                    <line x1="5" y1="12" x2="19" y2="12"></line>
                  </svg>
                  Add Debt Line
                </button>
              </td>
            </tr>

          </template>

          <!-- Summary Section -->
          <tr class="section-header summary-section">
            <td colspan="100%">
              <button class="section-toggle" @click="toggleSection('summary')">
                <svg :class="{ collapsed: !sections.summary }" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
                Summary
              </button>
            </td>
          </tr>
          <template v-if="sections.summary">
            <tr v-for="row in summaryRows" :key="row.id" :class="['data-row', 'spreadsheet-summary-row']">
              <td class="row-header sticky-col">
                <span class="row-label summary-label">{{ row.label }}</span>
              </td>
              <td 
                v-for="(col, colIdx) in columns" 
                :key="col.key"
                :class="['cell', 'spreadsheet-summary-cell', { 'year-total': col.isYearTotal, 'negative': getCellValue(row, colIdx) < 0 }]"
              >
                <span class="cell-value">{{ formatValue(getCellValue(row, colIdx)) }}</span>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <!-- Context Menu -->
    <Teleport to="body">
      <div 
        v-if="contextMenu.visible" 
        class="context-menu"
        :style="{ top: contextMenu.y + 'px', left: contextMenu.x + 'px' }"
        @click.stop
      >
        <button class="context-item" @click="handleContextAction('edit')">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
          </svg>
          Edit Value
        </button>
        <button class="context-item" @click="handleContextAction('copyLeft')">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"></polyline>
          </svg>
          Copy Left
        </button>
        <button class="context-item" @click="handleContextAction('copyRight')">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="9 18 15 12 9 6"></polyline>
          </svg>
          Copy Right
        </button>
        <div class="context-divider"></div>
        <button class="context-item" @click="handleContextAction('unlockRow')">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
            <path d="M7 11V7a5 5 0 0 1 9.9-1"></path>
          </svg>
          Unlock Row
        </button>
        <div class="context-divider"></div>
        <button class="context-item" @click="handleContextAction('reset')" :disabled="!isCellLocked(contextMenu.rowId, contextMenu.colIdx)">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="1 4 1 10 7 10"></polyline>
            <path d="M3.51 15a9 9 0 1 0 2.13-9.36L1 10"></path>
          </svg>
          Reset to Calculated
        </button>
      </div>
      <div v-if="contextMenu.visible" class="context-backdrop" @click="closeContextMenu"></div>
    </Teleport>

    <!-- Add Expense Modal -->
    <Teleport to="body">
      <div v-if="showAddExpenseModal" class="modal-backdrop" @click.self="showAddExpenseModal = false">
        <div class="modal">
          <div class="modal-header">
            <h3>Add Expense Line Item</h3>
            <button class="modal-close" @click="showAddExpenseModal = false">×</button>
          </div>
          <form @submit.prevent="addExpenseLine" class="modal-body">
            <div class="form-group">
              <label>Label</label>
              <input v-model="newExpense.label" type="text" required placeholder="e.g., Body Corporate" />
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Annual Amount ($)</label>
                <input v-model.number="newExpense.amount" type="number" min="0" required />
              </div>
              <div class="form-group">
                <label>Growth Rate (%)</label>
                <input v-model.number="newExpense.growth" type="number" step="0.5" />
              </div>
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showAddExpenseModal = false">Cancel</button>
              <button type="submit" class="btn btn-primary">Add</button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- Add Revenue Modal -->
    <Teleport to="body">
      <div v-if="showAddRevenueModal" class="modal-backdrop" @click.self="showAddRevenueModal = false">
        <div class="modal">
          <div class="modal-header">
            <h3>Add Revenue Line Item</h3>
            <button class="modal-close" @click="showAddRevenueModal = false">×</button>
          </div>
          <form @submit.prevent="addRevenueLine" class="modal-body">
            <div class="form-group">
              <label>Label</label>
              <input v-model="newRevenue.label" type="text" required placeholder="e.g., Parking Income" />
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Annual Amount ($)</label>
                <input v-model.number="newRevenue.amount" type="number" min="0" required />
              </div>
              <div class="form-group">
                <label>Growth Rate (%)</label>
                <input v-model.number="newRevenue.growth" type="number" step="0.5" />
              </div>
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showAddRevenueModal = false">Cancel</button>
              <button type="submit" class="btn btn-primary">Add</button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- Add Debt Modal -->
    <Teleport to="body">
      <div v-if="showAddDebtModal" class="modal-backdrop" @click.self="showAddDebtModal = false">
        <div class="modal">
          <div class="modal-header">
            <h3>Add Debt Line Item</h3>
            <button class="modal-close" @click="showAddDebtModal = false">×</button>
          </div>
          <form @submit.prevent="addDebtLine" class="modal-body">
            <div class="form-group">
              <label>Label</label>
              <input v-model="newDebt.label" type="text" required placeholder="e.g., Second Mortgage" />
            </div>
            <div class="form-group">
              <label>Annual Payment ($)</label>
              <input v-model.number="newDebt.amount" type="number" min="0" required />
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showAddDebtModal = false">Cancel</button>
              <button type="submit" class="btn btn-primary">Add</button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, watch, nextTick, reactive, onMounted } from 'vue';

const props = defineProps({
  deal: { type: Object, required: true },
  holdingPeriod: { type: Number, default: 10 },
  initialOverridesJson: { type: String, default: null }
});

const emit = defineEmits(['update:overrides', 'apply-changes']);

// View state
const viewMode = ref('years');
const sections = reactive({ income: true, expenses: true, financing: true, summary: true });
const selectedCell = ref(null);
const editingCell = ref(null);
const editValue = ref('');
const initialEditValue = ref('');
const editInput = ref(null);
const spreadsheetWrapper = ref(null);

// Data state
const overrides = reactive({});
const customExpenses = ref([]);
const customRevenue = ref([]);
const customDebt = ref([]);

// Context menu
const contextMenu = reactive({ visible: false, x: 0, y: 0, rowId: '', colIdx: -1 });

// Modals
const showAddExpenseModal = ref(false);
const showAddRevenueModal = ref(false);
const showAddDebtModal = ref(false);
const newExpense = ref({ label: '', amount: 0, growth: 3 });
const newRevenue = ref({ label: '', amount: 0, growth: 2.5 });
const newDebt = ref({ label: '', amount: 0 });

// Debug logging

onMounted(() => {
  console.log('[CashflowSpreadsheet] Mounted with:', {
    deal: props.deal,
    holdingPeriod: props.holdingPeriod,
    columnsCount: columns.value.length,
    incomeRowsCount: incomeRows.value.length,
    expenseRowsCount: expenseRows.value.length,
    firstIncomeRow: incomeRows.value[0],
    columns: columns.value.slice(0, 3)
  });
  
  // Log first cell value
  if (incomeRows.value[0] && columns.value.length > 0) {
    console.log('[CashflowSpreadsheet] First cell value:', getCellValue(incomeRows.value[0], 0));
  }
});

// Load initial overrides
watch(() => props.initialOverridesJson, (json) => {
  if (json) {
    try {
      const parsed = JSON.parse(json);
      Object.keys(overrides).forEach(key => delete overrides[key]);
      Object.assign(overrides, parsed);
    } catch (e) {
      console.error('Failed to parse overrides', e);
    }
  }
}, { immediate: true });

// Columns
const columns = computed(() => {
  const result = [];
  const period = props.holdingPeriod || 10; // Ensure we have at least 10 years if undefined
  
  if (viewMode.value === 'years') {
    for (let y = 1; y <= period; y++) {
      result.push({ key: `Y${y}`, label: `Year ${y}`, year: y, isYearTotal: false });
    }
  } else {
    for (let y = 1; y <= period; y++) {
      for (let m = 1; m <= 12; m++) {
        result.push({
          key: `Y${y}M${m}`,
          label: `M${m}`,
          year: y,
          month: m,
          isYearStart: m === 1,
          isYearTotal: false
        });
      }
      result.push({
        key: `Y${y}Total`,
        label: `Y${y}`,
        year: y,
        isYearTotal: true
      });
    }
  }
  return result;
});

// Row definitions - separate editable rows from calculated rows
const incomeEditableRows = computed(() => {
  const deal = props.deal || {};
  const grossRent = deal.estimatedGrossRent || 0;
  const vacancyRate = deal.vacancyRatePercent || 5;
  const rentalGrowth = deal.rentalGrowthPercent || 2.5;
  
  return [
    { id: 'grossRent', label: 'Gross Rent', baseValue: grossRent, growth: rentalGrowth / 100, editable: true },
    { id: 'vacancy', label: 'Less: Vacancy Allowance', baseValue: grossRent * (vacancyRate / 100), growth: 0, editable: true, isExpense: true },
    ...customRevenue.value.map(r => ({
      id: r.id,
      label: r.label,
      isCustom: true,
      baseValue: r.amount || 0,
      growth: (r.growth || 2.5) / 100,
      editable: true
    }))
  ];
});

const incomeCalculatedRows = computed(() => [
  { id: 'netRentalIncome', label: 'Net Rental Income', isCalculated: true, formula: (data) => {
    let total = (data.grossRent || 0) - (data.vacancy || 0);
    customRevenue.value.forEach(r => { total += (data[r.id] || 0); });
    return total;
  }}
]);

const incomeRows = computed(() => [...incomeEditableRows.value, ...incomeCalculatedRows.value]);

const expenseEditableRows = computed(() => {
  const deal = props.deal || {};
  const outgoingsGrowth = (deal.outgoingsGrowthPercent || 3) / 100;
  const baseOutgoings = deal.outgoingsEstimate || 10000;
  const grossRent = deal.estimatedGrossRent || 0;
  const vacancyRate = deal.vacancyRatePercent || 5;
  const managementRate = deal.managementFeePercent || 7;
  const netRent = grossRent * (1 - vacancyRate / 100);
  
  return [
    { id: 'councilRates', label: 'Council Rates', isChild: true, baseValue: baseOutgoings * 0.35, growth: outgoingsGrowth, editable: true },
    { id: 'waterRates', label: 'Water Rates', isChild: true, baseValue: baseOutgoings * 0.10, growth: outgoingsGrowth, editable: true },
    { id: 'insurance', label: 'Insurance', isChild: true, baseValue: baseOutgoings * 0.20, growth: outgoingsGrowth, editable: true },
    { id: 'maintenance', label: 'Repairs & Maintenance', isChild: true, baseValue: baseOutgoings * 0.25, growth: outgoingsGrowth, editable: true },
    { id: 'managementFee', label: 'Management Fee', isChild: true, baseValue: netRent * (managementRate / 100), growth: outgoingsGrowth, editable: true },
    { id: 'otherExpenses', label: 'Other', isChild: true, baseValue: baseOutgoings * 0.10, growth: outgoingsGrowth, editable: true },
    ...customExpenses.value.map(e => ({
      id: e.id,
      label: e.label,
      isChild: true,
      isCustom: true,
      baseValue: e.amount || 0,
      growth: (e.growth || 3) / 100,
      editable: true
    }))
  ];
});

const expenseCalculatedRows = computed(() => [
  { id: 'totalExpenses', label: 'Total Operating Expenses', isCalculated: true, formula: (data) => {
    let total = (data.councilRates || 0) + (data.waterRates || 0) + (data.insurance || 0) + (data.maintenance || 0) + (data.managementFee || 0) + (data.otherExpenses || 0);
    customExpenses.value.forEach(e => { total += (data[e.id] || 0); });
    return total;
  }},
  { id: 'noi', label: 'Net Operating Income', isCalculated: true, formula: (data) => (data.netRentalIncome || 0) - (data.totalExpenses || 0) }
]);

const expenseRows = computed(() => [...expenseEditableRows.value, ...expenseCalculatedRows.value]);

const financingEditableRows = computed(() => {
  const deal = props.deal || {};
  const loanAmount = deal.loanAmount || 0;
  const interestRate = deal.interestRatePercent || 5;
  const annualDebtService = loanAmount * (interestRate / 100);
  
  return [
    { id: 'debtService', label: 'Debt Service (Interest)', baseValue: annualDebtService, growth: 0, editable: true },
    ...customDebt.value.map(d => ({
      id: d.id,
      label: d.label,
      isCustom: true,
      baseValue: d.amount || 0,
      growth: 0,
      editable: true
    }))
  ];
});

const financingRows = computed(() => financingEditableRows.value);

const summaryRows = computed(() => {
  return [
    { id: 'netCashflowSummary', label: 'Net Cashflow', isCalculated: true, formula: (data) => data.netCashflow || 0 }
  ];
});

// Functions
function toggleSection(section) {
  sections[section] = !sections[section];
}

function getMonthlyIndex(colIdx) {
  if (viewMode.value === 'years') {
    return colIdx * 12; // Map year to first month
  }
  const col = columns.value[colIdx];
  if (col?.isYearTotal) return -1;
  return (col.year - 1) * 12 + (col.month - 1);
}

function getCellValue(row, colIdx) {
  const col = columns.value[colIdx];
  
  if (row.isCalculated) {
    // Calculate from formula
    const data = getAllRowData(colIdx);
    return row.formula(data);
  }
  
  const monthIdx = getMonthlyIndex(colIdx);
  
  if (viewMode.value === 'years') {
    // Sum 12 months
    let total = 0;
    for (let m = 0; m < 12; m++) {
      const key = `${row.id}-${colIdx * 12 + m}`;
      if (overrides[key] !== undefined) {
        total += overrides[key];
      } else {
        total += calculateBaseValue(row, colIdx, m);
      }
    }
    return row.isExpense ? -total : total;
  } else {
    if (col?.isYearTotal) {
      // Sum months for this year
      let total = 0;
      for (let m = 0; m < 12; m++) {
        const mIdx = (col.year - 1) * 12 + m;
        const key = `${row.id}-${mIdx}`;
        if (overrides[key] !== undefined) {
          total += overrides[key];
        } else {
          total += calculateBaseValue(row, col.year - 1, m);
        }
      }
      return row.isExpense ? -total : total;
    }
    
    const key = `${row.id}-${monthIdx}`;
    if (overrides[key] !== undefined) {
      return row.isExpense ? -overrides[key] : overrides[key];
    }
    const val = calculateBaseValue(row, col.year - 1, col.month - 1);
    return row.isExpense ? -val : val;
  }
}

function calculateBaseValue(row, yearIdx, monthIdx = 0) {
  const baseValue = row.baseValue || 0;
  if (!baseValue || isNaN(baseValue)) return 0;
  const monthlyBase = baseValue / 12;
  const growth = row.growth || 0;
  return monthlyBase * Math.pow(1 + growth, yearIdx);
}

function getAllRowData(colIdx) {
  const data = {};
  const allRows = [...incomeRows.value, ...expenseRows.value, ...financingRows.value];
  
  for (const row of allRows) {
    if (!row.isCalculated) {
      data[row.id] = Math.abs(getCellValue(row, colIdx));
    }
  }
  
  // Calculate derived values
  data.netRentalIncome = (data.grossRent || 0) - (data.vacancy || 0);
  data.totalExpenses = (data.councilRates || 0) + (data.waterRates || 0) + (data.insurance || 0) + 
                       (data.maintenance || 0) + (data.managementFee || 0) + (data.otherExpenses || 0);
  customExpenses.value.forEach(e => {
    data.totalExpenses += data[e.id] || 0;
  });
  data.noi = data.netRentalIncome - data.totalExpenses;
  let totalDebt = (data.debtService || 0);
  customDebt.value.forEach(d => { totalDebt += data[d.id] || 0; });
  data.netCashflow = data.noi - totalDebt;
  
  return data;
}

function getCellClasses(row, col, colIdx) {
  const classes = ['cell'];
  if (col.isYearTotal) classes.push('year-total');
  if (col.isYearStart) classes.push('year-start');
  if (row.editable && !col.isYearTotal) classes.push('editable');
  if (row.isCalculated) classes.push('calculated');
  if (selectedCell.value?.rowId === row.id && selectedCell.value?.colIdx === colIdx) classes.push('selected');
  if (isCellLocked(row.id, colIdx)) classes.push('locked');
  if (hasMixedOverrides(row.id, colIdx)) classes.push('mixed-locked');
  
  const val = getCellValue(row, colIdx);
  if (val < 0) classes.push('negative');
  
  return classes;
}

function isCellLocked(rowId, colIdx) {
  if (viewMode.value === 'years') {
    // Check if any month in this year is overridden
    for (let m = 0; m < 12; m++) {
      if (overrides[`${rowId}-${colIdx * 12 + m}`] !== undefined) return true;
    }
    return false;
  }
  const monthIdx = getMonthlyIndex(colIdx);
  return monthIdx >= 0 && overrides[`${rowId}-${monthIdx}`] !== undefined;
}

function selectCell(rowId, colIdx) {
  selectedCell.value = { rowId, colIdx };
}

function handleCellClick(row, colIdx) {
  selectCell(row.id, colIdx);
  startEdit(row, colIdx);
}

function isEditing(rowId, colIdx) {
  return editingCell.value?.rowId === rowId && editingCell.value?.colIdx === colIdx;
}

function focusWrapper() {
  nextTick(() => {
    if (spreadsheetWrapper.value) spreadsheetWrapper.value.focus();
  });
}

function hasMixedOverrides(rowId, colIdx) {
  if (viewMode.value !== 'years') return false;
  
  const startMonth = colIdx * 12;
  const k0 = `${rowId}-${startMonth}`;
  const firstVal = overrides[k0];
  
  // If first month is not set, check if ANY other is set (partial overrides)
  if (firstVal === undefined) {
    for (let m = 1; m < 12; m++) {
      if (overrides[`${rowId}-${startMonth + m}`] !== undefined) return true;
    }
    return false; // No overrides
  }
  
  // If first month is set, all must match
  for (let m = 1; m < 12; m++) {
    if (overrides[`${rowId}-${startMonth + m}`] !== firstVal) return true;
  }
  return false; // Uniform overrides
}

function startEdit(row, colIdx) {
  const col = columns.value[colIdx];
  // Don't edit calculated rows or year totals
  if (row.isCalculated || col?.isYearTotal) return;
  // Don't edit if locked due to mixed monthly inputs
  if (hasMixedOverrides(row.id, colIdx)) return;
  
  editingCell.value = { rowId: row.id, colIdx };
  editValue.value = Math.abs(getCellValue(row, colIdx)).toFixed(0);
  initialEditValue.value = editValue.value;
  
  nextTick(() => {
    const input = document.querySelector('.cell-edit');
    if (input) {
      input.focus();
      input.select();
    }
  });
}

function finishEdit() {
  if (!editingCell.value) return;
  
  const { rowId, colIdx } = editingCell.value;
  const value = parseFloat(editValue.value) || 0;
  const initial = parseFloat(initialEditValue.value) || 0;
  
  // If value hasn't changed, don't set override (don't lock)
  if (Math.abs(value - initial) < 0.01) {
    editingCell.value = null;
    focusWrapper();
    return;
  }
  
  if (viewMode.value === 'years') {
    // Distribute to 12 months
    const monthlyValue = value / 12;
    for (let m = 0; m < 12; m++) {
      overrides[`${rowId}-${colIdx * 12 + m}`] = monthlyValue;
    }
  } else {
    const monthIdx = getMonthlyIndex(colIdx);
    if (monthIdx >= 0) {
      overrides[`${rowId}-${monthIdx}`] = value;
    }
  }
  
  editingCell.value = null;
  emitChanges();
  focusWrapper();
}

function cancelEdit() {
  editingCell.value = null;
  focusWrapper();
}

function moveSelection(direction) {
  if (!selectedCell.value) return;
  const { rowId, colIdx } = selectedCell.value;
  const allRows = [...incomeRows.value, ...expenseRows.value, ...financingRows.value, ...summaryRows.value];
  const currentRowIdx = allRows.findIndex(r => r.id === rowId);

  switch (direction) {
    case 'right':
      if (colIdx < columns.value.length - 1) {
        selectedCell.value = { rowId, colIdx: colIdx + 1 };
      }
      break;
    case 'left':
      if (colIdx > 0) {
        selectedCell.value = { rowId, colIdx: colIdx - 1 };
      }
      break;
    case 'down':
      if (currentRowIdx < allRows.length - 1) {
        selectedCell.value = { rowId: allRows[currentRowIdx + 1].id, colIdx };
      }
      break;
    case 'up':
      if (currentRowIdx > 0) {
        selectedCell.value = { rowId: allRows[currentRowIdx - 1].id, colIdx };
      }
      break;
  }
}

function handleKeyDown(e) {
  if (!selectedCell.value || editingCell.value) return;
  
  switch (e.key) {
    case 'ArrowRight':
      e.preventDefault();
      moveSelection('right');
      break;
    case 'ArrowLeft':
      e.preventDefault();
      moveSelection('left');
      break;
    case 'ArrowDown':
      e.preventDefault();
      moveSelection('down');
      break;
    case 'ArrowUp':
      e.preventDefault();
      moveSelection('up');
      break;
    case 'Enter':
      const allRows = [...incomeRows.value, ...expenseRows.value, ...financingRows.value, ...summaryRows.value];
      const row = allRows.find(r => r.id === selectedCell.value.rowId);
      if (row?.editable) startEdit(row, selectedCell.value.colIdx);
      break;
    default:
      // If it's a printable character (digit or letter), start editing with that character
      if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && /[0-9.]/.test(e.key)) {
        e.preventDefault();
        const allRowsForEdit = [...incomeRows.value, ...expenseRows.value, ...financingRows.value, ...summaryRows.value];
        const rowForEdit = allRowsForEdit.find(r => r.id === selectedCell.value.rowId);
        if (rowForEdit?.editable) {
          startEdit(rowForEdit, selectedCell.value.colIdx);
          // Replace value with typed character
          nextTick(() => {
            editValue.value = e.key;
          });
        }
      }
      break;
  }
}

function finishEditAndMove(direction) {
  finishEdit();
  moveSelection(direction);
}

function handleInputArrow(e, direction) {
  const input = e.target;
  const cursorPos = input.selectionStart;
  const valueLength = editValue.value.length;
  
  // Right arrow at end of value → commit and move right
  if (direction === 'right' && cursorPos === valueLength) {
    e.preventDefault();
    finishEditAndMove('right');
    return;
  }
  
  // Left arrow at start of value → commit and move left
  if (direction === 'left' && cursorPos === 0) {
    e.preventDefault();
    finishEditAndMove('left');
    return;
  }
  
  // Otherwise, let default cursor movement happen
}

function showContextMenu(e, row, colIdx) {
  if (row.isCalculated) return;
  
  contextMenu.visible = true;
  contextMenu.x = e.clientX;
  contextMenu.y = e.clientY;
  contextMenu.rowId = row.id;
  contextMenu.colIdx = colIdx;
}

function closeContextMenu() {
  contextMenu.visible = false;
}

function handleContextAction(action) {
  const { rowId, colIdx } = contextMenu;
  
  switch (action) {
    case 'edit':
      const row = [...incomeRows.value, ...expenseRows.value, ...financingRows.value].find(r => r.id === rowId);
      if (row) startEdit(row, colIdx);
      break;
    case 'copyLeft':
      copyValue(rowId, colIdx, 'left');
      break;
    case 'copyRight':
      copyValue(rowId, colIdx, 'right');
      break;
    case 'reset':
      resetCell(rowId, colIdx);
      break;
    case 'unlockRow':
      unlockRow(rowId);
      break;
  }
  
  closeContextMenu();
}

function hasRowLocks(rowId) {
  return Object.keys(overrides).some(key => key.startsWith(`${rowId}-`));
}

function unlockRow(rowId) {
  Object.keys(overrides).forEach(key => {
    if (key.startsWith(`${rowId}-`)) delete overrides[key];
  });
  emitChanges();
}

function copyValue(rowId, colIdx, direction) {
  const monthIdx = getMonthlyIndex(colIdx);
  if (monthIdx < 0) return;
  
  const key = `${rowId}-${monthIdx}`;
  const sourceValue = overrides[key] ?? calculateBaseValue(
    [...incomeRows.value, ...expenseRows.value, ...financingRows.value].find(r => r.id === rowId),
    Math.floor(monthIdx / 12),
    monthIdx % 12
  );
  
  const maxMonths = props.holdingPeriod * 12;
  
  if (direction === 'right') {
    for (let i = monthIdx + 1; i < maxMonths; i++) {
      overrides[`${rowId}-${i}`] = sourceValue;
    }
  } else {
    for (let i = 0; i < monthIdx; i++) {
      overrides[`${rowId}-${i}`] = sourceValue;
    }
  }
  
  emitChanges();
}

function resetCell(rowId, colIdx) {
  if (viewMode.value === 'years') {
    for (let m = 0; m < 12; m++) {
      delete overrides[`${rowId}-${colIdx * 12 + m}`];
    }
  } else {
    const monthIdx = getMonthlyIndex(colIdx);
    if (monthIdx >= 0) {
      delete overrides[`${rowId}-${monthIdx}`];
    }
  }
  emitChanges();
}

function unlockAll() {
  Object.keys(overrides).forEach(key => delete overrides[key]);
  emitChanges();
}

function addExpenseLine() {
  if (!newExpense.value.label) return;
  customExpenses.value.push({
    id: `expense-${Date.now()}`,
    label: newExpense.value.label,
    amount: newExpense.value.amount,
    growth: newExpense.value.growth || 3
  });
  newExpense.value = { label: '', amount: 0, growth: 3 };
  showAddExpenseModal.value = false;
}

function addRevenueLine() {
  if (!newRevenue.value.label) return;
  customRevenue.value.push({
    id: `revenue-${Date.now()}`,
    label: newRevenue.value.label,
    amount: newRevenue.value.amount,
    growth: newRevenue.value.growth || 2.5
  });
  newRevenue.value = { label: '', amount: 0, growth: 2.5 };
  showAddRevenueModal.value = false;
}

function addDebtLine() {
  if (!newDebt.value.label) return;
  customDebt.value.push({
    id: `debt-${Date.now()}`,
    label: newDebt.value.label,
    amount: newDebt.value.amount
  });
  newDebt.value = { label: '', amount: 0 };
  showAddDebtModal.value = false;
}

function deleteExpenseRow(id) {
  const idx = customExpenses.value.findIndex(e => e.id === id);
  if (idx >= 0) {
    customExpenses.value.splice(idx, 1);
    Object.keys(overrides).forEach(key => {
      if (key.startsWith(`${id}-`)) delete overrides[key];
    });
  }
}

function deleteRevenueRow(id) {
  const idx = customRevenue.value.findIndex(r => r.id === id);
  if (idx >= 0) {
    customRevenue.value.splice(idx, 1);
    Object.keys(overrides).forEach(key => {
      if (key.startsWith(`${id}-`)) delete overrides[key];
    });
  }
}

function deleteDebtRow(id) {
  const idx = customDebt.value.findIndex(d => d.id === id);
  if (idx >= 0) {
    customDebt.value.splice(idx, 1);
    Object.keys(overrides).forEach(key => {
      if (key.startsWith(`${id}-`)) delete overrides[key];
    });
  }
}

function formatValue(value, isExpense = false) {
  // Handle undefined, null, and NaN
  if (value === undefined || value === null || isNaN(value)) return '-';
  if (value === 0) return '-';
  
  const absValue = Math.abs(value);
  
  if (viewMode.value === 'months') {
    if (absValue >= 1000000) return `${Math.round(absValue / 1000000)}m`;
    if (absValue >= 1000) return `${Math.round(absValue / 1000)}k`;
    return Math.round(absValue).toString();
  }
  
  const formatted = new Intl.NumberFormat('en-AU', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(absValue);
  
  return value < 0 ? `(${formatted})` : formatted;
}

function emitChanges() {
  emit('update:overrides', JSON.stringify(overrides));
}

// Debounced emit
let debounceTimer = null;
watch(overrides, () => {
  if (debounceTimer) clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => {
    emit('update:overrides', JSON.stringify(overrides));
  }, 1000);
}, { deep: true });
</script>

<style>
/* Styles for this component are in src/style.css under .detailed-model selector */
/* This avoids Vue scoped style issues with global CSS specificity */
.detailed-model-placeholder { display: none; }
</style>
