import { createWorker, OEM } from 'tesseract.js';
import { imageSize } from 'image-size';
import { readFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);

let worker;
try {
  const image = await readFile(process.argv[2]);
  const { width, height, type } = imageSize(image);
  if (!['png', 'jpg'].includes(type) || !width || !height || width * height > 25000000)
    throw new Error('Imagen inválida o superior a 25 megapíxeles.');
  const { langPath } = require('@tesseract.js-data/spa');
  worker = await createWorker('spa', OEM.LSTM_ONLY, {
    langPath,
    gzip: true,
    cacheMethod: 'none',
    errorHandler: () => {}
  });
  const { data } = await worker.recognize(image);
  process.stdout.write(JSON.stringify({ texto: data.text.trim().slice(0, 30000), confianza: data.confidence }));
} catch {
  process.stderr.write('No se pudo reconocer la imagen con el modelo local.');
  process.exitCode = 1;
} finally {
  if (worker) await worker.terminate();
}

