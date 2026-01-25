/**
 * Monte Carlo Simulation Engine for Property Deal Analysis
 * Runs client-side for performance (5000 iterations < 1 second)
 */

const Distributions = {
    NORMAL: 'normal',
    LOGNORMAL: 'lognormal',
    TRIANGULAR: 'triangular',
    UNIFORM: 'uniform'
};

/**
 * Box-Muller transform for generating normally distributed random numbers
 */
function randomNormal(mean = 0, stdDev = 1) {
    const u1 = Math.random();
    const u2 = Math.random();
    const z = Math.sqrt(-2 * Math.log(u1)) * Math.cos(2 * Math.PI * u2);
    return mean + z * stdDev;
}

/**
 * Generate Log-Normal Distributed Variable
 * @param {number} mu - Mean of the underlying normal distribution
 * @param {number} sigma - StdDev of the underlying normal distribution
 */
function randomLognormal(mu, sigma) {
    const normal = randomNormal(mu, sigma);
    return Math.exp(normal);
}

/**
 * Generate Triangular Distributed Variable
 * @param {number} min - Minimum value
 * @param {number} max - Maximum value
 * @param {number} mode - Peak/Most likely value
 */
function randomTriangular(min, max, mode) {
    const u = Math.random();
    const f = (mode - min) / (max - min);

    if (u <= f) {
        return min + Math.sqrt(u * (max - min) * (mode - min));
    } else {
        return max - Math.sqrt((1 - u) * (max - min) * (max - mode));
    }
}

/**
 * Generate Uniform Distributed Variable (Flat)
 * @param {number} min 
 * @param {number} max 
 */
function randomUniform(min, max) {
    return min + Math.random() * (max - min);
}

/**
 * Cholesky Decomposition for Correlation Matrix
 * Returns Lower Triangular Matrix (L) such that L * L^T = CorrelationMatrix
 */
function choleskyDecomposition(matrix) {
    const n = matrix.length;
    const lower = Array(n).fill(0).map(() => Array(n).fill(0));

    for (let i = 0; i < n; i++) {
        for (let j = 0; j <= i; j++) {
            let sum = 0;
            for (let k = 0; k < j; k++) {
                sum += lower[i][k] * lower[j][k];
            }

            if (i === j) {
                lower[i][j] = Math.sqrt(Math.max(0, matrix[i][i] - sum)); // Max(0) handles partial correlations > 1 floating errors
            } else {
                lower[i][j] = (matrix[i][j] - sum) / lower[j][j];
            }
        }
    }
    return lower;
}

/**
 * Generate correlated standard normal variables
 * @param {Array<number>} uncorrelated - Array of independent standard normal samples
 * @param {Array<Array<number>>} lowerTriangular - Cholesky decomposed correlation matrix (L)
 */
function applyCorrelation(uncorrelated, lowerTriangular) {
    const n = uncorrelated.length;
    const correlated = new Array(n).fill(0);

    for (let i = 0; i < n; i++) {
        for (let j = 0; j <= i; j++) {
            correlated[i] += lowerTriangular[i][j] * uncorrelated[j];
        }
    }
    return correlated;
}

/**
 * Sample Generator helper
 * Handles type dispatch
 */
function sampleDistribution(type, params) {
    // Params format depends on distribution
    // For now we map legacy "variancePercent" to params dynamically in runtime loop
    // But this function expects standardized params if possible.

    // Normal: { mean, stdDev }
    // Lognormal: { mean, stdDev } (of underlying normal)
    // Triangular: { min, max, mode }
    // Uniform: { min, max }

    switch (type) {
        case Distributions.LOGNORMAL:
            // We assume user supplies arithmetic mean/variance requirement, 
            // we convert to mu/sigma for underlying normal.
            // Or user supplies direct params. 
            // Let's assume params are { mu, sigma } for lognormal for now.
            return randomLognormal(params.mu, params.sigma);
        case Distributions.TRIANGULAR:
            return randomTriangular(params.min, params.max, params.mode);
        case Distributions.UNIFORM:
            return randomUniform(params.min, params.max);
        case Distributions.NORMAL:
        default:
            return randomNormal(params.mean, params.stdDev);
    }
}

