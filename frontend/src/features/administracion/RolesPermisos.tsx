import { useId } from 'react';
import { Check, ShieldCheck, ClipboardList, Building2 } from 'lucide-react';
import { Select } from '../../components/common/UI';

export const perfiles = [
  {
    value: 'Administrador', nombre: 'Administrador', icono: ShieldCheck,
    descripcion: 'Organiza las cuentas y los registros del sistema.',
    alcance: 'Todos los establecimientos.',
    permisos: ['Crear y editar usuarios y asignar sus roles', 'Administrar establecimientos y trabajadores', 'Consultar la auditoría de cambios', 'Consultar indicadores y exportar reportes'],
    limite: 'Las investigaciones y la revisión de medidas corresponden a Prevención.'
  },
  {
    value: 'Prevencion', nombre: 'Prevención de Riesgos', icono: ClipboardList,
    descripcion: 'Registra casos, investiga y verifica las medidas.',
    alcance: 'Todos los establecimientos.',
    permisos: ['Registrar casos y gestionar sus antecedentes', 'Realizar investigaciones y escanear evidencias', 'Definir planes, revisar medidas y cerrar casos', 'Consultar indicadores y exportar reportes'],
    limite: 'La administración de cuentas y establecimientos corresponde al Administrador.'
  },
  {
    value: 'Jefatura', nombre: 'Jefatura', icono: Building2,
    descripcion: 'Consulta su centro y entrega sus medidas asignadas.',
    alcance: 'Solo el establecimiento asignado.',
    permisos: ['Consultar los casos y las investigaciones de su centro', 'Agregar observaciones en casos abiertos de su centro', 'Entregar evidencia de sus propias medidas asignadas', 'Consultar indicadores y reportes de su centro'],
    limite: 'Prevención registra los casos y verifica el cumplimiento de las medidas.'
  }
] as const;

export function nombreRol(value?: string) { return perfiles.find(p => p.value === value)?.nombre ?? value; }

export function RolesPermisos({ value, onChange, disabled, centros, establecimientoId }: {
  value: string; onChange: (value: string) => void; disabled: boolean;
  centros: { id: string; nombre?: string; activo?: boolean }[]; establecimientoId?: string;
}) {
  const id = useId();
  const perfil = perfiles.find(p => p.value === value) ?? perfiles[1];
  return <section className="roles-permisos">
    <fieldset className="role-picker" disabled={disabled} aria-describedby={`${id}-ayuda`}>
      <legend>Rol y permisos</legend>
      <p id={`${id}-ayuda`} className="help">Selecciona un perfil. Cada uno incluye los permisos necesarios para su función.</p>
      <div className="role-options">
        {perfiles.map(p => <label key={p.value} className={`role-option${value === p.value ? ' selected' : ''}`}>
          <input type="radio" name="rol" value={p.value} checked={value === p.value} required
            aria-describedby={`${id}-${p.value}-descripcion`} onChange={() => onChange(p.value)}/>
          <span><span className="role-option-title"><p.icono size={20} aria-hidden="true"/><strong>{p.nombre}</strong></span>
            <span id={`${id}-${p.value}-descripcion`} className="role-option-description">{p.descripcion}</span>
          </span>
        </label>)}
      </div>
    </fieldset>
    <div className="role-summary" aria-live="polite" aria-atomic="true">
      <h3>Permisos incluidos · {perfil.nombre}</h3>
      <ul>{perfil.permisos.map(p => <li key={p}><Check size={17} aria-hidden="true"/><span>{p}</span></li>)}</ul>
      <p><strong>Acceso:</strong> {perfil.alcance}</p>
      <p className="help">{perfil.limite}</p>
    </div>
    {value === 'Jefatura' && <Select label="Establecimiento asignado" name="establecimientoId" required
      disabled={disabled} defaultValue={establecimientoId || ''}>
      <option value="">Selecciona un establecimiento</option>
      {centros.filter(c => c.activo).map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
    </Select>}
  </section>;
}
