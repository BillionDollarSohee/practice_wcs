import axios from 'axios'
import { boot } from 'quasar/wrappers'

// 백엔드 주소
const api = axios.create({ baseURL : 'http://localhost:9090'})

export default boot(({ app }) => {
  app.config.globalProperties.$axios = axios
  app.config.globalProperties.$api = api
})

export { api }