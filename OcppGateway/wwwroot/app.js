let authToken = localStorage.getItem('jwt');
let currentUserRole = localStorage.getItem('role');

const originalFetch = window.fetch;
window.fetch = async function () {
    let [resource, config] = arguments;
    if (authToken && resource !== '/api/auth/login') {
        if (!config) config = {};
        if (!config.headers) config.headers = {};

        if (config.headers instanceof Headers) {
            if (!config.headers.has('Authorization')) {
                config.headers.append('Authorization', `Bearer ${authToken}`);
            }
        } else {
            config.headers['Authorization'] = `Bearer ${authToken}`;
        }
    }

    const response = await originalFetch(resource, config);
    if (response.status === 401) {
        logout();
    }
    return response;
};

document.addEventListener('DOMContentLoaded', () => {
    if (authToken) {
        document.getElementById('login-container').style.display = 'none';
        document.getElementById('app-layout').style.display = 'flex';
        if (currentUserRole === 'Super Admin') {
            document.getElementById('nav-assets').style.display = 'flex';
        }
    } else {
        document.getElementById('login-container').style.display = 'flex';
        document.getElementById('app-layout').style.display = 'none';
    }
});

async function login() {
    const user = document.getElementById('loginUsername').value;
    const pass = document.getElementById('loginPassword').value;
    const errorMsg = document.getElementById('loginErrorMsg');

    errorMsg.style.display = 'none';

    try {
        const res = await originalFetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username: user, password: pass })
        });

        if (res.ok) {
            const data = await res.json();
            authToken = data.token;
            currentUserRole = data.role;
            localStorage.setItem('jwt', authToken);
            localStorage.setItem('role', currentUserRole);

            document.getElementById('login-container').style.display = 'none';
            document.getElementById('app-layout').style.display = 'flex';

            if (currentUserRole === 'Super Admin') {
                document.getElementById('nav-assets').style.display = 'flex';
            } else {
                document.getElementById('nav-assets').style.display = 'none';
            }

            switchTab(currentTab || 'overview');
        } else {
            errorMsg.innerText = 'Credenciales inválidas.';
            errorMsg.style.display = 'block';
        }
    } catch (e) {
        errorMsg.innerText = 'Error al conectar con el servidor.';
        errorMsg.style.display = 'block';
    }
}

function logout() {
    authToken = null;
    currentUserRole = null;
    localStorage.removeItem('jwt');
    localStorage.removeItem('role');

    document.getElementById('login-container').style.display = 'flex';
    document.getElementById('app-layout').style.display = 'none';
}

const API_URL = '/api/chargers';
let currentHistoryData = [];
let currentOverviewData = null;
window.pendingCommands = window.pendingCommands || {};

let currentFilter = 'todos';

function setFilter(filterType) {
    currentFilter = filterType;
    document.querySelectorAll('.btn-filter').forEach(btn => btn.classList.remove('active'));
    document.querySelector(`.btn-filter[onclick="setFilter('${filterType}')"]`).classList.add('active');
    fetchChargers();
}

let logsWs = null;
let isLogsPaused = false;
let hideHeartbeats = false;

function toggleHeartbeats(checkbox) {
    hideHeartbeats = checkbox.checked;
}

