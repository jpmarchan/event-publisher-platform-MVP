// docker compose --profile loadtest run --rm k6
// El límite es de 10 POST/min por usuario: esperar 1 minuto entre ejecuciones.

import http from "k6/http";
import { check } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:5101";
const TOKEN_URL = __ENV.TOKEN_URL || "http://localhost:8180/realms/plataforma-eventos/protocol/openid-connect/token";
const CLIENT_SECRET = __ENV.CLIENT_SECRET || "load-tester-dev-secret";

const BURST_SIZE = 15;
const RATE_LIMIT = 10;

export const options = {
  batch: BURST_SIZE,
  batchPerHost: BURST_SIZE,
  scenarios: {
    reads: { executor: "constant-vus", vus: 50, duration: "30s", exec: "readEvents" },
    idempotent_burst: { executor: "shared-iterations", vus: 1, iterations: 1, exec: "idempotentBurst", startTime: "5s" }
  },
  thresholds: {
    "http_req_duration{scenario:reads}": ["p(95)<300"],
    "http_req_failed{scenario:reads}": ["rate<0.01"],
    "checks{scenario:idempotent_burst}": ["rate==1"]
  }
};

export function setup() {
  const res = http.post(TOKEN_URL, {
    grant_type: "client_credentials",
    client_id: "load-tester",
    client_secret: CLIENT_SECRET
  });
  if (res.status !== 200) throw new Error(`No se pudo obtener el token (${res.status}): ${res.body}`);
  return { token: res.json("access_token") };
}

export function readEvents(data) {
  const res = http.get(`${BASE_URL}/events`, { headers: { Authorization: `Bearer ${data.token}` } });
  check(res, { "GET /events 200": (r) => r.status === 200 });
}

export function idempotentBurst(data) {
  const idempotencyKey = `k6-${Date.now()}`;
  const body = JSON.stringify({
    name: `Carga k6 ${new Date().toISOString()}`,
    date: new Date(Date.now() + 30 * 24 * 3600 * 1000).toISOString(),
    venue: "Arena k6",
    zones: [{ name: "General", price: 10, capacity: 5000 }]
  });
  const params = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      "Content-Type": "application/json",
      "Idempotency-Key": idempotencyKey
    }
  };

  const responses = http.batch(Array.from({ length: BURST_SIZE }, () => ["POST", `${BASE_URL}/events`, body, params]));

  const accepted = responses.filter((r) => r.status === 201);
  const limited = responses.filter((r) => r.status === 429);
  const eventIds = new Set(accepted.map((r) => r.json("id")));
  const replays = accepted.filter((r) => r.headers["Idempotent-Replayed"] === "true");

  console.log(
    `Ráfaga de ${BURST_SIZE}: ${accepted.length} aceptadas (${replays.length} replay), ${limited.length} con 429, ` +
      `eventos distintos creados: ${eventIds.size}`
  );

  check(responses, {
    [`${RATE_LIMIT} aceptadas (límite por usuario)`]: () => accepted.length === RATE_LIMIT,
    [`${BURST_SIZE - RATE_LIMIT} rechazadas con 429`]: () => limited.length === BURST_SIZE - RATE_LIMIT,
    "cada 429 trae Retry-After": () => limited.every((r) => !!r.headers["Retry-After"]),
    "un solo evento creado pese a la concurrencia": () => eventIds.size === 1,
    "el resto de las aceptadas son replay": () => replays.length === RATE_LIMIT - 1
  });
}
