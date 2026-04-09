// =============================================
// CONFIGURACIÓN DINÁMICA DEL SITIO
// Se carga una sola vez al iniciar cualquier página
// y aplica el nombre/datos de la empresa en toda la UI
// =============================================

let sitioConfig = null;

async function cargarConfigSitio() {
    try {
        const res = await fetch('/api/sitio');
        const data = await res.json();
        if (data.exitoso && data.data) {
            sitioConfig = data.data;
            aplicarConfigSitio(sitioConfig);
        }
    } catch (e) {
        console.warn('No se pudo cargar config del sitio:', e);
    }
}

function aplicarConfigSitio(cfg) {
    if (!cfg) return;

    // ── Título del navegador (tab) ──
    document.title = cfg.nombreComercial + ' — Control de Parqueo';

    // ── Color primario (CSS variable) ──
    if (cfg.colorPrimario) {
        document.documentElement.style.setProperty('--brand-500', cfg.colorPrimario);
        document.documentElement.style.setProperty('--brand-600', ajustarColor(cfg.colorPrimario, -20));
        document.documentElement.style.setProperty('--brand-400', ajustarColor(cfg.colorPrimario, +20));
    }

    // ── Sidebar: nombre de la empresa ──
    const sidebarTitle = document.getElementById('sidebarTitle');
    if (sidebarTitle) sidebarTitle.textContent = cfg.nombreComercial.toUpperCase();

    const sidebarSubtitle = document.getElementById('sidebarSubtitle');
    if (sidebarSubtitle) sidebarSubtitle.textContent = cfg.slogan || 'Sistema de Parqueo';

    // ── Login: nombre en el logo box ──
    const logoName = document.querySelector('.logo-name');
    if (logoName) logoName.textContent = cfg.nombreComercial;

    const logoSub = document.querySelector('.logo-sub');
    if (logoSub) logoSub.textContent = cfg.slogan || '';

    // ── Logo (si hay URL) ──
    if (cfg.logoUrl) {
        const logoBox = document.querySelector('.logo-box, .sidebar-logo');
        if (logoBox) {
            logoBox.innerHTML = `<img src="${cfg.logoUrl}" 
                style="width:100%;height:100%;object-fit:contain;border-radius:inherit;" 
                alt="${cfg.nombreComercial}">`;
        }
    }

    // ── Elementos con data-sitio-field ──
    document.querySelectorAll('[data-sitio-field]').forEach(el => {
        const field = el.dataset.sitioField;
        const val = cfg[field];
        if (val !== undefined && val !== null) el.textContent = val;
    });
}

// Oscurece o aclara un color hex en N puntos (simple, sin dependencias)
function ajustarColor(hex, delta) {
    hex = hex.replace('#', '');
    let r = parseInt(hex.substring(0, 2), 16);
    let g = parseInt(hex.substring(2, 4), 16);
    let b = parseInt(hex.substring(4, 6), 16);
    r = Math.min(255, Math.max(0, r + delta));
    g = Math.min(255, Math.max(0, g + delta));
    b = Math.min(255, Math.max(0, b + delta));
    return `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`;
}

// Helpers para que los reportes usen los datos del sitio
function getSitioNombreComercial() {
    return sitioConfig?.nombreComercial ?? 'Parqueo IOT';
}
function getSitioRazonSocial() {
    return sitioConfig?.razonSocial ?? 'Parqueo IOT, S.A. DE C.V.';
}
function getSitioFooter() {
    const cfg = sitioConfig;
    if (!cfg) return '';
    const partes = [cfg.nombreComercial];
    if (cfg.direccion) partes.push(cfg.direccion);
    if (cfg.telefono) partes.push('Tel: ' + cfg.telefono);
    if (cfg.email) partes.push(cfg.email);
    return partes.join(' | ');
}
function getSitioEncabezadoPDF() {
    const cfg = sitioConfig;
    if (!cfg) return { linea1: '', linea2: '', linea3: '' };
    return {
        linea1: cfg.razonSocial,
        linea2: cfg.giroActividad ?? '',
        linea3: [cfg.direccion, cfg.municipio, cfg.departamento].filter(Boolean).join(', ')
    };
}