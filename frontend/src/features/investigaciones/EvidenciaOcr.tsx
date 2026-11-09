import { useRef, useState } from 'react';
import { request, descargar } from '../../api/client';
import { Field, Select, TextArea, Message } from '../../components/common/UI';

interface Imagen { id: string; nombre: string; tipo: string }
interface Lectura { texto: string; confianza: number }
export function EvidenciaOcr({ id, casoId, evidencias, onSaved }: {
  id: string; casoId: string; evidencias: Imagen[]; onSaved: () => void;
}) {
  const imagenes = evidencias.filter(e => ['image/png', 'image/jpeg'].includes(e.tipo));
  const [vistaPrevia, setVistaPrevia] = useState(false);
  const [fuente, setFuente] = useState(''), [titulo, setTitulo] = useState('Transcripción de evidencia');
  const [lectura, setLectura] = useState<Lectura | null>(null), [texto, setTexto] = useState('');
  const [busy, setBusy] = useState(false), [error, setError] = useState(''), [success, setSuccess] = useState('');
  const [documento, setDocumento] = useState<{ id: string; nombre: string } | null>(null);
  const archivoTemporal = useRef<File | null>(null);
  const enProceso = useRef(false);
  const [estado, setEstado] = useState('');
  const [nombreFuente, setNombreFuente] = useState('');
  const base = `/investigaciones/${id}/evidencias/${fuente}`;
  async function escanear(fuenteId: string) {
    setEstado('Reconociendo el texto de la imagen…');
    const form = new FormData();
    if (archivoTemporal.current) form.append('archivo', archivoTemporal.current);
    const result = archivoTemporal.current
      ? await request<Lectura>(`/investigaciones/${id}/ocr/escanear`, { method: 'POST', body: form })
      : await request<Lectura>(`/investigaciones/${id}/evidencias/${fuenteId}/escanear`, { method: 'POST' });
    setLectura(result); setTexto(result.texto);
    setSuccess('Lectura terminada. Revisa y corrige el texto antes de crear el documento.');
  }
  async function procesar(archivo?: File, existente?: Imagen) {
    if (enProceso.current) return;
    setVistaPrevia(false); enProceso.current = true; setBusy(true); setError(''); setSuccess(''); setDocumento(null); setLectura(null); setTexto('');
    try {
      let fuenteId = existente?.id || fuente;
      if (archivo) {
        setFuente(''); setNombreFuente('');
        if (!/\.(png|jpe?g)$/i.test(archivo.name) || archivo.size === 0 || archivo.size > 5 * 1024 * 1024) throw new Error('Selecciona una imagen PNG o JPG de hasta 5 MB.');
        archivoTemporal.current = archivo; fuenteId = 'temporal'; setFuente(fuenteId); setNombreFuente(archivo.name);
      } else if (existente) { archivoTemporal.current = null; setFuente(existente.id); setNombreFuente(existente.nombre); }
      if (!fuenteId) throw new Error('Selecciona una imagen para escanear.');
      await escanear(fuenteId);
    } catch (e) { setError((e as Error).message); }
    finally { enProceso.current = false; setBusy(false); setEstado(''); }
  }
  return <div className="measure-form">
    <h3>Cargar imagen y transcribir con IA</h3>
    <p className="help">Selecciona una imagen y comenzará el reconocimiento automáticamente. El texto impreso en español se procesa en el servidor del proyecto. Después podrás revisarlo y guardar un Word.</p>
    <Field label="Imagen para escanear" type="file" accept=".png,.jpg,.jpeg" disabled={busy} help="PNG o JPG de hasta 5 MB. La imagen solo se usa temporalmente para reconocer el texto. Únicamente se guarda el Word que confirmes." onChange={e => { const archivo = e.target.files?.[0]; e.target.value = ''; if (archivo) void procesar(archivo); }}/>
    {imagenes.length > 0 && <details><summary>Usar una imagen ya adjunta</summary><Select label="Imagen existente" value={imagenes.some(i => i.id === fuente) ? fuente : ''} disabled={busy} onChange={e => { const imagen = imagenes.find(i => i.id === e.target.value); if (imagen) void procesar(undefined, imagen); }}><option value="">Selecciona para escanear automáticamente</option>{imagenes.map(e => <option key={e.id} value={e.id}>{e.nombre}</option>)}</Select></details>}
    <Message error={error} success={success}/>
    <p role="status" className="help">{busy ? estado || 'Guardando documento…' : nombreFuente ? `Imagen seleccionada: ${nombreFuente}` : 'Elige una imagen para comenzar.'}</p>
    {error && fuente && !lectura && <button type="button" className="secondary" disabled={busy} onClick={() => void procesar()}>Reintentar escaneo</button>}
      {lectura && <form onSubmit={async e => {
        e.preventDefault(); if (!vistaPrevia) { setVistaPrevia(true); return; } setBusy(true); setError(''); setSuccess('');
        try {
          const saved = await request<{ id: string; nombre: string }>(archivoTemporal.current ? `/investigaciones/${id}/ocr/documento` : `${base}/documento`, {
            method: 'POST', body: JSON.stringify({ titulo, texto, nombreImagen: nombreFuente })
          });
          setDocumento(saved); setLectura(null); setVistaPrevia(false); onSaved();
          setSuccess('Documento Word guardado en las evidencias de esta investigación. La imagen temporal no se guarda como evidencia.');
        } catch (e) { setError((e as Error).message); } finally { setBusy(false); }
      }}>
        <p className="help">Confianza estimada del reconocimiento: {Math.round(lectura.confianza)} %. Es una estimación del modelo, no una garantía de exactitud.</p>
        <Field label="Título del documento" value={titulo} required minLength={3} maxLength={120} disabled={busy} onChange={e => { setTitulo(e.target.value); setVistaPrevia(false); }}/>
        <TextArea label="Texto reconocido para revisar y corregir" value={texto} required maxLength={30000} rows={12} disabled={busy} onChange={e => { setTexto(e.target.value); setVistaPrevia(false); }}/>
        <p className="help">Al guardar confirmas que revisaste el texto. No se modificarán el relato ni los hechos de la investigación.</p>
        {vistaPrevia && <section className="ocr-preview" aria-label="Vista previa del documento"><div className="ocr-preview-toolbar"><h3>Vista previa del Word</h3><span>Revisa el contenido antes de confirmar</span></div><article className="ocr-preview-page"><h2>{titulo.trim()}</h2><p className="ocr-preview-subtitle">Transcripción de evidencia · GIATEP</p><p className="ocr-preview-meta">Imagen utilizada: {nombreFuente}{archivoTemporal.current ? '. Procesada temporalmente; no almacenada como evidencia.' : ''}</p><p className="ocr-preview-meta">Texto obtenido mediante OCR local y revisado por el usuario. No constituye una conclusión de la investigación.</p><div className="ocr-preview-text">{texto.trim()}</div></article><p className="help">La distribución de páginas puede variar al abrir el archivo en Word. El documento incluirá el autor y la fecha de guardado.</p></section>}
        <div className="action-row">{vistaPrevia && <button type="button" className="secondary" disabled={busy} onClick={() => setVistaPrevia(false)}>Volver a editar</button>}<button className="primary" disabled={busy || !texto.trim()}>{busy ? 'Guardando…' : vistaPrevia ? 'Confirmar y guardar Word' : 'Ver vista previa del Word'}</button></div>
      </form>}
      {documento && <button type="button" className="secondary" onClick={async () => {
        try { await descargar(`/casos/${casoId}/evidencias/${documento.id}`, documento.nombre); }
        catch (e) { setError((e as Error).message); }
      }}>Descargar documento Word</button>}
  </div>;
}
