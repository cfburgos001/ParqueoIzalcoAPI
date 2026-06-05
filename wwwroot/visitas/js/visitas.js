// =============================================
// CONTROL DE INGRESO - SISTEMA DE PARQUEO
// Lógica del frontend
// =============================================

const API_BASE = '/api/visitas';
let operadorActual = null;
let tiposVisitante = [];
let areasDestino = [];
let visitasHoy = [];
let filtroActual = 'TODOS';
let debounceTimer = null;
let lastTableHTML = ''; // Anti-Blinking / Anti-Parpadeo

// ===== PROTECCIÓN DE SESIÓN + PERMISOS =====
function verificarSesion() {
    const datos = sessionStorage.getItem('operador');
    if (!datos) {
        window.location.href = '/visitas/login.html';
        return false;
    }
    operadorActual = JSON.parse(datos);

    document.body.classList.add('role-' + operadorActual.tipoUsuario);

    const sidebarName = document.getElementById('sidebarUserName');
    const sidebarRole = document.getElementById('sidebarUserRole');
    if (sidebarName) sidebarName.textContent = operadorActual.nombreCompleto;
    if (sidebarRole) sidebarRole.textContent = operadorActual.tipoUsuario;

    return true;
}

function abrirModalCerrarSesion() {
    document.getElementById('modalCerrarSesion').style.display = 'flex';
}

function confirmarCerrarSesion() {
    sessionStorage.removeItem('operador');
    window.location.href = '/visitas/login.html';
}

function cerrarSesion() {
    abrirModalCerrarSesion();
}

// ===== INICIALIZACIÓN =====
const DASHBOARD_INTERVAL_MS = 30000; // 30 segundos
let dashboardIntervalId = null;

document.addEventListener('DOMContentLoaded', () => {
    if (!verificarSesion()) return;

    cargarConfigSitio();
    cargarCatalogos();
    cargarVisitasHoy();
    actualizarReloj();
    setInterval(actualizarReloj, 1000);
    setInterval(cargarVisitasHoy, 30000);

    const navInicio = document.querySelector('.nav-item[data-page="inicio"]');
    navegarA('inicio', navInicio);

    document.addEventListener('click', (e) => {
        if (!e.target.closest('.autocomplete-container')) {
            const autoList = document.getElementById('autocompleteList');
            if (autoList) autoList.classList.remove('active');
        }
    });
});

// ===== RELOJ =====
function actualizarReloj() {
    const now = new Date();
    const opciones = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
    const fecha = now.toLocaleDateString('es-GT', opciones);
    const hora = now.toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
    const elFecha = document.getElementById('fechaActual');
    const elHora = document.getElementById('horaActual');
    if (elFecha) elFecha.textContent = fecha;
    if (elHora) elHora.textContent = hora;
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
            if (select) {
                select.innerHTML = '<option value="">Seleccione...</option>';
                tiposVisitante.forEach(t => {
                    select.innerHTML += `<option value="${t.id}">${t.nombre}</option>`;
                });
            }
        }

        if (dataAreas.exitoso) {
            areasDestino = dataAreas.data;
            const select = document.getElementById('areaDestino');
            if (select) {
                select.innerHTML = '<option value="">Seleccione...</option>';
                areasDestino.forEach(a => {
                    select.innerHTML += `<option value="${a.id}">${a.nombre}</option>`;
                });
            }
        }
    } catch (err) {
        console.error('Error al cargar catálogos:', err);
        mostrarToast('Error al cargar catálogos', 'error');
    }
}

