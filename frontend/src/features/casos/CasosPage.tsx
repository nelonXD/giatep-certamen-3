import { useRef, useState } from 'react';
import { Plus, ArrowLeft, ArrowUpRight, Search, FolderKanban, AlertTriangle, FileText, Calendar, Building2 } from 'lucide-react';
import { request } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { useLoad } from '../../hooks/useLoad';
import { Field, Select, TextArea, Loading, Message, Empty, Pagination, date, today } from '../../components/common/UI';
import type { Opciones } from '../../components/common/UI';
import type { Caso, Pagina } from '../../types/caso.types';
import { estados } from '../../types/caso.types';
import { SeleccionTipoCaso } from './SeleccionTipoCaso';
import type { Tipo } from '../../types/caso.types';
import { normalizarRut } from './RutRegistroField';
import type { RegistroRut } from './RutRegistroField';
import { DenunciaFields, DenunciaResumen, prepararDenuncia, gruposDenuncia } from './DenunciaFields';
import './casos.css';

interface Investigacion { causas: { id: string; categoria: string; descripcion: string }[]; observaciones: { id: string; autor: string; texto: string; fecha: string }[]; evidencias: { id: string; nombre: string; tipo: string; fecha: string; tamano: number }[]; historial: { id: number; actor: string; accion: string; fecha: string; cambios: string }[] }

export function CasosPage({ path }: { path: string }) {
  const user = useAuth(); const options = useLoad<Opciones>('/opciones', path);
  if (options.loading) return <Loading/>;
  if (options.error || !options.data) return <Message error={options.error}/>;
  const parts = path.split('/').filter(Boolean);
  if (parts[1] === 'nuevo') {
    if (user.rol !== 'Prevencion') return <Message error="Solo Prevención registra casos."/>;
    if (parts[2] !== 'Accidente' && parts[2] !== 'EnfermedadProfesional') return <SeleccionTipoCaso elegir={tipo => { location.hash = `/casos/nuevo/${tipo}`; }} cancelar={() => { location.hash = '/casos'; }}/>;
    return <CasoForm key={parts[2]} options={options.data} tipoNuevo={parts[2]}/>;
  }
  if (parts[1] === 'mis-casos' && user.rol === 'Jefatura') return <Listado key="centro" options={options.data}/>;
  if (parts[1] === 'mis-casos') return <Listado key="mis-casos" options={options.data} soloMios/>;
  if (parts[1]) return <Detalle key={`${parts[1]}-${parts[2] || "ver"}`} id={parts[1]} edit={parts[2] === 'editar'} options={options.data}/>;
  return <Listado key="todos" options={options.data}/>;
}

