import axios from 'axios'

import { clearSession, getAccessToken, setSession } from './authState'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5000',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

let refreshing: Promise<string | null> | null = null

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const response = error.response
    const originalRequest = error.config

    if (response?.status !== 401 || originalRequest?._retry) {
      return Promise.reject(error)
    }

    originalRequest._retry = true
    refreshing ??= axios
      .post(`${api.defaults.baseURL}/api/v1/auth/refresh`, undefined, { withCredentials: true })
      .then((refreshResponse) => {
        setSession(refreshResponse.data.accessToken, refreshResponse.data.usuario)
        return refreshResponse.data.accessToken as string
      })
      .catch(() => {
        clearSession()
        return null
      })
      .finally(() => {
        refreshing = null
      })

    const token = await refreshing
    if (!token) {
      return Promise.reject(error)
    }

    originalRequest.headers.Authorization = `Bearer ${token}`
    return api(originalRequest)
  },
)
