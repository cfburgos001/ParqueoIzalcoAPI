// =============================================
// CERRAR TICKETS ANTIGUOS
// Solo visible/accesible para ADMINISTRADOR
// =============================================

const API_TICKETS = '/api/tickets';
let ticketsAntiguosData = [];     // todos los tickets cargados
let ticketsSeleccionados = [];    // IDs seleccionados con checkbox

// ===== CARGAR TICKETS =====
async function cargarTicketsAntiguos() {
    const tbody = document.getElementById('tbodyTicketsAntiguos');
    const conteo = document.getElementById('ctConteo');
    if (!tbody) return;

    tbody.innerHTML = `<tr><td colspan="10" class="empty-row"><i data-lucide="loader" style="width:16px; animation: spin 2s linear infinite;"></i> Cargando…</td></tr>`;
    if (window.lucide) lucide.createIcons({ root: tbody });

    ticketsSeleccionados = [];
    actualizarBotonMasivo();

    try {
        const res = await fetch(`${API_TICKETS}/antiguos`);
        const data = await res.json();

        if (!data.exitoso) {
            tbody.innerHTML = `<tr><td colspan="10" class="empty-row" style="color:var(--danger);">
                <i data-lucide="x-circle" style="width:16px;"></i> Error al cargar: ${data.mensaje}</td></tr>`;
            if (window.lucide) lucide.createIcons({ root: tbody });
            return;
        }

        ticketsAntiguosData = data.data || [];
        conteo.textContent = `${ticketsAntiguosData.length} pendientes`;

        if (ticketsAntiguosData.length === 0) {
            tbody.innerHTML = `<tr><td colspan="10" class="empty-row">
                <i data-lucide="check-circle" style="width:16px; color:var(--success);"></i> No hay tickets antiguos pendientes</td></tr>`;
            if (window.lucide) lucide.createIcons({ root: tbody });
            return;
        }

        renderizarTablaTickets();

    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="10" class="empty-row" style="color:var(--danger);">
            <i data-lucide="wifi-off" style="width:16px;"></i> Error de conexión</td></tr>`;
        if (window.lucide) lucide.createIcons({ root: tbody });
        mostrarToast('Error al cargar tickets antiguos', 'error');
    }
}

// ===== RENDERIZAR TABLA =====
function renderizarTablaTickets() {
    const tbody = document.getElementById('tbodyTicketsAntiguos');
    if (!tbody) return;

    tbody.innerHTML = ticketsAntiguosData.map(t => {
        const fechaStr = new Date(t.fechaEntrada).toLocaleString('es-GT', {
            day: '2-digit', month: '2-digit', year: 'numeric',
            hour: '2-digit', minute: '2-digit'
        });

        const diasColor = t.diasAdentro > 7
            ? 'color:#b91c1c;font-weight:700;'
            : t.diasAdentro > 3
                ? 'color:#d97706;font-weight:700;'
                : 'color:var(--gray-700);';

        const rateLabel = {
            'A': '<i data-lucide="car" style="width:14px; display:inline-block; vertical-align:middle;"></i> Auto',
            'M': '<i data-lucide="bike" style="width:14px; display:inline-block; vertical-align:middle;"></i> Moto',
            'C': '<i data-lucide="truck" style="width:14px; display:inline-block; vertical-align:middle;"></i> Carga',
            'X': '<i data-lucide="refresh-ccw" style="width:14px; display:inline-block; vertical-align:middle;"></i> Reingreso',
            'Z': '<i data-lucide="gift" style="width:14px; display:inline-block; vertical-align:middle;"></i> Cortesía'
        }[t.strRateKey] || (t.strRateKey || '—');

        return `
        <tr id="ticket-row-${t.id}">
            <td>
                <input type="checkbox"
                       id="ctCheck_${t.id}"
                       value="${t.id}"
                       onchange="toggleTicketSeleccion(${t.id}, this.checked)">
            </td>
            <td><strong style="font-size:15px;">${t.placa}</strong></td>
            <td style="font-family:monospace;font-size:11px;color:var(--gray-500);">
                ${t.codigoBarras || '—'}
            </td>
            <td>${fechaStr}</td>
            <td><span style="${diasColor}">${t.diasAdentro} día${t.diasAdentro !== 1 ? 's' : ''}</span></td>
            <td><span class="badge" style="background:#e8effc;color:#1a56db; display:flex; align-items:center; gap:4px;">${rateLabel}</span></td>
            <td style="font-size:12px;">${t.nombreOperador || '—'}</td>
            <td style="font-size:12px;color:var(--gray-500);">${t.idDispositivoEntrada || '—'}</td>
            <td>
                <input type="number"
                       id="ctMonto_${t.id}"
                       min="0" step="0.01" placeholder="0.00"
                       style="width:110px;padding:6px 8px;border:2px solid var(--gray-200);
                              border-radius:6px;font-size:14px;font-weight:600;text-align:right;"
                       onfocus="this.select()">
            </td>
            <td>
                <button class="btn btn-sm btn-primary" style="display:flex; align-items:center; gap:4px;"
                        onclick="abrirModalIndividual(${t.id})">
                    <i data-lucide="ticket" style="width:14px;"></i> Cerrar
                </button>
            </td>
        </tr>`;
    }).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

// ===== SELECCIÓN =====
function toggleTicketSeleccion(id, checked) {
    if (checked) {
        if (!ticketsSeleccionados.includes(id))
            ticketsSeleccionados.push(id);
    } else {
        ticketsSeleccionados = ticketsSeleccionados.filter(x => x !== id);
    }
    actualizarBotonMasivo();

    const checkAll = document.getElementById('ctCheckAll');
    if (checkAll) {
        checkAll.checked = ticketsSeleccionados.length === ticketsAntiguosData.length;
        checkAll.indeterminate = ticketsSeleccionados.length > 0 &&
            ticketsSeleccionados.length < ticketsAntiguosData.length;
    }
}

function toggleTodosTickets(checkAllEl) {
    ticketsSeleccionados = [];
    document.querySelectorAll('[id^="ctCheck_"]').forEach(cb => {
        cb.checked = checkAllEl.checked;
        if (checkAllEl.checked) {
            const id = parseInt(cb.value);
            ticketsSeleccionados.push(id);
        }
    });
    actualizarBotonMasivo();
}

function actualizarBotonMasivo() {
    const btn = document.getElementById('btnCerrarSeleccionados');
    const count = document.getElementById('ctSelCount');
    if (!btn || !count) return;

    count.textContent = ticketsSeleccionados.length;
    btn.disabled = ticketsSeleccionados.length === 0;
}

// ===== MODAL INDIVIDUAL =====
function abrirModalIndividual(id) {
    const ticket = ticketsAntiguosData.find(t => t.id === id);
    if (!ticket) return;

    document.getElementById('ctModalId').value = id;
    document.getElementById('ctModalPlaca').textContent = ticket.placa;
    document.getElementById('ctModalFechaEntrada').textContent = new Date(ticket.fechaEntrada)
        .toLocaleString('es-GT', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
    document.getElementById('ctModalDias').textContent =
        `${ticket.diasAdentro} día${ticket.diasAdentro !== 1 ? 's' : ''} dentro`;
    document.getElementById('ctModalCodigo').textContent = ticket.codigoBarras || '—';

    const montoEnFila = document.getElementById(`ctMonto_${id}`)?.value;
    document.getElementById('ctModalMonto').value = montoEnFila || '';

    document.getElementById('modalCerrarTicket').style.display = 'flex';
    setTimeout(() => document.getElementById('ctModalMonto').focus(), 100);
}

function cerrarModalTicket() {
    document.getElementById('modalCerrarTicket').style.display = 'none';
    document.getElementById('ctModalMonto').value = '';
}

async function confirmarCierreTicket() {
    const id = parseInt(document.getElementById('ctModalId').value);
    const montoStr = document.getElementById('ctModalMonto').value;
    const monto = parseFloat(montoStr);

    if (isNaN(monto) || monto < 0) {
        mostrarToast('Ingrese un monto válido (0 o mayor)', 'error');
        return;
    }

    const btn = document.querySelector('#modalCerrarTicket .btn-primary');
    btn.disabled = true;
    btn.innerHTML = '<i data-lucide="loader" class="spin" style="width:14px;"></i> Cerrando…';
    if (window.lucide) lucide.createIcons({ root: btn });

    try {
        const res = await fetch(`${API_TICKETS}/cerrar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                id,
                monto,
                idOperador: operadorActual?.idOperador || 0,
                nombreOperador: operadorActual?.nombreCompleto || 'Admin'
            })
        });

        const data = await res.json();
        cerrarModalTicket();

        if (data.exitoso && data.data) {
            const r = data.data;
            const mins = r.tiempoEstancia || 0;
            const horas = Math.floor(mins / 60);
            const minRest = mins % 60;
            const tiempoStr = horas > 0 ? `${horas}h ${minRest}m` : `${mins}m`;

            mostrarResumenCierre([{
                exitoso: true,
                placa: r.placa,
                monto: r.monto,
                tiempoEstancia: r.tiempoEstancia,
                mensaje: `Placa ${r.placa} — $${parseFloat(r.monto).toFixed(2)} — ${tiempoStr}`
            }], 1, 0);

            await cargarTicketsAntiguos();
        } else {
            mostrarToast(data.mensaje || 'Error al cerrar ticket', 'error');
        }
    } catch (err) {
        mostrarToast('Error de conexión', 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Confirmar cierre';
    }
}

