import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { get } from '../../../shared/api/httpClient'

type DatabasePingResponse = {
  connected: boolean
  database: string
}

type ApiHealthState = {
  status: 'idle' | 'loading' | 'succeeded' | 'failed'
  database: string
  error: string
}

const INITIAL_STATE: ApiHealthState = {
  status: 'idle',
  database: '',
  error: '',
}

export const checkDatabaseConnection = createAsyncThunk(
  'apiHealth/checkDatabaseConnection',
  async () => get<DatabasePingResponse>('/api/database/ping'),
)

const apiHealthSlice = createSlice({
  name: 'apiHealth',
  initialState: INITIAL_STATE,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(checkDatabaseConnection.pending, (state) => {
        state.status = 'loading'
        state.error = ''
      })
      .addCase(checkDatabaseConnection.fulfilled, (state, action) => {
        state.status = action.payload.connected ? 'succeeded' : 'failed'
        state.database = action.payload.database
        state.error = ''
      })
      .addCase(checkDatabaseConnection.rejected, (state, action) => {
        state.status = 'failed'
        state.error = action.error.message ?? ''
      })
  },
})

export default apiHealthSlice.reducer
