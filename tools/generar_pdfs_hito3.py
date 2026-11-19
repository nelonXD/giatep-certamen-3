import os
import subprocess
from markdown_it import MarkdownIt

def render_markdown(md_text):
    md = MarkdownIt("commonmark").enable("table")
    return md.render(md_text)

def generate_pdf(html_content, output_pdf_path, landscape=False):
    temp_html = output_pdf_path.replace(".pdf", "_temp.html")
    with open(temp_html, "w", encoding="utf-8") as f:
        f.write(html_content)

    url = "file:///" + os.path.abspath(temp_html).replace("\\", "/")
    abs_pdf = os.path.abspath(output_pdf_path)

    cmd = [
        r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        "--headless",
        "--disable-gpu",
        "--no-pdf-header-footer",
        f"--print-to-pdf={abs_pdf}",
        url
    ]
    res = subprocess.run(cmd, capture_output=True, text=True)
    if os.path.exists(temp_html):
        os.remove(temp_html)
    
    if os.path.exists(abs_pdf):
        print(f"[OK] Generado exitosamente: {output_pdf_path} ({os.path.getsize(abs_pdf):,} bytes)")
    else:
        print(f"[ERROR] No se pudo generar {output_pdf_path}: {res.stderr}")

def build_informe_tecnico():
    md_path = "docs/INFORME_TECNICO.md"
    with open(md_path, "r", encoding="utf-8") as f:
        md_text = f.read()

    body_html = render_markdown(md_text)

    css = """
    @page {
        size: letter;
        margin: 20mm 18mm 22mm 18mm;
        @bottom-right {
            content: counter(page);
        }
    }
    body {
        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
        color: #1e293b;
        background-color: #ffffff;
        font-size: 10.5pt;
        line-height: 1.55;
        margin: 0;
        padding: 0;
    }
    h1 {
        color: #0f172a;
        font-size: 20pt;
        font-weight: 700;
        border-bottom: 2.5px solid #0284c7;
        padding-bottom: 8px;
        margin-top: 0;
        margin-bottom: 6px;
    }
    h2 {
        color: #0369a1;
        font-size: 14pt;
        font-weight: 600;
        border-bottom: 1px solid #e2e8f0;
        padding-bottom: 5px;
        margin-top: 24px;
        margin-bottom: 12px;
        page-break-after: avoid;
    }
    h3 {
        color: #1e293b;
        font-size: 11.5pt;
        font-weight: 600;
        margin-top: 18px;
        margin-bottom: 8px;
        page-break-after: avoid;
    }
    h4 {
        color: #334155;
        font-size: 10.5pt;
        font-weight: 600;
        margin-top: 14px;
        margin-bottom: 6px;
        page-break-after: avoid;
    }
    p, li {
        margin-top: 4px;
        margin-bottom: 6px;
    }
    ul, ol {
        padding-left: 20px;
        margin-top: 4px;
        margin-bottom: 8px;
    }
    hr {
        border: 0;
        height: 1px;
        background: #cbd5e1;
        margin: 20px 0;
    }
    table {
        width: 100%;
        border-collapse: collapse;
        margin: 14px 0;
        font-size: 9pt;
        page-break-inside: avoid;
    }
    th, td {
        padding: 6px 10px;
        text-align: left;
        border: 1px solid #cbd5e1;
    }
    th {
        background-color: #f1f5f9;
        color: #0f172a;
        font-weight: 600;
    }
    tr:nth-child(even) {
        background-color: #f8fafc;
    }
    code {
        font-family: "Cascadia Code", Consolas, "Courier New", monospace;
        font-size: 8.8pt;
        background-color: #f1f5f9;
        color: #0369a1;
        padding: 1px 4px;
        border-radius: 3px;
        border: 1px solid #e2e8f0;
    }
    pre {
        background-color: #0f172a;
        color: #f8fafc;
        padding: 10px 14px;
        border-radius: 6px;
        overflow-x: auto;
        font-size: 8.5pt;
        line-height: 1.4;
        margin: 12px 0;
        page-break-inside: avoid;
    }
    pre code {
        background-color: transparent;
        color: inherit;
        border: none;
        padding: 0;
    }
    blockquote {
        margin: 12px 0;
        padding: 8px 16px;
        border-left: 4px solid #0284c7;
        background-color: #f0f9ff;
        color: #0c4a6e;
        border-radius: 0 4px 4px 0;
    }
    .badge {
        display: inline-block;
        padding: 2px 6px;
        font-size: 8pt;
        font-weight: 600;
        border-radius: 4px;
        background-color: #e0f2fe;
        color: #0369a1;
    }
    img {
        max-width: 100%;
        height: auto;
        border-radius: 4px;
        margin: 10px 0;
        border: 1px solid #e2e8f0;
    }
    """

    full_html = f"""<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8">
    <title>Informe Técnico de Ingeniería — Sistema GIATEP</title>
    <style>{css}</style>
</head>
<body>
{body_html}
</body>
</html>"""
    generate_pdf(full_html, "docs/INFORME_TECNICO.pdf")

