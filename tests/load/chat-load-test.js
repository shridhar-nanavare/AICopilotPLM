import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const responseTime = new Trend('chat_response_time', true);
const errorRate = new Rate('chat_error_rate');
const throughput = new Counter('chat_requests_total');

const baseUrl = __ENV.BASE_URL || 'http://127.0.0.1:5099';
const username = __ENV.K6_USERNAME || 'user';
const password = __ENV.K6_PASSWORD || 'user123!';
const query = __ENV.K6_QUERY || 'Recommend parts related to motor housing';

export const options = {
  scenarios: {
    chat_load: {
      executor: 'constant-vus',
      vus: 100,
      duration: '1m',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    chat_error_rate: ['rate<0.05'],
    http_req_duration: ['p(95)<3000'],
    chat_response_time: ['p(95)<3000'],
  },
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'max'],
};

export function setup() {
  const tokenResponse = http.post(
    `${baseUrl}/api/auth/token`,
    JSON.stringify({
      username,
      password,
    }),
    {
      headers: {
        'Content-Type': 'application/json',
      },
    },
  );

  check(tokenResponse, {
    'auth token request succeeded': (res) => res.status === 200,
    'auth token is present': (res) => !!res.json('accessToken'),
  });

  return {
    accessToken: tokenResponse.json('accessToken'),
  };
}

export default function (data) {
  const response = http.post(
    `${baseUrl}/api/chat`,
    JSON.stringify({
      sessionId: null,
      query,
    }),
    {
      headers: {
        Authorization: `Bearer ${data.accessToken}`,
        'Content-Type': 'application/json',
      },
      tags: {
        endpoint: 'chat',
      },
    },
  );

  throughput.add(1);
  responseTime.add(response.timings.duration);

  const success = check(response, {
    'chat returned 200': (res) => res.status === 200,
    'chat response has sessionId': (res) => !!res.json('sessionId'),
    'chat response has summary': (res) => typeof res.json('summary') === 'string',
    'chat response has recommendations array': (res) => Array.isArray(res.json('recommendations')),
  });

  errorRate.add(!success);
  sleep(1);
}
