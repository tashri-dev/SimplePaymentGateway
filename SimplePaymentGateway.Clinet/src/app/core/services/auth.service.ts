import { Injectable } from "@angular/core";
import { Router } from '@angular/router';
import { UserInfo } from "../models/auth.model";
import { BehaviorSubject } from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly STORAGE_KEY = 'auth_token';
  private readonly DUMMY_USERS = [
    { username: 'admin', password: 'admin123', role: 'admin' },
    { username: 'user', password: 'user123', role: 'user' }
  ];

  private userSubject = new BehaviorSubject<UserInfo | null>(null);
  user$ = this.userSubject.asObservable();

  constructor(private router: Router) {
    // Check for existing token on startup
    const token = localStorage.getItem(this.STORAGE_KEY);
    if (token) {
      const user = this.getUserFromToken(token);
      this.userSubject.next(user);
    }
  }

  async login(username: string, password: string): Promise<boolean> {
    // Simulate API call
    const user = this.DUMMY_USERS.find(u =>
      u.username === username && u.password === password);

    if (user) {
      const token = this.generateToken(user);
      localStorage.setItem(this.STORAGE_KEY, token);
      this.userSubject.next({ username: user.username, role: user.role });
      return true;
    }
    return false;
  }

  logout(): void {
    localStorage.removeItem(this.STORAGE_KEY);
    this.userSubject.next(null);
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    return this.userSubject.value !== null;
  }

  private generateToken(user: any): string {
    // Simple token generation (in real app, use proper JWT)
    return btoa(JSON.stringify({
      username: user.username,
      role: user.role,
      exp: Date.now() + 3600000 // 1 hour
    }));
  }

  private getUserFromToken(token: string): UserInfo | null {
    try {
      const decoded = JSON.parse(atob(token));
      if (decoded.exp < Date.now()) {
        localStorage.removeItem(this.STORAGE_KEY);
        return null;
      }
      return {
        username: decoded.username,
        role: decoded.role
      };
    } catch {
      return null;
    }
  }
}
