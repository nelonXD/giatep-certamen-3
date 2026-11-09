import { useEffect, useRef, useState } from 'react';
import { Users, Building2, BriefcaseBusiness, History, Plus, Search, Pencil, RefreshCw, ArrowUpRight, ShieldCheck, UserCheck } from 'lucide-react';
import { useLoad } from '../../hooks/useLoad';
import { Field, Select, Message, Loading, Pagination } from '../../components/common/UI';
import { nombreRol, perfiles } from './RolesPermisos';
import { AdminForm } from './AdminForm';
import { AuditoriaDetalle, accionAuditoria } from './AuditoriaDetalle';
import type { ListaAdmin, RegistroAdmin, SeccionAdmin } from './admin.types';
import './administracion.css';

export const seccionesAdmin = [
  { id: 'usuarios', nombre: 'Usuarios', singular: 'usuario', icono: Users, detalle: 'Cuentas y permisos', descripcion: 'Administra las cuentas y el acceso de cada persona.' },
  { id: 'establecimientos', nombre: 'Establecimientos', singular: 'establecimiento', icono: Building2, detalle: 'Centros de trabajo', descripcion: 'Mantén actualizados los centros y sus direcciones.' },
  { id: 'trabajadores', nombre: 'Trabajadores', singular: 'trabajador', icono: BriefcaseBusiness, detalle: 'Personas registradas', descripcion: 'Consulta y actualiza los registros de los trabajadores.' },
  { id: 'auditoria', nombre: 'Auditoría', singular: 'movimiento', icono: History, detalle: 'Historial de cambios', descripcion: 'Revisa quién realizó cada cambio y cuándo ocurrió.' }
] as const;
const fecha = new Intl.DateTimeFormat('es-CL', { dateStyle: 'medium', timeZone: 'America/Santiago' });
const hora = new Intl.DateTimeFormat('es-CL', { timeStyle: 'short', timeZone: 'America/Santiago' });
const iniciales = (nombre = '') => nombre.split(' ').filter(Boolean).slice(0, 2).map(n => n[0]).join('').toUpperCase();

