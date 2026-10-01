import type { AuthProviderProps } from "react-oidc-context";
import { WebStorageStateStore, type User } from "oidc-client-ts";

export const oidcConfig: AuthProviderProps = {
  authority: import.meta.env.VITE_OIDC_AUTHORITY ?? "http://localhost:8180/realms/plataforma-eventos",
  client_id: import.meta.env.VITE_OIDC_CLIENT_ID ?? "event-registration-spa",
  redirect_uri: `${window.location.origin}/`,
  post_logout_redirect_uri: `${window.location.origin}/`,
  response_type: "code",
  scope: "openid profile email events:read events:write",
  automaticSilentRenew: true,
  userStore: new WebStorageStateStore({ store: window.sessionStorage }),
  // Limpia ?code=&state= de la URL después del login.
  onSigninCallback: () => {
    window.history.replaceState({}, document.title, window.location.pathname);
  }
};

export function getRoles(user: User | null | undefined): string[] {
  const roles = user?.profile.roles;
  return Array.isArray(roles) ? roles.filter((r): r is string => typeof r === "string") : [];
}
