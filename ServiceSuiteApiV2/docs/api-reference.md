# ServiceSuite API V2 â€” Full API Reference

**Version:** v1  
**Base URL:** `https://collectbox.servicesuitecloud.com/ServiceSuiteApiV2/`  
**Format:** JSON  
**Swagger UI:** `https://collectbox.servicesuitecloud.com/ServiceSuiteApiV2/swagger`

---

## Table of Contents

1. [Authentication](#1-authentication)
2. [Loans](#2-loans)
3. [Payments](#3-payments)
4. [Notifications](#4-notifications)
5. [Fraud Analytics](#5-fraud-analytics)
6. [Webhooks](#6-webhooks)
7. [Response Envelope](#7-response-envelope)
8. [Schema Reference](#8-schema-reference)
9. [Error Reference](#9-error-reference)
10. [Code Examples](#10-code-examples)

---

## 1. Authentication

### 1.1 Get Access Token

**POST** `/auth/token`

Generates a JWT for use on all protected endpoints.  
Rate-limited to **5 requests per minute** per client.

**Headers**

| Header         | Value              |
|----------------|--------------------|
| `Content-Type` | `application/json` |

**Request Body**

```json
{
  "clientId":     "your_client_id",
  "clientSecret": "your_client_secret",
  "entityId":     1
}
```

| Field          | Type    | Required | Description                 |
|----------------|---------|----------|-----------------------------|
| `clientId`     | string  | Yes      | API client identifier       |
| `clientSecret` | string  | Yes      | API client secret           |
| `entityId`     | integer | Yes      | Your organisation/entity ID |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "Token generated successfully.",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType":   "Bearer",
    "expiresIn":   3600
  }
}
```

| Field         | Type    | Description                            |
|---------------|---------|----------------------------------------|
| `accessToken` | string  | JWT to pass in all subsequent requests |
| `tokenType`   | string  | Always `"Bearer"`                      |
| `expiresIn`   | integer | Lifetime in seconds                    |

---

### 1.2 Create API Client *(Admin only)*

**POST** `/auth/clients`

Provisions a new API client (generates `clientId` + `clientSecret`).  
Requires the `X-Admin-Key` header â€” not for use by integrators.

**Headers**

| Header         | Value              |
|----------------|--------------------|
| `Content-Type` | `application/json` |
| `X-Admin-Key`  | `<admin_key>`      |

**Request Body**

```json
{
  "entityId":   "1",
  "clientName": "My Integration App"
}
```

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "Client saved. Store the ClientSecret now â€” it will not be shown again.",
  "data": {
    "clientId":     "generated_id",
    "clientSecret": "generated_secret_shown_once"
  }
}
```

> **Important:** The `clientSecret` is only returned once. Store it securely immediately.

---

### Using the Token

Pass the token in the `Authorization` header on every protected request:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 2. Loans

All loan endpoints require `Authorization: Bearer <token>`.  
The `entityId` is automatically scoped from your token â€” you can only access your own entity's data.

---

### 2.1 Get Loans by Filter

**POST** `/loans/GetLoansByFilter`

Returns active loans matching the supplied filter criteria. All filter fields are optional.

**Request Body**

```json
{
  "minDays":   30,
  "maxDays":   90,
  "minAmount": 5000,
  "maxAmount": 100000,
  "minOlb":    1000,
  "maxOlb":    50000
}
```

| Field       | Type    | Required | Description                          |
|-------------|---------|----------|--------------------------------------|
| `minDays`   | integer | No       | Minimum days past due                |
| `maxDays`   | integer | No       | Maximum days past due                |
| `minAmount` | decimal | No       | Minimum original disbursed amount    |
| `maxAmount` | decimal | No       | Maximum original disbursed amount    |
| `minOlb`    | decimal | No       | Minimum outstanding loan balance     |
| `maxOlb`    | decimal | No       | Maximum outstanding loan balance     |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "",
  "data": {
    "count": 10,
    "data": [ /* array of LoanDto */ ]
  }
}
```

---

### 2.2 Get Active Loans

**GET** `/loans/ActiveLoans`

Returns a list of active loans (balance > 0) with optional keyword search. Ordered by loan ID descending.

**Query Parameters**

| Parameter | Type    | Required | Default | Description                                                  |
|-----------|---------|----------|---------|--------------------------------------------------------------|
| `search`  | string  | No       | â€”       | Search by borrower name, phone number, or loan ID            |
| `top`     | integer | No       | `20`    | Maximum number of records to return                          |

**Example**

```
GET /loans/ActiveLoans?search=0712345678&top=50
```

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "",
  "data": {
    "count": 3,
    "data": [ /* array of LoanDto */ ]
  }
}
```

---

### 2.3 Get Loan by ID

**GET** `/loans/{loanId}`  
**GET** `/loans/GetLoanById/{loanId}`

Returns a single loan by its ID.

**Path Parameters**

| Parameter | Type   | Required | Description     |
|-----------|--------|----------|-----------------|
| `loanId`  | string | Yes      | Numeric loan ID |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "Loan retrieved successfully.",
  "data": { /* LoanDto */ }
}
```

**Response â€” 404 Not Found**

```json
{
  "success": false,
  "message": "Loan 9999 not found or access denied.",
  "data": null
}
```

---

### 2.4 Get Loan Details

**GET** `/loans/details/{loanId}`

Returns detailed form/metadata items attached to a loan (from `BorrowerDetails` and `BorrowerFormItems`).

**Path Parameters**

| Parameter | Type   | Required | Description     |
|-----------|--------|----------|-----------------|
| `loanId`  | string | Yes      | Numeric loan ID |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "",
  "data": [
    {
      "id":        1,
      "loanId":    "4501",
      "itemName":  "Employer Name",
      "itemValue": "ABC Company Ltd"
    }
  ]
}
```

---

### 2.5 Get Loan Balance

**GET** `/loans/balance/{loanId}`

Returns the current outstanding balance for a single loan.

**Path Parameters**

| Parameter | Type   | Required | Description     |
|-----------|--------|----------|-----------------|
| `loanId`  | string | Yes      | Numeric loan ID |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "Balance retrieved successfully.",
  "data": {
    "id":          "4501",
    "loanBalance": 32000.00
  }
}
```

---

### 2.6 Get Disbursed Loans

**GET** `/loans/disbursements`

Returns all loans disbursed within a date range, ordered by disbursement date descending.

**Query Parameters**

| Parameter   | Type     | Required | Description                          |
|-------------|----------|----------|--------------------------------------|
| `startDate` | datetime | Yes      | Start of date range (e.g. `2024-01-01`) |
| `endDate`   | datetime | Yes      | End of date range (e.g. `2024-01-31`)   |

**Example**

```
GET /loans/disbursements?startDate=2024-01-01&endDate=2024-01-31
```

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "5 disbursement(s) found.",
  "data": [
    {
      "loanId":           "4501",
      "borrowerId":       "101",
      "borrowerName":     "Jane Wanjiru",
      "phoneNumber":      "0712345678",
      "loanAmount":       50000.00,
      "amountToDisburse": 50000.00,
      "disbursementDate": "2024-01-15T00:00:00",
      "productName":      "Monthly Loan",
      "currentBalance":   32000.00
    }
  ]
}
```

---

### 2.7 Get Payments

**GET** `/loans/payments`

Returns all payments received within a date range (from the Transactions database), ordered by date descending.

**Query Parameters**

| Parameter   | Type     | Required | Description           |
|-------------|----------|----------|-----------------------|
| `startDate` | datetime | Yes      | Start of date range   |
| `endDate`   | datetime | Yes      | End of date range     |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "12 payment(s) found.",
  "data": [
    {
      "id":              1,
      "transId":         "QHX7YABCDE",
      "transAmount":     2000.00,
      "billRefNumber":   "4501",
      "payerName":       "Jane Wanjiru",
      "dateDone":        "2024-01-20T10:34:00",
      "isPosted":        1,
      "transactionType": "Pay Bill",
      "loanId":          "4501"
    }
  ]
}
```

---

### 2.8 Get Overdue Loans

**GET** `/loans/overdue`

Returns loans that are past their due date, ordered by days in arrears descending.

**Query Parameters**

| Parameter | Type    | Required | Default | Description                                   |
|-----------|---------|----------|---------|-----------------------------------------------|
| `minDays` | integer | No       | `1`     | Minimum number of days past due               |
| `top`     | integer | No       | `50`    | Maximum number of records to return           |

**Example**

```
GET /loans/overdue?minDays=30&top=100
```

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "8 overdue loan(s) found with at least 30 day(s) in arrears.",
  "data": {
    "count": 8,
    "data": [ /* array of LoanDto */ ]
  }
}
```

---

### 2.9 Get Loans Due Today

**GET** `/loans/due-today`

Returns loans that have an installment scheduled for today (unpaid, status = 0), ordered by amount due descending.

**Query Parameters**

| Parameter | Type    | Required | Default | Description                         |
|-----------|---------|----------|---------|-------------------------------------|
| `top`     | integer | No       | `500`   | Maximum number of records to return |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "15 loan(s) due today.",
  "data": [
    {
      "loanId":           "4501",
      "firstName":        "Jane",
      "otherName":        "Wanjiru",
      "phoneNumber":      "0712345678",
      "emailAddress":     "jane@email.com",
      "nationalId":       "30123456",
      "amountToDisburse": 50000.00,
      "loanBalance":      32000.00,
      "dueTodayAmount":   5000.00,
      "productName":      "Monthly Loan",
      "dueDate":          "2024-06-16T00:00:00"
    }
  ]
}
```

---

### 2.10 Get Borrower

**GET** `/loans/borrower`

Looks up a client's personal details by phone number, National ID, or borrower ID.

**Query Parameters**

| Parameter | Type   | Required | Description                                          |
|-----------|--------|----------|------------------------------------------------------|
| `search`  | string | Yes      | Phone number, National ID, or Borrower ID            |

**Example**

```
GET /loans/borrower?search=0712345678
```

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "",
  "data": {
    "borrowerId":    "101",
    "firstName":     "Jane",
    "otherName":     "Wanjiru",
    "nationalID":    "30123456",
    "phoneNumber":   "0712345678",
    "emailAddress":  "jane@email.com",
    "accountNo":     "ACC-00101",
    "accountStatus": 1
  }
}
```

---

### 2.11 Get Borrower Loans

**GET** `/loans/borrower/loans`

Returns all loans (active and historical) for a specific borrower.

**Query Parameters**

| Parameter | Type   | Required | Description                                          |
|-----------|--------|----------|------------------------------------------------------|
| `search`  | string | Yes      | Phone number, National ID, or Borrower ID            |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "3 loan(s) found.",
  "data": {
    "count": 3,
    "data": [ /* array of LoanDto */ ]
  }
}
```

---

### 2.12 Get Borrower Statement

**GET** `/loans/borrower/statement`

Returns the full transaction statement for a borrower â€” all disbursements, repayments, and adjustments.

**Query Parameters**

| Parameter | Type   | Required | Description                                          |
|-----------|--------|----------|------------------------------------------------------|
| `search`  | string | Yes      | Phone number, National ID, or Borrower ID            |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "24 statement line(s) found.",
  "data": {
    "borrowerId":   "101",
    "borrowerName": "Jane Wanjiru",
    "phoneNumber":  "0712345678",
    "totalLines":   24,
    "statement": [
      {
        "id":             1,
        "loanId":         "4501",
        "amount":         50000.00,
        "transType":      1,
        "narration":      "Loan Disbursement",
        "mpesaRef":       "",
        "loanBalance":    50000.00,
        "accountBalance": 0.00,
        "transactedDate": "2024-01-15T00:00:00"
      }
    ]
  }
}
```

**Transaction Type codes**

| `transType` | Meaning          |
|-------------|------------------|
| `1`         | Disbursement     |
| `2`         | Repayment        |
| `3`         | Penalty          |
| `6`         | Write-off        |

---

### 2.13 Get Client Profile *(Combined)*

**GET** `/loans/borrower/profile`

Returns a client's personal details **and** all their currently active loans in a single call. Best used for client lookup screens or USSD lookup flows.

**Query Parameters**

| Parameter | Type   | Required | Description                                          |
|-----------|--------|----------|------------------------------------------------------|
| `search`  | string | Yes      | Phone number, National ID, or Borrower ID            |

**Example**

```
GET /loans/borrower/profile?search=0712345678
```

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "2 active loan(s) found.",
  "data": {
    "client": {
      "borrowerId":    "101",
      "firstName":     "Jane",
      "otherName":     "Wanjiru",
      "nationalID":    "30123456",
      "phoneNumber":   "0712345678",
      "emailAddress":  "jane@email.com",
      "accountNo":     "ACC-00101",
      "accountStatus": 1
    },
    "activeLoans": [
      {
        "id":               "4501",
        "borrowerId":       "101",
        "firstName":        "Jane",
        "otherName":        "Wanjiru",
        "phoneNumber":      "0712345678",
        "nationalId":       "30123456",
        "amountToDisburse": 50000.00,
        "repaymentPeriod":  "Monthly Loan",
        "loanBalance":      32000.00,
        "penalty":          0.00,
        "arrears":          5000.00,
        "daysInArrears":    15,
        "productName":      "Monthly Loan",
        "agent":            "John Kamau",
        "agentId":          "12"
      }
    ]
  }
}
```

> `activeLoans` only includes loans where `LoanBalance > 0`. The array is empty `[]` if the client has no outstanding loans.

---

## 3. Payments

### 3.1 Initiate M-Pesa STK Push

**POST** `/payments/stk-push`

Triggers an M-Pesa STK push prompt on the customer's phone. The M-Pesa credentials (shortcode, passkey, etc.) are configured per entity in the database.

**Request Body**

```json
{
  "amount":           2000,
  "phoneNumber":      "254712345678",
  "accountReference": "4501",
  "transactionDesc":  "Loan Repayment"
}
```

| Field              | Type    | Required | Description                                            |
|--------------------|---------|----------|--------------------------------------------------------|
| `amount`           | decimal | Yes      | Amount to charge. Must be greater than `0`             |
| `phoneNumber`      | string  | Yes      | Customer phone in international format (`254XXXXXXXXX`) |
| `accountReference` | string  | No       | Reference shown on the STK prompt (e.g. loan ID)       |
| `transactionDesc`  | string  | No       | Description shown on the STK prompt. Defaults to `"Payment"` |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "STK push initiated.",
  "data":    "ws_CO_16062024103400001234567890"
}
```

> `data` contains the M-Pesa `CheckoutRequestID`. Use it to reconcile the payment when the M-Pesa callback arrives.

**Response â€” 500** â€” if the STK push request to Safaricom fails.

---

## 4. Notifications

### 4.1 Send SMS

**POST** `/notifications/sms`

Sends an SMS to the specified phone number using the entity's configured SMS provider.

**Request Body**

```json
{
  "message":      "Dear Jane, your loan balance is KES 32,000. Please pay to avoid penalties.",
  "phoneNumber":  "0712345678",
  "scheduleDate": "2024-06-16T10:00:00"
}
```

| Field          | Type     | Required | Description                                              |
|----------------|----------|----------|----------------------------------------------------------|
| `message`      | string   | Yes      | SMS body text                                            |
| `phoneNumber`  | string   | Yes      | Recipient phone number                                   |
| `scheduleDate` | datetime | No       | Future datetime to schedule delivery. Omit to send now   |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "Message sent successfully.",
  "data":    null
}
```

---

## 5. Fraud Analytics

All fraud endpoints require `Authorization: Bearer <token>`.  
Signals are scoped to your entity and based on live loan and payment data.

**Risk levels:** `"High"` | `"Medium"` | `"Low"`

---

### 5.1 Full Fraud Report *(All signals combined)*

**GET** `/analytics/fraud/report`

Runs all fraud signal queries in parallel and returns everything in one call. Use for dashboard initial load.

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "Fraud report generated. High-risk agents: 2, High-risk shops: 1, ...",
  "data": {
    "summary": {
      "highRiskAgentCount":      2,
      "highRiskShopCount":       1,
      "duplicateIdentityCount":  3,
      "loanStackingCount":       5,
      "zeroPaymentLoanCount":    8,
      "unpostedPaymentCount":    2,
      "unpostedPaymentAmount":   15000.00,
      "estimatedFraudExposure":  320000.00,
      "generatedAt":             "2024-06-16T10:00:00Z"
    },
    "agentSignals":        [ /* array of AgentFraudSignal */ ],
    "shopSignals":         [ /* array of ShopFraudSignal */ ],
    "duplicateIdentities": [ /* array of DuplicateIdentitySignal */ ],
    "loanStacking":        [ /* array of LoanStackingSignal */ ],
    "suspiciousLoans":     [ /* array of SuspiciousLoanSignal */ ],
    "paymentFraud": {
      "unpostedCount":  2,
      "unpostedAmount": 15000.00,
      "oldestUnposted": "2024-06-10T00:00:00",
      "newestUnposted": "2024-06-13T00:00:00"
    }
  }
}
```

