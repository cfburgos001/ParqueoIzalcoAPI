const ACT_API = (typeof API_BASE !== 'undefined') ? API_BASE : '/api/visitas';

let _actChart = null;
let _actVistaActual = 'hora';

// ===== Helpers de fecha (compartidos con dashboard-comparativa.js) =====
function formatearFechaISO(d) {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}

function semanaActualISO() {
    const hoy = new Date();
    const d = new Date(Date.UTC(hoy.getFullYear(), hoy.getMonth(), hoy.getDate()));
    const dayNum = d.getUTCDay() || 7;
    d.setUTCDate(d.getUTCDate() + 4 - dayNum);
    const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
    const weekNo = Math.ceil((((d - yearStart) / 86400000) + 1) / 7);
    return `${d.getUTCFullYear()}-W${String(weekNo).padStart(2, '0')}`;
}

function mesActualISO() {
    const hoy = new Date();
    return `${hoy.getFullYear()}-${String(hoy.getMonth() + 1).padStart(2, '0')}`;
}

function semanaInputToMonday(weekStr) {
    const [yearStr, wStr] = weekStr.split('-W');
    const year = parseInt(yearStr, 10);
    const week = parseInt(wStr, 10);
    const simple = new Date(year, 0, 1 + (week - 1) * 7);
    const dow = simple.getDay();
    const monday = new Date(simple);
    if (dow <= 4) monday.setDate(simple.getDate() - dow + 1);
    else monday.setDate(simple.getDate() + 8 - dow);
    return monday;
}

function mesInputToFecha(monthStr) {
    const [year, month] = monthStr.split('-').map(Number);
    return new Date(year, month - 1, 1);
}

// ===== INIT =====
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('actFechaHora').value = formatearFechaISO(new Date());
    document.getElementById('actFechaSemana').value = semanaActualISO();
    document.getElementById('actFechaMes').value = mesActualISO();
    actConsultar();
});

function actCambiarVista(vista) {
    _actVistaActual = vista;
    document.querySelectorAll('#actGranularidad .granularidad-tab').forEach(btn => {
        btn.classList.toggle('activo', btn.dataset.vista === vista);
    });
    document.getElementById('actFechaWrapHora').style.display = vista === 'hora' ? '' : 'none';
    document.getElementById('actFechaWrapSemana').style.display = vista === 'semana' ? '' : 'none';
    document.getElementById('actFechaWrapMes').style.display = vista === 'mes' ? '' : 'none';
    actConsultar();
}

function actFechaAnclaActual() {
    if (_actVistaActual === 'hora') {
        return document.getElementById('actFechaHora').value || null;
    } else if (_actVistaActual === 'semana') {
        const v = document.getElementById('actFechaSemana').value;
        return v ? formatearFechaISO(semanaInputToMonday(v)) : null;
    } else {
        const v = document.getElementById('actFechaMes').value;
        return v ? formatearFechaISO(mesInputToFecha(v)) : null;
    }
}

async function actConsultar(silencioso = false) {
    const fechaAncla = actFechaAnclaActual();
    if (!fechaAncla) {
        if (!silencioso) mostrarToast('Selecciona una fecha válida.', 'warning');
        return;
    }

    const loading = document.getElementById('actLoading');
    if (loading && !silencioso) loading.style.display = 'flex';

    try {
        const res = await fetch(`${ACT_API}/dashboard/actividad?vista=${_actVistaActual}&fecha=${fechaAncla}`);
        const data = await res.json();
        if (!data.exitoso) throw new Error(data.mensaje || 'Error al consultar');
        actRenderChart(data.data);
    } catch (err) {
        console.error('Error al consultar actividad:', err);
        if (!silencioso) mostrarToast('No se pudo consultar la afluencia.', 'error');
    } finally {
        if (loading) loading.style.display = 'none';
    }
}

function actRenderChart(datos) {
    const buckets = datos.buckets ?? [];
    const labels = buckets.map(b => b.label);
    const totales = buckets.map(b => b.total);

    let picoIdx = -1, picoVal = -1;
    totales.forEach((t, i) => { if (t > picoVal) { picoVal = t; picoIdx = i; } });

    const badge = document.getElementById('actPicoBadge');
    if (picoVal > 0 && picoIdx >= 0) {
        const etiquetaVista = datos.vista === 'hora' ? 'Hora pico' : (datos.vista === 'semana' ? 'Día pico' : 'Día pico del mes');
        badge.innerHTML = `<i data-lucide="flame"></i> ${etiquetaVista}: ${labels[picoIdx]} con ${picoVal} vehículo${picoVal === 1 ? '' : 's'}`;
        badge.style.display = 'inline-flex';
    } else {
        badge.style.display = 'none';
    }

    const puntoRadios = totales.map((t, i) => (i === picoIdx && picoVal > 0) ? 6 : 2.5);
    const puntoColores = totales.map((t, i) => (i === picoIdx && picoVal > 0) ? 'rgba(245,158,11,1)' : 'rgba(99,102,241,1)');
    const puntoBordes = totales.map((t, i) => (i === picoIdx && picoVal > 0) ? 2 : 0);

    const ctx = document.getElementById('actChart');
    if (!ctx) return;

    if (_actChart) {
        _actChart.data.labels = labels;
        _actChart.data.datasets[0].data = totales;
        _actChart.data.datasets[0].pointRadius = puntoRadios;
        _actChart.data.datasets[0].pointBackgroundColor = puntoColores;
        _actChart.data.datasets[0].pointBorderWidth = puntoBordes;
        _actChart.update();
    } else {
        const ctx2d = ctx.getContext('2d');
        const gradient = ctx2d.createLinearGradient(0, 0, 0, 280);
        gradient.addColorStop(0, 'rgba(99,102,241,0.30)');
        gradient.addColorStop(1, 'rgba(99,102,241,0.02)');

        _actChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Entradas',
                    data: totales,
                    borderColor: 'rgba(99,102,241,1)',
                    backgroundColor: gradient,
                    fill: true,
                    tension: 0.35,
                    borderWidth: 2.5,
                    pointRadius: puntoRadios,
                    pointBackgroundColor: puntoColores,
                    pointBorderColor: '#fff',
                    pointBorderWidth: puntoBordes,
                    pointHoverRadius: 7
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: { legend: { display: false } },
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
    }

    if (window.lucide) lucide.createIcons();
}