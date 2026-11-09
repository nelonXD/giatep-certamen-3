import denunciasSchema from '../../shared/denuncias.json' with { type: 'json' };
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import fs from 'node:fs';
import path from 'node:path';
const root = path.resolve(process.cwd(), '..');
const env = Object.fromEntries(fs.readFileSync(path.join(root, 'backend', '.env'), 'utf8').split(/\r?\n/).filter(l => l && !l.startsWith('#')).map(l => { const i = l.indexOf('='); return [l.slice(0, i), l.slice(i + 1)]; }));
const evidence = path.join(root, 'evidencias');
fs.mkdirSync(evidence, { recursive: true });
async function login(page: import('@playwright/test').Page, cuenta: string, key: string) { await page.goto('/'); await page.getByLabel('Cuenta').fill(cuenta); await page.getByLabel('Contraseña').fill(env[key]); await page.getByRole('button', { name: 'Iniciar sesión', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Comprende. Prioriza. Actúa.' })).toBeVisible(); await expect(page.getByText('Casos registrados', { exact: true })).toBeVisible(); }
async function axe(page: import('@playwright/test').Page, name: string) { const result = await new AxeBuilder({ page }).withTags(['wcag2a','wcag2aa','wcag21a','wcag21aa','wcag22aa']).analyze(); fs.writeFileSync(path.join(evidence, `axe-${name}.json`), JSON.stringify({ url: page.url(), fecha: new Date().toISOString(), violations: result.violations, passes: result.passes.map(p => p.id) }, null, 2)); expect(result.violations, JSON.stringify(result.violations.map(v => ({ id: v.id, nodes: v.nodes.map(n => n.target) })))).toEqual([]); }
async function completarDenuncia(page: import('@playwright/test').Page, formulario: string, buscarRegistrados = true) {
  await expect(page.getByText('D. Identificación del denunciante', { exact: true })).toHaveCount(0);
  await expect(page.getByLabel('RUN del denunciante', { exact: false })).toHaveCount(0);
  if (buscarRegistrados) {
    const token = await page.evaluate(() => sessionStorage.getItem('giatep_token'));
    const options = await (await page.request.get('/api/v1/opciones', { headers: { Authorization: `Bearer ${token}` } })).json();
    const centro = await page.getByRole('combobox', { name: 'Establecimiento', exact: true }).inputValue();
    const empleador = options.empleadores.find((e: { nombre: string }) => e.nombre.startsWith('Demo')) || options.empleadores[0];
    const trabajador = options.trabajadores.find((t: { run?: string; establecimientoId: string }) => t.run && (!centro || t.establecimientoId === centro));
    await page.getByLabel('RUT del empleador').fill(empleador.rut);
    await expect(page.getByLabel('Nombre o razón social')).toHaveValue(empleador.nombre);
    await page.getByLabel('RUN del trabajador').fill(trabajador.run);
    await expect(page.getByLabel('Nombre completo del trabajador')).toHaveValue(trabajador.nombre);
  }
  for (const grupo of denunciasSchema.filter(g => g.types.includes(formulario))) for (const campo of grupo.fields.filter(c => c.required && !('when' in c))) {
    const control = page.getByLabel(campo.label, { exact: false });
    if (await control.inputValue()) continue;
    if (campo.type === 'select') { const options = 'options' in campo ? campo.options as string[] : []; await control.selectOption(campo.key === 'tipoAccidente' ? 'Trabajo' : campo.key === 'clasificacionAccidente' ? 'Otro' : options[0]); }
    else await control.fill(campo.type === 'rut' ? '12.345.678-5' : campo.key === 'fechaNacimiento' ? '1990-01-01' : campo.type === 'date' ? '2026-11-03' : campo.type === 'time' ? '09:30' : campo.type === 'number' ? '10' : 'Antecedente sintético para prueba del formulario');
  }
}
test('Login, dashboard accesible y responsive', async ({ page }) => {
  await page.goto('/'); await axe(page, 'login');
  await login(page, 'prevencion', 'Seed__PrevencionPassword');
  await expect(page.locator('.dash-kpi')).toHaveCount(4); await expect(page.locator('.dash-closure-item')).toHaveCount(2); await expect(page.locator('.dash-chart')).toHaveCount(4); await axe(page, 'dashboard');
  for (const width of [360, 768, 1440]) { await page.setViewportSize({ width, height: 1000 }); await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true); await page.screenshot({ path: path.join(evidence, `dashboard-${width}.png`), fullPage: true }); }
  await page.setViewportSize({ width: 1440, height: 1000 }); await page.getByRole('combobox', { name: 'Establecimiento', exact: true }).selectOption({ index: 1 }); await page.getByRole('button', { name: 'Aplicar filtros', exact: true }).click(); await expect(page.locator('.dash-chart').filter({ has: page.getByRole('heading', { name: 'Casos por establecimiento', exact: true }) }).locator('.dash-centers li')).toHaveCount(1);
  await page.keyboard.press('Control+Home'); await page.keyboard.press('Tab');
});
test('Investigación separada, árbol y plan de acción después del cierre', async ({ page }) => {
  await login(page, 'prevencion', 'Seed__PrevencionPassword');
  await page.getByRole('link', { name: 'Gestión de casos', exact: true }).click(); await page.getByRole('button', { name: 'Registrar caso', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: /Accidente del trabajo/ }).click(); await page.getByLabel('Fecha del evento').fill('2026-10-15'); await page.getByRole('combobox', { name: 'Establecimiento', exact: true }).selectOption({ index: 1 });  await page.getByLabel('Relato de los hechos').fill('Caso sintético E2E: caída durante desplazamiento por el pasillo de insumos.'); await completarDenuncia(page, 'DIAT'); await page.getByRole('button', { name: 'Guardar caso', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Antecedentes del caso', exact: true })).toBeVisible();
  const codigo = await page.locator('h1').innerText(); await page.getByRole('link', { name: 'Volver al listado', exact: true }).click(); await page.getByRole('searchbox', { name: 'Buscar', exact: true }).fill(codigo); await page.getByRole('button', { name: `Iniciar investigación del caso ${codigo}`, exact: true }).click(); await expect(page.getByRole('heading', { name: /Investigación · GIA-/ })).toBeVisible(); await axe(page, 'investigacion-antecedentes'); page.on('dialog', d => d.accept());
  await page.getByRole('button', { name: 'Entrevistas', exact: true }).click(); await page.getByLabel('Persona entrevistada').fill('Testigo sintético E2E'); await page.getByLabel('Fecha de entrevista').fill('2026-10-16'); await page.getByLabel('Declaración de la entrevista').fill('El testigo observó materiales en el pasillo antes del tropiezo de la persona.'); await page.getByRole('button', { name: 'Guardar entrevista', exact: true }).click(); await expect(page.getByText('Testigo sintético E2E · Trabajador', { exact: true })).toBeVisible(); await axe(page, 'investigacion-entrevistas');
  await page.getByRole('button', { name: 'Evidencias', exact: true }).click(); await page.getByLabel('Archivo de evidencia').setInputFiles({ name: 'investigacion.png', mimeType: 'image/png', buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jF5kAAAAASUVORK5CYII=', 'base64') }); await page.getByRole('button', { name: 'Adjuntar evidencia', exact: true }).click(); await expect(page.getByText('investigacion.png', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Relato final', exact: true }).click(); await page.getByLabel('Relato final', { exact: false }).fill('La evidencia y las entrevistas permiten reconstruir la caída por obstrucción del pasillo durante el traslado de insumos.'); await page.getByRole('button', { name: 'Lista de hechos', exact: true }).click(); await page.getByRole('button', { name: 'Relato final', exact: true }).click(); await expect(page.getByLabel('Relato final', { exact: false })).toHaveValue('La evidencia y las entrevistas permiten reconstruir la caída por obstrucción del pasillo durante el traslado de insumos.'); await page.getByRole('button', { name: 'Guardar relato final', exact: true }).click(); await expect(page.getByRole('status')).toContainText('Información guardada');
  await page.getByRole('button', { name: 'Lista de hechos', exact: true }).click(); await page.getByLabel('Descripción del hecho').fill('Materiales obstruían el pasillo de circulación'); await page.getByRole('button', { name: 'Agregar hecho', exact: true }).click(); await expect(page.getByRole('article').getByText('Materiales obstruían el pasillo de circulación', { exact: true })).toBeVisible();
  await page.getByLabel('Descripción del hecho').fill('La persona cayó al tropezar en el pasillo'); await page.getByRole('combobox', { name: 'Tipo de hecho', exact: true }).selectOption('true'); await page.getByRole('button', { name: 'Agregar hecho', exact: true }).click(); await expect(page.getByRole('article').getByText('La persona cayó al tropezar en el pasillo', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Árbol de causas', exact: true }).click(); await page.getByRole('combobox', { name: 'Hecho antecedente', exact: true }).selectOption({ label: 'Materiales obstruían el pasillo de circulación' }); await page.getByRole('combobox', { name: 'Hecho consecuencia', exact: true }).selectOption({ label: 'La persona cayó al tropezar en el pasillo' }); await page.getByRole('button', { name: 'Agregar relación', exact: true }).click(); await expect(page.locator('.tree-scroll svg path[marker-end]')).toHaveCount(1); await axe(page, 'investigacion-arbol'); await page.screenshot({ path: path.join(evidence, 'investigacion-arbol.png'), fullPage: true });
  await page.getByRole('button', { name: 'Medidas', exact: true }).click(); await page.getByRole('combobox', { name: 'Hecho causal asociado', exact: true }).selectOption({ index: 1 }); await page.getByLabel('Medida preventiva o correctiva').fill('Despejar el pasillo y verificar diariamente su circulación segura'); await page.getByRole('combobox', { name: 'Responsable de la medida', exact: true }).selectOption({ label: 'Cuenta demostración prevencion' }); await page.getByLabel('Plazo de implementación').fill('2026-11-20'); await page.getByRole('button', { name: 'Guardar medida', exact: true }).click(); await expect(page.getByText('Despejar el pasillo y verificar diariamente su circulación segura', { exact: true })).toBeVisible(); await axe(page, 'investigacion-medidas');
  await page.getByRole('button', { name: 'Cerrar investigación', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Cerrada', exact: true })).toBeVisible(); await expect(page.getByRole('button', { name: 'Iniciar plan de acción', exact: true })).toBeVisible(); await page.getByRole('button', { name: 'Iniciar plan de acción', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Plan de acción', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Cerrar caso y plan', exact: true })).toBeDisabled();
  await page.getByLabel('Detalle de implementación').fill('Se despejó el pasillo y se verificó su circulación segura.');
  const prueba = { name: 'cumplimiento.png', mimeType: 'image/png', buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jF5kAAAAASUVORK5CYII=', 'base64') };
  await page.getByLabel('Evidencia de cumplimiento').setInputFiles(prueba);
  await page.getByRole('button', { name: 'Enviar a verificación', exact: true }).click();
  await expect(page.locator('.badge').filter({ hasText: /^Por verificar$/ })).toBeVisible();
  await page.getByLabel('Observación de revisión').fill('Falta evidenciar la inspección preventiva del pasillo.');
  await page.getByRole('combobox', { name: 'Decisión de revisión', exact: true }).selectOption('devolver');
  await page.getByRole('button', { name: 'Guardar revisión', exact: true }).click();
  await expect(page.locator('.badge').filter({ hasText: /^Devuelta$/ })).toBeVisible();
  await page.getByLabel('Detalle de implementación').fill('Se implementó también la inspección diaria del pasillo.');
  await page.getByLabel('Evidencia de cumplimiento').setInputFiles({ ...prueba, name: 'inspeccion.png' });
  await page.getByRole('button', { name: 'Enviar a verificación', exact: true }).click();
  await page.getByLabel('Observación de revisión').fill('Implementación y evidencia verificadas satisfactoriamente.');
  await page.getByRole('button', { name: 'Guardar revisión', exact: true }).click();
  await expect(page.locator('.badge').filter({ hasText: /^Verificada$/ })).toBeVisible();
  await axe(page, 'plan-accion');
  for (const width of [360, 768, 1440]) { await page.setViewportSize({ width, height: 1000 }); await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true); }
  await page.screenshot({ path: path.join(evidence, 'plan-accion.png'), fullPage: true });
  await page.getByRole('button', { name: 'Cerrar caso y plan', exact: true }).click();
  await expect(page.getByText(/Caso cerrado el/)).toBeVisible(); await page.reload(); await expect(page.getByText(/Caso cerrado el/)).toBeVisible();
  await page.getByRole('link', { name: 'Volver a planes', exact: true }).click();
  await page.getByLabel('Buscar plan').fill(codigo); await expect(page.getByRole('cell', { name: codigo, exact: true })).toBeVisible(); await axe(page, 'planes-listado');
});
test('Reportes, nueve exportaciones e impresión', async ({ page }) => {
  await login(page, 'prevencion', 'Seed__PrevencionPassword'); await page.getByRole('link', { name: 'Reportes', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Detalle operativo de casos', exact: true })).toBeVisible(); await axe(page, 'reportes'); fs.mkdirSync(path.join(evidence, 'exportaciones'), { recursive: true });
  for (const tipo of ['operativo','gerencial','parametrizable']) { await page.getByRole('combobox', { name: 'Reporte', exact: true }).selectOption(tipo); await expect(page.locator('.report-sheet')).toBeVisible(); for (const formato of ['XLSX','PDF','CSV']) { const download = page.waitForEvent('download'); await page.getByRole('button', { name: formato, exact: true }).click(); const file = await download; await file.saveAs(path.join(evidence, 'exportaciones', `giatep-${tipo}.${formato.toLowerCase()}`)); } }
  await page.getByRole('combobox', { name: 'Reporte', exact: true }).selectOption('gerencial'); await expect(page.getByRole('heading', { name: 'Resumen gerencial por establecimiento', exact: true })).toBeVisible(); await page.emulateMedia({ media: 'print' }); await page.pdf({ path: path.join(evidence, 'impresion-gerencial.pdf'), format: 'A4', landscape: true, printBackground: true }); await page.screenshot({ path: path.join(evidence, 'impresion-gerencial.png'), fullPage: true });
});
test('Administrador, catálogos y auditoría accesibles', async ({ page }) => {
  await login(page, 'admin', 'Seed__AdminPassword'); await expect(page.getByRole('link', { name: 'Gestión de casos', exact: true })).toHaveCount(0); await page.getByRole('link', { name: 'Usuarios y permisos', exact: true }).click(); await expect(page.getByRole('button', { name: /^Editar / }).first()).toBeVisible(); await axe(page, 'administracion'); await page.getByRole('button', { name: 'Agregar usuario', exact: true }).click(); await axe(page, 'formulario-admin'); await page.getByRole('button', { name: 'Cancelar', exact: true }).click(); await page.getByRole('link', { name: 'Auditoría', exact: true }).click(); await expect(page.getByRole('cell', { name: 'Exportación', exact: true }).first()).toBeVisible(); await axe(page, 'auditoria');
});
test('Jefatura ve su centro y no registra casos', async ({ page }) => {
  await login(page, 'jefatura', 'Seed__JefaturaPassword'); await page.getByRole('link', { name: 'Gestión de casos', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Registro de casos' })).toBeVisible(); await expect(page.getByRole('button', { name: 'Registrar caso', exact: true })).toHaveCount(0); await expect(page.getByRole('combobox', { name: 'Establecimiento', exact: true }).locator('option')).toHaveCount(2); await axe(page, 'jefatura');
});
test('Flujo completo de reporte con teclado y reflow equivalente al 200 por ciento', async ({ page }) => {
  await page.goto('/'); await page.keyboard.press('Tab'); await expect(page.getByRole('textbox', { name: 'Cuenta', exact: true })).toBeFocused(); await page.keyboard.type('prevencion'); await page.keyboard.press('Tab'); await page.keyboard.type(env.Seed__PrevencionPassword); await page.keyboard.press('Tab'); await page.keyboard.press('Enter'); await expect(page.locator('.dash-kpi')).toHaveCount(4); await expect(page.locator('.dash-closure-item')).toHaveCount(2);
  await page.setViewportSize({ width: 720, height: 500 }); await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true); await page.screenshot({ path: path.join(evidence, 'dashboard-reflow-200.png'), fullPage: true }); await page.setViewportSize({ width: 1440, height: 1000 });
  async function tabTo(locator: import('@playwright/test').Locator) { for (let i = 0; i < 90; i++) { if (await locator.evaluate(el => el === document.activeElement)) return; await page.keyboard.press('Tab'); } throw new Error('No se alcanzó el control con teclado'); }
  await tabTo(page.getByRole('link', { name: 'Reportes', exact: true })); await page.keyboard.press('Enter'); await expect(page.getByRole('heading', { name: 'Detalle operativo de casos', exact: true })).toBeVisible(); await tabTo(page.getByRole('combobox', { name: 'Reporte', exact: true })); await page.keyboard.press('ArrowDown'); await page.keyboard.press('Tab'); await expect(page.getByRole('heading', { name: 'Resumen gerencial por establecimiento', exact: true })).toBeVisible(); await tabTo(page.getByRole('button', { name: 'CSV', exact: true })); const download = page.waitForEvent('download'); await page.keyboard.press('Enter'); await (await download).saveAs(path.join(evidence, 'exportaciones', 'reporte-teclado.csv'));
  fs.writeFileSync(path.join(evidence, 'teclado.json'), JSON.stringify({ fecha: new Date().toISOString(), resultado: 'Login, navegación, selección y exportación de reporte completos con teclado, sin foco programático ni clicks. Reflow equivalente al 200% comprobado.', lectorPantalla: 'Pendiente de revisión humana con NVDA o VoiceOver' }, null, 2));
});


test('DIEP: formulario específico, edición, persistencia e inicio desde el listado', async ({ page }) => {
  await login(page, 'prevencion', 'Seed__PrevencionPassword');
  await page.getByRole('link', { name: 'Gestión de casos', exact: true }).click();
  await page.getByRole('button', { name: 'Registrar caso', exact: true }).click();
  await expect(page.getByLabel('Hora del accidente')).toHaveCount(0);
  await page.getByRole('dialog').getByRole('button', { name: /Enfermedad profesional/ }).click();
  await expect(page.getByLabel('Hora del accidente')).toHaveCount(0);
  await page.getByLabel('Fecha de inicio de las molestias').fill('2026-10-10');
  await page.getByRole('combobox', { name: 'Establecimiento', exact: true }).selectOption({ index: 1 });
  
  await page.getByLabel('Molestias o síntomas actuales').fill('Dolor de hombro durante tareas repetitivas de una actividad sintética.');
  await completarDenuncia(page, 'DIEP');
  await page.getByLabel('Mes y año de ingreso a la empresa', { exact: true }).fill('2024-08');
  await expect(page.getByText('2 años y 2 meses', { exact: true })).toBeVisible();
  await expect(page.getByLabel('Unidad de antigüedad', { exact: true })).toHaveCount(0);
  await page.getByLabel('Fecha de nacimiento', { exact: false }).fill('1990-10-11');
  await expect(page.getByText('35 años', { exact: true })).toBeVisible();
  await page.getByLabel('Fecha de nacimiento', { exact: false }).fill('1990-10-10');
  await expect(page.getByText('36 años', { exact: true })).toBeVisible();
  await axe(page, 'formulario-diep');
  for (const width of [360, 768, 1440]) { await page.setViewportSize({ width, height: 1000 }); await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true); }
  await page.screenshot({ path: path.join(evidence, 'formulario-diep.png'), fullPage: true });
  await page.getByRole('button', { name: 'Guardar caso', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Antecedentes del caso', exact: true })).toBeVisible();
  const codigo = await page.locator('h1').innerText();
  await page.getByText('Ver datos de la denuncia DIEP', { exact: true }).click();
  await expect(page.getByText('RUN del trabajador', { exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'Editar antecedentes', exact: true }).click();
  await expect(page.getByLabel('Mes y año de ingreso a la empresa', { exact: true })).toHaveValue('2024-08');
  await expect(page.getByText('2 años y 2 meses', { exact: true })).toBeVisible();
  await page.getByLabel('Parte del cuerpo afectada').fill('Hombro derecho');
  await page.getByRole('button', { name: 'Guardar caso', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Antecedentes del caso', exact: true })).toBeVisible();
  await page.getByText('Ver datos de la denuncia DIEP', { exact: true }).click(); await expect(page.getByText('Hombro derecho', { exact: true })).toBeVisible();
  await page.reload(); await page.getByText('Ver datos de la denuncia DIEP', { exact: true }).click();
  await expect(page.getByText('Hombro derecho', { exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'Volver al listado', exact: true }).click();
  await page.getByRole('searchbox', { name: 'Buscar', exact: true }).fill(codigo);
  await page.getByRole('button', { name: `Iniciar investigación del caso ${codigo}`, exact: true }).click();
  await expect(page.getByRole('heading', { name: /Investigación · GIA-/ })).toBeVisible();
  await page.getByText('Ver datos de la denuncia DIEP', { exact: true }).click();
  await expect(page.getByText('Hombro derecho', { exact: true })).toBeVisible();
});


test('Elegir tipo de caso en diálogo, cancelar y reutilizar datos guardados', async ({ page }) => {
  await login(page, 'prevencion', 'Seed__PrevencionPassword');
  await page.getByRole('link', { name: 'Gestión de casos', exact: true }).click();
  const crear = page.getByRole('button', { name: 'Registrar caso', exact: true });
  await crear.click(); await expect(page.getByRole('dialog')).toBeVisible();
  await axe(page, 'seleccion-tipo-caso'); await page.screenshot({ path: path.join(evidence, 'modal-tipo-caso.png') });
  await page.keyboard.press('Escape'); await expect(page.getByRole('dialog')).toHaveCount(0); await expect(crear).toBeFocused();
  await crear.click(); await page.getByRole('dialog').getByRole('button', { name: /Enfermedad profesional/ }).click();
  await expect(page.getByRole('radio')).toHaveCount(0); await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByRole('combobox', { name: 'Empleador guardado', exact: true })).toHaveCount(0);
  await expect(page.getByRole('combobox', { name: 'Trabajador', exact: true })).toHaveCount(0);
  await completarDenuncia(page, 'DIEP');
  await expect(page.getByLabel('RUT del empleador')).not.toHaveValue('');
  await expect(page.getByLabel('Nombre completo del trabajador')).not.toHaveValue('');
  await page.screenshot({ path: path.join(evidence, 'caso-datos-relacionados.png'), fullPage: true });
});

test('Datos de rúbrica: 600 casos nuevos y denuncias completas consultables', async ({ page }) => {
  await login(page, 'prevencion', 'Seed__PrevencionPassword');
  await page.getByRole('link', { name: 'Gestión de casos', exact: true }).click();
  await page.getByRole('searchbox', { name: 'Buscar', exact: true }).fill('RUB26-');
  await expect(page.getByText('600 casos encontrados', { exact: true })).toBeVisible();
  await page.screenshot({ path: path.join(evidence, 'casos-datos-rubrica.png'), fullPage: true });
  await page.getByRole('combobox', { name: 'Tipo de caso', exact: true }).selectOption('EnfermedadProfesional');
  await expect(page.getByText('180 casos encontrados', { exact: true })).toBeVisible();
  await page.getByRole('link', { name: /^Ver caso RUB26-/ }).first().click();
  await page.getByText('Ver datos de la denuncia DIEP', { exact: true }).click();
  await expect(page.getByText('RUT del empleador', { exact: true })).toBeVisible();
  await expect(page.getByText('Parte del cuerpo afectada', { exact: true })).toBeVisible();
});


function rutPrueba(numero: number) { const n = String(numero); let suma = 0, factor = 2; for (let i = n.length - 1; i >= 0; i--) { suma += Number(n[i]) * factor; factor = factor === 7 ? 2 : factor + 1; } const dv = 11 - suma % 11; return n + '-' + (dv === 11 ? '0' : dv === 10 ? 'K' : dv); }
test('Alta directa y búsqueda desde las mismas casillas de RUT', async ({ page }) => {
  await login(page, 'prevencion', 'Seed__PrevencionPassword');
  await page.getByRole('link', { name: 'Gestión de casos', exact: true }).click(); await page.getByRole('button', { name: 'Registrar caso', exact: true }).click(); await page.getByRole('dialog').getByRole('button', { name: /Accidente del trabajo/ }).click();
  const run = rutPrueba(70000000 + Date.now() % 900000), rut = rutPrueba(72000000 + Date.now() % 900000);
  await expect(page.getByRole('combobox', { name: 'Trabajador', exact: true })).toHaveCount(0); await expect(page.getByLabel('Empleador guardado')).toHaveCount(0);
  await page.getByLabel('Fecha del evento').fill('2026-10-20'); await page.getByRole('combobox', { name: 'Establecimiento', exact: true }).selectOption({ index: 1 });
  const centro = await page.getByRole('combobox', { name: 'Establecimiento', exact: true }).inputValue();
  await page.getByLabel('RUT del empleador').fill(rut); await page.getByLabel('RUN del trabajador').fill(run);
  await expect(page.getByText('No está registrado. Completa sus datos: se registrará al guardar el caso.', { exact: true })).toHaveCount(2);
  await page.getByLabel('Nombre o razón social').fill('Demo · Empresa registrada desde el caso'); await page.getByLabel('Nombre completo del trabajador').fill('Demo · Persona registrada desde el caso');
  await page.getByLabel('Relato de los hechos').fill('Caso de prueba sintético para registrar trabajador y empleador directamente desde sus RUT.');
  await completarDenuncia(page, 'DIAT', false); await axe(page, 'rut-alta-directa');
  await page.screenshot({ path: path.join(evidence, 'caso-alta-directa-rut.png'), fullPage: true });
  await page.getByRole('button', { name: 'Guardar caso', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Antecedentes del caso', exact: true })).toBeVisible();
  await page.goto('/#/casos/nuevo/Accidente'); await page.getByLabel('RUN del trabajador').fill(run);
  await expect(page.getByLabel('Nombre completo del trabajador')).toHaveValue('Demo · Persona registrada desde el caso'); await expect(page.getByRole('combobox', { name: 'Establecimiento', exact: true })).toHaveValue(centro);
  await page.getByLabel('RUT del empleador').fill(rut); await page.getByLabel('RUT del empleador').press('Enter');
  await expect(page.getByLabel('Nombre o razón social')).toHaveValue('Demo · Empresa registrada desde el caso');
});


test('Sugerencias mientras se escribe RUT sin botón, teclado y selección', async ({ page }) => {
  await login(page, 'prevencion', 'Seed__PrevencionPassword'); await page.goto('/#/casos/nuevo/Accidente');
  const trabajador = page.getByRole('combobox', { name: 'RUN del trabajador', exact: true });
  await expect(page.getByRole('button', { name: /Buscar trabajador por RUT|Buscar empleador por RUT/ })).toHaveCount(0);
  await trabajador.fill('50'); const lista = page.getByRole('listbox', { name: 'Sugerencias de trabajador' });
  await expect(lista.getByRole('option')).toHaveCount(8); await axe(page, 'rut-sugerencias');
  await page.screenshot({ path: path.join(evidence, 'rut-sugerencias.png'), fullPage: true });
  const primero = await lista.getByRole('option').first().innerText();
  await trabajador.press('ArrowDown'); await expect(lista.getByRole('option').first()).toHaveAttribute('aria-selected', 'true'); await trabajador.press('Enter');
  await expect(page.getByLabel('Nombre completo del trabajador')).toHaveValue(primero.split('\n')[1]);
  expect(await trabajador.evaluate(el => (el as HTMLInputElement).checkValidity())).toBe(true);
  const empleador = page.getByRole('combobox', { name: 'RUT del empleador', exact: true }); await empleador.fill('90');
  const empresas = page.getByRole('listbox', { name: 'Sugerencias de empleador' }); await expect(empresas.getByRole('option')).toHaveCount(6);
  await empleador.press('Escape'); await expect(empresas).toBeHidden(); await empleador.click(); await expect(empresas).toBeVisible();
  const empresa = await empresas.getByRole('option').first().innerText(); await empresas.getByRole('option').first().click();
  await expect(page.getByLabel('Nombre o razón social')).toHaveValue(empresa.split('\n')[1]);
});
