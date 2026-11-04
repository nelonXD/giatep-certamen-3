export class ApiError extends Error { constructor(message: string, public status: number) { super(message); this.name = "ApiError"; } }
export const TOKEN_KEY = 'giatep_token';
export async function request<T>(path: string, options?: RequestInit): Promise<T> {
  let response: Response;
  try { response = await fetch(`/api/v1${path}`, { ...options, headers: { ...(options?.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }), ...(sessionStorage.getItem(TOKEN_KEY) ? { Authorization: `Bearer ${sessionStorage.getItem(TOKEN_KEY)}` } : {}), ...options?.headers }, signal: options?.signal ? AbortSignal.any([options.signal, AbortSignal.timeout(60000)]) : AbortSignal.timeout(60000) }); }
  catch { throw new Error('No pudimos conectar con el servicio. Intenta nuevamente en unos momentos.'); }
  if (!response.ok) { if (response.status === 401 && !path.includes('/login')) window.dispatchEvent(new Event('giatep:expired')); const error = await response.json().catch(() => ({})); throw new ApiError(error.title || 'No se pudo completar la operación.', response.status); }
  if (response.status === 204) return undefined as T;
  return response.json();
}
export async function descargar(path: string, nombre: string) {
  const response = await fetch(`/api/v1${path}`, { headers: { Authorization: `Bearer ${sessionStorage.getItem(TOKEN_KEY) || ''}` }, signal: AbortSignal.timeout(120000) });
  if (!response.ok) { if (response.status === 401) window.dispatchEvent(new Event('giatep:expired')); const error = await response.json().catch(() => ({})); throw new Error(error.title || 'No se pudo descargar el archivo.'); }
  const url = URL.createObjectURL(await response.blob()); const a = document.createElement('a'); a.href = url; a.download = nombre; document.body.append(a); a.click(); a.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000);
}
