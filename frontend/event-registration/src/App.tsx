import { useAuth } from "react-oidc-context";
import { getRoles } from "./auth/oidcConfig";
import RegisterEvent from "./pages/RegisterEvent";

export default function App() {
  const auth = useAuth();

  if (auth.isLoading) {
    return <CenteredMessage>Verificando sesión...</CenteredMessage>;
  }

  if (auth.error) {
    return (
      <CenteredMessage>
        <p className="text-red-700">No se pudo iniciar sesión: {auth.error.message}</p>
        <button onClick={() => auth.signinRedirect()} className="mt-4 text-sm font-medium text-indigo-600 hover:text-indigo-500">
          Reintentar
        </button>
      </CenteredMessage>
    );
  }

  if (!auth.isAuthenticated || !auth.user) {
    return (
      <CenteredMessage>
        <h1 className="text-xl font-semibold text-slate-900">Plataforma de Eventos</h1>
        <p className="mt-2 text-sm text-slate-500">
          El login se hace en Keycloak (OIDC, Authorization Code + PKCE). Esta aplicación nunca ve tu contraseña.
        </p>
        <button
          onClick={() => auth.signinRedirect()}
          className="mt-6 rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500"
        >
          Iniciar sesión
        </button>
        <p className="mt-4 text-xs text-slate-400">Usuarios demo: admin / Admin123! · user / User123!</p>
      </CenteredMessage>
    );
  }

  const roles = getRoles(auth.user);

  return (
    <div className="min-h-screen bg-slate-50">
      <nav className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-2xl items-center justify-between px-4 py-3 text-sm">
          <span className="text-slate-600">
            <strong className="text-slate-900">{auth.user.profile.preferred_username}</strong>
            {roles.length > 0 && <span className="ml-2 text-slate-400">({roles.join(", ")})</span>}
          </span>
          <button
            onClick={() => auth.signoutRedirect({ id_token_hint: auth.user?.id_token })}
            className="font-medium text-indigo-600 hover:text-indigo-500"
          >
            Cerrar sesión
          </button>
        </div>
      </nav>
      <RegisterEvent accessToken={auth.user.access_token} canCreate={roles.includes("Admin")} />
    </div>
  );
}

function CenteredMessage({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <div className="max-w-md rounded-lg bg-white p-8 text-center shadow-sm ring-1 ring-slate-200">{children}</div>
    </div>
  );
}
