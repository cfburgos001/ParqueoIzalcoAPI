// =============================================
// MÓDULO: GESTIÓN DE TARIFAS
// Edición inline (doble clic) + modal completo
// Solo ADMINISTRADOR
// =============================================

const API_TARIFAS = '/api/tarifas';
let tarifasData = [];
let inlineEditId = null;   // qué fila está en modo edición inline

const TARIFA_META = {
    'A': { icon: '🚗', color: '#3b82f6' },
    'M': { icon: '🏍️', color: '#8b5cf6' },
    'C': { icon: '🚚', color: '#f59e0b' },
    'Z': { icon: '🎁', color: '#10b981' },
    'X': { icon: '🔄', color: '#64748b' },
    'Y': { icon: '📋', color: '#475569' }
};

const CAMPOS_PRECIO = [
    { key: 'precio1Hora', label: '1ª Hora' },
    { key: 'precio2Horas', label: '2ª Hora' },
    { key: 'precioDiaCompleto', label: 'Día completo' },
    { key: 'precioPorHora', label: 'Por hora' },
    { key: 'precioMinimo', label: 'Mínimo' },
    { key: 'precioMax', label: 'Máximo' }
];

// ===== CARGAR =====
async function cargarTarifas() {
    const tbody = document.getElementById('tbodyTarifas');
    if (!tbody) return;

    tbody.innerHTML = `<tr><td colspan="10" class="empty-row">
        <div style="display:flex;align-items:center;justify-content:center;gap:10px;">
            <div class="dash-spinner"></div> Cargando tarifas…
        </div></td></tr>`;

    try {
        const res = await fetch(API_TARIFAS);
        const data = await res.json();

        if (!data.exitoso) {
            tbody.innerHTML = `<tr><td colspan="10" class="empty-row"
                style="color:var(--danger-text);">❌ ${data.mensaje}</td></tr>`;
            return;
        }

        tarifasData = data.data || [];
        renderizarFilas();

    } catch {
        tbody.innerHTML = `<tr><td colspan="10" class="empty-row"
            style="color:var(--danger-text);">❌ Error de conexión</td></tr>`;
    }
}

// ===== RENDERIZAR FILAS =====
function renderizarFilas() {
    const tbody = document.getElementById('tbodyTarifas');
    if (!tbody) return;

    if (tarifasData.length === 0) {
        tbody.innerHTML = `<tr><td colspan="10" class="empty-row">Sin tarifas</td></tr>`;
        return;
    }

    tbody.innerHTML = tarifasData.map(t => {
        const meta = TARIFA_META[t.strRateKey] || { icon: '💲', color: '#94a3b8' };
        const isEdit = inlineEditId === t.id;

        return `
        <tr id="tarRow_${t.id}" class="tar-row ${isEdit ? 'tar-row--editing' : ''}">

            <!-- Identificación -->
            <td>
                <div style="display:flex;align-items:center;gap:8px;">
                    <span style="font-size:17px;">${meta.icon}</span>
                    <div>
                        <div style="font-weight:700;font-size:13px;color:var(--text-primary);">
                            ${t.tipoTarifa}
                        </div>
                        <span class="tar-ratekey-pill" style="--c:${meta.color}">
                            ${t.strRateKey}
                        </span>
                    </div>
                </div>
            </td>

            <!-- Precios (inline editable) -->
            ${CAMPOS_PRECIO.map(c => `
            <td class="tar-cell-price" title="Doble clic para editar">
                ${isEdit
                ? `<input type="number" class="tar-inline-input"
                             id="ti_${t.id}_${c.key}"
                             value="${parseFloat(t[c.key]).toFixed(2)}"
                             min="0" step="0.01"
                             onkeydown="tarInlineKey(event, ${t.id})">`
                : `<span class="tar-price-display"
                             ondblclick="activarEdicionInline(${t.id})">
                             $${parseFloat(t[c.key]).toFixed(2)}
                       </span>`
            }
            </td>`).join('')}

            <!-- Estado Activa -->
            <td>
                <label class="tar-toggle" title="${t.activa ? 'Activa — clic para desactivar' : 'Inactiva — clic para activar'}">
                    <input type="checkbox"
                           id="tarActiva_${t.id}"
                           ${t.activa ? 'checked' : ''}
                           onchange="toggleActiva(${t.id}, this.checked)">
                    <span class="tar-toggle-slider"></span>
                </label>
                <span class="tar-activa-label ${t.activa ? 'tar-activa--on' : 'tar-activa--off'}">
                    ${t.activa ? 'Activa' : 'Inactiva'}
                </span>
            </td>

            <!-- Acciones -->
            <td>
                <div style="display:flex;gap:6px;align-items:center;">
                    ${isEdit
                ? `<button class="btn btn-xs btn-success" onclick="guardarInline(${t.id})"
                                   title="Guardar (Enter)">✓ Guardar</button>
                           <button class="btn btn-xs btn-secondary" onclick="cancelarInline()"
                                   title="Cancelar (Esc)">✕</button>`
                : `<button class="btn btn-xs btn-secondary"
                                   onclick="activarEdicionInline(${t.id})"
                                   title="Editar fila">✏️</button>
                           <button class="btn btn-xs btn-primary"
                                   onclick="abrirModalTarifa(${t.id})"
                                   title="Editar en modal">⊞</button>`
            }
                </div>
            </td>
        </tr>`;
    }).join('');
}

