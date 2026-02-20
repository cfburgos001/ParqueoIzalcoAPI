// =============================================
// CONTROL DE INGRESO - CENTRO PANAMERICANO DE OJOS
// Lógica del frontend
// =============================================

// ===== CONFIGURACIÓN =====
const API_BASE = '/api/visitas';
let tiposVisitante = [];
let areasDestino = [];
let visitasHoy = [];
let filtroActual = 'TODOS';
let debounceTimer = null;
let operadorActual = null;

// ===== PROTECCIÓN DE SESIÓN =====
function verificarSesion() {
    const datos = sessionStorage.getItem('operador');
    if (!datos) {
        window.location.href = '/visitas/login.html';
        return false;
    }
    operadorActual = JSON.parse(datos);
    return true;
}

function cerrarSesion() {
    if (confirm('¿Desea cerrar sesión?')) {
        sessionStorage.removeItem('operador');
        window.location.href = '/visitas/login.html';
    }
}

// ===== INICIALIZACIÓN =====
document.addEventListener('DOMContentLoaded', () => {
    if (!verificarSesion()) return;

    // Mostrar nombre del operador en el header
    const headerRight = document.querySelector('.header-right');
    if (headerRight && operadorActual) {
        const userBox = document.createElement('div');
        userBox.className = 'user-box';
        userBox.innerHTML = `
            <span class="user-name">👤 ${operadorActual.nombreCompleto}</span>
            <button class="btn-logout" onclick="cerrarSesion()" title="Cerrar sesión">🚪</button>
        `;
        headerRight.insertBefore(userBox, headerRight.firstChild);
    }

    cargarCatalogos();
    cargarVisitasHoy();
    actualizarReloj();
    setInterval(actualizarReloj, 1000);

    // Auto-refrescar visitas cada 30 segundos
    setInterval(cargarVisitasHoy, 30000);

    // Cerrar autocompletar al hacer click fuera
    document.addEventListener('click', (e) => {
        if (!e.target.closest('.autocomplete-container')) {
            document.getElementById('autocompleteList').classList.remove('active');
        }
    });
});

// ===== RELOJ =====
function actualizarReloj() {
    const now = new Date();
    const opciones = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
    const fecha = now.toLocaleDateString('es-GT', opciones);
    const hora = now.toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
    document.getElementById('fechaHoraActual').innerHTML = `${fecha}<br><strong>${hora}</strong>`;
}

// ===== CARGAR CATÁLOGOS =====
async function cargarCatalogos() {
    try {
        const [resTipos, resAreas] = await Promise.all([
            fetch(`${API_BASE}/tipos-visitante`),
            fetch(`${API_BASE}/areas-destino`)
        ]);

        const dataTipos = await resTipos.json();
        const dataAreas = await resAreas.json();

        if (dataTipos.exitoso) {
            tiposVisitante = dataTipos.data;
            const select = document.getElementById('tipoVisitante');
            select.innerHTML = '<option value="">Seleccione...</option>';
            tiposVisitante.forEach(t => {
                select.innerHTML += `<option value="${t.id}">${t.nombre}</option>`;
            });
        }

        if (dataAreas.exitoso) {
            areasDestino = dataAreas.data;
            const select = document.getElementById('areaDestino');
            select.innerHTML = '<option value="">Seleccione...</option>';
            areasDestino.forEach(a => {
                select.innerHTML += `<option value="${a.id}">${a.nombre}</option>`;
            });
        }
    } catch (err) {
        console.error('Error al cargar catálogos:', err);
        mostrarToast('Error al cargar catálogos', 'error');
    }
}