async function fetchChargers() {
    const tableBody = document.querySelector('#chargersTable tbody');

    try {
        const response = await fetch(API_URL);
        if (!response.ok) throw new Error('Error al conectar con API');

        const chargers = await response.json();

        let counts = { todos: 0, offline: 0, charging: 0, errors: 0 };
        tableBody.innerHTML = '';

        if (chargers.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="9" style="text-align:center">No hay cargadores registrados.</td></tr>';
            return;
        }

        chargers.forEach(c => {
            counts.todos++;

            const row = document.createElement('tr');

            let statusClass = 'status-offline';
            let originalStatus = c.status || 'Desconocido';
            let statusText = originalStatus;

            switch (originalStatus) {
                case 'Available':
                case 'Online':
                    statusClass = 'status-online';
                    statusText = 'Disponible';
                    break;
                case 'Occupying':
                case 'Preparing':
                    statusClass = 'status-occupying';
                    statusText = 'Ocupado';
                    break;
                case 'Charging':
                    statusClass = 'status-charging';
                    statusText = 'Cargando';
                    break;
                case 'Faulted':
                    statusClass = 'status-faulted';
                    statusText = 'Error';
                    break;
                case 'Finishing':
                    statusClass = 'status-offline';
                    statusText = 'Finalizando';
                    break;
                case 'Offline':
                    statusClass = 'status-offline';
                    statusText = 'Desconectado';
                    break;
                case 'Booting':
                    statusClass = 'status-online';
                    statusText = 'Iniciando';
                    break;
                default:
                    statusClass = 'status-offline';
                    statusText = originalStatus;
            }

            const errorText = (c.latestErrorCode && c.latestErrorCode !== 'NoError')
                ? `<span style="color:var(--error-color)">⚠ ${c.latestErrorCode}</span>`
                : '<span style="color:#8b949e">Ninguno</span>';

            let uptimeText = "Desconocido";
            if (c.lastHeartbeat) {
                const lastHeartbeat = new Date(c.lastHeartbeat);
                const diffMs = new Date() - lastHeartbeat;
                const diffMins = Math.floor(diffMs / 60000);
                if (diffMins < 1) {
                    uptimeText = "Ahora";
                } else if (diffMins < 60) {
                    uptimeText = `Hace ${diffMins} min`;
                } else {
                    const diffHours = Math.floor(diffMins / 60);
                    if (diffHours < 24) {
                        uptimeText = `Hace ${diffHours} h`;
                    } else {
                        const diffDays = Math.floor(diffHours / 24);
                        uptimeText = `Hace ${diffDays} d`;
                    }
                }
            }

            const powerText = (c.currentPowerKw > 0)
                ? `⚡ ${c.currentPowerKw} kW`
                : `<span style="color:#8b949e">0 kW</span>`;

            if (statusClass === 'status-offline') counts.offline++;
            if (statusClass === 'status-charging') counts.charging++;
            if (c.latestErrorCode && c.latestErrorCode !== 'NoError') counts.errors++;

            if (currentFilter === 'offline' && statusClass !== 'status-offline') return;
            if (currentFilter === 'charging' && statusClass !== 'status-charging') return;
            if (currentFilter === 'errors' && (!c.latestErrorCode || c.latestErrorCode === 'NoError')) return;

            let networkBars = '<div style="display:inline-flex; gap:2px; margin-left:10px; opacity:0.3;" title="Sin señal">';
            networkBars += '<div style="width:3px; height:6px; background:grey"></div>';
            networkBars += '<div style="width:3px; height:9px; background:grey"></div>';
            networkBars += '<div style="width:3px; height:12px; background:grey"></div>';
            networkBars += '<div style="width:3px; height:15px; background:grey"></div>';
            networkBars += '</div>';

            if (c.rssi !== null && c.rssi !== undefined) {
                let bars = 0;
                if (c.rssi > -60) bars = 4;
                else if (c.rssi > -70) bars = 3;
                else if (c.rssi > -80) bars = 2;
                else if (c.rssi > -90) bars = 1;

                networkBars = `<div style="display:inline-flex; gap:2px; margin-left:10px; align-items:flex-end;" title="Red: ${c.networkType || 'Desconocida'} | RSSI: ${c.rssi} dBm">`;
                for (let i = 1; i <= 4; i++) {
                    let h = 3 + (i * 3);
                    let color = (i <= bars) ? (bars < 2 ? '#da3633' : (bars < 3 ? '#d29922' : '#238636')) : 'rgba(255,255,255,0.1)';
                    networkBars += `<div style="width:3px; height:${h}px; background:${color}; border-radius:1px;"></div>`;
                }
                networkBars += '</div>';
            } else if (statusClass === 'status-online' || statusClass === 'status-charging' || statusClass === 'status-occupying') {
                networkBars = `<div style="display:inline-flex; gap:2px; margin-left:10px; align-items:flex-end;" title="Conectado">`;
                for (let i = 1; i <= 4; i++) {
                    let h = 3 + (i * 3);
                    networkBars += `<div style="width:3px; height:${h}px; background:#238636; border-radius:1px;"></div>`;
                }
                networkBars += '</div>';
            }

            let actionButtonHtml = '';
            if (originalStatus === 'Available' || originalStatus === 'Online' || originalStatus === 'Preparing' || originalStatus === 'Finishing' || originalStatus === 'SuspendedEV' || originalStatus === 'SuspendedEVSE') {
                actionButtonHtml = `<button class="btn-action" onclick="sendCommand('${c.chargePointId}', 'start')" title="Iniciar sesión remotamente">🟢 Iniciar</button>`;
            } else if (originalStatus === 'Charging') {
                actionButtonHtml = `<button class="btn-action" onclick="sendCommand('${c.chargePointId}', 'stop')" title="Detener sesión remotamente">⏹ Stop</button>`;
            } else {
                actionButtonHtml = `<button class="btn-action" disabled style="opacity:0.5; cursor:not-allowed;" title="No disponible">⏹ Stop</button>`;
            }

            const displayId = c.name || c.physicalIdentifier || c.chargePointId;

            row.innerHTML = `
                <td style="font-weight:bold; color:var(--accent-color); cursor:pointer; z-index: 100;" onclick="openLogs('${c.chargePointId}')" title="Ver logs en tiempo real">
                    ${displayId} <span style="font-size: 0.8em;">🔍</span>
                </td>
                <td style="display:flex; align-items:center; border-bottom:none; padding-bottom:0;">
                    <span class="status-badge ${statusClass}">${statusText}</span>
                    ${networkBars}
                </td>
                <td>${errorText}</td>
                <td>${powerText}</td>
                <td style="font-size:0.9em">${c.model || '-'}<br><span style="color:#8b949e">FW: ${c.firmwareVersion || '-'}</span></td>
                <td>${c.connectorType || '-'}</td>
                <td style="font-size:0.9em; color:#8b949e">${c.latestInfo || '-'}</td>
                <td style="font-size:0.9em; color:#8b949e">${uptimeText}</td>
                <td style="white-space: nowrap;">
                    ${actionButtonHtml}
                    <button class="btn-action" onclick="sendCommand('${c.chargePointId}', 'reset')" title="Reiniciar cargador">🔄 Reset</button>
                    <button class="btn-action" onclick="viewHistory('${c.chargePointId}')" title="Ver historial de sesiones">📜 Historial</button>
                    <div class="dropdown">
                        <button class="dropdown-btn" title="Más opciones">
                            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <circle cx="12" cy="12" r="2"></circle>
                                <circle cx="19" cy="12" r="2"></circle>
                                <circle cx="5" cy="12" r="2"></circle>
                            </svg>
                        </button>
                        <div class="dropdown-content">
                            <button class="btn-action dropdown-item" onclick="sendCommand('${c.chargePointId}', 'unlock')" title="Liberar cable del cliente">🔓 Unlock</button>
                        </div>
                    </div>
                </td>
            `;

            tableBody.appendChild(row);
        });

        document.getElementById('count-todos').innerText = counts.todos;
        document.getElementById('count-offline').innerText = counts.offline;
        document.getElementById('count-charging').innerText = counts.charging;
        document.getElementById('count-errors').innerText = counts.errors;

        if (mapInstance && currentTab === 'map') {
            updateMapMarkers(chargers);
        }

    } catch (error) {
        console.error(error);
        tableBody.innerHTML = '<tr><td colspan="9" style="color:var(--error-color)">Error al cargar datos. Asegúrate de que el servidor esté corriendo.</td></tr>';
    }
}

function updateMapMarkers(chargers) {
    mapMarkers.forEach(m => mapInstance.removeLayer(m));
    mapMarkers = [];

    const mockCoords = [
        [4.6534, -74.0836],
        [4.6811, -74.0416],
        [4.6284, -74.0651],
        [4.6050, -74.0722],
        [4.6974, -74.0298] 
    ];

    chargers.forEach((c, index) => {
        const lat = c.latitude || mockCoords[index % mockCoords.length][0];
        const lng = c.longitude || mockCoords[index % mockCoords.length][1];

        let color = '#8b949e';
        if (c.status === 'Available' || c.status === 'Online') color = '#3fb950';
        else if (c.status === 'Charging') color = '#2f81f7';
        else if (c.status === 'Faulted') color = '#f85149';

        const markerHtmlStyles = `
          background-color: ${color};
          width: 20px;
          height: 20px;
          display: block;
          left: -10px;
          top: -10px;
          position: relative;
          border-radius: 50% 50% 50% 0;
          transform: rotate(-45deg);
          border: 2px solid #FFFFFF;
          box-shadow: 0 0 10px ${color};
        `;

        const icon = L.divIcon({
            className: "custom-pin",
            iconAnchor: [0, 24],
            labelAnchor: [-6, 0],
            popupAnchor: [0, -36],
            html: `<span style="${markerHtmlStyles}" />`
        });

        const marker = L.marker([lat, lng], { icon: icon }).addTo(mapInstance);

        const popupContent = `
            <div style="color: #333; font-family: 'Outfit', sans-serif;">
                <h4 style="margin:0 0 5px 0">${c.physicalIdentifier || c.chargePointId}</h4>
                <p style="margin:3px 0; font-size:12px">Estado: <strong style="color:${color}">${c.status}</strong></p>
                <p style="margin:3px 0; font-size:12px">Potencia: <strong>${c.currentPowerKw || 0} kW</strong></p>
                ${c.latestErrorCode && c.latestErrorCode !== 'NoError' ? `<p style="margin:3px 0; font-size:12px; color:#f85149">Error: ${c.latestErrorCode}</p>` : ''}
                <div style="margin-top:10px; text-align:center;">
                    <a href="#" onclick="openLogs('${c.chargePointId}'); return false;" style="color:#2f81f7; text-decoration:none; font-size:12px;">Ver Logs</a>
                </div>
            </div>
        `;

        marker.bindPopup(popupContent);
        mapMarkers.push(marker);
    });
}

