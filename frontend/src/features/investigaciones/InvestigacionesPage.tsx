import './investigaciones.css';
import { useSessionView } from '../../hooks/useSessionView';
import { useId, useState } from 'react';
import { DenunciaResumen } from '../casos/DenunciaFields';
import type { Denuncia } from '../casos/DenunciaFields';
import { EvidenciaOcr } from './EvidenciaOcr';
import { ArrowLeft, ArrowUpRight, Download, Plus, FileText, Mic, FolderOpen, AlignLeft, ListChecks, GitBranch, ShieldCheck, Check, ChevronRight, ScanLine, Image as ImageIcon, CalendarDays, Building2 } from 'lucide-react';
import { request, descargar } from '../../api/client';
import { useLoad } from '../../hooks/useLoad';
import { useAuth } from '../../context/AuthContext';
import { Field, Select, TextArea, Message, Loading, Empty, Pagination, date, today } from '../../components/common/UI';
import type { Opciones } from '../../components/common/UI';

interface Ficha { id: string; casoId: string; estado: string; esHistorica: boolean; iniciadaEn: string; cerradaEn?: string; planIniciadoEn?: string; codigo?: string; establecimiento?: string }
interface Entrevista { id: string; entrevistado: string; calidad: string; fecha: string; declaracion: string }
interface Hecho { id: string; descripcion: string; esFinal: boolean }
interface Relacion { id: string; antecedenteId: string; consecuenciaId: string }
interface Propuesta { id: string; hechoId?: string; descripcion: string; usuarioResponsableId: string; responsable: string; plazo: string }
interface DetalleInvestigacion extends Ficha { antecedentes: { codigo: string; tipo: string; establecimiento: string; establecimientoId?: string; trabajador: string; trabajadorReferenciaActual?: boolean; denuncia?: Denuncia; fecha: string; relatoInicial: string }; relatoFinal: string; entrevistas: Entrevista[]; hechos: Hecho[]; relaciones: Relacion[]; medidas: Propuesta[]; evidencias: { id: string; nombre: string; tipo?: string; tamano: number; fecha: string }[]; causasPrevias: { categoria: string; descripcion: string }[]; historial: { actor: string; accion: string; fecha: string }[] }

const etiquetas: Record<string, string> = { EnCurso: 'En curso', Cerrada: 'Cerrada', Historica: 'Registro anterior' };

export function InvestigacionesPage({ path }: { path: string }) {
  const parts = path.split('/').filter(Boolean);
  const id = parts[1];
  if (id === 'mis-investigaciones') return <div className="investigation-workspace"><Listado key="mias" soloMias/></div>;
  return <div className="investigation-workspace">{id ? <Expediente key={id} id={id}/> : <Listado key="todas"/>}</div>;
}

function Listado({ soloMias = false }: { soloMias?: boolean }) {
  const [page, setPage] = useState(1), [buscar, setBuscar] = useState(''), [estado, setEstado] = useState('');
  const result = useLoad<{ total: number; datos: Ficha[] }>(`/investigaciones?${new URLSearchParams({ ...(soloMias ? { soloMias: 'true' } : {}), pagina: String(page), buscar, estado })}`);
  return <><div className="page-heading"><div><p className="eyebrow">INVESTIGACIÓN DE EVENTOS</p><h1>{soloMias ? 'Mis investigaciones' : 'Investigaciones'}</h1><p className="subtitle">{soloMias ? 'Investigaciones iniciadas por tu cuenta.' : 'Expedientes iniciados desde los casos. La investigación se cierra antes de activar su plan de acción.'}</p></div><a className="secondary" href="#/casos">Ir a los casos</a></div><section className="panel filter-panel"><Field label="Buscar investigación" type="search" maxLength={150} value={buscar} onChange={e => { setBuscar(e.target.value); setPage(1); }}/><Select label="Estado de la investigación" value={estado} onChange={e => { setEstado(e.target.value); setPage(1); }}><option value="">Todos</option><option value="EnCurso">En curso</option><option value="Cerrada">Cerrada</option><option value="Historica">Registros anteriores</option></Select></section><Message error={result.error}/>{result.loading ? <Loading/> : result.data && <section className="panel"><div className="table-wrap"><table><caption className="sr-only">Listado de investigaciones</caption><thead><tr><th scope="col">Caso</th><th scope="col">Establecimiento</th><th scope="col">Registro</th><th scope="col">Estado</th><th scope="col">Plan de acción</th><th scope="col">Acción</th></tr></thead><tbody>{result.data.datos.map(i => <tr key={i.id}><td>{i.codigo}</td><td>{i.establecimiento}</td><td>{date(i.iniciadaEn)}</td><td><span className={`badge ${i.estado === 'EnCurso' ? 'EnInvestigacion' : 'Cerrado'}`}>{etiquetas[i.estado]}</span></td><td>{i.estado === 'Historica' ? 'Plan anterior conservado' : i.planIniciadoEn ? 'Iniciado' : 'Aún no iniciado'}</td><td><a className="row-link" href={`#/investigaciones/${i.id}`}>Abrir investigación <ArrowUpRight size={15}/></a></td></tr>)}</tbody></table></div>{!result.data.datos.length && <Empty>Inicia una investigación desde el detalle de un caso registrado.</Empty>}<Pagination page={page} total={result.data.total} onChange={setPage}/></section>}</>;
}

