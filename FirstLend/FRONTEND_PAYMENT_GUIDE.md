# Frontend Payment Integration Guide

## Overview
This guide shows how to integrate Paystack payments in your frontend application.

## Payment Flow

```
User clicks "Make Payment" 
  → Frontend calls /api/payments/initialize
  → User redirected to Paystack checkout
  → User completes payment
  → Paystack redirects to Backend callback (http://localhost:5128/api/payments/callback)
  → Backend verifies payment
  → Backend redirects to Frontend:
      - Success: http://localhost:8080/payment/success
      - Failure: http://localhost:8080/payment/failed
```

## Required Frontend Routes

You need to create these two routes in your frontend application:

### 1. `/payment/success` - Payment Success Page
### 2. `/payment/failed` - Payment Failure Page

---

## Implementation Examples

### **React/Next.js Example**

#### Payment Button Component

```jsx
// components/PaymentButton.jsx
import { useState } from 'react';

export default function PaymentButton({ loanId, amount }) {
  const [loading, setLoading] = useState(false);

  const handlePayment = async () => {
    setLoading(true);
    
    try {
      const token = localStorage.getItem('authToken'); // or however you store your JWT
      
      const response = await fetch('http://localhost:5128/api/payments/initialize', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          loanId: loanId,
          amount: amount
        })
      });

      const result = await response.json();

      if (result.success && result.data.authorizationUrl) {
        // Redirect user to Paystack checkout
        window.location.href = result.data.authorizationUrl;
      } else {
        alert('Payment initialization failed: ' + result.message);
      }
    } catch (error) {
      console.error('Error:', error);
      alert('An error occurred while initializing payment');
    } finally {
      setLoading(false);
    }
  };

  return (
    <button 
      onClick={handlePayment} 
      disabled={loading}
      className="btn btn-primary"
    >
      {loading ? 'Processing...' : `Pay ₦${amount.toLocaleString()}`}
    </button>
  );
}
```

#### Success Page

```jsx
// pages/payment/success.jsx (or app/payment/success/page.jsx for Next.js App Router)
import { useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

export default function PaymentSuccess() {
  const router = useRouter();
  const searchParams = useSearchParams();
  
  const reference = searchParams.get('reference');
  const amount = searchParams.get('amount');

  useEffect(() => {
    // Auto-redirect to loans page after 5 seconds
    const timer = setTimeout(() => {
      router.push('/loans');
    }, 5000);

    return () => clearTimeout(timer);
  }, [router]);

  return (
    <div className="container mx-auto p-8 text-center">
      <div className="max-w-md mx-auto bg-white rounded-lg shadow-lg p-8">
        <div className="text-6xl mb-4">✅</div>
        <h1 className="text-3xl font-bold text-green-600 mb-4">
          Payment Successful!
        </h1>
        <p className="text-gray-700 mb-2">
          Your payment of <strong>₦{parseFloat(amount).toLocaleString()}</strong> has been confirmed.
        </p>
        <p className="text-sm text-gray-500 mb-6">
          Reference: {reference}
        </p>
        <p className="text-sm text-gray-600 mb-4">
          Redirecting to loans page in 5 seconds...
        </p>
        <button
          onClick={() => router.push('/loans')}
          className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700"
        >
          Go to Loans Now
        </button>
      </div>
    </div>
  );
}
```

#### Failed Page

```jsx
// pages/payment/failed.jsx (or app/payment/failed/page.jsx for Next.js App Router)
import { useRouter, useSearchParams } from 'next/navigation';

export default function PaymentFailed() {
  const router = useRouter();
  const searchParams = useSearchParams();
  
  const reference = searchParams.get('reference');
  const message = searchParams.get('message');

  return (
    <div className="container mx-auto p-8 text-center">
      <div className="max-w-md mx-auto bg-white rounded-lg shadow-lg p-8">
        <div className="text-6xl mb-4">❌</div>
        <h1 className="text-3xl font-bold text-red-600 mb-4">
          Payment Failed
        </h1>
        <p className="text-gray-700 mb-4">
          {message || 'Your payment could not be processed. Please try again.'}
        </p>
        {reference && (
          <p className="text-sm text-gray-500 mb-6">
            Reference: {reference}
          </p>
        )}
        <div className="flex gap-4 justify-center">
          <button
            onClick={() => router.push('/loans')}
            className="bg-gray-600 text-white px-6 py-2 rounded hover:bg-gray-700"
          >
            Back to Loans
          </button>
          <button
            onClick={() => window.location.reload()}
            className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700"
          >
            Try Again
          </button>
        </div>
      </div>
    </div>
  );
}
```

---

### **Vue.js Example**

#### Payment Button Component