---

### 5.2 Agent Fraud Signals

**GET** `/analytics/fraud/agents`

Flags collection agents with high default rates, write-off concentration, or arrears-heavy portfolios.

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "10 agent(s) analysed â€” 2 High risk, 3 Medium risk.",
  "data": [
    {
      "agentId":       "12",
      "agentName":     "John Kamau",
      "totalLoans":    50,
      "activeLoans":   30,
      "defaultedLoans": 20,
      "defaultRate":   0.40,
      "totalArrears":  200000.00,
      "totalDisbursed": 500000.00,
      "writeoffCount": 5,
      "totalWrittenOff": 50000.00,
      "riskLevel":     "High",
      "flags":         ["High default rate (40%)", "Concentrated write-offs"]
    }
  ]
}
```

---

### 5.3 Shop / Merchant Fraud Signals

**GET** `/analytics/fraud/shops`

Groups borrowers by their onboarding agent (shop) and flags shops where the majority of clients are defaulting.

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "8 shop(s) analysed â€” 1 High risk.",
  "data": [
    {
      "agentId":              "12",
      "agentName":            "John Kamau Shop",
      "uniqueBorrowers":      25,
      "defaultedBorrowers":   20,
      "borrowerDefaultRate":  0.80,
      "totalDisbursed":       250000.00,
      "totalArrears":         180000.00,
      "portfolioAtRisk":      0.72,
      "riskLevel":            "High"
    }
  ]
}
```