function Expediente({ id }: { id: string }) {
  const user = useAuth(), options = useLoad<Opciones>('/opciones'), result = useLoad<DetalleInvestigacion>(`/investigaciones/${id}`);
  const viewKey = `giatep:view:${user.id}:investigacion:${id}`;
  const [tab, setTab] = useSessionView<string>(`${viewKey}:tab`, 'antecedentes', (v): v is string => typeof v === 'string' && ['antecedentes','entrevistas','evidencias','relato','hechos','arbol','medidas'].includes(v));
  const [ocrAbierto, setOcrAbierto] = useSessionView<boolean>(`${viewKey}:ocr`, false, (v): v is boolean => typeof v === 'boolean');
  const [error, setError] = useState(''), [success, setSuccess] = useState(''), [busy, setBusy] = useState(false), [editarMedida, setEditarMedida] = useState<Propuesta | null>(null), [editarEntrevista, setEditarEntrevista] = useState<Entrevista | null>(null), [editarHecho, setEditarHecho] = useState<Hecho | null>(null);
  async function action(suffix: string, body?: object | FormData, method = 'POST', absolute = false) {
    setBusy(true); setError(''); setSuccess('');
    try {
      await request(absolute ? suffix : `/investigaciones/${id}${suffix}`, { method, ...(body ? { body: body instanceof FormData ? body : JSON.stringify(body) } : {}) });
      result.refresh(); setSuccess('Información guardada correctamente.'); return true;
    } catch (e) { setError((e as Error).message); return false; }
    finally { setBusy(false); }
  }
  if (!result.data) return result.loading ? <Loading/> : <Message error={result.error}/>;
  const inv = result.data, editable = inv.estado === 'EnCurso' && user.rol === 'Prevencion';
  const centro = inv.antecedentes.establecimientoId || options.data?.establecimientos.find(c => c.nombre === inv.antecedentes.establecimiento)?.id;
  const pendientes = [!inv.entrevistas.length && 'entrevistas', !inv.evidencias.length && 'evidencias', inv.relatoFinal.trim().length < 20 && 'relato final', inv.hechos.length < 2 && 'hechos', !inv.hechos.some(h => h.esFinal) && 'hecho final', !inv.relaciones.length && 'árbol de causas', (!inv.medidas.length || inv.medidas.some(m => !m.hechoId)) && 'medidas vinculadas'].filter(Boolean);
  const tabs = [['antecedentes','Antecedentes'],['entrevistas','Entrevistas'],['evidencias','Evidencias'],['relato','Relato final'],['hechos','Lista de hechos'],['arbol','Árbol de causas'],['medidas','Medidas']];
  const registered = [true, inv.entrevistas.length > 0, inv.evidencias.length > 0, inv.relatoFinal.trim().length >= 20, inv.hechos.length >= 2 && inv.hechos.some(h => h.esFinal), inv.relaciones.length > 0, inv.medidas.length > 0 && inv.medidas.every(m => m.hechoId)];
  const completed = registered.filter(Boolean).length;
  const icons = [FileText, Mic, FolderOpen, AlignLeft, ListChecks, GitBranch, ShieldCheck];


  return <>
    <nav className="investigation-breadcrumb" aria-label="Ruta de navegación"><a href="#/investigaciones">Investigaciones</a><ChevronRight size={14} aria-hidden="true"/><span>Expediente</span></nav>
    <div className="page-heading investigation-heading">
      <div>
        <p className="eyebrow">EXPEDIENTE DE INVESTIGACIÓN</p>
        <h1>{inv.antecedentes.codigo} <span className={`investigation-status status-${inv.estado}`}>{etiquetas[inv.estado]}</span></h1>
        <p className="subtitle"><span><Building2 size={15} aria-hidden="true"/>{inv.antecedentes.establecimiento}</span><span><CalendarDays size={15} aria-hidden="true"/>{inv.esHistorica ? 'Antecedente registrado' : 'Iniciada'} {date(inv.iniciadaEn)}</span></p>
      </div>
      <a className="secondary" href={`#/casos/${inv.casoId}`}><ArrowLeft size={16}/> Ver caso</a>
    </div>
    <Message error={error || result.error || options.error} success={success}/>
    {inv.esHistorica && <p className="dev-notice">Este expediente conserva datos de la versión anterior. No se inventaron entrevistas, hechos ni relatos finales. {inv.estado === 'EnCurso' ? 'Completa las secciones y vincula las medidas conservadas a sus hechos antes de cerrarlo.' : 'Se conservan los antecedentes disponibles y sus actualizaciones; la investigación cerrada permanece de solo lectura.'}</p>}
    <section className="investigation-summary" aria-label="Resumen del expediente">
      <div className="investigation-progress"><div><span>Avance de registro</span><strong>{completed} <small>/ 7 secciones</small></strong></div><progress value={completed} max={7} aria-label="Secciones registradas"/><p>El cierre requiere la validación del expediente.</p></div>
      {[{label:'Entrevistas',value:inv.entrevistas.length,icon:Mic,key:'entrevistas'},{label:'Evidencias',value:inv.evidencias.length,icon:FolderOpen,key:'evidencias'},{label:'Hechos registrados',value:inv.hechos.length,icon:ListChecks,key:'hechos'}].map(item=><button type="button" key={item.key} onClick={()=>setTab(item.key)} className="investigation-metric"><span className="investigation-metric-icon"><item.icon size={20} aria-hidden="true"/></span><span><strong>{item.value}</strong><small>{item.label}</small></span><ChevronRight size={16} aria-hidden="true"/></button>)}
    </section>
    <nav className="tabs investigation-tabs" aria-label="Secciones de la investigación">
      {tabs.map(([key, label], index) => { const Icon = icons[index]; return <button key={key} className={tab === key ? 'selected' : ''} aria-pressed={tab === key} onClick={() => setTab(key)}><span className={`investigation-step${registered[index] ? ' complete' : ''}`}>{registered[index] ? <Check size={12} aria-hidden="true"/> : index+1}</span><Icon size={16} aria-hidden="true"/>{label}</button>; })}
    </nav>

    {<section hidden={tab !== 'antecedentes'} className="panel detail">
      <h2>Antecedentes del caso al iniciar</h2>
      <dl className="research-facts">
        <div><dt>Caso</dt><dd>{inv.antecedentes.codigo}</dd></div>
        <div><dt>Tipo</dt><dd>{inv.antecedentes.tipo === 'Accidente' ? 'Accidente del trabajo' : 'Enfermedad profesional'}</dd></div>
        <div><dt>Fecha del evento</dt><dd>{date(inv.antecedentes.fecha)}</dd></div>
        <div><dt>{inv.antecedentes.trabajadorReferenciaActual ? 'Trabajador asociado (referencia actual)' : 'Trabajador'}</dt><dd>{inv.antecedentes.trabajador}</dd></div>
      </dl>
      <h3>Relato inicial</h3>
      <p className="relato">{inv.antecedentes.relatoInicial}</p>
      <DenunciaResumen denuncia={inv.antecedentes.denuncia}/>
      <p className="help">Estos antecedentes se conservan como instantánea del caso. El análisis y el relato final se registran en sus propias secciones.</p>
      {inv.causasPrevias.length > 0 && <details><summary>Antecedentes causales conservados de la versión anterior</summary>{inv.causasPrevias.map((c, i) => <p key={i}>{c.categoria}: {c.descripcion}</p>)}</details>}
    </section>}

    {<section hidden={tab !== 'entrevistas'} className="panel detail">
      <h2>Lista de entrevistas</h2>
      {inv.entrevistas.map(e => <article className="observation" key={e.id}>
        <strong>{e.entrevistado} · {e.calidad}</strong>
        <small>{date(e.fecha)}</small>
        <p>{e.declaracion}</p>
        {editable && <button className="secondary" onClick={() => setEditarEntrevista(e)}>Editar entrevista</button>}
      </article>)}
      {!inv.entrevistas.length && <p className="help">Todavía no hay entrevistas.</p>}
      {editable && <form key={editarEntrevista?.id || 'nueva'} className="measure-form" onSubmit={async e => {
        e.preventDefault();
        const form = e.currentTarget, f = new FormData(form);
        if (await action(`/entrevistas${editarEntrevista ? `/${editarEntrevista.id}` : ''}`, { entrevistado: f.get('entrevistado'), calidad: f.get('calidad'), fecha: f.get('fecha'), declaracion: f.get('declaracion') }, editarEntrevista ? 'PUT' : 'POST')) {
          setEditarEntrevista(null);
          form.reset();
        }
      }}>
        <h3>Registrar entrevista</h3>
        <div className="form-grid">
          <Field label="Persona entrevistada" name="entrevistado" defaultValue={editarEntrevista?.entrevistado} required minLength={3} maxLength={120}/>
          <Select label="Calidad de la persona" name="calidad" defaultValue={editarEntrevista?.calidad}><option>Trabajador</option><option>Testigo</option><option>Jefatura</option><option>Otro</option></Select>
          <Field label="Fecha de entrevista" name="fecha" defaultValue={editarEntrevista?.fecha} type="date" min={inv.antecedentes.fecha} max={today()} required/>
        </div>
        <TextArea label="Declaración de la entrevista" name="declaracion" defaultValue={editarEntrevista?.declaracion} required minLength={20} maxLength={5000} rows={5}/>
        <button className="secondary" disabled={busy}>Guardar entrevista</button>
        {editarEntrevista && <button type="button" className="secondary" onClick={() => setEditarEntrevista(null)}>Cancelar edición</button>}
      </form>}
    </section>}

    {<section hidden={tab !== 'evidencias'} className="panel detail">
      <header className="investigation-section-heading"><span className="investigation-section-icon"><FolderOpen size={23} aria-hidden="true"/></span><div><p className="eyebrow">DOCUMENTACIÓN DEL EXPEDIENTE</p><h2>Evidencias de la investigación</h2><p className="help">Reúne los documentos que respaldan los hechos y revisa las transcripciones antes de incorporarlas.</p></div><span className="investigation-count">{inv.evidencias.length} archivos</span></header>
      {inv.evidencias.map(e => <article className="measure investigation-evidence" key={e.id}>
        <span className="investigation-file-icon">{e.tipo?.startsWith('image/') ? <ImageIcon size={21} aria-hidden="true"/> : <FileText size={21} aria-hidden="true"/>}</span><div className="investigation-file-info"><strong>{e.nombre}</strong><p>{date(e.fecha)} <span aria-hidden="true">·</span> {Math.ceil(e.tamano / 1024)} KB <span aria-hidden="true">·</span> {e.tipo?.startsWith('image/') ? 'Imagen original' : e.tipo?.includes('wordprocessingml') ? 'Documento Word' : 'Documento'}</p></div>
        <button className="secondary" onClick={async () => { try { await descargar(`/casos/${inv.casoId}/evidencias/${e.id}`, e.nombre); } catch (e) { setError((e as Error).message); } }}><Download size={16}/> Descargar</button>
      </article>)}
      {!inv.evidencias.length && <div className="investigation-empty"><FolderOpen size={28} aria-hidden="true"/><div><strong>Un espacio para respaldar los hechos</strong><p>Adjunta la primera evidencia o transcribe una imagen con el escáner.</p></div></div>}
      {editable && <form className="measure-form" onSubmit={async e => { e.preventDefault(); const form = e.currentTarget; if (await action(`/casos/${inv.casoId}/evidencias`, new FormData(form), 'POST', true)) form.reset(); }}>
        <h3><Plus size={17} aria-hidden="true"/> Adjuntar un archivo</h3>
        <Field label="Archivo de evidencia" name="archivo" type="file" required accept=".pdf,.jpg,.jpeg,.png"/>
        <button className="secondary" disabled={busy}>Adjuntar evidencia</button>
      </form>}
      {editable && <details open={ocrAbierto} onToggle={e => setOcrAbierto(e.currentTarget.open)} className="ocr-accordion" style={{ marginTop: '1.5rem' }}>
        <summary><span className="investigation-scan-icon"><ScanLine size={22} aria-hidden="true"/></span><span><strong>Escanear imagen con IA</strong><small>Extrae el texto, conserva su distribución y añade recortes del original.</small></span><span className="investigation-optional">Opcional</span></summary>
        <div style={{ marginTop: '1rem' }}>
          <EvidenciaOcr id={id} casoId={inv.casoId} evidencias={inv.evidencias.map(e => ({ id: e.id, nombre: e.nombre, tipo: e.tipo || 'image/png' }))} onSaved={result.refresh}/>
        </div>
      </details>}
    </section>}

    {<section hidden={tab !== 'relato'} className="panel detail">
      <h2>Relato final de la investigación</h2>
      <p className="help">Reconstruye lo ocurrido a partir de antecedentes, entrevistas y evidencias. Es distinto del relato inicial del caso.</p>
      {editable ? <form key={inv.relatoFinal} className="measure-form" onSubmit={e => { e.preventDefault(); void action('/relato', { texto: new FormData(e.currentTarget).get('texto') }, 'PUT'); }}>
        <TextArea label="Relato final" name="texto" required minLength={20} maxLength={10000} rows={10} defaultValue={inv.relatoFinal}/>
        <button className="primary" disabled={busy}>Guardar relato final</button>
      </form> : <p className="relato">{inv.relatoFinal || 'No fue registrado en el expediente anterior.'}</p>}
    </section>}

    {<section hidden={tab !== 'hechos'} className="panel detail">
      <h2>Lista de hechos verificados</h2>
      <p className="help">Registra hechos concretos. Marca uno como hecho final y relaciona los demás en el árbol.</p>
      {inv.hechos.map((h, i) => <article className="observation" key={h.id}>
        <span className={`badge ${h.esFinal ? 'Cerrado' : ''}`}>{h.esFinal ? 'Hecho final' : `Hecho ${i + 1}`}</span>
        <p>{h.descripcion}</p>
        {editable && <button className="secondary" onClick={() => setEditarHecho(h)}>Editar hecho</button>}
      </article>)}
      {editable && <form key={editarHecho?.id || 'nuevo'} className="measure-form" onSubmit={async e => {
        e.preventDefault();
        const form = e.currentTarget, f = new FormData(form);
        if (await action(`/hechos${editarHecho ? `/${editarHecho.id}` : ''}`, { descripcion: f.get('descripcion'), esFinal: f.get('esFinal') === 'true' }, editarHecho ? 'PUT' : 'POST')) {
          setEditarHecho(null);
          form.reset();
        }
      }}>
        <TextArea label="Descripción del hecho" name="descripcion" defaultValue={editarHecho?.descripcion} required minLength={10} maxLength={1000} rows={3}/>
        <Select label="Tipo de hecho" name="esFinal" defaultValue={String(editarHecho?.esFinal || false)}>
          <option value="false">Antecedente</option>
          {!inv.hechos.some(h => h.esFinal && h.id !== editarHecho?.id) && <option value="true">Hecho final (evento investigado)</option>}
        </Select>
        <button className="secondary" disabled={busy}>{editarHecho ? 'Guardar hecho' : 'Agregar hecho'}</button>
        {editarHecho && <button type="button" className="secondary" onClick={() => setEditarHecho(null)}>Cancelar edición</button>}
      </form>}
    </section>}

    {<section hidden={tab !== 'arbol'} className="panel detail">
      <h2>Árbol de causas</h2>
      <p className="help">Las flechas indican antecedente → consecuencia. Todos los hechos deben llegar al hecho final; no se permiten ciclos. Puedes representar varios antecedentes necesarios para un mismo hecho.</p>
      <Arbol hechos={inv.hechos} relaciones={inv.relaciones}/>
      {editable && <form className="measure-form" onSubmit={async e => {
        e.preventDefault();
        const f = new FormData(e.currentTarget);
        await action('/relaciones', { antecedenteId: f.get('antecedente'), consecuenciaId: f.get('consecuencia') });
      }}>
        <h3>Relacionar hechos</h3>
        <div className="form-grid">
          <Select label="Hecho antecedente" name="antecedente" required>
            <option value="">Selecciona</option>
            {inv.hechos.filter(h => !h.esFinal).map(h => <option key={h.id} value={h.id}>{h.descripcion}</option>)}
          </Select>
          <Select label="Hecho consecuencia" name="consecuencia" required>
            <option value="">Selecciona</option>
            {inv.hechos.map(h => <option key={h.id} value={h.id}>{h.descripcion}</option>)}
          </Select>
        </div>
        <button className="secondary" disabled={busy}>Agregar relación</button>
      </form>}
      {inv.relaciones.map(r => <article className="measure" key={r.id}>
        <p>{inv.hechos.find(h => h.id === r.antecedenteId)?.descripcion} → {inv.hechos.find(h => h.id === r.consecuenciaId)?.descripcion}</p>
        {editable && <button className="secondary" disabled={busy} onClick={() => void action(`/relaciones/${r.id}`, undefined, 'DELETE')}>Quitar relación</button>}
      </article>)}
    </section>}

    {<section hidden={tab !== 'medidas'} className="panel detail">
      <h2>Medidas de la investigación</h2>
      <p className="help">Son propuestas vinculadas a hechos causales. Pasan al plan de acción únicamente después del cierre de la investigación y al iniciar el plan.</p>
      {inv.medidas.map(m => <article className="measure" key={m.id}>
        <div>
          <strong>{m.descripcion}</strong>
          <p>{m.responsable} · Plazo: {date(m.plazo)}</p>
          <p className={m.hechoId ? 'help' : 'field-error'}>{m.hechoId ? `Hecho: ${inv.hechos.find(h => h.id === m.hechoId)?.descripcion}` : 'Medida conservada: falta vincular a un hecho causal.'}</p>
        </div>
        {editable && <button className="secondary" onClick={() => setEditarMedida(m)}>Editar medida</button>}
      </article>)}
      {editable && <form key={editarMedida?.id || 'nueva'} className="measure-form" onSubmit={async e => {
        e.preventDefault();
        const form = e.currentTarget, f = new FormData(form);
        if (await action(`/medidas${editarMedida ? `/${editarMedida.id}` : ''}`, { hechoId: f.get('hechoId'), descripcion: f.get('descripcion'), usuarioResponsableId: f.get('responsable'), plazo: f.get('plazo') }, editarMedida ? 'PUT' : 'POST')) {
          setEditarMedida(null);
          form.reset();
        }
      }}>
        <h3>{editarMedida ? 'Editar medida de investigación' : 'Proponer medida'}</h3>
        <Select label="Hecho causal asociado" name="hechoId" required defaultValue={editarMedida?.hechoId || ''}>
          <option value="">Selecciona un hecho antecedente</option>
          {inv.hechos.filter(h => !h.esFinal).map(h => <option key={h.id} value={h.id}>{h.descripcion}</option>)}
        </Select>
        <TextArea label="Medida preventiva o correctiva" name="descripcion" required minLength={10} maxLength={1000} rows={3} defaultValue={editarMedida?.descripcion}/>
        <div className="form-grid">
          <Select label="Responsable de la medida" name="responsable" required defaultValue={editarMedida?.usuarioResponsableId || ''}>
            <option value="">Selecciona responsable</option>
            {options.data?.responsables.filter(u => u.rol === 'Prevencion' || u.establecimientoId === centro).map(u => <option key={u.id} value={u.id}>{u.nombre}</option>)}
          </Select>
          <Field label="Plazo de implementación" name="plazo" type="date" min={inv.antecedentes.fecha} required defaultValue={editarMedida?.plazo}/>
        </div>
        <div className="action-row">
          <button className="secondary" disabled={busy}><Plus size={16}/> Guardar medida</button>
          {editarMedida && <button className="secondary" type="button" onClick={() => setEditarMedida(null)}>Cancelar edición</button>}
        </div>
      </form>}
    </section>}
    <section className="panel detail investigation-governance">
      <div className="detail-heading">
        <div>
          <h2>{etiquetas[inv.estado]}</h2>
          <p className="help">{inv.cerradaEn ? `Cierre de investigación: ${date(inv.cerradaEn)}` : 'El cierre de la investigación no cierra el caso.'}</p>
        </div>
        <div className="action-row">
          {editable && <button className="secondary" disabled={busy} onClick={() => { if (window.confirm('¿Cerrar la investigación? Se bloqueará su edición; luego podrás iniciar el plan de acción.')) void action('/cerrar'); }}>Cerrar investigación</button>}
          {inv.estado === 'Cerrada' && !inv.planIniciadoEn && user.rol === 'Prevencion' && <button className="primary" disabled={busy} onClick={async () => { if (await action('/plan')) location.hash = `/planes/${inv.casoId}`; }}>Iniciar plan de acción</button>}
          {(inv.planIniciadoEn || inv.estado === 'Historica') && <a className="primary" href={`#/planes/${inv.casoId}`}>Ver plan de acción <ArrowUpRight size={16}/></a>}
        </div>
      </div>
      {editable && <p className="help">{pendientes.length ? `Secciones por completar: ${pendientes.join(', ')}.` : 'Secciones registradas. El servidor verificará también las conexiones del árbol y los responsables antes del cierre.'}</p>}
    </section>

  </>;
}

