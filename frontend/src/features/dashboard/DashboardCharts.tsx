import { useId, useState } from 'react';
import { Select } from '../../components/common/UI';
import { TrendingUp, PieChart, GitCommitHorizontal, Building, ArrowUpRight } from 'lucide-react';

export type Serie = { etiqueta: string; valor: number }[];
const numero = (value: number) => new Intl.NumberFormat('es-CL').format(value);
const porcentaje = (value: number, total: number) => total ? `${new Intl.NumberFormat('es-CL', { maximumFractionDigits: 1 }).format(value / total * 100)} %` : '0 %';
const colores = ['#4318ff', '#01b574', '#ffb547', '#0284c7'];
export const mesLegible = (value: string) => new Intl.DateTimeFormat('es-CL', { month: 'short', year: '2-digit', timeZone: 'UTC' }).format(new Date(`${value}-01T12:00:00Z`));

function TablaDatos({ data, titulo }: { data: Serie; titulo: string }) {
  return <details className="dash-data-table"><summary>Ver tabla de datos detallada</summary><div className="table-wrap" tabIndex={0} role="region" aria-label={`Datos de ${titulo}`}>
    <table><caption className="sr-only">{titulo}</caption><thead><tr><th scope="col">Categoría</th><th scope="col">Casos</th><th scope="col">Participación</th></tr></thead><tbody>{data.map(d => {
      const total = data.reduce((a, b) => a + b.valor, 0);
      return <tr key={d.etiqueta}><th scope="row">{d.etiqueta}</th><td><strong>{numero(d.valor)}</strong></td><td>{porcentaje(d.valor, total)}</td></tr>;
    })}</tbody></table>
  </div></details>;
}

