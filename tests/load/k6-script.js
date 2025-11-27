// ===========================================
// MToGo Load Test Script (k6)
// ===========================================
// Run with: k6 run k6-script.js --env API_URL=http://your-api-url
//
// This script simulates realistic user behavior.

import http from "k6/http";
import { check, group, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

// Custom metrics
const errorRate = new Rate("errors");
const orderCreationTime = new Trend("order_creation_time");

// Test configuration
export const options = {
  scenarios: {
    // Smoke test - quick validation
    smoke: {
      executor: "constant-vus",
      vus: 1,
      duration: "30s",
      startTime: "0s",
      tags: { test_type: "smoke" },
    },
    // Load test - normal load
    load: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "1m", target: 20 }, // Ramp up
        { duration: "3m", target: 20 }, // Stay at 20 users
        { duration: "1m", target: 0 }, // Ramp down
      ],
      startTime: "30s",
      tags: { test_type: "load" },
    },
    // Stress test - find breaking point (optional)
    // stress: {
    //   executor: 'ramping-vus',
    //   startVUs: 0,
    //   stages: [
    //     { duration: '2m', target: 50 },
    //     { duration: '5m', target: 50 },
    //     { duration: '2m', target: 100 },
    //     { duration: '5m', target: 100 },
    //     { duration: '2m', target: 0 },
    //   ],
    //   startTime: '6m',
    //   tags: { test_type: 'stress' },
    // },
  },
  thresholds: {
    // Response time thresholds
    http_req_duration: ["p(95)<500", "p(99)<1000"],

    // Error rate threshold
    errors: ["rate<0.1"], // Less than 10% errors

    // Custom metric thresholds
    order_creation_time: ["p(95)<1000"],
  },
};

const BASE_URL = __ENV.API_URL || "http://localhost:8080";

// ===========================================
// Test Scenarios
// ===========================================

export default function () {
  // Simulate user browsing and ordering flow

  group("Health Checks", () => {
    const healthRes = http.get(`${BASE_URL}/health`, {
      tags: { name: "health_check" },
    });

    check(healthRes, {
      "health check status is 200": (r) => r.status === 200,
    }) || errorRate.add(1);
  });

  sleep(1);

  group("Browse Partners", () => {
    const partnersRes = http.get(`${BASE_URL}/api/partners`, {
      tags: { name: "list_partners" },
    });

    const success = check(partnersRes, {
      "partners status is 2xx": (r) => r.status >= 200 && r.status < 300,
      "partners response has data": (r) => {
        try {
          const body = JSON.parse(r.body);
          return Array.isArray(body) || body.data !== undefined;
        } catch {
          return r.status === 200;
        }
      },
    });

    if (!success) errorRate.add(1);
  });

  sleep(Math.random() * 2 + 1); // Random 1-3 second pause

  group("View Menu", () => {
    // Simulate viewing a partner's menu
    const menuRes = http.get(`${BASE_URL}/api/partners/1/menu`, {
      tags: { name: "view_menu" },
    });

    check(menuRes, {
      "menu status is 2xx or 404": (r) => r.status >= 200 && r.status < 500,
    }) || errorRate.add(1);
  });

  sleep(Math.random() * 2 + 1);

  // Simulate order creation (10% of users)
  if (Math.random() < 0.1) {
    group("Create Order", () => {
      const startTime = Date.now();

      const orderPayload = JSON.stringify({
        customerId: Math.floor(Math.random() * 1000) + 1,
        partnerId: 1,
        items: [{ menuItemId: 1, quantity: Math.floor(Math.random() * 3) + 1 }],
        deliveryAddress: {
          street: "Test Street 123",
          city: "Copenhagen",
          zipCode: "1000",
        },
      });

      const orderRes = http.post(`${BASE_URL}/api/orders`, orderPayload, {
        headers: { "Content-Type": "application/json" },
        tags: { name: "create_order" },
      });

      const duration = Date.now() - startTime;
      orderCreationTime.add(duration);

      check(orderRes, {
        "order creation status is 2xx": (r) =>
          r.status >= 200 && r.status < 300,
      }) || errorRate.add(1);
    });
  }

  sleep(Math.random() * 3 + 2); // Random 2-5 second pause between iterations
}

// ===========================================
// Lifecycle Hooks
// ===========================================

export function setup() {
  console.log(`Starting load test against: ${BASE_URL}`);

  // Verify API is reachable
  const res = http.get(`${BASE_URL}/health`);
  if (res.status !== 200) {
    console.warn(`Warning: Health check returned ${res.status}`);
  }

  return { startTime: Date.now() };
}

export function teardown(data) {
  const duration = (Date.now() - data.startTime) / 1000;
  console.log(`Load test completed in ${duration.toFixed(2)} seconds`);
}
