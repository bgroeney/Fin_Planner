# Fin_Planner - Project Scope

A comprehensive financial planning application for portfolio tracking, performance analysis, AI-powered recommendations, tax optimization, and wealth management.

**Stack:** C# ASP.NET Core (.NET 10) | Vue 3 + Vite | PostgreSQL | GCP Cloud Run | Gemini AI

---

## Phase 1: Core Platform (✅ Implemented)

### 1.1 Authentication & Multi-User Support
- [x] JWT-based authentication with login/register
- [x] Role-based permissions (Admin/Editor)
- [x] Protected API routes and frontend navigation guards
- [x] User session management

### 1.2 Portfolio Management
- [x] Multiple portfolios per user
- [x] Portfolio CRUD operations with target allocations
- [x] Account-based structure (e.g., Netwealth Wrap, Bank Accounts)
- [x] Holdings tracking per account with units, cost base, current value
- [x] User-defined asset categories with target percentages
- [x] Default category templates (AU Large Cap, International, Fixed Interest, Cash, etc.)

### 1.3 Transaction & Holding Tracking
- [x] Transaction types: Buy, Sell, Deposit, Withdrawal, Dividend, Fee
- [x] Transaction history per asset/account
- [x] Cached holding calculations (units, average cost)
- [x] Attached order tracking for transaction sequencing

### 1.4 Price System
- [x] Multi-source price fetching (Yahoo Finance, Morningstar AU, Alpha Vantage, Polygon.io)
- [x] Configurable price source priority with fallback
- [x] Per-asset price source overrides
- [x] Historical price storage for performance tracking
- [x] Imported file prices from CSV valuations
- [x] Admin panel for managing price sources and API keys

### 1.5 CSV Import
- [x] Netwealth Portfolio Valuation import
- [x] Netwealth Transaction Listing import
- [x] Column preview and category mapping UI
- [x] Automatic asset detection and creation
- [x] Imported price extraction from valuation files

### 1.6 AI-Powered Recommendations (Gemini)
- [x] Portfolio analysis and rebalancing recommendations
- [x] Individual holding analysis (Buy/Sell/Hold)
- [x] AI recommendations with confidence scores
- [x] Consider CGT implications and franking credits
- [x] Decision tracking workflow (Draft → Pending → Approved/Rejected → Implemented)
- [x] Decision history logging with status changes

### 1.7 Reporting
- [x] Performance charts (1Y, YTD, 5Y, ALL ranges)
- [x] Performance history snapshots with rebuild capability
- [x] Tax summary (FY dividends, franking credits, realized gains)
- [x] Transaction ledger with filtering
- [x] Australian Financial Year support (July-June)

### 1.8 Tax Optimization
- [x] Tax parcel tracking with FIFO, MinTax, MaxGain strategies
- [x] CGT discount calculation (50% for holdings > 12 months)
- [x] Taxable gain estimation for sales

### 1.9 Commercial Property Module
- [x] Commercial property portfolio with purchase/valuation tracking
- [x] Lease profile management with tenant details
- [x] Rent review tracking and calculations
- [x] Property ledger for income/expense recording
- [x] Property valuation history
- [x] Dashboard with yield gauges, vacancy heatmaps, cashflow forecasts

### 1.10 Property Acquisition Planner
- [x] Deal workbench with full financial modeling
- [x] Lifecycle management (Draft → Analysis → Due Diligence → Negotiation → Completed/Passed)
- [x] Monte Carlo simulation engine (client-side, 5000 iterations)
- [x] Distribution types: Normal, Lognormal, Triangular, Uniform
- [x] Correlation matrix support for correlated variables
- [x] IRR, NPV, and Cash-on-Cash return calculations
- [x] Risk analysis with percentile charts (P10/P50/P90)
- [x] Value at Risk (VaR) visualization
- [x] Decision matrix for deal comparison
- [x] Document management per deal (PDF upload/preview)
- [x] Simulation result history

### 1.11 Dashboard & UI
- [x] Portfolio overview cards with total value and performance
- [x] Allocation charts (ApexCharts)
- [x] Asset detail drawer with transaction history
- [x] Decision management view with Kanban-style workflow
- [x] Custom MoneyBox loader component
- [x] Responsive design with dark mode aesthetics

