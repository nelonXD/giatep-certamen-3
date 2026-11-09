export type SeccionAdmin = 'usuarios' | 'establecimientos' | 'trabajadores' | 'auditoria';
export interface RegistroAdmin {
  id: string; nombre?: string; cuenta?: string; rol?: string; codigo?: string; cargo?: string;
  direccion?: string; establecimientoId?: string; activo?: boolean; actor?: string; fecha?: string;
  accion?: string; entidad?: string; registroId?: string; cambios?: string;
}
export type ListaAdmin = RegistroAdmin[] | { datos: RegistroAdmin[]; total: number };