export function EvolucionMensual({ data, mesesDisponibles, seleccionado, onSelect }: { data: Serie; mesesDisponibles: string[]; seleccionado?: string; onSelect: (mes: string) => void }) {
  const id = useId();
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);
  const indiceEncontrado = seleccionado ? data.findIndex(d => d.etiqueta === seleccionado) : -1;
  const indice = hoverIndex !== null ? hoverIndex : indiceEncontrado >= 0 ? indiceEncontrado : Math.max(0, data.length - 1);
  const actual = data[indice];
  const maximo = Math.max(1, ...data.map(d => d.valor));
  const paso = Math.max(1, Math.ceil(maximo / 4)), techo = paso * 4;
  const ancho = 720, alto = 260, izquierda = 44, derecha = 24, arriba = 28, abajo = 44;
  const x = (i: number) => izquierda + i * (ancho - izquierda - derecha) / Math.max(1, data.length - 1);
  const y = (value: number) => alto - abajo - value / techo * (alto - arriba - abajo);
  const puntos = data.map((d, i) => `${x(i)},${y(d.valor)}`).join(' ');
  const cada = Math.max(1, Math.ceil(data.length / 6));

  const total = data.reduce((n, d) => n + d.valor, 0);
  const promedio = data.length ? (total / data.length).toFixed(1) : '0';

  return <section className="dash-chart dash-chart-hero" aria-labelledby={`${id}-heading`}>
    <header className="dash-card-header">
      <div className="dash-card-title-group">
        <div className="dash-icon-badge primary">
          <TrendingUp size={18} aria-hidden="true"/>
        </div>
        <div>
          <span className="dash-category-tag">SERIE TEMPORAL</span>
          <h2 id={`${id}-heading`}>Evolución mensual</h2>
        </div>
      </div>
      <div className="dash-card-actions">
        <span className="dash-stat-pill">
          <strong>{numero(total)}</strong> casos
        </span>
        <span className="dash-stat-pill secondary">
          Prom: {promedio}/mes
        </span>
      </div>
    </header>

    <div className="dash-inspector-toolbar">
      {actual && <div className="dash-live-metric">
        <span className="dash-pulse-dot" aria-hidden="true"/>
        <span className="dash-live-label">{mesLegible(actual.etiqueta)}:</span>
        <strong className="dash-live-value">{numero(actual.valor)} casos</strong>
        {seleccionado === actual.etiqueta && <span className="dash-active-tag">Filtro aplicado</span>}
      </div>}
      <div className="dash-select-compact">
        <Select label="Filtrar por mes" value={seleccionado || ''} onChange={e => { if (e.target.value) onSelect(e.target.value); else if (seleccionado) onSelect(seleccionado); }}>
          <option value="">Todos los meses</option>
          {mesesDisponibles.map(mes => <option key={mes} value={mes}>{mesLegible(mes)}</option>)}
        </Select>
      </div>
    </div>

    <div className="dash-canvas-wrap">
      <svg className="dash-svg-chart" viewBox={`0 0 ${ancho} ${alto}`} role="img" aria-labelledby={`${id}-title ${id}-desc`}>
        <title id={`${id}-title`}>Evolución mensual de casos</title>
        <desc id={`${id}-desc`}>{data.map(d => `${mesLegible(d.etiqueta)}: ${d.valor}`).join('; ')}</desc>
        <defs>
          <linearGradient id={`${id}-gradient`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#4318ff" stopOpacity="0.32"/>
            <stop offset="60%" stopColor="#4318ff" stopOpacity="0.08"/>
            <stop offset="100%" stopColor="#4318ff" stopOpacity="0.00"/>
          </linearGradient>
          <filter id={`${id}-shadow`} x="-20%" y="-20%" width="140%" height="140%">
            <feDropShadow dx="0" dy="4" stdDeviation="4" floodColor="#1b2559" floodOpacity="0.16"/>
          </filter>
        </defs>

        {/* Ejes y líneas horizontales de cuadrícula */}
        {[0, 1, 2, 3, 4].map(n => <g key={n} className="dash-grid-line">
          <line x1={izquierda} x2={ancho - derecha} y1={y(n * paso)} y2={y(n * paso)} stroke="#f4f7fe" strokeDasharray={n ? '3 4' : undefined} strokeWidth={n ? 1 : 1.5}/>
          <text x={izquierda - 10} y={y(n * paso) + 4} textAnchor="end" className="dash-chart-axis">{numero(n * paso)}</text>
        </g>)}

        {/* Polígono de área degradada */}
        {data.length > 1 && <polygon points={`${x(0)},${alto - abajo} ${puntos} ${x(data.length - 1)},${alto - abajo}`} fill={`url(#${id}-gradient)`}/>}

        {/* Línea principal del gráfico */}
        <polyline points={puntos} fill="none" stroke="#4318ff" strokeWidth="3.5" strokeLinejoin="round" strokeLinecap="round"/>

        {/* Indicador de cursor / selección vertical */}
        {actual && <>
          <line x1={x(indice)} x2={x(indice)} y1={arriba} y2={alto - abajo} stroke="#4318ff" strokeWidth="1.5" strokeDasharray="4 3"/>
          <g transform={`translate(${Math.max(izquierda + 38, Math.min(ancho - derecha - 44, x(indice)))}, ${Math.max(arriba + 12, y(actual.valor) - 18)})`}>
            <rect x="-42" y="-14" width="84" height="24" rx="8" fill="#1b2559" filter={`url(#${id}-shadow)`}/>
            <text x="0" y="2" fill="#ffffff" fontSize="11" fontWeight="700" textAnchor="middle" dominantBaseline="middle" fontFamily="'DM Sans', sans-serif">
              {numero(actual.valor)} casos
            </text>
          </g>
        </>}

        {/* Puntos interactivos con hitboxes ampliadas para fácil click */}
        {data.map((d, i) => {
          const isPointActive = i === indice;
          return <g key={d.etiqueta} className="dash-point-trigger" onMouseEnter={() => setHoverIndex(i)} onMouseLeave={() => setHoverIndex(null)} onClick={() => onSelect(d.etiqueta)}>
            <circle cx={x(i)} cy={y(d.valor)} r="16" fill="transparent" className="dash-hitbox"/>
            {isPointActive && <circle cx={x(i)} cy={y(d.valor)} r="9" fill="#4318ff" opacity="0.22" className="dash-point-halo"/>}
            <circle cx={x(i)} cy={y(d.valor)} r={isPointActive ? 6.5 : 4} fill={isPointActive ? '#4318ff' : '#ffffff'} stroke="#1b2559" strokeWidth={isPointActive ? 2.5 : 2} className="dash-data-point"/>
            {(i % cada === 0 || i === data.length - 1) && <text x={x(i)} y={alto - 12} textAnchor="middle" className="dash-x-label">{mesLegible(d.etiqueta)}</text>}
          </g>;
        })}
      </svg>
    </div>

    <TablaDatos data={data} titulo="Evolución mensual de casos"/>
  </section>;
}

