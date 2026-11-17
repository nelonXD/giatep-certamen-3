# Maquetas y guía de estilos

## Sistema visual

Paleta principal verde profundo #174C40 sobre fondos #F5F7F5 y blanco. Texto #233B35 y secundario #526959. Estados informativos azul, seguimiento o atraso ocre, cierre verde. El texto de estado acompaña siempre al color. Tipografía de sistema sin dependencia de fuentes externas. Tamaño base 15 px, encabezados adaptables, escala de espacios de aproximadamente 4, 8, 12, 16, 20, 24 y 32 px.

Componentes compartidos: Field, Select, TextArea, Message, Pagination, Loading, Empty y Bars. Los controles tienen foco visible, estado deshabilitado y mensajes textuales. Field y TextArea relacionan errores con aria-describedby y anuncian cambios mediante aria-live. Las confirmaciones de cierre y cumplimiento usan diálogos nativos del navegador.

## Organización por ancho

Escritorio: barra lateral fija, contenido central y gráficos en dos columnas. Tablet: tarjetas adaptadas y gráficos en una columna. Celular: navegación colapsable, formularios apilados, indicadores en dos columnas y tablas con desplazamiento interno. El diseño debe funcionar entre 360 y 1440 px. Los detalles/tablas de gráficos proporcionan una alternativa al SVG.

## Maquetas de pantallas

- Login: explicación breve del producto y formulario de cuenta/contraseña.
- Dashboard: título, filtros, actualización, seis indicadores, interpretación y cuatro gráficos con tablas.
- Casos: filtros de búsqueda, tipo, estado, centro y orden; tabla paginada y acción de registro para Prevención.
- Expediente: antecedentes, estado, acciones y secciones de investigación, plan, evidencias e historial.
- Reportes: tipo, fechas, centro, exportaciones, encabezado institucional, gráfico gerencial y tabla.
- Administración: usuarios, establecimientos, trabajadores y auditoría; formularios de alta/edición inline.

Las capturas exportadas en evidencias constituyen las referencias visuales de la implementación final. Los diagramas fuente y sus SVG se encuentran en docs/diagramas. Las maquetas son una descripción de la versión implementada, no evidencia de una fase previa que no se ha documentado.
