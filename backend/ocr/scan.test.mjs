import { test } from 'node:test';
import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { fileURLToPath } from 'node:url';
const run = promisify(execFile);
const script = fileURLToPath(new URL('./scan.mjs', import.meta.url));

test('el modelo local reconoce una evidencia en español', { timeout: 45000 }, async () => {
  const file = fileURLToPath(new URL('./test-fixtures/documento.png', import.meta.url));
  const { stdout } = await run(process.execPath, [script, file], { timeout: 40000 });
  const lectura = JSON.parse(stdout);
  assert.match(lectura.texto, /pasillo estaba obstruido con cajas/i);
  assert.match(lectura.texto, /08-11-2026/);
  assert.ok(lectura.confianza > 60);
});

test('rechaza contenido que no es una imagen', async () => {
  await assert.rejects(run(process.execPath, [script, script]), error => {
    assert.equal(error.code, 1);
    assert.equal(error.stdout, '');
    assert.match(error.stderr, /No se pudo reconocer/);
    return true;
  });
});
