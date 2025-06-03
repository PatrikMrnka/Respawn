import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
  HubConnectionState,
} from '@microsoft/signalr'
import { useAuthStore } from '@/stores/authStore'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL

interface HubConfig {
  connection: HubConnection | null
  connectionPromise: Promise<void> | null
  eventCallbacks: Map<string, Set<(...args: any[]) => void>>
}

class SignalRService {
  private hubConfigs: Map<string, HubConfig> = new Map()

  private getHubConfig(hubPath: string): HubConfig {
    if (!this.hubConfigs.has(hubPath)) {
      this.hubConfigs.set(hubPath, {
        connection: null,
        connectionPromise: null,
        eventCallbacks: new Map(),
      })
    }
    return this.hubConfigs.get(hubPath)!
  }

  public async startConnection(hubPath: string): Promise<void> {
    // Přidána kontrola a logování pro hubPath
    if (typeof hubPath !== 'string' || !hubPath.startsWith('/')) {
      const errMsg = `SignalRService: Invalid hubPath provided: '${hubPath}'. Must be a non-empty string starting with '/'.`
      console.error(errMsg)
      return Promise.reject(new Error(errMsg))
    }

    const config = this.getHubConfig(hubPath)
    const fullHubUrl = `${API_BASE_URL}${hubPath}`
    console.log(`SignalRService: Attempting to start connection to ${fullHubUrl}`)

    if (config.connection && config.connection.state === HubConnectionState.Connected) {
      console.log(`SignalR connection to ${fullHubUrl} already established.`)
      return
    }

    if (config.connectionPromise) {
      console.log(`SignalR connection attempt to ${fullHubUrl} in progress.`)
      return config.connectionPromise
    }

    const authStore = useAuthStore()
    const token = authStore.token

    // Kontrola tokenu pro chráněné huby
    // Předpokládáme, že všechny huby mohou vyžadovat token, pokud je uživatel přihlášen
    if (!token && authStore.isLoggedIn) {
      // Pokud je uživatel přihlášen, ale token chybí (nemělo by nastat)
      console.warn(
        `SignalRService (${hubPath}): Auth token is missing for a logged-in user. Connection might fail if hub requires auth.`,
      )
    }

    const existingConnection = config.connection
    if (existingConnection && existingConnection.state !== HubConnectionState.Disconnected) {
      console.log(
        `SignalRService: Stopping existing connection to ${fullHubUrl} before restarting.`,
      )
      await existingConnection
        .stop()
        .catch((err) => console.error(`Error stopping existing connection to ${fullHubUrl}:`, err))
    }

    config.connection = new HubConnectionBuilder()
      .withUrl(fullHubUrl, {
        accessTokenFactory: () => token || '',
      })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 15000, 30000])
      .build()

    config.connection.onclose(async (error) => {
      console.error(`SignalR connection to ${fullHubUrl} closed.`, error)
      config.connectionPromise = null
    })

    config.eventCallbacks.forEach((callbacks, eventName) => {
      callbacks.forEach((callback) => {
        config.connection?.on(eventName, callback)
      })
    })

    config.connectionPromise = config.connection
      .start()
      .then(() => {
        console.log(`SignalR connection to ${fullHubUrl} established.`)
        config.connectionPromise = null
      })
      .catch((err) => {
        console.error(`Error establishing SignalR connection to ${fullHubUrl}:`, err)
        config.connectionPromise = null
        // config.connection = null; // Necháme spojení, aby se mohlo pokusit o reconnect
        throw err
      })
    return config.connectionPromise
  }

  public async stopConnection(hubPath: string): Promise<void> {
    const config = this.hubConfigs.get(hubPath)
    if (config?.connection && config.connection.state === HubConnectionState.Connected) {
      await config.connection.stop()
      console.log(`SignalR connection to ${API_BASE_URL}${hubPath} stopped.`)
    }
    if (config) {
      // Neodstraňujeme config.connection úplně, aby se mohlo znovu připojit
      // config.connection = null;
      config.connectionPromise = null
    }
  }

  public async stopAllConnections(): Promise<void> {
    console.log('SignalRService: Stopping all connections...')
    for (const hubPath of this.hubConfigs.keys()) {
      await this.stopConnection(hubPath)
    }
    // this.hubConfigs.clear(); // Nečistíme mapu, aby listenery zůstaly pro případné znovupřipojení
  }

  public on(hubPath: string, eventName: string, callback: (...args: any[]) => void): void {
    const config = this.getHubConfig(hubPath) // Zajistí existenci configu
    if (!config.eventCallbacks.has(eventName)) {
      config.eventCallbacks.set(eventName, new Set())
    }
    config.eventCallbacks.get(eventName)?.add(callback)

    if (config.connection && config.connection.state === HubConnectionState.Connected) {
      config.connection.on(eventName, callback)
    }
  }

  public off(hubPath: string, eventName: string, callback: (...args: any[]) => void): void {
    const config = this.hubConfigs.get(hubPath)
    config?.eventCallbacks.get(eventName)?.delete(callback)
    if (config?.connection) {
      config.connection.off(eventName, callback)
    }
  }

  public getConnectionState(hubPath: string): HubConnectionState | null {
    return this.hubConfigs.get(hubPath)?.connection?.state || null
  }
}

export const signalRService = new SignalRService()