// ===== AUTOCOMPLETAR VISITANTES =====
function buscarVisitante(termino) {
    clearTimeout(debounceTimer);

    if (termino.length < 2) {
        document.getElementById('autocompleteList').classList.remove('active');
        return;
    }

    document.getElementById('visitanteId').value = '';

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
                                ${v.placaFrecuente ? 'Vehículo: ' + v.placaFrecuente : ''}
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
                        No encontrado — se creará como nuevo registro
                    </div>`;
                lista.classList.add('active');
            }
        } catch (err) {
            console.error('Error en autocompletar:', err);
        }
    }, 300);
}

function seleccionarVisitante(visitante) {
    document.getElementById('visitanteId').value = visitante.id;
    document.getElementById('nombreVisitante').value = visitante.nombreCompleto;
    document.getElementById('tipoVisitante').value = visitante.idTipoVisitante;
    document.getElementById('placaVisitante').value = visitante.placaFrecuente || '';
    document.getElementById('autocompleteList').classList.remove('active');
}

// ===== REGISTRAR ENTRADA =====
let registrando = false;
async function registrarEntrada() {
    if (registrando) return;
    registrando = true;

    try {
        const idVisitante = document.getElementById('visitanteId').value;
        const nombreVisitante = document.getElementById('nombreVisitante').value.trim();
        const idTipoVisitante = parseInt(document.getElementById('tipoVisitante').value);
        const placa = document.getElementById('placaVisitante').value.trim().toUpperCase();
        const idAreaDestino = document.getElementById('areaDestino').value;
        const observacion = document.getElementById('observacion').value.trim();

        if (!nombreVisitante) {
            mostrarToast('El nombre o identificador es requerido', 'error');
            registrando = false;
            return;
        }
        if (!idTipoVisitante) {
            mostrarToast('Seleccione la clasificación del visitante', 'error');
            registrando = false;
            return;
        }

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
            mostrarToast(`Entrada registrada para ${nombreVisitante}`, 'success');
            limpiarFormulario();
            cargarVisitasHoy();
        } else {
            mostrarToast(data.mensaje || 'Error al registrar entrada', 'error');
        }
    } catch (err) {
        console.error('Error al registrar entrada:', err);
        mostrarToast('Error de conexión al servidor', 'error');
    } finally {
        registrando = false;
    }
}

// ===== REGISTRAR SALIDA =====
async function registrarSalida(id, nombre) {
    if (!confirm(`¿Registrar salida de ${nombre}?`)) return;

    try {
        const res = await fetch(`${API_BASE}/salida/${id}`, { method: 'PUT' });
        const data = await res.json();

        if (data.exitoso) {
            mostrarToast(`Salida registrada para ${nombre}`, 'success');
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

        if (dataStats.exitoso && dataStats.data) {
            const statDentro = document.getElementById('statDentro');
            const statHoy = document.getElementById('statHoy');
            if (statDentro) statDentro.textContent = `${dataStats.data.visitantesDentro || 0} Dentro`;
            if (statHoy) statHoy.textContent = `${dataStats.data.totalVisitas || 0} Hoy`;
        }

        if (dataVisitas.exitoso) {
            visitasHoy = dataVisitas.data || [];
            renderizarTabla();
        }
    } catch (err) {
        console.error('Error al cargar visitas:', err);
    }
}

// ===== RENDERIZAR TABLA CON ANTI-PARPADEO Y LUCIDE =====
function renderizarTabla() {
    const tbody = document.getElementById('tbodyVisitas');
    if (!tbody) return;

    let visitas = visitasHoy;

    if (filtroActual !== 'TODOS') {
        visitas = visitas.filter(v => v.estado === filtroActual);
    }

    if (visitas.length === 0) {
        const newHTML = `<tr><td colspan="10" class="empty-row">No hay registros hoy</td></tr>`;
        if (lastTableHTML !== newHTML) {
            lastTableHTML = newHTML;
            tbody.innerHTML = newHTML;
        }
        return;
    }

    const newHTML = visitas.map(v => {
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
        // Quitamos los emojis y usamos puntos CSS sencillos
        const badgeText = esDentro ? '● Dentro' : '● Salió';

        const nombreEscapado = (v.nombreVisitante || '').replace(/'/g, "\\'");

        return `
            <tr>
                <td><strong>${horaEntrada}</strong></td>
                <td>${v.nombreVisitante || ''}</td>
                <td><span style="font-size: 11px; background: rgba(59,130,246,0.1); color: var(--brand-600); padding: 2px 6px; border-radius: 4px;">${v.tipoVisitante || ''}</span></td>
                <td>${v.placa || '—'}</td>
                <td>${v.areaDestino || '—'}</td>
                <td>${horaSalida}</td>
                <td>${tiempo}</td>
                <td>${v.observacion || '—'}
                    <button class="btn-table btn-obs" style="background: none; border: none; cursor: pointer; color: var(--text-tertiary);" onclick="abrirModalObservacion(${v.id}, '${(v.observacion || '').replace(/'/g, "\\'")}')" title="Editar detalle">
                        <i data-lucide="edit-3" style="width: 14px; height: 14px;"></i>
                    </button>
                </td>
                <td><span class="badge ${badgeClass}" style="border: 1px solid transparent;">${badgeText}</span></td>
                <td>
                    ${esDentro
                ? `<button class="btn-table btn-salida" onclick="registrarSalida(${v.id}, '${nombreEscapado}')" style="display:flex; align-items:center; gap:4px; font-weight:600;"><i data-lucide="log-out" style="width:14px; height:14px;"></i> Salida</button>`
                : '—'}
                </td>
            </tr>
        `;
    }).join('');

    // MAGIA ANTI-PARPADEO
    if (lastTableHTML !== newHTML) {
        lastTableHTML = newHTML;
        tbody.innerHTML = newHTML;
        if (window.lucide) lucide.createIcons({ root: tbody });
    }
}

// ===== FILTRAR TABLA =====
function filtrarVisitas(filtro, btn) {
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
            mostrarToast('Detalle actualizado exitosamente', 'success');
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
function mostrarFormAdmin() {
    const section = document.getElementById('adminSection');
    const isVisible = section.style.display !== 'none';
    section.style.display = isVisible ? 'none' : 'block';

    if (!isVisible) {
        cargarListaAdmin();
    }
}

async function cargarListaAdmin() {
    const ulTipos = document.getElementById('listaTiposVisitante');
    if (ulTipos) ulTipos.innerHTML = tiposVisitante.map(t => `<li>${t.nombre}</li>`).join('');

    const ulAreas = document.getElementById('listaAreasDestino');
    if (ulAreas) ulAreas.innerHTML = areasDestino.map(a => `<li>${a.nombre}</li>`).join('');
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
            mostrarToast(`Clasificación "${nombre}" creada`, 'success');
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
    document.getElementById('nombreVisitante').value = '';
    document.getElementById('visitanteId').value = '';
    document.getElementById('tipoVisitante').selectedIndex = 0;
    document.getElementById('placaVisitante').value = '';
    document.getElementById('areaDestino').selectedIndex = 0;
    document.getElementById('observacion').value = '';
    const autoList = document.getElementById('autocompleteList');
    if (autoList) autoList.classList.remove('active');
    document.getElementById('nombreVisitante').focus();
}

function mostrarToast(mensaje, tipo = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;
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


// =============================================
// REPORTES - EXCEL Y PDF
// =============================================

document.addEventListener('DOMContentLoaded', () => {
    const hoy = new Date().toISOString().split('T')[0];
    const inputInicio = document.getElementById('reporteFechaInicio');
    const inputFin = document.getElementById('reporteFechaFin');
    if (inputInicio) inputInicio.value = hoy;
    if (inputFin) inputFin.value = hoy;

    setTimeout(() => {
        const selectTipo = document.getElementById('reporteTipo');
        if (selectTipo && tiposVisitante.length > 0) {
            selectTipo.innerHTML = '<option value="">Todos</option>';
            tiposVisitante.forEach(t => {
                selectTipo.innerHTML += `<option value="${t.id}">${t.nombre}</option>`;
            });
        }
    }, 2000);
});

async function buscarDatosReporte() {
    const fechaInicio = document.getElementById('reporteFechaInicio').value;
    const fechaFin = document.getElementById('reporteFechaFin').value;
    const idTipo = document.getElementById('reporteTipo').value;

    if (!fechaInicio) {
        mostrarToast('Seleccione la fecha de inicio', 'error');
        return null;
    }

    try {
        const body = {
            fechaInicio: fechaInicio + 'T00:00:00',
            fechaFin: (fechaFin || fechaInicio) + 'T23:59:59',
            idTipoVisitante: idTipo ? parseInt(idTipo) : null,
            top: 5000
        };

        const res = await fetch(`${API_BASE}/buscar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        const data = await res.json();

        if (data.exitoso && data.data) {
            mostrarVistaPrevia(data.data, fechaInicio, fechaFin);
            return data.data;
        } else {
            mostrarToast('No se encontraron registros', 'warning');
            return null;
        }
    } catch (err) {
        console.error('Error al buscar datos para reporte:', err);
        mostrarToast('Error de conexión', 'error');
        return null;
    }
}