async function viewHistory(id) {
    document.getElementById('historyModal').style.display = 'flex';
    const tbody = document.querySelector('#historyTable tbody');
    tbody.innerHTML = '<tr><td colspan="5">Cargando historial...</td></tr>';
    currentHistoryData = [];

    try {
        const res = await fetch(`/api/reports/history/${id}`);
        const data = await res.json();
        currentHistoryData = data;

        tbody.innerHTML = '';
        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5">Sin historial registrado.</td></tr>';
            return;
        }

        data.forEach(t => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${new Date(t.startTime).toLocaleString()}</td>
                <td>${t.stopTime ? new Date(t.stopTime).toLocaleString() : '-'}</td>
                <td style="font-weight:bold">${t.duration}</td>
                <td style="color:#7ee787">${t.totalKw} kWh</td>
                <td>${t.stopReason}</td>
            `;
            tbody.appendChild(row);
        });
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="5" style="color:red">Error cargando historial</td></tr>';
    }
}

function downloadHistory() {
    if (!currentHistoryData || currentHistoryData.length === 0) {
        alert("No hay datos para exportar.");
        return;
    }

    const excelData = currentHistoryData.map(row => ({
        "ID Transacción": row.transactionId,
        "Cargador ID": row.chargePointId,
        "Usuario (IdTag)": row.idTag,
        "Inicio": new Date(row.startTime).toLocaleString(),
        "Fin": row.stopTime ? new Date(row.stopTime).toLocaleString() : '',
        "Duración": row.duration,
        "Energía (kWh)": row.totalKw,
        "Razón Fin": row.stopReason,
        "Estado": row.status
    }));

    const worksheet = XLSX.utils.json_to_sheet(excelData);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "Historial");

    const fileName = `historial_${new Date().toISOString().slice(0, 10)}.xlsx`;
    XLSX.writeFile(workbook, fileName);
}

function exportChartDataToExcel() {
    if (!currentOverviewData || !currentOverviewData.monthlyStats || currentOverviewData.monthlyStats.length === 0) {
        alert("No hay datos del gráfico para exportar.");
        return;
    }

    const excelData = currentOverviewData.monthlyStats.map(m => ({
        "Mes": m.month,
        "Tarifas cobradas ($)": m.fees,
        "Energía usada (kWh)": m.energy,
        "Recuento de sesiones": m.sessions
    }));

    const worksheet = XLSX.utils.json_to_sheet(excelData);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "Métricas Mensuales");

    XLSX.writeFile(workbook, `metricas_grafico_${new Date().toISOString().slice(0, 10)}.xlsx`);
}

function exportRecentSessionsToExcel() {
    if (!currentOverviewData || !currentOverviewData.recentSessions || currentOverviewData.recentSessions.length === 0) {
        alert("No hay sesiones recientes para exportar.");
        return;
    }

    const excelData = currentOverviewData.recentSessions.map(s => ({
        "Tipo Autenticación": s.authType,
        "Ubicación": s.location,
        "Cargador": s.chargerId,
        "Hora de inicio": new Date(s.startTime).toLocaleString(),
        "Estado": s.status,
        "Energía usada (kWh)": s.energyKwh,
        "Costo ($)": s.cost
    }));

    const worksheet = XLSX.utils.json_to_sheet(excelData);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "Sesiones Recientes");

    XLSX.writeFile(workbook, `sesiones_recientes_${new Date().toISOString().slice(0, 10)}.xlsx`);
}

function closeHistory() {
    document.getElementById('historyModal').style.display = 'none';
}

async function sendCommand(id, cmd) {
    let payload = {};
    if (cmd === 'start') {
        const userId = prompt(`Iniciando sesión en ${id}.\nIngresa el identificador de usuario(IdTag) o deja en blanco para predeterminado: `);
        if (userId === null) return;
        payload.IdTag = userId.trim();
    } else {
        if (!confirm(`¿Seguro que deseas enviar ${cmd.toUpperCase()} al cargador ${id}?`)) return;
    }

    try {
        const res = await fetch(`/api/command/${id}/${cmd}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!res.ok) {
            let errText = 'Error en servidor';
            try {
                const err = await res.json();
                errText = err.detail || err.title || err.Message || errText;
            } catch (ignore) { }
            throw new Error(errText);
        }

        const data = await res.json();

        if (data.msgId) {
            window.pendingCommands[data.msgId] = cmd.toUpperCase();
        }

        alert(`Respuesta: ${data.status}`);
    } catch (e) {
        alert(e.message || 'Error enviando comando');
    }
}

let currentTab = 'overview';
let statusChartInstance = null;
let monthlyChartInstance = null;
let mapInstance = null;
let mapMarkers = [];

function switchTab(tabId) {
    currentTab = tabId;

    document.querySelectorAll('.sidebar-link').forEach(link => link.classList.remove('active'));
    document.getElementById('nav-' + tabId).classList.add('active');

    document.querySelectorAll('.view-section').forEach(sec => sec.style.display = 'none');
    document.getElementById('sec-' + tabId).style.display = 'block';

    const title = document.getElementById('page-title');
    const subtitle = document.getElementById('page-subtitle');

    if (tabId === 'overview') {
        title.innerText = 'Visión General';
        subtitle.innerText = 'Métricas resumidas y desempeño financiero del ecosistema.';
        fetchOverviewData();
    } else if (tabId === 'chargers') {
        title.innerText = 'Métricas en Tiempo Real';
        subtitle.innerText = 'Monitorea y gestiona el estado de toda tu red de cargadores de vehículos eléctricos.';
        fetchChargers();
    } else if (tabId === 'map') {
        title.innerText = 'Mapa Geográfico';
        subtitle.innerText = 'Ubicación en tiempo real de tus puntos de carga en la ciudad.';
        initOrUpdateMap();
    } else if (tabId === 'assets') {
        title.innerText = 'Gestión de Activos';
        subtitle.innerText = 'Control centralizado del inventario de hardware y parámetros de cargadores.';
        fetchAssets();
    } else if (tabId === 'acceso') {
        title.innerText = 'Acceso y Roles';
        subtitle.innerText = 'Gestión integral de la identidad de administradores y privilegios del sistema.';
        fetchAdmins();
    } else if (tabId === 'clients') {
        title.innerText = 'Clientes y Tarjetas (RFID)';
        subtitle.innerText = 'Gestión integral de conductores, autorización de carga y asignación de tarifas.';
        fetchClients();
    } else if (tabId === 'tariffs') {
        title.innerText = 'Estructuras de Precios';
        subtitle.innerText = 'Define las reglas de cobro por energía, tiempo y conexión para tu red.';
        fetchTariffs();
    }
}

function initOrUpdateMap() {
    if (!mapInstance) {
        mapInstance = L.map('chargers-map').setView([4.6097, -74.0817], 12);

        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 20
        }).addTo(mapInstance);

        fetchChargers();
    } else {
        setTimeout(() => mapInstance.invalidateSize(), 100);
        fetchChargers();
    }
}

