// src/services/signalrService.ts
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useAuthStore } from '@/stores/authStore'; // Pro přístup k tokenu

const API_BASE_URL = 'http://localhost:5207'; // Základní URL vašeho API

class SignalRService {
  private connection: HubConnection | null = null;
  private connectionPromise: Promise<void> | null = null;
  private eventCallbacks: Map<string, Set<(...args: any[]) => void>> = new Map();

  public async startConnection(): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      console.log('SignalR connection already established.');
      return;
    }

    if (this.connectionPromise) {
      console.log('SignalR connection attempt in progress.');
      return this.connectionPromise;
    }

    const authStore = useAuthStore();
    const token = authStore.token;

    this.connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/pollHub`, {
        accessTokenFactory: () => token || '', // Poskytnutí tokenu, pokud existuje
        // skipNegotiation: true, // Může být potřeba pro některé konfigurace, zkuste bez toho
        // transport: signalR.HttpTransportType.WebSockets // Explicitní vynucení WebSockets
      })
      .configureLogging(LogLevel.Information) // Nebo LogLevel.Debug pro více detailů
      .withAutomaticReconnect([0, 2000, 10000, 30000, null]) // Intervaly pro znovupřipojení
      .build();

    this.connection.onclose(async (error) => {
      console.error('SignalR connection closed.', error);
      // Zde můžete implementovat logiku pro upozornění uživatele nebo pokus o manuální znovupřipojení
      // await this.startConnection(); // Automatické znovupřipojení je již nastaveno
    });

    // Registrace handlerů, které byly přidány před startem spojení
    this.eventCallbacks.forEach((callbacks, eventName) => {
      callbacks.forEach(callback => {
        this.connection?.on(eventName, callback);
      });
    });

    this.connectionPromise = this.connection.start()
      .then(() => {
        console.log('SignalR connection established.');
        this.connectionPromise = null;
      })
      .catch(err => {
        console.error('Error establishing SignalR connection:', err);
        this.connectionPromise = null;
        // Zde můžete zkusit znovu po nějaké době nebo informovat uživatele
        // setTimeout(() => this.startConnection(), 5000);
        throw err; // Vyhodit chybu dál, aby komponenta věděla
      });
    return this.connectionPromise;
  }

  public async stopConnection(): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      await this.connection.stop();
      console.log('SignalR connection stopped.');
    }
    this.connection = null;
    this.connectionPromise = null;
  }

  public on(eventName: string, callback: (...args: any[]) => void): void {
    if (!this.eventCallbacks.has(eventName)) {
      this.eventCallbacks.set(eventName, new Set());
    }
    this.eventCallbacks.get(eventName)?.add(callback);

    // Pokud je spojení již aktivní, zaregistrujte handler přímo
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

// Export jedné instance služby (singleton)
export const signalRService = new SignalRService();
