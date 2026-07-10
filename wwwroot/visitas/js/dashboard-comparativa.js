const COMP_API = (typeof API_BASE !== 'undefined') ? API_BASE : '/api/visitas';

let _compVista = { A: 'dia', B: 'dia' };
let _compChart = null;

// ===== INIT =====
document.addEventListener('DOMContentLoaded', () => {
    const hoyStr = formatearFechaISO(new Date());
    document.getElementById('compFechaA_dia').value = hoyStr;
    document.getElementById('compFechaB_dia').value = hoyStr;

    const semanaStr = semanaActualISO();
    document.getElementById('compFechaA_semana').value = semanaStr;
    document.getElementById('compFechaB_semana').value = semanaStr;

    const mesStr = mesActualISO();
    document.getElementById('compFechaA_mes').value = mesStr;
    document.getElementById('compFechaB_mes').value = mesStr;
});

function compCambiarVista(lado, vista) {
    _compVista[lado] = vista;
    document.querySelectorAll(`#compGranularidad${lado} .granularidad-tab`).forEach(btn => {
        btn.classList.toggle('activo', btn.dataset.vista === vista);
    });
    document.getElementById(`compFecha${lado}_dia`).style.display = vista === 'dia' ? '' : 'none';
    document.getElementById(`compFecha${lado}_semana`).style.display = vista === 'semana' ? '' : 'none';
    document.getElementById(`compFecha${lado}_mes`).style.display = vista === 'mes' ? '' : 'none';
}

function compRangoPeriodo(lado) {
    const vista = _compVista[lado];

    if (vista === 'dia') {
        const v = document.getElementById(`compFecha${lado}_dia`).value;
        if (!v) return null;
        const inicio = new Date(v + 'T00:00:00');
        const fin = new Date(inicio); fin.setDate(fin.getDate() + 1);
        return { inicio, fin, label: inicio.toLocaleDateString('es-GT', { day: '2-digit', month: 'short', year: 'numeric' }) };
    }

    if (vista === 'semana') {
        const v = document.getElementById(`compFecha${lado}_semana`).value;
        if (!v) return null;
        const inicio = semanaInputToMonday(v);
        const fin = new Date(inicio); fin.setDate(fin.getDate() + 7);
        const finVisible = new Date(fin); finVisible.setDate(finVisible.getDate() - 1);
        const corto = d => d.toLocaleDateString('es-GT', { day: '2-digit', month: 'short' });
        return { inicio, fin, label: `Semana ${corto(inicio)} – ${corto(finVisible)}` };
    }

    // mes
    const v = document.getElementById(`compFecha${lado}_mes`).value;
    if (!v) return null;
    const inicio = mesInputToFecha(v);
    const fin = new Date(inicio.getFullYear(), inicio.getMonth() + 1, 1);
    return { inicio, fin, label: inicio.toLocaleDateString('es-GT', { month: 'long', year: 'numeric' }) };
}

// Mapea la vista de comparativa (dia/semana/mes) a la vista que
// entiende /dashboard/actividad (hora/semana/mes)
function compVistaActividad(vista) {
    return vista === 'dia' ? 'hora' : vista;
}

async function compConsultar() {
    const periodoA = compRangoPeriodo('A');
    const periodoB = compRangoPeriodo('B');

    if (!periodoA || !periodoB) {
        mostrarToast('Selecciona ambos periodos.', 'warning');
        return;
    }

    const loading = document.getElementById('compLoading');
    if (loading) loading.style.display = 'flex';

    try {
        const vistaActA = compVistaActividad(_compVista.A);
        const vistaActB = compVistaActividad(_compVista.B);

        const [resA, resB, actA, actB] = await Promise.all([
            fetch(`${COMP_API}/dashboard/resumen-periodo?fechaInicio=${formatearFechaISO(periodoA.inicio)}&fechaFin=${formatearFechaISO(periodoA.fin)}`).then(r => r.json()),
            fetch(`${COMP_API}/dashboard/resumen-periodo?fechaInicio=${formatearFechaISO(periodoB.inicio)}&fechaFin=${formatearFechaISO(periodoB.fin)}`).then(r => r.json()),
            fetch(`${COMP_API}/dashboard/actividad?vista=${vistaActA}&fecha=${formatearFechaISO(periodoA.inicio)}`).then(r => r.json()),
            fetch(`${COMP_API}/dashboard/actividad?vista=${vistaActB}&fecha=${formatearFechaISO(periodoB.inicio)}`).then(r => r.json())
        ]);

        if (!resA.exitoso || !resB.exitoso) throw new Error(resA.mensaje || resB.mensaje || 'Error al consultar');

        compRenderResultados(periodoA, resA.data, periodoB, resB.data);

        if (actA.exitoso && actB.exitoso) {
            compRenderChart(periodoA, actA.data, periodoB, actB.data);
        }
    } catch (err) {
        console.error('Error al comparar periodos:', err);
        mostrarToast('No se pudo comparar los periodos.', 'error');
    } finally {
        if (loading) loading.style.display = 'none';
    }
}

