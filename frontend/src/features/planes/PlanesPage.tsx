import { useState } from 'react';
import './planes.css';
import {
  ArrowLeft,
  ArrowUpRight,
  Download,
  CheckCircle2,
  Clock,
  AlertTriangle,
  FileText,
  GitBranch,
  User,
  Calendar,
  ShieldCheck,
  Check,
  Send,
  RotateCcw,
  FolderOpen,
  Filter,
  AlertCircle
} from 'lucide-react';
import { request, descargar } from '../../api/client';
import { useLoad } from '../../hooks/useLoad';
import { useAuth } from '../../context/AuthContext';
import { Field, Select, TextArea, Loading, Message, Pagination, Empty, date, today } from '../../components/common/UI';

interface Ficha {
  casoId: string;
  codigo: string;
  establecimiento: string;
  estado: string;
  totalMedidas: number;
  verificadas: number;
  porRevisar: number;
  vencidas: number;
  planIniciadoEn?: string;
}

interface Medida {
  id: string;
  descripcion: string;
  responsable: string;
  usuarioResponsableId?: string;
  plazo: string;
  completada: boolean;
  implementadaEn?: string;
  comentarioCumplimiento?: string;
  revisadaEn?: string;
  revision?: string;
  revisor?: string;
  hecho?: string;
}

interface Plan {
  id: string;
  codigo: string;
  establecimiento: string;
  estado: string;
  cerradoEn?: string;
  investigacion: { id: string; estado: string; planIniciadoEn?: string };
  medidas: Medida[];
  evidencias: { id: string; medidaId: string; nombre: string; fecha: string; tamano: number }[];
}

export function PlanesPage({ path }: { path: string }) {
  const user = useAuth();
  const id = path.split('/')[2];
  if (id === 'mis-planes' && user.rol === 'Jefatura') return <Listado key="centro" />;
  return id === 'mis-planes' ? <Listado key="mios" soloMios /> : id ? <Detalle id={id} /> : <Listado key="todos" />;
}