function mostrarVistaPrevia(datos, fechaInicio, fechaFin) {
    const preview = document.getElementById('reportePreview');
    const titulo = document.getElementById('reporteTitulo');
    const conteo = document.getElementById('reporteConteo');
    const tbody = document.getElementById('tbodyReporte');
    if (!preview) return;

    titulo.textContent = `Bitácora: ${formatearFechaCorta(fechaInicio)}${fechaFin && fechaFin !== fechaInicio ? ' al ' + formatearFechaCorta(fechaFin) : ''}`;
    conteo.textContent = `${datos.length} registros`;

    tbody.innerHTML = datos.map(v => {
        const fecha = new Date(v.fechaVisita || v.horaEntrada).toLocaleDateString('es-GT');
        const entrada = new Date(v.horaEntrada).toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit' });
        const salida = v.horaSalida ? new Date(v.horaSalida).toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit' }) : '—';
        const minutos = v.minutosEstancia || 0;
        const horas = Math.floor(minutos / 60);
        const mins = minutos % 60;
        const tiempo = horas > 0 ? `${horas}h ${mins}m` : `${mins}m`;

        return `<tr>
            <td>${fecha}</td>
            <td>${v.nombreVisitante || ''}</td>
            <td>${v.tipoVisitante || ''}</td>
            <td>${v.placa || '—'}</td>
            <td>${entrada}</td>
            <td>${salida}</td>
            <td>${tiempo}</td>
            <td>${v.areaDestino || '—'}</td>
            <td>${v.observacion || '—'}</td>
        </tr>`;
    }).join('');

    preview.style.display = 'block';
    preview.scrollIntoView({ behavior: 'smooth' });
}

