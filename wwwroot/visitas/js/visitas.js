// =============================================
// CONTROL DE INGRESO - CENTRO PANAMERICANO DE OJOS
// Lógica del frontend
// =============================================

// ===== PROTECCIÓN DE SESIÓN + PERMISOS =====
function verificarSesion() {
    const datos = sessionStorage.getItem('operador');
    if (!datos) {
        window.location.href = '/visitas/login.html';
        return false;
    }
    operadorActual = JSON.parse(datos);

    // Aplicar rol al body para CSS de permisos
    document.body.classList.add('role-' + operadorActual.tipoUsuario);

    // Mostrar info en sidebar
    const sidebarName = document.getElementById('sidebarUserName');
    const sidebarRole = document.getElementById('sidebarUserRole');
    if (sidebarName) sidebarName.textContent = operadorActual.nombreCompleto;
    if (sidebarRole) sidebarRole.textContent = operadorActual.tipoUsuario;

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

    cargarCatalogos();
    cargarVisitasHoy();
    actualizarReloj();
    setInterval(actualizarReloj, 1000);
    setInterval(cargarVisitasHoy, 30000);

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


// =============================================
// REPORTES - EXCEL Y PDF
// =============================================

// Inicializar fechas del reporte
document.addEventListener('DOMContentLoaded', () => {
    // Poner fecha de hoy por defecto
    const hoy = new Date().toISOString().split('T')[0];
    const inputInicio = document.getElementById('reporteFechaInicio');
    const inputFin = document.getElementById('reporteFechaFin');
    if (inputInicio) inputInicio.value = hoy;
    if (inputFin) inputFin.value = hoy;

    // Llenar select de tipos en reportes
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

// Buscar datos para el reporte
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
            // Mostrar vista previa
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

    // Construir contenido XML para Excel
    let xml = '<?xml version="1.0" encoding="UTF-8"?>\n';
    xml += '<?mso-application progid="Excel.Sheet"?>\n';
    xml += '<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"\n';
    xml += ' xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">\n';

    // Estilos
    xml += '<Styles>\n';
    xml += '  <Style ss:ID="titulo"><Font ss:Bold="1" ss:Size="14"/><Alignment ss:Horizontal="Center"/></Style>\n';
    xml += '  <Style ss:ID="subtitulo"><Font ss:Bold="1" ss:Size="11"/><Alignment ss:Horizontal="Center"/></Style>\n';
    xml += '  <Style ss:ID="header"><Font ss:Bold="1" ss:Size="10" ss:Color="#FFFFFF"/><Interior ss:Color="#1F2937" ss:Pattern="Solid"/><Alignment ss:Horizontal="Center"/><Borders><Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1"/></Borders></Style>\n';
    xml += '  <Style ss:ID="celda"><Borders><Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E5E7EB"/></Borders></Style>\n';
    xml += '  <Style ss:ID="fecha"><NumberFormat ss:Format="dd/mm/yyyy"/><Borders><Border ss:Position="Bottom" ss:LineStyle="Continuous" ss:Weight="1" ss:Color="#E5E7EB"/></Borders></Style>\n';
    xml += '</Styles>\n';

    xml += '<Worksheet ss:Name="Bitácora de Visitas">\n';
    xml += '<Table>\n';

    // Anchos de columna
    xml += '<Column ss:Width="90"/><Column ss:Width="200"/><Column ss:Width="100"/><Column ss:Width="90"/><Column ss:Width="80"/><Column ss:Width="80"/><Column ss:Width="70"/><Column ss:Width="150"/><Column ss:Width="200"/>\n';

    // Título empresa
    xml += '<Row ss:Height="25">';
    xml += '<Cell ss:MergeAcross="8" ss:StyleID="titulo"><Data ss:Type="String">DOBLE GEMINIS, S.A. DE C.V.</Data></Cell>';
    xml += '</Row>\n';

    // Subtítulo
    xml += '<Row ss:Height="20">';
    xml += '<Cell ss:MergeAcross="8" ss:StyleID="subtitulo"><Data ss:Type="String">CONTROL DE PARQUEO PARA MÉDICOS, VISITANTES Y PROVEEDORES</Data></Cell>';
    xml += '</Row>\n';

    // Período
    const periodoTexto = fechaInicio === fechaFin
        ? `Fecha: ${formatearFechaLarga(fechaInicio)}`
        : `Período: ${formatearFechaLarga(fechaInicio)} al ${formatearFechaLarga(fechaFin)}`;

    xml += '<Row ss:Height="18">';
    xml += `<Cell ss:MergeAcross="8" ss:StyleID="subtitulo"><Data ss:Type="String">${periodoTexto}</Data></Cell>`;
    xml += '</Row>\n';

    // Fila vacía
    xml += '<Row></Row>\n';

    // Encabezados
    xml += '<Row>';
    const headers = ['FECHA DE VISITA', 'NOMBRE', 'TIPO', 'PLACA #', 'ENTRADA', 'SALIDA', 'TIEMPO', 'ÁREA DESTINO', 'OBSERVACIÓN'];
    headers.forEach(h => {
        xml += `<Cell ss:StyleID="header"><Data ss:Type="String">${h}</Data></Cell>`;
    });
    xml += '</Row>\n';

    // Datos
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

    // Descargar
    const blob = new Blob([xml], { type: 'application/vnd.ms-excel' });
    const nombreArchivo = `Bitacora_Visitas_${fechaInicio.replace(/-/g, '')}${fechaFin !== fechaInicio ? '_al_' + fechaFin.replace(/-/g, '') : ''}.xls`;
    descargarBlob(blob, nombreArchivo);

    mostrarToast(`📗 Excel descargado: ${datos.length} registros`, 'success');
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

    // Construir HTML para impresión/PDF
    const htmlContent = `
    <!DOCTYPE html>
    <html lang="es">
    <head>
        <meta charset="UTF-8">
        <title>Bitácora de Visitas</title>
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
            tbody tr:hover { background: #f3f4f6; }
            .footer-report {
                margin-top: 20px; padding-top: 10px; border-top: 1px solid #e5e7eb;
                font-size: 10px; color: #9ca3af; display: flex; justify-content: space-between;
            }
            .badge-tipo {
                display: inline-block; padding: 2px 6px; border-radius: 10px;
                font-size: 9px; font-weight: 600; background: #e8effc; color: #1a56db;
            }
            @media print {
                body { padding: 0; }
                .no-print { display: none; }
            }
        </style>
    </head>
    <body>
        <div class="header-report">
            <h1>DOBLE GEMINIS, S.A. DE C.V.</h1>
            <h2>CONTROL DE PARQUEO PARA MÉDICOS QUE NOS VISITAN PARA REALIZAR<br>PROCEDIMIENTOS EN SALA DE OPERACIONES Y UNIDAD DE DIAGNÓSTICO</h2>
            <p>${periodoTexto} &nbsp;|&nbsp; Total: ${datos.length} registros</p>
        </div>

        <table>
            <thead>
                <tr>
                    <th>FECHA DE VISITA</th>
                    <th>NOMBRE DE MÉDICO / VISITANTE</th>
                    <th>TIPO</th>
                    <th>PLACA #</th>
                    <th>ENTRADA</th>
                    <th>SALIDA</th>
                    <th>TIEMPO</th>
                    <th>ÁREA DESTINO</th>
                    <th>OBSERVACIÓN</th>
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
            <span>Centro Panamericano de Ojos — Sistema de Control de Parqueo</span>
            <span>Generado: ${new Date().toLocaleString('es-GT')}</span>
        </div>

        <script>
            window.onload = () => { window.print(); };
        <\/script>
    </body>
    </html>`;

    // Abrir ventana de impresión (el usuario puede guardar como PDF)
    const ventana = window.open('', '_blank');
    ventana.document.write(htmlContent);
    ventana.document.close();

    mostrarToast(`📕 PDF generado: ${datos.length} registros. Use "Guardar como PDF" en el diálogo de impresión.`, 'success');
}

// =============================================
// UTILIDADES DE REPORTE
// =============================================
function escapeXml(str) {
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&apos;');
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