async function fetchOverviewData() {
    try {
        const response = await fetch('/api/reports/overview');
        if (!response.ok) throw new Error('Error al cargar overview');
        const data = await response.json();

        currentOverviewData = data;

        document.getElementById('kpi-fees').innerText = '$' + (data.feesCollected || 0).toFixed(2);
        document.getElementById('kpi-energy').innerText = (data.energyUsedKwh || 0).toFixed(1) + ' kWh';
        document.getElementById('kpi-sessions').innerText = data.totalSessions || 0;

        const tbody = document.getElementById('recentSessionsBody');
        tbody.innerHTML = '';
        if (!data.recentSessions || data.recentSessions.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" style="text-align:center; color:#8b949e;">No hay sesiones recientes.</td></tr>';
        } else {
            data.recentSessions.forEach(s => {
                const row = document.createElement('tr');
                row.innerHTML = `
                    <td><span class="badge" style="background:var(--accent-color)">${s.authType}</span></td>
                    <td>${s.location}</td>
                    <td style="color:var(--accent-color); font-weight:600;">${s.chargerId}</td>
                    <td>${new Date(s.startTime).toLocaleString()}</td>
                    <td><span class="status-badge ${s.status === 'Active' ? 'status-charging' : 'status-online'}">${s.status}</span></td>
                    <td>${s.energyKwh} kWh</td>
                    <td style="font-weight:600;">$${s.cost.toFixed(2)}</td>
                `;
                tbody.appendChild(row);
            });
        }

        drawStatusChart(data.chargerStatus || {});

        drawMonthlyChart(data.monthlyStats || []);

    } catch (e) {
        console.error("Error fetching overview:", e);
    }
}

let currentChartTab = 'Tarifas cobradas';

function drawStatusChart(statusData) {
    const ctx = document.getElementById('statusChart').getContext('2d');

    if (statusChartInstance) {
        statusChartInstance.destroy();
    }

    const labels = Object.keys(statusData);
    const data = Object.values(statusData);

    if (labels.length === 0) {
        labels.push('Sin Datos');
        data.push(1);
    }

    const bgColors = labels.map(l => {
        if (l === 'Available' || l === 'Online') return '#3fb950';
        if (l === 'Charging') return '#2f81f7';
        if (l === 'Offline') return '#f85149';
        return '#8b949e';
    });

    statusChartInstance = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: bgColors,
                borderWidth: 0,
                cutout: '75%'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { color: '#e6edf3', padding: 20, font: { family: 'Outfit' } }
                }
            }
        }
    });
}

function drawMonthlyChart(monthlyData) {
    const ctx = document.getElementById('monthlyChart').getContext('2d');

    if (monthlyChartInstance) {
        monthlyChartInstance.destroy();
    }

    const labels = monthlyData.length > 0 ? monthlyData.map(m => m.month) : ['Mes Actual'];

    let chartData = [];
    let customLabel = '';

    switch (currentChartTab) {
        case 'Tarifas cobradas':
            chartData = monthlyData.length > 0 ? monthlyData.map(m => m.fees) : [0];
            customLabel = 'Tarifas cobradas ($)';
            break;
        case 'Energía usada':
            chartData = monthlyData.length > 0 ? monthlyData.map(m => m.energy) : [0];
            customLabel = 'Energía usada (kWh)';
            break;
        case 'Sesiones':
            chartData = monthlyData.length > 0 ? monthlyData.map(m => m.sessions) : [0];
            customLabel = 'Sesiones';
            break;
    }

    monthlyChartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: customLabel,
                data: chartData,
                backgroundColor: '#2f81f7',
                borderRadius: 4,
                barPercentage: 0.5
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    grid: { color: 'rgba(48, 54, 61, 0.5)' },
                    ticks: { color: '#8b949e', font: { family: 'Outfit' } }
                },
                x: {
                    grid: { display: false },
                    ticks: { color: '#8b949e', font: { family: 'Outfit' } }
                }
            },
            plugins: {
                legend: { display: false }
            }
        }
    });

    const tabs = document.querySelectorAll('.chart-tabs .tab-btn');
    tabs.forEach(btn => {
        btn.onclick = (e) => {
            tabs.forEach(t => t.classList.remove('active'));
            e.target.classList.add('active');

            currentChartTab = e.target.innerText;
            drawMonthlyChart(monthlyData);
        }
    });
}

document.addEventListener('DOMContentLoaded', () => {
    switchTab('overview');

    const sse = new EventSource('/api/dashboard/stream');
    sse.onmessage = (event) => {
        try {
            const data = JSON.parse(event.data);
            console.log('Real-time update received for Charger:', data.chargePointId);

            if (currentTab === 'chargers') {
                fetchChargers();
            }
        } catch (e) {
            console.error('Error parsing SSE', e);
        }
    };

    sse.onerror = (e) => {
        console.warn('Dashboard SSE stream disconnected or error. Will reconnect automatically.');
    };

    setInterval(() => {
        if (currentTab === 'chargers') {
            fetchChargers();
        } else if (currentTab === 'overview') {
            fetchOverviewData();
        }
    }, 5000);
});
function toggleChat() {
    const chatWindow = document.getElementById('chatWindow');
    const isVisible = chatWindow.style.display !== 'none';
    chatWindow.style.display = isVisible ? 'none' : 'flex';
    if (!isVisible) {
        document.getElementById('chatInput').focus();
    }
}

function handleChatKey(event) {
    if (event.key === 'Enter') {
        sendChatMessage();
    }
}

async function sendChatMessage() {
    const input = document.getElementById('chatInput');
    const message = input.value.trim();
    if (!message) return;

    appendMessage('user', message);
    input.value = '';

    const typingId = 'bot-typing-' + Date.now();
    appendMessage('bot', 'Pensando...', typingId);

    try {
        const tableRows = document.querySelectorAll('#chargersTable tbody tr');
        let validChargerId = 'system';

        const availableIds = Array.from(tableRows)
            .map(row => row.cells[0]?.innerText?.trim())
            .filter(id => id && id !== 'Cargando datos...' && id !== 'No hay cargadores registrados.');

        if (availableIds.length > 0) {
            validChargerId = availableIds[0];

            for (const id of availableIds) {
                if (message.includes(id)) {
                    validChargerId = id;
                    break;
                }
            }
        }

        const res = await fetch(`/chat?pregunta=${encodeURIComponent(message)}&cargadorId=${validChargerId}`);
        const data = await res.json();

        document.getElementById(typingId)?.remove();
        appendMessage('bot', data.bot || "Lo siento, no pude procesar eso.");
    } catch (e) {
        document.getElementById(typingId)?.remove();
        appendMessage('bot', "Error de conexión con el asistente.");
    }
}

function appendMessage(sender, text, id = null) {
    const chatBody = document.getElementById('chatBody');
    const msgDiv = document.createElement('div');
    msgDiv.className = `chat-message ${sender}`;
    if (id) msgDiv.id = id;
    msgDiv.innerText = text;
    chatBody.appendChild(msgDiv);
    chatBody.scrollTop = chatBody.scrollHeight;
}

