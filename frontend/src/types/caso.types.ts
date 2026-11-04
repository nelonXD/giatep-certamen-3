export type Estado = 'Borrador' | 'EnInvestigacion' | 'EnSeguimiento' | 'Cerrado';
export type Tipo = 'Accidente' | 'EnfermedadProfesional';
export interface Medida { id: string; descripcion: string; responsable: string; plazo: string; completada: boolean; usuarioResponsableId?: string }
export interface Caso { id: string; codigo: string; tipo: Tipo; establecimiento: string; establecimientoId?: string; trabajadorId?: string; fecha: string; relato: string; estado: Estado; medidas: Medida[]; cerradoEn?: string; denuncia?: import("../features/casos/DenunciaFields").Denuncia; investigacionId?: string; empleadorId?: string }
export interface CasoDTO { establecimiento: string; tipo: Tipo; fecha: string; relato: string; establecimientoId?: string; trabajadorId?: string }
export interface Pagina<T> { datos: T[]; total: number; pagina: number; tamano: number }
export const estados: Record<Estado, string> = { Borrador: 'Borrador', EnInvestigacion: 'En investigación', EnSeguimiento: 'En seguimiento', Cerrado: 'Cerrado' };
