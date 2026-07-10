// =============================================
// ESTACIONES DE PAGO — Dashboard
// Reutiliza el endpoint existente /api/visitas/reporte-vehiculos
// (mismo patrón que reportes-venta.js) para no duplicar lógica.
// =============================================

const EP_DEVICE_LABELS = { 5: 'PS_05', 6: 'PS_06' };
const EP_API = (typeof API_BASE !== 'undefined') ? API_BASE : '/api/visitas';

let _epChartSingle = null;
let _epChartA = null;
let _epChartB = null;

// ===== INIT =====
document.addEventListener('DOMContentLoaded', () => {
    const hoyStr = epFechaHoy();
    const inputA = document.getElementById('epFechaA');
    const inputB = document.getElementById('epFechaB');
    if (inputA) inputA.value = hoyStr;
    if (inputB) inputB.value = hoyStr;

    // Carga inicial automática con la fecha de hoy
    epConsultar();
});

function epFechaHoy() {
    const d = new Date();
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}

function epToggleComparar() {
    const activo = document.getElementById('epComparar').checked;
    document.getElementById('epFechaBWrap').style.display = activo ? '' : 'none';
    document.getElementById('epPanelSingle').style.display = activo ? 'none' : '';
    document.getElementById('epPanelCompare').style.display = activo ? 'grid' : 'none';
}

// ===== CONSULTA PRINCIPAL =====
async function epConsultar() {
    const comparar = document.getElementById('epComparar').checked;
    const fechaA = document.getElementById('epFechaA').value;
    const fechaB = document.getElementById('epFechaB').value;

    if (!fechaA) {
        mostrarToast('Selecciona una fecha.', 'warning');
        return;
    }
    if (comparar && !fechaB) {
        mostrarToast('Selecciona la fecha a comparar.', 'warning');
        return;
    }

    const loading = document.getElementById('epLoading');
    if (loading) loading.style.display = 'flex';

    try {
        if (!comparar) {
            const datos = await epFetchDia(fechaA);
            epRenderSingle(datos, fechaA);
        } else {
            const [datosA, datosB] = await Promise.all([epFetchDia(fechaA), epFetchDia(fechaB)]);
            epRenderCompare(datosA, datosB, fechaA, fechaB);
        }
    } catch (err) {
        console.error('Error al consultar estaciones de pago:', err);
        mostrarToast('No se pudo consultar las estaciones de pago.', 'error');
    } finally {
        if (loading) loading.style.display = 'none';
    }
}

// ===== FETCH + AGREGACIÓN =====
async function epFetchDia(fechaStr) {
    const body = {
        columnas: ['IdPayDevice', 'Monto', 'OperationType'],
        campoFecha: 'FechaPago',
        fechaInicio: fechaStr + 'T00:00:00',
        fechaFin: fechaStr + 'T23:59:59',
        soloPagados: true,
        top: 5000
    };

    const res = await fetch(`${EP_API}/reporte-vehiculos`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });
    const data = await res.json();

    if (!data.exitoso) throw new Error(data.mensaje || 'Error al consultar');

    return epAgregar(data.data || []);
}

function epAgregar(filas) {
    const porEstacion = {};
    let totalGeneral = 0, totalEfectivo = 0, totalTarjeta = 0, totalCortesia = 0;

    filas.forEach(f => {
        const idDisp = f['IdPayDevice'] ?? f['idPayDevice'] ?? null;
        const monto = parseFloat(f['Monto'] ?? f['monto'] ?? 0);
        const opType = parseInt(f['OperationType'] ?? f['operationType'] ?? 0);

        if (isNaN(monto) || monto <= 0 || !opType) return;

        const key = idDisp === null || idDisp === undefined ? 'sin_dispositivo' : idDisp;
        if (!porEstacion[key]) {
            porEstacion[key] = { idDisp, efectivo: 0, tarjeta: 0, cortesia: 0, total: 0 };
        }

        if (opType === 1) { porEstacion[key].efectivo += monto; totalEfectivo += monto; }
        else if (opType === 2) { porEstacion[key].tarjeta += monto; totalTarjeta += monto; }
        else if (opType === 3) { porEstacion[key].cortesia += monto; totalCortesia += monto; }

        porEstacion[key].total += monto;
        totalGeneral += monto;
    });

    return { porEstacion, totalGeneral, totalEfectivo, totalTarjeta, totalCortesia };
}

function epLabelEstacion(idDisp) {
    if (idDisp === null || idDisp === undefined) return 'Sin dispositivo';
    return EP_DEVICE_LABELS[idDisp] || `Estación #${idDisp}`;
}

// ===== VISTA SIMPLE (una fecha) =====
function epRenderSingle(datos, fechaStr) {
    const estaciones = Object.values(datos.porEstacion).sort((a, b) => b.total - a.total);
    const labels = estaciones.map(e => epLabelEstacion(e.idDisp));
    const dataEfectivo = estaciones.map(e => e.efectivo);
    const dataTarjeta = estaciones.map(e => e.tarjeta);

    const ctx = document.getElementById('epChartSingle');
    if (ctx) {
        if (_epChartSingle) {
            _epChartSingle.data.labels = labels;
            _epChartSingle.data.datasets[0].data = dataEfectivo;
            _epChartSingle.data.datasets[1].data = dataTarjeta;
            _epChartSingle.update();
        } else {
            _epChartSingle = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [
                        {
                            label: 'Efectivo',
                            data: dataEfectivo,
                            backgroundColor: 'rgba(59,130,246,0.8)',
                            borderColor: 'rgba(59,130,246,1)',
                            borderWidth: 1,
                            borderRadius: 6
                        },
                        {
                            label: 'Tarjeta',
                            data: dataTarjeta,
                            backgroundColor: 'rgba(124,58,237,0.8)',
                            borderColor: 'rgba(124,58,237,1)',
                            borderWidth: 1,
                            borderRadius: 6
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { position: 'bottom', labels: { color: '#6b7280', padding: 14 } }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: { color: '#6b7280', callback: v => '$' + v },
                            grid: { color: 'rgba(107,114,128,0.15)' }
                        },
                        x: { ticks: { color: '#6b7280' }, grid: { display: false } }
                    }
                }
            });
        }
    }

    document.getElementById('epResumenSingle').innerHTML = epRenderResumenHTML(datos, estaciones);
    if (window.lucide) lucide.createIcons();
}

