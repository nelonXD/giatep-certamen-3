import { useEffect, useId, useRef } from 'react';
import type { Tipo } from '../../types/caso.types';
export function SeleccionTipoCaso({ elegir, cancelar }: { elegir: (tipo: Tipo) => void; cancelar: () => void }) {
  const ref = useRef<HTMLDialogElement>(null), origen = useRef<HTMLElement | null>(null), titulo = useId(), descripcion = useId();
  useEffect(() => { const dialog = ref.current!; if (!origen.current && document.activeElement instanceof HTMLElement) origen.current = document.activeElement; dialog.showModal(); return () => { if (dialog.open) dialog.close(); }; }, []);
  function cerrar() { ref.current?.close(); if (origen.current?.isConnected) origen.current.focus(); cancelar(); }
  function seleccionar(tipo: Tipo) { ref.current?.close(); elegir(tipo); }
  return <dialog ref={ref} className="case-type-dialog" aria-labelledby={titulo} aria-describedby={descripcion} onCancel={e => { e.preventDefault(); cerrar(); }}><h2 id={titulo}>¿Qué tipo de caso quieres crear?</h2><p id={descripcion}>Selecciona el evento para abrir el formulario correspondiente.</p><div className="case-type-actions"><button className="secondary" onClick={() => seleccionar('Accidente')}><strong>Accidente del trabajo</strong><span>DIAT · Accidente en el trabajo o de trayecto</span></button><button className="secondary" onClick={() => seleccionar('EnfermedadProfesional')}><strong>Enfermedad profesional</strong><span>DIEP · Molestias, síntomas y exposición laboral</span></button></div><button className="secondary" onClick={cerrar}>Cancelar</button></dialog>;
}
