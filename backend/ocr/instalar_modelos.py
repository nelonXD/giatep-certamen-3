"""Descarga explícita de los dos modelos; no procesa fotos."""
import os
from pathlib import Path
os.environ['PADDLE_PDX_DISABLE_MODEL_SOURCE_CHECK'] = 'True'
os.environ['PADDLE_PDX_CACHE_HOME'] = str(Path(__file__).resolve().parent / '.model-cache')
from paddleocr import PaddleOCR
import shutil
PaddleOCR(text_detection_model_name='PP-OCRv5_mobile_det', text_recognition_model_name='latin_PP-OCRv5_mobile_rec', use_doc_orientation_classify=False, use_doc_unwarping=False, use_textline_orientation=False, enable_mkldnn=False, cpu_threads=1)
root = Path(__file__).resolve().parent
for name in ('PP-OCRv5_mobile_det', 'latin_PP-OCRv5_mobile_rec'):
    shutil.copytree(root / '.model-cache' / 'official_models' / name, root / '.paddle-models' / name, dirs_exist_ok=True)
print('Modelos locales preparados.')
