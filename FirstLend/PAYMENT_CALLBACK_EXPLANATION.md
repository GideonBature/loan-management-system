# Payment Callback URLs Explanation

## Two Different URLs, Two Different Purposes

### 1. **Paystack CallbackUrl** → `http://localhost:5128/api/payments/callback`
**What it does:** Tells Paystack where to send the user after payment

**Configuration:**
```json
{
  "Paystack": {
    "CallbackUrl": "http://localhost:5128/api/payments/callback"
  }
}
```

**When it's used:**
- When you call `/api/payments/initialize`
- This URL is sent to Paystack in the payment initialization payload
- Paystack stores it and uses it after payment completes

**Flow:**
```
Frontend → POST /api/payments/initialize
         ↓
Backend sends to Paystack:
{
  "email": "user@email.com",
  "amount": 6000000,
  "callback_url": "http://localhost:5128/api/payments/callback"  ← This one!
}
         ↓
User completes payment on Paystack
         ↓
Paystack redirects browser to:
http://localhost:5128/api/payments/callback?reference=FL-xxx&trxref=FL-xxx
```

---

### 2. **Frontend BaseUrl** → `http://localhost:8080`
**What it does:** Tells the backend where your frontend application is running

**Configuration:**
```json
{
  "Frontend": {
    "BaseUrl": "http://localhost:8080"
  }
}
```

**When it's used:**
- When the backend callback endpoint (`/api/payments/callback`) receives the redirect from Paystack
- After verifying the payment with Paystack
- To redirect the user back to your frontend with the result

**Flow:**
```
Backend receives: http://localhost:5128/api/payments/callback?reference=FL-xxx
         ↓
Backend verifies payment with Paystack API
         ↓
Payment Success?
         ↓ YES
Backend redirects to:
http://localhost:8080/payment/success?reference=FL-xxx&amount=60000  ← Frontend URL!
         ↓ NO
Backend redirects to:
http://localhost:8080/payment/failed?message=error  ← Frontend URL!
```

---

## Complete Payment Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         PAYMENT FLOW                                 │
└─────────────────────────────────────────────────────────────────────┘

1. USER INITIATES PAYMENT
   ┌──────────────┐
   │   Frontend   │ http://localhost:8080/loans
   │ (Port 8080)  │
   └──────┬───────┘
          │ POST /api/payments/initialize
          │ { loanId, amount }
          ↓
   ┌──────────────┐
   │   Backend    │ http://localhost:5128/api/payments/initialize
   │ (Port 5128)  │
   └──────┬───────┘
          │ Sends payload with callback_url:
          │ "http://localhost:5128/api/payments/callback"
          ↓
   ┌──────────────┐
   │   Paystack   │ Receives: email, amount, callback_url
   │   Server     │ Returns: authorization_url
   └──────┬───────┘
          │ Returns: { authorizationUrl: "https://checkout.paystack.com/xxx" }
          ↓
   ┌──────────────┐
   │   Frontend   │ Redirects user to authorizationUrl
   └──────────────┘

2. USER PAYS ON PAYSTACK
   ┌──────────────┐
   │   Paystack   │ https://checkout.paystack.com/xxx
   │  Checkout    │ User enters card details
   └──────┬───────┘
          │ Payment completed!
          │ Uses the callback_url from step 1
          ↓
   ┌──────────────┐
   │   Backend    │ GET http://localhost:5128/api/payments/callback
   │   Callback   │     ?reference=FL-xxx&trxref=FL-xxx
   └──────┬───────┘
          │ Calls Paystack API to verify payment
          ↓
   ┌──────────────┐
   │   Paystack   │ GET https://api.paystack.co/transaction/verify/FL-xxx
   │     API      │ Returns: { status: "success", amount: 6000000, ... }
   └──────┬───────┘
          │ Payment verified!
          ↓
   ┌──────────────┐
   │   Backend    │ Updates database:
   │   Callback   │ - PaymentHistory.Status = "Success"
   └──────┬───────┘ - Loan.AmountDue -= payment
          │ Uses Frontend.BaseUrl to redirect
          │ Redirect to: http://localhost:8080/payment/success
          ↓
   ┌──────────────┐
   │   Frontend   │ http://localhost:8080/payment/success
   │ Success Page │     ?reference=FL-xxx&amount=60000
   └──────────────┘ Shows: "Payment Successful! ✅"
