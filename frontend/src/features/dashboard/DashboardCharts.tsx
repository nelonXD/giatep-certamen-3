import { useId } from 'react';
import { Select } from '../../components/common/UI';

export type Serie = { etiqueta: string; valor: number }[];
const numero = (value: number) => new Intl.NumberFormat('es-CL').format(value);
const porcentaje = (value: number, total: number) => total ? `${new Intl.NumberFormat('es-CL', { maximumFractionDigits: 1 }).format(value / total * 100)} %` : '0 %';
const colores = ['#174c40', '#94ad78', '#c5d7a4', '#dce6d9'];
export const mesLegible = (value: string) => new Intl.DateTimeFormat('es-CL', { month: 'short', year: '2-digit', timeZone: 'UTC' }).format(new Date(`${value}-01T12:00:00Z`));

function TablaDatos({ data, titulo }: { data: Serie; titulo: string }) {
  return <details className="dash-data-table"><summary>Consultar tabla de datos</summary><div className="table-wrap" tabIndex={0} role="region" aria-label={`Datos de ${titulo}`}>
    <table><caption className="sr-only">{titulo}</caption><thead><tr><th scope="col">Categoría</th><th scope="col">Casos</th></tr></thead><tbody>{data.map(d => <tr key={d.etiqueta}><th scope="row">{d.etiqueta}</th><td>{numero(d.valor)}</td></tr>)}</tbody></table>
  </div></details>;
}

export function EvolucionMensual({ data, mesesDisponibles, seleccionado, onSelect }: { data: Serie; mesesDisponibles: string[]; seleccionado?: string; onSelect: (mes: string) => void }) {
  const id = useId();
  const indiceEncontrado = seleccionado ? data.findIndex(d => d.etiqueta === seleccionado) : -1;
  const indice = indiceEncontrado >= 0 ? indiceEncontrado : Math.max(0, data.length - 1);
  const actual = data[indice];
  const maximo = Math.max(1, ...data.map(d => d.valor));
  const paso = Math.max(1, Math.ceil(maximo / 4)), techo = paso * 4;
  const ancho = 700, alto = 250, izquierda = 48, derecha = 20, arriba = 18, abajo = 43;
  const x = (i: number) => izquierda + i * (ancho - izquierda - derecha) / Math.max(1, data.length - 1);
  const y = (value: number) => alto - abajo - value / techo * (alto - arriba - abajo);
  const puntos = data.map((d, i) => `${x(i)},${y(d.valor)}`).join(' ');
  const cada = Math.max(1, Math.ceil(data.length / 6));
  return <section className="dash-chart dash-trend" aria-labelledby={`${id}-heading`}>
    <div className="dash-chart-heading"><div><p className="dash-overline">VOLUMEN DE EVENTOS</p><h2 id={`${id}-heading`}>Evolución mensual</h2><p>Selecciona un mes para filtrar todo el panel.</p></div>
      <span className="dash-chart-total"><strong>{numero(data.reduce((n, d) => n + d.valor, 0))}</strong><small>casos en el período</small></span>
    </div>
    {actual && <div className="dash-trend-inspector"><span role="status"><strong>{numero(actual.valor)}</strong> casos · {mesLegible(actual.etiqueta)}</span>
      <Select label="Filtrar por mes" value={seleccionado || ''} onChange={e => { if (e.target.value) onSelect(e.target.value); else if (seleccionado) onSelect(seleccionado); }}>
        <option value="">Todos los meses</option>{mesesDisponibles.map(mes => <option key={mes} value={mes}>{mesLegible(mes)}</option>)}
      </Select>
    </div>}
    <svg className="dash-line-chart" viewBox={`0 0 ${ancho} ${alto}`} role="img" aria-labelledby={`${id}-title ${id}-desc`}>
      <title id={`${id}-title`}>Casos por mes</title><desc id={`${id}-desc`}>{data.map(d => `${mesLegible(d.etiqueta)}: ${d.valor}`).join('; ')}</desc>
      <defs><linearGradient id={`${id}-area`} x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor="#94ad78" stopOpacity=".3"/><stop offset="100%" stopColor="#94ad78" stopOpacity=".02"/></linearGradient></defs>
      {[0, 1, 2, 3, 4].map(n => <g key={n}><line x1={izquierda} x2={ancho - derecha} y1={y(n * paso)} y2={y(n * paso)} stroke="#e5ebe3" strokeDasharray={n ? '4 5' : undefined}/><text x={izquierda - 12} y={y(n * paso) + 4} textAnchor="end">{numero(n * paso)}</text></g>)}
      {data.length > 1 && <polygon points={`${x(0)},${y(0)} ${puntos} ${x(data.length - 1)},${y(0)}`} fill={`url(#${id}-area)`}/>}
      <polyline points={puntos} fill="none" stroke="#174c40" strokeWidth="3" strokeLinejoin="round"/>
      {actual && <line x1={x(indice)} x2={x(indice)} y1={arriba} y2={y(0)} stroke="#a5b69c" strokeDasharray="4 4"/>}
      {data.map((d, i) => <g key={d.etiqueta} className="dash-mark" role="button" tabIndex={0} aria-pressed={seleccionado === d.etiqueta} aria-label={`Filtrar todo el dashboard por ${mesLegible(d.etiqueta)}: ${numero(d.valor)} casos`} onClick={() => onSelect(d.etiqueta)} onKeyDown={e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onSelect(d.etiqueta); } }}><circle className="dash-chart-point" cx={x(i)} cy={y(d.valor)} r={i === indice ? 6 : 3.5} fill={i === indice ? '#174c40' : 'white'} stroke="#174c40" strokeWidth="2"><title>{mesLegible(d.etiqueta)}: {numero(d.valor)} casos. Filtrar todos los gráficos.</title></circle>
        {(i % cada === 0 || i === data.length - 1) && <text x={x(i)} y={alto - 14} textAnchor="middle">{mesLegible(d.etiqueta)}</text>}
      </g>)}
    </svg>
    <TablaDatos data={data} titulo="Evolución mensual de casos"/>
  </section>;
}