// ===== AUTOCOMPLETAR VISITANTES =====
function buscarVisitantes(termino) {
    clearTimeout(debounceTimer);

    if (termino.length < 2) {
        document.getElementById('autocompleteList').classList.remove('active');
        return;
    }

    // Limpiar selección previa cuando el usuario escribe
    document.getElementById('idVisitante').value = '';

    debounceTimer = setTimeout(async () => {
        try {
            const res = await fetch(`${API_BASE}/visitantes/buscar?termino=${encodeURIComponent(termino)}`);
            const data = await res.json();

            const lista = document.getElementById('autocompleteList');

            if (data.exitoso && data.data && data.data.length > 0) {
                lista.innerHTML = data.data.map(v => `
                    <div class="autocomplete-item" onclick="seleccionarVisitante(${JSON.stringify(v).replace(/"/g, '&quot;')})">
                        <div>
                            <div class="item-name">${v.nombreCompleto}</div>
                            <div class="item-detail">
                                ${v.placaFrecuente ? '🚗 ' + v.placaFrecuente : ''}
                                ${v.especialidad ? ' · ' + v.especialidad : ''}
                                ${v.empresa ? ' · ' + v.empresa : ''}
                            </div>
                        </div>
                        <span class="item-badge">${v.tipoVisitante || ''}</span>
                    </div>
                `).join('');
                lista.classList.add('active');
            } else {
                lista.innerHTML = `
                    <div class="autocomplete-item" style="color: var(--gray-400); cursor: default; justify-content: center;">
                        No encontrado — se creará como nuevo visitante
                    </div>`;
                lista.classList.add('active');
            }
        } catch (err) {
            console.error('Error en autocompletar:', err);
        }
    }, 300);
}

function seleccionarVisitante(visitante) {
    document.getElementById('idVisitante').value = visitante.id;
    document.getElementById('nombreVisitante').value = visitante.nombreCompleto;
    document.getElementById('tipoVisitante').value = visitante.idTipoVisitante;
    document.getElementById('placa').value = visitante.placaFrecuente || '';
    document.getElementById('autocompleteList').classList.remove('active');
}

// ===== REGISTRAR ENTRADA =====
async function registrarEntrada(event) {
    event.preventDefault();

    const btnRegistrar = document.getElementById('btnRegistrar');
    btnRegistrar.disabled = true;
    btnRegistrar.textContent = '⏳ Registrando...';

    try {
        const idVisitante = document.getElementById('idVisitante').value;
        const nombreVisitante = document.getElementById('nombreVisitante').value.trim();
        const idTipoVisitante = parseInt(document.getElementById('tipoVisitante').value);
        const placa = document.getElementById('placa').value.trim().toUpperCase();
        const idAreaDestino = document.getElementById('areaDestino').value;
        const observacion = document.getElementById('observacion').value.trim();

        // Validaciones
        if (!nombreVisitante) {
            mostrarToast('El nombre del visitante es requerido', 'error');
            return;
        }
        if (!idTipoVisitante) {
            mostrarToast('Seleccione el tipo de visitante', 'error');
            return;
        }

        // Si no se seleccionó un visitante existente, crear uno nuevo
        let visitanteId = idVisitante ? parseInt(idVisitante) : null;

        if (!visitanteId) {
            const resCrear = await fetch(`${API_BASE}/visitantes`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    nombreCompleto: nombreVisitante,
                    idTipoVisitante: idTipoVisitante,
                    placaFrecuente: placa || null
                })
            });
            const dataCrear = await resCrear.json();
            if (dataCrear.exitoso && dataCrear.data) {
                visitanteId = dataCrear.data.id;
            }
        }

        // Registrar la entrada
        const body = {
            idVisitante: visitanteId,
            nombreVisitante: nombreVisitante,
            idTipoVisitante: idTipoVisitante,
            placa: placa || null,
            idAreaDestino: idAreaDestino ? parseInt(idAreaDestino) : null,
            observacion: observacion || null,
            idOperador: operadorActual ? operadorActual.idOperador : null,
            nombreOperador: operadorActual ? operadorActual.nombreCompleto : null
        };

        const res = await fetch(`${API_BASE}/entrada`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        const data = await res.json();

        if (data.exitoso) {
            mostrarToast(`✅ ${nombreVisitante} — Entrada registrada`, 'success');
            limpiarFormulario();
            cargarVisitasHoy();
        } else {
            mostrarToast(data.mensaje || 'Error al registrar entrada', 'error');
        }
    } catch (err) {
        console.error('Error al registrar entrada:', err);
        mostrarToast('Error de conexión al servidor', 'error');
    } finally {
        btnRegistrar.disabled = false;
        btnRegistrar.textContent = '✅ Registrar Entrada';
    }
}

