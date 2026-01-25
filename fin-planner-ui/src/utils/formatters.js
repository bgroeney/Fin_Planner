/**
 * Shared formatting utilities for the FinPlanner UI
 */

/**
 * Format a number as Australian currency
 * @param {number} value - The value to format
 * @param {Object} options - Formatting options
 * @param {string} options.currency - Currency code (default: 'AUD')
 * @param {number} options.minimumFractionDigits - Minimum decimal places (default: 0)
 * @param {number} options.maximumFractionDigits - Maximum decimal places (default: 0)
 * @returns {string} Formatted currency string
 */
export function formatCurrency(value, options = {}) {
    const {
        currency = 'AUD',
        minimumFractionDigits = 0,
        maximumFractionDigits = 0
    } = options;

    return new Intl.NumberFormat('en-AU', {
        style: 'currency',
        currency,
        minimumFractionDigits,
        maximumFractionDigits
    }).format(value || 0);
}

/**
 * Format a number as currency with decimal places (for precise values)
 * @param {number} value - The value to format
 * @returns {string} Formatted currency string with 2 decimal places
 */
export function formatCurrencyPrecise(value) {
    return formatCurrency(value, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

/**
 * Format a date for display
 * @param {string|Date} date - The date to format
 * @param {string} style - Format style: 'short', 'medium', 'long' (default: 'medium')
 * @returns {string} Formatted date string
 */
export function formatDate(date, style = 'medium') {
    if (!date) return '';

    const d = new Date(date);

    const styles = {
        short: { day: 'numeric', month: 'numeric', year: '2-digit' },
        medium: { day: 'numeric', month: 'short', year: 'numeric' },
        long: { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }
    };

    return d.toLocaleDateString('en-AU', styles[style] || styles.medium);
}

/**
 * Format a percentage value
 * @param {number} value - The percentage value
 * @param {number} decimals - Number of decimal places (default: 2)
 * @returns {string} Formatted percentage string
 */
export function formatPercent(value, decimals = 2) {
    if (value == null) return '0%';
    const sign = value >= 0 ? '+' : '';
    return `${sign}${value.toFixed(decimals)}%`;
}

/**
 * Format a number with locale-appropriate separators
 * @param {number} value - The value to format
 * @param {number} decimals - Number of decimal places (default: 0)
 * @returns {string} Formatted number string
 */
export function formatNumber(value, decimals = 0) {
    if (value == null) return '0';
    return new Intl.NumberFormat('en-AU', {
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals
    }).format(value);
}
