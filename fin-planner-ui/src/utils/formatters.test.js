import { describe, it, expect } from 'vitest';
import { formatCurrency, formatCurrencyPrecise, formatDate, formatPercent, formatNumber } from './formatters';

describe('Formatters', () => {
    describe('formatCurrency', () => {
        it('should format default AUD', () => {
            expect(formatCurrency(1000)).toBe('$1,000');
        });

        it('should handle zero', () => {
            expect(formatCurrency(0)).toBe('$0');
        });

        it('should handle large numbers', () => {
            expect(formatCurrency(1234567)).toBe('$1,234,567');
        });
    });

    describe('formatCurrencyPrecise', () => {
        it('should include 2 decimal places', () => {
            expect(formatCurrencyPrecise(1000.5)).toBe('$1,000.50');
        });

        it('should round correctly', () => {
            expect(formatCurrencyPrecise(1000.556)).toBe('$1,000.56');
        });
    });

    describe('formatPercent', () => {
        it('should format positive percentage with sign', () => {
            expect(formatPercent(5.5)).toBe('+5.50%');
        });

        it('should format negative percentage', () => {
            expect(formatPercent(-5.5)).toBe('-5.50%');
        });

        it('should handle zero', () => {
            expect(formatPercent(0)).toBe('+0.00%');
        });
    });

    describe('formatDate', () => {
        it('should format medium date by default', () => {
            const date = new Date('2023-01-01');
            // Depending on locale, might vary slightly, but checking basic format
            const formatted = formatDate(date);
            expect(formatted).toContain('Jan');
            expect(formatted).toContain('2023');
        });
    });
});
