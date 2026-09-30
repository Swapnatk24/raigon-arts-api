# Raigon Arts — REST API Specification & Complete Documentation

> **System**: Raigon Arts Custom Photo Framing & Customer Management System  
> **Base URL**: `http://localhost:3000/api/v1` (Development) / `https://api.raigonarts.com/api/v1` (Production)  
> **Content-Type**: `application/json`  
> **Authentication**: Bearer Token via `Authorization: Bearer <JWT_TOKEN>`  
> **API Version**: `v2.4.0`

---

## Table of Contents
1. [Architecture & Global Standards](#1-architecture--global-standards)
2. [Authentication & Account Recovery APIs](#2-authentication--account-recovery-apis)
3. [Dashboard & Statistics APIs](#3-dashboard--statistics-apis)
4. [Customer Management APIs](#4-customer-management-apis)
5. [Order Management APIs](#5-order-management-apis)
6. [Photo Gallery & Upload APIs](#6-photo-gallery--upload-apis)
7. [Frame Size Management APIs](#7-frame-size-management-apis)
8. [Financial & Production Reports APIs](#8-financial--production-reports-apis)
9. [Workshop & System Settings APIs](#9-workshop--system-settings-apis)
10. [Notifications APIs](#10-notifications-apis)
11. [Data Backup & Restore APIs](#11-data-backup--restore-apis)
12. [Global Error Codes & HTTP Status Mapping](#12-global-error-codes--http-status-mapping)

---

## 1. Architecture & Global Standards

### 1.1 Global Success Response Wrapper
Every successful response returns HTTP `200` (or `201 Created`) with a standard JSON envelope:

```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": {},
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

### 1.2 Global Error Response Wrapper
When a request fails, the API responds with a structured error envelope:

```json
{
  "success": false,
  "statusCode": 400,
  "error": "BAD_REQUEST",
  "message": "Validation failed on input fields.",
  "errors": [
    {
      "field": "phone",
      "message": "Phone number must be a valid 10-digit Indian phone number."
    }
  ],
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

## 2. Authentication & Account Recovery APIs

### 2.1 Sign In / Staff Login
Authenticates workshop staff or manager and returns a signed JWT token.

* **Method**: `POST`
* **Endpoint**: `/auth/login`
* **Access**: Public

#### Request Headers
```http
Content-Type: application/json
```

#### Request Body
```json
{
  "username": "admin@raigonarts.com",
  "password": "raigon@2026"
}
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Authentication successful.",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 86400,
    "user": {
      "id": "usr_01",
      "username": "admin",
      "displayName": "Workshop Manager",
      "role": "ADMIN",
      "registeredPhone": "+91 7902261255"
    }
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

#### Response Body (`401 Unauthorized`)
```json
{
  "success": false,
  "statusCode": 401,
  "error": "INVALID_CREDENTIALS",
  "message": "Invalid username or password. Please try again.",
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 2.2 Send WhatsApp OTP for Password Recovery (Step 1)
Dispatches a 4-digit verification code to the registered workshop manager phone.

* **Method**: `POST`
* **Endpoint**: `/auth/forgot-password/send-otp`
* **Access**: Public

#### Request Body
```json
{
  "phone": "+91 7902261255"
}
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "4-digit OTP has been sent via WhatsApp to +91 7902261255.",
  "data": {
    "sessionId": "otp_sess_9a8b7c6d5e",
    "targetPhone": "+91 7902261255",
    "expiresInSeconds": 80,
    "resendAvailableInSeconds": 80
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 2.3 Verify WhatsApp OTP (Step 2)
Validates the 4-digit code and issues a temporary password reset token.

* **Method**: `POST`
* **Endpoint**: `/auth/forgot-password/verify-otp`
* **Access**: Public

#### Request Body
```json
{
  "sessionId": "otp_sess_9a8b7c6d5e",
  "otpCode": "4829"
}
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "OTP verified successfully. Proceed to set new password.",
  "data": {
    "resetToken": "rst_tok_a1b2c3d4e5f6g7h8",
    "expiresInSeconds": 600
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

#### Response Body (`400 Bad Request`)
```json
{
  "success": false,
  "statusCode": 400,
  "error": "INVALID_OTP",
  "message": "The verification code entered is invalid or has expired.",
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 2.4 Submit New Password (Step 3)
Commits the new password using the verified reset token.

* **Method**: `POST`
* **Endpoint**: `/auth/forgot-password/reset-password`
* **Access**: Public

#### Request Body
```json
{
  "resetToken": "rst_tok_a1b2c3d4e5f6g7h8",
  "newPassword": "newSecurePassword2026",
  "confirmPassword": "newSecurePassword2026"
}
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Password updated successfully. Please log in with your new credentials.",
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 2.5 Current Authenticated User Info
Fetches the profile and permission claims of the authenticated user.

* **Method**: `GET`
* **Endpoint**: `/auth/me`
* **Access**: Authenticated (`Bearer <JWT_TOKEN>`)

#### Request Headers
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "User details fetched successfully.",
  "data": {
    "userId": "usr_01",
    "username": "admin",
    "displayName": "Workshop Manager",
    "registeredPhone": "+91 7902261255",
    "role": "ADMIN",
    "permissions": ["READ", "WRITE", "DELETE", "EXPORT", "SETTINGS"]
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

## 3. Dashboard & Statistics APIs

### 3.1 Get Dashboard Metric Summary
Returns KPI card statistics for orders, revenue, active workshop counts, and customer volume.

* **Method**: `GET`
* **Endpoint**: `/dashboard/stats`

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": {
    "totalOrdersCount": 42,
    "totalOrdersGrowth": "+12%",
    "inProgressCount": 16,
    "inProgressStatus": "Active in workshop",
    "completedOrdersCount": 24,
    "completedStatus": "Ready for delivery",
    "pendingOrdersCount": 2,
    "pendingStatus": "Urgent action needed",
    "totalRevenue": 142500,
    "revenueGrowth": "+28%",
    "totalCustomersCount": 38,
    "totalFramesInProduction": 58
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 3.2 Get Recent Customer Frame Orders
Returns the latest framing orders for the dashboard activity table.

* **Method**: `GET`
* **Endpoint**: `/dashboard/recent-orders`
* **Query Parameters**:
  * `limit` *(integer, optional, default: 10)*

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": [
    {
      "id": "ord_1006",
      "orderNumber": "RA-1006",
      "customerId": "cust_106",
      "customerName": "Arun Kumar",
      "customerPhone": "+91 7902261255",
      "customerCity": "Trivandrum",
      "frameSize": "12 × 18 inch",
      "frameType": "Wooden Frame",
      "quantity": 1,
      "totalAmount": 2500,
      "advancePaid": 1000,
      "balanceAmount": 1500,
      "orderStatus": "In Progress",
      "paymentStatus": "Partial",
      "orderDate": "2026-09-02",
      "deliveryDate": "2026-09-09"
    }
  ],
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

## 4. Customer Management APIs

### 4.1 List All Customers
Retrieves paginated customers with search filtering and aggregate stats.

* **Method**: `GET`
* **Endpoint**: `/customers`
* **Query Parameters**:
  * `search` *(string, optional)* — Filter by name, phone, city, address
  * `page` *(integer, optional, default: 1)*
  * `limit` *(integer, optional, default: 50)*

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": {
    "total": 4,
    "page": 1,
    "limit": 50,
    "customers": [
      {
        "id": "cust_101",
        "name": "Arun Kumar",
        "phone": "+91 7902261255",
        "altPhone": "+91 9447000000",
        "city": "Trivandrum",
        "address": "Villa 42, Palm Meadows, Kowdiar",
        "pincode": "695003",
        "createdAt": "2026-08-20T10:00:00Z",
        "totalOrdersCount": 2,
        "totalSpent": 4800
      }
    ]
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 4.2 Create New Customer
* **Method**: `POST`
* **Endpoint**: `/customers`

#### Request Body
```json
{
  "name": "Arun Kumar",
  "phone": "+91 7012160065",
  "altPhone": "+91 9447000000",
  "city": "Trivandrum",
  "address": "Villa 42, Palm Meadows, Kowdiar",
  "pincode": "695003"
}
```

#### Response Body (`201 Created`)
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Customer profile created successfully.",
  "data": {
    "id": "cust_107",
    "name": "Arun Kumar",
    "phone": "+91 7012160065",
    "altPhone": "+91 9447000000",
    "city": "Trivandrum",
    "address": "Villa 42, Palm Meadows, Kowdiar",
    "pincode": "695003",
    "createdAt": "2026-09-03T11:05:00.000Z",
    "totalOrdersCount": 0,
    "totalSpent": 0
  },
  "timestamp": "2026-09-03T11:05:00.000Z"
}
```

---

### 4.3 Get Customer Profile & Order History
* **Method**: `GET`
* **Endpoint**: `/customers/:id`

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": {
    "customer": {
      "id": "cust_101",
      "name": "Arun Kumar",
      "phone": "+91 7012160065",
      "altPhone": "+91 9447000000",
      "city": "Trivandrum",
      "address": "Villa 42, Palm Meadows, Kowdiar",
      "pincode": "695003",
      "createdAt": "2026-08-20T10:00:00Z",
      "totalOrdersCount": 2,
      "totalSpent": 4800
    },
    "orders": [
      {
        "id": "ord_1001",
        "orderNumber": "RA-1001",
        "orderDate": "2026-08-28",
        "deliveryDate": "2026-09-05",
        "totalAmount": 4500,
        "advancePaid": 2000,
        "balanceAmount": 2500,
        "paymentStatus": "Partial",
        "orderStatus": "In Progress",
        "photos": [
          {
            "id": "p1",
            "photoUrl": "https://assets.raigonarts.com/photos/family_kowdiar.jpg",
            "photoName": "Family_Portrait_Kowdiar.jpg",
            "frameSize": "12 × 18 inch",
            "frameType": "Wooden Frame"
          }
        ]
      }
    ]
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 4.4 Update Customer Profile
* **Method**: `PUT`
* **Endpoint**: `/customers/:id`

#### Request Body
```json
{
  "name": "Arun Kumar Kowdiar",
  "phone": "+91 7902261255",
  "altPhone": "+91 9447112233",
  "city": "Trivandrum",
  "address": "Villa 42, Palm Meadows Phase 2, Kowdiar",
  "pincode": "695003"
}
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Customer profile updated successfully.",
  "data": {
    "id": "cust_101",
    "name": "Arun Kumar Kowdiar",
    "phone": "+91 7902261255",
    "altPhone": "+91 9447112233",
    "city": "Trivandrum",
    "address": "Villa 42, Palm Meadows Phase 2, Kowdiar",
    "pincode": "695003",
    "createdAt": "2026-08-20T10:00:00Z",
    "totalOrdersCount": 2,
    "totalSpent": 4800
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 4.5 Delete Customer
* **Method**: `DELETE`
* **Endpoint**: `/customers/:id`

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Customer profile and associated references deleted successfully.",
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

## 5. Order Management APIs

### 5.1 List All Workshop Orders
* **Method**: `GET`
* **Endpoint**: `/orders`
* **Query Parameters**:
  * `status` *(string, optional)* — `All`, `In Progress`, `Pending`, `Completed`, `Cancelled`
  * `paymentStatus` *(string, optional)* — `Paid`, `Partial`, `Unpaid`
  * `search` *(string, optional)* — Order number, customer name, phone
  * `page` *(integer, optional)*
  * `limit` *(integer, optional)*

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": {
    "total": 6,
    "orders": [
      {
        "id": "ord_1006",
        "orderNumber": "RA-1006",
        "customerId": "cust_106",
        "customerName": "Arun Kumar",
        "customerPhone": "+91 7902261255",
        "customerCity": "Trivandrum",
        "orderDate": "2026-09-02",
        "deliveryDate": "2026-09-09",
        "totalAmount": 2500,
        "advancePaid": 1000,
        "balanceAmount": 1500,
        "paymentStatus": "Partial",
        "orderStatus": "In Progress",
        "configMode": "same",
        "photos": [
          {
            "id": "p6",
            "photoUrl": "https://assets.raigonarts.com/photos/gallery_memory.jpg",
            "photoName": "Gallery_Memory.jpg",
            "frameSize": "12 × 18 inch",
            "unit": "inch",
            "frameType": "Wooden Frame",
            "frameMaterial": "Teak Wood Moulding",
            "frameColor": "Walnut Brown",
            "orientation": "Landscape",
            "quantity": 1
          }
        ],
        "commonSpecs": {
          "frameSize": "12 × 18 inch",
          "unit": "inch",
          "customWidth": null,
          "customHeight": null,
          "frameType": "Wooden Frame",
          "frameMaterial": "Teak Wood Moulding",
          "frameColor": "Walnut Brown",
          "orientation": "Landscape",
          "quantity": 1,
          "notes": "Anti-glare glass coating with back mounting hook"
        },
        "createdAt": "2026-09-02T10:00:00Z"
      }
    ]
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 5.2 Create Customer & Frame Order (Combined Modal Submission)
Creates or links a customer profile, generates a unique order number (`#RA-1007`), calculates balance amounts, and registers photo configurations.

* **Method**: `POST`
* **Endpoint**: `/orders`

#### Request Body
```json
{
  "customer": {
    "id": null,
    "name": "Siddharth Menon",
    "phone": "+91 9447112233",
    "altPhone": "+91 9847001122",
    "city": "Trivandrum",
    "address": "Flat 3C, Royal Palms, Sasthamangalam",
    "pincode": "695010"
  },
  "order": {
    "configMode": "same",
    "orderDate": "2026-09-03",
    "deliveryDate": "2026-09-12",
    "totalAmount": 3800,
    "advancePaid": 1500,
    "paymentStatus": "Partial",
    "orderStatus": "In Progress",
    "commonSpecs": {
      "frameSize": "12 × 18 inch",
      "unit": "inch",
      "customWidth": null,
      "customHeight": null,
      "frameType": "Wooden Frame",
      "frameMaterial": "Teak Wood Moulding",
      "frameColor": "Walnut Brown",
      "orientation": "Landscape",
      "quantity": 2,
      "notes": "Clear float glass, white border"
    },
    "photos": [
      {
        "photoUrl": "https://assets.raigonarts.com/photos/wedding_01.jpg",
        "photoName": "Wedding_Reception_01.jpg",
        "frameSize": "12 × 18 inch",
        "unit": "inch",
        "frameType": "Wooden Frame",
        "frameMaterial": "Teak Wood Moulding",
        "frameColor": "Walnut Brown",
        "orientation": "Landscape",
        "quantity": 1
      }
    ]
  }
}
```

#### Response Body (`201 Created`)
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Customer profile and frame order #RA-1007 created successfully.",
  "data": {
    "orderId": "ord_1007",
    "orderNumber": "RA-1007",
    "customerId": "cust_108",
    "customerName": "Siddharth Menon",
    "totalAmount": 3800,
    "advancePaid": 1500,
    "balanceAmount": 2300,
    "orderStatus": "In Progress",
    "paymentStatus": "Partial",
    "deliveryDate": "2026-09-12"
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 5.3 Update Order Status
* **Method**: `PATCH`
* **Endpoint**: `/orders/:id/status`

#### Request Body
```json
{
  "orderStatus": "Completed",
  "remarks": "Framing inspection passed, packed for client pickup."
}
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Order status updated to 'Completed'.",
  "data": {
    "id": "ord_1006",
    "orderNumber": "RA-1006",
    "orderStatus": "Completed",
    "updatedAt": "2026-09-03T11:15:00.000Z"
  },
  "timestamp": "2026-09-03T11:15:00.000Z"
}
```

---

### 5.4 Update Order & Payment Details
* **Method**: `PUT`
* **Endpoint**: `/orders/:id`

#### Request Body
```json
{
  "totalAmount": 2500,
  "advancePaid": 2500,
  "paymentStatus": "Paid",
  "orderStatus": "Completed",
  "deliveryDate": "2026-09-08",
  "remarks": "Final payment received via UPI"
}
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Order #RA-1006 updated successfully.",
  "data": {
    "id": "ord_1006",
    "orderNumber": "RA-1006",
    "totalAmount": 2500,
    "advancePaid": 2500,
    "balanceAmount": 0,
    "paymentStatus": "Paid",
    "orderStatus": "Completed"
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 5.5 Delete Order
* **Method**: `DELETE`
* **Endpoint**: `/orders/:id`

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Order #RA-1006 and associated attachments removed.",
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

## 6. Photo Gallery & Upload APIs

### 6.1 List Photo Collection
* **Method**: `GET`
* **Endpoint**: `/photos`
* **Query Parameters**:
  * `orientation` *(string, optional)* — `All`, `Landscape`, `Portrait`, `Square`
  * `search` *(string, optional)* — Filter by photo name or customer name

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": [
    {
      "id": "p1",
      "orderId": "RA-1001",
      "customerId": "cust_101",
      "customerName": "Arun Kumar",
      "photoName": "Family_Portrait_Kowdiar.jpg",
      "photoUrl": "https://assets.raigonarts.com/photos/family_kowdiar.jpg",
      "frameSize": "12 × 18 inch",
      "orientation": "Landscape",
      "uploadedAt": "2026-08-28T10:30:00Z"
    }
  ],
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 6.2 Multipart Photo File Upload
* **Method**: `POST`
* **Endpoint**: `/photos/upload`
* **Content-Type**: `multipart/form-data`

#### Form Fields
* `photos`: Binary image files (`.jpg`, `.jpeg`, `.png`, `.webp`)
* `orderId` *(optional)*: String

#### Response Body (`201 Created`)
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Photos uploaded successfully.",
  "data": [
    {
      "id": "p_upl_912",
      "photoName": "HighRes_Print_01.jpg",
      "photoUrl": "http://localhost:3000/uploads/2026/09/HighRes_Print_01_a1b2c3d4.jpg",
      "fileSizeBytes": 4829104,
      "dimensions": {
        "width": 3600,
        "height": 2400
      },
      "mimeType": "image/jpeg"
    }
  ],
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

## 7. Frame Size Management APIs

### 7.1 List All Frame Sizes
* **Method**: `GET`
* **Endpoint**: `/frames`
* **Query Parameters**:
  * `search` *(string, optional)* — Filter by size code, name, category, or unit

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": [
    {
      "id": "f1",
      "code": "FS-01",
      "name": "4 × 6 inch",
      "width": 4,
      "height": 6,
      "unit": "inch",
      "category": "Standard Photo",
      "activeOrdersCount": 142,
      "status": "Active"
    },
    {
      "id": "f5",
      "code": "FS-05",
      "name": "12 × 18 inch",
      "width": 12,
      "height": 18,
      "unit": "inch",
      "category": "Large Gallery",
      "activeOrdersCount": 455,
      "status": "Active"
    }
  ],
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 7.2 Create New Frame Size
* **Method**: `POST`
* **Endpoint**: `/frames`

#### Request Body
```json
{
  "code": "FS-08",
  "name": "24 × 36 inch",
  "width": 24,
  "height": 36,
  "unit": "inch",
  "category": "Exhibition Wall Art"
}
```

#### Response Body (`201 Created`)
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Frame size '24 × 36 inch' created.",
  "data": {
    "id": "f_size_08",
    "code": "FS-08",
    "name": "24 × 36 inch",
    "width": 24,
    "height": 36,
    "unit": "inch",
    "category": "Exhibition Wall Art",
    "activeOrdersCount": 0,
    "status": "Active"
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 7.3 Update Frame Size
* **Method**: `PUT`
* **Endpoint**: `/frames/:id`

#### Request Body
```json
{
  "code": "FS-05",
  "name": "12 × 18 inch",
  "width": 12,
  "height": 18,
  "unit": "inch",
  "category": "Large Gallery",
  "status": "Active"
}
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Frame size updated successfully.",
  "data": {
    "id": "f5",
    "code": "FS-05",
    "name": "12 × 18 inch",
    "width": 12,
    "height": 18,
    "unit": "inch",
    "category": "Large Gallery",
    "activeOrdersCount": 455,
    "status": "Active"
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 7.4 Delete Frame Size
* **Method**: `DELETE`
* **Endpoint**: `/frames/:id`

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Frame size deleted successfully.",
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

## 8. Financial & Production Reports APIs

### 8.1 Report Cards & Key Metrics Summary
Returns the real aggregated metrics calculated from PostgreSQL orders for the Reports cards (Total Revenue, Advance Collected, Outstanding Balance, Highest Order of the Day, Total Orders).

* **Method**: `GET`
* **Endpoint**: `/reports/cards` (or `/reports/summary`)

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": {
    "totalRevenue": 142500,
    "advanceCollected": 98200,
    "outstandingBalance": 44300,
    "highestOrderOfDay": 7200.00,
    "hod": 7200.00,
    "totalOrders": 42
  },
  "timestamp": "2026-09-20T11:00:00.000Z"
}
```

---

### 8.2 Financial Metrics & Summary
* **Method**: `GET`
* **Endpoint**: `/reports/financials`

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": {
    "totalBilled": 142500,
    "totalCollected": 98200,
    "totalOutstanding": 44300,
    "highestOrderOfDay": 7200.00,
    "hod": 7200.00,
    "totalOrders": 42,
    "settlementStats": {
      "paidCount": 24,
      "paidPercentage": 57,
      "partialCount": 16,
      "partialPercentage": 38,
      "unpaidCount": 2,
      "unpaidPercentage": 5
    }
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 8.2 Production Moulding Breakdown
* **Method**: `GET`
* **Endpoint**: `/reports/production-breakdown`

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": [
    {
      "type": "Wooden Frame",
      "count": 137,
      "percentage": 63
    },
    {
      "type": "Premium Frame",
      "count": 37,
      "percentage": 17
    },
    {
      "type": "Canvas Float",
      "count": 22,
      "percentage": 10
    },
    {
      "type": "Box Frame",
      "count": 13,
      "percentage": 6
    },
    {
      "type": "Tabletop Frame",
      "count": 7,
      "percentage": 3
    },
    {
      "type": "Classic Frame",
      "count": 2,
      "percentage": 1
    }
  ],
  "timestamp": "2026-09-29T12:00:00.000Z"
}
```

---

### 8.3 Top Frame Sizes Sold Breakdown
Returns the real sales volume breakdown and percentage calculated from PostgreSQL order items.

* **Method**: `GET`
* **Endpoint**: `/reports/top-frame-sizes`
* **Query Parameters**: `limit` (optional integer)

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": [
    {
      "size": "4 × 6 inch (Standard Photo)",
      "count": 46,
      "percentage": 21
    },
    {
      "size": "12 × 18 inch (Large Gallery)",
      "count": 36,
      "percentage": 17
    },
    {
      "size": "Custom Sizes",
      "count": 29,
      "percentage": 13
    },
    {
      "size": "5 × 10 inch (Custom)",
      "count": 25,
      "percentage": 11
    },
    {
      "size": "8 × 12 inch (Medium Portrait)",
      "count": 20,
      "percentage": 9
    },
    {
      "size": "16 × 20 inch (Wholesale Gallery)",
      "count": 14,
      "percentage": 6
    }
  ],
  "timestamp": "2026-09-29T12:00:00.000Z"
}
```

---

### 8.4 Popular Material Types Breakdown
Returns the real popular material types and percentages calculated from PostgreSQL order items.

* **Method**: `GET`
* **Endpoint**: `/reports/popular-materials`
* **Query Parameters**: `limit` (optional integer)

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": [
    {
      "material": "Teak Wood Moulding",
      "count": 103,
      "percentage": 47
    },
    {
      "material": "Gold Filigree Resin",
      "count": 38,
      "percentage": 17
    },
    {
      "material": "Standard Moulding",
      "count": 23,
      "percentage": 11
    },
    {
      "material": "Oak Wood",
      "count": 20,
      "percentage": 9
    },
    {
      "material": "Matte Black Aluminum",
      "count": 12,
      "percentage": 6
    }
  ],
  "timestamp": "2026-09-29T12:00:00.000Z"
}
```

---

### 8.5 Production Analytics & Breakdown Summary
Returns composite breakdown metrics including top frame sizes, popular materials, and frame types.

* **Method**: `GET`
* **Endpoint**: `/reports/production-analytics` (or `/reports/breakdown`)

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": {
    "topFrameSizes": [
      {
        "size": "4 × 6 inch (Standard Photo)",
        "count": 46,
        "percentage": 21
      },
      {
        "size": "12 × 18 inch (Large Gallery)",
        "count": 36,
        "percentage": 17
      }
    ],
    "popularMaterials": [
      {
        "material": "Teak Wood Moulding",
        "count": 103,
        "percentage": 47
      }
    ],
    "frameTypes": [
      {
        "type": "Wooden Frame",
        "count": 137,
        "percentage": 63
      }
    ],
    "totalVolume": 218
  },
  "timestamp": "2026-09-29T12:00:00.000Z"
}
```

---

### 8.6 Export Financial Report (CSV Stream)
Streams orders report directly formatted as standard comma-separated values (`text/csv`).

* **Method**: `GET`
* **Endpoint**: `/reports/export-csv`

#### Request Headers
```http
Accept: text/csv
```

#### Response (`200 OK` — `Content-Type: text/csv; charset=utf-8`)
```csv
Order Number,Customer Name,Phone,City,Order Date,Delivery Date,Total Amount,Advance Paid,Balance,Payment Status,Order Status
"RA-1006","Arun Kumar","+91 7902261255","Trivandrum","2026-09-02","2026-09-09",2500,1000,1500,"Partial","In Progress"
"RA-1001","Meera Nair","+91 9847123456","Kochi","2026-08-28","2026-09-05",4500,0,4500,"Unpaid","Cancelled"
"RA-1003","Rahul Raj","+91 9446554433","Kollam","2026-08-30","2026-09-02",12000,12000,0,"Paid","Completed"
```

---

## 9. Workshop & System Settings APIs

### 9.1 Get Workshop Settings
* **Method**: `GET`
* **Endpoint**: `/settings`

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": {
    "workshopName": "Raigon Arts",
    "subtitle": "Custom Photo Framing & Studio Workshop",
    "phone": "+91 7902261255",
    "whatsappPhone": "+91 7902261255",
    "address": "Workshop St, Art District, Trivandrum, Kerala 695001",
    "currency": "₹",
    "adminUsername": "admin",
    "taxRate": 5,
    "registeredPhone": "+91 7902261255"
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 9.2 Update Workshop Settings
* **Method**: `PUT`
* **Endpoint**: `/settings`

#### Request Body
```json
{
  "workshopName": "Raigon Arts Workshop",
  "subtitle": "Custom Frame Moulding & Gallery Framing",
  "phone": "+91 7902261255",
  "whatsappPhone": "+91 7902261255",
  "address": "Workshop St, Art District, Kowdiar, Trivandrum, Kerala 695003",
  "adminUsername": "admin",
  "registeredPhone": "+91 7902261255",
  "taxRate": 5,
  "currency": "₹"
}
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Workshop settings saved successfully.",
  "data": {
    "workshopName": "Raigon Arts Workshop",
    "subtitle": "Custom Frame Moulding & Gallery Framing",
    "phone": "+91 7902261255",
    "whatsappPhone": "+91 7902261255",
    "address": "Workshop St, Art District, Kowdiar, Trivandrum, Kerala 695003",
    "adminUsername": "admin",
    "registeredPhone": "+91 7902261255",
    "taxRate": 5,
    "currency": "₹"
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

## 10. Notifications APIs

### 10.1 List Notifications
* **Method**: `GET`
* **Endpoint**: `/notifications`

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Operation executed successfully.",
  "data": {
    "unreadCount": 2,
    "notifications": [
      {
        "id": "n1",
        "title": "New Order Received",
        "message": "Order #RA-1006 created for Arun Kumar",
        "time": "10m ago",
        "isRead": false,
        "type": "order"
      },
      {
        "id": "n2",
        "title": "Payment Updated",
        "message": "Advance paid ₹1000 for Order #RA-1006",
        "time": "1h ago",
        "isRead": false,
        "type": "order"
      },
      {
        "id": "n3",
        "title": "Customer Profile Created",
        "message": "Ananya Sreedhar added from Calicut",
        "time": "2h ago",
        "isRead": true,
        "type": "customer"
      }
    ]
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

### 10.2 Mark All Notifications as Read
* **Method**: `PATCH`
* **Endpoint**: `/notifications/mark-all-read`

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "All notifications marked as read.",
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

## 11. Data Backup & Restore APIs

### 11.1 Export Complete Database (JSON Backup)
* **Method**: `GET`
* **Endpoint**: `/backup/export`

#### Response Body (`200 OK` — `application/json`)
```json
{
  "backupVersion": "1.0",
  "exportedAt": "2026-09-03T11:00:00.000Z",
  "workshop": "Raigon Arts",
  "customers": [
    {
      "id": "cust_101",
      "name": "Arun Kumar",
      "phone": "+91 7902261255",
      "altPhone": "+91 9447000000",
      "city": "Trivandrum",
      "address": "Villa 42, Palm Meadows, Kowdiar",
      "pincode": "695003",
      "createdAt": "2026-08-20T10:00:00Z"
    }
  ],
  "orders": [
    {
      "id": "ord_1001",
      "orderNumber": "RA-1001",
      "customerId": "cust_102",
      "orderDate": "2026-08-28T00:00:00Z",
      "deliveryDate": "2026-09-05T00:00:00Z",
      "totalAmount": 4500,
      "advancePaid": 0,
      "balanceAmount": 4500,
      "paymentStatus": "Unpaid",
      "orderStatus": "Cancelled",
      "configMode": "same",
      "commonSpecsJson": "{\"frameSize\":\"12 × 18 inch\"}",
      "photos": []
    }
  ],
  "frames": [
    {
      "id": "f1",
      "code": "FS-01",
      "name": "4 × 6 inch",
      "width": 4,
      "height": 6,
      "unit": "inch",
      "category": "Standard Photo",
      "activeOrdersCount": 142,
      "status": "Active"
    }
  ],
  "settings": {
    "id": 1,
    "workshopName": "Raigon Arts",
    "subtitle": "Custom Photo Framing & Studio Workshop",
    "phone": "+91 7012160065",
    "whatsappPhone": "+91 7012160065",
    "address": "Workshop St, Art District, Trivandrum, Kerala 695001",
    "currency": "₹",
    "adminUsername": "admin",
    "taxRate": 5,
    "registeredPhone": "+91 7012160065"
  }
}
```

---

### 11.2 Restore Database Backup
* **Method**: `POST`
* **Endpoint**: `/backup/restore`

#### Request Body
```json
{
  "backupVersion": "1.0",
  "customers": [ /* Customer array */ ],
  "orders": [ /* Order array */ ],
  "frames": [ /* Frame array */ ],
  "settings": { /* Workshop settings */ }
}
```

#### Response Body (`200 OK`)
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Database successfully restored from JSON backup.",
  "data": {
    "restoredCustomers": 4,
    "restoredOrders": 6,
    "restoredFrames": 7
  },
  "timestamp": "2026-09-03T11:00:00.000Z"
}
```

---

## 12. Global Error Codes & HTTP Status Mapping

| HTTP Status | Error Code | Description | Typical Scenario |
|---|---|---|---|
| `400` | `BAD_REQUEST` | Validation error on payload fields. | Missing required phone number or invalid email/string length. |
| `400` | `INVALID_OTP` | The provided WhatsApp OTP code is incorrect or expired. | User entered wrong 4-digit OTP or OTP expired after 80 seconds. |
| `401` | `UNAUTHORIZED` | Bearer token is missing, malformed, or expired. | Client accessed protected `/auth/me` without valid JWT. |
| `401` | `INVALID_CREDENTIALS` | Invalid username or password on `/auth/login`. | Wrong username or password entered. |
| `403` | `FORBIDDEN` | Insufficient permissions for the requested operation. | User does not have the required role or permission. |
| `404` | `NOT_FOUND` | The specified entity (Customer, Order, Frame) does not exist. | Order ID or Customer ID not present in database. |
| `409` | `CONFLICT` | Resource already exists. | Duplicate phone number or duplicated frame code. |
| `500` | `INTERNAL_SERVER_ERROR` | Unhandled server exception occurred. | Uncaught database or runtime exception. |
