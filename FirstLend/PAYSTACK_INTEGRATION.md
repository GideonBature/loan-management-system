# Paystack Payment Integration Guide

## Overview
This guide explains how to integrate Paystack payment gateway for loan repayments in the FirstLend application.

## Prerequisites
1. **Paystack Account**: Sign up at [https://paystack.com](https://paystack.com)
2. **Test API Keys**: Get your test keys from the Paystack Dashboard

## Setup Instructions

### 1. Get Your Paystack API Keys

1. Go to [Paystack Dashboard](https://dashboard.paystack.com)
2. Navigate to **Settings** > **API Keys & Webhooks**
3. Copy your **Test Secret Key** (starts with `sk_test_`)
4. Copy your **Test Public Key** (starts with `pk_test_`)

### 2. Configure API Keys

Update the `appsettings.json` file with your Paystack keys:

```json
{
  "Paystack": {
    "SecretKey": "sk_test_your_actual_secret_key_here",
    "PublicKey": "pk_test_your_actual_public_key_here",
    "CallbackUrl": "http://localhost:5128/api/payments/callback"
  }
}
```

### 3. API Endpoints

#### **Initialize Payment**
```http
POST /api/payments/initialize
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "loanId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amount": 50000
}
```

**Response:**
```json
{
  "success": true,
  "message": "Payment initialized successfully",
  "code": "200",
  "data": {
    "success": true,
    "message": "Payment initialized successfully",
    "authorizationUrl": "https://checkout.paystack.com/xxxxxxxx",
    "accessCode": "xxxxxxxx",
    "reference": "FL-xxxxxxxx-1234567890"
  }
}
```

#### **Verify Payment**
```http
GET /api/payments/verify/{reference}
Authorization: Bearer {jwt_token}
```

**Response:**
```json
{
  "success": true,
  "message": "Payment verified successfully",
  "code": "200",
  "data": {
    "success": true,
    "message": "Payment verified successfully",
    "amount": 50000,
    "status": "success",
    "reference": "FL-xxxxxxxx-1234567890",
    "paidAt": "2025-11-09T12:00:00Z",
    "channel": "card"
  }
}
```

#### **Webhook Endpoint**
```http
POST /api/payments/webhook
x-paystack-signature: {signature}
Content-Type: application/json

{
  "event": "charge.success",
  "data": { ... }
}
```

## Frontend Integration Flow

### 1. Initialize Payment

```javascript
const initializePayment = async (loanId, amount) => {
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
    
    if (result.success) {
      // Redirect user to Paystack checkout
      window.location.href = result.data.authorizationUrl;
    }
  } catch (error) {
    console.error('Payment initialization failed:', error);
  }
};
```

### 2. Handle Payment Callback

After payment, Paystack redirects to the backend callback URL which verifies the payment and redirects to the frontend. Create these pages in your frontend:

#### **Success Page** (`/payment/success`)

```javascript
// /payment/success page
const PaymentSuccess = () => {
  const [searchParams] = useSearchParams();
  const reference = searchParams.get('reference');
  const amount = searchParams.get('amount');
  const navigate = useNavigate();

  useEffect(() => {
    // Auto-redirect after 3 seconds
    const timer = setTimeout(() => {
      navigate('/loans');
    }, 3000);

    return () => clearTimeout(timer);
  }, [navigate]);

  return (
    <div className="payment-success">
      <h1>✅ Payment Successful!</h1>
      <p>Your payment of ₦{parseFloat(amount).toLocaleString()} has been confirmed.</p>
      <p>Reference: {reference}</p>
      <p>Redirecting to loans page...</p>
      <button onClick={() => navigate('/loans')}>Go to Loans Now</button>
    </div>
  );
};
```

#### **Failed Page** (`/payment/failed`)

```javascript
// /payment/failed page
const PaymentFailed = () => {
  const [searchParams] = useSearchParams();
  const reference = searchParams.get('reference');
  const message = searchParams.get('message');
  const navigate = useNavigate();

  return (
    <div className="payment-failed">
      <h1>❌ Payment Failed</h1>
      <p>{message || 'Your payment could not be processed.'}</p>
      {reference && <p>Reference: {reference}</p>}
      <button onClick={() => navigate('/loans')}>Back to Loans</button>
      <button onClick={() => window.location.reload()}>Try Again</button>
    </div>
  );
};
```

**Note**: The backend now handles the callback, verifies the payment automatically, and redirects to the appropriate frontend page with the results.

### 3. Payment Button Component

```javascript
const MakePaymentButton = ({ loan }) => {
  const handlePayment = async () => {
    const amount = prompt('Enter payment amount (minimum ₦1,000):');
    
    if (!amount || parseFloat(amount) < 1000) {
      alert('Invalid amount');
      return;
    }

    await initializePayment(loan.id, parseFloat(amount));
  };

  return (
    <button onClick={handlePayment}>
      Make Payment
    </button>
  );
};
```

## Testing with Paystack

### Test Cards

Paystack provides test cards for different scenarios:

#### **Successful Payment**
- **Card Number**: `4084084084084081`
- **CVV**: `408`
- **Expiry**: Any future date
- **PIN**: `0000`
- **OTP**: `123456`

#### **Insufficient Funds**
- **Card Number**: `5060666666666666666`
- **CVV**: `123`
- **Expiry**: Any future date
- **PIN**: `1234`

#### **Declined Transaction**
- **Card Number**: `5143010522339965`
- **CVV**: `123`
- **Expiry**: Any future date

### Testing Flow

1. **Initialize Payment**: Call the `/api/payments/initialize` endpoint
2. **Get Authorization URL**: Extract the `authorizationUrl` from the response
3. **Complete Payment**: Open the URL and use test card details
4. **Verify Payment**: After redirect, verify the payment using the reference
5. **Check Database**: Verify that the payment was recorded and loan balance updated

## Webhook Configuration

### 1. Set Up Webhook URL

For local testing, use ngrok:

```bash
ngrok http 5128
```

This gives you a public URL like: `https://abc123.ngrok.io`

### 2. Configure Webhook in Paystack Dashboard

1. Go to **Settings** > **API Keys & Webhooks**
2. Click **Add Webhook**
3. Enter your webhook URL: `https://abc123.ngrok.io/api/payments/webhook`
4. Select events: `charge.success`
5. Save

### 3. Test Webhook

Paystack will send a POST request to your webhook endpoint when a payment is successful. The webhook handler will:
- Verify the signature
- Extract payment details
- Update the payment record
- Update the loan balance

## Payment Processing Logic

### Amount Calculation

When a payment is made:

1. **Interest First**: Interest portion is deducted first
2. **Principal Second**: Remaining amount goes to principal
3. **Balance Update**: Loan `AmountDue` is reduced by payment amount
4. **Status Update**: If `AmountDue <= 0`, loan status changes to `Paid`

### Example

For a loan with:
- Principal: ₦100,000
- Interest Rate: 10%
- Term: 6 months
- Total Due: ₦110,000

Payment of ₦20,000:
- Interest per month: ₦110,000 - ₦100,000 = ₦10,000 / 6 = ₦1,667
- Interest deducted: ₦1,667
- Principal deducted: ₦20,000 - ₦1,667 = ₦18,333
- New balance: ₦110,000 - ₦20,000 = ₦90,000

## Security Considerations

1. **API Keys**: Never commit API keys to version control
2. **Webhook Signature**: Always verify webhook signatures
3. **HTTPS**: Use HTTPS in production
4. **Amount Validation**: Validate payment amounts on the server
5. **User Authorization**: Verify user owns the loan before processing payment

## Troubleshooting

### Payment Initialization Fails
- Check API keys are correct
- Verify loan exists and is active
- Ensure user is authenticated

### Payment Verification Fails
- Check reference is correct
- Verify payment was completed on Paystack
- Check Paystack dashboard for transaction status

### Webhook Not Receiving Events
- Verify webhook URL is publicly accessible
- Check ngrok is running (for local testing)
- Verify webhook is configured in Paystack dashboard
- Check signature verification logic

## Production Checklist

- [ ] Replace test API keys with live keys
- [ ] Update callback URL to production URL
- [ ] Configure production webhook URL
- [ ] Enable HTTPS
- [ ] Add proper error logging
- [ ] Set up monitoring for failed payments
- [ ] Test with small amounts first
- [ ] Implement payment retry logic
- [ ] Add email notifications for successful payments

## Support

For Paystack-specific issues:
- Documentation: [https://paystack.com/docs](https://paystack.com/docs)
- Support: [support@paystack.com](mailto:support@paystack.com)
- Test Environment: [https://dashboard.paystack.com/test](https://dashboard.paystack.com/test)
