// =============================================
// REPORTES DE VENTA — IOT_Vehiculos
// =============================================

const TODAS_COLUMNAS = [
    { key: 'Id', label: 'ID' },
    { key: 'Placa', label: 'Placa' },
    { key: 'CodigoBarras', label: 'Código Barras' },
    { key: 'FechaEntrada', label: 'Fecha Entrada' },
    { key: 'FechaSalida', label: 'Fecha Salida' },
    { key: 'Estado', label: 'Estado' },
    { key: 'bitPaid', label: 'Pagado' },
    { key: 'Monto', label: 'Monto' },
    { key: 'FechaPago', label: 'Fecha Pago' },
    { key: 'strRateKey', label: 'Tarifa' },
    { key: 'TiempoEstancia', label: 'Tiempo (min)' },
    { key: 'IdDispositivoEntrada', label: 'Disp. Entrada' },
    { key: 'IdDispositivoSalida', label: 'Disp. Salida' },
    { key: 'NombreOperador', label: 'Operador' },
    { key: 'UsuarioRegistro', label: 'Usuario Registro' },
    { key: 'bitEntry', label: 'bitEntry' },
    { key: 'bitExit', label: 'bitExit' },
    { key: 'IdEntryDevice', label: 'Entry Device' },
    { key: 'IdExitDevice', label: 'Exit Device' },
    { key: 'bitCopy', label: 'bitCopy' }
];

const COLUMNAS_DEFAULT = ['Placa', 'FechaEntrada', 'FechaSalida', 'Estado', 'Monto', 'FechaPago', 'strRateKey', 'TiempoEstancia', 'NombreOperador'];

let datosVentaActual = [];

// ===== INICIALIZACIÓN =====
document.addEventListener('DOMContentLoaded', () => {
    renderizarColumnas();
    cargarFavoritos();

    const hoy = new Date().toISOString().split('T')[0];
    const rvInicio = document.getElementById('rvFechaInicio');
    const rvFin = document.getElementById('rvFechaFin');
    if (rvInicio) rvInicio.value = hoy + 'T00:00';
    if (rvFin) rvFin.value = hoy + 'T23:59';
});

// ===== NAVEGACIÓN =====
function navegarA(pagina, elemento) {
    // Ocultar todas las páginas
    document.getElementById('pageBitacora').style.display = 'none';
    document.getElementById('pageReportesVenta').style.display = 'none';

    // Mostrar la página seleccionada
    if (pagina === 'bitacora') {
        document.getElementById('pageBitacora').style.display = 'block';
        document.getElementById('pageTitle').textContent = '📋 Bitácora de Visitas';
    } else if (pagina === 'reportes-venta') {
        document.getElementById('pageReportesVenta').style.display = 'block';
        document.getElementById('pageTitle').textContent = '📊 Reportes de Venta';
    }

    // Actualizar nav activo
    document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
    if (elemento) elemento.classList.add('active');
}

function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    sidebar.classList.toggle('open');
    sidebar.classList.toggle('collapsed');
}

// ===== COLUMNAS =====
function renderizarColumnas() {
    const grid = document.getElementById('columnasGrid');
    if (!grid) return;

    grid.innerHTML = TODAS_COLUMNAS.map(col => {
        const checked = COLUMNAS_DEFAULT.includes(col.key) ? 'checked' : '';
        return `<label class="columna-check ${checked ? 'checked' : ''}" id="colCheck_${col.key}">
            <input type="checkbox" value="${col.key}" ${checked} onchange="toggleColumnaCheck(this)">
            ${col.label}
        </label>`;
    }).join('');
}

function toggleColumnaCheck(checkbox) {
    const label = checkbox.closest('.columna-check');
    label.classList.toggle('checked', checkbox.checked);
}

function getColumnasSeleccionadas() {
    const checks = document.querySelectorAll('#columnasGrid input[type=checkbox]:checked');
    return Array.from(checks).map(c => c.value);
}

