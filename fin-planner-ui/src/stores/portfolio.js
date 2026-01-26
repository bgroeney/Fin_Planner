import { defineStore } from 'pinia';
import api from '../services/api';

export const usePortfolioStore = defineStore('portfolio', {
    state: () => ({
        portfolios: [],
        currentPortfolioId: localStorage.getItem('currentPortfolioId') || null,
        loading: false
    }),
    getters: {
        currentPortfolio: (state) => {
            if (!state.currentPortfolioId && state.portfolios.length > 0) {
                return state.portfolios[0];
            }
            return state.portfolios.find(p => p.id === state.currentPortfolioId) || state.portfolios[0] || null;
        },
        currentPortfolioValue() {
            return this.currentPortfolio?.totalValue || 0;
        }
    },
    actions: {
        async fetchPortfolios() {
            this.loading = true;
            try {
                const res = await api.get('/portfolios');
                this.portfolios = res.data;

                // If we have a saved ID, but it's not in the new list, or if we have no ID
                if (this.portfolios.length > 0) {
                    const validId = this.portfolios.some(p => p.id === this.currentPortfolioId);
                    if (!validId) {
                        this.setCurrentPortfolio(this.portfolios[0].id);
                    }
                }
            } catch (error) {
                console.error('Failed to fetch portfolios', error);
            } finally {
                this.loading = false;
            }
        },
        setCurrentPortfolio(id) {
            this.currentPortfolioId = id;
            localStorage.setItem('currentPortfolioId', id);
        }
    }
});