```

---

## Code Examples

### Where Paystack CallbackUrl is Used

**File:** `FirstLend.Infrastructure/Services/PaymentService.cs`

```csharp
public async Task<PaymentInitiationResponse> InitiatePayment(
    InitiatePaymentRequest request, 
    string userId)
{
    // ... validation code ...

    var payload = new
    {
        email = user.Email,
        amount = amountInKobo,
        reference = reference,
        currency = "NGN",
        metadata = new { ... },
        
        // ✅ PAYSTACK CALLBACK URL - Where Paystack sends the user after payment
        callback_url = _configuration["Paystack:CallbackUrl"] 
                       ?? "http://localhost:5128/api/payments/callback"
    };

    // Send to Paystack
    var response = await _httpClient.PostAsync(
        "https://api.paystack.co/transaction/initialize", 
        content
    );
}
```

### Where Frontend BaseUrl is Used

**File:** `FirstLend.Api/Controllers/PaymentsController.cs`

```csharp
[HttpGet("callback")]
[AllowAnonymous]
public async Task<IActionResult> PaymentCallback(
    [FromQuery] string reference, 
    [FromQuery] string trxref)
{
    var paymentReference = reference ?? trxref;
    
    // Verify payment with Paystack
    var result = await _paymentService.VerifyPayment(paymentReference);

    if (result.Success)
    {
        // ✅ FRONTEND BASE URL - Where to send the user after verification
        return Redirect(
            $"{GetFrontendUrl()}/payment/success?reference={paymentReference}&amount={result.Amount}"
        );
    }
    else
    {
        // ✅ FRONTEND BASE URL - Where to send the user on failure
        return Redirect(
            $"{GetFrontendUrl()}/payment/failed?message={result.Message}"
        );
    }
}

private string GetFrontendUrl()
{
    return _configuration["Frontend:BaseUrl"] ?? "http://localhost:8080";
}
```

---

## Why Two Separate URLs?

### Paystack CallbackUrl (Backend)
- **Must be publicly accessible** (in production)
- **Receives the redirect** from Paystack
- **Verifies the payment** with Paystack API
- **Processes business logic** (update database, loan balance, etc.)
- **Security:** Can validate payment before showing user

### Frontend BaseUrl (Frontend)
- **Where your UI lives**
- **Shows the user** success/failure message
- **Better UX:** User sees a nice page, not JSON
- **Separation of concerns:** Backend handles logic, frontend handles display

---

## Current Configuration

Check your `appsettings.json`:

```json
{
  "Paystack": {
    "SecretKey": "sk_test_...",
    "PublicKey": "pk_test_...",
    "CallbackUrl": "http://localhost:5128/api/payments/callback"  ← Backend endpoint
  },
  "Frontend": {
    "BaseUrl": "http://localhost:8080"  ← Your frontend URL
  }
}
```

---

## What You Need to Do in Frontend

Create these two routes/pages at your frontend (`http://localhost:8080`):

### 1. `/payment/success`
```javascript
// This page receives:
// ?reference=FL-xxx&amount=60000

// Display:
// ✅ Payment Successful!
// Your payment of ₦60,000 has been confirmed.
// Reference: FL-xxx
```

### 2. `/payment/failed`
```javascript
// This page receives:
// ?reference=FL-xxx&message=error

// Display:
// ❌ Payment Failed
// [error message]
// [Try Again button]
```

---

## Testing Locally

### Test the Flow:
1. **Start Backend:** `dotnet run --project FirstLend.Api` (runs on port 5128)
2. **Start Frontend:** (runs on port 8080)
3. **Click "Pay" button** in your frontend
4. **Complete payment** on Paystack (use test card)
5. **Watch the redirects:**
   - Paystack → `http://localhost:5128/api/payments/callback?reference=FL-xxx`
   - Backend → `http://localhost:8080/payment/success?reference=FL-xxx&amount=60000`

### Check the Browser Network Tab:
```
1. POST http://localhost:5128/api/payments/initialize
2. GET  https://checkout.paystack.com/xxx
3. GET  http://localhost:5128/api/payments/callback?reference=FL-xxx  (Paystack redirects here)
4. 302 Redirect to http://localhost:8080/payment/success  (Backend redirects here)
```

---

## Production Configuration

When deploying to production (e.g., `api.yourdomain.com` and `yourdomain.com`):

```json
{
  "Paystack": {
    "SecretKey": "sk_live_...",
    "CallbackUrl": "https://api.yourdomain.com/api/payments/callback"
  },
  "Frontend": {
    "BaseUrl": "https://yourdomain.com"
  }
}
```

Flow becomes:
```
Paystack → https://api.yourdomain.com/api/payments/callback
Backend  → https://yourdomain.com/payment/success
```

---

## Summary

| Config | Purpose | Who Uses It | When |
|--------|---------|-------------|------|
| **Paystack CallbackUrl** | Where Paystack sends user after payment | Paystack | After user completes payment |
| **Frontend BaseUrl** | Where your frontend is running | Backend | After backend verifies payment |

**Both are needed for the complete payment flow to work!**

See `FRONTEND_PAYMENT_GUIDE.md` for complete frontend implementation examples.
