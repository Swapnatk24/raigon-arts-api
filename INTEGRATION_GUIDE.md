# Raigon Arts — End-to-End Connection & Integration Guide (UI + API + Database)

This document provides a step-by-step guide to connecting your **Angular Frontend**, **ASP.NET Core Web API (C#)**, and **PostgreSQL Database** into a cohesive full-stack application.

---

## 1. System Architecture & Data Flow

```
┌────────────────────────────────┐
│   Angular 18/19 Frontend       │   Runs on: http://localhost:4200
│   (Components, Services, UI)   │
└───────────────┬────────────────┘
                │ HTTP Requests (JSON / Multipart)
                │ Headers: Authorization: Bearer <JWT_TOKEN>
                ▼
┌────────────────────────────────┐
│   ASP.NET Core 8 Web API       │   Runs on: http://localhost:3000
│   (Controllers, Services, JWT) │   Base URL: /api/v1
└───────────────┬────────────────┘
                │ Entity Framework Core (Npgsql)
                ▼
┌────────────────────────────────┐
│   PostgreSQL Database          │   Runs on: localhost:5432
│   (raigonarts_db)              │
└────────────────────────────────┘
```

---

## Step 1: PostgreSQL Database Configuration

### 1.1 Start PostgreSQL Service
Ensure your local PostgreSQL server is running on port `5432`.
* On Windows, open **Services** (`services.msc`) and make sure **postgresql-x64-XX** is **Running**.
* Or run in PowerShell / Command Prompt:
  ```powershell
  net start postgresql-x64-16
  ```

### 1.2 Create the Database
Open **pgAdmin** or **psql** terminal:
```sql
CREATE DATABASE raigonarts_db;
```

### 1.3 Configure Connection String in API
In `d:\RAIGON ARTS\RaigonArtsAPI\appsettings.json` and `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=raigonarts_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD",
    "UseInMemoryDatabase": false
  },
  "Jwt": {
    "Key": "RaigonArts_SuperSecretWorkshopSigningKey_2026_SecureJwtAuthToken!",
    "Issuer": "RaigonArtsApi",
    "Audience": "RaigonArtsApp",
    "ExpirySeconds": 86400
  }
}
```

> **Note**: When `UseInMemoryDatabase: false`, EF Core will automatically create all tables and seed initial data into PostgreSQL on first launch via `DbInitializer.cs`.

---

## Step 2: Run & Verify the ASP.NET Core API

### 2.1 Start the API Server
Open a terminal in `d:\RAIGON ARTS\RaigonArtsAPI` and execute:
```powershell
cd "d:\RAIGON ARTS\RaigonArtsAPI"
dotnet run
```

The terminal should output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:3000
info: Microsoft.Hosting.Lifetime[0]
      Application started.
```

### 2.2 Verify CORS in `Program.cs`
The API is already configured to accept requests from Angular (`http://localhost:4200`):
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRaigonClient", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://127.0.0.1:4200",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

### 2.3 Verify in Browser
Open **[http://localhost:3000/swagger](http://localhost:3000/swagger)** to view the interactive OpenAPI documentation.

---

## Step 3: Configure Angular Frontend for API Connection

### 3.1 Define API Environment Endpoint
In your Angular project (`d:\RAIGON ARTS\Raigon\RaigonArts-angular\src\environments\environment.ts`):

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:3000/api/v1'
};
```

### 3.2 Enable `HttpClient` in `app.config.ts`
Ensure `provideHttpClient` with interceptors is registered in `src/app/app.config.ts`:

```typescript
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
```

---

## Step 4: Implement Angular Services & JWT Interceptor

### 4.1 JWT Auth Interceptor (`src/app/interceptors/auth.interceptor.ts`)
Automatically attaches the JWT Bearer token to outgoing HTTP requests:

```typescript
import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('raigon_token');

  if (token) {
    const cloned = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(cloned);
  }

  return next(req);
};
```

---

### 4.2 Generic API Response Interface (`src/app/models/api-response.model.ts`)
```typescript
export interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  data: T;
  timestamp: string;
}