function openLogs(id) {
    document.getElementById('logsDrawer').classList.add('open');
    document.getElementById('logsChargerId').innerText = id;
    document.getElementById('logContainer').innerHTML = '<div style="color:var(--accent-color)">Conectando al stream de logs...</div>';
    isLogsPaused = false;
    document.getElementById('btnPauseLogs').classList.remove('paused');
    document.getElementById('btnPauseLogs').innerText = '⏸ Pausar';

    if (logsWs) {
        logsWs.close();
    }

    const wsProtocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    logsWs = new WebSocket(`${wsProtocol}//${window.location.host}/api/logs/${id}`);

    logsWs.onopen = () => {
        document.getElementById('logContainer').innerHTML += '<div style="color:#3fb950; margin-bottom:10px;">Conectado. Esperando mensajes OCPP...</div>';
    };

    logsWs.onmessage = (event) => {
        if (isLogsPaused) return;

        try {
            const data = JSON.parse(event.data);
            const container = document.getElementById('logContainer');

            const isScrolledUp = container.scrollHeight - container.clientHeight > container.scrollTop + 5;

            const timeSpan = `<span class="log-time">${new Date(data.timestamp).toLocaleTimeString()}</span>`;
            const dirClass = data.direction === 'IN' ? 'log-in' : 'log-out';
            const dirIcon = data.direction === 'IN' ? '📥 Recibido' : '📤 Enviado';

            let parsedPayload = JSON.parse(data.payload);
            let action = 'Unknown API';
            let customIcon = '';

            if (Array.isArray(parsedPayload) && parsedPayload.length >= 3) {
                const msgType = parsedPayload[0];
                const msgId = parsedPayload[1];

                if (msgType === 2) {
                    action = `CALL: ${parsedPayload[2]}`;

                    if (parsedPayload[2] === 'Heartbeat' && hideHeartbeats) {
                        return;
                    }

                    if (parsedPayload[2] === 'Heartbeat') customIcon = '💙 ';
                    if (parsedPayload[2] === 'StatusNotification') {
                        if (parsedPayload[3]?.status === 'Available') customIcon = '🟢 ';
                        if (parsedPayload[3]?.status === 'Faulted') customIcon = '🔴 ';
                    }
                }
                else if (msgType === 3) {
                    action = `CALLRESULT`;

                    if (window.pendingCommands && window.pendingCommands[msgId]) {
                        action += ` (${window.pendingCommands[msgId]})`;
                        customIcon = '📱 ';
                        delete window.pendingCommands[msgId];
                    }

                    if (hideHeartbeats && Object.keys(parsedPayload[2] || {}).length === 0 && customIcon === '') {
                        return;
                    }
                }
                else if (msgType === 4) {
                    action = `CALLERROR`;
                }
            }

            const payloadStr = JSON.stringify(parsedPayload, null, 2)
                .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
                .replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)/g, function (match) {
                    let cls = 'log-number';
                    if (/^"/.test(match)) {
                        if (/:$/.test(match)) { cls = 'log-key'; } else { cls = 'log-string'; }
                    } else if (/true|false/.test(match)) { cls = 'log-boolean'; } else if (/null/.test(match)) { cls = 'log-null'; }
                    return '<span class="' + cls + '">' + match + '</span>';
                });

            const entry = document.createElement('div');
            entry.className = `log-entry ${dirClass}`;
            entry.innerHTML = `
                <div>${timeSpan} <span class="log-method" style="margin-left:10px;">${dirIcon} | ${customIcon}${action}</span></div>
                <pre style="margin:5px 0 0 0; white-space:pre-wrap;">${payloadStr}</pre>
            `;

            container.appendChild(entry);

            if (!isScrolledUp) {
                container.scrollTop = container.scrollHeight;
            }

            if (container.children.length > 200) {
                container.removeChild(container.children[1]);
            }

        } catch (e) {
            console.error("Error parsing log", e);
        }
    };

    logsWs.onclose = () => {
        const container = document.getElementById('logContainer');
        if (container) container.innerHTML += '<div style="color:var(--error-color); margin-top:10px;">Desconectado del stream de logs.</div>';
    };
}

function openScanner() {
    document.getElementById('scannerModal').style.display = 'flex';
    document.getElementById('scanInput').value = '';
    document.getElementById('scanResult').innerHTML = '';
}

function closeScanner() {
    document.getElementById('scannerModal').style.display = 'none';
}

async function simulateScan() {
    const input = document.getElementById('scanInput').value.trim();
    const resultDiv = document.getElementById('scanResult');

    if (!input) {
        resultDiv.innerHTML = '<span style="color:var(--warning-color)">Por favor ingresa un código.</span>';
        return;
    }

    resultDiv.innerHTML = '<span style="color:var(--accent-color)">Buscando cargador...</span>';

    try {
        const res = await fetch(`/api/scan/${encodeURIComponent(input)}`);

        if (res.status === 404) {
            resultDiv.innerHTML = `<span style="color:var(--error-color)">❌ Código no reconocido. Verifique la etiqueta del cargador.</span>`;
            return;
        }

        if (!res.ok) throw new Error('Network response was not ok');

        const charger = await res.json();

        let actionHtml = '';
        const allowedStartStatuses = ['Available', 'Online', 'Preparing', 'Finishing', 'SuspendedEV', 'SuspendedEVSE'];

        if (allowedStartStatuses.includes(charger.status)) {
            actionHtml = `<button class="btn-refresh" style="background:#238636; width:100%; margin-top:15px; font-size:1.1rem; padding:12px;"
                onclick="sendCommandAndCloseScanner('${charger.chargePointId}', 'start')">⚡ Iniciar Carga</button>`;
        } else {
            actionHtml = `<div style="margin-top:15px; padding:10px; background:rgba(255,255,255,0.05); border-radius:8px; color:#8b949e">
                 El cargador está actualmente <b>${charger.status}</b> y no puede iniciar una sesión.
             </div>`;
        }

        resultDiv.innerHTML = `
            <div style="background:rgba(47, 129, 247, 0.1); border:1px solid rgba(47, 129, 247, 0.3); border-radius:8px; padding:15px; text-align:left;">
                <h3 style="margin:0 0 10px 0; color:var(--text-color);">Cargador Encontrado</h3>
                <p style="margin:5px 0; color:#8b949e;">ID Lógico: <span style="color:var(--accent-color); font-weight:bold;">${charger.chargePointId}</span></p>
                <p style="margin:5px 0; color:#8b949e;">Estado: <span style="color:${allowedStartStatuses.includes(charger.status) ? '#3fb950' : '#d29922'}; font-weight:bold;">${charger.status}</span></p>
                ${actionHtml}
            </div>
        `;

    } catch (e) {
        resultDiv.innerHTML = '<span style="color:var(--error-color)">Error consultando la base de datos.</span>';
    }
}