export function ComposicionEventos({ data: datos, seleccionado, onSelect }: { data: Serie; seleccionado?: string; onSelect: (tipoCaso: string) => void }) {
  const data = [...datos].sort((a, b) => a.etiqueta.localeCompare(b.etiqueta, 'es'));
  const id = useId(), total = data.reduce((n, d) => n + d.valor, 0);
  const [hoverItem, setHoverItem] = useState<{ etiqueta: string; valor: number } | null>(null);
  const itemActivo = hoverItem || (seleccionado ? data.find(d => (d.etiqueta === 'Accidentes' ? 'Accidente' : 'EnfermedadProfesional') === seleccionado) : null);

  return <section className="dash-chart dash-chart-donut" aria-labelledby={`${id}-heading`}>
    <header className="dash-card-header">
      <div className="dash-card-title-group">
        <div className="dash-icon-badge secondary">
          <PieChart size={18} aria-hidden="true"/>
        </div>
        <div>
          <span className="dash-category-tag">DISTRIBUCIÓN</span>
          <h2 id={`${id}-heading`}>Composición de los casos</h2>
        </div>
      </div>
    </header>

    <div className="dash-donut-centerpiece">
      <svg viewBox="0 0 220 200" className="dash-donut-svg" role="img" aria-labelledby={`${id}-title ${id}-desc`}>
        <title id={`${id}-title`}>Tipos de evento</title>
        <desc id={`${id}-desc`}>{data.map(d => `${d.etiqueta}: ${d.valor} casos, ${porcentaje(d.valor, total)}`).join('; ')}</desc>
        <circle cx="110" cy="100" r="66" fill="none" stroke="#f4f7fe" strokeWidth="22"/>
        {data.map((d, i) => {
          const valor = d.etiqueta === 'Accidentes' ? 'Accidente' : 'EnfermedadProfesional';
          const isSelected = seleccionado === valor;
          const isHovered = hoverItem?.etiqueta === d.etiqueta;
          return <g key={d.etiqueta} className="dash-donut-slice-group" onMouseEnter={() => setHoverItem(d)} onMouseLeave={() => setHoverItem(null)} onClick={() => onSelect(valor)}>
            <circle cx="110" cy="100" r="66" fill="none" stroke={colores[i % colores.length]} strokeWidth={isHovered || isSelected ? 26 : 22} pathLength="100" strokeDasharray={`${total ? d.valor / total * 100 : 0} 100`} strokeDashoffset={-data.slice(0, i).reduce((n, v) => n + v.valor, 0) / Math.max(1, total) * 100} transform="rotate(-90 110 100)" className="dash-donut-segment" style={{ opacity: hoverItem && !isHovered ? 0.5 : 1 }}>
              <title>{d.etiqueta}: {numero(d.valor)} casos</title>
            </circle>
          </g>;
        })}
        <text x="110" y="96" textAnchor="middle" className="dash-donut-big-num">
          {itemActivo ? numero(itemActivo.valor) : numero(total)}
        </text>
        <text x="110" y="116" textAnchor="middle" className="dash-donut-subtext">
          {itemActivo ? porcentaje(itemActivo.valor, total) : 'casos totales'}
        </text>
      </svg>
    </div>

    <ul className="dash-interactive-legend">
      {data.map((d, i) => {
        const valor = d.etiqueta === 'Accidentes' ? 'Accidente' : 'EnfermedadProfesional';
        const isSelected = seleccionado === valor;
        return <li key={d.etiqueta}>
          <button type="button" className={`dash-legend-card ${isSelected ? 'active' : ''}`} aria-pressed={isSelected} onMouseEnter={() => setHoverItem(d)} onMouseLeave={() => setHoverItem(null)} onClick={() => onSelect(valor)}>
            <div className="dash-legend-top">
              <span className="dash-legend-indicator" style={{ background: colores[i % colores.length] }}/>
              <span className="dash-legend-title">{d.etiqueta}</span>
            </div>
            <div className="dash-legend-values">
              <strong>{numero(d.valor)}</strong>
              <small>{porcentaje(d.valor, total)}</small>
            </div>
          </button>
        </li>;
      })}
    </ul>

    <TablaDatos data={data} titulo="Composición de los eventos"/>
  </section>;
}

