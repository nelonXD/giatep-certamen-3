import { useEffect, useRef, useState } from 'react';
import { RefreshCw, ArrowUpRight, CalendarRange, ClipboardList, CircleCheck, Clock3, ListChecks, TriangleAlert, SlidersHorizontal, X, RotateCcw, Activity, ChevronDown } from 'lucide-react';
import { useLoad } from '../../hooks/useLoad';
import { useAuth } from '../../context/AuthContext';
import { Field, Select, Loading, Message, today } from '../../components/common/UI';
import type { Opciones } from '../../components/common/UI';
import { EvolucionMensual, ComposicionEventos, FlujoCasos, CasosCentros } from './DashboardCharts';
import type { Serie } from './DashboardCharts';
import './dashboard.css';

interface Indicador { nombre: string; valor: number; unidad: string; formula: string; pregunta: string; cantidad?: number | null; denominador?: number | null; muestra?: number | null }
interface Dashboard { indicadores: Indicador[]; meses: Serie; tipos: Serie; establecimientos: Serie; estados: Serie; interpretacion: string; actualizado: string }
type Filtros = { desde: string; hasta: string; centro: string };
type Dimension = 'mes' | 'tipoCaso' | 'estado' | 'centroId';
type Selecciones = Partial<Record<Dimension, string>>;
function periodo(meses: number): Filtros {
  const hasta = today(), [year, month] = hasta.split('-').map(Number);
  const inicio = new Date(Date.UTC(year, month - meses, 1)).toISOString().slice(0, 10);
  return { desde: inicio, hasta, centro: '' };
}
function mesesEntre(desde: string, hasta: string) {
  const [yearInicio, mesInicio] = desde.split('-').map(Number), [yearFin, mesFin] = hasta.split('-').map(Number);
  const cursor = new Date(Date.UTC(yearInicio, mesInicio - 1, 1)), fin = new Date(Date.UTC(yearFin, mesFin - 1, 1));
  const meses: string[] = [];
  while (cursor <= fin && meses.length < 1200) { meses.push(cursor.toISOString().slice(0, 7)); cursor.setUTCMonth(cursor.getUTCMonth() + 1); }
  return meses;
}
function periodoIncluyeMesParcial(desde: string, hasta: string) {
  const [, , inicioDia] = desde.split('-').map(Number);
  const [finAnio, finMes, finDia] = hasta.split('-').map(Number);
  const ultimoDia = new Date(Date.UTC(finAnio, finMes, 0)).getUTCDate();
  return inicioDia !== 1 || finDia !== ultimoDia;
}
const numero = (n: number) => new Intl.NumberFormat('es-CL', { maximumFractionDigits: 1 }).format(n);
const fechaCorta = (f: string) => new Intl.DateTimeFormat('es-CL', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(new Date(`${f}T12:00:00Z`));
const principales = [
  { nombre: 'Casos abiertos', icono: Clock3, clase: 'abiertos', detalle: 'Estado actual · casos por fecha del evento', accion: 'Ver casos' },
  { nombre: 'Medidas vencidas', icono: TriangleAlert, clase: 'vencidas', detalle: 'Pendientes con plazo anterior a hoy', accion: 'Ver medidas' },
  { nombre: 'Cierres del período', icono: CircleCheck, clase: 'cerrados', detalle: 'Según fecha de cierre', accion: 'Ver cierres' },
  { nombre: 'Medidas cumplidas', icono: ListChecks, clase: 'cumplidas', detalle: 'Medidas activas asociadas a los casos del período', accion: 'Ver medidas' }
];

export function DashboardPage() {
  const user = useAuth();
  const [filtros, setFiltros] = useState<Filtros>(() => periodo(12));
  const [borrador, setBorrador] = useState<Filtros>(filtros), [rango, setRango] = useState<number | null>(12);
  const [filtrosAbiertos, setFiltrosAbiertos] = useState(false);
  const botonFiltrosRef = useRef<HTMLButtonElement>(null);
  const [selecciones, setSelecciones] = useState<Selecciones>({});
  const options = useLoad<Opciones>('/opciones');
  const periodoSeleccionado = selecciones.mes ? (() => {
    const [year, month] = selecciones.mes!.split('-').map(Number);
    const inicioMes = `${selecciones.mes}-01`, finMes = new Date(Date.UTC(year, month, 0)).toISOString().slice(0, 10);
    return { desde: inicioMes < filtros.desde ? filtros.desde : inicioMes, hasta: finMes > filtros.hasta ? filtros.hasta : finMes };
  })() : { desde: filtros.desde, hasta: filtros.hasta };
  const establecimientoId = selecciones.centroId || filtros.centro;
  const mesesDisponibles = mesesEntre(filtros.desde, filtros.hasta);
  const query = new URLSearchParams({ desde: periodoSeleccionado.desde, hasta: periodoSeleccionado.hasta, ...(establecimientoId ? { establecimientoId } : {}), ...(selecciones.estado ? { estado: selecciones.estado } : {}), ...(selecciones.tipoCaso ? { tipoCaso: selecciones.tipoCaso } : {}) });
  const { data, error, loading, refresh } = useLoad<Dashboard>(`/dashboard?${query}`);
  useEffect(() => {
    const timer = window.setInterval(refresh, 60000);
    window.addEventListener('focus', refresh);
    return () => { window.clearInterval(timer); window.removeEventListener('focus', refresh); };
  }, [refresh]);
  const indicador = (nombre: string) => data?.indicadores.find(k => k.nombre === nombre);
  const total = indicador('Casos registrados')?.valor ?? 0;
  const periodoParcial = periodoIncluyeMesParcial(periodoSeleccionado.desde, periodoSeleccionado.hasta);
  const cambiado = JSON.stringify(borrador) !== JSON.stringify(filtros);
  const centroNombre = options.data?.establecimientos.find(c => c.id === establecimientoId)?.nombre || (user.rol === 'Jefatura' ? 'Mi establecimiento' : 'Todos los establecimientos');
  const enlaceReporte = (extras: Record<string, string> = {}, reporte = 'operativo') => {
    const parametros = new URLSearchParams(query); parametros.set('reporte', reporte);
    Object.entries(extras).forEach(([key, value]) => parametros.set(key, value));
    return `#/reportes?${parametros}`;
  };
  const seleccionar = (dimension: Dimension, valor: string) => setSelecciones(actuales => {
    const siguientes = { ...actuales };
    if (siguientes[dimension] === valor) delete siguientes[dimension]; else siguientes[dimension] = valor;
    return siguientes;
  });
  const limpiarSelecciones = () => setSelecciones({});
  function limpiarFiltros() {
    const predeterminados = periodo(12);
    setBorrador(predeterminados); setFiltros(predeterminados); setRango(12); limpiarSelecciones(); setFiltrosAbiertos(false);
    window.requestAnimationFrame(() => botonFiltrosRef.current?.focus());
  }
  const nombreEstado = (estado: string) => ({ Borrador: 'Borrador', EnInvestigacion: 'En investigación', EnSeguimiento: 'En seguimiento', Cerrado: 'Cerrado', Abierto: 'Abiertos' } as Record<string, string>)[estado] || estado;
  function rapido(meses: number) {
    const siguiente = { ...periodo(meses), centro: borrador.centro };
    setBorrador(siguiente); setFiltros(siguiente); setRango(meses); limpiarSelecciones(); setFiltrosAbiertos(false);
    window.requestAnimationFrame(() => botonFiltrosRef.current?.focus());
  }
  const etiquetaPeriodo = rango === 1 ? 'Este mes' : rango ? `${rango} meses` : 'Período personalizado';
  const renderKpis = (items: typeof principales) => items.map(p => {
    const k = indicador(p.nombre); if (!k) return null;
    const esTasa = k.unidad === '%';
    const sinDenominador = esTasa && !k.denominador;
    const drilldown = p.clase === 'abiertos' ? enlaceReporte({ estado: 'Abierto' })
      : p.clase === 'cerrados' ? enlaceReporte({ estado: 'Cerrado' }, 'cierres')
      : enlaceReporte({ estadoMedida: p.clase === 'vencidas' ? 'Vencida' : 'Completada' }, 'parametrizable');
    const medida = p.clase === 'vencidas' || p.clase === 'cumplidas';
    return <article key={p.nombre} className={`dash-kpi dash-kpi-${p.clase}${p.clase === 'vencidas' && (k.cantidad || 0) > 0 ? ' attention' : ''}`}>
      <div className="dash-kpi-label"><span>{k.nombre}</span><span className="dash-kpi-icon-wrap"><p.icono size={18} aria-hidden="true"/></span></div>
      <div className="dash-kpi-value"><strong>{numero(esTasa ? k.cantidad ?? 0 : k.valor)}</strong><span>{medida ? 'medidas' : 'casos'}</span></div>
      <p title={k.formula}>{esTasa ? sinDenominador ? 'Sin medidas activas para calcular el porcentaje' : <><strong className="dash-kpi-rate">{numero(k.valor)}%</strong> de {numero(k.denominador!)} medidas activas</> : p.detalle}</p>
      <a className="dash-kpi-link" href={drilldown} aria-label={`${p.accion}: ${p.nombre}`}>{p.accion}<ArrowUpRight size={14} aria-hidden="true"/></a>
    </article>;
  });
  return <div className="dash-workspace" aria-busy={loading}>
    <header className="dash-heading">
      <div>
        <div className="dash-badge-row">
          <span className="dash-badge-institutional"><Activity size={14} aria-hidden="true"/>GIATEP · Gestión diaria</span>
        </div>
        <h1>Panel de gestión</h1>
        <p>Casos, medidas y cierres del período seleccionado.</p>
      </div>
      <div className="dash-heading-actions">
        <a className="secondary" href="#/reportes">Ver reportes<ArrowUpRight size={16} aria-hidden="true"/></a>
        <button className="dash-refresh" disabled={loading} onClick={refresh}><RefreshCw size={16} aria-hidden="true"/>Actualizar</button>
      </div>
    </header>

    <form className={`dash-filters${filtrosAbiertos ? ' is-open' : ''}`} aria-label="Filtros del dashboard" onSubmit={e => {
      e.preventDefault(); setFiltros(borrador); setRango(null); limpiarSelecciones(); setFiltrosAbiertos(false);
      window.requestAnimationFrame(() => botonFiltrosRef.current?.focus());
    }}>
      <button ref={botonFiltrosRef} type="button" className="dash-filter-toggle" aria-expanded={filtrosAbiertos} aria-controls="dash-filter-panel" onClick={() => setFiltrosAbiertos(open => !open)}>
        <span className="dash-filter-toggle-title"><SlidersHorizontal size={19} aria-hidden="true"/><span>Filtros de Período y Establecimiento</span></span>
        <span className="dash-filter-toggle-summary">{fechaCorta(periodoSeleccionado.desde)} — {fechaCorta(periodoSeleccionado.hasta)} <span>· {centroNombre}</span></span>
        <span className="dash-filter-toggle-state"><span className={cambiado ? 'dash-filter-pending-badge pending' : 'dash-filter-pending-badge'}>{cambiado ? 'Cambios pendientes' : etiquetaPeriodo}</span><ChevronDown size={18} aria-hidden="true"/></span>
      </button>
      <div id="dash-filter-panel" className="dash-filter-panel" hidden={!filtrosAbiertos}>
        <div className="dash-filter-top">
          <span>Períodos rápidos</span>
          <div className="dash-presets" role="group" aria-label="Períodos rápidos">
            {[{ meses: 1, texto: 'Este mes' }, { meses: 3, texto: '3 meses' }, { meses: 12, texto: '12 meses' }].map(p => <button type="button" key={p.meses} aria-pressed={rango === p.meses && !cambiado} onClick={() => rapido(p.meses)}>{p.texto}</button>)}
          </div>
        </div>
        <div className="dash-filter-fields">
          <Field label="Desde" type="date" required value={borrador.desde} max={borrador.hasta || today()} onChange={e => setBorrador({ ...borrador, desde: e.target.value })}/>
          <Field label="Hasta" type="date" required value={borrador.hasta} min={borrador.desde} max={today()} onChange={e => setBorrador({ ...borrador, hasta: e.target.value })}/>
          <Select label="Establecimiento" value={borrador.centro} onChange={e => setBorrador({ ...borrador, centro: e.target.value })}><option value="">{user.rol === 'Jefatura' ? 'Mi establecimiento' : 'Todos los establecimientos'}</option>{options.data?.establecimientos.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}</Select>
          <div className="dash-filter-actions">
            <button className="secondary dash-reset-filters" type="button" onClick={limpiarFiltros}><RotateCcw size={15} aria-hidden="true"/>Limpiar filtros</button>
            <button className="primary" type="submit" disabled={!cambiado}>Aplicar filtros</button>
          </div>
        </div>
      </div>
      {cambiado && <p className="dash-filter-pending" role="status">Tienes cambios de filtros por aplicar.</p>}
    </form>

    <div className="dash-context">
      {periodoParcial && <span className="dash-period-warning"><TriangleAlert size={15} aria-hidden="true"/>Incluye un mes incompleto</span>}
      <span className="dash-updated" role="status">{loading ? 'Actualizando…' : data ? `Actualizado ${new Intl.DateTimeFormat('es-CL', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'America/Santiago' }).format(new Date(data.actualizado))}` : 'Datos no disponibles'}</span>
    </div>

    {Object.values(selecciones).some(Boolean) && <div className="dash-active-filters" aria-label="Filtros aplicados desde los gráficos" aria-live="polite">
      <span className="dash-active-label">Filtros cruzados activos:</span>
      {selecciones.mes && <button type="button" onClick={() => seleccionar('mes', selecciones.mes!)}>Mes: {new Intl.DateTimeFormat('es-CL', { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(new Date(`${selecciones.mes}-01T12:00:00Z`))}<X size={14}/></button>}
      {selecciones.tipoCaso && <button type="button" onClick={() => seleccionar('tipoCaso', selecciones.tipoCaso!)}>Tipo: {selecciones.tipoCaso === 'Accidente' ? 'Accidente' : 'Enfermedad profesional'}<X size={14}/></button>}
      {selecciones.estado && <button type="button" onClick={() => seleccionar('estado', selecciones.estado!)}>Estado: {nombreEstado(selecciones.estado)}<X size={14}/></button>}
      {selecciones.centroId && <button type="button" onClick={() => seleccionar('centroId', selecciones.centroId!)}>Centro: {options.data?.establecimientos.find(c => c.id === selecciones.centroId)?.nombre || 'Seleccionado'}<X size={14}/></button>}
      <button type="button" className="dash-clear-filters" onClick={limpiarSelecciones}><RotateCcw size={14}/>Limpiar filtros</button>
    </div>}

    <Message error={error || options.error}/>
    {loading && !data ? <div className="dash-loading"><Loading/></div> : error ? <section className="dash-empty"><TriangleAlert size={30} aria-hidden="true"/><h2>No pudimos cargar el panorama</h2><p>Los filtros se conservaron. Puedes volver a intentarlo.</p><button className="secondary" onClick={refresh}>Volver a intentar</button></section> : data && <>
      {!total ? <section className="dash-empty"><ClipboardList size={32} aria-hidden="true"/><h2>No hay casos en este período</h2><p>Amplía las fechas o selecciona otro establecimiento para consultar su información.</p><button className="secondary" onClick={() => rapido(12)}>Consultar últimos 12 meses</button></section> : <>
        <section className="dash-overview" aria-label="Resumen visual de casos y medidas">
          <div className="dash-overview-left"><div className="dash-kpis" aria-label="Casos abiertos y medidas vencidas">{renderKpis(principales.slice(0, 2))}</div></div>
          <div className="dash-overview-right"><div className="dash-kpis" aria-label="Cierres del período y medidas cumplidas">{renderKpis(principales.slice(2))}</div></div>
          <ComposicionEventos data={data.tipos} seleccionado={selecciones.tipoCaso} onSelect={tipoCaso => seleccionar('tipoCaso', tipoCaso)}/>
        </section>
        <section className="dash-monthly-wide" aria-label="Evolución mensual de casos">
          <EvolucionMensual key={query.toString()} data={data.meses} mesesDisponibles={mesesDisponibles} seleccionado={selecciones.mes} onSelect={mes => seleccionar('mes', mes)}/>
        </section>
        <section className="dash-dashboard-flow" aria-label="Estado de casos y gestión de medidas">
          <FlujoCasos data={data.estados} seleccionado={selecciones.estado} onSelect={estado => seleccionar('estado', estado)}/>
          <aside className="dash-priority dash-next-step">
            <span className="dash-priority-icon"><ListChecks size={24} aria-hidden="true"/></span>
            <div>
              <p className="dash-overline">PLANES DE ACCIÓN</p>
              <h2>Revisa responsables y plazos</h2>
              <p>Consulta las medidas activas y su seguimiento.</p>
            </div>
            <a className="secondary" href={user.rol === 'Administrador' ? '#/reportes' : '#/planes'}>
              {user.rol === 'Administrador' ? 'Ver reportes' : 'Ver planes'}<ArrowUpRight size={16} aria-hidden="true"/>
            </a>
          </aside>
        </section>

        <section className="dash-closure" aria-label="Resultados de cierre">
          {[
            { nombre: 'Casos cerrados', icono: CircleCheck, descripcion: 'Estado actual de los casos por fecha del evento.', href: enlaceReporte({ estado: 'Cerrado' }) },
            { nombre: 'Cierres del período', icono: CalendarRange, descripcion: 'Casos cerrados por fecha de cierre.', href: enlaceReporte({ estado: 'Cerrado' }, 'cierres') },
            { nombre: 'Tiempo de cierre', icono: Clock3, descripcion: 'Promedio desde el evento hasta el cierre.', href: enlaceReporte({ estado: 'Cerrado' }) }
          ].map(item => {
            const k = indicador(item.nombre); if (!k) return null;
            const porcentajeCerrado = item.nombre === 'Casos cerrados';
            const sinMuestra = item.nombre === 'Tiempo de cierre' && !k.muestra;
            return <article className="dash-closure-item" key={item.nombre}>
              <div className="dash-closure-icon-wrap"><item.icono size={22} aria-hidden="true"/></div>
              <div className="dash-closure-content">
                <span>{item.nombre}</span>
                {porcentajeCerrado ? <>
                  <strong>{numero(k.cantidad ?? 0)}<small>casos</small></strong>
                  <p>{k.denominador ? <><b>{numero(k.valor)}%</b> de {numero(k.denominador)} casos</> : 'Sin casos para calcular el porcentaje'}</p>
                </> : item.nombre === 'Tiempo de cierre' ? <>
                  <strong>{sinMuestra ? '—' : numero(k.valor)}<small>{k.unidad}</small></strong>
                  <p>{sinMuestra ? 'Sin cierres con fecha disponible' : `${numero(k.muestra ?? 0)} casos con fecha de cierre`}</p>
                </> : <>
                  <strong>{numero(k.valor)}<small>casos</small></strong>
                  <p>{item.descripcion}</p>
                </>}
                <a className="dash-kpi-link" href={item.href}>Ver datos<ArrowUpRight size={14} aria-hidden="true"/></a>
              </div>
            </article>;
          })}
        </section>

        <CasosCentros data={data.establecimientos} idCentro={nombre => options.data?.establecimientos.find(c => c.nombre === nombre)?.id} seleccionado={selecciones.centroId} onSelect={centroId => seleccionar('centroId', centroId)}/>
      </>}
      <p className="dash-footnote">Actualización automática cada minuto. Los casos se agrupan por fecha del evento; las medidas corresponden a esos casos. El plazo vencido se compara con la fecha local actual.</p>
    </>}
  </div>;
}