async function sendCommandAndCloseScanner(id, cmd) {
    let payload = {};
    if (cmd === 'start') {
        const userId = prompt(`Autorizando sesión en ${id}.\nIngresa el identificador de usuario (IdTag) o deja en blanco para predeterminado:`);
        if (userId === null) return;
        payload.IdTag = userId.trim();
    } else {
        if (!confirm(`¿Deseas iniciar la acción en el conector ${id}?`)) return;
    }

    try {
        const res = await fetch(`/api/command/${id}/${cmd}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json();

        if (data.msgId) {
            window.pendingCommands[data.msgId] = cmd.toUpperCase();
        }

        alert(`Sesión Autorizada: ${data.status}`);
        closeScanner();
        if (typeof fetchChargers === 'function') fetchChargers();
    } catch (e) {
        alert(e.message || 'Error conectando al gateway');
    }
}

function closeLogs() {
    document.getElementById('logsDrawer').classList.remove('open');
    if (logsWs) {
        logsWs.close();
        logsWs = null;
    }
}

function toggleHeartbeats(checkbox) {
    hideHeartbeats = checkbox.checked;
}

function togglePauseLogs() {
    isLogsPaused = !isLogsPaused;
    const btn = document.getElementById('btnPauseLogs');
    if (isLogsPaused) {
        btn.classList.add('paused');
        btn.innerText = '▶ Reanudar';
    } else {
        btn.classList.remove('paused');
        btn.innerText = '⏸ Pausar';

        const container = document.getElementById('logContainer');
        container.scrollTop = container.scrollHeight;
    }
}

function copyLogs() {
    const container = document.getElementById('logContainer');
    const textToCopy = container.innerText;
    navigator.clipboard.writeText(textToCopy).then(() => {
        alert("Logs copiados al portapapeles.");
    }).catch(err => {
        console.error('No se pudo copiar el texto: ', err);
    });
}

let usersListCache = [];
let tariffsObjCache = {};

async function fetchAdmins() {
    const tableBody = document.querySelector('#adminsTable tbody');

    try {
        const response = await fetch('/api/users');
        if (!response.ok) throw new Error('Error al conectar con API de usuarios');

        const users = await response.json();
        usersListCache = users;

        const admins = users.filter(u => u.role !== 'Driver');

        tableBody.innerHTML = '';

        if (admins.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="5" style="text-align:center">No hay personal registrado.</td></tr>';
            return;
        }

        admins.forEach(u => {
            const row = document.createElement('tr');

            let roleBadgeClass = '';
            if (u.role === 'Super Admin') roleBadgeClass = 'status-faulted';
            else if (u.role === 'CPO') roleBadgeClass = 'status-charging';
            else if (u.role === 'Soporte / Instalador') roleBadgeClass = 'status-online';
            else roleBadgeClass = 'status-occupying';

            row.innerHTML = `
                <td>${u.id}</td>
                <td style="font-weight:bold; color:var(--text-color)">${u.username}</td>
                <td>${u.email || '-'}</td>
                <td><span class="status-badge ${roleBadgeClass}">${u.role}</span></td>
                <td>
                    <button class="btn-action" onclick="editAdmin(${u.id})" title="Editar Detalles">✏ Editar</button>
                    <button class="btn-action" style="color:var(--error-color)" onclick="deleteUser(${u.id}, 'admin')" title="Eliminar Personal">🗑 Eliminar</button>
                </td>
            `;

            tableBody.appendChild(row);
        });

    } catch (error) {
        console.error(error);
        tableBody.innerHTML = '<tr><td colspan="5" style="color:var(--error-color)">Error al cargar personal.</td></tr>';
    }
}

async function fetchClients() {
    const tableBody = document.querySelector('#clientsTable tbody');

    try {
        try {
            const tRes = await fetch('/api/tariffs');
            if (tRes.ok) {
                const tariffs = await tRes.json();
                tariffsObjCache = {};
                tariffs.forEach(t => tariffsObjCache[t.id] = t.name);
            }
        } catch (e) { }

        const response = await fetch('/api/users');
        if (!response.ok) throw new Error('Error al conectar con API de usuarios');

        const users = await response.json();
        usersListCache = users;

        const clients = users.filter(u => u.role === 'Driver');

        tableBody.innerHTML = '';

        if (clients.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="7" style="text-align:center">No hay clientes registrados.</td></tr>';
            return;
        }

        clients.forEach(u => {
            const row = document.createElement('tr');

            const isAuthHtml = u.isAuthorized
                ? '<span style="color:var(--accent-color); font-weight:bold;">Sí</span>'
                : '<span style="color:var(--error-color)">No</span>';

            const tariffName = u.tariffId && tariffsObjCache[u.tariffId] ? `<span style="color:var(--accent-color)">${tariffsObjCache[u.tariffId]}</span>` : '<span style="color:#8b949e">Según Cargador</span>';

            row.innerHTML = `
                <td>${u.id}</td>
                <td style="font-weight:bold; color:var(--text-color)">${u.username}</td>
                <td>${u.email || '-'}</td>
                <td>${u.idTag || '<span style="color:#8b949e">-</span>'}</td>
                <td>${isAuthHtml}</td>
                <td>${tariffName}</td>
                <td>
                    <button class="btn-action" onclick="editClient(${u.id})" title="Editar Detalles">✏ Editar</button>
                    <button class="btn-action" style="color:var(--error-color)" onclick="deleteUser(${u.id}, 'client')" title="Eliminar Cliente">🗑 Eliminar</button>
                </td>
            `;

            tableBody.appendChild(row);
        });

    } catch (error) {
        console.error(error);
        tableBody.innerHTML = '<tr><td colspan="7" style="color:var(--error-color)">Error al cargar clientes.</td></tr>';
    }
}

function openAdminModal() {
    document.getElementById('adminModalTitle').innerText = 'Nuevo Personal';
    document.getElementById('adminIdInput').value = '';
    document.getElementById('adminUsernameInput').value = '';
    document.getElementById('adminUsernameInput').readOnly = false;
    document.getElementById('adminEmailInput').value = '';
    document.getElementById('adminRoleSelect').value = '';

    document.getElementById('adminModal').style.display = 'flex';
}

function closeAdminModal() {
    document.getElementById('adminModal').style.display = 'none';
}

async function openClientModal() {
    document.getElementById('clientModalTitle').innerText = 'Nuevo Cliente';
    document.getElementById('clientIdInput').value = '';
    document.getElementById('clientUsernameInput').value = '';
    document.getElementById('clientUsernameInput').readOnly = false;
    document.getElementById('clientEmailInput').value = '';
    document.getElementById('clientIdTagInput').value = '';
    document.getElementById('clientIsAuthorizedCheck').checked = false;

    await populateClientTariffDropdown(null);
    document.getElementById('clientModal').style.display = 'flex';
}

function closeClientModal() {
    document.getElementById('clientModal').style.display = 'none';
}

async function populateClientTariffDropdown(selectedId) {
    const select = document.getElementById('clientTariffSelect');
    if (!select) return;

    select.innerHTML = '<option value="">-- Sin tarifa (Usar tarifa del cargador) --</option>';

    try {
        if (Object.keys(tariffsObjCache).length === 0) {
            const res = await fetch('/api/tariffs');
            if (res.ok) {
                const tariffs = await res.json();
                tariffsObjCache = {};
                tariffs.forEach(t => tariffsObjCache[t.id] = t.name);
            }
        }

        for (const [id, name] of Object.entries(tariffsObjCache)) {
            const opt = document.createElement('option');
            opt.value = id;
            opt.text = name;
            if (selectedId && Number(selectedId) === Number(id)) {
                opt.selected = true;
            }
            select.appendChild(opt);
        }
    } catch (e) { console.error('Error fetching tariffs for Client dropdown', e); }
}

async function fetchTariffs() {
    const tbody = document.querySelector('#tariffsTable tbody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;">Cargando precios...</td></tr>';
    try {
        const response = await fetch('/api/tariffs');
        if (!response.ok) throw new Error('Error de conexión');
        const tariffs = await response.json();

        tbody.innerHTML = '';
        if (tariffs.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;">No hay tarifas configuradas.</td></tr>';
        } else {
            tariffs.forEach(t => {
                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td>${t.id}</td>
                    <td style="font-weight:bold; color:var(--accent-color)">${t.name}</td>
                    <td>$${t.pricePerKwh.toFixed(2)}</td>
                    <td>$${t.pricePerMinute.toFixed(2)}</td>
                    <td>$${t.flatFee.toFixed(2)}</td>
                    <td>
                        <button class="btn-action" onclick="editTariff(${t.id}, '${t.name.replace(/'/g, "\\'")}', ${t.pricePerKwh}, ${t.pricePerMinute}, ${t.flatFee})" style="background:rgba(47, 129, 247, 0.2)">✏️</button>
                        <button class="btn-action" onclick="deleteTariff(${t.id})" style="background:rgba(248, 81, 73, 0.2)">🗑️</button>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        }
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="6" style="color:var(--error-color); text-align:center;">Error al cargar tarifas.</td></tr>';
    }
}

function openTariffModal() {
    document.getElementById('tariffModalTitle').innerText = 'Nueva Tarifa';
    document.getElementById('tariffIdInput').value = '';
    document.getElementById('tariffName').value = '';
    document.getElementById('tariffKwh').value = '0.30';
    document.getElementById('tariffMin').value = '0.00';
    document.getElementById('tariffFlat').value = '0.00';
    document.getElementById('tariffModal').style.display = 'flex';
}

function closeTariffModal() {
    document.getElementById('tariffModal').style.display = 'none';
}

function editTariff(id, name, kwh, min, flat) {
    document.getElementById('tariffModalTitle').innerText = 'Editar Tarifa';
    document.getElementById('tariffIdInput').value = id;
    document.getElementById('tariffName').value = name;
    document.getElementById('tariffKwh').value = kwh;
    document.getElementById('tariffMin').value = min;
    document.getElementById('tariffFlat').value = flat;
    document.getElementById('tariffModal').style.display = 'flex';
}

async function saveTariff() {
    const id = document.getElementById('tariffIdInput').value;
    const isNew = !id;

    const payload = {
        name: document.getElementById('tariffName').value,
        pricePerKwh: parseFloat(document.getElementById('tariffKwh').value || 0),
        pricePerMinute: parseFloat(document.getElementById('tariffMin').value || 0),
        flatFee: parseFloat(document.getElementById('tariffFlat').value || 0)
    };

    if (!payload.name) {
        alert("El nombre de la tarifa es obligatorio.");
        return;
    }

    try {
        const method = isNew ? 'POST' : 'PUT';
        const url = isNew ? '/api/tariffs' : `/api/tariffs/${id}`;

        const res = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            closeTariffModal();
            fetchTariffs();
        } else {
            alert('Error guardando tarifa.');
        }
    } catch (e) {
        alert('Error de red al guardar tarifa.');
    }
}

async function deleteTariff(id) {
    if (!confirm('¿Seguro que deseas eliminar esta tarifa? Los cargadores asociados pasarán a cobro 0.')) return;
    try {
        const res = await fetch(`/api/tariffs/${id}`, { method: 'DELETE' });
        if (res.ok) fetchTariffs();
        else alert('Error borrando tarifa.');
    } catch (e) {
        alert('Error de red.');
    }
}

async function editAdmin(id) {
    document.getElementById('adminModalTitle').innerText = 'Editar Personal';
    const user = usersListCache.find(u => u.id === id);
    if (!user) return;
    document.getElementById('adminIdInput').value = user.id;
    document.getElementById('adminUsernameInput').value = user.username;
    document.getElementById('adminUsernameInput').readOnly = true;
    document.getElementById('adminEmailInput').value = user.email || '';
    document.getElementById('adminRoleSelect').value = user.role || '';
    document.getElementById('adminModal').style.display = 'flex';
}

async function saveAdmin() {
    const id = document.getElementById('adminIdInput').value;
    const username = document.getElementById('adminUsernameInput').value.trim();
    const email = document.getElementById('adminEmailInput').value.trim();
    const role = document.getElementById('adminRoleSelect').value;

    if (!username || !role) {
        alert("El Nombre de Usuario y Rol son obligatorios.");
        return;
    }

    let payload = { username, email, role };
    if (id) {
        payload.id = parseInt(id);
        const existingInfo = usersListCache.find(u => u.id === parseInt(id));
        if (existingInfo) payload.passwordHash = existingInfo.passwordHash;
    } else {
        payload.passwordHash = 'cambiame123';
    }

    const method = id ? 'PUT' : 'POST';
    const url = id ? `/api/users/${id}` : '/api/users';

    try {
        const res = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            closeAdminModal();
            fetchAdmins();
        } else alert('Error guardando el administrador');
    } catch (e) { alert('Error de conexión'); }
}

async function editClient(id) {
    document.getElementById('clientModalTitle').innerText = 'Editar Cliente';
    const user = usersListCache.find(u => u.id === id);
    if (!user) return;
    document.getElementById('clientIdInput').value = user.id;
    document.getElementById('clientUsernameInput').value = user.username;
    document.getElementById('clientUsernameInput').readOnly = true;
    document.getElementById('clientEmailInput').value = user.email || '';
    document.getElementById('clientIdTagInput').value = user.idTag || '';
    document.getElementById('clientIsAuthorizedCheck').checked = user.isAuthorized || false;
    await populateClientTariffDropdown(user.tariffId);
    document.getElementById('clientModal').style.display = 'flex';
}

async function saveClient() {
    const id = document.getElementById('clientIdInput').value;
    const username = document.getElementById('clientUsernameInput').value.trim();
    const email = document.getElementById('clientEmailInput').value.trim();
    const role = 'Driver';
    const idTag = document.getElementById('clientIdTagInput').value.trim();
    const isAuthorized = document.getElementById('clientIsAuthorizedCheck').checked;
    const tariffVal = document.getElementById('clientTariffSelect').value;
    const tariffId = tariffVal ? parseInt(tariffVal) : null;

    if (!username) {
        alert("El Nombre del Cliente es obligatorio.");
        return;
    }

    let payload = { username, email, role, idTag, isAuthorized, tariffId };
    if (id) {
        payload.id = parseInt(id);
        const existingInfo = usersListCache.find(u => u.id === parseInt(id));
        if (existingInfo) payload.passwordHash = existingInfo.passwordHash;
    } else {
        payload.passwordHash = 'cambiame123';
    }

    const method = id ? 'PUT' : 'POST';
    const url = id ? `/api/users/${id}` : '/api/users';

    try {
        const res = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (res.ok) {
            closeClientModal();
            fetchClients();
        } else alert('Error guardando el cliente');
    } catch (e) { alert('Error de conexión'); }
}

async function deleteUser(id, type) {
    if (!confirm('¿Seguro que deseas eliminar el registro de manera permanente?')) return;
    try {
        const res = await fetch(`/api/users/${id}`, { method: 'DELETE' });
        if (res.ok) {
            if (type === 'admin') fetchAdmins();
            else fetchClients();
        } else alert('No se pudo eliminar.');
    } catch (e) { alert('Error de red al intentar eliminar.'); }
}

async function fetchAssets() {
    const tableBody = document.querySelector('#assetsTable tbody');

    try {
        const response = await fetch('/api/assets');
        if (!response.ok) throw new Error('Error al conectar con API de activos');

        const assets = await response.json();
        tableBody.innerHTML = '';

        if (assets.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="6" style="text-align:center">No hay cargadores registrados en el inventario.</td></tr>';
            return;
        }

        assets.forEach(c => {
            const tr = document.createElement('tr');

            let tariffBadge = '<span style="color:#8b949e">Sin Tarifa (Gratis)</span>';
            if (c.tariff) {
                tariffBadge = `<span class="badge" style="background:rgba(47, 129, 247, 0.2); color:#2f81f7">${c.tariff.name}</span>`;
            }

            tr.innerHTML = `
                <td style="font-weight:bold">${c.chargePointId}</td>
                <td style="color:#8b949e">${c.name || '-'}</td>
                <td style="color:#8b949e">${c.physicalIdentifier || '-'}</td>
                <td>${c.location || '-'}</td>
                <td style="font-size:0.8em; color:#8b949e">Lat: ${c.latitude || '-'}, Lng: ${c.longitude || '-'}</td>
                <td>${c.model || '-'}</td>
                <td>${tariffBadge}</td>
                <td>
                    <button class="btn-action" onclick="editAsset('${c.chargePointId}')" style="background:rgba(47, 129, 247, 0.2)">✏️</button>
                    <button class="btn-action" onclick="deleteAsset('${c.chargePointId}')" style="background:rgba(248, 81, 73, 0.2)">🗑️</button>
                </td>
            `;
            tableBody.appendChild(tr);
        });
    } catch (error) {
        console.error(error);
        tableBody.innerHTML = '<tr><td colspan="6" style="color:var(--error-color)">Error al cargar inventario de activos.</td></tr>';
    }
}

let allTariffsCache = [];

async function populateTariffDropdown(selectedId) {
    const select = document.getElementById('assetTariff');
    select.innerHTML = '<option value="">-- Sin tarifa (Usar cobro gratuito) --</option>';

    try {
        if (allTariffsCache.length === 0) {
            const res = await fetch('/api/tariffs');
            if (res.ok) {
                allTariffsCache = await res.json();
            }
        }

        allTariffsCache.forEach(t => {
            const opt = document.createElement('option');
            opt.value = t.id;
            opt.text = `${t.name} (Kwh: $${t.pricePerKwh}, Min: $${t.pricePerMinute}, Fijo: $${t.flatFee})`;
            if (selectedId && selectedId === t.id) {
                opt.selected = true;
            }
            select.appendChild(opt);
        });
    } catch (e) { console.error('Error fetching tariffs for dropdown', e); }
}

function openAssetModal() {
    document.getElementById('assetModalTitle').innerText = 'Nuevo Cargador';
    document.getElementById('assetIdInput').value = '';
    document.getElementById('assetIdInput').readOnly = false;
    document.getElementById('assetNameInput').value = '';
    document.getElementById('assetPhysInput').value = '';
    document.getElementById('assetLocInput').value = '';
    document.getElementById('assetLatInput').value = '';
    document.getElementById('assetLngInput').value = '';
    document.getElementById('assetModelInput').value = '';
    document.getElementById('assetConnectorInput').value = 'Type 2';

    populateTariffDropdown(null);
    document.getElementById('assetModal').style.display = 'flex';
}

function closeAssetModal() {
    document.getElementById('assetModal').style.display = 'none';
}

async function editAsset(chargePointId) {
    try {
        const res = await fetch(`/api/assets/${chargePointId}`);
        if (!res.ok) throw new Error('Cargador no encontrado');
        const charger = await res.json();

        document.getElementById('assetModalTitle').innerText = 'Editar Cargador';
        const idInput = document.getElementById('assetIdInput');
        idInput.value = charger.chargePointId;
        idInput.readOnly = true;
        document.getElementById('assetNameInput').value = charger.name || '';
        document.getElementById('assetPhysInput').value = charger.physicalIdentifier || '';
        document.getElementById('assetLocInput').value = charger.location || '';
        document.getElementById('assetLatInput').value = charger.latitude || '';
        document.getElementById('assetLngInput').value = charger.longitude || '';
        document.getElementById('assetModelInput').value = charger.model || '';
        document.getElementById('assetConnectorInput').value = charger.connectorType || 'Type 2';

        populateTariffDropdown(charger.tariffId);

        document.getElementById('assetModal').style.display = 'flex';
    } catch (error) {
        console.error(error);
        alert("No se pudieron obtener los datos del cargador.");
    }
}

async function saveAsset() {
    const chargePointId = document.getElementById('assetIdInput').value.trim();
    const name = document.getElementById('assetNameInput').value.trim();
    const physicalIdentifier = document.getElementById('assetPhysInput').value.trim();
    const location = document.getElementById('assetLocInput').value.trim();
    const latStr = document.getElementById('assetLatInput').value;
    const lngStr = document.getElementById('assetLngInput').value;

    const payload = {
        chargePointId,
        name,
        physicalIdentifier,
        location,
        latitude: latStr && !isNaN(parseFloat(latStr)) ? parseFloat(latStr) : null,
        longitude: lngStr && !isNaN(parseFloat(lngStr)) ? parseFloat(lngStr) : null,
        model: document.getElementById('assetModelInput').value.trim(),
        connectorType: document.getElementById('assetConnectorInput').value,
        tariffId: document.getElementById('assetTariff').value ? parseInt(document.getElementById('assetTariff').value) : null
    };

    const isEdit = document.getElementById('assetIdInput').readOnly;
    const method = isEdit ? 'PUT' : 'POST';
    const url = isEdit ? `/api/assets/${chargePointId}` : '/api/assets';

    try {
        const res = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            closeAssetModal();
            fetchAssets();
        } else {
            const errorText = await res.text();
            alert(`Error guardando el cargador:\n${errorText}`);
        }
    } catch (e) {
        console.error(e);
        alert('Error de red al intentar guardar.');
    }
}

async function deleteAsset(chargePointId) {
    if (!confirm(`¿Seguro que deseas eliminar el cargador ${chargePointId} de manera permanente?`)) return;

    try {
        const res = await fetch(`/api/assets/${chargePointId}`, { method: 'DELETE' });
        if (res.ok) {
            fetchAssets();
        } else {
            alert('No se pudo eliminar el cargador.');
        }
    } catch (e) {
        console.error(e);
        alert('Error de red al intentar eliminar.');
    }
}