function Arbol({ hechos, relaciones }: { hechos: Hecho[]; relaciones: Relacion[] }) {
  const marker = useId().replace(/[^a-zA-Z0-9]/g, '');
  if (!hechos.length) return <Empty>Registra los hechos antes de construir el árbol.</Empty>;
  function nivel(h: Hecho, visited = new Set<string>()): number {
    if (h.esFinal) return 0;
    if (visited.has(h.id)) return 1;
    const next = relaciones.filter(r => r.antecedenteId === h.id).map(r => hechos.find(h => h.id === r.consecuenciaId)).filter((h): h is Hecho => !!h);
    const seen = new Set(visited); seen.add(h.id);
    return next.length ? 1 + Math.max(...next.map(h => nivel(h, seen))) : 1;
  }
  const levels = hechos.map(h => nivel(h)), max = Math.max(...levels), posiciones = hechos.map((h, i) => ({ ...h, x: (max - levels[i]) * 300 + 20, y: hechos.slice(0, i).filter((_, j) => levels[j] === levels[i]).length * 135 + 20 }));
  const height = Math.max(...posiciones.map(h => h.y)) + 135, width = (max + 1) * 300 + 20;
  return <>
    <div className="tree-scroll">
      <svg width={width} height={height} role="img" aria-labelledby={`${marker}title ${marker}desc`}>
        <title id={`${marker}title`}>Árbol de causas de la investigación</title>
        <desc id={`${marker}desc`}>{relaciones.length} relaciones entre {hechos.length} hechos. Consulta la lista de hechos y las relaciones textuales para leer los datos completos.</desc>
        <defs>
          <marker id={marker} markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">
            <path d="M0,0 L8,4 L0,8 Z" fill="#174c40"/>
          </marker>
        </defs>
        {relaciones.map(r => {
          const a = posiciones.find(h => h.id === r.antecedenteId), b = posiciones.find(h => h.id === r.consecuenciaId);
          return a && b ? <path key={r.id} d={`M${a.x + 250},${a.y + 50} C${a.x + 280},${a.y + 50} ${b.x - 30},${b.y + 50} ${b.x},${b.y + 50}`} stroke="#174c40" strokeWidth="2" fill="none" markerEnd={`url(#${marker})`}/> : null;
        })}
        {posiciones.map(h => {
          const words = h.descripcion.match(/.{1,30}(?:\s|$)|.{1,30}/g) || [h.descripcion];
          return <g key={h.id}>
            <rect x={h.x} y={h.y} width="250" height="105" rx="8" fill={h.esFinal ? '#174c40' : '#eff4eb'} stroke="#799581"/>
            <text x={h.x + 12} y={h.y + 20} fontSize="11" fill={h.esFinal ? 'white' : '#174c40'}>{h.esFinal ? 'HECHO FINAL' : 'HECHO ANTECEDENTE'}</text>
            {words.slice(0, 3).map((w, i) => <text key={i} x={h.x + 12} y={h.y + 43 + i * 18} fontSize="12" fill={h.esFinal ? 'white' : '#233b35'}>{w}{i === 2 && words.length > 3 ? '…' : ''}</text>)}
          </g>;
        })}
      </svg>
    </div>
    <details className="chart-table">
      <summary>Ver hechos y relaciones como tabla</summary>
      <div className="table-wrap">
        <table>
          <caption>Relaciones causales</caption>
          <thead><tr><th scope="col">Antecedente</th><th scope="col">Consecuencia</th></tr></thead>
          <tbody>{relaciones.map(r => <tr key={r.id}><td>{hechos.find(h => h.id === r.antecedenteId)?.descripcion}</td><td>{hechos.find(h => h.id === r.consecuenciaId)?.descripcion}</td></tr>)}</tbody>
        </table>
      </div>
    </details>
  </>;
}
