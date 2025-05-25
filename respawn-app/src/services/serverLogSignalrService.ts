// File: haha/respawn-app/src/services/serverLogSignalrService.ts
import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
  HubConnectionState,
} from '@microsoft/signalr'
import { useAuthStore } from '@/stores/authStore'
import Swal from 'sweetalert2'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL // Your API base URL
const SERVER_LOG_HUB_URL = `${API_BASE_URL}/serverLogHub`

class ServerLogSignalRService {
  private connection: HubConnection | null = null
  private onLogLineReceivedCallback: ((line: string) => void) | null = null
  private currentWatchingGameServerId: string | null = null

  private getAuthToken(): string | null {
    const authStore = useAuthStore()
    return authStore.token
  }

  public async startConnection(): Promise<boolean> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      console.log('ServerLogSignalRService: Connection already established.')
      return true
    }

    const token = this.getAuthToken()
    if (!token) {
      console.warn(
        'ServerLogSignalRService: No auth token available. Connection will not be started.',
      )
      Swal.fire({
        icon: 'error',
        titleText: 'Chyba autentizace',
        text: 'Pro sledování logů je nutné přihlášení.',
        background: '#1A2033',
        color: '#E0E0E0',
      })
      return false
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(SERVER_LOG_HUB_URL, {
        accessTokenFactory: () => token,
      })
      .configureLogging(LogLevel.Information) // Or LogLevel.Debug for more details
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build()

    this.connection.onclose((error) => {
      console.error('ServerLogSignalRService: Connection closed.', error)
      // Optionally notify the user or attempt to restart if not handled by withAutomaticReconnect
      if (this.currentWatchingGameServerId && this.onLogLineReceivedCallback) {
        this.onLogLineReceivedCallback(
          `[SYSTEM] Spojení se serverem logů bylo přerušeno. Probíhá pokus o znovupřipojení... Error: ${error?.message || 'Unknown error'}`,
        )
      }
    })

    this.connection.onreconnecting((error) => {
      console.warn(`ServerLogSignalRService: Connection reconnecting due to: ${error}`)
      if (this.currentWatchingGameServerId && this.onLogLineReceivedCallback) {
        this.onLogLineReceivedCallback(
          `[SYSTEM] Ztráta spojení se serverem logů, pokus o znovupřipojení...`,
        )
      }
    })

    this.connection.onreconnected((connectionId) => {
      console.log(
        `ServerLogSignalRService: Connection reconnected with new connectionId: ${connectionId}. Re-watching logs if previously watching.`,
      )
      if (this.currentWatchingGameServerId && this.onLogLineReceivedCallback) {
        this.onLogLineReceivedCallback(`[SYSTEM] Spojení se serverem logů obnoveno.`)
        // Re-invoke WatchLogs if a server was being watched
        this.watchLogs(this.currentWatchingGameServerId, this.onLogLineReceivedCallback)
      }
    })

    // Register client-side handler for receiving log lines
    this.connection.on('ReceiveLogLine', (line: string) => {
      if (this.onLogLineReceivedCallback) {
        this.onLogLineReceivedCallback(line)
      }
    })

    try {
      await this.connection.start()
      console.log('ServerLogSignalRService: Connection started successfully.')
      return true
    } catch (err) {
      console.error('ServerLogSignalRService: Error starting connection:', err)
      Swal.fire({
        icon: 'error',
        titleText: 'Chyba připojení k logům',
        text: `Nepodařilo se připojit k real-time službě logů: ${err instanceof Error ? err.message : String(err)}`,
        background: '#1A2033',
        color: '#E0E0E0',
      })
      this.connection = null // Reset connection on failure
      return false
    }
  }

  public async watchLogs(gameServerId: string, callback: (line: string) => void): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      console.warn(
        'ServerLogSignalRService: Not connected. Attempting to start connection before watching logs.',
      )
      const connected = await this.startConnection()
      if (!connected) {
        callback('[SYSTEM ERROR] Nepodařilo se připojit k serveru logů.')
        return
      }
    }

    // If already watching another server, stop that first
    if (this.currentWatchingGameServerId && this.currentWatchingGameServerId !== gameServerId) {
      await this.unwatchLogs(this.currentWatchingGameServerId)
    }

    this.onLogLineReceivedCallback = callback
    this.currentWatchingGameServerId = gameServerId
    try {
      await this.connection?.invoke('WatchLogs', gameServerId)
      console.log(
        `ServerLogSignalRService: Requested to watch logs for gameServerId: ${gameServerId}`,
      )
    } catch (err) {
      console.error(`ServerLogSignalRService: Error invoking "WatchLogs" for ${gameServerId}:`, err)
      this.onLogLineReceivedCallback(
        `[SYSTEM ERROR] Nepodařilo se zahájit sledování logů pro server ${gameServerId}: ${err instanceof Error ? err.message : String(err)}`,
      )
      this.currentWatchingGameServerId = null // Reset if watch failed
    }
  }

  public async unwatchLogs(gameServerId: string | null = null): Promise<void> {
    const idToUnwatch = gameServerId || this.currentWatchingGameServerId
    if (idToUnwatch && this.connection && this.connection.state === HubConnectionState.Connected) {
      try {
        await this.connection.invoke('UnwatchLogs', idToUnwatch)
        console.log(
          `ServerLogSignalRService: Requested to unwatch logs for gameServerId: ${idToUnwatch}`,
        )
      } catch (err) {
        console.error(
          `ServerLogSignalRService: Error invoking "UnwatchLogs" for ${idToUnwatch}:`,
          err,
        )
        // Optionally notify user, though usually unwatch is silent on error
      }
    }
    if (idToUnwatch === this.currentWatchingGameServerId) {
      this.onLogLineReceivedCallback = null // Clear callback
      this.currentWatchingGameServerId = null
    }
  }

  public async stopConnection(): Promise<void> {
    if (this.currentWatchingGameServerId) {
      await this.unwatchLogs(this.currentWatchingGameServerId) // Ensure unwatch is called
    }
    if (this.connection) {
      try {
        await this.connection.stop()
        console.log('ServerLogSignalRService: Connection stopped.')
      } catch (err) {
        console.error('ServerLogSignalRService: Error stopping connection:', err)
      } finally {
        this.connection = null
        this.onLogLineReceivedCallback = null
        this.currentWatchingGameServerId = null
      }
    }
  }

  public getConnectionState(): HubConnectionState | null {
    return this.connection?.state || null
  }
}

export const serverLogSignalr = new ServerLogSignalRService()
