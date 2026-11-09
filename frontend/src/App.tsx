import { PlanesPage } from './features/planes/PlanesPage';
import { useEffect, useRef, useState } from 'react';
import { ShieldCheck, LayoutDashboard, ClipboardList, FileBarChart, LogOut, Menu, LockKeyhole, ChevronDown, Bell } from 'lucide-react';
import { ApiError, request, TOKEN_KEY } from './api/client';
import { AuthContext } from './context/AuthContext';
import type { Usuario } from './context/AuthContext';
import { Field, Loading, Message } from './components/common/UI';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { ActividadPage } from './features/dashboard/ActividadPage';
import { InvestigacionesPage } from './features/investigaciones/InvestigacionesPage';
import { CasosPage } from './features/casos/CasosPage';
import { ReportesPage } from './features/reportes/ReportesPage';
import { AdminPage, seccionesAdmin } from './features/administracion/AdminPage';

export function App() {
  const [user, setUser] = useState<Usuario | null>(null), [ready, setReady] = useState(false), [error, setError] = useState(''), [busy, setBusy] = useState(false), [menu, setMenu] = useState(false);
  const [sessionError, setSessionError] = useState('');
  const [sessionAttempt, setSessionAttempt] = useState(0);
  const [mostrarRecuperacion, setMostrarRecuperacion] = useState(false);
  const [path, setPath] = useState(location.hash.slice(1) || '/dashboard'); const mainRef = useRef<HTMLElement>(null);
  useEffect(() => { if (!menu) return; const close = (event: KeyboardEvent) => { if (event.key === 'Escape') { setMenu(false); document.querySelector<HTMLButtonElement>('.mobile-toggle')?.focus(); } }; window.addEventListener('keydown', close); return () => window.removeEventListener('keydown', close); }, [menu]);
  useEffect(() => {
    let cancelled = false;
    const changed = () => { setPath(location.hash.slice(1) || '/dashboard'); setMenu(false); };
    const expired = () => { sessionStorage.removeItem(TOKEN_KEY); setUser(null); setSessionError(''); setError('Tu sesión expiró o dejó de ser válida. Inicia sesión nuevamente.'); };
    window.addEventListener('hashchange', changed);
    window.addEventListener('giatep:expired', expired);
    if (sessionStorage.getItem(TOKEN_KEY)) {
      setReady(false); setSessionError('');
      request<Usuario>('/auth/me').then(usuario => { if (!cancelled) setUser(usuario); }).catch(error => {
        if (cancelled) return;
        if (error instanceof ApiError && error.status === 401) expired();
        else setSessionError('No pudimos comprobar tu sesión. Tu acceso se conserva; vuelve a intentarlo cuando se restablezca la conexión.');
      }).finally(() => { if (!cancelled) setReady(true); });
    } else setReady(true);
    return () => { cancelled = true; window.removeEventListener('hashchange', changed); window.removeEventListener('giatep:expired', expired); };
  }, [sessionAttempt]);
  useEffect(() => { mainRef.current?.focus(); document.title = `${path.startsWith('/resumen') ? 'Actividad y alertas' : path.startsWith('/planes') ? 'Planes de acción' : path.startsWith('/investigaciones') ? 'Investigaciones' : path.startsWith('/casos') ? 'Casos' : path.startsWith('/reportes') ? 'Reportes' : path.startsWith('/admin') ? (seccionesAdmin.find(s => path === `/admin/${s.id}`)?.nombre || 'Administración') : path === '/password' ? 'Contraseña' : 'Dashboard'} · GIATEP`; }, [path, user]);
  const [submenus, setSubmenus] = useState<Record<string, boolean>>({});
  useEffect(() => { setSubmenus(prev => ({ ...prev, [path.startsWith('/casos') ? '/casos' : path.startsWith('/investigaciones') ? '/investigaciones' : path.startsWith('/planes') ? '/planes' : '']: true })); }, [path]);
  if (!ready) return <Loading/>;
  if (sessionError) return <main className="narrow"><h1>No pudimos conectar</h1><Message error={sessionError}/><button className="primary" onClick={() => setSessionAttempt(value => value + 1)}>Reintentar conexión</button></main>;
  if (!user) return <div className="login-page"><section className="login-story"><ShieldCheck size={50}/><p className="eyebrow">GIATEP · SALUD OCUPACIONAL</p><h1>Información clara.<br/>Prevención con seguimiento.</h1><p>Gestiona los casos, comprende sus causas y acompaña cada medida hasta su cumplimiento.</p><div className="login-facts"><span>01 · Registrar</span><span>02 · Investigar</span><span>03 · Prevenir</span></div></section><main className="login-box"><span className="brand login-brand"><ShieldCheck/> GIATEP</span><h2>Bienvenido a tu espacio</h2><p>Ingresa con tu cuenta institucional.</p><Message error={error}/><form onSubmit={async e => { e.preventDefault(); setBusy(true); setError(''); const form = new FormData(e.currentTarget); try { const result = await request<{ token: string; usuario: Usuario }>('/auth/login', { method: 'POST', body: JSON.stringify({ cuenta: form.get('cuenta'), password: form.get('password') }) }); sessionStorage.setItem(TOKEN_KEY, result.token); setUser(result.usuario); location.hash = '/dashboard'; } catch (e) { setError((e as Error).message); } finally { setBusy(false); } }}><Field label="Cuenta" name="cuenta" autoComplete="username" required maxLength={50}/><Field label="Contraseña" name="password" type="password" autoComplete="current-password" required maxLength={200}/><button className="primary" disabled={busy}>{busy ? 'Ingresando…' : 'Iniciar sesión'}</button></form><button type="button" className="login-recovery-link" aria-expanded={mostrarRecuperacion} aria-controls="login-recovery" onClick={() => setMostrarRecuperacion(!mostrarRecuperacion)}>¿Olvidaste tu contraseña?</button>{mostrarRecuperacion && <section className="login-recovery" id="login-recovery" aria-live="polite"><h3>¿Necesitas ayuda para entrar?</h3><p>Comunícate con el administrador del sistema para que restablezca tu contraseña.</p></section>}<p className="help">Las cuentas las administra el responsable del sistema. Utiliza datos sintéticos en este entorno académico.</p></main></div>;
  const links = [{ path: '/dashboard', label: 'Dashboard', icon: LayoutDashboard }, { path: '/resumen', label: 'Actividad y alertas', icon: Bell }, ...(user.rol !== 'Administrador' ? [{ path: '/casos', label: 'Gestión de casos', icon: ClipboardList }, { path: '/investigaciones', label: 'Investigaciones', icon: ClipboardList }, { path: '/planes', label: 'Planes de acción', icon: ClipboardList }] : []), { path: '/reportes', label: 'Reportes', icon: FileBarChart }];
  let page;
  if (path === '/dashboard') page = <DashboardPage/>;
  else if (path === '/resumen' || path === '/actividad') page = <ActividadPage/>;
  else if (path.startsWith('/planes')) page = user.rol === 'Administrador' ? <ErrorPage forbidden/> : <PlanesPage key={path} path={path}/>;
  else if (path.startsWith('/investigaciones')) page = user.rol === 'Administrador' ? <ErrorPage forbidden/> : <InvestigacionesPage key={path} path={path}/>;
  else if (path.startsWith('/casos')) page = user.rol === 'Administrador' ? <ErrorPage forbidden/> : <CasosPage key={path} path={path}/>;
  else if (path.startsWith('/reportes')) page = <ReportesPage key={path}/>;
  else if (path === '/admin' || path.startsWith('/admin/')) {
    const seccion = seccionesAdmin.find(s => path === `/admin/${s.id}` || path === '/admin' && s.id === 'usuarios');
    page = user.rol !== 'Administrador' ? <ErrorPage forbidden/> : seccion ? <AdminPage key={seccion.id} seccionId={seccion.id}/> : <ErrorPage/>;
  }
  else if (path === '/password') page = <PasswordPage onChanged={() => { sessionStorage.removeItem(TOKEN_KEY); setUser(null); setError('Contraseña actualizada. Ingresa con tu nueva contraseña.'); }}/>;
  else page = <ErrorPage/>;
  return <AuthContext.Provider value={user}><div className="app"><a className="skip" href="#contenido" onClick={e => { e.preventDefault(); mainRef.current?.focus(); }}>Saltar al contenido</a><aside className={`sidebar${user.rol === 'Administrador' ? ' admin-sidebar' : ''}`}><a className="brand" href="#/dashboard"><span className="brand-icon"><ShieldCheck size={26}/></span><span>GIATEP<small>Gestión preventiva</small></span></a><button className="mobile-toggle" aria-expanded={menu} aria-controls="navegacion" aria-label="Mostrar navegación" onClick={() => setMenu(!menu)}><Menu/></button><nav id="navegacion" className={menu ? 'navigation open' : 'navigation'} aria-label="Principal"><p className="nav-title">ESPACIO DE TRABAJO</p>{links.map(item => {
    const activo = path.startsWith(item.path);
    const esCasos = item.path === '/casos';
    const esPlanes = item.path === '/planes';
    const esInvestigaciones = item.path === '/investigaciones';
    if (!esCasos && !esPlanes && !esInvestigaciones) {
      return <a key={item.path} className={`nav-item ${activo ? 'active' : ''}`} href={`#${item.path}`} aria-current={activo ? 'page' : undefined}><item.icon size={19}/>{item.label}</a>;
    }
    const abierto = submenus[item.path] ?? activo;
    const hijos = esPlanes
      ? [{ path: '/planes/mis-planes', label: 'Mis planes', active: path === '/planes/mis-planes' }, { path: '/planes', label: 'Lista de planes', active: path === '/planes' }]
      : [{ path: esCasos ? '/casos/mis-casos' : '/investigaciones/mis-investigaciones', label: esCasos ? 'Mis casos' : 'Mis investigaciones', active: esCasos ? path === '/casos/mis-casos' : path === '/investigaciones/mis-investigaciones' }, ...(esCasos ? [{ path: '/casos', label: 'Lista de casos', active: path === '/casos' }] : [{ path: '/investigaciones', label: 'Lista de investigaciones', active: path === '/investigaciones' }])];
    const submenuId = esCasos ? 'submenu-casos' : esPlanes ? 'submenu-planes' : 'submenu-investigaciones';

    return <div className="nav-group" key={item.path}>
      <div className="nav-group-row" style={{ display: 'flex', alignItems: 'center' }}>
        <a
          className={`nav-item ${activo ? 'active' : ''}`}
          href={`#${item.path}`}
          aria-current={activo && !hijos.some(h => h.active) ? 'page' : undefined}
          style={{ flex: 1 }}
        >
          <item.icon size={19}/>
          <span>{item.label}</span>
        </a>
        <button
          type="button"
          className="nav-group-toggle-btn"
          aria-expanded={abierto}
          aria-controls={submenuId}
          aria-label={`Desplegar opciones de ${item.label}`}
          onClick={(e) => {
            e.preventDefault();
            setSubmenus(prev => ({ ...prev, [item.path]: !abierto }));
          }}
          style={{
            background: 'transparent',
            border: 'none',
            color: 'rgba(255, 255, 255, 0.7)',
            cursor: 'pointer',
            padding: '8px 12px',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            borderRadius: '4px'
          }}
        >
          <ChevronDown size={16} style={{ transition: 'transform 0.15s ease', transform: abierto ? 'rotate(180deg)' : 'none' }}/>
        </button>
      </div>
      <ul id={submenuId} className="nav-submenu" hidden={!abierto}>
        {hijos.map(hijo => (
          <li key={hijo.label}>
            <a
              className={`nav-subitem ${hijo.active ? 'active' : ''}`}
              href={`#${hijo.path}`}
              aria-current={hijo.active ? 'page' : undefined}
            >
              {hijo.label}
            </a>
          </li>
        ))}
      </ul>
    </div>;
    })}{user.rol === 'Administrador' && <><p className="nav-section-title">ADMINISTRACIÓN</p>{seccionesAdmin.map(s => { const activo = path === `/admin/${s.id}` || path === '/admin' && s.id === 'usuarios'; return <a key={s.id} className={`nav-item ${activo ? 'active' : ''}`} href={`#/admin/${s.id}`} aria-current={activo ? 'page' : undefined}><s.icono size={19} aria-hidden="true"/>{s.nombre === 'Usuarios' ? 'Usuarios y permisos' : s.nombre}</a>; })}</>}</nav><div className="sidebar-note"><ShieldCheck size={22}/><strong>De los datos a la acción.</strong><p>Conoce los eventos, identifica prioridades y verifica el cumplimiento.</p></div><div className="workspace"><span className="avatar">{user.nombre[0]}</span><div><strong>{user.nombre}</strong><small>{user.rol === 'Prevencion' ? 'Prevención de Riesgos' : user.rol}</small></div></div></aside><div className="main-wrap"><header className="topbar"><span>Seguridad y salud ocupacional</span><div className="top-actions"><a href="#/password"><LockKeyhole size={15}/><span>Contraseña</span></a><button onClick={() => { sessionStorage.removeItem(TOKEN_KEY); setUser(null); setError(''); }}><LogOut size={15}/><span>Salir</span></button></div></header><main id="contenido" tabIndex={-1} ref={mainRef}>{page}<footer className="footer">GIATEP<span>Gestión preventiva · Datos sintéticos de demostración</span></footer></main></div></div></AuthContext.Provider>;
}
function ErrorPage({ forbidden = false }: { forbidden?: boolean }) { return <section className="empty"><h1>{forbidden ? 'Acceso restringido' : 'Página no encontrada'}</h1><p>{forbidden ? 'Tu rol no tiene acceso a esta sección.' : 'La dirección solicitada no existe.'}</p><a className="primary" href="#/dashboard">Ir al dashboard</a></section>; }
function PasswordPage({ onChanged }: { onChanged: () => void }) { const [error, setError] = useState(''), [busy, setBusy] = useState(false); return <><div className="page-heading"><div><p className="eyebrow">MI CUENTA</p><h1>Cambiar contraseña</h1></div></div><Message error={error}/><form className="panel form narrow" onSubmit={async e => { e.preventDefault(); const data = new FormData(e.currentTarget); if (data.get('nueva') !== data.get('confirmacion')) { setError('Las contraseñas nuevas no coinciden.'); return; } setBusy(true); try { await request('/auth/password', { method: 'PUT', body: JSON.stringify({ actual: data.get('actual'), nueva: data.get('nueva') }) }); onChanged(); } catch (e) { setError((e as Error).message); } finally { setBusy(false); } }}><Field label="Contraseña actual" name="actual" type="password" autoComplete="current-password" required/><Field label="Nueva contraseña" name="nueva" type="password" autoComplete="new-password" required minLength={12} maxLength={200} help="Al menos 12 caracteres."/><Field label="Confirmar nueva contraseña" name="confirmacion" type="password" autoComplete="new-password" required minLength={12}/><button className="primary" disabled={busy}>Guardar contraseña</button></form></>; }