// ===== REGISTRAR SALIDA =====
async function registrarSalida(id, nombre) {
    if (!confirm(`¿Registrar salida de ${nombre}?`)) return;

    try {
        const res = await fetch(`${API_BASE}/salida/${id}`, { method: 'PUT' });
        const data = await res.json();

        if (data.exitoso) {
            mostrarToast(`🔴 ${nombre} — Salida registrada`, 'success');
            cargarVisitasHoy();
        } else {
            mostrarToast(data.mensaje || 'Error al registrar salida', 'error');
        }
    } catch (err) {
        console.error('Error al registrar salida:', err);
        mostrarToast('Error de conexión', 'error');
    }
}

// ===== CARGAR VISITAS DEL DÍA =====
async function cargarVisitasHoy() {
    try {
        const [resVisitas, resEstadisticas] = await Promise.all([
            fetch(`${API_BASE}/hoy`),
            fetch(`${API_BASE}/estadisticas`)
        ]);

        const dataVisitas = await resVisitas.json();
        const dataStats = await resEstadisticas.json();

        // Actualizar estadísticas
        if (dataStats.exitoso && dataStats.data) {
            document.getElementById('statsNumDentro').textContent = dataStats.data.visitantesDentro || 0;
            document.getElementById('statsNumTotal').textContent = dataStats.data.totalVisitas || 0;
        }

        // Actualizar tabla
        if (dataVisitas.exitoso) {
            visitasHoy = dataVisitas.data || [];
            renderizarTabla();
        }
    } catch (err) {
        console.error('Error al cargar visitas:', err);
    }
}