// ===== Gráfica de línea doble (Periodo A vs Periodo B) =====
function compRenderChart(periodoA, datosA, periodoB, datosB) {
    const bucketsA = datosA.buckets ?? [];
    const bucketsB = datosB.buckets ?? [];

    const baseLabels = bucketsA.length >= bucketsB.length ? bucketsA : bucketsB;
    const maxLen = baseLabels.length;
    const labels = baseLabels.map(b => b.label);

    const dataA = Array.from({ length: maxLen }, (_, i) => bucketsA[i] ? bucketsA[i].total : null);
    const dataB = Array.from({ length: maxLen }, (_, i) => bucketsB[i] ? bucketsB[i].total : null);

    const ctx = document.getElementById('compChart');
    if (!ctx) return;

    if (_compChart) {
        _compChart.destroy();
        _compChart = null;
    }

    _compChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels,
            datasets: [
                {
                    label: periodoA.label,
                    data: dataA,
                    borderColor: 'rgba(59,130,246,1)',
                    backgroundColor: 'rgba(59,130,246,0.08)',
                    fill: false,
                    tension: 0.3,
                    borderWidth: 2.5,
                    pointRadius: 2.5,
                    pointBackgroundColor: 'rgba(59,130,246,1)',
                    spanGaps: true
                },
                {
                    label: periodoB.label,
                    data: dataB,
                    borderColor: 'rgba(124,58,237,1)',
                    backgroundColor: 'rgba(124,58,237,0.08)',
                    fill: false,
                    tension: 0.3,
                    borderWidth: 2.5,
                    pointRadius: 2.5,
                    pointBackgroundColor: 'rgba(124,58,237,1)',
                    spanGaps: true
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: { position: 'bottom', labels: { color: '#6b7280', padding: 14, usePointStyle: true } }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: { precision: 0, color: '#6b7280' },
                    grid: { color: 'rgba(107,114,128,0.15)' }
                },
                x: {
                    ticks: { color: '#6b7280', maxRotation: 0, autoSkip: true, autoSkipPadding: 16 },
                    grid: { display: false }
                }
            }
        }
    });

    if (window.lucide) lucide.createIcons();
}

// ===== Resumen en texto (tarjetas) ===== 
function compRenderResultados(periodoA, datosA, periodoB, datosB) {
    document.getElementById('compTituloA').textContent = periodoA.label;
    document.getElementById('compTituloB').textContent = periodoB.label;

    document.getElementById('compResumenA').innerHTML = compResumenHTML(datosA);
    document.getElementById('compResumenB').innerHTML = compResumenHTML(datosB);

    document.getElementById('compPanel').style.display = 'block';

    const diff = (datosB.ingresoTotal ?? 0) - (datosA.ingresoTotal ?? 0);
    const pct = (datosA.ingresoTotal ?? 0) > 0 ? (diff / datosA.ingresoTotal * 100) : (diff > 0 ? 100 : 0);
    const deltaEl = document.getElementById('compDelta');
    deltaEl.classList.remove('positivo', 'negativo', 'neutro');

    if (Math.abs(diff) < 0.01) {
        deltaEl.classList.add('neutro');
        deltaEl.innerHTML = `<i data-lucide="minus"></i> Ingresos iguales entre ambos periodos`;
    } else if (diff > 0) {
        deltaEl.classList.add('positivo');
        deltaEl.innerHTML = `<i data-lucide="trending-up"></i> +$${diff.toFixed(2)} más en "${periodoB.label}" (${pct.toFixed(1)}%)`;
    } else {
        deltaEl.classList.add('negativo');
        deltaEl.innerHTML = `<i data-lucide="trending-down"></i> -$${Math.abs(diff).toFixed(2)} menos en "${periodoB.label}" (${pct.toFixed(1)}%)`;
    }

    if (window.lucide) lucide.createIcons();
}

function compResumenHTML(d) {
    const comp = d.composicion ?? {};
    return `
        <div class="ep-summary-row">
            <span class="ep-label"><i data-lucide="car" style="width:14px;"></i> Vehículos totales</span>
            <span class="ep-value">${d.totalVehiculos ?? 0}</span>
        </div>
        <div class="ep-summary-row" style="padding-left:20px; font-size:12px;">
            <span class="ep-label">Liviano</span><span class="ep-value">${comp.liviano ?? 0}</span>
        </div>
        <div class="ep-summary-row" style="padding-left:20px; font-size:12px;">
            <span class="ep-label">Moto</span><span class="ep-value">${comp.moto ?? 0}</span>
        </div>
        <div class="ep-summary-row" style="padding-left:20px; font-size:12px;">
            <span class="ep-label">Pesado</span><span class="ep-value">${comp.pesado ?? 0}</span>
        </div>
        <div class="ep-summary-row">
            <span class="ep-label"><i data-lucide="banknote" style="width:14px;"></i> Efectivo</span>
            <span class="ep-value">$${(d.totalEfectivo ?? 0).toFixed(2)}</span>
        </div>
        <div class="ep-summary-row">
            <span class="ep-label"><i data-lucide="credit-card" style="width:14px;"></i> Tarjeta</span>
            <span class="ep-value">$${(d.totalTarjeta ?? 0).toFixed(2)}</span>
        </div>
        <div class="ep-summary-row">
            <span class="ep-label"><i data-lucide="receipt" style="width:14px;"></i> Ticket promedio</span>
            <span class="ep-value">${d.ticketPromedio != null ? '$' + Number(d.ticketPromedio).toFixed(2) : '—'}</span>
        </div>
        <div class="ep-summary-total">
            <span class="ep-label">Ingreso total</span>
            <span class="ep-value">$${(d.ingresoTotal ?? 0).toFixed(2)}</span>
        </div>`;
}