function seleccionarTodasColumnas() {
    document.querySelectorAll('#columnasGrid input[type=checkbox]').forEach(c => {
        c.checked = true;
        c.closest('.columna-check').classList.add('checked');
    });
}

function deseleccionarTodasColumnas() {
    document.querySelectorAll('#columnasGrid input[type=checkbox]').forEach(c => {
        c.checked = false;
        c.closest('.columna-check').classList.remove('checked');
    });
}

function aplicarColumnas(columnas) {
    document.querySelectorAll('#columnasGrid input[type=checkbox]').forEach(c => {
        const checked = columnas.includes(c.value);
        c.checked = checked;
        c.closest('.columna-check').classList.toggle('checked', checked);
    });
}

// ===== FAVORITOS (localStorage — persiste siempre) =====
function getFavoritos() {
    try {
        return JSON.parse(localStorage.getItem('rv_favoritos') || '[]');
    } catch { return []; }
}

function guardarFavorito() {
    const nombre = document.getElementById('nombreFavorito').value.trim();
    if (!nombre) { mostrarToast('Escriba un nombre para el favorito', 'error'); return; }

    const columnas = getColumnasSeleccionadas();
    if (columnas.length === 0) { mostrarToast('Seleccione al menos una columna', 'error'); return; }

    const favoritos = getFavoritos();
    // Si ya existe, reemplazar
    const idx = favoritos.findIndex(f => f.nombre === nombre);
    if (idx >= 0) {
        favoritos[idx].columnas = columnas;
    } else {
        favoritos.push({ nombre, columnas });
    }

    localStorage.setItem('rv_favoritos', JSON.stringify(favoritos));
    document.getElementById('nombreFavorito').value = '';
    cargarFavoritos();
    mostrarToast(`⭐ Favorito "${nombre}" guardado`, 'success');
}

function cargarFavoritos() {
    const lista = document.getElementById('favoritosList');
    if (!lista) return;

    const favoritos = getFavoritos();
    if (favoritos.length === 0) {
        lista.innerHTML = '<span style="font-size:12px;color:var(--gray-400);">No hay favoritos guardados</span>';
        return;
    }

    lista.innerHTML = favoritos.map((f, i) =>
        `<div class="favorito-chip" onclick="aplicarFavorito(${i})">
            ⭐ ${f.nombre} <span class="fav-delete" onclick="event.stopPropagation();eliminarFavorito(${i})">✕</span>
        </div>`
    ).join('');
}

function aplicarFavorito(index) {
    const favoritos = getFavoritos();
    if (favoritos[index]) {
        aplicarColumnas(favoritos[index].columnas);
        mostrarToast(`Favorito "${favoritos[index].nombre}" aplicado`, 'success');
    }
}

function eliminarFavorito(index) {
    const favoritos = getFavoritos();
    const nombre = favoritos[index]?.nombre;
    favoritos.splice(index, 1);
    localStorage.setItem('rv_favoritos', JSON.stringify(favoritos));
    cargarFavoritos();
    mostrarToast(`Favorito "${nombre}" eliminado`, 'warning');
}

