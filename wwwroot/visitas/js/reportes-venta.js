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
    { key: 'OperationType', label: 'Tipo Pago' },   // ← NUEVO
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

const COLUMNAS_DEFAULT = [
    'Placa', 'FechaEntrada', 'FechaSalida', 'Estado',
    'Monto', 'FechaPago', 'strRateKey', 'OperationType',   // ← OperationType en default
    'TiempoEstancia', 'NombreOperador'
];

// Mapa de OperationType → texto legible
const OPERATION_TYPE_LABELS = {
    1: '💵 Efectivo',
    2: '💳 Tarjeta',
    3: '🎁 Cortesía'
};

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

// ===== FAVORITOS =====
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

    let fechaInicio = fechaInicioRaw;
    if (fechaInicio.length === 10) fechaInicio += 'T00:00:00';
    else if (fechaInicio.length === 16) fechaInicio += ':00';

    let fechaFin = fechaFinRaw;
    if (fechaFin.length === 10) fechaFin += 'T23:59:59';
    else if (fechaFin.length === 16) fechaFin += ':59';

    // Asegurarse de que OperationType siempre venga del backend para los totales
    const columnasConOp = columnas.includes('OperationType')
        ? columnas
        : [...columnas, 'OperationType'];

    const body = {
        columnas: columnasConOp,
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
            renderizarTablaVenta(data.data, columnas);   // mostrar solo columnas seleccionadas
            mostrarToast(`${data.data.length} registros encontrados`, 'success');
        } else {
            mostrarToast(data.mensaje || 'Sin resultados', 'warning');
        }
    } catch (err) {
        mostrarToast('Error de conexión', 'error');
    }
}

// ===== HELPER: formatear valor de celda =====
function formatearCelda(key, val) {
    if (val === null || val === undefined || val === '') return '';

    if (key === 'FechaEntrada' || key === 'FechaSalida' || key === 'FechaPago') {
        return new Date(val).toLocaleString('es-GT', { dateStyle: 'short', timeStyle: 'short' });
    }
    if (key === 'Monto') return '$' + parseFloat(val).toFixed(2);
    if (key === 'bitPaid') return val == 1 ? '✅ Sí' : '❌ No';
    if (key === 'Estado') return val === 'DENTRO' ? '🟢 DENTRO' : val === 'SALIO' ? '🔴 SALIO' : val;
    if (key === 'OperationType') return OPERATION_TYPE_LABELS[val] || `Tipo ${val}`;

    return val;
}

// ===== CALCULAR TOTALES =====
function calcularTotales(datos) {
    let totalGeneral = 0;
    let totalEfectivo = 0;
    let totalTarjeta = 0;
    let totalCortesia = 0;
    let countEfectivo = 0;
    let countTarjeta = 0;
    let countCortesia = 0;

    datos.forEach(r => {
        const monto = parseFloat(r['Monto'] ?? r['monto'] ?? 0);
        const opType = parseInt(r['OperationType'] ?? r['operationtype'] ?? 0);

        // Sumar si tiene monto y tiene tipo de operación válido
        if (!isNaN(monto) && monto > 0 && opType > 0) {
            totalGeneral += monto;
            if (opType === 1) { totalEfectivo += monto; countEfectivo++; }
            else if (opType === 2) { totalTarjeta += monto; countTarjeta++; }
            else if (opType === 3) { totalCortesia += monto; countCortesia++; }
        }
    });

    return {
        totalGeneral, totalEfectivo, totalTarjeta, totalCortesia,
        countEfectivo, countTarjeta, countCortesia
    };
}

