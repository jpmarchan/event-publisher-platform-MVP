const EVENT_SERVICE_URL = import.meta.env.VITE_EVENT_SERVICE_URL ?? "http://localhost:5101";

export interface ZoneInput {
  name: string;
  price: number;
  capacity: number;
}

export interface CreateEventInput {
  name: string;
  date: string;
  venue: string;
  zones: ZoneInput[];
}

export interface EventDto {
  id: string;
  name: string;
  date: string;
  venue: string;
  status: string;
  zones: Array<ZoneInput & { id: string }>;
}

class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly details?: unknown
  ) {
    super(message);
  }
}

async function parseErrorResponse(response: Response): Promise<ApiError> {
  if (response.status === 401) return new ApiError("Tu sesión expiró. Vuelve a iniciar sesión.", 401);
  if (response.status === 403)
    return new ApiError("No tienes permiso para esta acción (requiere rol Admin y scope events:write).", 403);
  if (response.status === 429) {
    const retryAfter = response.headers.get("Retry-After");
    return new ApiError(`Demasiadas solicitudes. Intenta de nuevo en ${retryAfter ?? "unos"} segundos.`, 429);
  }
  try {
    const body = await response.json();
    return new ApiError(body.error ?? "Ocurrió un error.", response.status, body.details);
  } catch {
    return new ApiError("Ocurrió un error inesperado.", response.status);
  }
}

export interface CreateEventResult {
  event: EventDto;
  replayed: boolean;
}

export async function createEvent(input: CreateEventInput, token: string, idempotencyKey: string): Promise<CreateEventResult> {
  const response = await fetch(`${EVENT_SERVICE_URL}/events`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
      "Idempotency-Key": idempotencyKey
    },
    body: JSON.stringify(input)
  });

  if (!response.ok) throw await parseErrorResponse(response);
  return { event: await response.json(), replayed: response.headers.get("Idempotent-Replayed") === "true" };
}

export async function getEvents(token: string): Promise<EventDto[]> {
  const response = await fetch(`${EVENT_SERVICE_URL}/events`, {
    headers: { Authorization: `Bearer ${token}` }
  });

  if (!response.ok) throw await parseErrorResponse(response);
  return response.json();
}

export { ApiError };