```vue
<!-- components/PaymentButton.vue -->
<template>
  <button 
    @click="handlePayment" 
    :disabled="loading"
    class="btn btn-primary"
  >
    {{ loading ? 'Processing...' : `Pay ₦${amount.toLocaleString()}` }}
  </button>
</template>

<script setup>
import { ref } from 'vue';

const props = defineProps({
  loanId: String,
  amount: Number
});

const loading = ref(false);

const handlePayment = async () => {
  loading.value = true;
  
  try {
    const token = localStorage.getItem('authToken');
    
    const response = await fetch('http://localhost:5128/api/payments/initialize', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({
        loanId: props.loanId,
        amount: props.amount
      })
    });

    const result = await response.json();

    if (result.success && result.data.authorizationUrl) {
      window.location.href = result.data.authorizationUrl;
    } else {
      alert('Payment initialization failed: ' + result.message);
    }
  } catch (error) {
    console.error('Error:', error);
    alert('An error occurred while initializing payment');
  } finally {
    loading.value = false;
  }
};
</script>
```

#### Success Page

```vue
<!-- views/payment/Success.vue -->
<template>
  <div class="container mx-auto p-8 text-center">
    <div class="max-w-md mx-auto bg-white rounded-lg shadow-lg p-8">
      <div class="text-6xl mb-4">✅</div>
      <h1 class="text-3xl font-bold text-green-600 mb-4">
        Payment Successful!
      </h1>
      <p class="text-gray-700 mb-2">
        Your payment of <strong>₦{{ parseFloat(amount).toLocaleString() }}</strong> has been confirmed.
      </p>
      <p class="text-sm text-gray-500 mb-6">
        Reference: {{ reference }}
      </p>
      <p class="text-sm text-gray-600 mb-4">
        Redirecting to loans page in 5 seconds...
      </p>
      <button
        @click="$router.push('/loans')"
        class="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700"
      >
        Go to Loans Now
      </button>
    </div>
  </div>
</template>

<script setup>
import { onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const route = useRoute();
const router = useRouter();

const reference = route.query.reference;
const amount = route.query.amount;

onMounted(() => {
  setTimeout(() => {
    router.push('/loans');
  }, 5000);
});
</script>
```

#### Failed Page

```vue
<!-- views/payment/Failed.vue -->
<template>
  <div class="container mx-auto p-8 text-center">
    <div class="max-w-md mx-auto bg-white rounded-lg shadow-lg p-8">
      <div class="text-6xl mb-4">❌</div>
      <h1 class="text-3xl font-bold text-red-600 mb-4">
        Payment Failed
      </h1>
      <p class="text-gray-700 mb-4">
        {{ message || 'Your payment could not be processed. Please try again.' }}
      </p>
      <p v-if="reference" class="text-sm text-gray-500 mb-6">
        Reference: {{ reference }}
      </p>
      <div class="flex gap-4 justify-center">
        <button
          @click="$router.push('/loans')"
          class="bg-gray-600 text-white px-6 py-2 rounded hover:bg-gray-700"
        >
          Back to Loans
        </button>
        <button
          @click="window.location.reload()"
          class="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700"
        >
          Try Again
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useRoute } from 'vue-router';

const route = useRoute();

const reference = route.query.reference;
const message = route.query.message;
</script>
```

---

### **Plain JavaScript/HTML Example**

#### Payment Button

```html
<!-- In your loans page -->
<button onclick="initiatePayment('loan-id-here', 60000)" class="btn-payment">
  Pay ₦60,000
</button>

<script>
async function initiatePayment(loanId, amount) {
  const token = localStorage.getItem('authToken');
  
  try {
    const response = await fetch('http://localhost:5128/api/payments/initialize', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ loanId, amount })
    });

    const result = await response.json();

    if (result.success && result.data.authorizationUrl) {
      // Redirect to Paystack checkout
      window.location.href = result.data.authorizationUrl;
    } else {
      alert('Payment initialization failed: ' + result.message);
    }
  } catch (error) {
    console.error('Error:', error);
    alert('An error occurred while initializing payment');
  }
}
</script>
```

