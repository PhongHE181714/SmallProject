import { configureStore } from '@reduxjs/toolkit'
import apiHealthReducer from '../features/apiHealth/model/apiHealthSlice'

export const store = configureStore({
  reducer: {
    apiHealth: apiHealthReducer,
  },
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
