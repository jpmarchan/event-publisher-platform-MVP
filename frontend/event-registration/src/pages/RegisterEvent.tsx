import { useEffect, useState } from "react";
import { ApiError, createEvent, getEvents, type EventDto, type ZoneInput } from "../api/eventsApi";

interface ZoneFormRow extends ZoneInput {
  key: string;
}

function newZoneRow(): ZoneFormRow {
  return { key: crypto.randomUUID(), name: "", price: 0, capacity: 1 };
}

type Status = { kind: "idle" } | { kind: "loading" } | { kind: "success"; event: EventDto; replayed: boolean } | { kind: "error"; message: string; fields?: Array<{ field: string; message: string }> };

interface RegisterEventProps {
  accessToken: string;
  canCreate: boolean;
}

export default function RegisterEvent({ accessToken, canCreate }: RegisterEventProps) {
  const [name, setName] = useState("");
  const [date, setDate] = useState("");
  const [venue, setVenue] = useState("");
  const [zones, setZones] = useState<ZoneFormRow[]>([newZoneRow()]);
  const [status, setStatus] = useState<Status>({ kind: "idle" });
  const [events, setEvents] = useState<EventDto[]>([]);
  const [loadingEvents, setLoadingEvents] = useState(false);
  // Nueva key al cambiar el formulario; un reintento reusa la misma.
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());

  useEffect(() => {
    setIdempotencyKey(crypto.randomUUID());
  }, [name, date, venue, zones]);

  async function loadEvents() {
    setLoadingEvents(true);
    try {
      const data = await getEvents(accessToken);
      setEvents(data);
    } catch {
    } finally {
      setLoadingEvents(false);
    }
  }

  useEffect(() => {
    loadEvents();
  }, []);

  function updateZone(key: string, patch: Partial<ZoneInput>) {
    setZones((prev) => prev.map((z) => (z.key === key ? { ...z, ...patch } : z)));
  }

  function addZone() {
    setZones((prev) => [...prev, newZoneRow()]);
  }

  function removeZone(key: string) {
    setZones((prev) => (prev.length > 1 ? prev.filter((z) => z.key !== key) : prev));
  }

  function validate(): string | null {
    if (!name.trim()) return "El nombre del evento es obligatorio.";
    if (!date) return "La fecha del evento es obligatoria.";
    if (!venue.trim()) return "El lugar es obligatorio.";
    if (zones.length === 0) return "Debe incluir al menos una zona.";
    for (const zone of zones) {
      if (!zone.name.trim()) return "Cada zona necesita un nombre.";
      if (zone.capacity <= 0) return "La capacidad de cada zona debe ser mayor a cero.";
      if (zone.price < 0) return "El precio de cada zona no puede ser negativo.";
    }
    return null;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    const validationError = validate();
    if (validationError) {
      setStatus({ kind: "error", message: validationError });
      return;
    }

    setStatus({ kind: "loading" });
    try {
      const { event: created, replayed } = await createEvent(
        {
          name: name.trim(),
          date: new Date(date).toISOString(),
          venue: venue.trim(),
          zones: zones.map(({ name, price, capacity }) => ({ name: name.trim(), price, capacity }))
        },
        accessToken,
        idempotencyKey
      );

      setStatus({ kind: "success", event: created, replayed });
      setName("");
      setDate("");
      setVenue("");
      setZones([newZoneRow()]);
      loadEvents();
    } catch (error) {
      if (error instanceof ApiError) {
        setStatus({
          kind: "error",
          message: error.message,
          fields: error.details as Array<{ field: string; message: string }> | undefined
        });
      } else {
        setStatus({ kind: "error", message: "No se pudo conectar con el servicio de eventos." });
      }
    }
  }

  return (
    <div className="py-10 px-4">
      <div className="mx-auto max-w-2xl space-y-8">
        <header>
          <h1 className="text-2xl font-semibold text-slate-900">Registrar Evento</h1>
          <p className="text-sm text-slate-500">
            Crea un evento con sus zonas mediante <code className="rounded bg-slate-200 px-1">POST /events</code> de EventService.
          </p>
        </header>

        {!canCreate && (
          <div className="rounded-md bg-amber-50 p-3 text-sm text-amber-800">
            Tu usuario no tiene el rol <strong>Admin</strong>: puedes ver los eventos, pero no crearlos.
          </div>
        )}

        <form onSubmit={handleSubmit} className="rounded-lg bg-white p-6 shadow-sm ring-1 ring-slate-200">
          <fieldset disabled={!canCreate} className="space-y-6 disabled:opacity-60">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-slate-700">Nombre del evento</label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                  placeholder="Concierto de Rock"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700">Fecha</label>
                <input
                  type="datetime-local"
                  value={date}
                  onChange={(e) => setDate(e.target.value)}
                  className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700">Lugar</label>
                <input
                  type="text"
                  value={venue}
                  onChange={(e) => setVenue(e.target.value)}
                  className="mt-1 block w-full rounded-md border-slate-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                  placeholder="Estadio Nacional"
                />
              </div>
            </div>

            <div>
              <div className="mb-2 flex items-center justify-between">
                <h2 className="text-sm font-medium text-slate-700">Zonas</h2>
                <button
                  type="button"
                  onClick={addZone}
                  className="text-sm font-medium text-indigo-600 hover:text-indigo-500"
                >
                  + Agregar zona
                </button>
              </div>

              <div className="space-y-3">
                {zones.map((zone) => (
                  <div key={zone.key} className="grid grid-cols-12 gap-2">
                    <input
                      type="text"
                      value={zone.name}
                      onChange={(e) => updateZone(zone.key, { name: e.target.value })}
                      placeholder="Nombre (ej. VIP)"
                      className="col-span-5 rounded-md border-slate-300 shadow-sm text-sm focus:border-indigo-500 focus:ring-indigo-500"
                    />
                    <input
                      type="number"
                      min={0}
                      step="0.01"
                      value={zone.price}
                      onChange={(e) => updateZone(zone.key, { price: Number(e.target.value) })}
                      placeholder="Precio"
                      className="col-span-3 rounded-md border-slate-300 shadow-sm text-sm focus:border-indigo-500 focus:ring-indigo-500"
                    />
                    <input
                      type="number"
                      min={1}
                      value={zone.capacity}
                      onChange={(e) => updateZone(zone.key, { capacity: Number(e.target.value) })}
                      placeholder="Capacidad"
                      className="col-span-3 rounded-md border-slate-300 shadow-sm text-sm focus:border-indigo-500 focus:ring-indigo-500"
                    />
                    <button
                      type="button"
                      onClick={() => removeZone(zone.key)}
                      disabled={zones.length === 1}
                      className="col-span-1 text-slate-400 hover:text-red-500 disabled:opacity-30"
                      aria-label="Quitar zona"
                    >
                      ✕
                    </button>
                  </div>
                ))}
              </div>
            </div>

            {status.kind === "error" && (
              <div className="rounded-md bg-red-50 p-3 text-sm text-red-700">
                <p>{status.message}</p>
                {status.fields && (
                  <ul className="mt-1 list-disc pl-5">
                    {status.fields.map((f, i) => (
                      <li key={i}>
                        {f.field}: {f.message}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            )}

            {status.kind === "success" && (
              <div className="rounded-md bg-green-50 p-3 text-sm text-green-700">
                {status.replayed
                  ? `El evento "${status.event.name}" ya se había creado con esta solicitud (id: ${status.event.id}); no se duplicó.`
                  : `Evento "${status.event.name}" creado y publicado (id: ${status.event.id}).`}
              </div>
            )}

            <button
              type="submit"
              disabled={status.kind === "loading"}
              className="w-full rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 disabled:opacity-60"
            >
              {status.kind === "loading" ? "Guardando..." : "Guardar"}
            </button>
          </fieldset>
        </form>

        <section>
          <h2 className="mb-2 text-sm font-medium text-slate-700">
            Eventos publicados {loadingEvents && "(cargando...)"}
          </h2>
          <ul className="space-y-2">
            {events.map((ev) => (
              <li key={ev.id} className="rounded-md bg-white p-3 text-sm shadow-sm ring-1 ring-slate-200">
                <strong>{ev.name}</strong> — {new Date(ev.date).toLocaleString()} — {ev.venue} ({ev.zones.length} zona(s))
              </li>
            ))}
            {events.length === 0 && !loadingEvents && (
              <li className="text-sm text-slate-400">Todavía no hay eventos publicados.</li>
            )}
          </ul>
        </section>
      </div>
    </div>
  );
}