### 1.12 Phase 1 Enhancements (Planned)
- [ ] **Portfolio Benchmarking**: Compare portfolio performance against ASX200, S&P500, or custom benchmarks
- [ ] **Dividend Calendar**: Track upcoming dividend dates and expected payments
- [ ] **Rebalancing Wizard**: Guided workflow to execute AI recommendations with transaction creation
- [ ] **Goal Tracking**: Set financial goals (e.g., "Save $100k for house deposit by 2027") and track progress
- [ ] **Mobile Responsive Improvements**: Ensure full functionality on mobile devices
- [ ] **Audit Log**: Track all changes to financial data for compliance
- [ ] **Data Export**: Full export of all data in CSV/JSON for portability
- [ ] **Multi-Factor Authentication**: Enhanced security for sensitive financial data

---

## Phase 2: Enhanced Tax & Entity Structures (🚧 Planned)

### 2.1 Person Accounts
Each Portfolio can contain **Person Accounts** representing individuals:

- [ ] **Person Account entity** with personal details
- [ ] **Super Accounts**
  - Superannuation fund details and balance tracking
  - Contribution history (employer, salary sacrifice, personal)
  - Preservation age and access conditions
  - Investment option allocations
- [ ] **Payroll Income tracking**
  - Taxable Income (Gross Salary)
  - Tax Withheld (PAYG)
  - Superannuation contributions
  - Employer and salary sacrifice details
- [ ] **Other Taxable Income**
  - Interest income
  - Rental income (from property module integration)
  - Dividend income (from portfolio integration)
  - Capital gains (from portfolio integration)
- [ ] **Deductions**
  - Work-related expenses
  - Investment expenses
  - Donations
  - Private health insurance
- [ ] **Tax offsets and credits** (Franking credits, LMITO, etc.)
- [ ] **Medicare levy** and surcharge calculations

### 2.2 Entity Account Types

#### Family Discretionary Trust
- [ ] **Trust Account entity** with trust details (ABN, Trustee)
- [ ] **Beneficiary management**
  - Link to Person Accounts and other entities (Bucket Companies)
  - Define eligible beneficiaries
- [ ] **Income and Capital Gain tracking**
  - Franked dividends
  - Unfranked income
  - Capital gains (discount and non-discount)
- [ ] **Distribution requirements** (must distribute all income by 30 June)
- [ ] **Distribution resolution recording**

#### Bucket Company
- [ ] **Company Account entity** with company details (ABN, ACN)
- [ ] **Retained profits tracking**
- [ ] **30% company tax rate** calculations
- [ ] **Franking account balance** management
- [ ] **Division 7A loan tracking** for shareholder loans

### 2.3 Tax Distribution Optimization Module

#### Trust Distribution Recommendation Engine
- [ ] **Automatic beneficiary tax analysis**
  - Calculate marginal tax rate for each beneficiary
  - Consider existing income sources
  - Model franking credit utilization
- [ ] **Distribution optimization algorithm**
  - Minimize family group total tax
  - Maximize franking credit utilization
  - Consider CGT discount eligibility
  - Avoid wastage (low/nil tax thresholds)
- [ ] **Distribution recommendation report**
  - Proposed allocation per beneficiary
  - Tax impact comparison (current vs. optimized)
  - Compliance warnings (streaming rules, present entitlement)
- [ ] **Multi-year planning**
  - Project optimal distributions for future years
  - Consider changing income expectations

### 2.4 Enhanced Cashflow Module
- [ ] **Sankey diagram visualization** of money flows (income → accounts → expenses → investments)
- [ ] **Consolidated family cashflow view** across all entities
- [ ] **Tax-impacted cashflow projections**
- [ ] **Loan repayment scheduling** (including Division 7A)
- [ ] **Dividend payment planning** from bucket companies

### 2.5 Improved Tax Position Analysis
- [ ] **Multi-entity tax summary**
  - Individual marginal rates
  - Trust distributions received
  - Company retentions
- [ ] **Effective tax rate calculations** per entity
- [ ] **What-if scenario modeling** for distributions
- [ ] **ATO lodgement reminders** and deadline tracking

### 2.6 Phase 2 Enhancements
- [ ] **Entity Relationship Diagram**: Visual representation of family/entity structures
- [ ] **Document Vault**: Secure storage for trust deeds, company constitution, tax returns
- [ ] **Beneficiary History**: Track all distributions to each beneficiary over time
- [ ] **Compliance Calendar**: Reminders for trust resolutions, BAS, tax lodgements
- [ ] **Collaboration Mode**: Family members can view/contribute to the same plan
- [ ] **Webhooks/Notifications**: Email or push notifications for important events
- [ ] **Localization**: Support for Australian states' stamp duty, land tax variations