// ===== MODAL MASIVO =====
function abrirModalMasivo() {
    if (ticketsSeleccionados.length === 0) {
        mostrarToast('Seleccione al menos un ticket', 'error');
        return;
    }

    const lista = document.getElementById('ctMasivoLista');
    const countEl = document.getElementById('ctMasivoCount');
    if (!lista) return;

    countEl.textContent = ticketsSeleccionados.length;

    lista.innerHTML = ticketsSeleccionados.map(id => {
        const ticket = ticketsAntiguosData.find(t => t.id === id);
        if (!ticket) return '';

        const montoEnFila = document.getElementById(`ctMonto_${id}`)?.value || '';
        const fechaStr = new Date(ticket.fechaEntrada).toLocaleString('es-GT', {
            day: '2-digit', month: '2-digit', year: 'numeric',
            hour: '2-digit', minute: '2-digit'
        });

        return `
        <div style="display:grid;grid-template-columns:1fr auto;gap:12px;align-items:center;
                    padding:12px;border:1px solid var(--gray-200);border-radius:8px;margin-bottom:8px;">
            <div>
                <strong style="font-size:15px;">${ticket.placa}</strong>
                <span style="font-size:12px;color:var(--gray-500);margin-left:8px;">${fechaStr}</span><br>
                <span style="font-size:12px;color:var(--danger);">${ticket.diasAdentro} día${ticket.diasAdentro !== 1 ? 's' : ''} dentro</span>
                <span style="font-size:11px;color:var(--gray-400);margin-left:8px;">${ticket.codigoBarras || ''}</span>
            </div>
            <div>
                <label style="font-size:11px;color:var(--gray-500);display:block;text-align:right;margin-bottom:2px;">
                    Monto ($)
                </label>
                <input type="number"
                       id="ctMasivoMonto_${id}"
                       min="0" step="0.01"
                       value="${montoEnFila}"
                       placeholder="0.00"
                       style="width:110px;padding:8px;border:2px solid var(--gray-200);
                              border-radius:6px;font-size:15px;font-weight:700;text-align:right;"
                       onfocus="this.select()">
            </div>
        </div>`;
    }).join('');

    document.getElementById('modalCerrarMasivo').style.display = 'flex';
}

