
import { runSimulation } from './src/services/monteCarloEngine.js';

if (typeof performance === 'undefined') {
    global.performance = { now: () => Date.now() };
}

const inputs = {
    askingPrice: 1000000,
    estimatedGrossRent: 50000,
    rentVariancePercent: 10,
    vacancyRatePercent: 5,
    vacancyVariancePercent: 5,
    outgoingsEstimate: 10000,
    // holdingPeriodYears: 10, // Default 10
    capitalGrowthPercent: 3,
    capitalGrowthVariancePercent: 5,
    discountRate: 8,
    stampDutyRate: 5,
    legalCosts: 2000,
    loanAmount: 800000,
    interestRatePercent: 6,
    interestVariancePercent: 1,
    distributions: {
        rent: { type: 'normal', variancePercent: 10 },
        vacancy: { type: 'normal', variancePercent: 5 },
        growth: { type: 'normal', variancePercent: 5 },
        interest: { type: 'normal', variancePercent: 1 }
    }
};

console.log("--- GROSS MODE (Intrinsic Value) ---");
const resultGross = runSimulation(inputs, 1000, false); // includeAcquisitionCost = false
console.log("Median Gross PV:", resultGross.medianNPV);
console.log("P10 Gross PV:", resultGross.p10NPV);
console.log("P90 Gross PV:", resultGross.p90NPV);
console.log("Curve 0%:", resultGross.probabilityCurve[0].value);
console.log("Curve 100%:", resultGross.probabilityCurve[100].value);

console.log("\n--- NET MODE (Profit) ---");
const resultNet = runSimulation(inputs, 1000, true); // includeAcquisitionCost = true
console.log("Median NPV:", resultNet.medianNPV);
console.log("P10 NPV:", resultNet.p10NPV);
console.log("P90 NPV:", resultNet.p90NPV);
console.log("Curve 0%:", resultNet.probabilityCurve[0].value);
console.log("Curve 100%:", resultNet.probabilityCurve[100].value);

// Check if Net makes sense relative to Gross
// Net should be approx Gross - EquityRequired
// EquityRequired = Price + Costs - Loan
const stampDuty = inputs.askingPrice * (inputs.stampDutyRate / 100);
const totalCost = inputs.askingPrice + stampDuty + inputs.legalCosts;
const equity = totalCost - inputs.loanAmount;

console.log("\nEst. Equity Required:", equity);
console.log("Difference (Median Gross - Median Net):", resultGross.medianNPV - resultNet.medianNPV);

