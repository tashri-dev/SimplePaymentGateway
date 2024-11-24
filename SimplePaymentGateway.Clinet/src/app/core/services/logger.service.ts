import { Injectable } from "@angular/core";

// src/app/core/services/logger.service.ts
@Injectable({
  providedIn: 'root'
})
export class LoggerService {
  private logLevel: LogLevel = LogLevel.Debug;
  private readonly APP_NAME = 'PaymentGateway';

  constructor() {}

  debug(message: string, ...args: any[]): void {
    this.log(LogLevel.Debug, message, args);
  }

  info(message: string, ...args: any[]): void {
    this.log(LogLevel.Info, message, args);
  }

  warn(message: string, ...args: any[]): void {
    this.log(LogLevel.Warn, message, args);
  }

  error(message: string, error?: any, ...args: any[]): void {
    this.log(LogLevel.Error, message, args, error);
  }

  private log(level: LogLevel, message: string, args: any[] = [], error?: any): void {
    if (level < this.logLevel) return;

    const timestamp = new Date().toISOString();
    const formattedMessage = `[${timestamp}] [${LogLevel[level]}] [${this.APP_NAME}] ${message}`;

    switch (level) {
      case LogLevel.Debug:
        if (error) {
          console.debug(formattedMessage, ...args, error);
        } else {
          console.debug(formattedMessage, ...args);
        }
        break;

      case LogLevel.Info:
        console.info(formattedMessage, ...args);
        break;

      case LogLevel.Warn:
        console.warn(formattedMessage, ...args);
        break;

      case LogLevel.Error:
        if (error) {
          console.error(formattedMessage, ...args, '\nError:', error);
          if (error.stack) {
            console.error('Stack trace:', error.stack);
          }
        } else {
          console.error(formattedMessage, ...args);
        }
        break;
    }

    // You could also send logs to a backend service here
    this.sendToBackend(level, message, args, error);
  }

  private sendToBackend(level: LogLevel, message: string, args: any[], error?: any): void {
    // Implementation for sending logs to backend
    // This is where you could integrate with your backend logging system
    // For now, we'll just store in localStorage as an example
    try {
      const logs = JSON.parse(localStorage.getItem('app_logs') || '[]');
      logs.push({
        timestamp: new Date().toISOString(),
        level: LogLevel[level],
        message,
        args: args.map(arg => this.safeStringify(arg)),
        error: error ? this.safeStringify(error) : undefined
      });
      // Keep only last 100 logs
      while (logs.length > 100) logs.shift();
      localStorage.setItem('app_logs', JSON.stringify(logs));
    } catch (e) {
      console.error('Error saving log to storage:', e);
    }
  }

  private safeStringify(obj: any): string {
    try {
      return JSON.stringify(obj);
    } catch (e) {
      return `[Unstringifiable Object: ${typeof obj}]`;
    }
  }

  // Optional: Method to get logs (useful for debugging)
  getLogs(): any[] {
    try {
      return JSON.parse(localStorage.getItem('app_logs') || '[]');
    } catch {
      return [];
    }
  }

  // Optional: Method to clear logs
  clearLogs(): void {
    localStorage.removeItem('app_logs');
  }
}

// Log levels enum
enum LogLevel {
  Debug = 0,
  Info = 1,
  Warn = 2,
  Error = 3
}