// ===== EDICIÓN INLINE =====
function activarEdicionInline(id) {
    if (inlineEditId !== null && inlineEditId !== id) {
        cancelarInline();   // cerrar edición anterior sin guardar
    }
    inlineEditId = id;
    renderizarFilas();

    // Focus en primer input
    setTimeout(() => {
        const first = document.getElementById(`ti_${id}_precio1Hora`);
        if (first) { first.focus(); first.select(); }
    }, 50);
}

function cancelarInline() {
    inlineEditId = null;
    renderizarFilas();
}

function tarInlineKey(event, id) {
    if (event.key === 'Enter') guardarInline(id);
    if (event.key === 'Escape') cancelarInline();
    // Tab pasa al siguiente input de la misma fila
}

async function guardarInline(id) {
    const t = tarifasData.find(x => x.id === id);
    if (!t) return;

    // Leer valores de los inputs inline
    const request = buildRequest(id, t.activa);
    if (!request) return;

    const btn = document.querySelector(`#tarRow_${id} .btn-success`);
    if (btn) { btn.disabled = true; btn.textContent = '…'; }

    const exito = await enviarActualizacion(request);

    if (exito) {
        // Actualizar datos locales
        CAMPOS_PRECIO.forEach(c => { t[c.key] = request[c.key]; });
        inlineEditId = null;
        renderizarFilas();
        mostrarToast(`✅ ${t.tipoTarifa} actualizada`, 'success');
    } else {
        if (btn) { btn.disabled = false; btn.textContent = '✓ Guardar'; }
    }
}

// Toggle activa desde la tabla (sin abrir modal)
async function toggleActiva(id, nuevoEstado) {
    const t = tarifasData.find(x => x.id === id);
    if (!t) return;

    const request = {
        id,
        precioPorHora: t.precioPorHora,
        precioMinimo: t.precioMinimo,
        precioMax: t.precioMax,
        precio1Hora: t.precio1Hora,
        precio2Horas: t.precio2Horas,
        precioDiaCompleto: t.precioDiaCompleto,
        activa: nuevoEstado
    };

    const exito = await enviarActualizacion(request);

    if (exito) {
        t.activa = nuevoEstado;
        // Actualizar solo el label sin re-render completo
        const lbl = document.querySelector(`#tarRow_${id} .tar-activa-label`);
        if (lbl) {
            lbl.textContent = nuevoEstado ? 'Activa' : 'Inactiva';
            lbl.className = `tar-activa-label ${nuevoEstado ? 'tar-activa--on' : 'tar-activa--off'}`;
        }
        mostrarToast(
            `${nuevoEstado ? '✅' : '⭕'} ${t.tipoTarifa} ${nuevoEstado ? 'activada' : 'desactivada'}`,
            nuevoEstado ? 'success' : 'warning'
        );
    } else {
        // Revertir el toggle si falló
        const cb = document.getElementById(`tarActiva_${id}`);
        if (cb) cb.checked = !nuevoEstado;
    }
}

