// =============================================
// MÓDULO: CUENTAS Y TARJETAS (Access Level)
// Gestión de Cuentas Corporativas, Tarjetas,
// Horarios y Dispositivos de Acceso
// =============================================

const API_BASE = '/api';
const API_CUENTAS   = `${API_BASE}/cuentas`;
const API_TARJETAS  = `${API_BASE}/tarjetas`;

// Estado de la aplicación
let cuentasData      = [];
let tarjetasData     = [];
let cuentaSeleccionada = null;   // { id, nombre, codigoUnico }
let tabActual        = 'tarjetas';

// ─────────────────────────────────────────────────────────────────
// NAVEGACIÓN
// ─────────────────────────────────────────────────────────────────

function navegarSeccion(seccion, linkEl) {
    document.querySelectorAll('.nav-item').forEach(a => a.classList.remove('active'));
    if (linkEl) linkEl.classList.add('active');

    document.getElementById('seccionCuentas').style.display  = 'none';
    document.getElementById('seccionTarjetas').style.display = 'none';

    const pageTitle = document.getElementById('pageTitle');

    if (seccion === 'cuentas') {
        document.getElementById('seccionCuentas').style.display = '';
        if (pageTitle) pageTitle.innerHTML = '<i data-lucide="users"></i> Cuentas Corporativas';
        cargarCuentas();
    } else if (seccion === 'tarjetas') {
        document.getElementById('seccionTarjetas').style.display = '';
        if (pageTitle) pageTitle.innerHTML = '<i data-lucide="credit-card"></i> Todas las Tarjetas';
        cargarTodasTarjetas();
    }

    if (window.lucide) lucide.createIcons();
}

function toggleSidebar() {
    document.getElementById('sidebar').classList.toggle('sidebar-collapsed');
}

// ─────────────────────────────────────────────────────────────────
// CUENTAS — Listar
// ─────────────────────────────────────────────────────────────────

async function cargarCuentas() {
    const tbody = document.getElementById('tbodyCuentas');
    if (!tbody) return;
    tbody.innerHTML = `<tr><td colspan="7" class="empty-row">
        <div style="display:flex;align-items:center;justify-content:center;gap:10px;">
            <div class="dash-spinner"></div> Cargando cuentas…
        </div></td></tr>`;

    try {
        const res  = await fetch(API_CUENTAS);
        const data = await res.json();

        if (!data.exitoso) {
            tbody.innerHTML = `<tr><td colspan="7" class="empty-row" style="color:var(--danger-text);">
                ${data.mensaje}</td></tr>`;
            return;
        }

        cuentasData = data.data || [];
        renderizarCuentas();
        poblarFiltroCuentas();

    } catch {
        tbody.innerHTML = `<tr><td colspan="7" class="empty-row" style="color:var(--danger-text);">
            Error de conexión</td></tr>`;
    }
}

