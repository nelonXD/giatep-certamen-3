"""OCR local de bajo consumo. Solo devuelve texto; no conserva imágenes."""
import contextlib
import json
import os
import sys
from pathlib import Path

for name in ('OMP_NUM_THREADS', 'OPENBLAS_NUM_THREADS', 'MKL_NUM_THREADS', 'NUMEXPR_NUM_THREADS'):
    os.environ[name] = '1'
os.environ['PADDLE_PDX_DISABLE_MODEL_SOURCE_CHECK'] = 'True'
os.environ['HF_HUB_OFFLINE'] = '1'
os.environ['PYTHONIOENCODING'] = 'utf-8'


def main():
    with contextlib.redirect_stdout(sys.stderr):
        import cv2
        import numpy as np
        from PIL import Image
        from paddleocr import PaddleOCR
        cv2.setNumThreads(1)
        Image.MAX_IMAGE_PIXELS = 25000000
        with Image.open(sys.argv[1]) as image:
            if image.format not in ('PNG', 'JPEG') or image.width * image.height > 25000000:
                raise ValueError('Imagen inválida')
            image.load()
            image.thumbnail((1800, 1800))
            pixels = cv2.cvtColor(np.asarray(image.convert('RGB')), cv2.COLOR_RGB2BGR)
        root = Path(os.environ.get('GIATEP_PADDLE_MODELS', Path(__file__).parent / '.paddle-models'))
        det, rec = root / 'PP-OCRv5_mobile_det', root / 'latin_PP-OCRv5_mobile_rec'
        if not (det / 'inference.json').is_file() or not (rec / 'inference.json').is_file():
            raise RuntimeError('Faltan los modelos locales de PaddleOCR')
        ocr = PaddleOCR(text_detection_model_name=det.name, text_detection_model_dir=str(det),
                        text_recognition_model_name=rec.name, text_recognition_model_dir=str(rec),
                        use_doc_orientation_classify=False, use_doc_unwarping=False,
                        use_textline_orientation=False, device='cpu', enable_mkldnn=False, cpu_threads=1)
        result = list(ocr.predict(pixels))[0].json
        if isinstance(result, str):
            result = json.loads(result)
        data = result.get('res', result)
        scores = data['rec_scores']
        output = {'texto': '\n'.join(data['rec_texts']).strip()[:30000],
                  'confianza': sum(scores) / max(1, len(scores)) * 100}
    print(json.dumps(output, ensure_ascii=False))


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        sys.stderr.write(f"{type(error).__name__}: {error}\n")
        sys.stderr.write('No se pudo reconocer la imagen con PaddleOCR local.\n')
        sys.exit(1)