// =============================================
// DESCARGAR EXCEL
// =============================================
async function descargarExcel() {
    const datos = await buscarDatosReporte();
    if (!datos || datos.length === 0) return;

    const fechaInicio = document.getElementById('reporteFechaInicio').value;
    const fechaFin = document.getElementById('reporteFechaFin').value || fechaInicio;

    let xml = '<?xml version="1.0" encoding="UTF-8"?>\n';
    xml += '<?mso-application progid="Excel.Sheet"?>\n';
    xml += '<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"\n';
    xml += ' xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">\n';

    xml += '<Styles>\n';
    xml += '  <Style ss:ID="titulo"><Font ss:Bold="1" ss:Size="14"/><Alignment ss:Horizontal="Center"/></Style>\n';
    xml += '  <Style ss:ID="subtitulo"><Font ss:Bold="1" ss:Size="11"/><Alignment ss:Horizontal="Center"/></Style>\n';
    xml += '  <Style ss:ID="header"><Font ss:Bold="1" ss:Size="10" ss:Color="#FFFFFF"/><Interior ss:Color="#1F2937" ss:Pattern="Solid"/><Alignment ss:Horizontal="Center"/><Borders><Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1"/></Borders></Style>\n';
    xml += '  <Style ss:ID="celda"><Borders><Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E5E7EB"/></Borders></Style>\n';
    xml += '  <Style ss:ID="fecha"><NumberFormat ss:Format="dd/mm/yyyy"/><Borders><Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E5E7EB"/></Borders></Style>\n';
    xml += '</Styles>\n';

    xml += '<Worksheet ss:Name="Bitácora de Accesos">\n';
    xml += '<Table>\n';

    xml += '<Column ss:Width="90"/><Column ss:Width="200"/><Column ss:Width="100"/><Column ss:Width="90"/><Column ss:Width="80"/><Column ss:Width="80"/><Column ss:Width="70"/><Column ss:Width="150"/><Column ss:Width="200"/>\n';

    xml += '<Row ss:Height="25">';
    xml += `<Cell ss:MergeAcross="8" ss:StyleID="titulo"><Data ss:Type="String">${getSitioRazonSocial()}</Data></Cell>`;
    xml += '</Row>\n';

    xml += '<Row ss:Height="20">';
    xml += `<Cell ss:MergeAcross="8" ss:StyleID="subtitulo"><Data ss:Type="String">${sitioConfig?.slogan ?? 'Control de Accesos y Vehículos'}</Data></Cell>`;
    xml += '</Row>\n';

    const periodoTexto = fechaInicio === fechaFin
        ? `Fecha: ${formatearFechaLarga(fechaInicio)}`
        : `Período: ${formatearFechaLarga(fechaInicio)} al ${formatearFechaLarga(fechaFin)}`;

    xml += '<Row ss:Height="18">';
    xml += `<Cell ss:MergeAcross="8" ss:StyleID="subtitulo"><Data ss:Type="String">${periodoTexto}</Data></Cell>`;
    xml += '</Row>\n';

    xml += '<Row></Row>\n';

    xml += '<Row>';
    const headers = ['FECHA DE ACCESO', 'NOMBRE / IDENTIFICADOR', 'CLASIFICACIÓN', 'PLACA #', 'ENTRADA', 'SALIDA', 'TIEMPO', 'ÁREA DESTINO', 'DETALLE'];
    headers.forEach(h => {
        xml += `<Cell ss:StyleID="header"><Data ss:Type="String">${h}</Data></Cell>`;
    });
    xml += '</Row>\n';

    datos.forEach(v => {
        const fecha = new Date(v.fechaVisita || v.horaEntrada).toLocaleDateString('es-GT');
        const entrada = new Date(v.horaEntrada).toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit' });
        const salida = v.horaSalida ? new Date(v.horaSalida).toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit' }) : '';
        const minutos = v.minutosEstancia || 0;
        const horas = Math.floor(minutos / 60);
        const mins = minutos % 60;
        const tiempo = horas > 0 ? `${horas}h ${mins}m` : `${mins}m`;

        xml += '<Row>';
        xml += `<Cell ss:StyleID="celda"><Data ss:Type="String">${fecha}</Data></Cell>`;
        xml += `<Cell ss:StyleID="celda"><Data ss:Type="String">${escapeXml(v.nombreVisitante || '')}</Data></Cell>`;
        xml += `<Cell ss:StyleID="celda"><Data ss:Type="String">${escapeXml(v.tipoVisitante || '')}</Data></Cell>`;
        xml += `<Cell ss:StyleID="celda"><Data ss:Type="String">${escapeXml(v.placa || '')}</Data></Cell>`;
        xml += `<Cell ss:StyleID="celda"><Data ss:Type="String">${entrada}</Data></Cell>`;
        xml += `<Cell ss:StyleID="celda"><Data ss:Type="String">${salida}</Data></Cell>`;
        xml += `<Cell ss:StyleID="celda"><Data ss:Type="String">${tiempo}</Data></Cell>`;
        xml += `<Cell ss:StyleID="celda"><Data ss:Type="String">${escapeXml(v.areaDestino || '')}</Data></Cell>`;
        xml += `<Cell ss:StyleID="celda"><Data ss:Type="String">${escapeXml(v.observacion || '')}</Data></Cell>`;
        xml += '</Row>\n';
    });

    xml += '</Table>\n</Worksheet>\n</Workbook>';

    const blob = new Blob([xml], { type: 'application/vnd.ms-excel' });
    const nombreArchivo = `Bitacora_Accesos_${fechaInicio.replace(/-/g, '')}${fechaFin !== fechaInicio ? '_al_' + fechaFin.replace(/-/g, '') : ''}.xls`;
    descargarBlob(blob, nombreArchivo);

    mostrarToast(`Excel descargado: ${datos.length} registros`, 'success');
}

