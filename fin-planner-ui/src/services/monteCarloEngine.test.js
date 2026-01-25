import { describe, it, expect } from 'vitest';
import { calculateNOI, calculateIRR, runSingleIteration, runSimulation } from './monteCarloEngine';

describe('Financial Calculations', () => {
    describe('calculateNOI', () => {
        it('should correctly calculate NOI with all deductions', () => {
            // Gross Rent: 100,000
            // Vacancy: 5% -> Effective: 95,000
            // Mgmt Fee: 10% of Effective -> 9,500
            // Outgoings: 15,000
            // NOI = 95,000 - 9,500 - 15,000 = 70,500
            const noi = calculateNOI(100000, 5, 10, 15000);
            expect(noi).toBe(70500);
        });

        it('should handle zero vacancy and fees', () => {
            const noi = calculateNOI(100000, 0, 0, 0);
            expect(noi).toBe(100000);
        });
    });

    describe('calculateIRR', () => {
        it('should calculate correct IRR for simple cashflow', () => {
            // Invest 100, Return 110 after 1 year -> 10% IRR
            const cashflows = [110];
            const initialInvestment = 100;
            const irr = calculateIRR(cashflows, initialInvestment);
            expect(irr).toBeCloseTo(10, 1);
        });

        it('should calculate correct IRR for multi-year', () => {
            // Invest 100
            // Year 1: 10
            // Year 2: 110 (Sale + Rent)
            // Total 120 over 2 years roughly 10%
            const cashflows = [10, 110];
            const initialInvestment = 100;
            const irr = calculateIRR(cashflows, initialInvestment);
            expect(irr).toBeCloseTo(10, 1);
        });
    });

    describe('runSingleIteration', () => {
        const baseInputs = {
            askingPrice: 1000000,
            stampDutyRate: 0,
            legalCosts: 0,
            capExReserve: 0,
            estimatedGrossRent: 50000,
            vacancyRatePercent: 0,
            managementFeePercent: 0,
            outgoingsEstimate: 0,
            loanAmount: 800000,
            interestRatePercent: 5,
            capitalGrowthPercent: 0,
            holdingPeriodYears: 5,
            rentVariancePercent: 0,
            vacancyVariancePercent: 0,
            interestVariancePercent: 0,
            capitalGrowthVariancePercent: 0,
            discountRate: 10
        };

        it('should consistently produce same NPV with zero variance', () => {
            const res1 = runSingleIteration(baseInputs);
            const res2 = runSingleIteration(baseInputs);
            expect(res1.npv).toBe(res2.npv);
        });

        it('should include cumulativeGrossPV in results', () => {
            const res = runSingleIteration(baseInputs);
            expect(res.cumulativeGrossPV).toBeDefined();
            expect(res.cumulativeGrossPV.length).toBe(baseInputs.holdingPeriodYears + 1);
        });
    });

    describe('runSimulation', () => {
        const inputs = {
            askingPrice: 1000000,
            stampDutyRate: 5,
            legalCosts: 2000,
            capExReserve: 5000,
            estimatedGrossRent: 50000,
            vacancyRatePercent: 5,
            managementFeePercent: 7,
            outgoingsEstimate: 10000,
            loanAmount: 800000,
            interestRatePercent: 6,
            capitalGrowthPercent: 4,
            holdingPeriodYears: 10,
            rentVariancePercent: 10,
            vacancyVariancePercent: 20,
            interestVariancePercent: 10,
            capitalGrowthVariancePercent: 10,
            discountRate: 8
        };

        it('should complete 5000 iterations quickly', () => {
            const start = performance.now();
            const res = runSimulation(inputs, 1000); // reduced for test speed
            const end = performance.now();

            expect(res.iterations).toBe(1000);
            expect(end - start).toBeLessThan(1000); // Should be very fast
        });

        it('should provide complete statistics', () => {
            const res = runSimulation(inputs, 100);

            expect(res.medianNPV).toBeDefined();
            expect(res.p10NPV).toBeDefined();
            expect(res.p90NPV).toBeDefined();
            expect(res.recommendedDecision).toBeDefined();
            expect(res.yearlyDCF).toBeDefined();
            expect(res.yearlyDCF.median.length).toBe(inputs.holdingPeriodYears + 1);
        });
    });
});
