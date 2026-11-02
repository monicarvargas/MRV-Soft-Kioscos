// Poner en: wwwroot/js/descargas.js (del proyecto compartido)
// Se carga solo cuando hace falta (import dinámico desde el componente), no hay que tocar index.html.

// ---------------------------------------------------------------------------
// WORD: descarga un archivo de texto/HTML
// ---------------------------------------------------------------------------
export function descargarArchivo(nombre, mime, contenido) {
    const blob = new Blob(["\ufeff", contenido], { type: mime + ";charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = nombre;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// ---------------------------------------------------------------------------
// BINARIO (Excel .xlsx, etc.): llega en base64 desde C#
// ---------------------------------------------------------------------------
export function descargarBinario(nombre, mime, base64) {
    const bin = atob(base64);
    const bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    const blob = new Blob([bytes], { type: mime });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = nombre;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 2000);
}

// ---------------------------------------------------------------------------
// PDF: se arma en el navegador y se descarga directo (sin cuadro de impresión).
// Usa jsPDF + autoTable, que se cargan una sola vez, la primera vez que se pide un PDF.
// ---------------------------------------------------------------------------
const URL_JSPDF = "https://cdn.jsdelivr.net/npm/jspdf@2.5.1/dist/jspdf.umd.min.js";
const URL_AUTOTABLE = "https://cdn.jsdelivr.net/npm/jspdf-autotable@3.8.2/dist/jspdf.plugin.autotable.min.js";

let cargaLibrerias = null;

function cargarScript(src) {
    return new Promise((resolve, reject) => {
        const s = document.createElement("script");
        s.src = src;
        s.onload = () => resolve();
        s.onerror = () => reject(new Error("No se pudo cargar " + src + ". Revisá la conexión a internet."));
        document.head.appendChild(s);
    });
}

function cargarLibreriasPdf() {
    if (window.jspdf && window.jspdf.jsPDF) {
        // jsPDF ya está; si falta el plugin de tablas se carga igual
        if (typeof window.jspdf.jsPDF.API.autoTable === "function") return Promise.resolve();
    }

    if (!cargaLibrerias) {
        cargaLibrerias = (async () => {
            if (!(window.jspdf && window.jspdf.jsPDF)) await cargarScript(URL_JSPDF);
            if (typeof window.jspdf.jsPDF.API.autoTable !== "function") await cargarScript(URL_AUTOTABLE);
        })().catch(err => {
            cargaLibrerias = null;   // para que se pueda reintentar
            throw err;
        });
    }
    return cargaLibrerias;
}

// ---------------------------------------------------------------------------
// CARTELES DE PRECIOS en PDF (A4), mismo tamaño que la impresión.
// datos = { archivo, chico: bool, carteles: [{ nombre, precio, codigo, fecha }] }
// ---------------------------------------------------------------------------
export async function generarCartelesPdf(datos) {
    await cargarLibreriasPdf();
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ unit: "mm", format: "a4" });

    const chico = !!datos.chico;
    const cols = chico ? 3 : 2;
    const w = chico ? 63 : 95, h = chico ? 28 : 32, gap = 4;
    const fNombre = chico ? 8.5 : 11, fPrecio = chico ? 17 : 25, fPie = chico ? 6.5 : 7.5;
    const pt = 0.3528;   // 1 pt en mm
    const anchoPag = doc.internal.pageSize.getWidth(), altoPag = doc.internal.pageSize.getHeight();
    const x0 = (anchoPag - (cols * w + (cols - 1) * gap)) / 2;
    const y0 = 8;
    const filasPorPag = Math.floor((altoPag - 16 + gap) / (h + gap));
    const porPag = filasPorPag * cols;

    doc.setLineWidth(0.25);
    doc.setDrawColor(34);
    doc.setTextColor(0);

    datos.carteles.forEach((c, i) => {
        if (i > 0 && i % porPag === 0) doc.addPage();
        const k = i % porPag;
        const x = x0 + (k % cols) * (w + gap);
        const y = y0 + Math.floor(k / cols) * (h + gap);
        doc.rect(x, y, w, h);

        // nombre (hasta 2 renglones)
        doc.setFont("helvetica", "normal");
        doc.setFontSize(fNombre);
        const lineas = doc.splitTextToSize(c.nombre || "", w - 6).slice(0, 2);
        doc.text(lineas, x + 3, y + 2.5 + fNombre * pt);

        // precio: grande y centrado (se achica si no entra)
        doc.setFont("helvetica", "bold");
        let tam = fPrecio;
        doc.setFontSize(tam);
        while (tam > 8 && doc.getTextWidth(c.precio) > w - 6) { tam -= 1; doc.setFontSize(tam); }
        const altoNombre = 2.5 + lineas.length * fNombre * pt * 1.15;
        const yPrecio = y + altoNombre + (h - altoNombre - fPie * pt - 2) / 2 + tam * pt * 0.35;
        doc.text(c.precio || "", x + w / 2, yPrecio, { align: "center" });

        // pie: código y fecha
        doc.setFont("helvetica", "normal");
        doc.setFontSize(fPie);
        const yPie = y + h - 2;
        if (c.codigo) doc.text(c.codigo, x + w / 4, yPie, { align: "center" });
        if (c.fecha) doc.text(c.fecha, x + (w * 3) / 4, yPie, { align: "center" });
    });

    doc.save(datos.archivo);
}