export function ComposicionEventos({ data: datos, seleccionado, onSelect }: { data: Serie; seleccionado?: string; onSelect: (tipoCaso: string) => void }) {
  const data = [...datos].sort((a, b) => a.etiqueta.localeCompare(b.etiqueta, 'es'));
  const id = useId(), total = data.reduce((n, d) => n + d.valor, 0);
  return <section className="dash-chart dash-composition" aria-labelledby={`${id}-heading`}>
    <div className="dash-chart-heading"><div><p className="dash-overline">TIPO DE EVENTO</p><h2 id={`${id}-heading`}>Composición de los casos</h2><p>Selecciona un tipo para filtrar todo el panel.</p></div></div>
    <svg viewBox="0 0 220 190" className="dash-donut" role="img" aria-labelledby={`${id}-title ${id}-desc`}><title id={`${id}-title`}>Tipos de evento</title><desc id={`${id}-desc`}>{data.map(d => `${d.etiqueta}: ${d.valor} casos, ${porcentaje(d.valor, total)}`).join('; ')}</desc>
      <circle cx="110" cy="95" r="65" fill="none" stroke="#edf2e9" strokeWidth="22"/>
      {data.map((d, i) => { const valor = d.etiqueta === 'Accidentes' ? 'Accidente' : 'EnfermedadProfesional'; return <g key={d.etiqueta} className="dash-mark" role="button" tabIndex={0} aria-pressed={seleccionado === valor} aria-label={`Filtrar todo el dashboard por ${d.etiqueta}: ${numero(d.valor)} casos`} onClick={() => onSelect(valor)} onKeyDown={e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onSelect(valor); } }}><circle className="dash-donut-slice" cx="110" cy="95" r="65" fill="none" stroke={colores[i % colores.length]} strokeWidth="22" pathLength="100" strokeDasharray={`${total ? d.valor / total * 100 : 0} 100`} strokeDashoffset={-data.slice(0, i).reduce((n, v) => n + v.valor, 0) / Math.max(1, total) * 100} transform="rotate(-90 110 95)"><title>{d.etiqueta}: {numero(d.valor)} casos. Filtrar todos los gráficos.</title></circle></g>; })}
      <text x="110" y="95" textAnchor="middle" className="dash-donut-total">{numero(total)}</text><text x="110" y="117" textAnchor="middle" className="dash-donut-label">casos</text>
    </svg>
    <ul className="dash-type-legend">{data.map((d, i) => { const valor = d.etiqueta === 'Accidentes' ? 'Accidente' : 'EnfermedadProfesional'; return <li key={d.etiqueta}><button type="button" aria-pressed={seleccionado === valor} aria-label={`Filtrar el dashboard por ${d.etiqueta}: ${numero(d.valor)} casos`} onClick={() => onSelect(valor)}><span className="dash-legend-dot" style={{ background: colores[i % colores.length] }}/><span>{d.etiqueta}</span><strong>{numero(d.valor)}<small>{porcentaje(d.valor, total)}</small></strong></button></li>; })}</ul>
    <TablaDatos data={data} titulo="Composición de los eventos"/>
  </section>;
}