export function FlujoCasos({ data, seleccionado, onSelect }: { data: Serie; seleccionado?: string; onSelect: (estado: string) => void }) {
  const id = useId();
  const estados = ['Borrador', 'En investigación', 'En seguimiento', 'Cerrado'].map(etiqueta => ({ etiqueta, valor: data.find(d => d.etiqueta === etiqueta)?.valor || 0 }));
  const total = estados.reduce((n, d) => n + d.valor, 0);
  const estadoApi: Record<string, string> = { 'Borrador': 'Borrador', 'En investigación': 'EnInvestigacion', 'En seguimiento': 'EnSeguimiento', 'Cerrado': 'Cerrado' };
  
  return <section className="dash-chart dash-chart-flow" aria-labelledby={`${id}-heading`}>
    <header className="dash-card-header">
      <div className="dash-card-title-group">
        <div className="dash-icon-badge tertiary">
          <GitCommitHorizontal size={18} aria-hidden="true"/>
        </div>
        <div>
          <span className="dash-category-tag">TRAZABILIDAD</span>
          <h2 id={`${id}-heading`}>Flujo de los casos</h2>
        </div>
      </div>
      <span className="dash-stat-pill">4 Etapas</span>
    </header>

    <p className="dash-chart-subtitle">Progreso de expedientes según ciclo de resolución y medidas correctivas.</p>

    <ol className="dash-pipeline-list">
      {estados.map((d, i) => {
        const pct = total ? Math.round(d.valor / total * 100) : 0;
        const isSelected = seleccionado === estadoApi[d.etiqueta];
        return <li key={d.etiqueta}>
          <button type="button" className={`dash-pipeline-card ${isSelected ? 'selected' : ''}`} aria-pressed={isSelected} onClick={() => onSelect(estadoApi[d.etiqueta])}>
            <div className="dash-pipeline-badge">Paso 0{i + 1}</div>
            <div className="dash-pipeline-body">
              <div className="dash-pipeline-header">
                <span className="dash-pipeline-title">{d.etiqueta}</span>
                <span className="dash-pipeline-metric">
                  <strong>{numero(d.valor)}</strong>
                  <small>({pct}%)</small>
                </span>
              </div>
              <div className="dash-pipeline-progress-track">
                <div className="dash-pipeline-progress-fill" style={{ width: `${pct}%` }}/>
              </div>
            </div>
          </button>
        </li>;
      })}
    </ol>

    <TablaDatos data={estados} titulo="Avance del flujo de gestión"/>
  </section>;
}

export function CasosCentros({ data, idCentro, seleccionado, onSelect }: { data: Serie; idCentro: (nombre: string) => string | undefined; seleccionado?: string; onSelect: (centroId: string) => void }) {
  const id = useId(), total = data.reduce((n, d) => n + d.valor, 0), max = Math.max(1, ...data.map(d => d.valor));
  const ordenados = [...data].sort((a, b) => b.valor - a.valor);

  return <section className="dash-chart dash-chart-centers" aria-labelledby={`${id}-heading`}>
    <header className="dash-card-header">
      <div className="dash-card-title-group">
        <div className="dash-icon-badge dark">
          <Building size={18} aria-hidden="true"/>
        </div>
        <div>
          <span className="dash-category-tag">RED ASISTENCIAL</span>
          <h2 id={`${id}-heading`}>Casos por establecimiento</h2>
        </div>
      </div>
      <span className="dash-stat-pill">{ordenados.length} centros</span>
    </header>

    <p className="dash-chart-subtitle">Volumen de incidentes por CESFAM, CECOSF y Postas de Salud.</p>

    <ol className="dash-centers">
      {ordenados.map((d, i) => {
        const centroId = idCentro(d.etiqueta);
        const isSelected = seleccionado === centroId;
        const pct = total ? Math.round(d.valor / total * 100) : 0;
        return <li key={d.etiqueta}>
          <button type="button" className={`dash-center-row ${isSelected ? 'selected' : ''}`} aria-pressed={isSelected} disabled={!centroId} onClick={() => centroId && onSelect(centroId)}>
            <span className="dash-rank-chip">{String(i + 1).padStart(2, '0')}</span>
            <div className="dash-center-details">
              <div className="dash-center-topline">
                <span className="dash-center-name">{d.etiqueta}</span>
                <span className="dash-center-count">
                  <strong>{numero(d.valor)}</strong> casos <small>({pct}%)</small>
                </span>
              </div>
              <div className="dash-center-progress-bar">
                <div className="dash-center-progress-fill" style={{ width: `${d.valor / max * 100}%` }}/>
              </div>
            </div>
            <ArrowUpRight size={14} className="dash-row-chevron" aria-hidden="true"/>
          </button>
        </li>;
      })}
    </ol>

    <p className="dash-chart-caveat">Información normalizada según DIAT/DIEP emitidas. No representa tasa de riesgo por falta de censo de personal expuesto.</p>
    <TablaDatos data={ordenados} titulo="Casos por establecimiento"/>
  </section>;
}
