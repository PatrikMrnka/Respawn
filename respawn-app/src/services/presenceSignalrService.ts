// src/services/presenceSignalrService.ts
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useAuthStore } from '@/stores/authStore';

const API_BASE_URL = 'http://localhost:5207'; // Základní URL vašeho API

class PresenceSignalRService {
  private connection: HubConnection | null = null;
  private connectionPromise: Promise<void> | null = null;
  private eventCallbacks: Map<string, Set<(...args: any[]) => void>> = new Map();

  public async startConnection(): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      return;
    }
    if (this.connectionPromise) {
      return this.connectionPromise;
    }

    const authStore = useAuthStore();
    const token = authStore.token;

    if (!token) {
      console.warn('PresenceSignalRService: No auth token available. Connection will not be started.');
      return Promise.reject(new Error("No auth token for SignalR."));
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/presenceHub`, { // Cesta k PresenceHubu
        accessTokenFactory: () => token
      })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 15000, 30000]) // Delší intervaly pro presence
      .build();

    this.connection.onclose(error => {
      console.error('PresenceSignalR connection closed.', error);
    });

    // Znovu navázat listenery, pokud byly přidány před startem
    this.eventCallbacks.forEach((callbacks, eventName) => {
      callbacks.forEach(callback => {
        this.connection?.on(eventName, callback);
      });
    });

    this.connectionPromise = this.connection.start()
      .then(() => {
        console.log('PresenceSignalR connection established.');
        this.connectionPromise = null;
      })
      .catch(err => {
        console.error('Error establishing PresenceSignalR connection:', err);
        this.connectionPromise = null;
        throw err;
      });
    return this.connectionPromise;
  }

  public async stopConnection(): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      await this.connection.stop();
      console.log('PresenceSignalR connection stopped.');
    }
    this.connection = null;
    this.connectionPromise = null;
  }

  public on(eventName: string, callback: (...args: any[]) => void): void {
    if (!this.eventCallbacks.has(eventName)) {
      this.eventCallbacks.set(eventName, new Set());
    }
    this.eventCallbacks.get(eventName)?.add(callback);

    if (this.connection && this.connection.state === 'Connected') {
      this.connection.on(eventName, callback);
    }
  }

  public off(eventName: string, callback: (...args: any[]) => void): void {
    this.eventCallbacks.get(eventName)?.delete(callback);
    if (this.connection) {
      this.connection.off(eventName, callback);
    }
  }

   public getConnectionState(): string | null {
    return this.connection?.state || null;
  }
}

export const presenceSignalRService = new PresenceSignalRService();