/**
 * Generate a random value with variance (normal distribution) - Legacy/Default Wrapper
 * @param {number} baseValue - The central/expected value
 * @param {number} variancePercent - Variance as percentage (e.g., 10 = ±10%)
 */
function randomWithVariance(baseValue, variancePercent) {
    const stdDev = baseValue * (variancePercent / 100) / 2; // 2 std devs = 95% within range
    return randomNormal(baseValue, stdDev);
}

/**
 * Calculate Net Operating Income (NOI)
 */
export function calculateNOI(grossRent, vacancyRate, managementFee, outgoings) {
    const effectiveIncome = grossRent * (1 - vacancyRate / 100);
    const managementCost = effectiveIncome * (managementFee / 100);
    return effectiveIncome - managementCost - outgoings;
}

/**
 * Calculate annual debt service (interest-only simplified)
 */
function calculateDebtService(loanAmount, interestRate) {
    return loanAmount * (interestRate / 100);
}

/**
 * Calculate NPV of cashflows
 */


/**
 * Calculate IRR using Newton-Raphson method
 */
export function calculateIRR(cashflows, initialInvestment, maxIterations = 100) {
    let rate = 0.1; // Initial guess 10%

    for (let i = 0; i < maxIterations; i++) {
        let npv = -initialInvestment;
        let dnpv = 0;

        for (let t = 0; t < cashflows.length; t++) {
            const factor = Math.pow(1 + rate, t + 1);
            npv += cashflows[t] / factor;
            dnpv -= (t + 1) * cashflows[t] / Math.pow(1 + rate, t + 2);
        }

        if (Math.abs(npv) < 0.01) break; // Converged

        rate = rate - npv / dnpv;

        // Bounds check
        if (rate < -0.99) rate = -0.99;
        if (rate > 1) rate = 1;
    }

    return rate * 100; // Return as percentage
}

/**
 * Run a single simulation iteration
 */
