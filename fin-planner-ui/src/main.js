import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import './style.css'
import VueApexCharts from 'vue3-apexcharts'
import googleLogin from 'vue3-google-login'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(VueApexCharts)
app.use(googleLogin, {
    clientId: import.meta.env.VITE_GOOGLE_CLIENT_ID || 'INSERT_YOUR_GOOGLE_CLIENT_ID_HERE'
})

app.mount('#app')