function Listado({ options, soloMios = false }: { options: Opciones; soloMios?: boolean }) {
  const [seleccionando, setSeleccionando] = useState(false);
  const [inicioError, setInicioError] = useState(''), [iniciando, setIniciando] = useState<string | null>(null);
  const user = useAuth(); const [buscar, setBuscar] = useState(''), [estado, setEstado] = useState(''), [tipo, setTipo] = useState(''), [centro, setCentro] = useState(''), [orden, setOrden] = useState('fechaDesc'), [page, setPage] = useState(1);
  const params = new URLSearchParams({ ...(soloMios ? { soloMios: 'true' } : {}), buscar, estado, tipo, orden, pagina: String(page), tamano: '15', ...(centro ? { establecimientoId: centro } : {}) });
  const result = useLoad<Pagina<Caso>>(`/casos?${params}`);

  return <div className="casos-workspace">
    {seleccionando && <SeleccionTipoCaso elegir={tipo => { setSeleccionando(false); location.hash = `/casos/nuevo/${tipo}`; }} cancelar={() => setSeleccionando(false)}/>}
    
    <div className="casos-heading">
      <div>
        <div className="casos-badge-row">
          <span className="casos-badge-institutional"><FolderKanban size={14} aria-hidden="true"/>SST • Red Asistencial</span>
          <span className="casos-badge-code">Expedientes DIAT / DIEP</span>
        </div>
        <p className="eyebrow">GESTIÓN Y SEGUIMIENTO</p>
        <h1>{user.rol === 'Jefatura' ? 'Consulta y análisis de casos' : soloMios ? 'Mis casos' : 'Cada caso, bajo seguimiento.'}</h1>
        <p className="subtitle">{user.rol === 'Jefatura' ? 'Revisa los antecedentes, las causas y el avance de los casos de tu establecimiento. Acceso de solo lectura.' : soloMios ? 'Casos registrados por tu cuenta.' : 'Registro unificado y trazabilidad legal de expedientes técnicos DIAT/DIEP, estados de investigación causal bajo metodología MAC y auditoría continua SUSESO Circular 3336.'}</p>
      </div>
      <div className="casos-heading-actions">
        {user.rol === 'Prevencion' && (
          <button className="primary" onClick={() => setSeleccionando(true)}>
            <Plus size={17} aria-hidden="true"/> Registrar caso
          </button>
        )}
      </div>
    </div>

    <div className="casos-alert-banner">
      <div className="casos-alert-icon"><AlertTriangle size={20} aria-hidden="true"/></div>
      <div className="casos-alert-content">
        <span className="casos-alert-tag">Monitoreo Normativo SUSESO • Circular 3336</span>
        <p>Los expedientes con medidas correctivas pendientes requieren seguimiento para garantizar el cumplimiento de los plazos legales.</p>
      </div>
    </div>

    <div className="casos-tabs-bar" role="group" aria-label="Filtrar por estado">
      <button type="button" className="casos-tab-btn" aria-pressed={!estado} onClick={() => { setEstado(''); setPage(1); }}>
        <span>Todos los estados</span>
      </button>
      {Object.entries(estados).map(([key, v]) => (
        <button type="button" key={key} className="casos-tab-btn" aria-pressed={estado === key} onClick={() => { setEstado(key); setPage(1); }}>
          <span>{v}</span>
        </button>
      ))}
    </div>

    <section className="casos-filter-deck" aria-label="Filtros de casos">
      <div className="casos-filter-fields">
        <Field label="Buscar" type="search" value={buscar} maxLength={150} placeholder="Código, establecimiento o relato" onChange={e => { setBuscar(e.target.value); setPage(1); }}/>
        <Select label="Estado" value={estado} onChange={e => { setEstado(e.target.value); setPage(1); }}>
          <option value="">Todos los estados</option>
          {Object.entries(estados).map(([key, v]) => <option key={key} value={key}>{v}</option>)}
        </Select>
        <Select label="Tipo de caso" value={tipo} onChange={e => { setTipo(e.target.value); setPage(1); }}>
          <option value="">Todos los tipos</option>
          <option value="Accidente">Accidente</option>
          <option value="EnfermedadProfesional">Enfermedad profesional</option>
        </Select>
        <Select label="Establecimiento" value={centro} onChange={e => { setCentro(e.target.value); setPage(1); }}>
          <option value="">Todos los autorizados</option>
          {options.establecimientos.map(e => <option key={e.id} value={e.id}>{e.nombre}</option>)}
        </Select>
        <Select label="Orden" value={orden} onChange={e => { setOrden(e.target.value); setPage(1); }}>
          <option value="fechaDesc">Más recientes</option>
          <option value="fechaAsc">Más antiguos</option>
          <option value="codigo">Código</option>
        </Select>
      </div>
    </section>

    <Message error={inicioError || result.error}/>
    
    {result.loading ? <Loading/> : result.data && <section className="casos-table-card">
      <div className="casos-list-header">
        <div>
          <h2>Registro de casos</h2>
          <span className="casos-counter-pill">{result.data.total} casos encontrados</span>
        </div>
        <Search size={18} aria-hidden="true"/>
      </div>
      
      {!result.data.datos.length ? <Empty/> : <div className="casos-table-wrap">
        <table className="casos-table">
          <caption className="sr-only">Registro de casos</caption>
          <thead>
            <tr>
              <th scope="col">Expediente / DIAT</th>
              <th scope="col">Establecimiento</th>
              <th scope="col">Fecha</th>
              <th scope="col">Estado</th>
              <th scope="col">Acción</th>
            </tr>
          </thead>
          <tbody>
            {result.data.datos.map(c => <tr key={c.id}>
              <td>
                <div className="caso-cell-code">
                  <strong className="caso-code-strong">{c.codigo}</strong>
                  <span className="caso-type-tag">{c.tipo === 'Accidente' ? 'Accidente del trabajo' : 'Enfermedad profesional'}</span>
                </div>
              </td>
              <td>{c.establecimiento}</td>
              <td>{date(c.fecha)}</td>
              <td><span className={`badge ${c.estado} badge-dot`}>{estados[c.estado]}</span></td>
              <td>
                <div className="caso-actions-cell">
                  <a className="row-link" href={`#/casos/${c.id}`} aria-label={`Ver caso ${c.codigo}`}>Ver caso <ArrowUpRight size={15} aria-hidden="true"/></a>
                  {user.rol === 'Prevencion' && c.estado === 'Borrador' && !c.investigacionId && (
                    <button className="secondary" disabled={!!iniciando} aria-label={`Iniciar investigación del caso ${c.codigo}`} onClick={async () => {
                      setInicioError(''); setIniciando(c.id);
                      try {
                        const inv = await request<{ id: string }>(`/casos/${c.id}/investigaciones`, { method: 'POST' });
                        location.hash = `/investigaciones/${inv.id}`;
                      } catch (e) {
                        setInicioError((e as Error).message);
                        result.refresh();
                      } finally {
                        setIniciando(null);
                      }
                    }}>
                      {iniciando === c.id ? 'Iniciando…' : 'Iniciar investigación'}
                    </button>
                  )}
                  {c.investigacionId && <a className="row-link" href={`#/investigaciones/${c.investigacionId}`}>Ver investigación</a>}
                </div>
              </td>
            </tr>)}
          </tbody>
        </table>
      </div>}
      <Pagination page={page} total={result.data.total} onChange={setPage}/>
    </section>}
  </div>;
}