// ===== RENDERIZAR TABLA =====
function renderizarTablaVenta(datos, columnas) {
    const seccion = document.getElementById('rvResultados');
    const thead = document.getElementById('rvThead');
    const tbody = document.getElementById('rvTbody');
    const conteo = document.getElementById('rvConteo');
    const resumen = document.getElementById('rvResumen');

    // Headers — solo las columnas seleccionadas por el usuario
    thead.innerHTML = columnas.map(c => {
        const col = TODAS_COLUMNAS.find(tc => tc.key === c);
        return `<th>${col ? col.label : c}</th>`;
    }).join('');

    // Rows
    tbody.innerHTML = datos.map(row => {
        return '<tr>' + columnas.map(c => {
            const val = row[c] ?? row[c.toLowerCase()] ?? '';
            return `<td>${formatearCelda(c, val)}</td>`;
        }).join('') + '</tr>';
    }).join('');

    conteo.textContent = `${datos.length} registros`;

    // ===== RESUMEN SIEMPRE PRESENTE =====
    const t = calcularTotales(datos);
    const totalDentro = datos.filter(r => r['Estado'] === 'DENTRO' || r['estado'] === 'DENTRO').length;
    const totalPagados = datos.filter(r => r['bitPaid'] == 1 || r['bitpaid'] == 1).length;

    resumen.innerHTML = `
        <!-- Fila 1: conteos -->
        <div class="resumen-card">
            <div class="rc-label">Total Registros</div>
            <div class="rc-value">${datos.length}</div>
        </div>
        <div class="resumen-card">
            <div class="rc-label">Pagados</div>
            <div class="rc-value">${totalPagados}</div>
        </div>
        <div class="resumen-card">
            <div class="rc-label">Dentro</div>
            <div class="rc-value">${totalDentro}</div>
        </div>

        <!-- Fila 2: montos -->
        <div class="resumen-card" style="border-top:3px solid #059669;">
            <div class="rc-label">💰 Monto Total Cobrado</div>
            <div class="rc-value" style="color:#059669;">$${t.totalGeneral.toFixed(2)}</div>
        </div>
        <div class="resumen-card" style="border-top:3px solid #1a56db;">
            <div class="rc-label">💵 Total Efectivo (${t.countEfectivo})</div>
            <div class="rc-value" style="color:#1a56db;">$${t.totalEfectivo.toFixed(2)}</div>
        </div>
        <div class="resumen-card" style="border-top:3px solid #7c3aed;">
            <div class="rc-label">💳 Total Tarjeta (${t.countTarjeta})</div>
            <div class="rc-value" style="color:#7c3aed;">$${t.totalTarjeta.toFixed(2)}</div>
        </div>
        <div class="resumen-card" style="border-top:3px solid #d97706;">
            <div class="rc-label">🎁 Total Cortesía (${t.countCortesia})</div>
            <div class="rc-value" style="color:#d97706;">$${t.totalCortesia.toFixed(2)}</div>
        </div>`;

    seccion.style.display = 'block';
    seccion.scrollIntoView({ behavior: 'smooth' });
}

