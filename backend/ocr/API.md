# OCR.space

Proveedor predeterminado: OCR.space, motor 2 con detección automática de idioma, incluido español. Admite imágenes PNG/JPG de hasta 1 MB (1000000 bytes). El flujo sigue siendo imagen → texto revisable → vista previa → Word confirmado.

1. Obtener una clave gratuita en https://ocr.space/ocrapi/freekey.
2. En backend/.env reemplazar Ocr__Engine por OcrSpace y Ocr__ApiKey por la nueva clave. No duplicar variables ni subir secretos a Git.
3. Reiniciar la API y probar con backend/ocr/test-fixtures/documento.png.

Las imágenes se envían a OCR.space: usar únicamente datos sintéticos en la evaluación. La imagen y el Word revisado se guardan juntos al confirmar; el Word identifica el original por nombre e ID. Antes de confirmar, el escaneo no guarda evidencias. El proveedor no entrega porcentaje de confianza; el valor 0 del contrato significa no informado, no precisión nula.

La clave viaja en una cabecera redactada en los logs de HttpClient. Hay un límite de espera de 45 segundos y no se reintenta automáticamente. Los fallos del proveedor se devuelven como Problem Details sin exponer su respuesta. Los resultados parciales se rechazan. Las pruebas simuladas no consumen cuota.

Plan gratuito: 25000 solicitudes mensuales, 500 diarias por IP y archivos de hasta 1 MB; sujeto a cambios del proveedor. No garantiza disponibilidad. Documentación y condiciones: https://ocr.space/ocrapi.

Google Cloud Vision sigue disponible con Ocr__Engine=GoogleVision y su clave correspondiente; requiere facturación habilitada. Paddle/Tesseract siguen disponibles como alternativas locales, pero por sí solos no evidencian el requisito de servicio externo. La interfaz limita el escaneo a 1 MB para todos los motores.

## Distribución del texto
Se solicita TextOverlay a OCR.space. La edición y la vista previa permiten corregir cada línea en su posición. El Word usa marcos de texto editables anclados a la página; las notas y la referencia al original quedan en una segunda página. Los márgenes se estiman a partir de las coordenadas detectadas. No se reconstruyen tipografías, logos, firmas ni sellos. Si el proveedor no entrega posiciones, se mantiene la transcripción normal. Cambiar al texto libre y editarlo descarta las coordenadas para evitar desalinear el contenido.


## Recortes de elementos gráficos
En el modo de distribución se pueden marcar hasta 12 zonas de la imagen para incluir logos, firmas, sellos o QR. La selección es manual, con rectángulo o coordenadas porcentuales; no se usa otra API. Se conservan los píxeles, fondo y texto superpuesto. Las zonas aparecen en la vista previa y se incorporan como imágenes recortadas en el Word. La imagen completa se incluye en el archivo Word como recurso interno: los recortes no son una herramienta de ocultación de datos. El borrador local conserva las selecciones al refrescar.