// =============================================
// DESCARGAR PDF
// =============================================
async function descargarPDF() {
    const datos = await buscarDatosReporte();
    if (!datos || datos.length === 0) return;

    const fechaInicio = document.getElementById('reporteFechaInicio').value;
    const fechaFin = document.getElementById('reporteFechaFin').value || fechaInicio;

    const periodoTexto = fechaInicio === fechaFin
        ? `Fecha: ${formatearFechaLarga(fechaInicio)}`
        : `Período: ${formatearFechaLarga(fechaInicio)} al ${formatearFechaLarga(fechaFin)}`;

    const htmlContent = `
    <!DOCTYPE html>
    <html lang="es">
    <head>
        <meta charset="UTF-8">
        <title>Bitácora de Accesos</title>
        <style>
            @page { size: landscape; margin: 15mm; }
            body { font-family: Arial, Helvetica, sans-serif; color: #1f2937; margin: 0; padding: 20px; }
            .header-report { text-align: center; margin-bottom: 20px; border-bottom: 3px solid #1f2937; padding-bottom: 15px; }
            .header-report h1 { font-size: 18px; margin: 0 0 4px; letter-spacing: 1px; }
            .header-report h2 { font-size: 13px; font-weight: 600; color: #374151; margin: 0 0 8px; }
            .header-report p { font-size: 12px; color: #6b7280; margin: 0; }
            table { width: 100%; border-collapse: collapse; font-size: 11px; margin-top: 10px; }
            thead th {
                background: #1f2937; color: #fff; padding: 8px 6px;
                text-align: left; font-size: 10px; text-transform: uppercase;
                letter-spacing: 0.5px; border: 1px solid #1f2937;
            }
            tbody td { padding: 7px 6px; border: 1px solid #e5e7eb; vertical-align: top; }
            tbody tr:nth-child(even) { background: #f9fafb; }
            .footer-report {
                margin-top: 20px; padding-top: 10px; border-top: 1px solid #e5e7eb;
                font-size: 10px; color: #9ca3af; display: flex; justify-content: space-between;
            }
            .badge-tipo {
                display: inline-block; padding: 2px 6px; border-radius: 10px;
                font-size: 9px; font-weight: 600; background: #e8effc; color: #1a56db;
            }
        </style>
    </head>
    <body>
        <div class="header-report">
            <h1>${getSitioRazonSocial()}</h1>
            <h2>${sitioConfig?.slogan ?? 'Sistema de Control de Parqueos'}</h2>
            <p>${periodoTexto} &nbsp;|&nbsp; Total: ${datos.length} registros</p>
        </div>

        <table>
            <thead>
                <tr>
                    <th>FECHA DE ACCESO</th>
                    <th>IDENTIFICADOR / VISITANTE</th>
                    <th>CLASIFICACIÓN</th>
                    <th>PLACA #</th>
                    <th>ENTRADA</th>
                    <th>SALIDA</th>
                    <th>TIEMPO</th>
                    <th>ÁREA DESTINO</th>
                    <th>DETALLE</th>
                </tr>
            </thead>
            <tbody>
                ${datos.map(v => {
        const fecha = new Date(v.fechaVisita || v.horaEntrada).toLocaleDateString('es-GT');
        const entrada = new Date(v.horaEntrada).toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit' });
        const salida = v.horaSalida ? new Date(v.horaSalida).toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit' }) : '';
        const minutos = v.minutosEstancia || 0;
        const horas = Math.floor(minutos / 60);
        const mins = minutos % 60;
        const tiempo = horas > 0 ? `${horas}h ${mins}m` : `${mins}m`;

        return `<tr>
                        <td>${fecha}</td>
                        <td><strong>${v.nombreVisitante || ''}</strong></td>
                        <td><span class="badge-tipo">${v.tipoVisitante || ''}</span></td>
                        <td>${v.placa || ''}</td>
                        <td>${entrada}</td>
                        <td>${salida}</td>
                        <td>${tiempo}</td>
                        <td>${v.areaDestino || ''}</td>
                        <td>${v.observacion || ''}</td>
                    </tr>`;
    }).join('')}
            </tbody>
        </table>

        <div class="footer-report">
            <span>${getSitioFooter()} — Plataforma IOT</span>
            <span>Generado: ${new Date().toLocaleString('es-GT')}</span>
        </div>

        <script>
            window.onload = () => { window.print(); };
        <\/script>
    </body>
    </html>`;

    const ventana = window.open('', '_blank');
    ventana.document.write(htmlContent);
    ventana.document.close();

    mostrarToast(`PDF generado con éxito.`, 'success');
}

