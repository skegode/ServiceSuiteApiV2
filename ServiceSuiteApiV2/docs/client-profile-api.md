# ServiceSuite API V2 — Client Profile Endpoint

**Version:** v1  
**Base URL:** `https://<your-host>/`  
**Format:** JSON  
**Auth:** Bearer JWT (obtained from `/auth/token`)

---

## Table of Contents

1. [Authentication](#1-authentication)
2. [Get Client Profile](#2-get-client-profile)
3. [Response Schemas](#3-response-schemas)
4. [Error Responses](#4-error-responses)
5. [Code Examples](#5-code-examples)

---

## 1. Authentication

All endpoints (except `/auth/token`) require a **Bearer JWT** in the `Authorization` header.

### 1.1 Obtain a Token

**POST** `/auth/token`

> Rate-limited to **5 requests per minute** per client.

**Request Headers**

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

| Field          | Type    | Required | Description                          |
|----------------|---------|----------|--------------------------------------|
| `clientId`     | string  | Yes      | Your API client identifier           |
| `clientSecret` | string  | Yes      | Your API client secret               |
| `entityId`     | integer | Yes      | Your organisation/entity ID          |

**Success Response — 200 OK**

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

| Field         | Type    | Description                             |
|---------------|---------|-----------------------------------------|
| `accessToken` | string  | JWT to include in all subsequent calls  |
| `tokenType`   | string  | Always `"Bearer"`                       |
| `expiresIn`   | integer | Token lifetime in seconds               |

### 1.2 Using the Token

Include the token in the `Authorization` header on every protected request:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 2. Get Client Profile

Returns the client's personal details together with all their currently active loans in a single call.

**GET** `/loans/borrower/profile`

**Request Headers**

| Header          | Value                          |
|-----------------|-------------------------------|
| `Authorization` | `Bearer <access_token>`        |

**Query Parameters**

| Parameter | Type   | Required | Description                                                                 |
|-----------|--------|----------|-----------------------------------------------------------------------------|
| `search`  | string | Yes      | Phone number, National ID, or Borrower ID of the client to look up          |

**Example Request**

```
GET /loans/borrower/profile?search=0712345678
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response — 200 OK**

```json
{
  "success": true,
  "message": "2 active loan(s) found.",
  "data": {
    "client": {
      "borrowerId":     "101",
      "firstName":      "Jane",
      "otherName":      "Wanjiru",
      "nationalID":     "30123456",
      "phoneNumber":    "0712345678",
      "emailAddress":   "jane.wanjiru@email.com",
      "accountNo":      "ACC-00101",
      "accountStatus":  1
    },
    "activeLoans": [
      {
        "id":               "4501",
        "borrowerId":       "101",
        "firstName":        "Jane",
        "otherName":        "Wanjiru",
        "phoneNumber":      "0712345678",
        "emailAddress":     "jane.wanjiru@email.com",
        "nationalId":       "30123456",
        "amountToDisburse": 50000.00,
        "repaymentPeriod":  "Monthly Loan",
        "loanBalance":      32000.00,
        "penalty":          0.00,
        "arrears":          5000.00,
        "daysInArrears":    15,
        "outsourcedAmount": 37000.00,
        "productName":      "Monthly Loan",
        "branch":           "",
        "agent":            "John Kamau",
        "agentId":          "12"
      }
    ]
  }
}
```

**Notes**

- `activeLoans` contains only loans where `LoanBalance > 0` — fully repaid loans are excluded.
- `activeLoans` will be an empty array `[]` if the client exists but has no outstanding loans.
- `search` is matched against **phone number**, **National ID**, and **Borrower ID** — any one of these will work.
- The `entityId` is derived automatically from your token; you cannot query clients from a different entity.

---

## 3. Response Schemas

### 3.1 Wrapper — `ApiResponse<T>`

All endpoints return responses in this envelope:

| Field     | Type    | Description                                  |
|-----------|---------|----------------------------------------------|
| `success` | boolean | `true` on success, `false` on error          |
| `message` | string  | Human-readable status message                |
| `data`    | object  | The payload (null on error responses)        |

### 3.2 `ClientProfileDto`

| Field         | Type            | Description                          |
|---------------|-----------------|--------------------------------------|
| `client`      | `BorrowerDto`   | Personal details of the client       |
| `activeLoans` | `LoanDto[]`     | List of active (unpaid) loans        |

### 3.3 `BorrowerDto`

| Field           | Type    | Description                                               |
|-----------------|---------|-----------------------------------------------------------|
| `borrowerId`    | string  | Unique borrower identifier                                |
| `firstName`     | string  | First name                                                |
| `otherName`     | string  | Other name / surname                                      |
| `nationalID`    | string  | National ID number                                        |
| `phoneNumber`   | string  | Registered phone number                                   |
| `emailAddress`  | string  | Email address                                             |
| `accountNo`     | string  | Account number                                            |
| `accountStatus` | integer | Account status code (`1` = active, `0` = inactive)       |

### 3.4 `LoanDto`

| Field              | Type    | Description                                              |
|--------------------|---------|----------------------------------------------------------|
| `id`               | string  | Unique loan identifier                                   |
| `borrowerId`       | string  | Borrower identifier                                      |
| `firstName`        | string  | Borrower first name                                      |
| `otherName`        | string  | Borrower other name                                      |
| `phoneNumber`      | string  | Borrower phone number                                    |
| `emailAddress`     | string  | Borrower email address                                   |
| `nationalId`       | string  | Borrower national ID                                     |
| `amountToDisburse` | decimal | Original disbursed loan amount                           |
| `repaymentPeriod`  | string  | Loan product / repayment period name                     |
| `loanBalance`      | decimal | Current outstanding loan balance                         |
| `penalty`          | decimal | Accrued penalty amount                                   |
| `arrears`          | decimal | Total overdue amount                                     |
| `daysInArrears`    | integer | Number of days the loan is past due                      |
| `outsourcedAmount` | decimal | Total scheduled amount (sum of all unpaid installments)  |
| `productName`      | string  | Loan product name                                        |
| `branch`           | string  | Branch (reserved, currently empty)                       |
| `agent`            | string  | Collection agent full name                               |
| `agentId`          | string  | Collection agent identifier                              |

---

## 4. Error Responses

### 400 Bad Request — missing `search` parameter

```json
{
  "success": false,
  "message": "search parameter is required.",
  "data": null
}
```

### 401 Unauthorized — missing or expired token

```json
{
  "success": false,
  "message": "Invalid client credentials.",
  "data": null
}
```

### 404 Not Found — client not found

```json
{
  "success": false,
  "message": "Client not found.",
  "data": null
}
```

### 429 Too Many Requests — rate limit exceeded (token endpoint only)

Returned when the `/auth/token` endpoint is called more than 5 times per minute.

---

## 5. Code Examples

### cURL

**Step 1 — Get token**
```bash
curl -X POST https://<your-host>/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "clientId":     "your_client_id",
    "clientSecret": "your_client_secret",
    "entityId":     1
  }'
```

**Step 2 — Get client profile**
```bash
curl -X GET "https://<your-host>/loans/borrower/profile?search=0712345678" \
  -H "Authorization: Bearer <access_token>"
```

---

### JavaScript (fetch)

```javascript
const BASE_URL = "https://<your-host>";

async function getClientProfile(phoneNumber) {
  // 1. Authenticate
  const authRes = await fetch(`${BASE_URL}/auth/token`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      clientId:     "your_client_id",
      clientSecret: "your_client_secret",
      entityId:     1
    })
  });
  const { data: auth } = await authRes.json();

  // 2. Fetch client profile
  const profileRes = await fetch(
    `${BASE_URL}/loans/borrower/profile?search=${encodeURIComponent(phoneNumber)}`,
    { headers: { Authorization: `Bearer ${auth.accessToken}` } }
  );
  return profileRes.json();
}

getClientProfile("0712345678").then(console.log);
```

---

### C# (HttpClient)

```csharp
using var http = new HttpClient { BaseAddress = new Uri("https://<your-host>/") };

// 1. Authenticate
var authPayload = new { clientId = "your_client_id", clientSecret = "your_client_secret", entityId = 1 };
var authResp = await http.PostAsJsonAsync("auth/token", authPayload);
var authBody = await authResp.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();
var token = authBody!.Data!.AccessToken;

// 2. Fetch client profile
http.DefaultRequestHeaders.Authorization = new("Bearer", token);
var profileResp = await http.GetAsync($"loans/borrower/profile?search=0712345678");
var profile = await profileResp.Content.ReadFromJsonAsync<ApiResponse<ClientProfileDto>>();

Console.WriteLine($"Client: {profile!.Data!.Client.FirstName}");
Console.WriteLine($"Active loans: {profile.Data.ActiveLoans.Count}");
```

---

### PHP (cURL)

```php
<?php
$baseUrl = "https://<your-host>";

// 1. Authenticate
$ch = curl_init("$baseUrl/auth/token");
curl_setopt_array($ch, [
    CURLOPT_POST           => true,
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_HTTPHEADER     => ["Content-Type: application/json"],
    CURLOPT_POSTFIELDS     => json_encode([
        "clientId"     => "your_client_id",
        "clientSecret" => "your_client_secret",
        "entityId"     => 1
    ])
]);
$auth  = json_decode(curl_exec($ch), true);
$token = $auth["data"]["accessToken"];
curl_close($ch);

// 2. Fetch client profile
$phone = urlencode("0712345678");
$ch = curl_init("$baseUrl/loans/borrower/profile?search=$phone");
curl_setopt_array($ch, [
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_HTTPHEADER     => ["Authorization: Bearer $token"]
]);
$profile = json_decode(curl_exec($ch), true);
curl_close($ch);

print_r($profile);
```

---

*For access credentials or to report issues, contact the ServiceSuite API team.*
