# k6 Load Test

This folder contains a k6 load test for the protected `POST /api/chat` endpoint.

## What it does

- authenticates once through `POST /api/auth/token`
- simulates `100` concurrent users
- repeatedly calls `POST /api/chat`
- measures:
  - response time
  - error rate
  - throughput

## Script

- [`chat-load-test.js`](/C:/Users/ShridharNanavare/AICopilotPLM/tests/load/chat-load-test.js)

## Prerequisites

1. Start the API locally
2. Ensure the API can reach its configured dependencies
3. Install k6 locally

Windows example:

```powershell
winget install k6.k6
```

## Run locally

From the repo root:

```powershell
k6 run .\tests\load\chat-load-test.js
```

## Useful environment variables

```powershell
$env:BASE_URL = "http://127.0.0.1:5099"
$env:K6_USERNAME = "user"
$env:K6_PASSWORD = "user123!"
$env:K6_QUERY = "Recommend parts related to motor housing"
k6 run .\tests\load\chat-load-test.js
```

## What to look at

k6 will print the main metrics at the end of the run:

- `http_req_duration`
  overall request latency
- `chat_response_time`
  custom latency metric for `/api/chat`
- `http_req_failed`
  transport/request failure rate
- `chat_error_rate`
  failed checks against expected response structure
- `chat_requests_total`
  total completed chat requests

## Notes

- The script uses the default sample JWT user from API config unless overridden by environment variables.
- Because `/api/chat` depends on the rest of the application stack, results will reflect database and AI provider availability too.
- If you want, this can be extended next with staged ramp-up, CSV/JSON result export, or separate scenarios for `User`, `Engineer`, and `Admin` roles.