function Listado({ soloMios = false }: { soloMios?: boolean }) {
  const user = useAuth();
  const [buscar, setBuscar] = useState('');
  const [estado, setEstado] = useState('');
  const [pagina, setPagina] = useState(1);
  const result = useLoad<{ total: number; datos: Ficha[] }>(
    `/planes?${new URLSearchParams({ ...(soloMios ? { soloMios: 'true' } : {}), buscar, estado, pagina: String(pagina) })}`
  );

  return (
    <div className="plan-workspace">
      <div className="page-heading">
        <div>
          <p className="eyebrow">IMPLEMENTACIÓN Y VERIFICACIÓN</p>
          <h1>
            {user.rol === 'Jefatura'
              ? 'Seguimiento de planes de acción'
              : soloMios
              ? 'Mis planes de acción'
              : 'Lista de planes de acción'}
          </h1>
          <p className="subtitle">
            {user.rol === 'Jefatura'
              ? 'Consulta el avance, los responsables, los plazos y las evidencias de los planes de tu establecimiento.'
              : soloMios
              ? 'Planes con medidas asignadas a tu cuenta.'
              : 'Todos los planes disponibles dentro de tu ámbito de acceso.'}
          </p>
        </div>
        <a className="secondary" href="#/investigaciones">
          Ver investigaciones
        </a>
      </div>

      <section className="panel filter-panel" aria-label="Filtros de planes">
        <Field
          label="Buscar plan"
          type="search"
          maxLength={150}
          value={buscar}
          placeholder="Código de caso o establecimiento..."
          onChange={e => {
            setBuscar(e.target.value);
            setPagina(1);
          }}
        />
        <Select
          label="Estado del plan"
          value={estado}
          onChange={e => {
            setEstado(e.target.value);
            setPagina(1);
          }}
        >
          <option value="">Todos los estados</option>
          <option value="PorIniciar">Por iniciar</option>
          <option value="EnSeguimiento">En seguimiento</option>
          <option value="Cerrado">Cerrado</option>
        </Select>
      </section>

      <Message error={result.error} />
      {result.loading ? (
        <Loading />
      ) : (
        result.data && (
          <section className="panel">
            <p className="help" style={{ marginBottom: 16 }}>
              {result.data.total} planes encontrados
            </p>
            {!result.data.datos.length ? (
              <Empty>
                {soloMios
                  ? 'No tienes planes con medidas asignadas que coincidan con estos filtros.'
                  : 'No hay planes con estos filtros. Los planes están disponibles después del cierre de la investigación.'}
              </Empty>
            ) : (
              <div className="table-wrap">
                <table>
                  <caption className="sr-only">Planes de acción por caso</caption>
                  <thead>
                    <tr>
                      <th scope="col">Caso</th>
                      <th scope="col">Establecimiento</th>
                      <th scope="col">Estado</th>
                      <th scope="col">Avance verificado</th>
                      <th scope="col">Por revisar</th>
                      <th scope="col">Vencidas</th>
                      <th scope="col">Acción</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.data.datos.map(p => {
                      const pct = p.totalMedidas > 0 ? Math.round((p.verificadas / p.totalMedidas) * 100) : 0;
                      return (
                        <tr key={p.casoId}>
                          <td>
                            <strong>{p.codigo}</strong>
                          </td>
                          <td>{p.establecimiento}</td>
                          <td>
                            <span
                              className={`badge ${
                                p.estado === 'Cerrado'
                                  ? 'Cerrado'
                                  : p.estado === 'EnSeguimiento'
                                  ? 'EnSeguimiento'
                                  : 'Borrador'
                              }`}
                            >
                              {p.estado === 'Cerrado'
                                ? 'Cerrado'
                                : p.estado === 'EnSeguimiento'
                                ? 'En seguimiento'
                                : 'Por iniciar'}
                            </span>
                          </td>
                          <td>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 8, minWidth: 120 }}>
                              <div
                                style={{
                                  flex: 1,
                                  height: 6,
                                  background: 'var(--surface-container)',
                                  borderRadius: 4,
                                  overflow: 'hidden'
                                }}
                              >
                                <div
                                  style={{
                                    width: `${pct}%`,
                                    height: '100%',
                                    background: pct === 100 ? '#10b981' : '#0284c7',
                                    borderRadius: 4
                                  }}
                                />
                              </div>
                              <span style={{ fontSize: 12, fontWeight: 600 }}>
                                {p.verificadas}/{p.totalMedidas} ({pct}%)
                              </span>
                            </div>
                          </td>
                          <td>
                            {p.porRevisar > 0 ? (
                              <span style={{ color: 'var(--secondary)', fontWeight: 600 }}>{p.porRevisar} pendientes</span>
                            ) : (
                              '0'
                            )}
                          </td>
                          <td>
                            {p.vencidas > 0 ? (
                              <span style={{ color: '#dc2626', fontWeight: 700 }}>⚠️ {p.vencidas} vencidas</span>
                            ) : (
                              '0'
                            )}
                          </td>
                          <td>
                            <a className="row-link" href={`#/planes/${p.casoId}`}>
                              Abrir plan <ArrowUpRight size={15} />
                            </a>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
            <Pagination page={pagina} total={result.data.total} size={15} onChange={setPagina} />
          </section>
        )
      )}
    </div>
  );
}

function Detalle({ id }: { id: string }) {
  const user = useAuth();
  const load = useLoad<Plan>(`/planes/${id}`);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [busy, setBusy] = useState(false);
  const [filtro, setFiltro] = useState('');
  const prevencion = user.rol === 'Prevencion';

  async function operar(path: string, method: string, body?: object | FormData) {
    setBusy(true);
    setError('');
    setSuccess('');
    try {
      await request(path, {
        method,
        body: body === undefined ? undefined : body instanceof FormData ? body : JSON.stringify(body)
      });
      load.refresh();
      setSuccess('Plan de acción actualizado correctamente.');
      return true;
    } catch (e) {
      setError((e as Error).message);
      return false;
    } finally {
      setBusy(false);
    }
  }

  if (!load.data) return load.loading ? <Loading /> : <Message error={load.error} />;

  const plan = load.data;
  const activo = plan.estado === 'EnSeguimiento';
  const totalMedidas = plan.medidas.length;
  const verificadas = plan.medidas.filter(m => m.completada).length;
  const porVerificar = plan.medidas.filter(m => !m.completada && m.implementadaEn).length;
  const devueltas = plan.medidas.filter(m => !m.completada && !m.implementadaEn && m.revisadaEn).length;
  const pendientes = plan.medidas.filter(m => !m.completada && !m.implementadaEn && !m.revisadaEn).length;
  const vencidas = plan.medidas.filter(m => !m.completada && m.plazo < today()).length;
  const mias = plan.medidas.filter(m => m.usuarioResponsableId === user.id).length;
  const pct = totalMedidas > 0 ? Math.round((verificadas / totalMedidas) * 100) : 0;

  const estadoMedida = (m: Medida) =>
    m.completada
      ? 'Verificada'
      : m.implementadaEn
      ? 'Por verificar'
      : m.revisadaEn
      ? 'Devuelta'
      : 'Pendiente';

  const medidas = plan.medidas.filter(m => {
    if (!filtro) return true;
    if (filtro === 'Mias') return m.usuarioResponsableId === user.id;
    if (filtro === 'Vencidas') return !m.completada && m.plazo < today();
    return estadoMedida(m) === filtro;
  });

  return (
    <div className="plan-workspace">
      {/* 1. Encabezado y Navegación */}
      <header className="plan-header">
        <p className="eyebrow">GESTIÓN PREVENTIVA · CASO {plan.codigo}</p>
        <div className="plan-header-title-row">
          <h1>
            Plan de acción preventivo
            <span className={`plan-status-badge status-${plan.estado}`}>
              {plan.estado === 'Cerrado'
                ? '✓ Cerrado'
                : activo
                ? '● En seguimiento'
                : '○ Por iniciar'}
            </span>
          </h1>
          <div className="action-row">
            <a className="secondary" href="#/planes">
              <ArrowLeft size={16} /> Volver a planes
            </a>
            <a className="secondary" href={`#/casos/${id}`}>
              Ver caso <ArrowUpRight size={14} />
            </a>
            <a className="secondary" href={`#/investigaciones/${plan.investigacion.id}`}>
              Ver investigación causal <ArrowUpRight size={14} />
            </a>
          </div>
        </div>
        <div className="plan-meta-row">
          <span className="plan-meta-item">
            <strong>Establecimiento:</strong> {plan.establecimiento}
          </span>
          {plan.cerradoEn && (
            <span className="plan-meta-item">
              <strong>Cerrado formalmente:</strong> {date(plan.cerradoEn)}
            </span>
          )}
          {!prevencion && (
            <span className="plan-meta-item" style={{ color: 'var(--secondary)' }}>
              (Modo consulta / Jefatura)
            </span>
          )}
        </div>
      </header>

      <Message error={error || load.error} success={success} />

      {/* 2. Resumen Ejecutivo de Métricas (KPI Summary) */}
      <section className="plan-kpi-grid" aria-label="Resumen de avance del plan">
        {/* Tarjeta Hero: Porcentaje y Progreso */}
        <div className="plan-kpi-card hero">
          <div className="plan-kpi-hero-header">
            <div>
              <span className="hero-pct">{pct}%</span>
              <span className="hero-sub" style={{ marginLeft: 8 }}>cumplimiento</span>
            </div>
            <span className="hero-sub">
              {verificadas} de {totalMedidas} verificadas
            </span>
          </div>
          <div className="plan-progress-track" role="progressbar" aria-valuenow={pct} aria-valuemin={0} aria-valuemax={100}>
            <div className="plan-progress-fill" style={{ width: `${pct}%` }} />
          </div>
          <p className="plan-kpi-hero-footer">
            Ciclo S&SO: <strong>Implementación en terreno</strong> → <strong>Revisión técnica</strong> → <strong>Verificación</strong>.
          </p>
        </div>

        {/* Tarjeta 2: Verificadas */}
        <div className="plan-kpi-card">
          <div className="plan-kpi-stat-top">
            <span className="plan-kpi-label">VERIFICADAS</span>
            <span className="plan-kpi-stat-icon success">
              <CheckCircle2 size={18} />
            </span>
          </div>
          <p className="plan-kpi-number" style={{ color: '#059669' }}>
            {verificadas}
          </p>
          <small className="help">Aprobadas por Prevención</small>
        </div>

        {/* Tarjeta 3: Por Verificar */}
        <div className="plan-kpi-card">
          <div className="plan-kpi-stat-top">
            <span className="plan-kpi-label">POR VERIFICAR</span>
            <span className="plan-kpi-stat-icon info">
              <Clock size={18} />
            </span>
          </div>
          <p className="plan-kpi-number" style={{ color: '#0284c7' }}>
            {porVerificar}
          </p>
          <small className="help">Con evidencia esperando revisión</small>
        </div>

        {/* Tarjeta 4: Requieren Atención (Pendientes + Devueltas + Vencidas) */}
        <div className="plan-kpi-card">
          <div className="plan-kpi-stat-top">
            <span className="plan-kpi-label">PENDIENTES / ATENCIÓN</span>
            <span className={`plan-kpi-stat-icon ${vencidas > 0 ? 'danger' : 'warning'}`}>
              <AlertTriangle size={18} />
            </span>
          </div>
          <p className="plan-kpi-number" style={{ color: vencidas > 0 ? '#dc2626' : '#d97706' }}>
            {pendientes + devueltas}
          </p>
          <small className="help">
            {vencidas > 0 ? (
              <strong style={{ color: '#dc2626' }}>⚠️ {vencidas} con plazo vencido</strong>
            ) : devueltas > 0 ? (
              <strong style={{ color: '#d97706' }}>{devueltas} devuelta(s) para corregir</strong>
            ) : (
              'En fase de implementación'
            )}
          </small>
        </div>
      </section>

      {/* Caso: Plan Por Iniciar */}
      {!activo && plan.estado !== 'Cerrado' && (
        <section className="panel detail">
          <div className="detail-heading">
            <div>
              <h2>Plan pendiente de activación</h2>
              <p className="help">
                Las medidas, responsables y plazos provienen de la investigación causal cerrada. Prevención debe activar el plan para iniciar el seguimiento.
              </p>
            </div>
          </div>
          {prevencion && plan.investigacion.estado === 'Cerrada' && (
            <button
              className="primary"
              disabled={busy}
              onClick={() => void operar(`/investigaciones/${plan.investigacion.id}/plan`, 'POST')}
            >
              <ShieldCheck size={16} /> Iniciar plan de acción y activar medidas
            </button>
          )}
        </section>
      )}

      {/* 3. Barra Interactiva de Filtros */}
      {(activo || plan.estado === 'Cerrado') && (
        <>
          <nav className="plan-filter-strip" aria-label="Filtros de medidas">
            <span className="plan-filter-label">
              <Filter size={14} /> Filtrar:
            </span>
            <button
              type="button"
              className={`plan-filter-chip ${!filtro ? 'active' : ''}`}
              onClick={() => setFiltro('')}
            >
              Todas <span className="count">{totalMedidas}</span>
            </button>
            <button
              type="button"
              className={`plan-filter-chip ${filtro === 'Pendiente' ? 'active' : ''}`}
              onClick={() => setFiltro('Pendiente')}
            >
              Pendientes <span className="count">{pendientes}</span>
            </button>
            <button
              type="button"
              className={`plan-filter-chip ${filtro === 'Por verificar' ? 'active' : ''}`}
              onClick={() => setFiltro('Por verificar')}
            >
              Por verificar <span className="count">{porVerificar}</span>
            </button>
            {devueltas > 0 && (
              <button
                type="button"
                className={`plan-filter-chip ${filtro === 'Devuelta' ? 'active' : ''}`}
                onClick={() => setFiltro('Devuelta')}
              >
                Devueltas <span className="count">{devueltas}</span>
              </button>
            )}
            <button
              type="button"
              className={`plan-filter-chip ${filtro === 'Verificada' ? 'active' : ''}`}
              onClick={() => setFiltro('Verificada')}
            >
              Verificadas <span className="count">{verificadas}</span>
            </button>
            {vencidas > 0 && (
              <button
                type="button"
                className={`plan-filter-chip ${filtro === 'Vencidas' ? 'active' : ''}`}
                style={{ borderColor: '#ef4444' }}
                onClick={() => setFiltro('Vencidas')}
              >
                ⚠️ Vencidas <span className="count">{vencidas}</span>
              </button>
            )}
            {prevencion && mias > 0 && (
              <button
                type="button"
                className={`plan-filter-chip ${filtro === 'Mias' ? 'active' : ''}`}
                onClick={() => setFiltro('Mias')}
              >
                Asignadas a mí <span className="count">{mias}</span>
              </button>
            )}
          </nav>

          {!medidas.length && (
            <Empty>No hay medidas que coincidan con el filtro seleccionado.</Empty>
          )}

          {/* 4. Lista de Medidas Preventivas Rediseñadas */}
          <div className="measure-card-list">
            {medidas.map((m, index) => {
              const vencida = !m.completada && m.plazo < today();
              const evidenciasMedida = plan.evidencias.filter(e => e.medidaId === m.id);
              const cardStatusClass = m.completada
                ? 'status-verificada'
                : vencida
                ? 'status-vencida'
                : m.implementadaEn
                ? 'status-por-verificar'
                : m.revisadaEn
                ? 'status-devuelta'
                : '';

              return (
                <article
                  className={`measure-card ${cardStatusClass}`}
                  key={m.id}
                  aria-label={`Medida preventiva ${index + 1}: ${m.descripcion}`}
                >
                  {/* Cabecera de la Medida */}
                  <div className="measure-card-header">
                    <div className="measure-badges-row">
                      <span className="measure-idx-pill">MEDIDA #{index + 1}</span>

                      {m.completada ? (
                        <span className="measure-status-pill verificada">
                          <CheckCircle2 size={13} /> Verificada por Prevención
                        </span>
                      ) : m.implementadaEn ? (
                        <span className="measure-status-pill por-verificar">
                          <Clock size={13} /> En espera de revisión técnica
                        </span>
                      ) : m.revisadaEn ? (
                        <span className="measure-status-pill devuelta">
                          <RotateCcw size={13} /> Devuelta para corrección
                        </span>
                      ) : (
                        <span className="measure-status-pill pendiente">
                          <FileText size={13} /> Pendiente de implementación
                        </span>
                      )}

                      {vencida && (
                        <span className="measure-status-pill vencida">
                          <AlertTriangle size={13} /> Plazo límite vencido
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Descripción de la Medida Preventiva */}
                  <h2 className="measure-desc">{m.descripcion}</h2>

                  {/* Causa Raíz Investigada (Árbol Causal) - CLAVE PARA ENTENDER LA MEDIDA */}
                  <div className="measure-cause-callout">
                    <GitBranch className="measure-cause-icon" size={20} aria-hidden="true" />
                    <div className="measure-cause-content">
                      <strong>Causa raíz que elimina (Árbol causal de la investigación):</strong>
                      <p>
                        {m.hecho
                          ? m.hecho
                          : 'Medida correctiva general vinculada a los antecedentes y factores causales del caso.'}
                      </p>
                    </div>
                  </div>

                  {/* Metadatos: Responsable y Plazo */}
                  <div className="measure-meta-strip">
                    <div className="measure-meta-item">
                      <User size={15} aria-hidden="true" />
                      <span>
                        Responsable: <strong>{m.responsable}</strong>
                      </span>
                    </div>
                    <div className={`measure-meta-item ${vencida ? 'overdue' : ''}`}>
                      <Calendar size={15} aria-hidden="true" />
                      <span>
                        Fecha límite: <strong>{date(m.plazo)}</strong> {vencida && '(Atrasada)'}
                      </span>
                    </div>
                  </div>

                  {/* Bloques de Ciclo de Vida: Implementación y Revisión */}
                  {(m.comentarioCumplimiento || m.revision) && (
                    <div className="measure-feedback-grid">
                      {m.comentarioCumplimiento && (
                        <div className="measure-feedback-box implementation">
                          <div className="measure-feedback-header">
                            <span>
                              <Check size={14} aria-hidden="true" /> Informe de implementación en terreno
                            </span>
                            {m.implementadaEn && <small>{date(m.implementadaEn)}</small>}
                          </div>
                          <p className="measure-feedback-text">{m.comentarioCumplimiento}</p>
                        </div>
                      )}

                      {m.revision && (
                        <div
                          className={`measure-feedback-box ${
                            m.completada ? 'review-approved' : 'review-rejected'
                          }`}
                        >
                          <div className="measure-feedback-header">
                            <span>
                              {m.completada ? (
                                <ShieldCheck size={14} aria-hidden="true" />
                              ) : (
                                <AlertCircle size={14} aria-hidden="true" />
                              )}
                              Dictamen de revisión por Prevención
                            </span>
                            <small>
                              {m.revisor} · {m.revisadaEn && date(m.revisadaEn)}
                            </small>
                          </div>
                          <p className="measure-feedback-text">{m.revision}</p>
                        </div>
                      )}
                    </div>
                  )}

                  {/* Evidencias de Cumplimiento Adjuntas */}
                  <div className="measure-evidence-section">
                    <h3 className="measure-evidence-title">
                      <FolderOpen size={15} aria-hidden="true" /> Evidencias de cumplimiento ({evidenciasMedida.length})
                    </h3>
                    {evidenciasMedida.length > 0 ? (
                      <div className="measure-evidence-list">
                        {evidenciasMedida.map(e => (
                          <div className="measure-evidence-item" key={e.id}>
                            <div className="measure-evidence-file-info">
                              <FileText size={18} color="var(--secondary)" aria-hidden="true" />
                              <div>
                                <strong>{e.nombre}</strong>
                                <small>
                                  {date(e.fecha)} · {Math.ceil(e.tamano / 1024)} KB
                                </small>
                              </div>
                            </div>
                            <button
                              type="button"
                              className="secondary"
                              onClick={async () => {
                                try {
                                  await descargar(`/planes/${id}/evidencias/${e.id}`, e.nombre);
                                } catch (err) {
                                  setError((err as Error).message);
                                }
                              }}
                            >
                              <Download size={15} /> Descargar archivo
                            </button>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <p className="help">
                        Sin evidencias adjuntas.{' '}
                        {m.completada ? 'Cumplimiento verificado en registro histórico previo.' : ''}
                      </p>
                    )}
                  </div>

                  {/* Formularios de Acción Operativa */}
                  {/* Formulario 1: Responsable entrega evidencia de implementación */}
                  {activo && !m.completada && !m.implementadaEn && prevencion && (
                    <div className="measure-action-form-wrap">
                      <h4>
                        <Send size={16} aria-hidden="true" /> Registrar entrega de implementación en terreno
                      </h4>
                      <p className="help" style={{ marginBottom: 12 }}>
                        Detalla las acciones ejecutadas para implementar esta medida y adjunta el documento o foto de respaldo.
                      </p>
                      <form
                        className="measure-form"
                        onSubmit={async e => {
                          e.preventDefault();
                          const form = e.currentTarget;
                          if (await operar(`/planes/${id}/medidas/${m.id}/entrega`, 'POST', new FormData(form))) {
                            form.reset();
                          }
                        }}
                      >
                        <TextArea
                          label="Detalle de implementación en terreno"
                          name="comentario"
                          required
                          minLength={10}
                          maxLength={2000}
                          rows={3}
                          placeholder="Ej: Se instalaron barandas de contención reglamentarias y se capacitó a la cuadrilla de turno..."
                        />
                        <Field
                          label="Documento o foto de respaldo"
                          name="archivo"
                          type="file"
                          required
                          accept=".pdf,.png,.jpg,.jpeg"
                          help="Formatos admitidos: PDF, PNG o JPG (hasta 5 MB)."
                        />
                        <button className="primary" disabled={busy}>
                          <Send size={15} /> Enviar evidencia a revisión
                        </button>
                      </form>
                    </div>
                  )}

                  {/* Formulario 2: Prevención evalúa y aprueba/devuelve */}
                  {activo && !m.completada && m.implementadaEn && prevencion && (
                    <div className="measure-action-form-wrap">
                      <h4>
                        <ShieldCheck size={16} aria-hidden="true" /> Evaluación y verificación técnica (Prevención de Riesgos)
                      </h4>
                      <p className="help" style={{ marginBottom: 12 }}>
                        Evalúa si la evidencia y la acción reportada resuelven efectivamente la causa raíz del incidente.
                      </p>
                      <form
                        className="measure-form"
                        onSubmit={e => {
                          e.preventDefault();
                          const f = new FormData(e.currentTarget);
                          void operar(`/planes/${id}/medidas/${m.id}/revision`, 'PUT', {
                            aprobada: f.get('decision') === 'aprobar',
                            observacion: f.get('observacion')
                          });
                        }}
                      >
                        <TextArea
                          label="Observación o dictamen de revisión"
                          name="observacion"
                          required
                          minLength={10}
                          maxLength={2000}
                          rows={3}
                          placeholder="Fundamenta tu decisión técnica (si apruebas el cumplimiento o qué corrección debe realizar el responsable)..."
                        />
                        <Select label="Decisión técnica" name="decision">
                          <option value="aprobar">Aprobar cumplimiento de la medida (Verificada)</option>
                          <option value="devolver">Devolver para subsanar o aportar nueva evidencia</option>
                        </Select>
                        <button className="primary" disabled={busy}>
                          <Check size={15} /> Guardar dictamen técnico
                        </button>
                      </form>
                    </div>
                  )}
                </article>
              );
            })}
          </div>

          {/* 5. Cierre Formal del Plan y del Caso */}
          {activo && prevencion && (
            <div className="plan-close-card">
              <div className="plan-close-content">
                <div className="plan-close-icon">
                  <ShieldCheck size={26} aria-hidden="true" />
                </div>
                <div>
                  <h3>
                    {verificadas === totalMedidas && totalMedidas > 0
                      ? '¡Todas las medidas están verificadas! (100%)'
                      : `Avance del Plan: ${verificadas} de ${totalMedidas} medidas verificadas`}
                  </h3>
                  <p>
                    {verificadas === totalMedidas && totalMedidas > 0
                      ? 'Se ha completado el ciclo de seguimiento preventivo. Puedes formalizar el cierre del caso.'
                      : `Para habilitar el cierre del caso, se requiere que el 100% de las medidas estén formalmente verificadas por Prevención (faltan ${totalMedidas - verificadas}).`}
                  </p>
                </div>
              </div>
              <button
                type="button"
                className="primary"
                disabled={busy || !totalMedidas || verificadas !== totalMedidas}
                onClick={() => {
                  if (
                    window.confirm(
                      '¿Confirmas el cierre formal del caso? Al cerrar, el expediente quedará en modo de solo lectura.'
                    )
                  ) {
                    void operar(`/casos/${id}/estado`, 'PUT', { estado: 'Cerrado' });
                  }
                }}
              >
                <CheckCircle2 size={16} /> Cerrar caso y plan de acción
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
