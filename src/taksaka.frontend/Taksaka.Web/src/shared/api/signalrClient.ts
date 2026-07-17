import * as signalR from '@microsoft/signalr'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

export function createOperationsConnection(): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${baseURL}/hubs/operations`)
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build()
}

export async function connectOperationsHub(): Promise<signalR.HubConnection> {
  const connection = createOperationsConnection()

  connection.on('HealthChanged', (dimension: string, state: string) => {
    console.info('[SignalR] HealthChanged', { dimension, state })
  })

  await connection.start()
  console.info('[SignalR] Connected to operations hub')

  return connection
}
