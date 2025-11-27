import http from 'k6/http';
import { check } from 'k6';

// Spike test: Sudden traffic increase
export const options = {
  stages: [
    { duration: '10s', target: 10 },    // Normal load
    { duration: '10s', target: 500 },   // SPIKE! 500 users
    { duration: '30s', target: 500 },   // Sustain spike
    { duration: '10s', target: 10 },    // Back to normal
    { duration: '10s', target: 0 },     // Ramp down
  ],
  thresholds: {
    'http_req_duration': ['p(99)<2000'],  // 99% under 2s even during spike
    // Note: High failure rate (rate limits) expected during spike - this is GOOD!
    // Rate limiting protects the system from being overwhelmed
  },
};

const BASE_URL = __ENV.API_URL || 'http://localhost:5001';

export function setup() {
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ username: 'admin', password: 'Admin123!' }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  return { token: JSON.parse(loginRes.body).token };
}

export default function (data) {
  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${data.token}`,
    },
  };

  const res = http.post(
    `${BASE_URL}/api/reverse`,
    JSON.stringify({ sentence: 'spike test' }),
    params
  );

  check(res, {
    'status is 200 or 429': (r) => r.status === 200 || r.status === 429,
    'response time acceptable': (r) => r.timings.duration < 2000,
  });
}
