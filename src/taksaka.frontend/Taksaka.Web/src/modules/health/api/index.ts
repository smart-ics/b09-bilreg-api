import { httpClient } from '../../../shared/api/httpClient'

export async function fetchHealth() {
  const response = await httpClient.get('/api/health')
  return response.data
}