function cerrarModalMasivo() {
    document.getElementById('modalCerrarMasivo').style.display = 'none';
}

async function confirmarCierreMasivo() {
    const tickets = ticketsSeleccionados.map(id => {
        const montoStr = document.getElementById(`ctMasivoMonto_${id}`)?.value;
        const monto = parseFloat(montoStr);
        return {
            id,
            monto: isNaN(monto) || monto < 0 ? 0 : monto
        };
    });

    const btn = document.querySelector('#modalCerrarMasivo .btn-primary');
    btn.disabled = true;
    btn.innerHTML = '<i data-lucide="loader" class="spin" style="width:14px;"></i> Cerrando…';
    if (window.lucide) lucide.createIcons({ root: btn });

    try {
        const res = await fetch(`${API_TICKETS}/cerrar-masivo`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                tickets,
                idOperador: operadorActual?.idOperador || 0,
                nombreOperador: operadorActual?.nombreCompleto || 'Admin'
            })
        });

        const data = await res.json();
        cerrarModalMasivo();

        if (data.data) {
            const r = data.data;
            mostrarResumenCierre(
                r.resultados || [],
                r.totalExitosos,
                r.totalFallidos
            );
            await cargarTicketsAntiguos();
        } else {
            mostrarToast(data.mensaje || 'Error en cierre masivo', 'error');
        }
    } catch (err) {
        mostrarToast('Error de conexión', 'error');
    } finally {
        btn.disabled = false;
        btn.innerHTML = `Cerrar todos (<span id="ctMasivoCount">${ticketsSeleccionados.length}</span>)`;
    }
}