export interface ApiErrorResponse {
  success: false;
  statusCode: number;
  error: string;
  message: string;
  errors?: { field: string; message: string }[];
  timestamp: string;
}
```

---

### 4.3 Authentication Service (`src/app/services/auth.service.ts`)
Handles login, WhatsApp OTP recovery flow, and session persistence:

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface UserProfile {
  id: string;
  username: string;
  displayName: string;
  role: string;
  registeredPhone: string;
}

export interface LoginResult {
  token: string;
  expiresIn: number;
  user: UserProfile;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/auth`;

  login(credentials: { username: string; password: string }): Observable<ApiResponse<LoginResult>> {
    return this.http.post<ApiResponse<LoginResult>>(`${this.baseUrl}/login`, credentials).pipe(
      tap(res => {
        if (res.success && res.data.token) {
          localStorage.setItem('raigon_token', res.data.token);
          localStorage.setItem('raigon_user', JSON.stringify(res.data.user));
        }
      })
    );
  }

  getCurrentUser(): Observable<ApiResponse<UserProfile>> {
    return this.http.get<ApiResponse<UserProfile>>(`${this.baseUrl}/me`);
  }

  sendOtp(phone: string): Observable<ApiResponse<{ sessionId: string; targetPhone: string }>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/forgot-password/send-otp`, { phone });
  }

  verifyOtp(sessionId: string, otpCode: string): Observable<ApiResponse<{ resetToken: string }>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/forgot-password/verify-otp`, { sessionId, otpCode });
  }

  resetPassword(payload: { resetToken: string; newPassword: string; confirmPassword: string }): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/forgot-password/reset-password`, payload);
  }

  logout(): void {
    localStorage.removeItem('raigon_token');
    localStorage.removeItem('raigon_user');
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem('raigon_token');
  }
}
```

---

### 4.4 Dashboard Service (`src/app/services/dashboard.service.ts`)
```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface DashboardStats {
  totalOrdersCount: number;
  totalOrdersGrowth: string;
  inProgressCount: number;
  inProgressStatus: string;
  completedOrdersCount: number;
  completedStatus: string;
  pendingOrdersCount: number;
  pendingStatus: string;
  totalRevenue: number;
  revenueGrowth: string;
  totalCustomersCount: number;
  totalFramesInProduction: number;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/dashboard`;

  getStats(): Observable<ApiResponse<DashboardStats>> {
    return this.http.get<ApiResponse<DashboardStats>>(`${this.baseUrl}/stats`);
  }

  getRecentOrders(limit: number = 10): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/recent-orders?limit=${limit}`);
  }
}
```

---

### 4.5 Customer & Order Services (`src/app/services/order.service.ts`)
```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/orders`;

  getOrders(filters?: { status?: string; paymentStatus?: string; search?: string; page?: number; limit?: number }): Observable<ApiResponse<any>> {
    let params = new HttpParams();
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.paymentStatus) params = params.set('paymentStatus', filters.paymentStatus);
    if (filters?.search) params = params.set('search', filters.search);
    if (filters?.page) params = params.set('page', filters.page.toString());
    if (filters?.limit) params = params.set('limit', filters.limit.toString());

    return this.http.get<ApiResponse<any>>(this.baseUrl, { params });
  }

  createOrder(payload: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(this.baseUrl, payload);
  }

  updateOrderStatus(id: string, status: string, remarks?: string): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.baseUrl}/${id}/status`, { orderStatus: status, remarks });
  }

  updateOrder(id: string, payload: any): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.baseUrl}/${id}`, payload);
  }

  deleteOrder(id: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.baseUrl}/${id}`);
  }
}
```

---

## Step 5: End-to-End Verification Workflow

1. **Start Backend**:
   ```powershell
   cd "d:\RAIGON ARTS\RaigonArtsAPI"
   dotnet run
   ```
2. **Start Frontend**:
   ```powershell
   cd "d:\RAIGON ARTS\Raigon\RaigonArts-angular"
   ng serve
   ```
3. **Open Application**: Navigate to `http://localhost:4200`
4. **Log In**:
   - Username: `admin@raigonarts.com`
   - Password: `raigon@2026`
5. **Verify**:
   - Dashboard KPI metric cards show live data.
   - Orders table loads from `/api/v1/orders`.
   - New order modal successfully submits to `/api/v1/orders` and persists data in PostgreSQL.

---

## Troubleshooting Common Issues

| Issue | Cause | Solution |
|---|---|---|
| **CORS policy error** in browser console | Origin not permitted | Ensure `http://localhost:4200` is listed in `Program.cs` CORS policy. |
| **Password authentication failed for user "postgres"** | Incorrect DB password in `appsettings.json` | Update the `Password=` in `DefaultConnection` to match your local PostgreSQL password. |
| **401 Unauthorized** on API calls | Missing or expired JWT token | Ensure `authInterceptor` is registered and `raigon_token` is present in `localStorage`. |
| **Cannot GET /api/v1/...** | API server not running or wrong port | Verify `dotnet run` is listening on `http://localhost:3000`. |
