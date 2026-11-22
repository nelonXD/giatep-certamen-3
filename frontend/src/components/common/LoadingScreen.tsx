import { ShieldCheck } from 'lucide-react';

export function LoadingScreen() {
  return <main className="app-loading" role="status" aria-live="polite">
    <div className="app-loading-content">
      <div className="app-loading-brand">
        <span className="app-loading-icon" aria-hidden="true"><ShieldCheck size={30}/></span>
        <span className="app-loading-brand-text"><strong>GIATEP</strong><small>GESTIÓN PREVENTIVA</small></span>
      </div>
      <div className="app-loading-separator" aria-hidden="true"/>
      <h1>Preparando tu espacio</h1>
      <p>Un momento mientras cargamos la información.</p>
      <div className="app-loading-progress" aria-hidden="true"><span/></div>
      <span className="app-loading-label">CARGANDO</span>
    </div>
    <span className="app-loading-footer">SALUD OCUPACIONAL · GESTIÓN CON SEGUIMIENTO</span>
  </main>;
}