---

### 5.4 Duplicate Identity Detection

**GET** `/analytics/fraud/borrowers/duplicate-ids`

Identifies borrowers sharing the same National ID â€” a signal of synthetic or stolen identity fraud.

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "3 duplicate National ID(s) detected.",
  "data": [
    {
      "nationalId":         "30123456",
      "borrowerCount":      2,
      "borrowerIds":        "101, 205",
      "names":              "Jane Wanjiru, Jane W Mwangi",
      "phoneNumbers":       "0712345678, 0798765432",
      "totalActiveBalance": 82000.00
    }
  ]
}
```

---

### 5.5 Loan Stacking Detection

**GET** `/analytics/fraud/borrowers/stacking`

Identifies borrowers who hold more than one active loan simultaneously â€” indicating over-borrowing or coordinated fraud.

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "5 borrower(s) with multiple active loans.",
  "data": [
    {
      "borrowerId":       "101",
      "borrowerName":     "Jane Wanjiru",
      "phoneNumber":      "0712345678",
      "nationalId":       "30123456",
      "agentName":        "John Kamau",
      "activeLoanCount":  2,
      "totalBalance":     82000.00,
      "totalDisbursed":   100000.00,
      "firstLoanDate":    "2023-10-01T00:00:00",
      "latestLoanDate":   "2024-01-15T00:00:00"
    }
  ]
}
```