export function FlujoCasos({ data, seleccionado, onSelect }: { data: Serie; seleccionado?: string; onSelect: (estado: string) => void }) {
  const id = useId();
  const estados = ['Borrador', 'En investigación', 'En seguimiento', 'Cerrado'].map(etiqueta => ({ etiqueta, valor: data.find(d => d.etiqueta === etiqueta)?.valor || 0 }));
  const total = estados.reduce((n, d) => n + d.valor, 0);
  const estadoApi: Record<string, string> = { 'Borrador': 'Borrador', 'En investigación': 'EnInvestigacion', 'En seguimiento': 'EnSeguimiento', 'Cerrado': 'Cerrado' };
  return <section className="dash-chart" aria-labelledby={`${id}-heading`}><div className="dash-chart-heading"><div><p className="dash-overline">AVANCE DEL TRABAJO</p><h2 id={`${id}-heading`}>Flujo de los casos</h2><p>Selecciona una etapa para filtrar todo el panel.</p></div></div>
    <ol className="dash-flow">{estados.map((d, i) => <li key={d.etiqueta}><button type="button" className={seleccionado === estadoApi[d.etiqueta] ? 'selected' : ''} aria-pressed={seleccionado === estadoApi[d.etiqueta]} aria-label={`Filtrar todo el dashboard por ${d.etiqueta}: ${numero(d.valor)} casos, ${porcentaje(d.valor, total)}`} onClick={() => onSelect(estadoApi[d.etiqueta])}><span className="dash-flow-step">{i + 1}</span><span className="dash-flow-content"><span className="dash-flow-label"><span>{d.etiqueta}</span><strong>{numero(d.valor)}<small>{porcentaje(d.valor, total)}</small></strong></span><span className="dash-flow-track"><span style={{ width: `${total ? d.valor / total * 100 : 0}%` }}/></span></span></button></li>)}</ol>
    <TablaDatos data={estados} titulo="Avance del flujo de gestión"/>
  </section>;
}

export function CasosCentros({ data, idCentro, seleccionado, onSelect }: { data: Serie; idCentro: (nombre: string) => string | undefined; seleccionado?: string; onSelect: (centroId: string) => void }) {
  const id = useId(), total = data.reduce((n, d) => n + d.valor, 0), max = Math.max(1, ...data.map(d => d.valor));
  const ordenados = [...data].sort((a, b) => b.valor - a.valor);
  return <section className="dash-chart" aria-labelledby={`${id}-heading`}><div className="dash-chart-heading"><div><p className="dash-overline">DISTRIBUCIÓN POR CENTRO</p><h2 id={`${id}-heading`}>Casos por establecimiento</h2><p>Selecciona un centro para filtrar todo el panel.</p></div></div>
    <ol className="dash-centers">{ordenados.map((d, i) => { const centroId = idCentro(d.etiqueta); return <li key={d.etiqueta}><button type="button" className={seleccionado === centroId ? 'selected' : ''} aria-pressed={seleccionado === centroId} disabled={!centroId} aria-label={`Filtrar todo el dashboard por ${d.etiqueta}: ${numero(d.valor)} casos, ${porcentaje(d.valor, total)}`} onClick={() => centroId && onSelect(centroId)}><span className="dash-center-rank">{String(i + 1).padStart(2, '0')}</span><span className="dash-center-content"><span className="dash-flow-label"><span>{d.etiqueta}</span><strong>{numero(d.valor)}<small>{porcentaje(d.valor, total)}</small></strong></span><span className="dash-center-track"><span style={{ width: `${d.valor / max * 100}%` }}/></span></span></button></li>; })}</ol>
    <p className="dash-chart-caveat">Los conteos no representan una tasa de riesgo: no consideran la cantidad de personas expuestas.</p>
    <TablaDatos data={ordenados} titulo="Casos por establecimiento"/>
  </section>;
}
