# FirstLend Backend Implementation Documentation

## Table of Contents
1. [Authentication System (Login & Register)](#authentication-system)
2. [Database Schema](#database-schema)
3. [API Endpoints](#api-endpoints)
4. [Security Requirements](#security-requirements)
5. [Frontend Integration](#frontend-integration)

---

## Authentication System

### Overview
The authentication system supports two user types:
1. **Customer** - Regular users applying for loans and managing their loan applications
2. **Admin** - Administrative users managing the system, approvals, and reports

### User Registration Flow

#### Registration Endpoint: `POST /auth/register`

**Request Body:**
```json
{
  "fullName": "string (required)",
  "email": "string (required, must be valid email)",
  "phone": "string (required, phone format)",
  "password": "string (required, min 8 characters, must include uppercase, lowercase, number, special char)"
}
```

**Response Success (201 Created):**
```json
{
  "success": true,
  "message": "Registration successful. Please log in.",
  "data": {
    "userId": "uuid",
    "email": "user@example.com",
    "fullName": "John Doe",
    "userType": "customer"
  }
}
```

**Response Error Examples:**
```json
{
  "success": false,
  "message": "Email already registered",
  "code": "EMAIL_EXISTS"
}
```

```json
{
  "success": false,
  "message": "Validation error",
  "code": "VALIDATION_ERROR",
  "errors": [
    {
      "field": "password",
      "message": "Password must contain at least one uppercase letter"
    }
  ]
}
```

**Backend Responsibilities:**
1. Validate all input fields (format, length, required fields)
2. Check if email is already registered
3. Hash password using bcrypt (min 10 salt rounds)
4. Create user record in database with `userType: 'customer'`
5. Send verification email (optional but recommended)
6. Store registration timestamp
7. Return success response without sensitive data

**Database Fields to Store:**
- userId (UUID, primary key)
- fullName (string)
- email (string, unique index)
- phone (string, unique index)
- passwordHash (string)
- userType (enum: 'customer', 'admin')
- createdAt (timestamp)
- updatedAt (timestamp)
- emailVerified (boolean, default: false)
- status (enum: 'active', 'inactive', 'suspended')

---

### User Login Flow

#### Login Endpoint: `POST /auth/login`

**Request Body:**
```json
{
  "emailOrUsername": "string (required, email or username)",
  "password": "string (required)",
  "userType": "string (required, 'customer' or 'admin')"
}
```

**Response Success (200 OK):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "token": "jwt_token_string",
    "refreshToken": "refresh_token_string",
    "user": {
      "userId": "uuid",
      "email": "user@example.com",
      "fullName": "John Doe",
      "userType": "customer",
      "status": "active"
    }
  }
}
```

**Response Error Examples:**
```json
{
  "success": false,
  "message": "Invalid email or password",
  "code": "INVALID_CREDENTIALS"
}
```

```json
{
  "success": false,
  "message": "User account is suspended",
  "code": "ACCOUNT_SUSPENDED"
}
```

```json
{
  "success": false,
  "message": "User type mismatch. This is a customer account.",
  "code": "USER_TYPE_MISMATCH"
}
```

**Backend Responsibilities:**
1. Validate input fields
2. Find user by email or username (case-insensitive)
3. Verify password using bcrypt comparison
4. Check user status (must be 'active')
5. Verify user type matches requested type
6. Generate JWT token (expires in 1 hour)
7. Generate refresh token (expires in 7 days)
8. Log login activity
9. Return tokens and user info (without password)

**JWT Token Payload:**
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "userType": "customer",
  "iat": 1234567890,
  "exp": 1234571490,
  "iss": "firstlend"
}
```

---

### Forgot Password Flow

#### Forgot Password Request Endpoint: `POST /auth/forgot-password`

**Request Body:**
```json
{
  "email": "string (required)"
}
```

**Response:**
```json
{
  "success": true,
  "message": "If the email exists, a reset link has been sent"
}
```

**Backend Responsibilities:**
1. Find user by email
2. Generate password reset token (expires in 30 minutes)
3. Send reset link via email
4. Always return success message (don't reveal if email exists for security)
5. Store reset token hash in database

---

#### Password Reset Endpoint: `POST /auth/reset-password`

**Request Body:**
```json
{
  "token": "string (required)",
  "newPassword": "string (required, same validation as registration)"
}
```

**Response Success:**
```json
{
  "success": true,
  "message": "Password reset successful"
}
```

---

### Token Refresh Flow

#### Refresh Token Endpoint: `POST /auth/refresh-token`

**Request Body:**
```json
{
  "refreshToken": "string (required)"
}
```

**Response Success:**
```json
{
  "success": true,
  "data": {
    "token": "new_jwt_token",
    "refreshToken": "new_refresh_token"
  }
}
```

---

### Logout Endpoint

#### Logout Endpoint: `POST /auth/logout`

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

**Backend Responsibilities:**
1. Blacklist the current token (add to Redis or database)
2. Optionally invalidate refresh token
3. Clear any sessions associated with user

---

## Database Schema

### Users Table
```sql
CREATE TABLE users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  full_name VARCHAR(255) NOT NULL,
  email VARCHAR(255) UNIQUE NOT NULL,
  phone VARCHAR(20) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  user_type ENUM('customer', 'admin') NOT NULL DEFAULT 'customer',
  status ENUM('active', 'inactive', 'suspended') NOT NULL DEFAULT 'active',
  email_verified BOOLEAN DEFAULT FALSE,
  phone_verified BOOLEAN DEFAULT FALSE,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  last_login_at TIMESTAMP,
  
  -- Indices for performance
  INDEX idx_email (email),
  INDEX idx_phone (phone),
  INDEX idx_user_type (user_type),
  INDEX idx_status (status),
  INDEX idx_created_at (created_at)
);
```

### Password Reset Tokens Table
```sql
CREATE TABLE password_reset_tokens (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL,
  token_hash VARCHAR(255) NOT NULL UNIQUE,
  expires_at TIMESTAMP NOT NULL,
  used_at TIMESTAMP,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  INDEX idx_user_id (user_id),
  INDEX idx_expires_at (expires_at)
);
```

### Token Blacklist Table (for logout)
```sql
CREATE TABLE token_blacklist (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL,
  token_jti VARCHAR(255) NOT NULL UNIQUE,
  expires_at TIMESTAMP NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  INDEX idx_expires_at (expires_at)
);
```

### Login Activity Log Table
```sql
CREATE TABLE login_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL,
  ip_address VARCHAR(45),
  user_agent TEXT,
  status ENUM('success', 'failed', 'locked') DEFAULT 'success',
  failure_reason VARCHAR(255),
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  INDEX idx_user_id (user_id),
  INDEX idx_created_at (created_at)
);
```

---

## API Endpoints

### Summary Table

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/register` | No | Register new user |
| POST | `/auth/login` | No | User login |
| POST | `/auth/logout` | Yes | User logout |
| POST | `/auth/refresh-token` | No | Refresh JWT token |
| POST | `/auth/forgot-password` | No | Request password reset |
| POST | `/auth/reset-password` | No | Reset password with token |
| GET | `/auth/me` | Yes | Get current user info |
| GET | `/auth/verify-email/:token` | No | Verify email address |

---

## Security Requirements

### Password Policy
- Minimum 8 characters
- At least 1 uppercase letter (A-Z)
- At least 1 lowercase letter (a-z)
- At least 1 number (0-9)
- At least 1 special character (!@#$%^&*)

### Hashing & Encryption
- Use bcrypt for password hashing with minimum 10 salt rounds
- JWT secret should be strong and stored in environment variables
- Refresh tokens should be stored securely (hashed in database)

### Rate Limiting
- Login attempts: Max 5 failed attempts per 15 minutes per IP
- Register endpoint: Max 3 registrations per 24 hours per IP
- Password reset: Max 3 requests per 24 hours per email

### JWT Configuration
- Access token expiration: 1 hour
- Refresh token expiration: 7 days
- Use HS256 or RS256 algorithm
- Include issuer claim (`iss: 'firstlend'`)

### CORS & Headers
- Set appropriate CORS headers for frontend domain
- Include security headers:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `X-XSS-Protection: 1; mode=block`
  - `Strict-Transport-Security: max-age=31536000`

### Email Verification
- Send verification email upon registration
- Token expires in 24 hours
- Prevent login until email is verified (optional, based on business requirement)

### Phone Verification (Optional)
- Send OTP via SMS for phone verification
- OTP expires in 10 minutes
- Max 3 attempts to verify OTP

---

## Frontend Integration

### API Base URL Configuration
The frontend should be configured to communicate with the backend API. Store the base URL in an environment variable:

```env
VITE_API_BASE_URL=http://localhost:3000/api
```

### Authentication Flow in Frontend

#### 1. Registration
```typescript
// POST to /auth/register
const registerResponse = await fetch(`${VITE_API_BASE_URL}/auth/register`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    fullName: "John Doe",
    email: "john@example.com",
    phone: "+234 800 000 0000",
    password: "SecurePass123!"
  })
});
```

**Frontend expects:**
- Success response with status 201
- Error response with status 400 or 409
- Validation errors with detailed field messages

#### 2. Login
```typescript
// POST to /auth/login
const loginResponse = await fetch(`${VITE_API_BASE_URL}/auth/login`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    emailOrUsername: "john@example.com",
    password: "SecurePass123!",
    userType: "customer" // or "admin"
  })
});
```

**Frontend expects:**
- Success response with `token` and `refreshToken`
- User object with `userId`, `email`, `fullName`, `userType`
- Error response with specific error codes

#### 3. Token Storage
Tokens should be stored securely:
- Access token: Store in memory or localStorage (with CSRF protection)
- Refresh token: Store in secure, httpOnly cookie (recommended)

#### 4. Authenticated Requests
Include JWT token in Authorization header:
```typescript
headers: {
  'Authorization': `Bearer ${accessToken}`
}
```

#### 5. Token Refresh
When access token expires (401 response):
```typescript
// POST to /auth/refresh-token
const refreshResponse = await fetch(`${VITE_API_BASE_URL}/auth/refresh-token`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ refreshToken })
});
```

#### 6. Logout
```typescript
// POST to /auth/logout
await fetch(`${VITE_API_BASE_URL}/auth/logout`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});
// Clear tokens from storage
```

### Frontend Navigation After Login

**Customer Login Success:**
- Redirect to `/customer/dashboard`
- Store user info in state/context

**Admin Login Success:**
- Redirect to `/admin/dashboard`
- Store user info with admin privileges

**Login Failure:**
- Display error toast/alert
- Keep user on login page
- Clear sensitive data

### Frontend Validation

Before sending to backend, validate:
1. Email format
2. Password strength
3. Phone number format
4. Required fields not empty

---

## Implementation Priority

### Phase 1: Core Authentication
1. User registration endpoint
2. User login endpoint
3. JWT token generation
4. Token validation middleware

### Phase 2: Token Management
1. Token refresh endpoint
2. Logout endpoint
3. Token blacklist mechanism
4. Auth middleware for protected routes

### Phase 3: Security Features
1. Password hashing (bcrypt)
2. Rate limiting
3. Email verification
4. Login activity logging

### Phase 4: Enhanced Features
1. Forgot password flow
2. Password reset
3. Remember me functionality
4. Two-factor authentication (optional)

---

## Error Codes Reference

| Code | Status | Description |
|------|--------|-------------|
| `VALIDATION_ERROR` | 400 | Input validation failed |
| `EMAIL_EXISTS` | 409 | Email already registered |
| `PHONE_EXISTS` | 409 | Phone already registered |
| `INVALID_CREDENTIALS` | 401 | Email/password incorrect |
| `ACCOUNT_SUSPENDED` | 403 | Account is suspended |
| `EMAIL_NOT_VERIFIED` | 403 | Email not verified |
| `USER_TYPE_MISMATCH` | 401 | Wrong user type for login |
| `INVALID_TOKEN` | 401 | JWT token invalid |
| `TOKEN_EXPIRED` | 401 | JWT token expired |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `USER_NOT_FOUND` | 404 | User doesn't exist |
| `INTERNAL_ERROR` | 500 | Server error |

---

## Environment Variables Required

```env
# Database
DATABASE_URL=postgresql://user:password@localhost:5432/firstlend_db