// ===== MODAL COMPLETO =====
function abrirModalTarifa(id) {
    const t = tarifasData.find(x => x.id === id);
    if (!t) return;

    const meta = TARIFA_META[t.strRateKey] || { icon: '💲', color: '#94a3b8' };

    document.getElementById('tarModalIcon').textContent = meta.icon;
    document.getElementById('tarModalNombre').textContent = t.tipoTarifa;
    document.getElementById('tarModalKey').textContent = `strRateKey: ${t.strRateKey}`;
    document.getElementById('tarModalId').value = t.id;

    // Rellenar inputs del modal
    CAMPOS_PRECIO.forEach(c => {
        const el = document.getElementById(`tarM_${c.key}`);
        if (el) el.value = parseFloat(t[c.key]).toFixed(2);
    });

    // Toggle activa
    const cb = document.getElementById('tarM_activa');
    if (cb) {
        cb.checked = t.activa;
        actualizarLabelActiva(cb);
    }

    document.getElementById('modalEditarTarifa').style.display = 'flex';
    setTimeout(() => document.getElementById('tarM_precio1Hora')?.focus(), 80);
}

function cerrarModalTarifa() {
    document.getElementById('modalEditarTarifa').style.display = 'none';
}

function actualizarLabelActiva(cb) {
    const lbl = document.getElementById('tarM_activa_label');
    if (!lbl) return;
    lbl.textContent = cb.checked ? '● Activa' : '○ Inactiva';
    lbl.className = `tar-tog-label ${cb.checked ? 'tar-tog--on' : 'tar-tog--off'}`;
}

async function guardarModal() {
    const id = parseInt(document.getElementById('tarModalId').value);
    const t = tarifasData.find(x => x.id === id);
    if (!t) return;

    const activa = document.getElementById('tarM_activa').checked;
    const request = buildRequestFromModal(id, activa);
    if (!request) return;

    const btn = document.getElementById('btnGuardarModal');
    btn.disabled = true;
    btn.textContent = '⏳ Guardando…';

    const exito = await enviarActualizacion(request);

    if (exito) {
        // Actualizar datos locales
        CAMPOS_PRECIO.forEach(c => { t[c.key] = request[c.key]; });
        t.activa = activa;
        cerrarModalTarifa();
        renderizarFilas();
        mostrarToast(`✅ ${t.tipoTarifa} actualizada correctamente`, 'success');
    }

    btn.disabled = false;
    btn.textContent = '💾 Guardar cambios';
}

// ===== HELPERS =====
function buildRequest(id, activa) {
    const vals = {};
    for (const c of CAMPOS_PRECIO) {
        const el = document.getElementById(`ti_${id}_${c.key}`);
        const val = el ? parseFloat(el.value) : 0;
        if (isNaN(val) || val < 0) {
            mostrarToast(`El valor de "${c.label}" no es válido`, 'error');
            return null;
        }
        vals[c.key] = val;
    }
    return { id, ...vals, activa };
}

function buildRequestFromModal(id, activa) {
    const vals = {};
    for (const c of CAMPOS_PRECIO) {
        const el = document.getElementById(`tarM_${c.key}`);
        const val = el ? parseFloat(el.value) : 0;
        if (isNaN(val) || val < 0) {
            mostrarToast(`El valor de "${c.label}" no es válido`, 'error');
            return null;
        }
        vals[c.key] = val;
    }
    return { id, ...vals, activa };
}

async function enviarActualizacion(request) {
    try {
        const res = await fetch(`${API_TARIFAS}/${request.id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(request)
        });
        const data = await res.json();
        if (!data.exitoso) {
            mostrarToast(data.mensaje || 'Error al guardar', 'error');
            return false;
        }
        return true;
    } catch {
        mostrarToast('Error de conexión', 'error');
        return false;
    }
}