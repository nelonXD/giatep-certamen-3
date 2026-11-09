import { useEffect, useRef, useState } from 'react';
import { Check, LockKeyhole, Save, X } from 'lucide-react';
import { Field, Select, Message } from '../../components/common/UI';
import { request } from '../../api/client';
import { RolesPermisos } from './RolesPermisos';
import type { RegistroAdmin, SeccionAdmin } from './admin.types';

export function AdminForm({ seccion, registro, centros, onCancel, onSaved, onBusy }: {
  seccion: SeccionAdmin; registro: RegistroAdmin; centros: RegistroAdmin[];
  onCancel: () => void; onSaved: () => void; onBusy: (busy: boolean) => void;
}) {
  const [rol, setRol] = useState(registro.rol || 'Prevencion');
  const [busy, setBusy] = useState(false), [error, setError] = useState('');
  const form = useRef<HTMLFormElement>(null);
  const nombre = seccion === 'usuarios' ? 'usuario' : seccion === 'trabajadores' ? 'trabajador' : 'establecimiento';
  useEffect(() => { form.current?.scrollIntoView({ block: 'start', behavior: 'instant' }); form.current?.querySelector<HTMLInputElement>('input')?.focus(); }, []);
  return <form ref={form} className="panel admin-editor" aria-label={`${registro.id ? 'Editar' : 'Crear'} ${nombre}`} onSubmit={async e => {
    e.preventDefault();
    const data = new FormData(e.currentTarget), activo = data.get('activo') === 'true';
    if (registro.id && registro.activo && !activo && !window.confirm(`¿Desactivar este ${nombre}? Su historial se conservará.`)) return;
    const body: Record<string, unknown> = { nombre: data.get('nombre'), activo };
    if (seccion === 'usuarios') Object.assign(body, { cuenta: data.get('cuenta'), rol: data.get('rol'), establecimientoId: data.get('establecimientoId') || null, password: data.get('password') || null });
    if (seccion === 'trabajadores') Object.assign(body, { codigo: data.get('codigo'), cargo: data.get('cargo'), establecimientoId: data.get('establecimientoId') });
    if (seccion === 'establecimientos') body.direccion = data.get('direccion');
    setBusy(true); onBusy(true); setError('');
    try {
      await request(`/admin/${seccion}${registro.id ? `/${registro.id}` : ''}`, { method: registro.id ? 'PUT' : 'POST', body: JSON.stringify(body) });
      onSaved();
    } catch (e) { setError((e as Error).message); }
    finally { setBusy(false); onBusy(false); }
  }}>
    <div className="admin-editor-heading"><div><p className="eyebrow">{registro.id ? 'ACTUALIZAR REGISTRO' : 'NUEVO REGISTRO'}</p>
      <h2>{registro.id ? `Editar ${nombre}` : `Agregar ${nombre}`}</h2>
      <p>{registro.id ? registro.nombre : 'Completa los datos para incorporar un nuevo registro.'}</p></div>
      <button type="button" className="admin-icon-button" aria-label="Cerrar formulario" disabled={busy} onClick={onCancel}><X size={20}/></button>
    </div>
    <Message error={error}/>
    <fieldset className="admin-fields" disabled={busy}><legend className="sr-only">Datos del {nombre}</legend>
      <div className="admin-form-section"><h3>Datos {seccion === 'usuarios' ? 'de la cuenta' : seccion === 'trabajadores' ? 'del trabajador' : 'del establecimiento'}</h3>
        <p className="help">Los campos marcados con * son obligatorios.</p>
        <div className="form-grid">
          <Field label={seccion === 'usuarios' || seccion === 'trabajadores' ? 'Nombre completo' : 'Nombre del establecimiento'} name="nombre" required minLength={3} maxLength={120} defaultValue={registro.nombre}/>
          {seccion === 'usuarios' && <Field label="Nombre de usuario" name="cuenta" required minLength={3} maxLength={50} pattern="[a-zA-Z0-9._\-]+" help="Letras, números, puntos o guiones. Sin espacios." defaultValue={registro.cuenta}/>}
          {seccion === 'trabajadores' && <>
            <Field label="Código interno" name="codigo" required minLength={3} maxLength={30} defaultValue={registro.codigo}/>
            <Field label="Cargo" name="cargo" required minLength={3} maxLength={100} defaultValue={registro.cargo}/>
            <Select label="Establecimiento" name="establecimientoId" required defaultValue={registro.establecimientoId || ''}>
              <option value="">Selecciona un establecimiento</option>{centros.filter(c => c.activo).map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
            </Select>
          </>}
          {seccion === 'establecimientos' && <Field label="Dirección" name="direccion" required minLength={3} maxLength={200} defaultValue={registro.direccion}/>}
        </div>
      </div>
      {seccion === 'usuarios' && <RolesPermisos value={rol} onChange={setRol} disabled={busy} centros={centros} establecimientoId={registro.establecimientoId}/>}
      {seccion === 'usuarios' && <div className="admin-form-section admin-password"><h3><LockKeyhole size={17} aria-hidden="true"/>{registro.id ? 'Cambiar contraseña' : 'Contraseña de acceso'}</h3>
        <Field label={registro.id ? 'Nueva contraseña (opcional)' : 'Contraseña inicial'} name="password" type="password" autoComplete="new-password" required={!registro.id} minLength={12} maxLength={200}
          help={registro.id ? 'Deja este campo vacío para conservar la contraseña actual. Al guardar una nueva, también se quita el bloqueo de acceso.' : 'Utiliza al menos 12 caracteres.'}/>
      </div>}
      <fieldset className="admin-state"><legend>Estado del {nombre}</legend>
        <label><input type="radio" name="activo" value="true" defaultChecked={registro.activo !== false}/><span><strong>Activo</strong><small>{seccion === 'usuarios' ? 'Puede iniciar sesión y utilizar sus permisos.' : 'Disponible para su uso en el sistema.'}</small></span><Check size={18} aria-hidden="true"/></label>
        <label><input type="radio" name="activo" value="false" defaultChecked={registro.activo === false}/><span><strong>Inactivo</strong><small>Conserva sus datos y su historial.</small></span></label>
      </fieldset>
    </fieldset>
    <div className="admin-form-footer"><span>Los cambios se guardan en la auditoría.</span><div>
      <button type="button" className="secondary" disabled={busy} onClick={onCancel}>Cancelar</button>
      <button className="primary" disabled={busy}><Save size={17} aria-hidden="true"/>{busy ? 'Guardando…' : 'Guardar cambios'}</button>
    </div></div>
  </form>;
}