---

### 5.6 Suspicious Loans

**GET** `/analytics/fraud/loans/suspicious`

Returns overdue loans with little or no repayment activity. Zero-payment loans appear first â€” the strongest indicator of intentional fraud.

**Query Parameters**

| Parameter | Type    | Required | Default | Description                                  |
|-----------|---------|----------|---------|----------------------------------------------|
| `minDays` | integer | No       | `30`    | Minimum days overdue to include in results   |

**Example**

```
GET /analytics/fraud/loans/suspicious?minDays=60
```

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "8 suspicious loan(s) â€” 3 with zero repayments.",
  "data": [
    {
      "loanId":           "4501",
      "borrowerId":       "101",
      "borrowerName":     "Jane Wanjiru",
      "phoneNumber":      "0712345678",
      "nationalId":       "30123456",
      "agentId":          "12",
      "agentName":        "John Kamau",
      "amountDisbursed":  50000.00,
      "disbursementDate": "2024-01-15T00:00:00",
      "totalArrears":     50000.00,
      "daysInArrears":    152,
      "loanBalance":      50000.00,
      "paymentCount":     0,
      "totalPaid":        0.00
    }
  ]
}
```

---

### 5.7 Unposted Payment Fraud

**GET** `/analytics/fraud/payments/unposted`

Flags payments received but not posted to a loan for 3+ days â€” a potential signal of staff diverting funds.

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "Alert: 2 unposted payment(s) totalling 15,000.00 older than 3 days.",
  "data": {
    "unpostedCount":  2,
    "unpostedAmount": 15000.00,
    "oldestUnposted": "2024-06-10T00:00:00",
    "newestUnposted": "2024-06-13T00:00:00"
  }
}
```