export function AdminPage({ seccionId = 'usuarios' }: { seccionId?: SeccionAdmin }) {
  const tab = seccionId;
  const [page, setPage] = useState(1);
  const [buscar, setBuscar] = useState(''), [consulta, setConsulta] = useState('');
  const [estado, setEstado] = useState('todos'), [perfil, setPerfil] = useState('todos');
  const [editing, setEditing] = useState<RegistroAdmin | null>(null), [success, setSuccess] = useState(''), [busy, setBusy] = useState(false);
  const trigger = useRef<HTMLButtonElement | null>(null), listado = useRef<HTMLElement>(null);
  useEffect(() => {
    const timer = window.setTimeout(() => { setConsulta(buscar); setPage(1); }, 300);
    return () => window.clearTimeout(timer);
  }, [buscar]);
  const path = `/admin/${tab}${tab === 'trabajadores' ? `?pagina=${page}&buscar=${encodeURIComponent(consulta)}` : tab === 'auditoria' ? `?pagina=${page}` : ''}`;
  const result = useLoad<ListaAdmin>(path);
  const centros = useLoad<RegistroAdmin[]>('/admin/establecimientos'), usuarios = useLoad<RegistroAdmin[]>('/admin/usuarios');
  const seccion = seccionesAdmin.find(s => s.id === tab)!;
  const rows = Array.isArray(result.data) ? result.data : result.data?.datos || [];
  const nombresCentros = new Map((centros.data || []).map(c => [c.id, c.nombre || 'Sin nombre']));
  const normalizar = (value = '') => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  const visibles = tab === 'usuarios' || tab === 'establecimientos' ? rows.filter(r =>
    (!buscar || normalizar(`${r.nombre} ${r.cuenta || ''} ${r.direccion || ''}`).includes(normalizar(buscar))) &&
    (estado === 'todos' || r.activo === (estado === 'activos')) &&
    (tab !== 'usuarios' || perfil === 'todos' || r.rol === perfil)) : rows;
  const total = Array.isArray(result.data) ? result.data.length : result.data?.total ?? 0;
  const filtrado = !!buscar || estado !== 'todos' || perfil !== 'todos';
  const resumenFuente = tab === 'usuarios' ? usuarios.data : centros.data;
  const resumen = [
    { titulo: tab === 'usuarios' ? 'Cuentas registradas' : 'Centros registrados', valor: resumenFuente?.length, icono: tab === 'usuarios' ? Users : Building2 },
    { titulo: tab === 'usuarios' ? 'Usuarios activos' : 'Centros activos', valor: resumenFuente?.filter(r => r.activo).length, icono: UserCheck },
    { titulo: tab === 'usuarios' ? 'Usuarios inactivos' : 'Centros inactivos', valor: resumenFuente?.filter(r => !r.activo).length, icono: ShieldCheck }
  ];
  function limpiar() { setBuscar(''); setConsulta(''); setEstado('todos'); setPerfil('todos'); setPage(1); }
  function cerrar() { setEditing(null); trigger.current?.focus(); }
  function actualizar() { result.refresh(); centros.refresh(); usuarios.refresh(); }
  function editar(r: RegistroAdmin, boton: HTMLButtonElement) { trigger.current = boton; setEditing(r); setSuccess(''); }
  return <div className="admin-workspace">
    <header className="admin-heading"><div><p className="eyebrow">ADMINISTRACIÓN / {seccion.nombre.toUpperCase()}</p><h1>{seccion.nombre}</h1>
      <p>{seccion.descripcion}</p></div><span className="admin-access"><ShieldCheck size={16} aria-hidden="true"/>Acceso de administrador</span></header>
    {(tab === 'usuarios' || tab === 'establecimientos') && <section className="admin-overview" aria-label={`Resumen de ${seccion.nombre.toLowerCase()}`}>
      {resumen.map(r => <div key={r.titulo}><span className="admin-stat-icon"><r.icono size={21} aria-hidden="true"/></span><span><strong>{r.valor ?? '—'}</strong><span>{r.titulo}</span></span></div>)}
    </section>}
    <Message error={result.error || centros.error || usuarios.error} success={success}/>
    {editing && <AdminForm key={`${tab}-${editing.id}`} seccion={tab} registro={editing} centros={centros.data || []} onBusy={setBusy} onCancel={cerrar}
      onSaved={() => { cerrar(); actualizar(); setSuccess(`${tab === 'usuarios' ? 'Usuario' : tab === 'trabajadores' ? 'Trabajador' : 'Establecimiento'} guardado correctamente.`); listado.current?.scrollIntoView({ block: 'start', behavior: 'instant' }); }}/ >}
    <section className="panel admin-list" ref={listado} aria-labelledby="admin-list-title">
      <div className="admin-list-heading"><div><h2 id="admin-list-title">{seccion.nombre}<span className="admin-count">{result.loading ? '…' : total}</span></h2><p>{seccion.descripcion}</p></div>
        <div className="admin-list-actions"><button className="admin-refresh" type="button" disabled={result.loading || busy} onClick={actualizar}><RefreshCw size={16} aria-hidden="true"/><span>Actualizar</span></button>
          {tab !== 'auditoria' && <button className="primary" type="button" disabled={busy} onClick={e => editar({ id: '', activo: true }, e.currentTarget)}><Plus size={17} aria-hidden="true"/>Agregar {seccion.singular}</button>}
        </div>
      </div>
      {tab !== 'auditoria' ? <div className="admin-toolbar">
        <div className="admin-search"><Search size={18} aria-hidden="true"/><Field label={`Buscar ${seccion.singular}`} type="search" placeholder={tab === 'usuarios' ? 'Buscar por nombre o cuenta…' : tab === 'establecimientos' ? 'Buscar por nombre o dirección…' : 'Buscar por nombre o código…'}
          maxLength={150} value={buscar} onChange={e => setBuscar(e.target.value)}/></div>
        {tab === 'usuarios' && <Select label="Perfil de acceso" value={perfil} onChange={e => setPerfil(e.target.value)}><option value="todos">Todos los perfiles</option>{perfiles.map(p => <option key={p.value} value={p.value}>{p.nombre}</option>)}</Select>}
        {tab !== 'trabajadores' && <Select label="Estado" value={estado} onChange={e => setEstado(e.target.value)}><option value="todos">Todos los estados</option><option value="activos">Activos</option><option value="inactivos">Inactivos</option></Select>}
        {filtrado && <button className="admin-clear" type="button" onClick={limpiar}>Limpiar filtros</button>}
      </div> : <div className="admin-audit-note"><History size={16} aria-hidden="true"/><span>Historial ordenado del más reciente al más antiguo. Las horas corresponden a Santiago.</span></div>}
      {result.loading ? <div className="admin-loading"><Loading/></div> : result.error ? <div className="admin-empty"><History size={28} aria-hidden="true"/><h3>No pudimos cargar los registros</h3><p>Intenta actualizar el listado.</p><button className="secondary" onClick={actualizar}>Volver a intentar</button></div> : !visibles.length ?
        <div className="admin-empty"><Search size={28} aria-hidden="true"/><h3>{filtrado ? 'No encontramos coincidencias' : `Todavía no hay ${seccion.nombre.toLowerCase()}`}</h3><p>{filtrado ? 'Prueba con otro nombre o ajusta los filtros.' : 'Los registros aparecerán aquí cuando se incorporen al sistema.'}</p>{filtrado && <button className="secondary" onClick={limpiar}>Limpiar filtros</button>}</div> :
        <div><p className="admin-table-hint">Desliza la tabla hacia los lados para ver todos los datos y acciones.</p><div className="table-wrap" tabIndex={0} role="region" aria-label={`Tabla de ${seccion.nombre.toLowerCase()}, desplazable horizontalmente`}><table className="admin-table"><caption className="sr-only">Listado de {seccion.nombre.toLowerCase()}</caption><thead><tr>
          {(tab === 'auditoria' ? ['Fecha y hora', 'Realizado por', 'Registro', 'Movimiento', 'Detalle'] : tab === 'usuarios' ? ['Usuario', 'Perfil de acceso', 'Ámbito', 'Estado', ''] : tab === 'trabajadores' ? ['Trabajador', 'Cargo', 'Establecimiento', 'Estado', ''] : ['Establecimiento', 'Dirección', 'Estado', '']).map((c, i) => <th key={i} scope="col">{c || <span className="sr-only">Acciones</span>}</th>)}
        </tr></thead><tbody>{visibles.map(r => <tr key={r.id}>
          {tab === 'auditoria' ? <>
            <td><strong>{r.fecha ? fecha.format(new Date(r.fecha)) : 'Sin fecha'}</strong><small>{r.fecha ? hora.format(new Date(r.fecha)) : ''}</small></td>
            <td><div className="admin-person"><span className="admin-avatar" aria-hidden="true">{iniciales(r.actor)}</span><strong>{r.actor || 'Sistema'}</strong></div></td>
            <td>{r.entidad || 'Registro'}</td><td><span className="admin-movement">{accionAuditoria(r.accion)}</span></td>
            <td><AuditoriaDetalle cambios={r.cambios} registroId={r.registroId} centros={nombresCentros}/></td>
          </> : <>
            {tab === 'usuarios' ? <>
              <td><div className="admin-person"><span className="admin-avatar" aria-hidden="true">{iniciales(r.nombre)}</span><span><strong>{r.nombre}</strong><small>@{r.cuenta}</small></span></div></td>
              <td><span className="admin-role">{nombreRol(r.rol)}</span></td><td>{r.rol === 'Jefatura' ? nombresCentros.get(r.establecimientoId || '') || 'Sin establecimiento' : 'Todos los establecimientos'}</td>
            </> : tab === 'trabajadores' ? <>
              <td><div className="admin-person"><span className="admin-avatar" aria-hidden="true">{iniciales(r.nombre)}</span><span><strong>{r.nombre}</strong><small>{r.codigo}</small></span></div></td>
              <td>{r.cargo}</td><td>{nombresCentros.get(r.establecimientoId || '') || 'Sin establecimiento'}</td>
            </> : <><td><div className="admin-person"><span className="admin-center-icon"><Building2 size={19} aria-hidden="true"/></span><strong>{r.nombre}</strong></div></td><td>{r.direccion}</td></>}
            <td><span className={`admin-status${r.activo ? ' active' : ''}`}><span aria-hidden="true"/>{r.activo ? 'Activo' : 'Inactivo'}</span></td>
            <td className="admin-action-cell"><button className="admin-edit" disabled={busy} onClick={e => editar(r, e.currentTarget)}><Pencil size={15} aria-hidden="true"/>Editar<span className="sr-only"> {r.nombre}</span><ArrowUpRight size={14} aria-hidden="true"/></button></td>
          </>}
        </tr>)}</tbody></table></div></div>}
      {!result.loading && !result.error && <div className="admin-list-footer"><span role="status">{visibles.length} {visibles.length === 1 ? 'registro visible' : 'registros visibles'}{tab === 'trabajadores' || tab === 'auditoria' ? ' en esta página' : ` de ${total}`}</span><span>GIATEP · Administración</span></div>}
      {result.data && !Array.isArray(result.data) && result.data.total > (tab === 'auditoria' ? 20 : 15) && <Pagination page={page} total={result.data.total} size={tab === 'auditoria' ? 20 : 15} onChange={next => { setPage(next); listado.current?.scrollIntoView({ block: 'start', behavior: 'instant' }); }}/ >}
    </section>
    <p className="admin-bottom-note"><ShieldCheck size={15} aria-hidden="true"/>Los registros inactivos conservan su historial. Los permisos se aplican según el perfil de cada cuenta.</p>
  </div>;
}