function renderizarCuentas() {
    const tbody = document.getElementById('tbodyCuentas');
    if (!tbody) return;

    if (cuentasData.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" class="empty-row">Sin cuentas registradas</td></tr>`;
        return;
    }

    tbody.innerHTML = cuentasData.map(c => {
        const activo = c.activo ?? c.activa ?? false;
        return `
        <tr id="cRow_${c.id}">
            <td><strong style="color:var(--brand-400);">${esc(c.codigoUnico)}</strong></td>
            <td>${esc(c.nombre)}</td>
            <td style="color:var(--text-secondary);max-width:200px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">
                ${esc(c.descripcion || '—')}
            </td>
            <td style="text-align:center;">
                <span class="badge badge-info" style="background:var(--info-dim);color:var(--text-brand);">
                    ${c.totalTarjetas ?? 0} tarjetas
                </span>
            </td>
            <td>
                <span class="badge ${activo ? 'badge-success' : 'badge-danger'}">
                    ${activo ? '✓ Activa' : '✗ Inactiva'}
                </span>
            </td>
            <td style="color:var(--text-secondary);font-size:12px;">
                ${formatFecha(c.fechaCreacion)}
            </td>
            <td>
                <div style="display:flex;gap:6px;align-items:center;flex-wrap:wrap;">
                    <button class="btn btn-xs btn-secondary cuenta-seleccionar"
                            onclick="seleccionarCuenta(${c.id}, '${esc(c.nombre)}', '${esc(c.codigoUnico)}')"
                            title="Ver detalle">
                        <i data-lucide="eye" style="width:12px;height:12px;"></i> Detalle
                    </button>
                    <button class="btn btn-xs btn-secondary"
                            onclick="abrirModalEditarCuenta(${c.id})"
                            title="Editar">
                        <i data-lucide="edit-3" style="width:12px;height:12px;"></i>
                    </button>
                    <button class="btn btn-xs ${activo ? 'btn-warning' : 'btn-success'}"
                            onclick="toggleCuenta(${c.id})"
                            title="${activo ? 'Desactivar' : 'Activar'}">
                        <i data-lucide="${activo ? 'toggle-left' : 'toggle-right'}" style="width:12px;height:12px;"></i>
                        ${activo ? 'Desactivar' : 'Activar'}
                    </button>
                </div>
            </td>
        </tr>
        `;
    }).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}


// ─────────────────────────────────────────────────────────────────
// CUENTAS — CRUD
// ─────────────────────────────────────────────────────────────────

function abrirModalNuevaCuenta() {
    document.getElementById('cuentaId').value = '';
    document.getElementById('cuentaCodigo').value = '';
    document.getElementById('cuentaNombre').value = '';
    document.getElementById('cuentaDescripcion').value = '';
    document.getElementById('cuentaCodigo').disabled = false;
    document.getElementById('modalCuentaTitulo').innerHTML = '<i data-lucide="users"></i> Nueva Cuenta';
    document.getElementById('modalCuenta').style.display = 'flex';
    if (window.lucide) lucide.createIcons({ root: document.getElementById('modalCuenta') });
    setTimeout(() => document.getElementById('cuentaCodigo').focus(), 80);
}

function abrirModalEditarCuenta(id) {
    const c = cuentasData.find(x => x.id === id);
    if (!c) return;
    document.getElementById('cuentaId').value = id;
    document.getElementById('cuentaCodigo').value = c.codigoUnico;
    document.getElementById('cuentaCodigo').disabled = true;   // no se puede cambiar el código único
    document.getElementById('cuentaNombre').value = c.nombre;
    document.getElementById('cuentaDescripcion').value = c.descripcion || '';
    document.getElementById('modalCuentaTitulo').innerHTML = '<i data-lucide="edit-3"></i> Editar Cuenta';
    document.getElementById('modalCuenta').style.display = 'flex';
    if (window.lucide) lucide.createIcons({ root: document.getElementById('modalCuenta') });
    setTimeout(() => document.getElementById('cuentaNombre').focus(), 80);
}

function cerrarModalCuenta() {
    document.getElementById('modalCuenta').style.display = 'none';
}

