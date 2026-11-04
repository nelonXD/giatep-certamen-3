import { request } from './client';
import type { Caso, CasoDTO, Estado, Pagina } from '../types/caso.types';
export const casosService = {
  listar: (buscar: string, estado: string, pagina: number) => request<Pagina<Caso>>(`/casos?${new URLSearchParams({ buscar, estado, pagina: String(pagina), tamano: '8' })}`),
  crear: (dto: CasoDTO) => request<Caso>('/casos', { method: 'POST', body: JSON.stringify(dto) }),
  cambiarEstado: (id: string, estado: Estado) => request<Caso>(`/casos/${id}/estado`, { method: 'PUT', body: JSON.stringify({ estado }) }),
  agregarMedida: (id: string, dto: { descripcion: string; responsable: string; plazo: string; usuarioResponsableId: string }) => request<Caso>(`/casos/${id}/medidas`, { method: 'POST', body: JSON.stringify(dto) }),
  completarMedida: (id: string, medidaId: string) => request<Caso>(`/casos/${id}/medidas/${medidaId}/cumplimiento`, { method: 'PUT' }),
};
