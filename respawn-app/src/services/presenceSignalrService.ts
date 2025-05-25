import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useAuthStore } from '@/stores/authStore'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL // Base URL of your API

class PresenceSignalRService {
  private connection: HubConnection | null = null
  private connectionPromise: Promise<void> | null = null
  // Map to store event callbacks for re-registration after reconnection
  private eventCallbacks: Map<string, Set<(...args: any[]) => void>> = new Map()

  /**
   * Starts a SignalR connection to the presence hub
   * @returns Promise that resolves when the connection is established
   */
  public async startConnection(): Promise<void> {
    // Return immediately if already connected
    if (this.connection && this.connection.state === 'Connected') {
      return
    }
    // Return the existing promise if connection is in progress
    if (this.connectionPromise) {
      return this.connectionPromise
    }

    const authStore = useAuthStore()
    const token = authStore.token

    // Verify authentication token is available
    if (!token) {
      console.warn(
        'PresenceSignalRService: No auth token available. Connection will not be started.',
      )
      return Promise.reject(new Error('No auth token for SignalR.'))
    }

    // Build the SignalR connection
    this.connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/presenceHub`, {
        // Path to PresenceHub
        accessTokenFactory: () => token,
      })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 15000, 30000]) // Longer intervals for presence
      .build()

    // Handle connection closure
    this.connection.onclose((error) => {
      console.error('PresenceSignalR connection closed.', error)
    })

    // Re-register event listeners if they were added before connection start
    this.eventCallbacks.forEach((callbacks, eventName) => {
      callbacks.forEach((callback) => {
        this.connection?.on(eventName, callback)
      })
    })

    // Start the connection
    this.connectionPromise = this.connection
      .start()
      .then(() => {
        console.log('PresenceSignalR connection established.')
        this.connectionPromise = null
      })
      .catch((err) => {
        console.error('Error establishing PresenceSignalR connection:', err)
        this.connectionPromise = null
        throw err
      })
    return this.connectionPromise
  }

  /**
   * Stops the SignalR connection if it's active
   */
  public async stopConnection(): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      await this.connection.stop()
      console.log('PresenceSignalR connection stopped.')
    }
    this.connection = null
    this.connectionPromise = null
  }

  /**
   * Registers an event handler for a SignalR event
   * @param eventName Name of the SignalR event
   * @param callback Function to execute when the event is triggered
   */
  public on(eventName: string, callback: (...args: any[]) => void): void {
    // Store callback for potential reconnection
    if (!this.eventCallbacks.has(eventName)) {
      this.eventCallbacks.set(eventName, new Set())
    }
    this.eventCallbacks.get(eventName)?.add(callback)

    // Register callback if connection is active
    if (this.connection && this.connection.state === 'Connected') {
      this.connection.on(eventName, callback)
    }
  }

  /**
   * Unregisters an event handler for a SignalR event
   * @param eventName Name of the SignalR event
   * @param callback Function to remove from event handlers
   */
  public off(eventName: string, callback: (...args: any[]) => void): void {
    this.eventCallbacks.get(eventName)?.delete(callback)
    if (this.connection) {
      this.connection.off(eventName, callback)
    }
  }

  /**
   * Returns the current state of the SignalR connection
   * @returns Connection state string or null if no connection exists
   */
  public getConnectionState(): string | null {
    return this.connection?.state || null
  }
}

// Export singleton instance of the service
export const presenceSignalRService = new PresenceSignalRService()