// ===== RENDERIZAR TABLA =====
function renderizarTabla() {
    const tbody = document.getElementById('tbodyVisitas');
    let visitas = visitasHoy;

    // Aplicar filtro
    if (filtroActual !== 'TODOS') {
        visitas = visitas.filter(v => v.estado === filtroActual);
    }

    if (visitas.length === 0) {
        tbody.innerHTML = `<tr><td colspan="10" class="empty-row">No hay visitas${filtroActual !== 'TODOS' ? ' con este filtro' : ' registradas hoy'}</td></tr>`;
        return;
    }

    tbody.innerHTML = visitas.map(v => {
        const horaEntrada = new Date(v.horaEntrada).toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit' });
        const horaSalida = v.horaSalida
            ? new Date(v.horaSalida).toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit' })
            : '—';

        const minutos = v.minutosEstancia || 0;
        const horas = Math.floor(minutos / 60);
        const mins = minutos % 60;
        const tiempo = horas > 0 ? `${horas}h ${mins}m` : `${mins}m`;

        const esDentro = v.estado === 'DENTRO';
        const badgeClass = esDentro ? 'badge-dentro' : 'badge-salio';
        const badgeText = esDentro ? '🟢 Dentro' : '🔴 Salió';

        const nombreEscapado = (v.nombreVisitante || '').replace(/'/g, "\\'");

        return `
            <tr>
                <td><strong>${horaEntrada}</strong></td>
                <td>${v.nombreVisitante || ''}</td>
                <td>${v.tipoVisitante || ''}</td>
                <td>${v.placa || '—'}</td>
                <td>${v.areaDestino || '—'}</td>
                <td>${horaSalida}</td>
                <td>${tiempo}</td>
                <td>${v.observacion || '—'}
                    <button class="btn-table btn-obs" onclick="abrirModalObservacion(${v.id}, '${(v.observacion || '').replace(/'/g, "\\'")}')">✏️</button>
                </td>
                <td><span class="badge ${badgeClass}">${badgeText}</span></td>
                <td>
                    ${esDentro
                ? `<button class="btn-table btn-salida" onclick="registrarSalida(${v.id}, '${nombreEscapado}')">🚪 Salida</button>`
                : '—'}
                </td>
            </tr>
        `;
    }).join('');
}

// ===== FILTRAR TABLA =====
function filtrarTabla(filtro, btn) {
    filtroActual = filtro;
    document.querySelectorAll('.btn-filter').forEach(b => b.classList.remove('active'));
    if (btn) btn.classList.add('active');
    renderizarTabla();
}

// ===== OBSERVACIÓN MODAL =====
function abrirModalObservacion(id, obsActual) {
    document.getElementById('modalObsId').value = id;
    document.getElementById('modalObsTexto').value = obsActual || '';
    document.getElementById('modalObservacion').style.display = 'flex';
    document.getElementById('modalObsTexto').focus();
}

function cerrarModalObservacion() {
    document.getElementById('modalObservacion').style.display = 'none';
}

async function guardarObservacion() {
    const id = document.getElementById('modalObsId').value;
    const observacion = document.getElementById('modalObsTexto').value.trim();

    try {
        const res = await fetch(`${API_BASE}/observacion/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ observacion: observacion })
        });

        const data = await res.json();

        if (data.exitoso) {
            mostrarToast('Observación actualizada', 'success');
            cerrarModalObservacion();
            cargarVisitasHoy();
        } else {
            mostrarToast(data.mensaje || 'Error al guardar', 'error');
        }
    } catch (err) {
        mostrarToast('Error de conexión', 'error');
    }
}

// ===== ADMINISTRACIÓN =====
function toggleAdmin() {
    const section = document.getElementById('adminSection');
    const isVisible = section.style.display !== 'none';
    section.style.display = isVisible ? 'none' : 'block';

    if (!isVisible) {
        cargarListaAdmin();
    }
}

async function cargarListaAdmin() {
    // Listar tipos de visitante
    const ulTipos = document.getElementById('listaTiposVisitante');
    ulTipos.innerHTML = tiposVisitante.map(t => `<li>${t.nombre}</li>`).join('');

    // Listar áreas destino
    const ulAreas = document.getElementById('listaAreasDestino');
    ulAreas.innerHTML = areasDestino.map(a => `<li>${a.nombre}</li>`).join('');
}

async function crearTipoVisitante() {
    const input = document.getElementById('nuevoTipoVisitante');
    const nombre = input.value.trim();
    if (!nombre) return;

    try {
        const res = await fetch(`${API_BASE}/tipos-visitante`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ nombre: nombre })
        });
        const data = await res.json();

        if (data.exitoso) {
            mostrarToast(`Tipo "${nombre}" creado`, 'success');
            input.value = '';
            await cargarCatalogos();
            cargarListaAdmin();
        } else {
            mostrarToast(data.mensaje || 'Error al crear', 'error');
        }
    } catch (err) {
        mostrarToast('Error de conexión', 'error');
    }
}

async function crearAreaDestino() {
    const input = document.getElementById('nuevaAreaDestino');
    const nombre = input.value.trim();
    if (!nombre) return;

    try {
        const res = await fetch(`${API_BASE}/areas-destino`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ nombre: nombre })
        });
        const data = await res.json();

        if (data.exitoso) {
            mostrarToast(`Área "${nombre}" creada`, 'success');
            input.value = '';
            await cargarCatalogos();
            cargarListaAdmin();
        } else {
            mostrarToast(data.mensaje || 'Error al crear', 'error');
        }
    } catch (err) {
        mostrarToast('Error de conexión', 'error');
    }
}

// ===== UTILIDADES =====
function limpiarFormulario() {
    document.getElementById('formEntrada').reset();
    document.getElementById('idVisitante').value = '';
    document.getElementById('autocompleteList').classList.remove('active');
    document.getElementById('nombreVisitante').focus();
}

function mostrarToast(mensaje, tipo = 'success') {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast toast-${tipo}`;
    toast.textContent = mensaje;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100px)';
        toast.style.transition = 'all 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}