// ===== DESCARGAR EXCEL =====
function descargarExcelVenta() {
    if (!datosVentaActual.length) { mostrarToast('Primero busque datos', 'error'); return; }
    const columnas = getColumnasSeleccionadas();

    // Asegurar que OperationType esté en los datos aunque no esté en columnas visibles
    const columnasExcel = columnas.includes('OperationType')
        ? columnas
        : [...columnas, 'OperationType'];

    const t = calcularTotales(datosVentaActual);

    let xml = '<?xml version="1.0" encoding="UTF-8"?>\n<?mso-application progid="Excel.Sheet"?>\n';
    xml += '<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">\n';
    xml += '<Styles>';
    xml += '<Style ss:ID="h"><Font ss:Bold="1" ss:Color="#FFFFFF"/><Interior ss:Color="#1F2937" ss:Pattern="Solid"/></Style>';
    xml += '<Style ss:ID="c"></Style>';
    xml += '<Style ss:ID="total"><Font ss:Bold="1"/><Interior ss:Color="#F3F4F6" ss:Pattern="Solid"/></Style>';
    xml += '<Style ss:ID="green"><Font ss:Bold="1" ss:Color="#065F46"/><Interior ss:Color="#D1FAE5" ss:Pattern="Solid"/></Style>';
    xml += '<Style ss:ID="blue"><Font ss:Bold="1" ss:Color="#1E3A8A"/><Interior ss:Color="#DBEAFE" ss:Pattern="Solid"/></Style>';
    xml += '<Style ss:ID="purple"><Font ss:Bold="1" ss:Color="#4C1D95"/><Interior ss:Color="#EDE9FE" ss:Pattern="Solid"/></Style>';
    xml += '<Style ss:ID="amber"><Font ss:Bold="1" ss:Color="#78350F"/><Interior ss:Color="#FEF3C7" ss:Pattern="Solid"/></Style>';
    xml += '</Styles>\n';

    xml += '<Worksheet ss:Name="Reporte Venta"><Table>\n';

    // Headers
    xml += '<Row>';
    columnasExcel.forEach(c => {
        const col = TODAS_COLUMNAS.find(tc => tc.key === c);
        xml += `<Cell ss:StyleID="h"><Data ss:Type="String">${col ? col.label : c}</Data></Cell>`;
    });
    xml += '</Row>\n';

    // Datos
    datosVentaActual.forEach(row => {
        xml += '<Row>';
        columnasExcel.forEach(c => {
            let val = row[c] ?? row[c.toLowerCase()] ?? '';
            // Texto legible para OperationType en Excel
            if (c === 'OperationType') {
                val = OPERATION_TYPE_LABELS[val] || (val !== '' ? `Tipo ${val}` : '');
            }
            if (val === null) val = '';
            xml += `<Cell ss:StyleID="c"><Data ss:Type="String">${String(val).replace(/&/g, '&amp;').replace(/</g, '&lt;')}</Data></Cell>`;
        });
        xml += '</Row>\n';
    });

    // Fila vacía separadora
    xml += '<Row></Row>\n';

    // ===== BLOQUE DE TOTALES =====
    xml += `<Row>
        <Cell ss:StyleID="green"><Data ss:Type="String">💰 Monto Total Cobrado</Data></Cell>
        <Cell ss:StyleID="green"><Data ss:Type="String">$${t.totalGeneral.toFixed(2)}</Data></Cell>
    </Row>\n`;
    xml += `<Row>
        <Cell ss:StyleID="blue"><Data ss:Type="String">💵 Total Efectivo (${t.countEfectivo} transacciones)</Data></Cell>
        <Cell ss:StyleID="blue"><Data ss:Type="String">$${t.totalEfectivo.toFixed(2)}</Data></Cell>
    </Row>\n`;
    xml += `<Row>
        <Cell ss:StyleID="purple"><Data ss:Type="String">💳 Total Tarjeta (${t.countTarjeta} transacciones)</Data></Cell>
        <Cell ss:StyleID="purple"><Data ss:Type="String">$${t.totalTarjeta.toFixed(2)}</Data></Cell>
    </Row>\n`;
    xml += `<Row>
        <Cell ss:StyleID="amber"><Data ss:Type="String">🎁 Total Cortesía (${t.countCortesia} transacciones)</Data></Cell>
        <Cell ss:StyleID="amber"><Data ss:Type="String">$${t.totalCortesia.toFixed(2)}</Data></Cell>
    </Row>\n`;

    xml += '</Table></Worksheet></Workbook>';

    const blob = new Blob([xml], { type: 'application/vnd.ms-excel' });
    const fecha = document.getElementById('rvFechaInicio').value.replace(/[-T:]/g, '').substring(0, 8);
    descargarBlob(blob, `Reporte_Venta_${fecha}.xls`);
    mostrarToast('📗 Excel descargado', 'success');
}