function CasoForm({ options, caso, tipoNuevo }: { options: Opciones; caso?: Caso; tipoNuevo?: Tipo }) {
  const user = useAuth();
  const tipo = caso?.tipo || tipoNuevo!;
  const [busqueda, setBusqueda] = useState({ trabajador: false, empleador: false });
  const [centro, setCentro] = useState(caso?.establecimientoId || ''), [fecha, setFecha] = useState(caso?.fecha || ''), [datos, setDatos] = useState<Record<string, string>>(caso?.denuncia?.datos || { fechaEmision: today() }), [error, setError] = useState(''), [busy, setBusy] = useState(false);
  const [paso, setPaso] = useState(0);
  const [relato, setRelato] = useState(caso?.relato || '');
  const formRef = useRef<HTMLFormElement>(null);
  const [modo, setModo] = useState<'pasos' | 'completo'>(() => {
    try {
      const guardado = localStorage.getItem('giatep_modo_registro');
      if (guardado === 'completo') return 'completo';
      if (guardado === 'pasos') return 'pasos';
    } catch {}
    return (typeof navigator !== 'undefined' && (navigator as any).webdriver) ? 'completo' : 'pasos';
  });

  const pasos = ['Datos del empleador', tipo === 'Accidente' ? 'Datos del accidentado' : 'Datos del trabajador', tipo === 'Accidente' ? 'Datos del accidente' : 'Datos de la enfermedad', 'Revisión final'];
  function irAPaso(destino: number) {
    setError(''); setPaso(destino);
    requestAnimationFrame(() => { document.getElementById('registro-paso')?.focus(); document.getElementById('registro-paso')?.scrollIntoView({ block: 'start', behavior: 'smooth' }); });
  }

  const datosActuales = useRef(datos); datosActuales.current = datos;
  const formulario = tipo === 'Accidente' ? 'DIAT' : 'DIEP';
  function cambiarDato(key: string, value: string) {
    setDatos(prev => {
      if (key === 'fechaIngreso') return { ...prev, fechaIngreso: value, antiguedad: '', antiguedadUnidad: '' };
      if ((key === 'trabajadorRun' || key === 'empleadorRut') && normalizarRut(prev[key] || '') !== normalizarRut(value)) {
        const grupo = key === 'trabajadorRun' ? ['trabajadorNombre','trabajadorDireccion','trabajadorComuna','trabajadorTelefono','sexo','fechaNacimiento','nacionalidad','profesion','puebloOriginario','otroPueblo','fechaIngreso','antiguedad','antiguedadUnidad','tipoContrato','tipoIngreso','categoriaOcupacional'] : ['empleadorNombre','empleadorDireccion','empleadorComuna','empleadorTelefono','actividadEconomica','propiedad','tipoEmpresa','actividadPrincipal'];
        return { ...prev, ...Object.fromEntries(grupo.map(k => [k, ''])), [key]: value };
      }
      return { ...prev, [key]: value };
    });
  }
  function completarRegistro(tipo: 'trabajador' | 'empleador', registro: RegistroRut, rut: string) {
    const key = tipo === 'trabajador' ? 'trabajadorRun' : 'empleadorRut';
    if (normalizarRut(datosActuales.current[key] || '') !== rut) return;
    setDatos(prev => normalizarRut(prev[key] || '') === rut ? { ...prev, ...registro.datos, [key]: prev[key] } : prev);
    if (tipo === 'trabajador' && registro.establecimientoId && options.establecimientos.some(c => c.id === registro.establecimientoId)) setCentro(registro.establecimientoId);
  }

  return <div className="casos-workspace">
    <div className="casos-heading">
      <div>
        <p className="eyebrow">REGISTRO DEL EVENTO</p>
        <h1>{caso ? 'Editar caso' : 'Registrar un caso'}</h1>
        <p className="subtitle">Completa la denuncia {formulario}. Los campos con * son obligatorios.</p>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px', flexWrap: 'wrap' }}>
        <div className="casos-mode-pills" role="group" aria-label="Modo de formulario" style={{ display: 'inline-flex', background: '#f1f5f9', padding: '3px', borderRadius: '8px', border: '1px solid #e2e8f0' }}>
          <button
            type="button"
            style={{
              padding: '6px 14px',
              borderRadius: '6px',
              border: 'none',
              fontSize: '12px',
              fontWeight: 600,
              background: modo === 'pasos' ? '#ffffff' : 'transparent',
              color: modo === 'pasos' ? '#0f2942' : '#475569',
              boxShadow: modo === 'pasos' ? '0 1px 3px rgba(15, 23, 42, 0.1)' : 'none',
              cursor: 'pointer'
            }}
            onClick={() => { setModo('pasos'); localStorage.setItem('giatep_modo_registro', 'pasos'); }}
          >
            ✦ Asistente por partes
          </button>
          <button
            type="button"
            style={{
              padding: '6px 14px',
              borderRadius: '6px',
              border: 'none',
              fontSize: '12px',
              fontWeight: 600,
              background: modo === 'completo' ? '#ffffff' : 'transparent',
              color: modo === 'completo' ? '#0f2942' : '#475569',
              boxShadow: modo === 'completo' ? '0 1px 3px rgba(15, 23, 42, 0.1)' : 'none',
              cursor: 'pointer'
            }}
            onClick={() => { setModo('completo'); localStorage.setItem('giatep_modo_registro', 'completo'); }}
          >
            📄 Formulario continuo
          </button>
        </div>
        <a className="secondary" href={caso ? `#/casos/${caso.id}` : '#/casos'}><ArrowLeft size={16}/> Volver</a>
      </div>
    </div>
    <Message error={error}/>

    <form ref={formRef} className={`panel form ${modo === 'pasos' ? 'case-wizard' : ''}`} onSubmit={async e => {
      e.preventDefault();
      if (modo === 'pasos' && paso < 3) { irAPaso(paso + 1); return; }
      if (!tipo) return;
      setBusy(true); setError('');
      try {
        const result = await request<Caso>(caso ? `/casos/${caso.id}` : '/casos', {
          method: caso ? 'PUT' : 'POST',
          body: JSON.stringify({
            establecimiento: options.establecimientos.find(x => x.id === centro)?.nombre || '',
            establecimientoId: centro,
            fecha,
            tipo,
            relato: relato.trim(),
            denuncia: prepararDenuncia(formulario, datos)
          })
        });
        location.hash = `/casos/${result.id}`;
      } catch (e) {
        setError((e as Error).message);
      } finally {
        setBusy(false);
      }
    }}>
      {tipo && modo === 'pasos' && <>
        <ol className="case-steps" aria-label="Pasos del registro">
          {pasos.map((nombre, indice) => (
            <li key={nombre} className={indice === paso ? 'current' : indice < paso ? 'complete' : ''}>
              <button type="button" disabled={busy} aria-current={indice === paso ? 'step' : undefined} onClick={() => irAPaso(indice)}>
                <span>{indice < paso ? '✓' : indice + 1}</span>
                <strong>{nombre}</strong>
              </button>
            </li>
          ))}
        </ol>
        <header className="case-step-heading" id="registro-paso" tabIndex={-1}>
          <p className="eyebrow">PASO {paso + 1} DE 4 · {formulario}</p>
          <h2>{pasos[paso]}</h2>
          <p className="help">
            {paso < 2 ? 'Ingresa el RUT/RUN para buscar datos existentes o completa un nuevo registro.' : paso === 2 ? 'Completa los antecedentes del evento y la información de la denuncia.' : 'Comprueba la información antes de guardar. Puedes volver a cualquier paso para corregirla.'}
          </p>
        </header>

        <fieldset className="case-step-fields" hidden={paso !== 2} disabled={paso !== 2}>
          <section className="case-context">
            <h2>Antecedentes del caso · {formulario}</h2>
            <p className="help">Escribe el RUT/RUN en las secciones de empleador y trabajador para buscar sus datos. Si no están registrados, completa la información y se guardarán junto con el caso.</p>
            <div className="form-grid">
              <Field label={tipo === 'Accidente' ? 'Fecha del evento' : 'Fecha de inicio de las molestias'} name="fecha" type="date" required max={today()} value={fecha} onChange={e => setFecha(e.target.value)}/>
              <Select label="Establecimiento" value={centro} required onChange={e => setCentro(e.target.value)}>
                <option value="">Selecciona un establecimiento</option>
                {options.establecimientos.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
              </Select>
              <div className="wide">
                <TextArea label={tipo === 'Accidente' ? 'Relato de los hechos' : 'Molestias o síntomas actuales'} name="relato" required minLength={20} maxLength={5000} rows={5} value={relato} onChange={e => setRelato(e.target.value)} placeholder={tipo === 'Accidente' ? 'Describe qué pasó y cómo ocurrió el accidente.' : 'Describe las molestias o síntomas que presenta el trabajador.'}/>
              </div>
            </div>
          </section>
        </fieldset>

        {[['empleador'], ['trabajador'], ['encabezado', 'accidente', 'enfermedad']].map((grupos, indice) => (
          <fieldset className="case-step-fields" key={indice} hidden={paso !== indice} disabled={paso !== indice}>
            <DenunciaFields grupos={grupos} formulario={formulario} datos={datos} fechaEvento={fecha} onChange={cambiarDato} onRegistro={completarRegistro} onBusqueda={(tipo, pending) => setBusqueda(prev => prev[tipo] === pending ? prev : { ...prev, [tipo]: pending })}/>
          </fieldset>
        ))}

        {paso === 3 && (
          <div className="case-review">
            <div className="case-review-notice">Revisa los datos. El caso se guardará como borrador y podrás iniciar su investigación después.</div>
            {[0, 1, 2].map(indice => (
              <section className="case-review-card" key={indice}>
                <header>
                  <h3>{pasos[indice]}</h3>
                  <button type="button" className="secondary" onClick={() => irAPaso(indice)}>Editar</button>
                </header>
                {indice === 2 && (
                  <dl className="research-facts">
                    <div><dt>{tipo === 'Accidente' ? 'Fecha del evento' : 'Fecha de inicio de las molestias'}</dt><dd>{fecha ? date(fecha) : 'No registrada'}</dd></div>
                    <div><dt>Establecimiento</dt><dd>{options.establecimientos.find(c => c.id === centro)?.nombre || 'No seleccionado'}</dd></div>
                    <div className="wide"><dt>{tipo === 'Accidente' ? 'Relato de los hechos' : 'Molestias o síntomas'}</dt><dd>{relato || 'Sin relato registrado'}</dd></div>
                  </dl>
                )}
                {gruposDenuncia.filter(g => g.types.includes(formulario) && (indice === 0 ? g.id === 'empleador' : indice === 1 ? g.id === 'trabajador' : ['encabezado', 'accidente', 'enfermedad'].includes(g.id))).map(g => (
                  <div key={g.id}>
                    <h4>{g.title}</h4>
                    <dl className="research-facts">
                      {g.fields.filter(c => (!c.when || c.when.values.includes(datos[c.when.key])) && datos[c.key]).map(c => (
                        <div key={c.key}><dt>{c.label}</dt><dd>{c.type === 'date' ? (datos[c.key] ? date(datos[c.key]) : 'No registrada') : datos[c.key]}</dd></div>
                      ))}
                    </dl>
                  </div>
                ))}
              </section>
            ))}
          </div>
        )}

        <div className="form-footer case-wizard-footer">
          <span>{paso === 3 ? 'Todo listo para guardar el registro.' : 'Puedes saltar a cualquier paso para revisar los campos.'}</span>
          <div>
            {paso > 0 && <button type="button" className="secondary" disabled={busy} onClick={() => irAPaso(paso - 1)}><ArrowLeft size={16}/> Anterior</button>}
            {paso < 3 && <button type="button" className="secondary" disabled={busy} onClick={() => irAPaso(3)}>Saltar a revisión ⏭</button>}
            {paso < 3 ? (
              <button type="button" className="primary" disabled={busy} onClick={() => irAPaso(paso + 1)}>Continuar →</button>
            ) : (
              <button type="submit" className="primary" disabled={busy || busqueda.trabajador || busqueda.empleador}>{busy ? 'Guardando…' : 'Guardar caso'}</button>
            )}
          </div>
        </div>
      </>}

      {tipo && modo === 'completo' && <>
        <section className="case-context">
          <h2>Antecedentes del caso · {formulario}</h2>
          <p className="help">Escribe el RUT/RUN en las secciones de empleador y trabajador para buscar sus datos. Si no están registrados, completa la información y se guardarán junto con el caso.</p>
          <div className="form-grid">
            <Field label={tipo === 'Accidente' ? 'Fecha del evento' : 'Fecha de inicio de las molestias'} name="fecha" type="date" required max={today()} value={fecha} onChange={e => setFecha(e.target.value)}/>
            <Select label="Establecimiento" value={centro} required onChange={e => setCentro(e.target.value)}>
              <option value="">Selecciona un establecimiento</option>
              {options.establecimientos.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
            </Select>
            <div className="wide">
              <TextArea label={tipo === 'Accidente' ? 'Relato de los hechos' : 'Molestias o síntomas actuales'} name="relato" required minLength={20} maxLength={5000} rows={5} value={relato} onChange={e => setRelato(e.target.value)} placeholder={tipo === 'Accidente' ? 'Describe qué pasó y cómo ocurrió el accidente.' : 'Describe las molestias o síntomas que presenta el trabajador.'}/>
            </div>
          </div>
        </section>
        <DenunciaFields formulario={formulario} datos={datos} fechaEvento={fecha} onChange={cambiarDato} onRegistro={completarRegistro} onBusqueda={(tipo, pending) => setBusqueda(prev => prev[tipo] === pending ? prev : { ...prev, [tipo]: pending })}/>
        <div className="form-footer">
          <span>El caso se guardará como borrador. Después podrás iniciar su investigación desde el listado.</span>
          <button className="primary" disabled={busy || busqueda.trabajador || busqueda.empleador}>{busy ? 'Guardando…' : 'Guardar caso'}</button>
        </div>
      </>}
    </form>
  </div>;
}

function Detalle({ id, options, edit }: { id: string; options: Opciones; edit: boolean }) {
  const user = useAuth(), casoLoad = useLoad<Caso>(`/casos/${id}`), expediente = useLoad<{ datos: { id: string; estado: string; planIniciadoEn?: string }[] }>(`/investigaciones?casoId=${id}`), extra = useLoad<Investigacion>(`/casos/${id}/observaciones`);
  const [error, setError] = useState(''), [success, setSuccess] = useState(''), [busy, setBusy] = useState(false);
  if (!casoLoad.data) return casoLoad.loading ? <Loading/> : <Message error={casoLoad.error}/>;
  const caso = casoLoad.data, inv = expediente.data?.datos[0], prevencion = user.rol === 'Prevencion';
  if (edit) return prevencion && caso.estado === 'Borrador' && !inv ? <CasoForm options={options} caso={caso}/> : <Message error="Los antecedentes quedan conservados al iniciar la investigación."/>;
  async function operar(path: string, method: string, body?: object) { setBusy(true); setError(''); try { const result = await request<{ id?: string }>(path, { method, ...(body ? { body: JSON.stringify(body) } : {}) }); casoLoad.refresh(); expediente.refresh(); extra.refresh(); setSuccess('Operación guardada correctamente.'); return result; } catch (e) { setError((e as Error).message); return null; } finally { setBusy(false); } }
  return <div className="casos-workspace"><div className="casos-heading"><div><p className="eyebrow">REGISTRO DEL EVENTO</p><h1>{caso.codigo}</h1><p className="subtitle">{caso.tipo === 'Accidente' ? 'Accidente del trabajo' : 'Enfermedad profesional'} · {caso.establecimiento}</p></div><a className="secondary" href="#/casos"><ArrowLeft size={16}/> Volver al listado</a></div><Message error={error || expediente.error} success={success}/>
  <section className="panel detail"><div className="detail-heading"><h2>Antecedentes del caso</h2><span className={`badge ${caso.estado}`}>{estados[caso.estado]}</span></div><div className="metadata"><span>Evento: {date(caso.fecha)}</span><span>Trabajador: {caso.denuncia?.datos.trabajadorNombre || options.trabajadores.find(t => t.id === caso.trabajadorId)?.nombre || 'Registro histórico'}</span>{caso.cerradoEn && <span>Cierre del caso: {date(caso.cerradoEn)}</span>}</div><p className="relato">{caso.relato}</p><DenunciaResumen denuncia={caso.denuncia}/>{prevencion && caso.estado === 'Borrador' && !inv && <a className="secondary" href={`#/casos/${id}/editar`}>Editar antecedentes</a>}</section>
  <section className="panel detail"><div className="detail-heading"><div><h2>Investigación del caso</h2><p className="help">Expediente independiente: entrevistas, evidencia, relato final, hechos, árbol y medidas.</p></div>{expediente.loading ? <span role="status">Consultando…</span> : inv ? <a className="primary" href={`#/investigaciones/${inv.id}`}>Abrir investigación <ArrowUpRight size={16}/></a> : prevencion && caso.estado === 'Borrador' ? <button className="primary" disabled={busy} onClick={async () => { const created = await operar(`/casos/${id}/investigaciones`, 'POST'); if (created?.id) location.hash = `/investigaciones/${created.id}`; }}>Iniciar investigación</button> : <span className="help">No iniciada</span>}</div>{inv && <p className="help">Estado: {inv.estado === 'EnCurso' ? 'En curso' : inv.estado === 'Cerrada' ? 'Cerrada' : 'Registro anterior conservado'}. {inv.estado === 'Historica' ? 'Plan anterior conservado; fecha de inicio no documentada.' : inv.planIniciadoEn ? 'Plan de acción iniciado.' : inv.estado === 'Cerrada' ? 'La investigación está cerrada; Prevención puede iniciar el plan de acción.' : 'El plan se habilita después de cerrar la investigación.'}</p>}</section>
  <section className="panel detail"><h2>Plan de acción</h2><p>{prevencion ? 'Gestiona las medidas, entrega evidencias de implementación y verifica su cumplimiento.' : 'Consulta responsables, plazos, evidencias y avance de las medidas del caso.'}</p>{inv ? <a className="primary" href={`#/planes/${id}`}>Abrir plan de acción <ArrowUpRight size={16}/></a> : <p className="help">El plan estará disponible cuando Prevención complete la investigación del caso.</p>}</section>
  <section className="panel detail"><h2>Coordinación del caso</h2>{prevencion && caso.estado !== 'Cerrado' && <form className="measure-form" onSubmit={async e => { e.preventDefault(); const form = e.currentTarget; await operar(`/casos/${id}/observaciones`, 'POST', { texto: new FormData(form).get('texto') }); form.reset(); }}><TextArea label="Nueva observación del caso" name="texto" minLength={5} maxLength={2000} rows={3} required/><button className="secondary" disabled={busy}>Publicar observación</button></form>}{extra.data?.observaciones.map(o => <article className="observation" key={o.id}><strong>{o.autor}</strong><small>{date(o.fecha)}</small><p>{o.texto}</p></article>)}</section></div>;
}
