import { nombreRol } from './RolesPermisos';

const campos: Record<string, string> = {
  Nombre: 'Nombre', Cuenta: 'Cuenta', Rol: 'Perfil de acceso', Activo: 'Estado',
  EstablecimientoId: 'Establecimiento', PasswordHash: 'Contraseña', Fallos: 'Intentos fallidos',
  BloqueadoHasta: 'Bloqueo hasta', Codigo: 'Código', Direccion: 'Dirección', Cargo: 'Cargo',
  Estado: 'Estado', Fecha: 'Fecha', Descripcion: 'Descripción', Plazo: 'Plazo',
  Completada: 'Cumplimiento', UsuarioResponsableId: 'Responsable'
};
interface Cambio { campo: string; antes?: unknown; despues?: unknown }
function valor(value: unknown, campo: string, centros: Map<string, string>) {
  if (value === null || value === undefined || value === '') return 'Sin valor';
  if (typeof value === 'boolean') return campo === 'Activo' ? (value ? 'Activo' : 'Inactivo') : (value ? 'Sí' : 'No');
  if (campo === 'Rol') return nombreRol(String(value));
  if (campo === 'EstablecimientoId') return centros.get(String(value)) || String(value);
  if (typeof value === 'object') return JSON.stringify(value);
  return String(value);
}
export function accionAuditoria(value?: string) {
  return ({ Added: 'Creación', Modified: 'Actualización', Deleted: 'Eliminación', Descarga: 'Descarga',
    Exportacion: 'Exportación', LecturaOCR: 'Lectura de imagen', DocumentoOCR: 'Documento creado',
    ArchivoNormalizacion: 'Normalización de formulario' } as Record<string, string>)[value || ''] || value?.replace(/([a-z])([A-Z])/g, '$1 $2') || 'Cambio';
}
export function AuditoriaDetalle({ cambios, registroId, centros }: { cambios?: string; registroId?: string; centros: Map<string, string> }) {
  let lista: Cambio[] = [];
  try {
    const parsed: unknown = JSON.parse(cambios || '');
    if (Array.isArray(parsed)) lista = parsed.filter((c): c is Cambio => !!c && typeof c === 'object' && typeof c.campo === 'string');
  } catch { /* Los registros anteriores también pueden contener descripciones de texto. */ }
  return <details className="admin-audit-detail"><summary>Ver detalle{lista.length ? ` · ${lista.length} ${lista.length === 1 ? 'campo' : 'campos'}` : ''}</summary>
    {lista.length ? <dl>{lista.map((c, i) => <div key={`${c.campo}-${i}`}>
      <dt>{campos[c.campo] || c.campo.replace(/([a-z])([A-Z])/g, '$1 $2')}</dt>
      <dd>{/password|contenido|token|secret/i.test(c.campo) ? 'Información protegida' : <>
        <span><small>Antes</small>{valor(c.antes, c.campo, centros)}</span>
        <span><small>Después</small>{valor(c.despues, c.campo, centros)}</span>
      </>}</dd>
    </div>)}</dl> : <p>{cambios || 'No hay información adicional para este movimiento.'}</p>}
    <p className="admin-record-id">Referencia: {registroId || 'Sin referencia'}</p>
  </details>;
}