// ===== BUSCAR REPORTE =====
async function buscarReporteVenta() {
    const columnas = getColumnasSeleccionadas();
    if (columnas.length === 0) { mostrarToast('Seleccione al menos una columna', 'error'); return; }

    const fechaInicioRaw = document.getElementById('rvFechaInicio').value;
    if (!fechaInicioRaw) { mostrarToast('Seleccione fecha de inicio', 'error'); return; }

    const fechaFinRaw = document.getElementById('rvFechaFin').value || fechaInicioRaw;
    const campoFecha = document.getElementById('rvCampoFecha').value;
    const estado = document.getElementById('rvEstado').value;
    const pagado = document.getElementById('rvPagado').value;
    const placa = document.getElementById('rvPlaca').value.trim();
    const tarifa = document.getElementById('rvTarifa').value;
    const top = parseInt(document.getElementById('rvTop').value) || 500;

    // Para datetime-local el valor ya viene como "2025-03-10T08:00"
    // Si solo tiene fecha sin hora, agregar hora por defecto
    // Longitud 10 = "YYYY-MM-DD" (solo fecha), 16 = "YYYY-MM-DDTHH:mm" (sin segundos)
    let fechaInicio = fechaInicioRaw;
    if (fechaInicio.length === 10) fechaInicio += 'T00:00:00';
    else if (fechaInicio.length === 16) fechaInicio += ':00';

    let fechaFin = fechaFinRaw;
    if (fechaFin.length === 10) fechaFin += 'T23:59:59';
    else if (fechaFin.length === 16) fechaFin += ':59';

    const body = {
        columnas,
        campoFecha,
        fechaInicio,
        fechaFin,
        estado: estado || null,
        placa: placa || null,
        soloPagados: pagado === '' ? null : pagado === 'true',
        strRateKey: tarifa || null,
        top
    };

    try {
        const res = await fetch(`${API_BASE}/reporte-vehiculos`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        const data = await res.json();

        if (data.exitoso && data.data) {
            datosVentaActual = data.data;
            renderizarTablaVenta(data.data, columnas);
            mostrarToast(`${data.data.length} registros encontrados`, 'success');
        } else {
            mostrarToast(data.mensaje || 'Sin resultados', 'warning');
        }
    } catch (err) {
        mostrarToast('Error de conexión', 'error');
    }
}

function renderizarTablaVenta(datos, columnas) {
    const seccion = document.getElementById('rvResultados');
    const thead = document.getElementById('rvThead');
    const tbody = document.getElementById('rvTbody');
    const conteo = document.getElementById('rvConteo');
    const resumen = document.getElementById('rvResumen');

    // Headers
    thead.innerHTML = columnas.map(c => {
        const col = TODAS_COLUMNAS.find(tc => tc.key === c);
        return `<th>${col ? col.label : c}</th>`;
    }).join('');

    // Rows
    tbody.innerHTML = datos.map(row => {
        return '<tr>' + columnas.map(c => {
            let val = row[c] ?? row[c.toLowerCase()] ?? '';
            // Formatear valores
            if ((c === 'FechaEntrada' || c === 'FechaSalida' || c === 'FechaPago') && val) {
                val = new Date(val).toLocaleString('es-GT', { dateStyle: 'short', timeStyle: 'short' });
            }
            if (c === 'Monto' && val !== '' && val !== null) val = '$' + parseFloat(val).toFixed(2);
            if (c === 'bitPaid') val = val == 1 ? '✅ Sí' : '❌ No';
            if (c === 'Estado') val = val === 'DENTRO' ? '🟢 DENTRO' : val === 'SALIO' ? '🔴 SALIO' : val;
            return `<td>${val ?? ''}</td>`;
        }).join('') + '</tr>';
    }).join('');

    conteo.textContent = `${datos.length} registros`;

    // Resumen
    let totalMonto = 0, totalPagados = 0, totalDentro = 0;
    datos.forEach(r => {
        const monto = parseFloat(r['Monto'] ?? r['monto'] ?? 0);
        if (!isNaN(monto)) totalMonto += monto;
        if (r['bitPaid'] == 1 || r['bitpaid'] == 1) totalPagados++;
        if (r['Estado'] === 'DENTRO' || r['estado'] === 'DENTRO') totalDentro++;
    });

    resumen.innerHTML = `
        <div class="resumen-card"><div class="rc-label">Total Registros</div><div class="rc-value">${datos.length}</div></div>
        <div class="resumen-card"><div class="rc-label">Monto Total</div><div class="rc-value">$${totalMonto.toFixed(2)}</div></div>
        <div class="resumen-card"><div class="rc-label">Pagados</div><div class="rc-value">${totalPagados}</div></div>
        <div class="resumen-card"><div class="rc-label">Dentro</div><div class="rc-value">${totalDentro}</div></div>
    `;

    seccion.style.display = 'block';
    seccion.scrollIntoView({ behavior: 'smooth' });
}

// ===== DESCARGAR EXCEL VENTA =====
function descargarExcelVenta() {
    if (!datosVentaActual.length) { mostrarToast('Primero busque datos', 'error'); return; }
    const columnas = getColumnasSeleccionadas();

    let xml = '<?xml version="1.0" encoding="UTF-8"?>\n<?mso-application progid="Excel.Sheet"?>\n';
    xml += '<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">\n';
    xml += '<Styles><Style ss:ID="h"><Font ss:Bold="1" ss:Color="#FFFFFF"/><Interior ss:Color="#1F2937" ss:Pattern="Solid"/></Style>';
    xml += '<Style ss:ID="c"></Style></Styles>\n';
    xml += '<Worksheet ss:Name="Reporte Venta"><Table>\n';

    // Headers
    xml += '<Row>';
    columnas.forEach(c => {
        const col = TODAS_COLUMNAS.find(tc => tc.key === c);
        xml += `<Cell ss:StyleID="h"><Data ss:Type="String">${col ? col.label : c}</Data></Cell>`;
    });
    xml += '</Row>\n';

    // Data
    datosVentaActual.forEach(row => {
        xml += '<Row>';
        columnas.forEach(c => {
            let val = row[c] ?? row[c.toLowerCase()] ?? '';
            if (val === null) val = '';
            xml += `<Cell ss:StyleID="c"><Data ss:Type="String">${String(val).replace(/&/g, '&amp;').replace(/</g, '&lt;')}</Data></Cell>`;
        });
        xml += '</Row>\n';
    });

    xml += '</Table></Worksheet></Workbook>';

    const blob = new Blob([xml], { type: 'application/vnd.ms-excel' });
    const fecha = document.getElementById('rvFechaInicio').value.replace(/-/g, '');
    descargarBlob(blob, `Reporte_Venta_${fecha}.xls`);
    mostrarToast('📗 Excel descargado', 'success');
}

// ===== DESCARGAR PDF VENTA =====
function descargarPDFVenta() {
    if (!datosVentaActual.length) { mostrarToast('Primero busque datos', 'error'); return; }
    const columnas = getColumnasSeleccionadas();

    const html = `<!DOCTYPE html><html><head><meta charset="UTF-8"><title>Reporte de Venta</title>
    <style>@page{size:landscape;margin:10mm}body{font-family:Arial;font-size:11px;padding:15px}
    h1{font-size:16px;text-align:center;margin-bottom:10px}
    table{width:100%;border-collapse:collapse;font-size:10px}
    th{background:#1f2937;color:#fff;padding:6px;text-align:left;font-size:9px}
    td{padding:5px;border:1px solid #e5e7eb}tr:nth-child(even){background:#f9fafb}
    .footer{margin-top:15px;font-size:9px;color:#999;text-align:right}</style></head>
    <body><h1>REPORTE DE VENTA — DOBLE GEMINIS S.A. DE C.V.</h1>
    <p style="text-align:center;font-size:11px">Generado: ${new Date().toLocaleString('es-GT')} | ${datosVentaActual.length} registros</p>
    <table><thead><tr>${columnas.map(c => {
        const col = TODAS_COLUMNAS.find(tc => tc.key === c);
        return `<th>${col ? col.label : c}</th>`;
    }).join('')}</tr></thead><tbody>
    ${datosVentaActual.map(row => '<tr>' + columnas.map(c => {
        let v = row[c] ?? row[c.toLowerCase()] ?? '';
        return `<td>${v ?? ''}</td>`;
    }).join('') + '</tr>').join('')}
    </tbody></table>
    <div class="footer">Centro Panamericano de Ojos — Sistema de Control de Parqueo</div>
    <script>window.onload=()=>window.print()<\/script></body></html>`;

    const ventana = window.open('', '_blank');
    ventana.document.write(html);
    ventana.document.close();
    mostrarToast('📕 PDF generado', 'success');
}