// ---------------------------------------------------------------------------
// ETIQUETAS CON CÓDIGO DE BARRAS (PDF y Word), mismo tamaño que la impresión.
// datos = { archivo, grande, conNombre, conPrecio,
//           etiquetas: [{ nombre, texto, precio, modulos, barras: [[desde, ancho], ...], cantidad }] }
// Las barras se dibujan con 10 módulos blancos a cada lado (los lectores los necesitan).
// ---------------------------------------------------------------------------
function medidasEtiqueta(grande) {
    return grande
        ? { cols: 3, w: 63, h: 30, pad: 2, fNombre: 8.5, fNum: 8, fPrecio: 11, altoBarras: 12 }
        : { cols: 5, w: 38, h: 21, pad: 1.3, fNombre: 6.5, fNum: 6.5, fPrecio: 8, altoBarras: 9 };
}

function expandirEtiquetas(datos) {
    const lista = [];
    for (const e of datos.etiquetas) for (let i = 0; i < e.cantidad; i++) lista.push(e);
    return lista;
}

export async function generarEtiquetasPdf(datos) {
    await cargarLibreriasPdf();
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ unit: "mm", format: "a4" });
    const m = medidasEtiqueta(datos.grande);
    const pt = 0.3528;
    const anchoPag = doc.internal.pageSize.getWidth(), altoPag = doc.internal.pageSize.getHeight();
    const x0 = (anchoPag - m.cols * m.w) / 2, y0 = 8;
    const filas = Math.floor((altoPag - 16) / m.h);
    const porPag = filas * m.cols;

    expandirEtiquetas(datos).forEach((e, i) => {
        if (i > 0 && i % porPag === 0) doc.addPage();
        const k = i % porPag;
        const x = x0 + (k % m.cols) * m.w, y = y0 + Math.floor(k / m.cols) * m.h;

        // borde punteado para cortar
        doc.setDrawColor(187);
        doc.setLineWidth(0.2);
        doc.setLineDashPattern([0.8, 0.8], 0);
        doc.rect(x, y, m.w, m.h);
        doc.setLineDashPattern([], 0);
        doc.setTextColor(0);

        const altoNombre = datos.conNombre ? m.fNombre * pt * 1.2 : 0;
        const altoPie = Math.max(m.fNum, datos.conPrecio ? m.fPrecio : 0) * pt * 1.1;
        const altoTotal = altoNombre + m.altoBarras + 0.8 + altoPie;
        let yy = y + (m.h - altoTotal) / 2;

        if (datos.conNombre) {
            doc.setFont("helvetica", "bold");
            doc.setFontSize(m.fNombre);
            let nom = e.nombre || "";
            while (nom.length > 1 && doc.getTextWidth(nom) > m.w - m.pad * 2) nom = nom.slice(0, -1);
            if (nom !== (e.nombre || "")) nom = nom.slice(0, -1) + "…";
            doc.text(nom, x + m.w / 2, yy + m.fNombre * pt * 0.85, { align: "center" });
            yy += altoNombre;
        }

        // barras
        const total = e.modulos + 20;
        const anchoBarras = m.w - m.pad * 2;
        const mod = anchoBarras / total;
        doc.setFillColor(0);
        for (const [desde, ancho] of e.barras)
            doc.rect(x + m.pad + (desde + 10) * mod, yy, ancho * mod, m.altoBarras, "F");
        yy += m.altoBarras + 0.8;

        // número y precio
        const base = yy + altoPie * 0.8;
        doc.setFont("courier", "normal");
        doc.setFontSize(m.fNum);
        const anchoNum = doc.getTextWidth(e.texto);
        let anchoPrecio = 0;
        if (datos.conPrecio) { doc.setFont("helvetica", "bold"); doc.setFontSize(m.fPrecio); anchoPrecio = doc.getTextWidth(e.precio); }
        const sep = datos.conPrecio ? 2 : 0;
        let xx = x + (m.w - (anchoNum + sep + anchoPrecio)) / 2;
        doc.setFont("courier", "normal");
        doc.setFontSize(m.fNum);
        doc.text(e.texto, xx, base);
        if (datos.conPrecio) {
            doc.setFont("helvetica", "bold");
            doc.setFontSize(m.fPrecio);
            doc.text(e.precio, xx + anchoNum + sep, base);
        }
    });

    doc.save(datos.archivo);
}

