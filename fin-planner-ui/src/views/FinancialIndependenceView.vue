<template>
  <div class="fi-view">
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Parameters Column -->
      <div class="card p-6">
        <h2 class="text-xl font-bold mb-6 flex items-center gap-2">
          <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>
          Retirement Params
        </h2>
        
        <div class="space-y-6">
          <div class="form-group">
            <label class="label flex justify-between">
              Retirement Age
              <span class="text-accent font-bold">{{ params.retirementAge }}</span>
            </label>
            <input type="range" v-model.number="params.retirementAge" min="30" max="80" class="slider" />
          </div>

          <div class="form-group">
            <label class="label flex justify-between">
              Target Annual Income
              <span class="text-accent font-bold">${{ formatCurrency(params.targetIncome) }}</span>
            </label>
            <input type="range" v-model.number="params.targetIncome" min="20000" max="300000" step="5000" class="slider" />
          </div>

          <div class="form-group">
            <label class="label flex justify-between">
              Initial Balance
              <span class="text-accent font-bold">${{ formatCurrency(params.initialBalance) }}</span>
            </label>
            <input type="number" v-model.number="params.initialBalance" class="input" />
          </div>

          <div class="form-group">
            <label class="label flex justify-between">
              Annual Contribution
              <span class="text-accent font-bold">${{ formatCurrency(params.annualContribution) }}</span>
            </label>
            <input type="number" v-model.number="params.annualContribution" class="input" />
          </div>

          <div class="divider"></div>

          <div class="form-group">
            <label class="label">Expected Return (%)</label>
            <input type="number" v-model.number="params.expectedReturn" step="0.1" class="input" />
          </div>

          <div class="form-group">
            <label class="label">Volatility (%)</label>
            <input type="number" v-model.number="params.volatility" step="0.1" class="input" />
          </div>

          <button @click="runSimulation" class="btn btn-primary w-full py-3 mt-4" :disabled="loading">
            <span v-if="loading">Simulating...</span>
            <span v-else>Run Simulation</span>
          </button>
        </div>
      </div>

      <!-- Results Column -->
      <div class="lg:col-span-2 space-y-6">
        <!-- Tabs -->
        <div class="flex gap-2">
          <button @click="activeTab = 'simulator'" 
                  :class="['px-6 py-2 rounded-lg font-bold transition-all', activeTab === 'simulator' ? 'bg-accent text-white shadow-accent' : 'bg-elevated text-muted hover:text-primary']">
            Simulator
          </button>
          <button @click="activeTab = 'advisor'" 
                  :class="['px-6 py-2 rounded-lg font-bold transition-all', activeTab === 'advisor' ? 'bg-accent text-white shadow-accent' : 'bg-elevated text-muted hover:text-primary']">
            AI Advisor
          </button>
        </div>

        <div v-if="activeTab === 'simulator'" class="space-y-6">
          <!-- Probability Gauge & Summary -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div class="card p-6 flex flex-col items-center justify-center text-center">
              <h3 class="text-sm font-medium text-muted mb-4 uppercase tracking-wider">Success Probability</h3>
              <div class="relative w-48 h-48 mb-4">
                <svg viewBox="0 0 100 100" class="w-full h-full transform -rotate-90">
                  <circle cx="50" cy="50" r="45" fill="none" stroke="var(--color-bg-elevated)" stroke-width="8" />
                  <circle cx="50" cy="50" r="45" fill="none" :stroke="probabilityColor" stroke-width="8" 
                          stroke-dasharray="283" :stroke-dashoffset="283 - (283 * successProbability)" 
                          stroke-linecap="round" class="transition-all duration-1000 ease-out" />
                </svg>
                <div class="absolute inset-0 flex flex-col items-center justify-center">
                  <span class="text-4xl font-bold">{{ (successProbability * 100).toFixed(0) }}%</span>
                  <span class="text-xs text-muted uppercase">Success Rate</span>
                </div>
              </div>
              <p class="text-sm text-muted max-w-xs">
                Based on {{ iterations }} simulations of market volatility.
              </p>
            </div>

            <div class="card p-6 grid grid-cols-1 gap-4">
              <div class="p-4 bg-elevated rounded-lg">
                <span class="text-xs text-muted uppercase">Median Final Outcome</span>
                <div class="text-2xl font-bold text-accent">${{ formatCurrency(medianOutcome / 1000000) }}M</div>
              </div>
              <div class="grid grid-cols-2 gap-4">
                <div class="p-4 bg-elevated rounded-lg">
                  <span class="text-xs text-muted uppercase">P10 (Worst)</span>
                  <div class="text-xl font-bold text-danger">${{ formatCurrency(p10Outcome / 1000000) }}M</div>
                </div>
                <div class="p-4 bg-elevated rounded-lg">
                  <span class="text-xs text-muted uppercase">P90 (Best)</span>
                  <div class="text-xl font-bold text-success">${{ formatCurrency(p90Outcome / 1000000) }}M</div>
                </div>
              </div>
              <div class="grid grid-cols-2 gap-4 border-t border-border pt-4 mt-2">
                <div class="flex flex-col">
                  <span class="text-[10px] text-muted uppercase">Coast FI Target</span>
                  <div class="text-sm font-bold">${{ formatCurrency(coastFITarget) }}</div>
                </div>
                <div class="flex flex-col">
                  <span class="text-[10px] text-muted uppercase">FI Number</span>
                  <div class="text-sm font-bold">${{ formatCurrency(fireNumber) }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Main Chart -->
          <div class="card p-6">
            <div class="flex justify-between items-center mb-6">
              <h3 class="text-lg font-bold">Wealth Projection</h3>
              <div class="flex gap-4 text-xs">
                <span class="flex items-center gap-1"><span class="w-3 h-3 bg-accent rounded-full"></span> Median</span>
                <span class="flex items-center gap-1"><span class="w-3 h-3 bg-danger opacity-50 rounded-full"></span> P10</span>
                <span class="flex items-center gap-1"><span class="w-3 h-3 bg-success opacity-50 rounded-full"></span> P90</span>
              </div>
            </div>
            <div class="h-80">
              <apexchart v-if="chartData.length" type="line" height="100%" :options="chartOptions" :series="chartSeries"></apexchart>
              <div v-else class="h-full flex items-center justify-center text-muted italic">
                Run simulation to see results
              </div>
            </div>
          </div>
        </div>

        <MoneyFlow class="mt-6" />

        <!-- AI Advisor Tab -->
        <div v-if="activeTab === 'advisor'" class="card h-[600px] flex flex-col overflow-hidden">
          <div class="p-4 border-b border-border bg-muted flex items-center gap-3">
            <div class="w-10 h-10 bg-accent rounded-full flex items-center justify-center text-white">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
            </div>
            <div>
              <div class="font-bold">Gemini Advisor</div>
              <div class="text-xs text-muted">Powered by Google Gemini</div>
            </div>
          </div>
          
          <div class="flex-1 overflow-y-auto p-6 space-y-4" ref="chatContainer">
            <div v-for="(msg, idx) in chatHistory" :key="idx" 
                 :class="['flex', msg.role === 'user' ? 'justify-end' : 'justify-start']">
              <div :class="['max-w-[80%] p-4 rounded-2xl', msg.role === 'user' ? 'bg-accent text-white rounded-tr-none' : 'bg-elevated text-primary rounded-tl-none']">
                <div class="text-sm whitespace-pre-wrap">{{ msg.text }}</div>
              </div>
            </div>
            <div v-if="chatLoading" class="flex justify-start">
              <div class="bg-elevated p-4 rounded-2xl rounded-tl-none flex gap-1">
                <span class="w-2 h-2 bg-muted rounded-full animate-bounce"></span>
                <span class="w-2 h-2 bg-muted rounded-full animate-bounce delay-100"></span>
                <span class="w-2 h-2 bg-muted rounded-full animate-bounce delay-200"></span>
              </div>
            </div>
          </div>

          <div class="p-4 border-t border-border bg-muted flex gap-2">
            <input type="text" v-model="userInput" @keyup.enter="sendMessage" 
                   placeholder="Ask about your retirement strategy..." 
                   class="flex-1 input" :disabled="chatLoading" />
            <button @click="sendMessage" class="btn btn-primary px-6" :disabled="chatLoading">
               <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="22" y1="2" x2="11" y2="13"/><polyline points="22 2 15 22 11 13 2 9 22 2"/></svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue';
import api from '../services/api';
import { usePortfolioStore } from '../stores/portfolio';
import MoneyFlow from '../components/MoneyFlow.vue';

const portfolioStore = usePortfolioStore();
const loading = ref(false);
const activeTab = ref('simulator');
const iterations = 5000;

// Chat State
const userInput = ref('');
const chatLoading = ref(false);
const chatHistory = ref([
  { role: 'ai', text: 'Hello! I am your AI Financial Advisor. How can I help you with your retirement planning today?' }
]);
const chatContainer = ref(null);

const params = reactive({
  retirementAge: 65,
  targetIncome: 80000,
  initialBalance: 500000,
  annualContribution: 30000,
  expectedReturn: 7.0,
  volatility: 15.0,
  inflationRate: 2.5,
  years: 40,
  includeAgePension: true
});

const successProbability = ref(0);
const medianOutcome = ref(0);
const p10Outcome = ref(0);
const p90Outcome = ref(0);
const chartData = ref([]);

const probabilityColor = computed(() => {
  if (successProbability.value > 0.8) return '#10b981'; // Success
  if (successProbability.value > 0.6) return '#f59e0b'; // Warning
  return '#ef4444'; // Danger
});

const formatCurrency = (val) => {
  if (val === undefined || val === null) return '0';
  return val.toLocaleString(undefined, { maximumFractionDigits: 0 });
};

const fireNumber = computed(() => params.targetIncome * 25);
const coastFITarget = computed(() => {
  const yearsToRetire = params.retirementAge - 45; // Assume age 45
  if (yearsToRetire <= 0) return params.initialBalance;
  return fireNumber.value / Math.pow(1 + params.expectedReturn / 100, yearsToRetire);
});

const runSimulation = async () => {
  loading.value = true;
  try {
    const response = await api.post('/Retirement/simulate', {}, {
      params: { scenarioId: '00000000-0000-0000-0000-000000000000' } // Placeholder for actual scenario ID
    }).catch(() => {
      // Fallback to direct stress test if scenario doesn't exist yet
      return api.post('/StressTest/simulate', {
        iterations,
        years: params.years,
        initialBalance: params.initialBalance,
        annualContribution: params.annualContribution,
        expectedReturn: params.expectedReturn / 100,
        volatility: params.volatility / 100,
        inflationRate: params.inflationRate / 100,
        targetIncome: params.targetIncome,
        retirementYear: params.retirementAge - 45,
        includeAgePension: params.includeAgePension
      }, {
        params: { portfolioId: portfolioStore.currentPortfolio?.id }
      });
    });

    const result = response.data;
    successProbability.value = result.successProbability;
    medianOutcome.value = result.medianOutcome;
    p10Outcome.value = result.worstCaseOutcome;
    p90Outcome.value = result.bestCaseOutcome;
    
    chartSeries.value = [
      { name: 'Median', data: result.medianPath },
      { name: 'P10 (Worst)', data: result.p10Path },
      { name: 'P90 (Best)', data: result.p90Path }
    ];
    chartData.value = result.medianPath;
  } catch (error) {
    console.error('Simulation failed', error);
  } finally {
    loading.value = false;
  }
};

const sendMessage = async () => {
  if (!userInput.value.trim() || chatLoading.value) return;

  const msg = userInput.value;
  userInput.value = '';
  chatHistory.value.push({ role: 'user', text: msg });
  chatLoading.value = true;
  
  scrollToBottom();

  try {
    const context = `
      User is targetting retirement at age ${params.retirementAge}.
      Target annual income: $${params.targetIncome}.
      Current initial balance: $${params.initialBalance}.
      Annual contribution: $${params.annualContribution}.
      Expected market return: ${params.expectedReturn}%.
      Current Success Probability: ${(successProbability.value * 100).toFixed(0)}%.
    `;

    const response = await api.post('/AI/chat', {
      message: msg,
      context: context
    });

    chatHistory.value.push({ role: 'ai', text: response.data.response });
  } catch (error) {
    chatHistory.value.push({ role: 'ai', text: 'Sorry, I encountered an error processing your request.' });
  } finally {
    chatLoading.value = false;
    scrollToBottom();
  }
};

const scrollToBottom = () => {
  setTimeout(() => {
    if (chatContainer.value) {
      chatContainer.value.scrollTop = chatContainer.value.scrollHeight;
    }
  }, 100);
};

const chartSeries = ref([]);

const chartOptions = computed(() => ({
  chart: {
    type: 'line',
    toolbar: { show: false },
    animations: { enabled: true },
    background: 'transparent'
  },
  stroke: {
    curve: 'smooth',
    width: [3, 2, 2],
    dashArray: [0, 4, 4]
  },
  colors: ['#3b82f6', '#ef4444', '#10b981'],
  xaxis: {
    title: { text: 'Years' },
    labels: { style: { colors: 'var(--color-text-muted)' } }
  },
  yaxis: {
    labels: {
      formatter: (val) => '$' + (val / 1000).toFixed(0) + 'k',
      style: { colors: 'var(--color-text-muted)' }
    }
  },
  grid: {
    borderColor: 'var(--color-border-subtle)',
    xaxis: { lines: { show: false } }
  },
  legend: { show: false },
  tooltip: {
    theme: 'dark',
    y: {
      formatter: (val) => '$' + val.toLocaleString()
    }
  }
}));

onMounted(() => {
  if (portfolioStore.currentPortfolio) {
    params.initialBalance = portfolioStore.currentPortfolioValue || 500000;
  }
});
</script>

<style scoped>
.fi-view {
  animation: fadeIn 0.5s ease-out;
}

.card {
  background: var(--glass-bg);
  backdrop-filter: blur(var(--glass-blur));
  -webkit-backdrop-filter: blur(var(--glass-blur));
  border: 1px solid var(--glass-border);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-glass);
}

.label {
  font-size: var(--font-size-sm);
  font-weight: 500;
  color: var(--color-text-secondary);
  display: block;
  margin-bottom: var(--spacing-xs);
}

.input {
  width: 100%;
  padding: 10px 14px;
  background: var(--color-bg-elevated);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-primary);
  font-size: var(--font-size-base);
  transition: all var(--transition-fast);
}

.input:focus {
  border-color: var(--color-accent);
  box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.2);
  outline: none;
}

.slider {
  width: 100%;
  height: 6px;
  background: var(--color-bg-elevated);
  border-radius: 3px;
  appearance: none;
  outline: none;
}

.slider::-webkit-slider-thumb {
  appearance: none;
  width: 18px;
  height: 18px;
  background: var(--color-accent);
  border-radius: 50%;
  cursor: pointer;
  box-shadow: var(--shadow-sm);
  transition: transform 0.1s;
}

.slider::-webkit-slider-thumb:hover {
  transform: scale(1.2);
}

.divider {
  height: 1px;
  background: var(--color-border-subtle);
  margin: var(--spacing-md) 0;
}

.bg-elevated {
  background: var(--color-bg-elevated);
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