If no anomalies exist:

```json
{
  "success": true,
  "message": "No unposted payment anomalies detected.",
  "data": {
    "unpostedCount":  0,
    "unpostedAmount": 0.00,
    "oldestUnposted": null,
    "newestUnposted": null
  }
}
```

---

## 6. Webhooks

### 6.1 Spin Analysis Webhook

**POST** `/webhooks/spin-analysis`

Receives an analysis result payload from the Spin financial analysis service. The payload is logged to a dated file on the server.

**Authentication:** HTTP Basic Auth (not Bearer JWT)

```
Authorization: Basic <base64(username:password)>
```

> Webhook credentials are configured server-side. Contact the ServiceSuite team for the `username` and `password`.

**Request Body**

```json
{
  "file_unique_id": "abc123xyz",
  "file_type":      "mpesa",
  "phone":          "0712345678",
  "id_number":      "30123456",
  "bank_name":      null,
  "account_number": null,
  "duration":       6.0,
  "json_data":      { },
  "timestamp":      "2024-06-16T10:00:00Z",
  "state_name":     "completed"
}
```

| Field            | Type    | Required | Description                                              |
|------------------|---------|----------|----------------------------------------------------------|
| `file_unique_id` | string  | Yes      | Unique ID of the analysed file                           |
| `file_type`      | string  | Yes      | `"mpesa"` or `"bank"`                                    |
| `phone`          | string  | No       | Phone number (M-Pesa statements)                         |
| `id_number`      | string  | No       | National ID (M-Pesa statements)                          |
| `bank_name`      | string  | No       | Bank name (bank statements)                              |
| `account_number` | string  | No       | Bank account number                                      |
| `duration`       | decimal | No       | Statement period in months                               |
| `json_data`      | object  | No       | Raw analysis output from Spin                            |
| `timestamp`      | datetime| No       | Time the analysis was completed                          |
| `state_name`     | string  | No       | Analysis state (e.g. `"completed"`, `"failed"`)          |

