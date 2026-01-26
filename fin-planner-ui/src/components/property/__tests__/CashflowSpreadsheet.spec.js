import { mount } from '@vue/test-utils'
import { describe, it, expect, vi } from 'vitest'
import CashflowSpreadsheet from '../CashflowSpreadsheet.vue'

describe('CashflowSpreadsheet', () => {
  const mockDeal = {
    estimatedGrossRent: 120000,
    vacancyRatePercent: 5,
    rentalGrowthPercent: 3,
    vacancyGrowthPercent: 0,
    outgoingsEstimate: 20000,
    outgoingsGrowthPercent: 2,
    managementFeePercent: 6,
    managementGrowthPercent: 0,
    loanAmount: 800000,
    interestRatePercent: 5,
    capitalGrowthPercent: 4,
    askingPrice: 1000000,
    holdingPeriodYears: 10
  }

  const defaultProps = {
    deal: mockDeal,
    holdingPeriod: 10
  }

  it('renders correctly', () => {
    const wrapper = mount(CashflowSpreadsheet, {
      props: defaultProps
    })

    expect(wrapper.find('.cashflow-spreadsheet').exists()).toBe(true)
    expect(wrapper.find('.spreadsheet-title').text()).toBe('Cashflow Model')
  })

  it('calculates gross rent correctly for year 1', async () => {
    const wrapper = mount(CashflowSpreadsheet, {
      props: defaultProps
    })

    // Find the row for Gross Rent
    // The implementation renders rows dynamically. We need to look for text content.
    const rows = wrapper.findAll('tr')
    const grossRentRow = rows.find(r => r.text().includes('Gross Rent'))

    expect(grossRentRow).toBeDefined()

    // Year 1 column (index 1, as index 0 is label)
    // 120,000 / 12 * 12 = 120,000
    // The component formats currency, so we look for "120,000"
    const cells = grossRentRow.findAll('td')
    // Index 1 is Y1
    expect(cells[1].text()).toContain('120,000')
  })

  it('applies rental growth correctly', async () => {
    const wrapper = mount(CashflowSpreadsheet, {
      props: defaultProps
    })

    const rows = wrapper.findAll('tr')
    const grossRentRow = rows.find(r => r.text().includes('Gross Rent'))
    const cells = grossRentRow.findAll('td')

    // Year 1: 120,000
    // Year 2: 120,000 * 1.03 = 123,600
    expect(cells[2].text()).toContain('123,600')
  })

  it('toggles granularity to months', async () => {
    const wrapper = mount(CashflowSpreadsheet, {
      props: defaultProps
    })

    // Click "Months" button
    const buttons = wrapper.findAll('.toggle-btn')
    const monthsBtn = buttons.find(b => b.text() === 'Months')
    await monthsBtn.trigger('click')

    // Check headers - should see M1, M2...
    const headers = wrapper.findAll('th')
    expect(headers.map(h => h.text())).toContain('M1')

    // Gross Rent row should now show monthly values (10,000)
    const rows = wrapper.findAll('tr')
    const grossRentRow = rows.find(r => r.text().includes('Gross Rent'))
    const cells = grossRentRow.findAll('td')

    // First data cell (M1)
    expect(cells[1].text()).toContain('10,000')
  })

  it('handles expanding and collapsing rows', async () => {
    const wrapper = mount(CashflowSpreadsheet, {
      props: defaultProps
    })

    // "Outgoings" is expandable.
    // Initially expandedRows includes 'outgoings' in the component state (default behavior in code).
    // Let's verify children are visible.
    expect(wrapper.text()).toContain('Council Rates')

    // Find the expand button for Outgoings
    // We need to find the row that has "Outgoings" and click its button
    const rows = wrapper.findAll('tr')
    const outgoingsRow = rows.find(r => r.text().includes('Outgoings') && !r.text().includes('Council Rates')) // The parent row

    const expandBtn = outgoingsRow.find('button.expand-btn')
    await expandBtn.trigger('click') // Collapse

    // Now children should not be visible (unless v-if removed them from DOM)
    // The component uses v-if="row.isChild && expandedRows.includes(row.parentId)"
    expect(wrapper.text()).not.toContain('Council Rates')
  })

  it('updates when props change', async () => {
    const wrapper = mount(CashflowSpreadsheet, {
      props: defaultProps
    })

    const newDeal = { ...mockDeal, estimatedGrossRent: 240000 }
    await wrapper.setProps({ deal: newDeal })

    const rows = wrapper.findAll('tr')
    const grossRentRow = rows.find(r => r.text().includes('Gross Rent'))
    const cells = grossRentRow.findAll('td')

    expect(cells[1].text()).toContain('240,000')
  })
})
