import { mount } from '@vue/test-utils'
import { describe, it, expect, vi } from 'vitest'
import DealWorkbench from '../DealWorkbench.vue'

// Mock child components
vi.mock('../LifecycleStepper.vue', () => ({ default: { template: '<div>Stepper</div>' } }))
vi.mock('../PropertyRiskAnalyzer.vue', () => ({ default: { template: '<div>RiskAnalyzer</div>' } }))
vi.mock('../CashflowSpreadsheet.vue', () => ({ default: { template: '<div>CashflowSpreadsheet</div>' } }))
vi.mock('../DealDocuments.vue', () => ({ default: { template: '<div>DealDocuments</div>' } }))
vi.mock('../DealStatusHistory.vue', () => ({ default: { template: '<div>DealStatusHistory</div>' } }))

// Mock services
vi.mock('../../services/api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn()
  }
}))

vi.mock('../../services/monteCarloEngine', () => ({
  formatCurrency: (val) => `$${val}`,
  calculateNOI: () => 100000
}))

describe('DealWorkbench', () => {
  const mockDeal = {
    id: '123',
    name: 'Test Deal',
    status: 'Draft',
    askingPrice: 1000000,
    estimatedGrossRent: 50000,
    loanAmount: 800000,
    interestRatePercent: 5,
    holdingPeriodYears: 10
  }

  it('renders loading state when deal is missing', () => {
    const wrapper = mount(DealWorkbench, {
      props: {
        deal: null
      }
    })

    expect(wrapper.text()).toContain('Loading deal...')
  })

  it('renders deal info when deal is provided', () => {
    const wrapper = mount(DealWorkbench, {
      props: {
        deal: mockDeal
      }
    })

    expect(wrapper.text()).toContain('Test Deal')
    expect(wrapper.text()).toContain('Assumptions & Inputs')
  })

  it('switches tabs correctly', async () => {
    const wrapper = mount(DealWorkbench, {
      props: {
        deal: mockDeal
      }
    })

    // Default view is summary
    expect(wrapper.find('.workbench-grid-container').exists()).toBe(true)

    // Click Detailed Model tab
    const tabs = wrapper.findAll('.view-tab')
    const detailedTab = tabs.find(t => t.text().includes('Detailed Model'))
    await detailedTab.trigger('click')

    // Expect CashflowSpreadsheet to be rendered (mocked)
    expect(wrapper.text()).toContain('CashflowSpreadsheet')
  })
})