async function guardarCuenta() {
    const id     = document.getElementById('cuentaId').value;
    const codigo = document.getElementById('cuentaCodigo').value.trim();
    const nombre = document.getElementById('cuentaNombre').value.trim();
    const desc   = document.getElementById('cuentaDescripcion').value.trim();

    if (!nombre) { mostrarToast('El nombre es requerido', 'error'); return; }

    const btn = document.getElementById('btnGuardarCuenta');
    btn.disabled = true;
    btn.innerHTML = '<i data-lucide="loader" class="spin" style="width:16px;"></i> Guardando…';
    if (window.lucide) lucide.createIcons({ root: btn });

    try {
        let res, data;
        if (id) {
            // Actualizar
            res  = await fetch(`${API_CUENTAS}/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ nombre, descripcion: desc || null })
            });
        } else {
            // Crear
            if (!codigo) {
                mostrarToast('El código único es requerido', 'error');
                btn.disabled = false;
                btn.innerHTML = '<i data-lucide="save" style="width:16px;height:16px;"></i> Guardar';
                if (window.lucide) lucide.createIcons({ root: btn });
                return;
            }
            res = await fetch(API_CUENTAS, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ codigoUnico: codigo, nombre, descripcion: desc || null })
            });
        }

        data = await res.json();
        if (data.exitoso) {
            mostrarToast(data.mensaje || 'Cuenta guardada', 'success');
            cerrarModalCuenta();
            cargarCuentas();
        } else {
            mostrarToast(data.mensaje || 'Error al guardar', 'error');
        }
    } catch {
        mostrarToast('Error de conexión', 'error');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i data-lucide="save" style="width:16px;height:16px;"></i> Guardar';
        if (window.lucide) lucide.createIcons({ root: btn });
    }
}

async function toggleCuenta(id) {
    try {
        const res  = await fetch(`${API_CUENTAS}/${id}/toggle`, { method: 'PATCH' });
        const data = await res.json();
        if (data.exitoso) {
            mostrarToast(data.mensaje || 'Estado actualizado', 'success');
            cargarCuentas();
            if (cuentaSeleccionada?.id === id) cerrarDetalleCuenta();
        } else {
            mostrarToast(data.mensaje || 'Error', 'error');
        }
    } catch {
        mostrarToast('Error de conexión', 'error');
    }
}

// ─────────────────────────────────────────────────────────────────
// DETALLE DE CUENTA (Tarjetas / Horarios / Dispositivos)
// ─────────────────────────────────────────────────────────────────

function seleccionarCuenta(id, nombre, codigo) {
    cuentaSeleccionada = { id, nombre, codigo };
    document.getElementById('panelDetalleCuenta').style.display = '';
    document.getElementById('detalleCuentaTitulo').innerHTML =
        `<i data-lucide="info"></i> ${esc(nombre)} <span style="font-size:12px;color:var(--text-secondary);font-weight:400;">(${esc(codigo)})</span>`;
    if (window.lucide) lucide.createIcons({ root: document.getElementById('panelDetalleCuenta') });

    activarTab(tabActual, document.querySelector(`.tab-btn.tab-active`) || document.querySelectorAll('.tab-btn')[0]);
    document.getElementById('panelDetalleCuenta').scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function cerrarDetalleCuenta() {
    cuentaSeleccionada = null;
    document.getElementById('panelDetalleCuenta').style.display = 'none';
}

function activarTab(tab, btnEl) {
    tabActual = tab;

    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('tab-active'));
    if (btnEl) btnEl.classList.add('tab-active');

    document.getElementById('tabTarjetas').style.display     = tab === 'tarjetas'    ? '' : 'none';
    document.getElementById('tabHorarios').style.display     = tab === 'horarios'    ? '' : 'none';
    document.getElementById('tabDispositivos').style.display = tab === 'dispositivos'? '' : 'none';

    if (!cuentaSeleccionada) return;

    if (tab === 'tarjetas')     cargarTarjetasDeCuenta(cuentaSeleccionada.id);
    if (tab === 'horarios')     cargarHorariosDeCuenta(cuentaSeleccionada.id);
    if (tab === 'dispositivos') cargarDispositivosDeCuenta(cuentaSeleccionada.id);
}

// ─────────────────────────────────────────────────────────────────
// TARJETAS DE UNA CUENTA
// ─────────────────────────────────────────────────────────────────

async function cargarTarjetasDeCuenta(idCuenta) {
    const tbody = document.getElementById('tbodyTarjetas');
    if (!tbody) return;
    tbody.innerHTML = `<tr><td colspan="7" class="empty-row">
        <div style="display:flex;align-items:center;justify-content:center;gap:10px;">
            <div class="dash-spinner"></div>
        </div></td></tr>`;

    try {
        const res  = await fetch(`${API_CUENTAS}/${idCuenta}/tarjetas`);
        const data = await res.json();
        tarjetasData = data.data || [];
        renderizarTarjetasDeCuenta();
    } catch {
        tbody.innerHTML = `<tr><td colspan="7" class="empty-row" style="color:var(--danger-text);">Error de conexión</td></tr>`;
    }
}

function renderizarTarjetasDeCuenta() {
    const tbody = document.getElementById('tbodyTarjetas');
    if (!tbody) return;

    if (tarjetasData.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" class="empty-row">Sin tarjetas en esta cuenta</td></tr>`;
        return;
    }

    tbody.innerHTML = tarjetasData.map(t => `
        <tr id="tRow_${t.id}">
            <td><code style="color:var(--brand-400);font-size:12px;">${esc(t.numeroTarjeta)}</code></td>
            <td>${esc(t.nombreUsuario)}</td>
            <td>${esc(t.placaVehiculo || '—')}</td>
            <td>${esc(t.telefono || '—')}</td>
            <td>
                <span class="badge ${t.activo ? 'badge-success' : 'badge-danger'}">
                    ${t.activo ? '✓ Activa' : '✗ Inactiva'}
                </span>
            </td>
            <td style="color:var(--text-secondary);font-size:12px;">${t.fechaUltimoUso ? formatFecha(t.fechaUltimoUso) : '—'}</td>
            <td>
                <div style="display:flex;gap:6px;flex-wrap:wrap;">
                    <button class="btn btn-xs btn-secondary" onclick="abrirModalEditarTarjeta(${t.id})" title="Editar">
                        <i data-lucide="edit-3" style="width:12px;height:12px;"></i>
                    </button>
                    <button class="btn btn-xs ${t.activo ? 'btn-warning' : 'btn-success'}"
                            onclick="toggleTarjeta(${t.id})" title="${t.activo ? 'Desactivar' : 'Activar'}">
                        <i data-lucide="${t.activo ? 'toggle-left' : 'toggle-right'}" style="width:12px;height:12px;"></i>
                    </button>
                </div>
            </td>
        </tr>
    `).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

function abrirModalNuevaTarjeta() {
    if (!cuentaSeleccionada) { mostrarToast('Seleccione una cuenta primero', 'warning'); return; }
    document.getElementById('tarjetaId').value = '';
    document.getElementById('tarjetaNumero').value = '';
    document.getElementById('tarjetaNumero').disabled = false;
    document.getElementById('tarjetaNombre').value = '';
    document.getElementById('tarjetaPlaca').value = '';
    document.getElementById('tarjetaTelefono').value = '';
    document.getElementById('modalTarjetaTitulo').innerHTML = '<i data-lucide="credit-card"></i> Nueva Tarjeta';
    document.getElementById('modalTarjeta').style.display = 'flex';
    if (window.lucide) lucide.createIcons({ root: document.getElementById('modalTarjeta') });
    setTimeout(() => document.getElementById('tarjetaNumero').focus(), 80);
}

function abrirModalEditarTarjeta(id) {
    const t = tarjetasData.find(x => x.id === id);
    if (!t) return;
    document.getElementById('tarjetaId').value = id;
    document.getElementById('tarjetaNumero').value = t.numeroTarjeta;
    document.getElementById('tarjetaNumero').disabled = true;
    document.getElementById('tarjetaNombre').value = t.nombreUsuario;
    document.getElementById('tarjetaPlaca').value = t.placaVehiculo || '';
    document.getElementById('tarjetaTelefono').value = t.telefono || '';
    document.getElementById('modalTarjetaTitulo').innerHTML = '<i data-lucide="edit-3"></i> Editar Tarjeta';
    document.getElementById('modalTarjeta').style.display = 'flex';
    if (window.lucide) lucide.createIcons({ root: document.getElementById('modalTarjeta') });
    setTimeout(() => document.getElementById('tarjetaNombre').focus(), 80);
}

function cerrarModalTarjeta() {
    document.getElementById('modalTarjeta').style.display = 'none';
}

async function guardarTarjeta() {
    const id      = document.getElementById('tarjetaId').value;
    const numero  = document.getElementById('tarjetaNumero').value.trim();
    const nombre  = document.getElementById('tarjetaNombre').value.trim();
    const placa   = document.getElementById('tarjetaPlaca').value.trim().toUpperCase();
    const telefono= document.getElementById('tarjetaTelefono').value.trim();

    if (!nombre) { mostrarToast('El nombre es requerido', 'error'); return; }

    const btn = document.getElementById('btnGuardarTarjeta');
    btn.disabled = true;
    btn.innerHTML = '<i data-lucide="loader" class="spin" style="width:16px;"></i> Guardando…';
    if (window.lucide) lucide.createIcons({ root: btn });

    try {
        let res, data;
        if (id) {
            res = await fetch(`${API_TARJETAS}/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    nombreUsuario: nombre,
                    placaVehiculo: placa || null,
                    telefono: telefono || null
                })
            });
        } else {
            if (!numero) {
                mostrarToast('El número de tarjeta es requerido', 'error');
                btn.disabled = false;
                btn.innerHTML = '<i data-lucide="save" style="width:16px;height:16px;"></i> Guardar';
                if (window.lucide) lucide.createIcons({ root: btn });
                return;
            }
            res = await fetch(API_TARJETAS, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    idCuenta: cuentaSeleccionada.id,
                    numeroTarjeta: numero,
                    nombreUsuario: nombre,
                    placaVehiculo: placa || null,
                    telefono: telefono || null
                })
            });
        }

        data = await res.json();
        if (data.exitoso) {
            mostrarToast(data.mensaje || 'Tarjeta guardada', 'success');
            cerrarModalTarjeta();
            cargarTarjetasDeCuenta(cuentaSeleccionada.id);
        } else {
            mostrarToast(data.mensaje || 'Error al guardar', 'error');
        }
    } catch {
        mostrarToast('Error de conexión', 'error');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i data-lucide="save" style="width:16px;height:16px;"></i> Guardar';
        if (window.lucide) lucide.createIcons({ root: btn });
    }
}

async function toggleTarjeta(id) {
    try {
        const res  = await fetch(`${API_TARJETAS}/${id}/toggle`, { method: 'PATCH' });
        const data = await res.json();
        if (data.exitoso) {
            mostrarToast(data.mensaje || 'Estado actualizado', 'success');
            if (cuentaSeleccionada) cargarTarjetasDeCuenta(cuentaSeleccionada.id);
        } else {
            mostrarToast(data.mensaje || 'Error', 'error');
        }
    } catch {
        mostrarToast('Error de conexión', 'error');
    }
}

// ─────────────────────────────────────────────────────────────────
// HORARIOS
// ─────────────────────────────────────────────────────────────────

async function cargarHorariosDeCuenta(idCuenta) {
    const tbody = document.getElementById('tbodyHorarios');
    if (!tbody) return;
    tbody.innerHTML = `<tr><td colspan="4" class="empty-row">
        <div style="display:flex;align-items:center;justify-content:center;gap:10px;">
            <div class="dash-spinner"></div>
        </div></td></tr>`;

    try {
        const res  = await fetch(`${API_CUENTAS}/${idCuenta}/horarios`);
        const data = await res.json();
        const horarios = data.data || [];
        renderizarHorarios(horarios, idCuenta);
    } catch {
        tbody.innerHTML = `<tr><td colspan="4" class="empty-row" style="color:var(--danger-text);">Error de conexión</td></tr>`;
    }
}

function renderizarHorarios(horarios, idCuenta) {
    const tbody = document.getElementById('tbodyHorarios');
    if (!tbody) return;

    if (horarios.length === 0) {
        tbody.innerHTML = `<tr><td colspan="4" class="empty-row">Sin horarios configurados</td></tr>`;
        return;
    }

    tbody.innerHTML = horarios.map(h => `
        <tr>
            <td><strong>${esc(h.nombreDia)}</strong></td>
            <td>${formatHora(h.horaInicio)}</td>
            <td>${formatHora(h.horaFin)}</td>
            <td>
                <button class="btn btn-xs btn-danger" onclick="eliminarHorario(${idCuenta}, ${h.id})"
                        title="Eliminar horario" style="display:flex;align-items:center;gap:4px;">
                    <i data-lucide="trash-2" style="width:12px;height:12px;"></i> Eliminar
                </button>
            </td>
        </tr>
    `).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

function abrirModalNuevoHorario() {
    if (!cuentaSeleccionada) { mostrarToast('Seleccione una cuenta primero', 'warning'); return; }
    document.getElementById('horarioDia').value    = '2';
    document.getElementById('horarioInicio').value = '08:00';
    document.getElementById('horarioFin').value    = '18:00';
    document.getElementById('modalHorario').style.display = 'flex';
}

function cerrarModalHorario() {
    document.getElementById('modalHorario').style.display = 'none';
}

async function guardarHorario() {
    if (!cuentaSeleccionada) return;

    const dia    = parseInt(document.getElementById('horarioDia').value);
    const inicio = document.getElementById('horarioInicio').value;
    const fin    = document.getElementById('horarioFin').value;

    if (!inicio || !fin) { mostrarToast('Hora inicio y fin son requeridas', 'error'); return; }
    if (inicio >= fin)   { mostrarToast('La hora inicio debe ser menor a la hora fin', 'error'); return; }

    const btn = document.getElementById('btnGuardarHorario');
    btn.disabled = true;
    btn.innerHTML = '<i data-lucide="loader" class="spin" style="width:16px;"></i> Guardando…';
    if (window.lucide) lucide.createIcons({ root: btn });

    try {
        const res  = await fetch(`${API_CUENTAS}/${cuentaSeleccionada.id}/horarios`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ diaSemana: dia, horaInicio: `${inicio}:00`, horaFin: `${fin}:00` })
        });
        const data = await res.json();
        if (data.exitoso) {
            mostrarToast(data.mensaje || 'Horario guardado', 'success');
            cerrarModalHorario();
            cargarHorariosDeCuenta(cuentaSeleccionada.id);
        } else {
            mostrarToast(data.mensaje || 'Error al guardar', 'error');
        }
    } catch {
        mostrarToast('Error de conexión', 'error');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i data-lucide="save" style="width:16px;height:16px;"></i> Guardar';
        if (window.lucide) lucide.createIcons({ root: btn });
    }
}

async function eliminarHorario(idCuenta, idHorario) {
    if (!confirm('¿Eliminar este horario?')) return;
    try {
        const res  = await fetch(`${API_CUENTAS}/${idCuenta}/horarios/${idHorario}`, { method: 'DELETE' });
        const data = await res.json();
        if (data.exitoso) {
            mostrarToast('Horario eliminado', 'success');
            cargarHorariosDeCuenta(idCuenta);
        } else {
            mostrarToast(data.mensaje || 'Error', 'error');
        }
    } catch {
        mostrarToast('Error de conexión', 'error');
    }
}

// ─────────────────────────────────────────────────────────────────
// DISPOSITIVOS
// ─────────────────────────────────────────────────────────────────

async function cargarDispositivosDeCuenta(idCuenta) {
    const tbody = document.getElementById('tbodyDispositivos');
    if (!tbody) return;
    tbody.innerHTML = `<tr><td colspan="4" class="empty-row">
        <div style="display:flex;align-items:center;justify-content:center;gap:10px;">
            <div class="dash-spinner"></div>
        </div></td></tr>`;

    try {
        const res  = await fetch(`${API_CUENTAS}/${idCuenta}/dispositivos`);
        const data = await res.json();
        renderizarDispositivos(data.data || [], idCuenta);
    } catch {
        tbody.innerHTML = `<tr><td colspan="4" class="empty-row" style="color:var(--danger-text);">Error de conexión</td></tr>`;
    }
}

function renderizarDispositivos(dispositivos, idCuenta) {
    const tbody = document.getElementById('tbodyDispositivos');
    if (!tbody) return;

    if (dispositivos.length === 0) {
        tbody.innerHTML = `<tr><td colspan="4" class="empty-row">Sin dispositivos asignados</td></tr>`;
        return;
    }

    tbody.innerHTML = dispositivos.map(d => `
        <tr>
            <td><code style="color:var(--brand-400);">${esc(d.idDispositivo)}</code></td>
            <td>${esc(d.nombreDispositivo || d.idDispositivo)}</td>
            <td style="color:var(--text-secondary);">${esc(d.tipoDispositivo || '—')}</td>
            <td>
                <button class="btn btn-xs btn-danger"
                        onclick="quitarDispositivo(${idCuenta}, '${esc(d.idDispositivo)}')"
                        title="Quitar dispositivo" style="display:flex;align-items:center;gap:4px;">
                    <i data-lucide="trash-2" style="width:12px;height:12px;"></i> Quitar
                </button>
            </td>
        </tr>
    `).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

async function asignarDispositivo() {
    if (!cuentaSeleccionada) { mostrarToast('Seleccione una cuenta primero', 'warning'); return; }
    const idDisp = document.getElementById('inputNuevoDispositivo').value.trim();
    if (!idDisp) { mostrarToast('Ingrese el ID del dispositivo', 'error'); return; }

    try {
        const res  = await fetch(`${API_CUENTAS}/${cuentaSeleccionada.id}/dispositivos`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ idDispositivo: idDisp })
        });
        const data = await res.json();
        if (data.exitoso) {
            mostrarToast(data.mensaje || 'Dispositivo asignado', 'success');
            document.getElementById('inputNuevoDispositivo').value = '';
            cargarDispositivosDeCuenta(cuentaSeleccionada.id);
        } else {
            mostrarToast(data.mensaje || 'Error', 'error');
        }
    } catch {
        mostrarToast('Error de conexión', 'error');
    }
}

async function quitarDispositivo(idCuenta, idDispositivo) {
    if (!confirm(`¿Quitar el dispositivo "${idDispositivo}" de esta cuenta?`)) return;
    try {
        const res  = await fetch(`${API_CUENTAS}/${idCuenta}/dispositivos/${encodeURIComponent(idDispositivo)}`,
            { method: 'DELETE' });
        const data = await res.json();
        if (data.exitoso) {
            mostrarToast('Dispositivo quitado', 'success');
            cargarDispositivosDeCuenta(idCuenta);
        } else {
            mostrarToast(data.mensaje || 'Error', 'error');
        }
    } catch {
        mostrarToast('Error de conexión', 'error');
    }
}

// ─────────────────────────────────────────────────────────────────
// TODAS LAS TARJETAS (vista global)
// ─────────────────────────────────────────────────────────────────

let todasTarjetasCache = [];

async function cargarTodasTarjetas() {
    const tbody = document.getElementById('tbodyTodasTarjetas');
    if (!tbody) return;
    tbody.innerHTML = `<tr><td colspan="8" class="empty-row">
        <div style="display:flex;align-items:center;justify-content:center;gap:10px;">
            <div class="dash-spinner"></div> Cargando tarjetas…
        </div></td></tr>`;

    try {
        // Cargamos cuentas para filtro
        if (cuentasData.length === 0) {
            const rc = await fetch(API_CUENTAS);
            const dc = await rc.json();
            cuentasData = dc.data || [];
            poblarFiltroCuentas();
        }

        // Obtenemos tarjetas de todas las cuentas en paralelo
        const promises = cuentasData.map(c =>
            fetch(`${API_CUENTAS}/${c.id}/tarjetas`)
                .then(r => r.json())
                .then(d => d.data || [])
                .catch(() => [])
        );
        const results = await Promise.all(promises);
        todasTarjetasCache = results.flat();
        renderizarTodasTarjetas(todasTarjetasCache);

    } catch {
        tbody.innerHTML = `<tr><td colspan="8" class="empty-row" style="color:var(--danger-text);">Error de conexión</td></tr>`;
    }
}

function poblarFiltroCuentas() {
    const sel = document.getElementById('filtroCuenta');
    if (!sel) return;
    const current = sel.value;
    sel.innerHTML = '<option value="">Todas las cuentas</option>' +
        cuentasData.map(c => `<option value="${c.id}">${esc(c.nombre)}</option>`).join('');
    sel.value = current;
}

function filtrarTarjetasPorCuenta() {
    const idCuenta = document.getElementById('filtroCuenta').value;
    const filtradas = idCuenta
        ? todasTarjetasCache.filter(t => String(t.idCuenta) === idCuenta)
        : todasTarjetasCache;
    renderizarTodasTarjetas(filtradas);
}

function renderizarTodasTarjetas(tarjetas) {
    const tbody = document.getElementById('tbodyTodasTarjetas');
    if (!tbody) return;

    if (tarjetas.length === 0) {
        tbody.innerHTML = `<tr><td colspan="8" class="empty-row">Sin tarjetas registradas</td></tr>`;
        return;
    }

    tbody.innerHTML = tarjetas.map(t => `
        <tr>
            <td><code style="color:var(--brand-400);font-size:12px;">${esc(t.numeroTarjeta)}</code></td>
            <td style="color:var(--text-secondary);">${esc(t.nombreCuenta || '—')}</td>
            <td>${esc(t.nombreUsuario)}</td>
            <td>${esc(t.placaVehiculo || '—')}</td>
            <td>${esc(t.telefono || '—')}</td>
            <td>
                <span class="badge ${t.activo ? 'badge-success' : 'badge-danger'}">
                    ${t.activo ? '✓ Activa' : '✗ Inactiva'}
                </span>
            </td>
            <td style="color:var(--text-secondary);font-size:12px;">${t.fechaUltimoUso ? formatFecha(t.fechaUltimoUso) : '—'}</td>
            <td>
                <button class="btn btn-xs ${t.activo ? 'btn-warning' : 'btn-success'}"
                        onclick="toggleTarjetaGlobal(${t.id}, ${t.idCuenta})"
                        title="${t.activo ? 'Desactivar' : 'Activar'}">
                    <i data-lucide="${t.activo ? 'toggle-left' : 'toggle-right'}" style="width:12px;height:12px;"></i>
                    ${t.activo ? 'Desactivar' : 'Activar'}
                </button>
            </td>
        </tr>
    `).join('');

    if (window.lucide) lucide.createIcons({ root: tbody });
}

async function toggleTarjetaGlobal(id, idCuenta) {
    try {
        const res  = await fetch(`${API_TARJETAS}/${id}/toggle`, { method: 'PATCH' });
        const data = await res.json();
        if (data.exitoso) {
            mostrarToast(data.mensaje || 'Estado actualizado', 'success');
            cargarTodasTarjetas();
        } else {
            mostrarToast(data.mensaje || 'Error', 'error');
        }
    } catch {
        mostrarToast('Error de conexión', 'error');
    }
}

// ─────────────────────────────────────────────────────────────────
// UTILIDADES
// ─────────────────────────────────────────────────────────────────

function esc(str) {
    if (str == null) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function formatFecha(iso) {
    if (!iso) return '—';
    try {
        return new Date(iso).toLocaleDateString('es-SV', { day: '2-digit', month: 'short', year: 'numeric' });
    } catch { return iso; }
}

function formatHora(span) {
    // span puede ser "HH:mm:ss" o "HH:mm"
    if (!span) return '—';
    const parts = String(span).split(':');
    return `${parts[0]}:${parts[1]}`;
}

function mostrarToast(mensaje, tipo = 'info') {
    const container = document.getElementById('toastContainer');
    if (!container) return;
    const div = document.createElement('div');
    div.className = `toast toast-${tipo}`;
    div.textContent = mensaje;
    container.appendChild(div);
    setTimeout(() => div.remove(), 3500);
}

// ─────────────────────────────────────────────────────────────────
// INICIALIZACIÓN
// ─────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    cargarCuentas();
});