---

## Phase 3: Financial Independence & Strategy (✅ Implemented)

### 3.1 Chance of Success (Monte Carlo for Wealth)
- [ ] **Retirement scenario modeling**
  - Define retirement age and target income
  - Model superannuation drawdowns
  - Account for Age Pension (Australian means testing)
- [ ] **Monte Carlo simulations** on whole-of-life cashflow
  - Market volatility scenarios
  - Inflation modeling
  - Longevity risk
- [ ] **Success probability gauge** (e.g., 85% chance of success)
- [ ] **Historical backtesting** on market data
- [ ] **Drill-down into individual simulation trials**
- [ ] **Risk tolerance configuration** (Conservative to Aggressive)
- [ ] **Black Swan Event Modeling**: Model low-probability high-impact events (job loss, disability, market crash)

### 3.2 Net Worth Tracking & Projection
- [ ] **Consolidated net worth calculation** (assets - liabilities)
- [ ] **Net worth over time** tracking with historical snapshots
- [ ] **Net worth projections** based on assumptions
- [ ] **Liability tracking** (mortgages, loans, credit cards)
- [ ] **Asset-liability matching** for retirement planning

### 3.3 Comprehensive Cashflow Visualization
- [ ] **Interactive Sankey diagrams** for yearly cashflow
- [ ] **Drill into specific years** to see flows
- [ ] **Model one-off events** (home purchase, inheritance, sabbatical)
- [ ] **Employment transitions** (part-time, self-employment, retirement)
- [ ] **Custom recurring events** (buy car every 5 years, major holiday every 2 years)

### 3.4 Tax Analytics Dashboard
- [ ] **Future tax liability estimation** by year
- [ ] **Effective tax bracket visualization**
- [ ] **Transition to Retirement (TTR) strategies**
- [ ] **Superannuation contribution optimization**
  - Concessional vs. non-concessional
  - Carry-forward unused contributions
  - Government co-contribution eligibility
- [ ] **CGT event planning** (timing sales for optimal tax outcomes)

### 3.5 Financial Independence Metrics
- [ ] **Time to Financial Independence (FI)** calculator
- [ ] **FIRE number** calculation based on expenses and withdrawal rate
- [ ] **Safe withdrawal rate analysis** (4% rule with Australian context)
- [ ] **Passive income vs. expenses** tracking
- [ ] **Coast FI** and **Barista FI** metrics

### 3.6 Life Event Modeling
- [ ] **Milestone events**
  - Home purchase/sale
  - Children (education, childcare)
  - Career changes
  - Inheritance
- [ ] **Expense phases** (higher in early retirement, healthcare in late retirement)
- [ ] **Social Security / Age Pension modeling** with means testing
- [ ] **Divorce/remarriage financial impact**

### 3.7 International & Couple Planning
- [ ] **Multi-currency support**
- [ ] **Couple planning** with joint and individual accounts
- [ ] **Different retirement ages** for partners
- [ ] **Survivor scenario modeling**

### 3.8 Custom Plots & Reports
- [ ] **Build custom charts** from any data points
- [ ] **Export financial plans** as PDF reports
- [ ] **Share plans** with advisors (read-only access)
- [ ] **Comparison views** (scenarios side-by-side)

### 3.9 Phase 3 Enhancements
- [ ] **AI Financial Advisor Chat**: Conversational interface to ask "What if I retire at 55?"
- [ ] **Insurance Needs Analysis**: Calculate appropriate life, income protection, TPD coverage
- [ ] **Performance Optimization**: Caching, lazy loading, and database indexing for scale

---

## Technical Requirements

### Functional Requirements
- Web-based architecture (C# / Vue / PostgreSQL)
- Multi-user support with role-based permissions
- Portfolio upload from CSV (Netwealth format)
- Reporting per Australian Financial Year (July-June)
- GCP Cloud Run deployment with Cloud SQL PostgreSQL

### Non-Functional Requirements
- Sub-second response times for dashboard loads
- Monte Carlo simulations < 1 second (5000 iterations client-side)
- 99.9% uptime target
- WCAG 2.1 AA accessibility compliance
- GDPR/Australian Privacy Principles compliance

---

*Last Updated: 25 January 2026*
