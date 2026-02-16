The intent of the Acquisitions page is to provide a tool to review potential commerical property deals and perform a detailed analysis of the asset to help inform a purchase decision. 


### Property Acquisition Planner Features
- [] Deal workbench with full financial modeling
- [] Lifecycle management (Draft → Analysis → Due Diligence → Negotiation → Completed/Passed)
- [] Monte Carlo simulation engine (client-side, 5000 iterations)
- [] Distribution types: Normal, Lognormal, Triangular, Uniform
- [] Correlation matrix support for correlated variables
- [] IRR, NPV, and Cash-on-Cash return calculations
- [] Risk analysis with percentile charts (P10/P50/P90)
- [] Value at Risk (VaR) visualization
- [] Decision matrix for deal comparison
- [] Document management per deal (PDF upload/preview)
- [] Simulation result history

**User flow**
A new property opportunity is identified and added to the system.
High level numbers are setup.
Rough sensitivity analysis is performed to assess whether asset is in a reasonable price point.
If assest is valued reasonably, proceed to deatiled model and adjust cashflow items appropriately.
Review Sensitivity analysis again to confirm the asset is still in a reasonable price point.
If so, proceed with making offer and performing due dilligence to purchase asset.
Once purchased add to property portfolio.


**Test cases**
A number of test cases should be constructed to test the core modelling logic.
These should cover core cashflow calcuations, variability input on parameters, Monte Carlo simulation, and the impact of variable correlation.


Outstanding issues
1) Test cases are insufficent to catch issues. Enable Playwright for UI testing.
2) The Detailed Model column alignment is incorrect and when switching to Months is complelety unreadable.
3) Unable to enter formulas into the Detailed Model to perform calculations such as Management Fee being a % of Gross Rent per year.
4) Sensitivity on variables does not save.
5) Cannot select a distribution for sensitivity.
6) Variable alignment is inconsistent so numebrs look messy on Assumptions page.
7) All sensitivities should be set on the Risk analysis screen.
8) Lifecycle management only allows forward progression - need to enable backwards progression through lifecycle.
9) Lifecycle history does not show - should open a modal showing changes over time in a vertical timeline by who and when and what.
10) The Risk analysis screen should show most parameters on a single page without the need to scroll vertically up and down.
11) Sensitivity parameters should be able to build in an underlying growth rate with variability on top. 