/** Dibuja las barras en un PNG (base64, sin el encabezado data:). */
function barrasPng(e) {
    const escala = 4, alto = 160;
    const total = e.modulos + 20;
    const c = document.createElement("canvas");
    c.width = total * escala;
    c.height = alto;
    const g = c.getContext("2d");
    g.fillStyle = "#fff";
    g.fillRect(0, 0, c.width, c.height);
    g.fillStyle = "#000";
    for (const [desde, ancho] of e.barras) g.fillRect((desde + 10) * escala, 0, ancho * escala, alto);
    return c.toDataURL("image/png").split(",")[1];
}

function escaparHtml(s) {
    return String(s ?? "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

function partirBase64(b64) {
    return b64.replace(/(.{76})/g, "$1\r\n");
}

/**
 * Word (.doc): una página web "MHTML" con las imágenes de las barras adentro, que Word abre y deja modificar.
 */
export function generarEtiquetasWord(datos) {
    const m = medidasEtiqueta(datos.grande);
    const imagenes = new Map();   // texto del código -> { nombre, b64 }
    for (const e of datos.etiquetas) {
        if (!imagenes.has(e.texto)) imagenes.set(e.texto, { nombre: "barras" + (imagenes.size + 1) + ".png", b64: barrasPng(e) });
    }
    const base = "file:///C:/etiquetas/";

    let h = "<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word' xmlns='http://www.w3.org/TR/REC-html40'>";
    h += "<head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'><title>Codigos de barras</title><style>";
    h += "@page Section1 { size: 21cm 29.7cm; margin: 0.8cm; } div.Section1 { page: Section1; }";
    h += "table { border-collapse: collapse; }";
    h += `td.e { width: ${m.w}mm; height: ${m.h}mm; border: 0.5pt dashed #bbbbbb; padding: 0.5mm ${m.pad}mm; text-align: center; vertical-align: middle; font-family: Arial, sans-serif; }`;
    h += `p.n { margin: 0; font-size: ${m.fNombre}pt; font-weight: bold; text-transform: uppercase; }`;
    h += `p.b { margin: 0.3mm 0; } p.f { margin: 0; }`;
    h += `span.num { font-family: 'Courier New', monospace; font-size: ${m.fNum}pt; } span.pre { font-size: ${m.fPrecio}pt; font-weight: bold; }`;
    h += "</style></head><body><div class='Section1'><table cellspacing='0' cellpadding='0'>";

    const lista = expandirEtiquetas(datos);
    const anchoImg = m.w - m.pad * 2;
    for (let i = 0; i < lista.length; i += m.cols) {
        h += "<tr style='page-break-inside:avoid'>";
        for (const e of lista.slice(i, i + m.cols)) {
            const img = imagenes.get(e.texto);
            h += "<td class='e'>";
            if (datos.conNombre) h += `<p class='n'>${escaparHtml(e.nombre)}</p>`;
            h += `<p class='b'><img src='${base}${img.nombre}' width='${Math.round(anchoImg * 3.78)}' height='${Math.round(m.altoBarras * 3.78)}' style='width:${anchoImg}mm;height:${m.altoBarras}mm'></p>`;
            h += `<p class='f'><span class='num'>${escaparHtml(e.texto)}</span>`;
            if (datos.conPrecio) h += `&nbsp;&nbsp;<span class='pre'>${escaparHtml(e.precio)}</span>`;
            h += "</p></td>";
        }
        h += "</tr>";
    }
    h += "</table></div></body></html>";

    const limite = "----=_Parte_MRV_Etiquetas";
    const htmlB64 = btoa(unescape(encodeURIComponent(h)));
    let mht = "MIME-Version: 1.0\r\n";
    mht += `Content-Type: multipart/related; boundary="${limite}"; type="text/html"\r\n\r\n`;
    mht += `--${limite}\r\nContent-Type: text/html; charset="utf-8"\r\nContent-Transfer-Encoding: base64\r\nContent-Location: ${base}etiquetas.htm\r\n\r\n`;
    mht += partirBase64(htmlB64) + "\r\n";
    for (const img of imagenes.values()) {
        mht += `--${limite}\r\nContent-Type: image/png\r\nContent-Transfer-Encoding: base64\r\nContent-Location: ${base}${img.nombre}\r\n\r\n`;
        mht += partirBase64(img.b64) + "\r\n";
    }
    mht += `--${limite}--\r\n`;

    const blob = new Blob([mht], { type: "application/msword" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = datos.archivo;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 2000);
}

/**
 * datos = {
 *   archivo: "Pedido Ruta 25 2026-10-07.pdf",
 *   grupos: [{
 *     titulo: "Pedido a Ruta 25",
 *     columnas: [{ titulo, numerica, ancho }],
 *     pedidos: [{ titulo, fecha, filas: [[valor, valor, ...], ...] }]
 *   }]
 * }
 */
export async function generarPdf(datos) {
    await cargarLibreriasPdf();

    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ unit: "pt", format: "a4" });

    const margen = 40;
    const anchoUtil = doc.internal.pageSize.getWidth() - margen * 2;
    const altoPagina = doc.internal.pageSize.getHeight();

    let primerGrupo = true;

    for (const grupo of datos.grupos) {
        // Cada proveedor empieza en una página nueva
        if (!primerGrupo) doc.addPage();
        primerGrupo = false;

        let y = margen + 12;

        doc.setFont("helvetica", "bold");
        doc.setFontSize(18);
        doc.setTextColor(0);
        const titulo = doc.splitTextToSize(grupo.titulo, anchoUtil);
        doc.text(titulo, margen, y);
        y += titulo.length * 20;

        const columnStyles = {};
        grupo.columnas.forEach((c, i) => {
            columnStyles[i] = { halign: c.numerica ? "right" : "left" };
            if (c.ancho > 0) columnStyles[i].cellWidth = c.ancho;
        });

        for (const pedido of grupo.pedidos) {
            // Si queda poco lugar en la página, el título del listado pasa a la siguiente
            if (y > altoPagina - 140) {
                doc.addPage();
                y = margen;
            }

            y += 14;
            doc.setFont("helvetica", "bold");
            doc.setFontSize(13);
            doc.setTextColor(0);
            const tituloPedido = doc.splitTextToSize(pedido.titulo, anchoUtil);
            doc.text(tituloPedido, margen, y);
            y += (tituloPedido.length - 1) * 15 + 14;

            doc.setFont("helvetica", "normal");
            doc.setFontSize(9);
            doc.setTextColor(100);
            // "subtitulo" (opcional) reemplaza a "Fecha del pedido: ..." (ej: comprobante de fiado)
            const sub = (pedido.subtitulo !== undefined && pedido.subtitulo !== null) ? pedido.subtitulo : ("Fecha del pedido: " + pedido.fecha);
            if (sub) doc.text(sub, margen, y);
            doc.setTextColor(0);

            doc.autoTable({
                startY: y + 8,
                head: [grupo.columnas.map(c => c.titulo)],
                body: pedido.filas.length
                    ? pedido.filas
                    : [[{ content: "Sin productos", colSpan: grupo.columnas.length }]],
                theme: "grid",
                margin: { left: margen, right: margen },
                rowPageBreak: "avoid",
                styles: {
                    font: "helvetica",
                    fontSize: 10,
                    cellPadding: 4,
                    textColor: 0,
                    lineColor: [150, 150, 150],
                    lineWidth: 0.5
                },
                headStyles: { fillColor: [241, 243, 245], textColor: 0, fontStyle: "bold" },
                columnStyles: columnStyles,
                didParseCell: (data) => {
                    // El título de las columnas numéricas va alineado a la derecha, igual que los números
                    if (data.section === "head" && grupo.columnas[data.column.index]?.numerica) {
                        data.cell.styles.halign = "right";
                    }
                }
            });

            y = doc.lastAutoTable.finalY + 8;
        }
    }

    doc.save(datos.archivo);
}
