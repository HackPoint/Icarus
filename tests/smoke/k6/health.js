import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 5,
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:5000';

export default function () {
  // Health check
  const healthRes = http.get(`${BASE_URL}/health`);
  check(healthRes, {
    'health status is 200': (r) => r.status === 200,
    'health body contains Healthy': (r) => r.body.includes('Healthy'),
  });

  // Chat query
  const chatRes = http.post(
    `${BASE_URL}/chat/query`,
    JSON.stringify({ query: 'What is Icarus?', topK: 3 }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  check(chatRes, {
    'chat status is 200': (r) => r.status === 200,
    'chat has answer': (r) => JSON.parse(r.body).answer !== undefined,
  });

  sleep(1);
}
