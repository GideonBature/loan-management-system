# FirstLend API Documentation

FirstLend is a comprehensive loan management system API built with .NET 9. It provides a full suite of endpoints for user authentication, loan application, automated credit scoring, KYC verification, and payment processing. The system is designed with a clear separation for customer-facing operations and a secure admin backend for management and loan disbursement.

It leverages a modern .NET stack and integrates with several key third-party services, including Paystack for payments, Google Gemini for AI-driven financial assistance, and is configured for Mono for credit data aggregation.

-----

## 🏛️ Project Architecture

The solution follows a **Clean Architecture** pattern, separating concerns into four main projects to ensure maintainability, testability, and a clean separation of concerns.

  * `FirstLend.Domain`: Contains the core domain entities (e.g., `Loan`, `LoanType`, `PaymentHistory`), enums, and the primary service abstractions (interfaces).
  * `FirstLend.Application`: Contains the application's business logic, DTOs (Request/Response models), and service interfaces. It also holds specific business logic components like the `CreditScoreEngine`.
  * `FirstLend.Infrastructure`: Implements the interfaces defined in the Domain/Application layers. This includes data access with Entity Framework Core (`FirstLendDbContext`), implementations for `AuthService`, `LoanService`, `CreditScoreService`, and integrations with external services like `GeminiService` and Paystack.
  * `FirstLend.Api`: The presentation layer. This is an ASP.NET Core Web API project that exposes all the controllers and endpoints, handles HTTP requests, and manages configuration.

This structure is defined in the solution file `FirstLend.sln`.

-----

## ✨ Core Features

### User & Authentication

  * **User Registration**: Secure registration for new "Customer" users.
  * **JWT Authentication**: All secure endpoints are protected using JSON Web Tokens (JWT).
  * **Role-Based Access**: Clear distinction between `Customer` and `Admin` roles.
  * **Customer & Admin Logins**: Separate login endpoints for customers and admins.
  * **Password Management**: Full password-less flow with "Forgot Password" and "Reset Password" endpoints.
  * **Profile Management**: Endpoint for users to retrieve their own data (`/api/auth/me`).

### Customer: Loans & Finance

  * **KYC Verification**: Endpoint to submit KYC details (`/api/kyc/verify`) and check status. Loan applications are blocked until KYC is verified.
  * **Credit Scoring**: Automated credit score calculation for users (`/api/credit-score`).
  * **Loan Application**: Customers can apply for new loans. The application is validated against their KYC status and credit score.
  * **Loan Management**: Customers can view their own loan history and the status of active loans.
  * **Loan Repayment**: Integrated with **Paystack** to initialize (`/api/payments/initialize`) and verify loan repayments. Includes a callback and webhook for processing payment notifications.

### Admin: Management & Operations

  * **User Management**: Full CRUD operations for users. Admins can view all customers, get user details, update user status (e.g., "Active", "Inactive"), and create new *admin* users.
  * **Loan Management**: Admins can view all loan applications across all users, filter by status (e.g., "pending", "active"), and view loan details.
  * **Loan Approval Flow**: Admins can **approve** or **reject** pending loan applications.
  * **Loan Disbursement**: Admins can disburse funds for approved loans. This is also integrated with **Paystack** to initiate a payout to the customer.

### AI Integration

  * **Financial Assistant**: An endpoint (`/api/gemini/analyze`) that uses **Google's Gemini** model to answer finance-related questions.
  * **System Prompt**: The AI is given a system prompt to act as a "financial assistant for FirstLend" and is provided with the company's loan types and interest rates to answer user questions accurately.

-----

## 🤖 Key Business Logic: Credit Scoring

A core feature of FirstLend is its internal credit scoring mechanism, which is used to automatically vet loan applications.

1.  **KYC Gating**: A user must be **KYC verified** before they can apply for a loan. The `LoanService` checks the `user.KycVerified` flag before proceeding.
2.  **Post-KYC Score**: After KYC, the `CreditScoreService` can be used to assign random, pre-seeded credit accounts to the user to simulate a credit history.
3.  **Loan Application Check**: When a user applies for a loan, the `LoanService` first calls the `CreditScoreService` to get their score.
4.  **Minimum Score**: A **minimum score of 50.0** is required to proceed with a loan application. If the user's score is below 50, the application is rejected.

### Score Calculation (`CreditScoreEngine`)

The score is calculated by the `CreditScoreEngine`.

  * **Default Score**: If a user has no credit accounts, they are given a **default score of 50.0**.
  * **Weighted Average**: For users with accounts, the engine calculates a final score based on a weighted average of five key factors:
      * **Payment History (35%)**: Score based on on-time vs. late/missed payments.
      * **Amounts Owed (30%)**: Score based on credit utilization (current balance vs. credit limit).
      * **Length of History (15%)**: Score based on the age of the oldest and average-age accounts.
      * **Credit Mix (10%)**: Score based on the variety of account types (e.g., revolving, installment).
      * **New Credit (10%)**: Score is penalized for recently opened accounts or hard inquiries.

-----

## 🛠️ Technologies & Services