def build_presentacion():
    md_path = "docs/PRESENTACION.md"
    with open(md_path, "r", encoding="utf-8") as f:
        md_text = f.read()

    slides = md_text.split("---")
    rendered_slides = []

    for i, slide in enumerate(slides):
        slide = slide.strip()
        if not slide:
            continue
        rendered_slide = render_markdown(slide)
        rendered_slides.append(f"""
        <div class="slide">
            <div class="slide-header">
                <span class="brand">GIATEP · Certamen 3 (Hito 3)</span>
                <span class="slide-num">Lámina {i+1} de {len(slides)}</span>
            </div>
            <div class="slide-content">
                {rendered_slide}
            </div>
        </div>
        """)

    css = """
    @page {
        size: 11in 8.5in landscape;
        margin: 12mm 15mm;
    }
    body {
        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
        color: #0f172a;
        background-color: #f8fafc;
        margin: 0;
        padding: 0;
    }
    .slide {
        page-break-after: always;
        height: 7.2in;
        display: flex;
        flex-direction: column;
        justify-content: flex-start;
        background: #ffffff;
        border-radius: 8px;
        box-sizing: border-box;
        padding: 16px 24px;
        border: 1px solid #e2e8f0;
    }
    .slide:last-child {
        page-break-after: avoid;
    }
    .slide-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        border-bottom: 2px solid #0284c7;
        padding-bottom: 8px;
        margin-bottom: 14px;
        font-size: 9pt;
        color: #64748b;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.5px;
    }
    .brand {
        color: #0369a1;
    }
    .slide-content h1 {
        font-size: 18pt;
        color: #0f172a;
        margin-top: 0;
        margin-bottom: 8px;
    }
    .slide-content h2 {
        font-size: 14pt;
        color: #0369a1;
        margin-top: 0;
        margin-bottom: 8px;
    }
    .slide-content h3 {
        font-size: 11.5pt;
        color: #1e293b;
        margin-top: 8px;
        margin-bottom: 6px;
    }
    .slide-content p {
        font-size: 10pt;
        line-height: 1.45;
        margin: 6px 0;
    }
    .slide-content ul {
        margin: 6px 0;
        padding-left: 20px;
    }
    .slide-content li {
        font-size: 9.8pt;
        line-height: 1.45;
        margin-bottom: 5px;
    }
    .slide-content table {
        width: 100%;
        border-collapse: collapse;
        font-size: 8.5pt;
        margin: 10px 0;
    }
    .slide-content th, .slide-content td {
        padding: 5px 8px;
        border: 1px solid #cbd5e1;
    }
    .slide-content th {
        background-color: #f1f5f9;
        font-weight: 600;
    }
    .slide-content pre {
        background-color: #0f172a;
        color: #f8fafc;
        padding: 8px 12px;
        border-radius: 6px;
        font-size: 8pt;
        line-height: 1.35;
        margin: 8px 0;
    }
    .slide-content code {
        font-family: Consolas, monospace;
        font-size: 8.5pt;
        background-color: #f1f5f9;
        color: #0369a1;
        padding: 1px 4px;
        border-radius: 3px;
    }
    .slide-content pre code {
        background-color: transparent;
        color: inherit;
    }
    """

    all_slides_html = "\n".join(rendered_slides)
    full_html = f"""<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8">
    <title>Defensa GIATEP — Certamen 3 (Hito 3)</title>
    <style>{css}</style>
</head>
<body>
{all_slides_html}
</body>
</html>"""
    generate_pdf(full_html, "docs/PRESENTACION.pdf", landscape=True)

if __name__ == "__main__":
    os.makedirs("docs", exist_ok=True)
    build_informe_tecnico()
    build_presentacion()