// ===== MODAL RESULTADO =====
function mostrarResumenCierre(resultados, exitosos, fallidos) {
    const modal = document.getElementById('modalResultadoCierre');
    const titulo = document.getElementById('ctResumenTitulo');
    const body = document.getElementById('ctResumenBody');

    if (!modal) return;

    const total = exitosos + fallidos;
    titulo.innerHTML = fallidos === 0
        ? `<i data-lucide="check-circle" style="color:var(--success); width:20px; display:inline-block; vertical-align:middle;"></i> ${exitosos} ticket${exitosos !== 1 ? 's' : ''} cerrado${exitosos !== 1 ? 's' : ''}`
        : `<i data-lucide="alert-triangle" style="color:var(--warning); width:20px; display:inline-block; vertical-align:middle;"></i> ${exitosos}/${total} tickets cerrados`;

    const items = resultados.map(r => {
        if (r.exitoso) {
            const mins = r.tiempoEstancia || 0;
            const horas = Math.floor(mins / 60);
            const minRest = mins % 60;
            const tiempoStr = horas > 0 ? `${horas}h ${minRest}m` : `${mins}m`;
            return `<div style="padding:10px;border:1px solid #d1fae5;border-radius:6px;
                                background:#f0fdf4;margin-bottom:6px;display:flex;
                                justify-content:space-between;align-items:center;">
                <div>
                    <strong style="color:#065f46;">${r.placa || 'N/A'}</strong>
                    <span style="font-size:12px;color:var(--gray-500);margin-left:6px;">${tiempoStr} de estancia</span>
                </div>
                <span style="font-weight:700;color:#059669;font-size:15px;">
                    $${parseFloat(r.monto || 0).toFixed(2)}
                </span>
            </div>`;
        } else {
            return `<div style="padding:10px;border:1px solid #fecaca;border-radius:6px;
                                background:#fef2f2;margin-bottom:6px;font-size:13px;color:#b91c1c; display:flex; align-items:center; gap:6px;">
                <i data-lucide="x-circle" style="width:16px;"></i> ${r.mensaje || 'Error desconocido'}
            </div>`;
        }
    }).join('');

    const totalCobrado = resultados
        .filter(r => r.exitoso)
        .reduce((s, r) => s + parseFloat(r.monto || 0), 0);

    body.innerHTML = `
        ${items}
        ${exitosos > 0 ? `
        <div style="margin-top:12px;padding:12px;background:var(--gray-50);border-radius:8px;
                    display:flex;justify-content:space-between;align-items:center;border-top:2px solid var(--gray-200);">
            <span style="font-size:14px;font-weight:600;color:var(--gray-700);">Total cobrado:</span>
            <span style="font-size:20px;font-weight:700;color:var(--success);">$${totalCobrado.toFixed(2)}</span>
        </div>` : ''}`;

    if (window.lucide) {
        lucide.createIcons({ root: titulo });
        lucide.createIcons({ root: body });
    }

    modal.style.display = 'flex';
}

function cerrarModalResultado() {
    document.getElementById('modalResultadoCierre').style.display = 'none';
}