### Framework & Database

  * **.NET 9** (Target Framework)
  * **ASP.NET Core Web API**
  * **Entity Framework Core**
  * **PostgreSQL** (Database provider)
  * **ASP.NET Identity** (For user and role management)

### Authentication & API

  * **JWT (JSON Web Tokens)**: Used for securing endpoints.
  * **Swagger/OpenAPI**: For API documentation and testing.

### External Services (from `appsettings.json`)

  * **Paystack**: For all payment processing (repayments and disbursements).
  * **Google Gemini**: For the AI-powered financial assistant.
  * **Mono**: Configured for use, likely for connecting to bank data or credit bureaus.

-----

## 🚀 Setup & Installation

To run this project locally:

1.  **Clone the Repository**

    ```sh
    git clone https://github.com/GideonBature/loan-management-system
    cd loan-management-system/FirstLend
    ```

2.  **Install .NET 9 SDK**
    Ensure you have the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed.

3.  **Configure `appsettings.json`**
    Open `FirstLend.Api/appsettings.json` and fill in the required values:

      * `ConnectionStrings:DefaultConnection`: Your PostgreSQL connection string.
      * `Jwt:Secret`: A long, random string for signing JWT tokens.
      * `Gemini:ApiKey`: Your Google Gemini API key.
      * `Paystack:SecretKey`: Your Paystack test secret key.
      * `Mono:SecretKey`: Your Mono test secret key.
      * `Frontend:BaseUrl`: The URL of your frontend application (for payment callbacks).

4.  **Apply Database Migrations**
    The project uses EF Core migrations to set up the database schema.

    ```sh
    dotnet ef database update --project FirstLend.Infrastructure
    ```

5.  **Run the Application**

    ```sh
    dotnet run --project FirstLend.Api/FirstLend.Api.csproj
    ```

6.  **Access the API**
    The API will be running (typically at `http://localhost:5128` or similar). You can access the Swagger documentation at the `/swagger` endpoint (e.g., `http://localhost:5128/swagger`).

-----

## 🗺️ API Endpoints Summary

### Authentication (`/api/auth`)

  * `POST /api/auth/register`: Register a new customer.
  * `POST /api/auth/login`: Login for customers (default).
  * `POST /api/auth/admin/login`: Login for admins.
  * `POST /api/auth/customer/login`: Explicit login for customers.
  * `POST /api/auth/forgot-password`: Start password reset flow.
  * `POST /api/auth/reset-password`: Complete password reset.
  * `POST /api/auth/refresh-token`: Get a new JWT.
  * `GET /api/auth/me`: [Auth] Get current user's profile.

### Loans (`/api/loan`)

  * `POST /api/loan`: [Auth: Customer] Apply for a new loan.
  * `GET /api/loan/my-loans`: [Auth: Customer] Get all loans for the current user.
  * `GET /api/loan/{id}`: [Auth: Customer] Get a specific loan by its ID.

### Payments (`/api/payments`)

  * `POST /api/payments/initialize`: [Auth] Initialize a Paystack payment for a loan.
  * `GET /api/payments/verify/{reference}`: [Auth] Verify a payment.
  * `GET /api/payments/callback`: [Public] Paystack callback URL.
  * `POST /api/payments/webhook`: [Public] Paystack webhook for payment events.

### KYC & Credit (`/api/kyc`, `/api/credit-score`)

  * `POST /api/kyc/verify`: [Auth] Submit KYC information.
  * `GET /api/kyc/status`: [Auth] Check current KYC status.
  * `GET /api/credit-score`: [Auth] Get the user's current credit score.

### Gemini AI (`/api/gemini`)

  * `POST /api/gemini/analyze`: [Public] Send a prompt to the financial assistant.

### Admin: Users (`/api/admin/users`)

  * `GET /api/admin/users`: [Auth: Admin] Get all users (customers by default).
  * `POST /api/admin/users`: [Auth: Admin] Create a new admin user.
  * `GET /api/admin/users/admins`: [Auth: Admin] Get all admin-role users.
  * `GET /api/admin/users/{id}`: [Auth: Admin] Get details for a specific user.
  * `PUT /api/admin/users/{id}/status`: [Auth: Admin] Update a user's status.
  * `DELETE /api/admin/users/{id}`: [Auth: Admin] Delete a user.
  * `GET /api/admin/users/{userId}/loans`: [Auth: Admin] Get loan history for a specific user.

### Admin: Loans (`/api/admin/loans`)

  * `GET /api/admin/loans`: [Auth: Admin] Get all loans (with filters for status, search, etc.).
  * `GET /api/admin/loans/{id}`: [Auth: Admin] Get details for a specific loan.
  * `PUT /api/admin/loans/{id}/approve`: [Auth: Admin] Approve a pending loan.
  * `PUT /api/admin/loans/{id}/reject`: [Auth: Admin] Reject a pending loan.
  * `PUT /api/admin/loans/{id}/disburse`: [Auth: Admin] Mark a loan as disbursed (activates it).
  * `POST /api/admin/loans/{id}/disburse/initialize`: [Auth: Admin] Initialize a Paystack disbursement payment.
  * `GET /api/admin/loans/{id}/disburse/verify`: [Public] Verify a disbursement payment (Paystack callback).