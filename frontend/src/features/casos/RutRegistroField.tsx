import { useEffect, useId, useRef, useState } from 'react';
import { ApiError, request } from '../../api/client';
import { Field } from '../../components/common/UI';
export interface RegistroRut { id: string; establecimientoId?: string; activo: boolean; datos: Record<string, string> }
export const normalizarRut = (v: string) => v.trim().replace(/[.-]/g, '').toUpperCase();
export function rutValido(v: string) {
  const rut = normalizarRut(v); if (!/^\d{7,8}[\dK]$/.test(rut)) return false;
  let suma = 0, factor = 2; for (let i = rut.length - 2; i >= 0; i--) { suma += Number(rut[i]) * factor; factor = factor === 7 ? 2 : factor + 1; }
  const dv = 11 - suma % 11; return rut.at(-1) === (dv === 11 ? '0' : dv === 10 ? 'K' : String(dv));
}
interface Sugerencia { rut: string; nombre: string; activo: boolean }
export function RutRegistroField({ tipo, label, value, onChange, onFound, onBusy }: { tipo: 'trabajador' | 'empleador'; label: string; value: string; onChange: (value: string) => void; onFound: (registro: RegistroRut, rut: string) => void; onBusy: (busy: boolean) => void }) {
  const listaId = useId(), wrap = useRef<HTMLDivElement>(null);
  const [consultado, setConsultado] = useState(false), [mensaje, setMensaje] = useState(''), [sugerencias, setSugerencias] = useState<Sugerencia[]>([]), [prefijoResultados, setPrefijoResultados] = useState(''), [focused, setFocused] = useState(false), [activo, setActivo] = useState(-1);
  const callbacks = useRef({ onFound, onBusy }); callbacks.current = { onFound, onBusy };
  const rut = normalizarRut(value), abierto = focused && prefijoResultados === rut && sugerencias.length > 0;
  useEffect(() => {
    wrap.current?.querySelector("input")?.setCustomValidity(value && !rutValido(value) ? "Completa el RUT/RUN y su dígito verificador." : "");
    setActivo(-1); setSugerencias([]);
    if (!consultado || !/^\d{2,8}[\dK]?$/.test(rut) || rut.length > 9) { setMensaje(value && consultado ? 'Escribe al menos 2 dígitos para ver sugerencias.' : ''); callbacks.current.onBusy(false); return; }
    const controller = new AbortController(); callbacks.current.onBusy(true); setMensaje('Buscando coincidencias…');
    const timer = setTimeout(async () => {
      const recurso = tipo === 'trabajador' ? 'trabajadores' : 'empleadores';
      let items: Sugerencia[] = [];
      try {
        items = await request<Sugerencia[]>(`/${recurso}/sugerencias?rut=${encodeURIComponent(rut)}`, { signal: controller.signal });
        if (controller.signal.aborted) return;
        if (rutValido(value)) {
          const registro = await request<RegistroRut>(`/${recurso}/por-rut/${encodeURIComponent(rut)}`, { signal: controller.signal });
          if (controller.signal.aborted) return;
          callbacks.current.onFound(registro, rut); setSugerencias([]); setMensaje(registro.activo ? 'Registro encontrado. Se completaron sus datos; revísalos antes de guardar.' : 'El trabajador está inactivo. Debe activarse antes de registrar el caso.');
        } else {
          setSugerencias(items); setPrefijoResultados(rut); setMensaje(items.length ? `${items.length} coincidencias. Usa las flechas y Enter o elige un registro.` : 'Sin coincidencias. Puedes completar un RUT nuevo y registrar sus datos.');
        }
      } catch (error) {
        if (controller.signal.aborted) return;
        if (error instanceof ApiError && error.status === 404 && items.length) { setSugerencias(items); setPrefijoResultados(rut); setMensaje(`${items.length} coincidencias. Elige el RUT completo.`); }
        else setMensaje(error instanceof ApiError && error.status === 404 ? 'No está registrado. Completa sus datos: se registrará al guardar el caso.' : `No se pudo consultar el registro: ${(error as Error).message}`);
      } finally { if (!controller.signal.aborted) callbacks.current.onBusy(false); }
    }, 300);
    return () => { clearTimeout(timer); controller.abort(); };
  }, [value, rut, consultado, tipo]);
  function elegir(item: Sugerencia) { setSugerencias([]); setActivo(-1); setConsultado(true); onChange(item.rut); }
  return <div className="rut-lookup"><div className="rut-input-wrap" ref={wrap}><Field label={label} required value={value} maxLength={16} autoComplete="off" spellCheck={false} role="combobox" aria-label={label} aria-autocomplete="list" aria-haspopup="listbox" aria-expanded={abierto} aria-controls={listaId} aria-activedescendant={abierto && activo >= 0 ? `${listaId}-${activo}` : undefined} help="Escribe el RUT: las sugerencias aparecen mientras escribes. Si es nuevo, completa sus datos debajo." onFocus={() => setFocused(true)} onClick={() => setFocused(true)} onBlur={() => setFocused(false)} onChange={e => { const v = e.target.value; e.currentTarget.setCustomValidity(v && !rutValido(v) ? 'Completa el RUT/RUN y su dígito verificador.' : ''); setConsultado(true); setFocused(true); onChange(v); }} onKeyDown={e => {
    if ((e.key === 'ArrowDown' || e.key === 'ArrowUp') && sugerencias.length && prefijoResultados === rut) { e.preventDefault(); setFocused(true); setActivo(i => e.key === 'ArrowDown' ? (i + 1) % sugerencias.length : i <= 0 ? sugerencias.length - 1 : i - 1); }
    else if (e.key === 'Escape') { e.preventDefault(); setFocused(false); setActivo(-1); }
    else if (e.key === 'Enter') { e.preventDefault(); if (abierto) elegir(sugerencias[activo >= 0 ? activo : 0]); else if (!rutValido(value)) e.currentTarget.reportValidity(); }
  }}/><ul id={listaId} role="listbox" aria-label={`Sugerencias de ${tipo}`} className="rut-suggestions" hidden={!abierto}>{sugerencias.map((item, i) => <li key={item.rut} id={`${listaId}-${i}`} role="option" aria-selected={activo === i} onPointerDown={e => e.preventDefault()} onClick={() => elegir(item)}><strong>{item.rut.slice(0, -1).replace(/\B(?=(\d{3})+(?!\d))/g, '.')}–{item.rut.slice(-1)}</strong><span>{item.nombre}{!item.activo ? ' · Inactivo' : ''}</span></li>)}</ul></div><p className="help" role="status" aria-live="polite">{mensaje}</p></div>;
}