**Response â€” 200 OK**

```json
{
  "success": true,
  "message": "Webhook received."
}
```

---

## 7. Response Envelope

Every response is wrapped in the same envelope:

```json
{
  "success": true,
  "message": "Human-readable status",
  "data":    null
}
```

| Field     | Type    | Description                              |
|-----------|---------|------------------------------------------|
| `success` | boolean | `true` on success, `false` on error      |
| `message` | string  | Human-readable status or error detail    |
| `data`    | any     | Response payload. `null` on error        |

---

## 8. Schema Reference

### LoanDto

| Field              | Type    | Description                                             |
|--------------------|---------|---------------------------------------------------------|
| `id`               | string  | Loan ID                                                 |
| `borrowerId`       | string  | Borrower ID                                             |
| `firstName`        | string  | Borrower first name                                     |
| `otherName`        | string  | Borrower other name                                     |
| `phoneNumber`      | string  | Borrower phone number                                   |
| `emailAddress`     | string  | Borrower email                                          |
| `nationalId`       | string  | Borrower National ID                                    |
| `amountToDisburse` | decimal | Original disbursed amount                               |
| `repaymentPeriod`  | string  | Loan product name                                       |
| `loanBalance`      | decimal | Current outstanding balance                             |
| `penalty`          | decimal | Accrued penalty                                         |
| `arrears`          | decimal | Total overdue amount                                    |
| `daysInArrears`    | integer | Days past due                                           |
| `outsourcedAmount` | decimal | Sum of all unpaid installments                          |
| `productName`      | string  | Loan product name                                       |
| `branch`           | string  | Branch (reserved)                                       |
| `agent`            | string  | Collection agent full name                              |
| `agentId`          | string  | Collection agent ID                                     |

### BorrowerDto

| Field           | Type    | Description                                 |
|-----------------|---------|---------------------------------------------|
| `borrowerId`    | string  | Unique borrower ID                          |
| `firstName`     | string  | First name                                  |
| `otherName`     | string  | Other name / surname                        |
| `nationalID`    | string  | National ID                                 |
| `phoneNumber`   | string  | Phone number                                |
| `emailAddress`  | string  | Email address                               |
| `accountNo`     | string  | Account number                              |
| `accountStatus` | integer | `1` = Active, `0` = Inactive                |

### BorrowerStatementLineDto