// ===== DESCARGAR PDF =====
function descargarPDFVenta() {
    if (!datosVentaActual.length) { mostrarToast('Primero busque datos', 'error'); return; }
    const columnas = getColumnasSeleccionadas();

    // Asegurar OperationType en datos para totales aunque no esté en columnas visibles
    const t = calcularTotales(datosVentaActual);

    // Columnas a mostrar en la tabla del PDF
    const columnasConOp = columnas.includes('OperationType')
        ? columnas
        : [...columnas, 'OperationType'];

    const html = `<!DOCTYPE html><html><head><meta charset="UTF-8">
    <title>Reporte de Venta</title>
    <style>
        @page { size:landscape; margin:10mm }
        body { font-family:Arial; font-size:11px; padding:15px }
        h1 { font-size:15px; text-align:center; margin-bottom:4px }
        h2 { font-size:11px; text-align:center; color:#555; margin-bottom:10px; font-weight:normal }
        table { width:100%; border-collapse:collapse; font-size:9px; margin-bottom:20px }
        th { background:#1f2937; color:#fff; padding:6px; text-align:left; font-size:8px }
        td { padding:5px; border:1px solid #e5e7eb }
        tr:nth-child(even) { background:#f9fafb }

        /* Bloque de totales */
        .totales { width:100%; border-collapse:collapse; margin-top:8px }
        .totales td { padding:8px 12px; font-size:11px; border-radius:4px }
        .tot-general { background:#d1fae5; color:#065f46; font-weight:bold; font-size:13px }
        .tot-efectivo { background:#dbeafe; color:#1e3a8a; font-weight:bold }
        .tot-tarjeta  { background:#ede9fe; color:#4c1d95; font-weight:bold }
        .tot-cortesia { background:#fef3c7; color:#78350f; font-weight:bold }
        .tot-label { width:60% }
        .tot-value { width:40%; text-align:right; font-size:14px }

        .footer { margin-top:15px; font-size:9px; color:#999; text-align:right }
    </style></head>
    <body>
        <h1>REPORTE DE VENTA — DOBLE GEMINIS S.A. DE C.V.</h1>
        <h2>Generado: ${new Date().toLocaleString('es-GT')} | ${datosVentaActual.length} registros</h2>

        <table>
            <thead><tr>
                ${columnasConOp.map(c => {
        const col = TODAS_COLUMNAS.find(tc => tc.key === c);
        return `<th>${col ? col.label : c}</th>`;
    }).join('')}
            </tr></thead>
            <tbody>
                ${datosVentaActual.map(row => '<tr>' + columnasConOp.map(c => {
        const val = row[c] ?? row[c.toLowerCase()] ?? '';
        return `<td>${formatearCelda(c, val)}</td>`;
    }).join('') + '</tr>').join('')}
            </tbody>
        </table>

        <!-- TOTALES -->
        <table class="totales">
            <tr>
                <td class="tot-label tot-general">💰 Monto Total Cobrado</td>
                <td class="tot-value tot-general">$${t.totalGeneral.toFixed(2)}</td>
            </tr>
            <tr>
                <td class="tot-label tot-efectivo">💵 Total Efectivo &nbsp;<small>(${t.countEfectivo} transacciones)</small></td>
                <td class="tot-value tot-efectivo">$${t.totalEfectivo.toFixed(2)}</td>
            </tr>
            <tr>
                <td class="tot-label tot-tarjeta">💳 Total Tarjeta &nbsp;<small>(${t.countTarjeta} transacciones)</small></td>
                <td class="tot-value tot-tarjeta">$${t.totalTarjeta.toFixed(2)}</td>
            </tr>
            <tr>
                <td class="tot-label tot-cortesia">🎁 Total Cortesía &nbsp;<small>(${t.countCortesia} transacciones)</small></td>
                <td class="tot-value tot-cortesia">$${t.totalCortesia.toFixed(2)}</td>
            </tr>
        </table>

        <div class="footer">Centro Panamericano de Ojos — Sistema de Control de Parqueo</div>
        <script>window.onload=()=>window.print()<\/script>
    </body></html>`;

    const ventana = window.open('', '_blank');
    ventana.document.write(html);
    ventana.document.close();
    mostrarToast('📕 PDF generado', 'success');
}