import { useState } from 'react';
import { Activity, Bell, ClipboardList, ShieldAlert, ArrowUpRight, CheckCircle2, Clock3, AlertCircle, PlusCircle, FileBarChart, Sparkles } from 'lucide-react';
import { useLoad } from '../../hooks/useLoad';
import { useAuth } from '../../context/AuthContext';
import { Loading, Message } from '../../components/common/UI';
import type { Caso, Pagina } from '../../types/caso.types';
import { HorizonComplexTable, HorizonTaskCard } from './HorizonCards';
import './dashboard.css';

interface Indicador { nombre: string; valor: number; unidad: string; formula: string; pregunta: string }
interface Dashboard { indicadores: Indicador[]; actualizado: string }

export function ActividadPage() {
  const user = useAuth();
  const { data: dashData, loading: dashLoading } = useLoad<Dashboard>('/dashboard');
  const { data: casosData, loading: casosLoading, error: casosError } = useLoad<Pagina<Caso>>('/casos?tamano=8&orden=Fecha&descendente=true');

  const casos = casosData?.datos || [];
  const indicador = (nombre: string) => dashData?.indicadores.find(k => k.nombre === nombre);
  const totalCasos = indicador('Casos registrados')?.valor ?? casos.length;
  const abiertos = indicador('Casos abiertos')?.valor ?? casos.filter(c => c.estado !== 'Cerrado').length;
  const vencidas = indicador('Medidas vencidas')?.valor ?? 0;
  const cumplidas = indicador('Medidas cumplidas')?.valor ?? 0;

  return (
    <div className="dash-workspace" aria-busy={casosLoading || dashLoading}>
      <header className="dash-heading">
        <div>
          <div className="dash-badge-row">
            <span className="dash-badge-institutional">
              <Sparkles size={14} aria-hidden="true" />
              Centro de Operaciones y Alertas
            </span>
            <span className="dash-badge-code">Circular SUSESO 3336</span>
          </div>
          <h1>Novedades, lo último que se hizo y alertas</h1>
          <p>
            Bitácora operativa de la red asistencial: seguimiento a los expedientes ingresados recientemente,
            medidas preventivas con plazo perentorio y llamados de atención de salud ocupacional.
          </p>
        </div>
        <div className="dash-heading-actions">
          {user.rol !== 'Administrador' && (
            <a className="dash-heading-actions" href="#/casos/nuevo" style={{ textDecoration: 'none' }}>
              <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '10px 18px', background: 'var(--dash-primary)', color: '#ffffff', borderRadius: 'var(--dash-radius-md)', fontWeight: 700, fontSize: 13 }}>
                <PlusCircle size={16} aria-hidden="true" />
                Registrar nuevo caso
              </span>
            </a>
          )}
          <a className="secondary" href="#/dashboard" style={{ display: 'inline-flex', alignItems: 'center', gap: 6, padding: '10px 16px', background: '#ffffff', color: 'var(--dash-navy-950)', border: '1px solid var(--dash-border)', borderRadius: 'var(--dash-radius-md)', fontWeight: 700, fontSize: 13, textDecoration: 'none' }}>
            Ir al Dashboard
            <ArrowUpRight size={15} aria-hidden="true" />
          </a>
        </div>
      </header>

      {/* Tarjetas Resumen Horizon UI */}
      <section className="dash-actividad-summary-grid" aria-label="Resumen de actividad">
        <article className="dash-actividad-card">
          <div className="dash-kpi-icon-wrap" style={{ background: '#f4f7fe', color: 'var(--dash-primary)' }}>
            <ClipboardList size={22} aria-hidden="true" />
          </div>
          <div>
            <span className="dash-actividad-label">EXPEDIENTES ACTIVOS</span>
            <strong className="dash-actividad-val">{abiertos} en gestión</strong>
            <p className="dash-actividad-desc">De un total de {totalCasos} casos registrados en la red</p>
          </div>
        </article>

        <article className="dash-actividad-card">
          <div className="dash-kpi-icon-wrap" style={{ background: vencidas > 0 ? '#feefee' : '#e6faf5', color: vencidas > 0 ? 'var(--dash-rose)' : 'var(--dash-emerald)' }}>
            {vencidas > 0 ? <AlertCircle size={22} aria-hidden="true" /> : <CheckCircle2 size={22} aria-hidden="true" />}
          </div>
          <div>
            <span className="dash-actividad-label">ESTADO DE MEDIDAS</span>
            <strong className="dash-actividad-val" style={{ color: vencidas > 0 ? 'var(--dash-rose-dark)' : 'var(--dash-emerald-dark)' }}>
              {vencidas > 0 ? `${vencidas}% fuera de plazo` : `${cumplidas}% cumplidas`}
            </strong>
            <p className="dash-actividad-desc">{vencidas > 0 ? 'Requiere intervención correctiva urgente' : 'Cumplimiento normativo al día'}</p>
          </div>
        </article>

        <article className="dash-actividad-card">
          <div className="dash-kpi-icon-wrap" style={{ background: '#e0f2fe', color: 'var(--dash-cyan)' }}>
            <Clock3 size={22} aria-hidden="true" />
          </div>
          <div>
            <span className="dash-actividad-label">ALERTAS NORMATIVAS</span>
            <strong className="dash-actividad-val">Plazos CPHS</strong>
            <p className="dash-actividad-desc">Monitoreo continuo según directrices SUSESO</p>
          </div>
        </article>
      </section>

      <Message error={casosError} />

      {casosLoading ? (
        <div className="dash-loading"><Loading /></div>
      ) : (
        <div className="dash-horizon-grid">
          <HorizonComplexTable casos={casos} loading={casosLoading} />
          <HorizonTaskCard casos={casos} pendientesVencidas={vencidas} />
        </div>
      )}
    </div>
  );
}