| Field            | Type     | Description                                      |
|------------------|----------|--------------------------------------------------|
| `id`             | integer  | Statement line ID                                |
| `loanId`         | string   | Associated loan ID                               |
| `amount`         | decimal  | Transaction amount                               |
| `transType`      | integer  | Transaction type code (see table in Â§2.12)       |
| `narration`      | string   | Transaction description                          |
| `mpesaRef`       | string   | M-Pesa transaction reference (if applicable)     |
| `loanBalance`    | decimal  | Loan balance after this transaction              |
| `accountBalance` | decimal  | Account balance after this transaction           |
| `transactedDate` | datetime | Date and time of the transaction                 |

### DisbursedLoanDto

| Field              | Type     | Description                   |
|--------------------|----------|-------------------------------|
| `loanId`           | string   | Loan ID                       |
| `borrowerId`       | string   | Borrower ID                   |
| `borrowerName`     | string   | Full name                     |
| `phoneNumber`      | string   | Phone number                  |
| `loanAmount`       | decimal  | Applied loan amount           |
| `amountToDisburse` | decimal  | Actual disbursed amount       |
| `disbursementDate` | datetime | Date of disbursement          |
| `productName`      | string   | Loan product                  |
| `currentBalance`   | decimal  | Current outstanding balance   |

### PaymentDto

| Field             | Type     | Description                                 |
|-------------------|----------|---------------------------------------------|
| `id`              | integer  | Payment record ID                           |
| `transId`         | string   | M-Pesa transaction ID                       |
| `transAmount`     | decimal  | Payment amount                              |
| `billRefNumber`   | string   | Bill reference (usually the loan account)   |
| `payerName`       | string   | Name of the payer                           |
| `dateDone`        | datetime | Payment date                                |
| `isPosted`        | integer  | `1` = posted to loan, `0` = pending         |
| `transactionType` | string   | e.g. `"Pay Bill"`                           |
| `loanId`          | string   | Associated loan ID (may be null)            |

### DueTodayLoanDto

| Field              | Type     | Description                          |
|--------------------|----------|--------------------------------------|
| `loanId`           | string   | Loan ID                              |
| `firstName`        | string   | Borrower first name                  |
| `otherName`        | string   | Borrower other name                  |
| `phoneNumber`      | string   | Borrower phone number                |
| `emailAddress`     | string   | Borrower email                       |
| `nationalId`       | string   | Borrower National ID                 |
| `amountToDisburse` | decimal  | Original loan amount                 |
| `loanBalance`      | decimal  | Current outstanding balance          |
| `dueTodayAmount`   | decimal  | Installment amount due today         |
| `productName`      | string   | Loan product                         |
| `dueDate`          | datetime | Today's date                         |

---

## 9. Error Reference

| HTTP Status | Meaning                     | When it occurs                                              |
|-------------|----------------------------|-------------------------------------------------------------|
| `400`       | Bad Request                | Missing required field, invalid parameter value             |
| `401`       | Unauthorized               | Missing, expired, or invalid token / wrong credentials      |
| `404`       | Not Found                  | Resource does not exist or belongs to another entity        |
| `429`       | Too Many Requests          | Rate limit exceeded on `/auth/token` (5 req/min)            |
| `500`       | Internal Server Error      | Unexpected server-side error (check server logs)            |

All error responses follow the same envelope:

```json
{
  "success": false,
  "message": "Description of what went wrong.",
  "data":    null
}
```

---

## 10. Code Examples

### cURL â€” Full flow

```bash
# 1. Get token
TOKEN=$(curl -s -X POST https://collectbox.servicesuitecloud.com/ServiceSuiteApiV2/auth/token \
  -H "Content-Type: application/json" \
  -d '{"clientId":"your_id","clientSecret":"your_secret","entityId":1}' \
  | jq -r '.data.accessToken')

# 2. Get client profile
curl -X GET "https://collectbox.servicesuitecloud.com/ServiceSuiteApiV2/loans/borrower/profile?search=0712345678" \
  -H "Authorization: Bearer $TOKEN"

# 3. Initiate STK push
curl -X POST https://collectbox.servicesuitecloud.com/ServiceSuiteApiV2/payments/stk-push \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"amount":2000,"phoneNumber":"254712345678","accountReference":"4501"}'

# 4. Get overdue loans (30+ days)
curl -X GET "https://collectbox.servicesuitecloud.com/ServiceSuiteApiV2/loans/overdue?minDays=30&top=100" \
  -H "Authorization: Bearer $TOKEN"

# 5. Full fraud report
curl -X GET "https://collectbox.servicesuitecloud.com/ServiceSuiteApiV2/analytics/fraud/report" \
  -H "Authorization: Bearer $TOKEN"
```