#### Success Page (`payment/success.html`)

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Payment Successful - FirstLend</title>
  <style>
    body {
      font-family: Arial, sans-serif;
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: #f0f0f0;
      margin: 0;
    }
    .container {
      background: white;
      padding: 40px;
      border-radius: 10px;
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
      text-align: center;
      max-width: 500px;
    }
    .success-icon { font-size: 60px; margin-bottom: 20px; }
    h1 { color: #22c55e; margin-bottom: 20px; }
    .btn { 
      padding: 12px 24px;
      background: #3b82f6;
      color: white;
      border: none;
      border-radius: 5px;
      cursor: pointer;
      margin-top: 20px;
    }
    .btn:hover { background: #2563eb; }
  </style>
</head>
<body>
  <div class="container">
    <div class="success-icon">✅</div>
    <h1>Payment Successful!</h1>
    <p>Your payment of <strong id="amount"></strong> has been confirmed.</p>
    <p><small>Reference: <span id="reference"></span></small></p>
    <p><small id="countdown">Redirecting to loans page in 5 seconds...</small></p>
    <button class="btn" onclick="goToLoans()">Go to Loans Now</button>
  </div>

  <script>
    // Get query parameters
    const urlParams = new URLSearchParams(window.location.search);
    const reference = urlParams.get('reference');
    const amount = urlParams.get('amount');

    // Display values
    document.getElementById('reference').textContent = reference || 'N/A';
    document.getElementById('amount').textContent = amount 
      ? `₦${parseFloat(amount).toLocaleString()}` 
      : 'N/A';

    // Auto-redirect countdown
    let seconds = 5;
    const countdownEl = document.getElementById('countdown');
    const interval = setInterval(() => {
      seconds--;
      if (seconds <= 0) {
        clearInterval(interval);
        goToLoans();
      } else {
        countdownEl.textContent = `Redirecting to loans page in ${seconds} seconds...`;
      }
    }, 1000);

    function goToLoans() {
      window.location.href = '/loans'; // Adjust to your loans page route
    }
  </script>
</body>
</html>
```

#### Failed Page (`payment/failed.html`)

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Payment Failed - FirstLend</title>
  <style>
    body {
      font-family: Arial, sans-serif;
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: #f0f0f0;
      margin: 0;
    }
    .container {
      background: white;
      padding: 40px;
      border-radius: 10px;
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
      text-align: center;
      max-width: 500px;
    }
    .error-icon { font-size: 60px; margin-bottom: 20px; }
    h1 { color: #ef4444; margin-bottom: 20px; }
    .btn { 
      padding: 12px 24px;
      color: white;
      border: none;
      border-radius: 5px;
      cursor: pointer;
      margin: 10px;
    }
    .btn-primary { background: #3b82f6; }
    .btn-primary:hover { background: #2563eb; }
    .btn-secondary { background: #6b7280; }
    .btn-secondary:hover { background: #4b5563; }
  </style>
</head>
<body>
  <div class="container">
    <div class="error-icon">❌</div>
    <h1>Payment Failed</h1>
    <p id="message">Your payment could not be processed. Please try again.</p>
    <p><small>Reference: <span id="reference"></span></small></p>
    <div>
      <button class="btn btn-secondary" onclick="goToLoans()">Back to Loans</button>
      <button class="btn btn-primary" onclick="tryAgain()">Try Again</button>
    </div>
  </div>

  <script>
    // Get query parameters
    const urlParams = new URLSearchParams(window.location.search);
    const reference = urlParams.get('reference');
    const message = urlParams.get('message');

    // Display values
    if (reference) {
      document.getElementById('reference').textContent = reference;
    } else {
      document.getElementById('reference').parentElement.style.display = 'none';
    }
    
    if (message) {
      document.getElementById('message').textContent = decodeURIComponent(message);
    }

    function goToLoans() {
      window.location.href = '/loans'; // Adjust to your loans page route
    }

    function tryAgain() {
      window.location.reload();
    }
  </script>
</body>
</html>
```

---

## Testing the Complete Flow

### Step 1: Make a Payment
```javascript
// From your frontend
initiatePayment('f199e088-06b4-4f4d-8181-a1153338afd0', 60000);
```

### Step 2: Complete Payment on Paystack
Use test card:
- **Card**: 4084084084084081
- **CVV**: 408
- **Expiry**: 12/25
- **PIN**: 0000
- **OTP**: 123456

### Step 3: Automatic Redirect
- Paystack → Backend callback → Frontend success/failed page

### Step 4: Verify in Database
Check that:
- Payment is recorded in `PaymentHistories` table
- Loan `AmountDue` is reduced
- Payment status is "Success"

---

## Configuration Summary

### Backend (`appsettings.json`)
```json
{
  "Paystack": {
    "CallbackUrl": "http://localhost:5128/api/payments/callback"
  },
  "Frontend": {
    "BaseUrl": "http://localhost:8080"
  }
}
```

### Frontend Routes Required
- `/payment/success` - Success page
- `/payment/failed` - Failure page

---

## Troubleshooting

### Payment initializes but doesn't redirect
- Check that `authorizationUrl` is being returned
- Verify `window.location.href` is being called

### Callback doesn't work
- Ensure backend is running on port 5128
- Check that callback URL in Paystack matches backend
- Verify frontend success/failed pages exist

### Database not updating
- Check migration was applied
- Verify `UserId` matches between JWT and database
- Check server logs for errors

---

## Production Deployment

When deploying to production:

1. Update `appsettings.json`:
```json
{
  "Paystack": {
    "SecretKey": "sk_live_your_live_key",
    "CallbackUrl": "https://api.yourdomain.com/api/payments/callback"
  },
  "Frontend": {
    "BaseUrl": "https://yourdomain.com"
  }
}
```

2. Create the same routes on your production frontend
3. Test with small amounts first
4. Monitor payment logs

---

## Need Help?

- Check the `PAYSTACK_INTEGRATION.md` for API details
- Review Paystack docs: https://paystack.com/docs
- Check backend logs for errors