export function runSingleIteration(inputs) {
    const {
        askingPrice,
        stampDutyRate,
        legalCosts,
        capExReserve,
        estimatedGrossRent,
        vacancyRatePercent,
        managementFeePercent,
        outgoingsEstimate,
        loanAmount,
        interestRatePercent,
        capitalGrowthPercent,
        holdingPeriodYears,
        rentVariancePercent,
        vacancyVariancePercent,
        interestVariancePercent,
        capitalGrowthVariancePercent,
        discountRate = 8, // Default 8% discount rate
        timeVarianceEarly = 0, // Months
        timeVarianceLate = 0   // Months
    } = inputs;

    // Calculate total acquisition cost
    const stampDuty = askingPrice * (stampDutyRate / 100);
    const totalAcquisitionCost = askingPrice + stampDuty + legalCosts + capExReserve;
    const equityRequired = totalAcquisitionCost - loanAmount;

    // Generate Correlated Random Variables if Matrix exists
    const variables = ['rent', 'vacancy', 'interest', 'growth']; // Standard Order
    // Standard Normal Samples (uncorrelated)
    const zScores = variables.map(() => randomNormal(0, 1));

    let correlatedZ = zScores;

    // Apply Correlation if Matrix Provided
    if (inputs.correlationMatrix && inputs.correlationMatrix.length === variables.length) {
        // Optimization: Cholesky L can be pre-calculated outside the loop if matrix is static!
        // For 5000 iterations, we should pre-calc.
        // We'll trust the caller passes 'choleskyL' if optimized, or we calculate strictly here (slow).
        // Let's assume inputs has `precalculatedCholesky` to save time.
        const L = inputs.precalculatedCholesky || choleskyDecomposition(inputs.correlationMatrix);
        correlatedZ = applyCorrelation(zScores, L);
    }

    // Helper to extract Z for a specific variable
    const getZ = (name) => correlatedZ[variables.indexOf(name)];

    // 1. Rent (Normal or Lognormal usually, supporting others)
    // Legacy: randomWithVariance(estimatedGrossRent, rentVariancePercent)
    // New: Use Distribution Settings if available, else Fallback.
    const rentDist = inputs.distributions?.rent || { type: Distributions.NORMAL, mean: estimatedGrossRent, stdDev: estimatedGrossRent * (rentVariancePercent / 100) / 2 };

    // If we are using correlated Z, we must transform Z back to Distribution.
    // For Normal: mean + Z * stdDev
    // For Lognormal: exp(mu + Z * sigma)
    // For others (Triangular/Uniform): We need CDF inversion (Inverse Transform Sampling) from Z (via CDF of Normal -> U -> InverseCDF of Target).
    // Simplifying: We only support Correlation for Normal/Lognormal effectively with this Gaussian Copula approach.
    // For Triangular/Uniform, we use the percentile implied by Z (Phi(Z)) to sample.

    const mapZToValue = (z, dist) => {
        if (!dist) return 0;

        // Convert Z (Standard Normal) to Probability U [0,1]
        // Approx Error Function or use a library? 
        // Simple approx for probability:
        // Or simpler: Just support Normal/Lognormal correlation direct mapping.

        switch (dist.type) {
            case Distributions.LOGNORMAL:
                // dist params should be underlying normal mu/sigma
                // If user passes arithmetic Mean/StdDev, we need conversion.
                // Assuming params are mu/sigma:
                return Math.exp(dist.mu + z * dist.sigma);

            case Distributions.NORMAL:
                return dist.mean + z * dist.stdDev;

            case Distributions.UNIFORM:
                // Prob U approximation from Z
                // let u = 0.5 * (1 + math.erf(z / Math.sqrt(2))); // Need erf
                // Simple fallback: Ignore correlation for Uniform/Triangular mixed with Normal in this version 
                // OR generate independent if correlation not essential for these edge cases.
                // Current MVP: Ignore correlation for Uniform/Triangular, just independent sample.
                return sampleDistribution(dist.type, dist);

            case Distributions.TRIANGULAR:
                return sampleDistribution(dist.type, dist);

            default:
                return dist.mean + z * dist.stdDev;
        }
    };

    // Construct Distribution Configs on the fly for legacy inputs if not provided
    // This maintains backward compatibility
    const rentConfig = inputs.distributions?.rent || {
        type: Distributions.NORMAL,
        mean: estimatedGrossRent,
        stdDev: estimatedGrossRent * (rentVariancePercent / 100) / 2
    };
    const vacancyConfig = inputs.distributions?.vacancy || {
        type: Distributions.NORMAL,
        mean: vacancyRatePercent,
        stdDev: vacancyRatePercent * (vacancyVariancePercent / 100) / 2
    };
    const interestConfig = inputs.distributions?.interest || {
        type: Distributions.NORMAL,
        mean: interestRatePercent,
        stdDev: interestRatePercent * (interestVariancePercent / 100) / 2
    };
    const growthConfig = inputs.distributions?.growth || {
        type: Distributions.NORMAL,
        mean: capitalGrowthPercent,
        stdDev: capitalGrowthPercent * (capitalGrowthVariancePercent / 100) / 2
    };

    // Calculate Actuals
    const actualRent = mapZToValue(getZ('rent'), rentConfig);
    const actualVacancy = Math.max(0, mapZToValue(getZ('vacancy'), vacancyConfig));
    const actualInterest = Math.max(0.1, mapZToValue(getZ('interest'), interestConfig));
    const actualGrowth = mapZToValue(getZ('growth'), growthConfig);

    // Sample timing variance (Triangle distribution approximation or simple uniform within range)
    // Range: -Early to +Late (e.g. -2 to +4)
    // We'll use a random integer for month shift
    const minShift = -timeVarianceEarly;
    const maxShift = timeVarianceLate;
    const timingShiftMonths = minShift + Math.random() * (maxShift - minShift);

    // Calculate annual cashflows
    const cashflows = [];
    let currentRent = actualRent;
    let propertyValue = askingPrice;

    // Check if we have specific detailed cashflows to use as the base
    const useDetailedCashflow = inputs.detailedCashflows && inputs.detailedCashflows.length > 0;

    for (let year = 1; year <= holdingPeriodYears; year++) {
        let netCashflow;

        if (useDetailedCashflow) {
            // Use the specific year from the detailed spreadsheet model
            // We apply variance to the NET result, representing uncertainty in that specific year's outcome
            // Detailed cashflows are usually 0-indexed in array, so year 1 is index 0
            const detailedYear = inputs.detailedCashflows[year - 1];
            const baseNetCashflow = detailedYear ? detailedYear.netCashflow : 0;

            // Apply aggregate variances to this specific number
            // We assume the detailed model is the "Mean", and we apply macro uncertainty
            // Composite variance factor based on Rent and Outgoings variances
            // Simplifying: Apply a blended variance to the net result
            const blendedVariance = (rentVariancePercent + vacancyVariancePercent + outgoingsEstimate) / 3;
            // Better: Re-use the random variables we already generated, but relative to 1.0

            // Calculate a composite performance factor for this iteration
            // Logic: actualRent / estimatedGrossRent gives us the "performance %" for revenue
            const rentPerformance = estimatedGrossRent > 0 ? actualRent / estimatedGrossRent : 1;
            const vacancyFactor = (1 - actualVacancy / 100) / (1 - vacancyRatePercent / 100);

            // Apply global scenario performance to the detailed line item
            netCashflow = baseNetCashflow * rentPerformance * vacancyFactor;

        } else {
            // Standard High-Level Model with Advanced Lease Logic
            if (inputs.leaseDetails) {
                const { remainingTerm, reviewType, reviewValue, vacancyDurationMonths, newLeaseTermYear } = inputs.leaseDetails;

                // Year-by-Year State Machine for Rent
                if (year <= remainingTerm) {
                    // Existing Lease
                    if (year > 1) { // Year 1 is current rent
                        if (reviewType === 'fixed') {
                            currentRent *= (1 + reviewValue / 100);
                        } else if (reviewType === 'cpi') {
                            // Stochastic CPI (proxy via Capital Growth variance or dedicated?)
                            // Use basic 2.5% + noise for now
                            currentRent *= 1.025;
                        }
                    }
                } else if (year > remainingTerm && year <= remainingTerm + (vacancyDurationMonths / 12)) {
                    // Transition Period / Vacancy
                    // This simple check implies vacancy fits within a year boundary, which is rough.
                    // Better: calculate days occupied.
                }

                // SIMPLIFIED LEASE LOGIC for speed:
                // 1. Calculate base growth
                if (year > 1) {
                    if (reviewType === 'fixed' && year <= remainingTerm) {
                        currentRent *= (1 + reviewValue / 100);
                    } else {
                        // Market growth (cpi/market)
                        currentRent *= 1.025;
                    }
                }

                // 2. Apply vacancy shock if this is the expiry year
                let yearVacancyRate = actualVacancy; // Base stochastic vacancy

                if (year === Math.ceil(remainingTerm + 0.1) && vacancyDurationMonths > 0) {
                    // This is the year the lease ends. Add structural vacancy.
                    // E.g. ends year 3.2. Year 4 has the vacancy start? 
                    // Let's assume remainingTerm is integer years for UI v1 simplicity.
                    // If remainingTerm = 2, end of year 2. Year 3 starts with vacancy.
                    // vacancyDurationMonths = 6. Year 3 vacancy = 50%.
                    const structuralVacancy = (vacancyDurationMonths / 12) * 100;
                    yearVacancyRate = Math.max(yearVacancyRate, structuralVacancy);
                }

                // Calculate NOI
                const noi = calculateNOI(currentRent, yearVacancyRate, managementFeePercent, outgoingsEstimate);

                // Calculate weighted average interest if multiple loans
                let totalInterestExpense = 0;
                if (inputs.loanDetails && inputs.loanDetails.length > 0) {
                    inputs.loanDetails.forEach(loan => {
                        const loanRate = Math.max(0.1, randomWithVariance(loan.rate, interestVariancePercent));
                        totalInterestExpense += loan.amount * (loanRate / 100);
                    });
                } else {
                    // Fallback to simple loan model
                    const debtService = calculateDebtService(loanAmount, actualInterest);
                    totalInterestExpense = debtService;
                }

                // Net cashflow before tax
                netCashflow = noi - totalInterestExpense;
            } else {
                // Fallback: Legacy simple logic
                if (year > 1) currentRent *= 1.025;

                // Property value grows
                // propertyValue *= (1 + actualGrowth / 100); // Done globally below to match else block structure if needed

                const noi = calculateNOI(currentRent, actualVacancy, managementFeePercent, outgoingsEstimate);
                const debtService = calculateDebtService(loanAmount, actualInterest);
                netCashflow = noi - debtService;
            }

            // Common growth logic for non-detailed views
            if (year > 1) propertyValue *= (1 + actualGrowth / 100);
        }

        // Handle Terminal Value separately to ensure Capital Growth applies to Property Value
        if (useDetailedCashflow && year > 1) {
            propertyValue *= (1 + actualGrowth / 100);
        } else if (!useDetailedCashflow && year > 1 && year <= holdingPeriodYears) {
            // Already handled in standard block above
        }

        // Add terminal value in final year
        if (year === holdingPeriodYears) {
            const saleProceeds = propertyValue - loanAmount; // Repay loan
            cashflows.push({ amount: netCashflow + saleProceeds, year, timingShiftMonths });
        } else {
            cashflows.push({ amount: netCashflow, year, timingShiftMonths });
        }
    }

    // Calculate NPV with timing adjustments
    // Formula: CF / (1 + r)^t
    // where t = year + (shift / 12)
    const discountRateDecimal = discountRate / 100;

    const grossPV = cashflows.reduce((acc, cf) => {
        const timeInYears = cf.year + (cf.timingShiftMonths / 12);
        return acc + cf.amount / Math.pow(1 + discountRateDecimal, timeInYears);
    }, 0);

    const npv = grossPV - equityRequired;

    // Simplified IRR (ignoring timing variance for standard comparison, or approximating)
    const simpleCashflows = cashflows.map(c => c.amount);
    const irr = calculateIRR(simpleCashflows, equityRequired);

    // Calculate Cumulative Gross PV (Value Generated)
    // Start at 0 (ignoring purchase price as requested for Intrinsic Value analysis)
    const cumulativeGrossPV = [0];
    let runningGrossPV = 0;

    for (let t = 0; t < cashflows.length; t++) {
        const timeInYears = cashflows[t].year + (cashflows[t].timingShiftMonths / 12);
        const discountedCF = cashflows[t].amount / Math.pow(1 + discountRateDecimal, timeInYears);
        runningGrossPV += discountedCF;
        cumulativeGrossPV.push(runningGrossPV);
    }

    return { npv, grossPV, irr, finalValue: propertyValue, cumulativeGrossPV };
}

