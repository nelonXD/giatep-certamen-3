import { readOcrDraft, writeOcrDraft } from '../../hooks/ocrDraft';
import { useAuth } from '../../context/AuthContext';
import { useEffect, useRef, useState } from 'react';
import { request, descargar, TOKEN_KEY } from '../../api/client';
import { Field, Select, TextArea, Message } from '../../components/common/UI';

interface Imagen { id: string; nombre: string; tipo: string }
interface Linea { texto: string; x: number; y: number; ancho: number; alto: number }
interface Distribucion { ancho: number; alto: number; lineas: Linea[] }
interface Recorte { nombre: string; x: number; y: number; ancho: number; alto: number }
interface Lectura { texto: string; confianza: number; distribucion?: Distribucion }
interface BorradorOcr { recortes?: Recorte[]; archivo: File | null; imagen: Blob | null; fuente: string; nombreFuente: string; titulo: string; lectura: Lectura | null; texto: string; distribucion: Distribucion | null; conservarDistribucion: boolean; vistaPrevia: boolean }
// Measure the complete line so its final characters remain inside the OCR region.
function tamanoLinea(line: Linea, anchoPagina: number) {
  const context = document.createElement('canvas').getContext('2d');
  if (context) context.font = '100px Arial';
  const anchoTexto = context ? context.measureText(line.texto || ' ').width / 100 : Math.max(1, line.texto.length * .55);
  const size = Math.min(line.alto * .95, line.ancho / Math.max(1, anchoTexto) * .96);
  return `${100 * size / anchoPagina}cqw`;
}
export function EvidenciaOcr({ id, casoId, evidencias, onSaved }: {
  id: string; casoId: string; evidencias: Imagen[]; onSaved: () => void;
}) {
  const [recortes, setRecortes] = useState<Recorte[]>([]);
  const [seleccionando, setSeleccionando] = useState(false);
  const inicioRecorte = useRef<{ x: number; y: number } | null>(null);
  const [seleccion, setSeleccion] = useState<Recorte | null>(null);
  const usuario = useAuth();
  const draftKey = `${usuario.id}:${id}`;
  const [borradorListo, setBorradorListo] = useState(false);
  const [avisoBorrador, setAvisoBorrador] = useState('');
  const imagenBorrador = useRef<Blob | null>(null);
  const imagenes = evidencias.filter(e => ['image/png', 'image/jpeg'].includes(e.tipo));
  const [distribucion, setDistribucion] = useState<Distribucion | null>(null);
  const [conservarDistribucion, setConservarDistribucion] = useState(true);
  const [vistaPrevia, setVistaPrevia] = useState(false);
  const [fuente, setFuente] = useState(''), [titulo, setTitulo] = useState('Transcripción de evidencia');
  const [lectura, setLectura] = useState<Lectura | null>(null), [texto, setTexto] = useState('');
  const [busy, setBusy] = useState(false), [error, setError] = useState(''), [success, setSuccess] = useState('');
  const [documento, setDocumento] = useState<{ id: string; nombre: string } | null>(null);
  const archivoTemporal = useRef<File | null>(null);
  const enProceso = useRef(false);
  const [estado, setEstado] = useState('');
  const [nombreFuente, setNombreFuente] = useState('');
  const [imagenUrl, setImagenUrl] = useState('');
  useEffect(() => () => { if (imagenUrl) URL.revokeObjectURL(imagenUrl); }, [imagenUrl]);
  useEffect(() => {
    let cancelled = false;
    readOcrDraft<BorradorOcr>(draftKey).then(draft => {
      if (cancelled || !draft) return;
      archivoTemporal.current = draft.archivo; imagenBorrador.current = draft.imagen || draft.archivo;
      setRecortes(draft.recortes || []); setFuente(draft.fuente); setNombreFuente(draft.nombreFuente); setTitulo(draft.titulo);
      setLectura(draft.lectura); setTexto(draft.texto); setDistribucion(draft.distribucion);
      setConservarDistribucion(draft.conservarDistribucion); setVistaPrevia(draft.vistaPrevia);
      if (imagenBorrador.current) setImagenUrl(URL.createObjectURL(imagenBorrador.current));
      setAvisoBorrador('Borrador recuperado en este navegador. Puedes continuar donde lo dejaste.');
      if (!draft.lectura) setError('El escaneo no había terminado. Pulsa Reintentar escaneo para continuar.');
    }).catch(() => { if (!cancelled) setAvisoBorrador('No se pudo recuperar el borrador local.'); })
      .finally(() => { if (!cancelled) setBorradorListo(true); });
    return () => { cancelled = true; };
  }, [draftKey]);
  useEffect(() => {
    if (!borradorListo) return;
    if (documento) { void writeOcrDraft(draftKey, null).catch(() => setAvisoBorrador('El documento se guardó, pero no se pudo borrar el borrador local.')); return; }
    if (!fuente) return;
    const draft: BorradorOcr = { recortes, archivo: archivoTemporal.current, imagen: imagenBorrador.current, fuente, nombreFuente, titulo, lectura, texto, distribucion, conservarDistribucion, vistaPrevia };
    void writeOcrDraft(draftKey, draft).catch(() => setAvisoBorrador('No se pudo guardar el borrador en este navegador. Evita refrescar hasta confirmar el Word.'));
  }, [borradorListo, draftKey, fuente, nombreFuente, titulo, lectura, texto, distribucion, conservarDistribucion, vistaPrevia, documento, imagenUrl, recortes]);
  const base = `/investigaciones/${id}/evidencias/${fuente}`;
  async function escanear(fuenteId: string) {
    setEstado('Reconociendo el texto de la imagen…');
    const form = new FormData();
    if (archivoTemporal.current) form.append('archivo', archivoTemporal.current);
    const result = archivoTemporal.current
      ? await request<Lectura>(`/investigaciones/${id}/ocr/escanear`, { method: 'POST', body: form })
      : await request<Lectura>(`/investigaciones/${id}/evidencias/${fuenteId}/escanear`, { method: 'POST' });
    setLectura(result); setTexto(result.texto); setDistribucion(result.distribucion || null); setConservarDistribucion(!!result.distribucion);
    setSuccess('Lectura terminada. Revisa y corrige el texto antes de crear el documento.');
  }
  async function procesar(archivo?: File, existente?: Imagen) {
    if (enProceso.current) return;
    setVistaPrevia(false); enProceso.current = true; setBusy(true); setError(''); setSuccess(''); setDocumento(null); setLectura(null); setDistribucion(null); setTexto('');
    try {
      let fuenteId = existente?.id || fuente;
      if (archivo) {
        setFuente(''); setNombreFuente(''); setRecortes([]);
        if (!/\.(png|jpe?g)$/i.test(archivo.name) || archivo.size === 0 || archivo.size > 1_000_000) throw new Error('Selecciona una imagen PNG o JPG de hasta 1 MB.');
        imagenBorrador.current = archivo; setImagenUrl(URL.createObjectURL(archivo)); archivoTemporal.current = archivo; fuenteId = 'temporal'; setFuente(fuenteId); setNombreFuente(archivo.name);
       } else if (existente) {
        setRecortes([]); setImagenUrl(''); archivoTemporal.current = null; setFuente(existente.id); setNombreFuente(existente.nombre);
        const response = await fetch(`/api/v1/casos/${casoId}/evidencias/${existente.id}`, { headers: { Authorization: `Bearer ${sessionStorage.getItem(TOKEN_KEY) || ''}` } });
        if (!response.ok) throw new Error('No se pudo cargar la imagen original.');
        const imagen = await response.blob(); imagenBorrador.current = imagen; setImagenUrl(URL.createObjectURL(imagen));
      }
      if (!fuenteId) throw new Error('Selecciona una imagen para escanear.');
      await escanear(fuenteId);
    } catch (e) { setError((e as Error).message); }
    finally { enProceso.current = false; setBusy(false); setEstado(''); }
  }
  return <div className="measure-form">
    <h3>Cargar imagen y transcribir con IA</h3>
    <p className="help">Selecciona una imagen y comenzará el reconocimiento automáticamente. La imagen se envía a un proveedor externo de OCR para reconocer el texto en español. Utiliza documentos de prueba sintéticos. Después podrás revisarlo y guardar un Word.</p>
    <Field label="Imagen para escanear" type="file" accept=".png,.jpg,.jpeg" disabled={busy || !borradorListo} help="PNG o JPG de hasta 1 MB. Al confirmar se guardan la imagen original y el Word revisado, vinculados en la investigación." onChange={e => { const archivo = e.target.files?.[0]; e.target.value = ''; if (archivo) void procesar(archivo); }}/>
    {imagenes.length > 0 && <details><summary>Usar una imagen ya adjunta</summary><Select label="Imagen existente" value={imagenes.some(i => i.id === fuente) ? fuente : ''} disabled={busy || !borradorListo} onChange={e => { const imagen = imagenes.find(i => i.id === e.target.value); if (imagen) void procesar(undefined, imagen); }}><option value="">Selecciona para escanear automáticamente</option>{imagenes.map(e => <option key={e.id} value={e.id}>{e.nombre} (adjunta)</option>)}</Select></details>}
    <p className="help" role="status">{!borradorListo ? 'Recuperando borrador…' : avisoBorrador || 'La imagen y las correcciones se guardan como borrador en este navegador hasta confirmar el Word.'}</p>
    {fuente && !busy && <button type="button" className="secondary" onClick={async () => {
      try { await writeOcrDraft(draftKey, null); archivoTemporal.current = null; imagenBorrador.current = null; setFuente(''); setNombreFuente(''); setImagenUrl(''); setLectura(null); setTexto(''); setDistribucion(null); setVistaPrevia(false); setDocumento(null); setRecortes([]); setError(''); setSuccess(''); setTitulo('Transcripción de evidencia'); setAvisoBorrador('Borrador descartado.'); }
      catch { setError('No se pudo descartar el borrador. Intenta nuevamente.'); }
    }}>Descartar borrador</button>}
    <Message error={error} success={success}/>
    <p role="status" className="help">{busy ? estado || 'Guardando documento…' : nombreFuente ? `Imagen seleccionada: ${nombreFuente}` : 'Elige una imagen para comenzar.'}</p>
    {error && fuente && !lectura && <button type="button" className="secondary" disabled={busy || !borradorListo} onClick={() => void procesar()}>Reintentar escaneo</button>}
      {lectura && <form onSubmit={async e => {
        e.preventDefault(); if (!vistaPrevia) { setVistaPrevia(true); return; } setBusy(true); setError(''); setSuccess('');
        try {
          const original = archivoTemporal.current;
          const datos = new FormData();
          if (original) { datos.append('archivo', original); datos.append('titulo', titulo); datos.append('texto', texto); if (conservarDistribucion && distribucion) { datos.append('distribucion', JSON.stringify(distribucion)); datos.append('recortes', JSON.stringify(recortes)); } }
          const saved = await request<{ id: string; nombre: string }>(archivoTemporal.current ? `/investigaciones/${id}/ocr/documento-con-original` : `${base}/documento`, {
            method: 'POST', body: original ? datos : JSON.stringify({ titulo, texto, distribucion: conservarDistribucion ? distribucion : null, recortes: conservarDistribucion ? recortes : [] })
          });
          setDocumento(saved); setLectura(null); setVistaPrevia(false); onSaved();
          setSuccess('Original y transcripción Word vinculados en las evidencias de esta investigación.');
        } catch (e) { setError((e as Error).message); } finally { setBusy(false); }
      }}>
        <p className="help">{lectura.confianza > 0 ? `Confianza estimada: ${Math.round(lectura.confianza)} %. Revisa el texto antes de guardar.` : 'El proveedor no informa confianza. Revisa el texto antes de guardar.'}</p>
        <Field label="Título del documento" value={titulo} required minLength={3} maxLength={120} disabled={busy || !borradorListo} onChange={e => { setTitulo(e.target.value); setVistaPrevia(false); }}/>
        {distribucion && <label className="ocr-layout-option"><input type="checkbox" checked={conservarDistribucion} disabled={busy || !borradorListo} onChange={e => { setConservarDistribucion(e.target.checked); setVistaPrevia(false); }}/> Conservar distribución original del texto (aproximada)</label>}
        {conservarDistribucion && distribucion && <p className="help">Corrige cada línea sin cambiar su posición. Los cambios largos pueden ocupar más espacio. Puedes añadir recortes de logos, firmas, sellos y QR desde la imagen original.</p>}
        <div className="ocr-comparison">
          <section aria-label="Imagen original"><h4>Imagen original</h4>{imagenUrl && <div className={`ocr-crop-source${seleccionando ? ' selecting' : ''}`} onPointerDown={e => {
              if (!seleccionando || !distribucion) return;
              const box=e.currentTarget.getBoundingClientRect(); inicioRecorte.current={x:Math.max(0,Math.min(1,(e.clientX-box.left)/box.width)),y:Math.max(0,Math.min(1,(e.clientY-box.top)/box.height))}; e.currentTarget.setPointerCapture(e.pointerId);
            }} onPointerMove={e => {
              if (!inicioRecorte.current) return; const box=e.currentTarget.getBoundingClientRect(); const x=Math.max(0,Math.min(1,(e.clientX-box.left)/box.width)), y=Math.max(0,Math.min(1,(e.clientY-box.top)/box.height)); const start=inicioRecorte.current;
              setSeleccion({nombre:`Elemento ${recortes.length+1}`,x:Math.min(x,start.x),y:Math.min(y,start.y),ancho:Math.abs(x-start.x),alto:Math.abs(y-start.y)});
            }} onPointerUp={() => { if (inicioRecorte.current && seleccion && seleccion.ancho>.005 && seleccion.alto>.005 && recortes.length<12) {setRecortes([...recortes,seleccion]);setVistaPrevia(false);} inicioRecorte.current=null;setSeleccion(null);setSeleccionando(false); }} onPointerCancel={() => {inicioRecorte.current=null;setSeleccion(null);setSeleccionando(false);}}>
              <img className="ocr-original-image" src={imagenUrl} draggable={false} alt="Documento original para comparar con la transcripción"/>
              {[...recortes,...(seleccion?[seleccion]:[])].map((r,i)=><div key={i} className="ocr-crop-box" style={{left:`${r.x*100}%`,top:`${r.y*100}%`,width:`${r.ancho*100}%`,height:`${r.alto*100}%`}}><span>{i+1}</span></div>)}
            </div>}
            {imagenUrl && conservarDistribucion && distribucion && <><div className="action-row"><button type="button" className="secondary" disabled={busy || recortes.length>=12} aria-pressed={seleccionando} onClick={()=>setSeleccionando(!seleccionando)}>{seleccionando?'Cancelar selección':'Seleccionar logo, firma, sello o QR'}</button><button type="button" className="secondary" disabled={busy || recortes.length>=12} onClick={()=>{setRecortes([...recortes,{nombre:`Elemento ${recortes.length+1}`,x:.1,y:.1,ancho:.2,alto:.1}]);setVistaPrevia(false);}}>Añadir recorte con coordenadas</button></div><p className="help">Marca un rectángulo sobre la imagen o ajusta sus coordenadas. Se conservará el fondo y cualquier texto superpuesto.</p></>}<p className="help">El original conserva todos sus elementos. Los recortes marcados se incluirán en el Word.</p></section>
          <div>{conservarDistribucion && distribucion ? <section aria-label="Texto por posición"><h4>Texto en su posición original</h4><div className="ocr-layout-canvas" style={{ aspectRatio: `${distribucion.ancho} / ${distribucion.alto}` }}>{recortes.map((r,i)=><div className="ocr-crop-preview" key={`crop-${i}`} style={{left:`${r.x*100}%`,top:`${r.y*100}%`,width:`${r.ancho*100}%`,height:`${r.alto*100}%`}}><img src={imagenUrl} alt={r.nombre} style={{width:`${100/r.ancho}%`,height:`${100/r.alto}%`,left:`${-100*r.x/r.ancho}%`,top:`${-100*r.y/r.alto}%`}}/></div>)}{distribucion.lineas.map((line, index) => <input key={index} aria-label={`Línea ${index + 1}`} value={line.texto} disabled={busy || !borradorListo} maxLength={2000} style={{ left: `${100 * line.x / distribucion.ancho}%`, top: `${100 * line.y / distribucion.alto}%`, width: `${100 * line.ancho / distribucion.ancho}%`, height: `${100 * line.alto / distribucion.alto}%`, fontSize: tamanoLinea(line, distribucion.ancho) }} onChange={e => {
              const lineas = distribucion.lineas.map((l, i) => i === index ? { ...l, texto: e.target.value } : l);
              setDistribucion({ ...distribucion, lineas }); setTexto(lineas.map(l => l.texto).join('\n')); setVistaPrevia(false);
            }}/>)}</div></section> : <TextArea label="Texto reconocido para revisar y corregir" value={texto} required maxLength={30000} rows={12} disabled={busy || !borradorListo} onChange={e => { setTexto(e.target.value); setDistribucion(null); setVistaPrevia(false); }}/>}</div></div>
        {recortes.length>0 && <fieldset className="ocr-crop-list"><legend>Elementos gráficos seleccionados</legend>{recortes.map((r,i)=><div className="ocr-crop-row" key={i}><Field label={`Nombre del recorte ${i+1}`} value={r.nombre} maxLength={80} disabled={busy} onChange={e=>{setRecortes(recortes.map((v,j)=>j===i?{...v,nombre:e.target.value}:v));setVistaPrevia(false);}}/>{(['x','y','ancho','alto'] as const).map(key=><Field key={key} label={`${key} (%) del recorte ${i+1}`} type="number" step="0.1" min={key==='ancho'||key==='alto'?0.1:0} max={100} value={Number((r[key]*100).toFixed(1))} disabled={busy} onChange={e=>{const value=Math.max((key==='ancho'||key==='alto') ? .001 : 0,Math.min(1,Number(e.target.value)/100)); const next={...r,[key]:value};next.x=Math.min(next.x,1-next.ancho);next.y=Math.min(next.y,1-next.alto);setRecortes(recortes.map((v,j)=>j===i?next:v));setVistaPrevia(false);}}/>)}<button type="button" className="secondary" disabled={busy} onClick={()=>{setRecortes(recortes.filter((_,j)=>j!==i));setVistaPrevia(false);}}>Quitar recorte {i+1}</button></div>)}</fieldset>}
        <p className="help">Al guardar confirmas que revisaste el texto. No se modificarán el relato ni los hechos de la investigación.</p>
        {vistaPrevia && <section className="ocr-preview" aria-label="Vista previa del documento"><div className="ocr-preview-toolbar"><h3>Vista previa del Word</h3><span>Revisa el contenido antes de confirmar</span></div><article className="ocr-preview-page"><h2>{titulo.trim()}</h2><p className="ocr-preview-subtitle">Transcripción de evidencia · GIATEP</p><p className="ocr-preview-meta">Imagen utilizada: {nombreFuente}. Se conserva como evidencia original vinculada.</p><p className="ocr-preview-meta">Texto obtenido mediante OCR y revisado por el usuario. No constituye una conclusión de la investigación.</p>{conservarDistribucion && distribucion ? <div className="ocr-layout-canvas" style={{ aspectRatio: `${distribucion.ancho} / ${distribucion.alto}` }}>{recortes.map((r,i)=><div className="ocr-crop-preview" key={`crop-${i}`} style={{left:`${r.x*100}%`,top:`${r.y*100}%`,width:`${r.ancho*100}%`,height:`${r.alto*100}%`}}><img src={imagenUrl} alt={r.nombre} style={{width:`${100/r.ancho}%`,height:`${100/r.alto}%`,left:`${-100*r.x/r.ancho}%`,top:`${-100*r.y/r.alto}%`}}/></div>)}{distribucion.lineas.map((line, i) => <span key={i} style={{ left: `${100*line.x/distribucion.ancho}%`, top: `${100*line.y/distribucion.alto}%`, width: `${100*line.ancho/distribucion.ancho}%`, fontSize: tamanoLinea(line, distribucion.ancho) }}>{line.texto}</span>)}</div> : <div className="ocr-preview-text">{texto.trim()}</div>}</article><p className="help">La distribución de páginas puede variar al abrir el archivo en Word. El documento incluirá el autor y la fecha de guardado.</p></section>}
        <div className="action-row">{vistaPrevia && <button type="button" className="secondary" disabled={busy || !borradorListo} onClick={() => setVistaPrevia(false)}>Volver a editar</button>}<button className="primary" disabled={busy || !texto.trim()}>{busy ? 'Guardando…' : vistaPrevia ? 'Confirmar y guardar Word' : 'Ver vista previa del Word'}</button></div>
      </form>}
      {documento && <button type="button" className="secondary" onClick={async () => {
        try { await descargar(`/casos/${casoId}/evidencias/${documento.id}`, documento.nombre); }
        catch (e) { setError((e as Error).message); }
      }}>Descargar documento Word</button>}
  </div>;
}