# JWT
JWT_SECRET=your-super-secret-key-min-32-chars
JWT_REFRESH_SECRET=your-refresh-secret-key-min-32-chars
JWT_EXPIRATION=3600  # 1 hour in seconds
JWT_REFRESH_EXPIRATION=604800  # 7 days in seconds

# Email (for verification and password reset)
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USER=your-email@example.com
SMTP_PASS=your-email-password
SMTP_FROM=noreply@firstlend.com

# Rate Limiting
RATE_LIMIT_WINDOW=900  # 15 minutes in seconds
RATE_LIMIT_MAX_ATTEMPTS=5

# Server
PORT=3000
NODE_ENV=development
FRONTEND_URL=http://localhost:5173

# Optional: SMS for OTP
TWILIO_ACCOUNT_SID=your-account-sid
TWILIO_AUTH_TOKEN=your-auth-token
TWILIO_PHONE_NUMBER=+1234567890
```

---

## Testing Credentials (Development Only)

For testing purposes, these credentials can be pre-seeded:

**Customer Account:**
- Email: `customer@test.com`
- Password: `Test@12345`
- Type: Customer

**Admin Account:**
- Email: `admin@test.com`
- Password: `Admin@12345`
- Type: Admin

---

## Next Steps

Once authentication is implemented, you can proceed to implement:
1. Customer Loan Application Management
2. Admin Dashboard & Analytics
3. Loan Products Management
4. Payment Processing
5. Risk Assessment System
6. User Management & Permissions