// ===== VISTA COMPARATIVA (dos fechas) =====
function epRenderCompare(datosA, datosB, fechaA, fechaB) {
    document.getElementById('epTituloFechaA').textContent = epFormatearFecha(fechaA);
    document.getElementById('epTituloFechaB').textContent = epFormatearFecha(fechaB);

    _epChartA = epRenderDonut('epChartA', _epChartA, datosA);
    _epChartB = epRenderDonut('epChartB', _epChartB, datosB);

    const estacionesA = Object.values(datosA.porEstacion).sort((a, b) => b.total - a.total);
    const estacionesB = Object.values(datosB.porEstacion).sort((a, b) => b.total - a.total);

    document.getElementById('epResumenA').innerHTML = epRenderResumenHTML(datosA, estacionesA);
    document.getElementById('epResumenB').innerHTML = epRenderResumenHTML(datosB, estacionesB);

    // Delta
    const diff = datosB.totalGeneral - datosA.totalGeneral;
    const deltaEl = document.getElementById('epDelta');
    const pct = datosA.totalGeneral > 0 ? (diff / datosA.totalGeneral * 100) : (diff > 0 ? 100 : 0);

    deltaEl.classList.remove('positivo', 'negativo', 'neutro');
    if (Math.abs(diff) < 0.01) {
        deltaEl.classList.add('neutro');
        deltaEl.innerHTML = `<i data-lucide="minus"></i> Sin variación entre ambas fechas`;
    } else if (diff > 0) {
        deltaEl.classList.add('positivo');
        deltaEl.innerHTML = `<i data-lucide="trending-up"></i> +$${diff.toFixed(2)} más en ${epFormatearFecha(fechaB)} (${pct >= 0 ? '+' : ''}${pct.toFixed(1)}%)`;
    } else {
        deltaEl.classList.add('negativo');
        deltaEl.innerHTML = `<i data-lucide="trending-down"></i> -$${Math.abs(diff).toFixed(2)} menos en ${epFormatearFecha(fechaB)} (${pct.toFixed(1)}%)`;
    }

    if (window.lucide) lucide.createIcons();
}

function epRenderDonut(canvasId, instancia, datos) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return instancia;

    const dataVals = [datos.totalEfectivo, datos.totalTarjeta];

    if (instancia) {
        instancia.data.datasets[0].data = dataVals;
        instancia.update();
        return instancia;
    }

    return new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Efectivo', 'Tarjeta'],
            datasets: [{
                data: dataVals,
                backgroundColor: ['rgba(59,130,246,0.8)', 'rgba(124,58,237,0.8)'],
                borderColor: ['rgba(59,130,246,1)', 'rgba(124,58,237,1)'],
                borderWidth: 2,
                hoverOffset: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '65%',
            plugins: {
                legend: { position: 'bottom', labels: { color: '#6b7280', padding: 10, boxWidth: 10 } }
            }
        }
    });
}

// ===== HTML de resumen por estación =====
function epRenderResumenHTML(datos, estaciones) {
    if (estaciones.length === 0) {
        return `<div class="ep-empty"><i data-lucide="inbox"></i><span>Sin pagos registrados en esta fecha.</span></div>`;
    }

    let html = '';
    estaciones.forEach(e => {
        html += `
            <div class="ep-summary-row">
                <span class="ep-label"><i data-lucide="monitor" style="width:14px;"></i> ${epLabelEstacion(e.idDisp)}</span>
                <span class="ep-value">$${e.total.toFixed(2)}</span>
            </div>
            <div class="ep-summary-row" style="padding-left:20px; font-size:12px;">
                <span class="ep-label"><i data-lucide="banknote" style="width:12px;"></i> Efectivo</span>
                <span class="ep-value">$${e.efectivo.toFixed(2)}</span>
            </div>
            <div class="ep-summary-row" style="padding-left:20px; font-size:12px;">
                <span class="ep-label"><i data-lucide="credit-card" style="width:12px;"></i> Tarjeta</span>
                <span class="ep-value">$${e.tarjeta.toFixed(2)}</span>
            </div>`;
    });

    html += `
        <div class="ep-summary-total">
            <span class="ep-label">Total general</span>
            <span class="ep-value">$${datos.totalGeneral.toFixed(2)}</span>
        </div>`;

    if (datos.totalCortesia > 0) {
        html += `
            <div class="ep-summary-row" style="font-size:12px;">
                <span class="ep-label"><i data-lucide="gift" style="width:12px;"></i> Cortesías (no cobradas)</span>
                <span class="ep-value">$${datos.totalCortesia.toFixed(2)}</span>
            </div>`;
    }

    return html;
}

function epFormatearFecha(fechaStr) {
    const [y, m, d] = fechaStr.split('-');
    const fecha = new Date(y, m - 1, d);
    return fecha.toLocaleDateString('es-GT', { day: '2-digit', month: 'short', year: 'numeric' });
}