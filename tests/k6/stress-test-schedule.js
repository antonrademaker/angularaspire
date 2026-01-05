import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 50 }, // Ramp up to 50 users
    { duration: '1m', target: 100 }, // Ramp up to 100 users
    { duration: '30s', target: 0 },  // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests must complete below 500ms
    http_req_failed: ['rate<0.01'],   // http errors should be less than 1%
  },
};

const BASE_URL = 'http://localhost:5051';

export default function () {
  // 1. Search for events (Simulating browsing the schedule/events list)
  const res = http.get(`${BASE_URL}/api/events/search?pageSize=20`);
  
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });

  if (res.status === 200) {
    try {
        const body = JSON.parse(res.body);
        if (body.events && body.events.length > 0) {
        // 2. Get details for the first event (Simulating viewing event details)
        const eventId = body.events[0].id;
        const detailRes = http.get(`${BASE_URL}/api/events/${eventId}`);
        
        check(detailRes, {
            'detail status is 200': (r) => r.status === 200,
            'detail response time < 500ms': (r) => r.timings.duration < 500,
        });
        }
    } catch (e) {
        // Ignore JSON parse errors if body is empty or invalid
    }
  }

  sleep(1);
}
