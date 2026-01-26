import api from './api';

export default {
    getGoals() {
        return api.get('/goals');
    },

    createGoal(goal) {
        return api.post('/goals', goal);
    },

    updateGoal(id, goal) {
        return api.put(`/goals/${id}`, goal);
    },

    deleteGoal(id) {
        return api.delete(`/goals/${id}`);
    }
}
