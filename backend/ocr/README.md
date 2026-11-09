# OCR local de GIATEP

La instalación local usa PaddleOCR 3.7.0 y PaddlePaddle 3.3.1 con los modelos `PP-OCRv5_mobile_det` y `latin_PP-OCRv5_mobile_rec`, que incluye español. Son modelos preentrenados; no aprenden de las imágenes subidas.

## Recursos y privacidad

Una lectura simultánea, un hilo por biblioteca, prioridad baja y afinidad a un núcleo en Windows. Las imágenes se reducen a un máximo de 1800 píxeles de lado para limitar recursos. La API cancela a los 45 segundos y elimina el archivo temporal. El proceso termina después de cada lectura y libera su memoria. No hay descargas ni llamadas a proveedores durante el reconocimiento.

En «Escanear con IA», seleccionar una imagen inicia su reconocimiento temporal. Solo se guarda como evidencia el Word que el usuario revisa y confirma. Los archivos adjuntados normalmente no se escanean automáticamente. Tesseract se conserva como alternativa configurable; no se cambia de motor silenciosamente.

## Instalación en Windows

Desde esta carpeta, con Python 3.12:

```powershell
python -m venv .venv-paddle
.venv-paddle/Scripts/python -m pip install -r requirements-paddle.txt
.venv-paddle/Scripts/python instalar_modelos.py
```

La instalación descarga los dos modelos oficiales una sola vez. Configura `Ocr__Engine=Paddle` en `backend/.env`. `Ocr__PythonPath` y `Ocr__ModelsPath` permiten usar otras ubicaciones. Para Tesseract, ejecuta `npm ci` y configura `Ocr__Engine=Tesseract`.

## Publicación y pruebas

Instala las dependencias de Python y modelos en el servidor antes de iniciar la API; el entorno virtual es específico del equipo y no se publica en Git. Conserva `scan_paddle.py` y `.paddle-models` junto a la API, o configura las rutas. El flujo existente de Node sigue probando y empaquetando la alternativa Tesseract; un despliegue Paddle requiere provisionar Python y estos modelos.

Las pruebas de backend incluyen lectura real con ambos motores, lectura temporal sin persistir la imagen, creación del Word, permisos y bloqueo de investigaciones cerradas. El entorno de Paddle es necesario para la prueba real de ese motor.