---

### JavaScript (fetch)

```javascript
const BASE = "https://collectbox.servicesuitecloud.com/ServiceSuiteApiV2";

async function apiClient(clientId, clientSecret, entityId) {
  const res = await fetch(`${BASE}/auth/token`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ clientId, clientSecret, entityId })
  });
  const { data } = await res.json();
  const token = data.accessToken;

  const get  = (path) => fetch(`${BASE}${path}`, { headers: { Authorization: `Bearer ${token}` } }).then(r => r.json());
  const post = (path, body) => fetch(`${BASE}${path}`, { method: "POST", headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json" }, body: JSON.stringify(body) }).then(r => r.json());

  return { get, post };
}

// Usage
const api = await apiClient("your_id", "your_secret", 1);

const profile  = await api.get("/loans/borrower/profile?search=0712345678");
const overdue  = await api.get("/loans/overdue?minDays=30");
const fraud    = await api.get("/analytics/fraud/report");
const stkPush  = await api.post("/payments/stk-push", { amount: 2000, phoneNumber: "254712345678", accountReference: "4501" });

console.log(profile, overdue, fraud, stkPush);
```

---

### C# (HttpClient)

```csharp
using var http = new HttpClient { BaseAddress = new Uri("https://collectbox.servicesuitecloud.com/ServiceSuiteApiV2/") };

// 1. Authenticate
var auth = await http.PostAsJsonAsync("auth/token", new
{
    clientId = "your_id", clientSecret = "your_secret", entityId = 1
});
var authData = (await auth.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>())!.Data!;
http.DefaultRequestHeaders.Authorization = new("Bearer", authData.AccessToken);

// 2. Client profile
var profile = await http.GetFromJsonAsync<ApiResponse<ClientProfileDto>>(
    "loans/borrower/profile?search=0712345678");

// 3. Overdue loans
var overdue = await http.GetFromJsonAsync<ApiResponse<LoanResponse>>(
    "loans/overdue?minDays=30&top=100");

// 4. Fraud report
var fraud = await http.GetFromJsonAsync<ApiResponse<FraudAnalyticsReport>>(
    "analytics/fraud/report");

// 5. STK Push
var stk = await http.PostAsJsonAsync("payments/stk-push", new
{
    amount = 2000m, phoneNumber = "254712345678", accountReference = "4501"
});
```

---

### PHP (cURL)

```php
<?php
$base = "https://collectbox.servicesuitecloud.com/ServiceSuiteApiV2";

function apiGet($base, $token, $path) {
    $ch = curl_init("$base$path");
    curl_setopt_array($ch, [
        CURLOPT_RETURNTRANSFER => true,
        CURLOPT_HTTPHEADER     => ["Authorization: Bearer $token"]
    ]);
    $res = curl_exec($ch);
    curl_close($ch);
    return json_decode($res, true);
}

function apiPost($base, $token, $path, $body) {
    $ch = curl_init("$base$path");
    curl_setopt_array($ch, [
        CURLOPT_POST           => true,
        CURLOPT_RETURNTRANSFER => true,
        CURLOPT_HTTPHEADER     => [
            "Authorization: Bearer $token",
            "Content-Type: application/json"
        ],
        CURLOPT_POSTFIELDS     => json_encode($body)
    ]);
    $res = curl_exec($ch);
    curl_close($ch);
    return json_decode($res, true);
}

// 1. Get token
$auth  = apiPost($base, "", "/auth/token", [
    "clientId" => "your_id", "clientSecret" => "your_secret", "entityId" => 1
]);
$token = $auth["data"]["accessToken"];

// 2. Client profile
$profile = apiGet($base, $token, "/loans/borrower/profile?search=0712345678");

// 3. Overdue loans
$overdue = apiGet($base, $token, "/loans/overdue?minDays=30&top=100");

// 4. Fraud report
$fraud = apiGet($base, $token, "/analytics/fraud/report");

// 5. STK Push
$stk = apiPost($base, $token, "/payments/stk-push", [
    "amount" => 2000, "phoneNumber" => "254712345678", "accountReference" => "4501"
]);

print_r($profile);
```

---

*For access credentials, integration support, or to report issues â€” contact the ServiceSuite API team.*