// =============================================
// UTILIDADES DE REPORTE
// =============================================
function escapeXml(str) {
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&apos;');
}

// =============================================
// DASHBOARD — KPIs + GRÁFICOS
// =============================================
let _chartSemanal = null;
let _chartEstado = null;
let _chartPagos = null;

async function cargarDashboard() {
    const loading = document.getElementById('dashLoading');
    const errorEl = document.getElementById('dashError');
    const kpiGrid = document.getElementById('kpiGrid');
    const lastUpdate = document.getElementById('dashLastUpdate');

    if (!loading) return;

    loading.style.display = 'flex';
    if (errorEl) errorEl.style.display = 'none';
    if (kpiGrid) kpiGrid.style.opacity = '0.4';

    try {
        const res = await fetch(`${API_BASE}/dashboard`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();

        if (!data.exitoso || !data.data) throw new Error(data.mensaje || 'Sin datos');

        const d = data.data;

        document.getElementById('kpiDentro').textContent = d.vehiculosDentroHoy ?? 0;
        document.getElementById('kpiHoy').textContent = d.totalVehiculosHoy ?? 0;
        document.getElementById('kpiSemana').textContent = d.vehiculosDentroSemana ?? 0;
        document.getElementById('kpiTiempo').textContent = formatearTiempo(d.tiempoPromedioEstanciaMin);
        document.getElementById('kpiMonto').textContent = formatearMonto(d.montoPromedioCobrado);
        document.getElementById('kpiMontoTotal').textContent = formatearMonto(d.montoTotalDia);

        const statDentro = document.getElementById('statDentro');
        const statHoy = document.getElementById('statHoy');
        if (statDentro) statDentro.textContent = `${d.vehiculosDentroHoy ?? 0} Dentro`;
        if (statHoy) statHoy.textContent = `${d.totalVehiculosHoy ?? 0} Hoy`;

        renderizarGraficos(d);

        const hora = new Date().toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        if (lastUpdate) lastUpdate.textContent = `Actualizado: ${hora}`;
        if (kpiGrid) kpiGrid.style.opacity = '1';
        if (errorEl) errorEl.style.display = 'none';
    } catch (err) {
        console.error('Error al cargar dashboard:', err);
        if (errorEl) {
            document.getElementById('dashErrorMsg').textContent = `No se pudo obtener los datos: ${err.message}`;
            errorEl.style.display = 'flex';
        }
        if (kpiGrid) kpiGrid.style.opacity = '0.3';
    } finally {
        if (loading) loading.style.display = 'none';
    }
}

function renderizarGraficos(d) {
    const semana = d.vehiculosPorDiaSemana ?? [];
    const estadoHoy = d.estadoHoy ?? { dentro: 0, salio: 0 };
    const pagoHoy = d.pagoHoy ?? { pagados: 0, noPagados: 0 };

    const ctxSemanal = document.getElementById('chartSemanal');
    if (ctxSemanal) {
        const labels = semana.map(x => x.label);
        const totals = semana.map(x => x.total);
        if (_chartSemanal) {
            _chartSemanal.data.labels = labels;
            _chartSemanal.data.datasets[0].data = totals;
            _chartSemanal.update();
        } else {
            _chartSemanal = new Chart(ctxSemanal, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [{
                        label: 'Vehículos',
                        data: totals,
                        backgroundColor: 'rgba(99, 102, 241, 0.75)',
                        borderColor: 'rgba(99, 102, 241, 1)',
                        borderWidth: 1,
                        borderRadius: 6,
                        borderSkipped: false
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: { precision: 0, color: '#6b7280' },
                            grid: { color: 'rgba(107,114,128,0.15)' }
                        },
                        x: { ticks: { color: '#6b7280' }, grid: { display: false } }
                    }
                }
            });
        }
    }

    const ctxEstado = document.getElementById('chartEstado');
    if (ctxEstado) {
        const labelsE = ['Dentro', 'Salió'];
        const dataE = [estadoHoy.dentro, estadoHoy.salio];
        if (_chartEstado) {
            _chartEstado.data.datasets[0].data = dataE;
            _chartEstado.update();
        } else {
            _chartEstado = new Chart(ctxEstado, {
                type: 'doughnut',
                data: {
                    labels: labelsE,
                    datasets: [{
                        data: dataE,
                        backgroundColor: ['rgba(16,185,129,0.8)', 'rgba(239,68,68,0.8)'],
                        borderColor: ['rgba(16,185,129,1)', 'rgba(239,68,68,1)'],
                        borderWidth: 2,
                        hoverOffset: 6
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    cutout: '65%',
                    plugins: {
                        legend: { position: 'bottom', labels: { color: '#6b7280', padding: 14 } }
                    }
                }
            });
        }
    }

    const ctxPagos = document.getElementById('chartPagos');
    if (ctxPagos) {
        const labelsP = ['Pagados', 'Pendientes'];
        const dataP = [pagoHoy.pagados, pagoHoy.noPagados];
        if (_chartPagos) {
            _chartPagos.data.datasets[0].data = dataP;
            _chartPagos.update();
        } else {
            _chartPagos = new Chart(ctxPagos, {
                type: 'doughnut',
                data: {
                    labels: labelsP,
                    datasets: [{
                        data: dataP,
                        backgroundColor: ['rgba(59,130,246,0.8)', 'rgba(245,158,11,0.8)'],
                        borderColor: ['rgba(59,130,246,1)', 'rgba(245,158,11,1)'],
                        borderWidth: 2,
                        hoverOffset: 6
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    cutout: '65%',
                    plugins: {
                        legend: { position: 'bottom', labels: { color: '#6b7280', padding: 14 } }
                    }
                }
            });
        }
    }
}

function formatearTiempo(minutos) {
    if (minutos === null || minutos === undefined) return 'N/D';
    const m = Math.round(minutos);
    if (m < 60) return `${m} min`;
    const h = Math.floor(m / 60);
    const rem = m % 60;
    return rem > 0 ? `${h}h ${rem}m` : `${h}h`;
}

function formatearMonto(monto) {
    if (monto === null || monto === undefined) return 'N/D';
    return '$' + parseFloat(monto).toFixed(2);
}

function formatearFechaCorta(fechaStr) {
    const [y, m, d] = fechaStr.split('-');
    return `${d}/${m}/${y}`;
}

function formatearFechaLarga(fechaStr) {
    const fecha = new Date(fechaStr + 'T12:00:00');
    return fecha.toLocaleDateString('es-GT', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
}

function descargarBlob(blob, nombreArchivo) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = nombreArchivo;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

// ===== NAVEGACIÓN SIDEBAR =====
function navegarA(pagina, elemento) {
    document.querySelectorAll('.content').forEach(p => p.style.display = 'none');
    document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));

    const pageTitle = document.getElementById('pageTitle');

    if (pagina === 'inicio') {
        document.getElementById('pageInicio').style.display = '';
        pageTitle.innerHTML = '<i data-lucide="layout-dashboard"></i> Inicio';
        cargarDashboard();
        if (dashboardIntervalId) clearInterval(dashboardIntervalId);
        dashboardIntervalId = setInterval(cargarDashboard, DASHBOARD_INTERVAL_MS);
    } else {
        if (dashboardIntervalId) {
            clearInterval(dashboardIntervalId);
            dashboardIntervalId = null;
        }
        if (pagina === 'bitacora') {
            document.getElementById('pageBitacora').style.display = '';
            pageTitle.innerHTML = '<i data-lucide="clipboard-list"></i> Bitácora de Accesos';
        } else if (pagina === 'reportes-venta') {
            document.getElementById('pageReportesVenta').style.display = '';
            pageTitle.innerHTML = '<i data-lucide="bar-chart-3"></i> Reportes de Venta';
        } else if (pagina === 'cerrar-tickets') {
            document.getElementById('pageCerrarTickets').style.display = '';
            pageTitle.innerHTML = '<i data-lucide="ticket"></i> Cerrar Tickets';
            cargarTicketsAntiguos();
        } else if (pagina === 'tarifas') {
            document.getElementById('pageTarifas').style.display = '';
            pageTitle.innerHTML = '<i data-lucide="circle-dollar-sign"></i> Tarifas';
            cargarTarifas();
        }
    }

    if (elemento) elemento.classList.add('active');

    // Al inyectar íconos por código (.innerHTML), requerimos re-crearlos:
    if (window.lucide) {
        lucide.createIcons({ root: pageTitle });
    }
}

function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const isMobile = window.innerWidth <= 1024;

    if (isMobile) {
        sidebar.classList.toggle('open');
        let overlay = document.getElementById('sidebarOverlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'sidebarOverlay';
            overlay.className = 'sidebar-overlay';
            overlay.onclick = () => toggleSidebar();
            document.body.appendChild(overlay);
        }
        overlay.classList.toggle('active', sidebar.classList.contains('open'));
    } else {
        sidebar.classList.toggle('collapsed');
    }
}