/**
 * Run full Monte Carlo simulation
 * @param {Object} inputs - Deal financial inputs, optionally including `detailedCashflows` array
 * @param {number} iterations - Number of simulation runs (default 5000)
 * @param {boolean} includeAcquisitionCost - If true, calculates NPV (Net); if false, Gross PV (Intrinsic)
 * @returns {Object} Simulation results with statistics and histograms
 */
export const DistributionTypes = Distributions;

export function getCholesky(matrix) {
    return choleskyDecomposition(matrix);
}

/**
 * Run full Monte Carlo simulation
 * @param {Object} inputs - Deal financial inputs
 * @param {number} iterations - Number of simulation runs (default 5000)
 * @param {boolean} includeAcquisitionCost - If true, calculates NPV (Net); if false, Gross PV (Intrinsic)
 * @returns {Object} Simulation results with statistics and histograms
 */
export function runSimulation(rawInputs, iterations = 5000, includeAcquisitionCost = false) {
    const startTime = performance.now();

    // Sanitize inputs to ensure all numeric fields are valid numbers
    const inputs = {
        ...rawInputs,
        askingPrice: Number(rawInputs.askingPrice) || 0,
        stampDutyRate: Number(rawInputs.stampDutyRate) || 0,
        legalCosts: Number(rawInputs.legalCosts) || 0,
        capExReserve: Number(rawInputs.capExReserve) || 0,
        estimatedGrossRent: Number(rawInputs.estimatedGrossRent) || 0,
        vacancyRatePercent: Number(rawInputs.vacancyRatePercent) || 0,
        managementFeePercent: Number(rawInputs.managementFeePercent) || 0,
        outgoingsEstimate: Number(rawInputs.outgoingsEstimate) || 0,
        loanAmount: Number(rawInputs.loanAmount) || 0,
        interestRatePercent: Number(rawInputs.interestRatePercent) || 0,
        capitalGrowthPercent: Number(rawInputs.capitalGrowthPercent) || 0,
        holdingPeriodYears: Number(rawInputs.holdingPeriodYears) || 10,
        rentVariancePercent: Number(rawInputs.rentVariancePercent) || 10,
        vacancyVariancePercent: Number(rawInputs.vacancyVariancePercent) || 5,
        interestVariancePercent: Number(rawInputs.interestVariancePercent) || 1,
        capitalGrowthVariancePercent: Number(rawInputs.capitalGrowthVariancePercent) || 2,
        discountRate: Number(rawInputs.discountRate) || 8,
        timeVarianceEarly: Number(rawInputs.timeVarianceEarly) || 0,
        timeVarianceLate: Number(rawInputs.timeVarianceLate) || 0
    };

    const targetValues = []; // Using generic array for Histogram/Percentiles based on mode
    const irrResults = [];
    const yearlyResults = []; // [iteration][year]

    // Pre-calculate Cholesky if correlation matrix exists
    let precalculatedCholesky = null;
    if (inputs.correlationMatrix) {
        precalculatedCholesky = choleskyDecomposition(inputs.correlationMatrix);
    }

    // Inject precalc into inputs for iteration
    const iterationInputs = { ...inputs, precalculatedCholesky };

    for (let i = 0; i < iterations; i++) {
        const { npv, grossPV, irr, cumulativeGrossPV } = runSingleIteration(iterationInputs);
        irrResults.push(irr);

        // Mode Selection
        if (includeAcquisitionCost) {
            // NET MODE (Profit): Value - Cost
            targetValues.push(npv);

            // Adjust cumulative series to start at -Equity (Cost)
            // aggregated cumulativeGrossPV starts at 0. We need to shift it down by equityRequired.
            // However, we don't have equityRequired handy here easily without re-calc.
            // Easier approach: runSingleIteration returns both. Let's update runSingleIteration to return cumulativeNetNPV too.
            // For now, let's approximate: cumulativeGrossPV is "Value Generated". 
            // cumulativeNetNPV = cumulativeGrossPV - Equity.
            // Actually, let's go back and fix runSingleIteration to be cleaner.

            // Re-calc equity for adjustment (fast enough)
            const stampDuty = inputs.askingPrice * (inputs.stampDutyRate / 100);
            const totalAcquisitionCost = inputs.askingPrice + stampDuty + inputs.legalCosts + inputs.capExReserve;
            const equityRequired = totalAcquisitionCost - inputs.loanAmount;

            const cumulativeNet = cumulativeGrossPV.map(v => v - equityRequired);
            yearlyResults.push(cumulativeNet);
        } else {
            // GROSS MODE (Intrinsic Value): Total Value
            targetValues.push(grossPV);
            yearlyResults.push(cumulativeGrossPV);
        }
    }

    // Sort for percentile calculations
    targetValues.sort((a, b) => a - b);
    irrResults.sort((a, b) => a - b);

    // Calculate percentiles
    const p10Index = Math.floor(iterations * 0.1);
    const p50Index = Math.floor(iterations * 0.5);
    const p90Index = Math.floor(iterations * 0.9);

    const p10NPV = targetValues[p10Index];
    const medianNPV = targetValues[p50Index];
    const p90NPV = targetValues[p90Index];

    const p10IRR = irrResults[p10Index];
    const medianIRR = irrResults[p50Index];
    const p90IRR = irrResults[p90Index];

    // Generate histograms (20 buckets)
    // Generate histograms (20 buckets) - Use Gross PV for the distribution chart as requested
    const npvHistogram = generateHistogram(targetValues, 20);
    const irrHistogram = generateHistogram(irrResults, 20);

    // Calculate Yearly Gross PV Percentiles
    const holdingPeriod = inputs.holdingPeriodYears;
    const yearlyDist = {
        p10: [],
        median: [],
        p90: []
    };

    // For each year (0 to N), extract all values, sort, and find percentiles
    for (let year = 0; year <= holdingPeriod; year++) {
        const yearValues = new Float32Array(iterations); // Optimization
        for (let i = 0; i < iterations; i++) {
            yearValues[i] = yearlyResults[i][year];
        }
        yearValues.sort();

        yearlyDist.p10.push(yearValues[p10Index]);
        yearlyDist.median.push(yearValues[p50Index]);
        yearlyDist.p90.push(yearValues[p90Index]);
    }

    // Calculate cap rate
    const noi = calculateNOI(
        inputs.estimatedGrossRent,
        inputs.vacancyRatePercent,
        inputs.managementFeePercent,
        inputs.outgoingsEstimate
    );
    const capRate = inputs.askingPrice > 0 ? (noi / inputs.askingPrice) * 100 : 0;

    // Determine recommendation
    let recommendedDecision = 'Analyzing';
    if (medianNPV < 0) {
        recommendedDecision = 'Uneconomic';
    } else if (p10NPV > 0) {
        recommendedDecision = 'Buy'; // Even downside is positive
    } else if (medianNPV > 0 && p10NPV > -inputs.askingPrice * 0.1) {
        recommendedDecision = 'Buy'; // Good median, limited downside
    } else {
        recommendedDecision = 'Pass'; // Too risky
    }

    const endTime = performance.now();

    // Generate Probability Curve (Cumulative Distribution)
    // Sample 100 points (0% to 100%) from sorted Gross PV results
    // Generate Probability Curve (Cumulative Distribution)
    // Sample 100 points (0% to 100%) from sorted target results
    // targetValues is already sorted
    const probabilityCurve = [];
    for (let i = 0; i <= 100; i++) {
        const index = Math.min(Math.floor((i / 100) * (iterations - 1)), iterations - 1);
        probabilityCurve.push({
            percent: i,
            value: targetValues[index]
        });
    }

    return {
        iterations,
        runTimeMs: Math.round(endTime - startTime),
        medianNPV: isFinite(medianNPV) ? Math.round(medianNPV) : 0,
        p10NPV: isFinite(p10NPV) ? Math.round(p10NPV) : 0,
        p90NPV: isFinite(p90NPV) ? Math.round(p90NPV) : 0,
        medianIRR: isFinite(medianIRR) ? Math.round(medianIRR * 10) / 10 : 0,
        p10IRR: isFinite(p10IRR) ? Math.round(p10IRR * 10) / 10 : 0,
        p90IRR: isFinite(p90IRR) ? Math.round(p90IRR * 10) / 10 : 0,
        calculatedCapRate: isFinite(capRate) ? Math.round(capRate * 100) / 100 : 0,
        recommendedDecision,
        npvHistogram,
        irrHistogram,
        yearlyDCF: yearlyDist,
        probabilityCurve: probabilityCurve.map(pt => ({
            percent: pt.percent,
            value: isFinite(pt.value) ? pt.value : 0
        }))
    };
}

/**
 * Generate histogram from array of values
 */
function generateHistogram(values, buckets = 20) {
    const min = Math.min(...values);
    const max = Math.max(...values);
    const range = max - min;
    const bucketSize = range > 0 ? range / buckets : 1;

    const histogram = new Array(buckets).fill(0).map((_, i) => ({
        min: min + i * (range > 0 ? bucketSize : 0),
        max: min + (i + 1) * (range > 0 ? bucketSize : 0),
        count: 0
    }));

    values.forEach(v => {
        let bucketIndex = range > 0 ? Math.floor((v - min) / bucketSize) : 0;
        if (isNaN(bucketIndex)) bucketIndex = 0;
        bucketIndex = Math.max(0, Math.min(bucketIndex, buckets - 1));

        if (histogram[bucketIndex]) {
            histogram[bucketIndex].count++;
        }
    });

    return histogram;
}

/**
 * Format currency for display
 */
export function formatCurrency(value) {
    return new Intl.NumberFormat('en-AU', {
        style: 'currency',
        currency: 'AUD',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(value || 0);
}

/**
 * Format percentage for display
 */
export function formatPercent(value, decimals = 1) {
    return `${value.toFixed(decimals)}%`;
}
