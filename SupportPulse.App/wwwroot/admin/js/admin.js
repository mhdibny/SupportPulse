console.log("🚀 admin.js starting...");

// ========== TOAST ==========
window.showToast = function (message, type = 'info', title = '', duration = 4000) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `custom-toast ${type}`;

    let iconHtml = '';
    switch (type) {
        case 'success': iconHtml = '<i class="fas fa-check-circle"></i>'; title = title || 'موفقیت'; break;
        case 'error': iconHtml = '<i class="fas fa-times-circle"></i>'; title = title || 'خطا'; break;
        case 'warning': iconHtml = '<i class="fas fa-exclamation-triangle"></i>'; title = title || 'هشدار'; break;
        default: iconHtml = '<i class="fas fa-info-circle"></i>'; title = title || 'اطلاعات';
    }

    toast.innerHTML = `
        <div class="toast-icon">${iconHtml}</div>
        <div class="toast-content">
            <div class="toast-title">${title}</div>
            <div class="toast-message">${message}</div>
        </div>
        <button class="toast-close"><i class="fas fa-times"></i></button>
    `;

    container.appendChild(toast);
    toast.querySelector('.toast-close').addEventListener('click', () => {
        toast.classList.add('hide');
        setTimeout(() => toast.remove(), 300);
    });
    if (duration > 0) {
        setTimeout(() => {
            if (toast.parentNode) {
                toast.classList.add('hide');
                setTimeout(() => toast.remove(), 300);
            }
        }, duration);
    }
};

// ========== ADMIN NOTIFICATION SYSTEM (Right‑side Glass) ==========
let adminNotifications = [];
let adminUnseenCount = 0;
let isAdminNotifMuted = localStorage.getItem('adminNotifMuted') === 'true';
const MAX_ADMIN_NOTIFICATIONS = 100;

// ========== GLOBALS ==========
let globalChatGhost = localStorage.getItem('globalChatGhost') === 'true';
const chatTimers = new Map(); // holds auto‑minimize timers per chatId
let adminConnection = null;
let chatConnection = null;
let renewTimer = null;
let isRenewing = false;
window.allPermissions = null;
let currentPage = null;
let currentSearch = { roleName: '', permissionIds: [] };
let searchDebounceTimer = null;
let filterModalInstance = null;
let waitingForPermissions = false;
let currentUploadingCardId = null;
let userState = {
    paging: { PageNumber: 1, PageSize: 10 },
    search: null,
    items: [],
    totalCount: 0,
    sortField: null,
    sortAsc: true
};

window.allRoles = null;
window.currentUserInfo = null;
window.allSupportCategories = null;
window.allSupportCategoriesForAssign = null;
window.currentUserSupportCategoryInfo = null;
window.iconMappings = null;

window.pendingUserRolesUserId = null;
window.pendingUserSupportCategoryUserId = null;
window.pendingEditSupportCategoryId = null;

// ========== ADMIN CHAT STATE ==========
const CANNED_STORAGE_KEY = 'adminCannedResponses';
let cannedResponses = [];
let cannedPanelPos = JSON.parse(localStorage.getItem('cannedPanelPos')) || null;
let cannedSearchTerm = '';
let cannedDragState = null;
let presenceTypewriterTimerId = null;
let typingTimer = null;
let typingActive = false;
const TYPING_TIMEOUT = 1500; // 1.5 seconds
let notificationBag = [];
let bagElement = null;
let bagListElement = null;
let allAdminChats = [];
let currentChatFilter = 'yours';        // 'yours' | 'free'
let currentOpenChatId = null;
let currentOpenChatUniqId = null;
let currentChatData = null;
let currentChatMessages = [];
let currentAdminUserId = null;
let currentAdminUserName = null;        // used to detect admin messages
let currentChatIsMine = false;
let selectedFiles = [];
let dragOverlay = null;
let dragHandlers = null;                // store drag event handlers for cleanup

const pageRoutes = {
    dashboard: '/Admin',
    roles: '/Admin/Roles',
    'roles-add': '/Admin/Roles/Add',
    'roles-edit': (roleId) => `/Admin/Roles/Edit/${roleId}`,
    'roles-delete': (roleId) => `/Admin/Roles/Delete/${roleId}`,
    users: '/Admin/Users',
    'users-roles': (userId) => `/Admin/Users/Roles/${userId}`,
    'users-supportcategories': (userId) => `/Admin/Users/SupportCategory/${userId}`,
    categories: '/Admin/SupportCategories',
    'categories-add': '/Admin/SupportCategories/Add',
    'categories-edit': (id) => `/Admin/SupportCategories/Edit/${id}`,
    chat: '/Admin/Chats',
    notifications: '/Admin/Notifications',
    settings: '/Admin/Settings'
};

// ========== PATH PARSING ==========
function getPageFromPath(path) {
    path = path.replace(/\/+$/, '');
    if (path === '/Admin' || path === '/Admin/') return 'dashboard';

    const lowerPath = path.toLowerCase();
    if (lowerPath === '/admin/roles/add') return 'roles-add';
    if (lowerPath === '/admin/supportcategories/add') return 'categories-add';

    const editRoleMatch = path.match(/^\/Admin\/Roles\/Edit\/(\d+)$/i);
    if (editRoleMatch) return { page: 'roles-edit', roleId: parseInt(editRoleMatch[1]) };
    const deleteRoleMatch = path.match(/^\/Admin\/Roles\/Delete\/(\d+)$/i);
    if (deleteRoleMatch) return { page: 'roles-delete', roleId: parseInt(deleteRoleMatch[1]) };
    const editCatMatch = path.match(/^\/Admin\/SupportCategories\/Edit\/(\d+)$/i);
    if (editCatMatch) return { page: 'categories-edit', id: parseInt(editCatMatch[1]) };

    const userRolesMatch = path.match(/^\/Admin\/Users\/Roles\/(\d+)$/i);
    if (userRolesMatch) return { page: 'users-roles', userId: parseInt(userRolesMatch[1]) };
    const userSupportCategoryMatch = path.match(/^\/Admin\/Users\/SupportCategory\/(\d+)$/i);
    if (userSupportCategoryMatch) return { page: 'users-supportcategories', userId: parseInt(userSupportCategoryMatch[1]) };

    if (lowerPath === '/admin/roles') return 'roles';
    if (lowerPath === '/admin/users') return 'users';
    if (lowerPath === '/admin/supportcategories') return 'categories';
    if (lowerPath === '/admin/chats') return 'chat';
    if (lowerPath === '/admin/notifications') return 'notifications';
    if (lowerPath === '/admin/settings') return 'settings';

    return 'dashboard';
}

// ========== TOKEN MANAGEMENT ==========
function getAccessToken() {
    return window.accessToken || sessionStorage.getItem('accessToken');
}

function getTokenExpiry(token) {
    try { return (JSON.parse(atob(token.split('.')[1])).exp || 0) * 1000; } catch (e) { return null; }
}

function scheduleTokenRenewal(token) {
    if (renewTimer) { clearTimeout(renewTimer); renewTimer = null; }
    const expiry = getTokenExpiry(token);
    if (!expiry) return;
    const timeLeft = expiry - Date.now();
    if (timeLeft <= 15000) { renewAndUpdateToken(); return; }
    const renewIn = timeLeft - 15000;
    console.log(`🔁 Scheduling token renewal in ${Math.round(renewIn / 1000)}s.`);
    renewTimer = setTimeout(() => { renewTimer = null; renewAndUpdateToken(); }, renewIn);
}

async function renewSignalRToken() {
    if (isRenewing) return null;
    isRenewing = true;
    try {
        const res = await fetch('/api/token/auto-renew', { credentials: 'include' });
        if (res.ok) {
            const data = await res.json();
            window.accessToken = data.accessToken;
            sessionStorage.setItem('accessToken', data.accessToken);
            return data.accessToken;
        }
    } catch (err) { console.error('❌ Token renewal error:', err); }
    finally { isRenewing = false; }
    return null;
}

function toggleAdminNotificationMute() {
    isAdminNotificationMuted = !isAdminNotificationMuted;
    localStorage.setItem('adminNotifMuted', isAdminNotificationMuted);
    updateMuteIconState();
}

function showAdminEventNotification(notification) {
    showAdminNotification(notification);
}

function showAdminNotification(notif) {
    notif.isSeen = false;
    adminNotifications.unshift(notif);
    if (adminNotifications.length > MAX_ADMIN_NOTIFICATIONS) adminNotifications.pop();

    adminUnseenCount++;
    updateAdminNotifBadge();

    if (!isAdminNotifMuted) {
        showAdminToast(notif);
    }

    const panelBody = document.getElementById('adminNotifPanelBody');
    if (panelBody) {
        const itemHtml = `
            <div class="admin-notif-panel-item">
                <div class="item-icon" style="color:${notif.color || '#6366f1'}">
                    <i class="fas ${notif.icon || 'fa-bell'}"></i>
                </div>
                <div class="item-content">
                    <div class="item-message">${buildNotifMessage(notif)}</div>
                    <div class="item-time">${formatTime(notif.createdAt)}</div>
                </div>
                <button class="item-delete" onclick="deleteNotification(0)" title="حذف">
                    <i class="fas fa-trash-alt"></i>
                </button>
            </div>`;
        const emptyMsg = panelBody.querySelector('.text-secondary');
        if (emptyMsg) emptyMsg.remove();
        panelBody.insertAdjacentHTML('afterbegin', itemHtml);
        const allItems = panelBody.querySelectorAll('.admin-notif-panel-item');
        allItems.forEach((item, index) => {
            const btn = item.querySelector('.item-delete');
            if (btn) btn.setAttribute('onclick', `deleteNotification(${index})`);
        });
    }
}

function closeAdminNotifPanel() {
    const panel = document.getElementById('adminNotifPanel');
    const overlay = document.getElementById('adminNotifOverlay');
    if (panel) {
        panel.classList.remove('open');
        setTimeout(() => panel.remove(), 350);
    }
    if (overlay) {
        overlay.classList.remove('show');
        setTimeout(() => overlay.remove(), 300);
    }
}

function renderNotifPanelItems() {
    if (adminNotifications.length === 0) {
        return '<div class="text-center p-4 text-secondary">اعلانی وجود ندارد</div>';
    }
    return adminNotifications.map((n, i) => `
        <div class="admin-notif-panel-item">
            <div class="item-icon" style="color:${n.color || '#6366f1'}">
                <i class="fas ${n.icon || 'fa-bell'}"></i>
            </div>
            <div class="item-content">
                <div class="item-message">${buildNotifMessage(n)}</div>
                <div class="item-time">${formatTime(n.createdAt)}</div>
            </div>
            <button class="item-delete" onclick="deleteNotification(${i})" title="حذف">
                <i class="fas fa-trash-alt"></i>
            </button>
        </div>
    `).join('');
}

function deleteNotification(index) {
    adminNotifications.splice(index, 1);
    updateAdminNotifBadge();
    const body = document.getElementById('adminNotifPanelBody');
    if (body) body.innerHTML = renderNotifPanelItems();
}

function updateAdminNotifBadge() {
    const badge = document.getElementById('adminNotifBadge');
    if (!badge) return;
    badge.textContent = adminUnseenCount;
    badge.style.display = adminUnseenCount > 0 ? 'flex' : 'none';
}

function openAdminNotifPanel() {
    adminNotifications.forEach(n => n.isSeen = true);
    adminUnseenCount = 0;
    updateAdminNotifBadge();

    const existing = document.getElementById('adminNotifPanel');
    if (existing) existing.remove();

    const overlay = document.createElement('div');
    overlay.className = 'admin-notif-overlay';
    overlay.id = 'adminNotifOverlay';
    overlay.addEventListener('click', closeAdminNotifPanel);
    document.body.appendChild(overlay);
    setTimeout(() => overlay.classList.add('show'), 10);

    const panel = document.createElement('div');
    panel.className = 'admin-notif-panel';
    panel.id = 'adminNotifPanel';

    panel.innerHTML = `
        <div class="admin-notif-panel-header">
            <h5>اعلان‌های مدیریتی</h5>
            <div style="display:flex; gap:0.5rem;">
                <button id="muteNotifBtn" class="btn-icon btn-sm" title="${isAdminNotifMuted ? 'فعال کردن صدا' : 'بی‌صدا'}">
                    <i class="fas ${isAdminNotifMuted ? 'fa-volume-mute' : 'fa-volume-up'}"></i>
                </button>
                <button id="clearAllNotifBtn" class="btn-icon btn-sm" title="پاک کردن همه">
                    <i class="fas fa-trash-alt"></i>
                </button>
                <button id="closeNotifPanelBtn" class="btn-icon btn-sm">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        </div>
        <div class="admin-notif-panel-body" id="adminNotifPanelBody">
            ${renderNotifPanelItems()}
        </div>
    `;
    document.body.appendChild(panel);
    setTimeout(() => panel.classList.add('open'), 10);

    document.getElementById('muteNotifBtn').addEventListener('click', toggleAdminNotifMute);
    document.getElementById('clearAllNotifBtn').addEventListener('click', clearAllNotifications);
    document.getElementById('closeNotifPanelBtn').addEventListener('click', closeAdminNotifPanel);
}

function toggleAdminNotifMute() {
    isAdminNotifMuted = !isAdminNotifMuted;
    localStorage.setItem('adminNotifMuted', isAdminNotifMuted);
    const btn = document.getElementById('muteNotifBtn');
    if (btn) {
        btn.innerHTML = `<i class="fas ${isAdminNotifMuted ? 'fa-volume-mute' : 'fa-volume-up'}"></i>`;
        btn.title = isAdminNotifMuted ? 'فعال کردن صدا' : 'بی‌صدا';
    }
}

function clearAllNotifications() {
    adminNotifications = [];
    adminUnseenCount = 0;
    updateAdminNotifBadge();
    const body = document.getElementById('adminNotifPanelBody');
    if (body) body.innerHTML = renderNotifPanelItems();
}

function showAdminToast(notif) {
    let stack = document.getElementById('adminNotificationStack');
    if (!stack) {
        stack = document.createElement('div');
        stack.id = 'adminNotificationStack';
        document.body.appendChild(stack);
    }

    const card = document.createElement('div');
    card.className = 'admin-notif-card';
    card.style.setProperty('--notif-color', notif.color || '#6366f1');

    const messageHtml = buildNotifMessage(notif);

    card.innerHTML = `
        <div class="admin-notif-icon" style="color:${notif.color || '#6366f1'}">
            <i class="fas ${notif.icon || 'fa-bell'}"></i>
        </div>
        <div class="admin-notif-content">
            <div class="admin-notif-message">${messageHtml}</div>
            <div class="admin-notif-time">${formatTime(notif.createdAt)}</div>
        </div>
        <button class="admin-notif-close" onclick="this.parentElement.remove()">
            <i class="fas fa-times"></i>
        </button>
    `;

    stack.appendChild(card);

    setTimeout(() => {
        if (card.parentElement) {
            card.classList.add('exit');
            setTimeout(() => card.remove(), 400);
        }
    }, 6000);
}

/**
 * Builds HTML for a notification message, turning actor and target names into links.
 * @param {Object} notif - notification object
 * @returns {string} HTML
 */
function buildNotifMessage(notif) {
    let msg = escapeHtml(notif.message);

    if (notif.actor && notif.actor.userName) {
        const actorLink = `<a href="javascript:void(0)" onclick="navigateToUserSearch('${escapeHtml(notif.actor.userName)}')">${escapeHtml(notif.actor.fullName)}</a>`;
        msg = msg.replace(new RegExp(escapeRegex(notif.actor.fullName), 'g'), actorLink);
    }

    if (notif.target) {
        let targetLink = '';
        if (notif.target.type === 'User' && notif.target.uniqId) {
            targetLink = `<a href="javascript:void(0)" onclick="navigateToUserSearch('${escapeHtml(notif.target.uniqId)}')">${escapeHtml(notif.target.name)}</a>`;
        } else if (notif.target.type === 'Chat' && notif.target.uniqId) {
            targetLink = `<a href="javascript:void(0)" onclick="openChatByUniqId('${escapeHtml(notif.target.uniqId)}')">${escapeHtml(notif.target.name)}</a>`;
        } else if (notif.target.type === 'Role' && notif.target.id) {
            targetLink = `<a href="javascript:void(0)" onclick="navigateToEditRole(${notif.target.id})">${escapeHtml(notif.target.name)}</a>`;
        } else if (notif.target.type === 'SupportCategory' && notif.target.id) {
            targetLink = `<a href="javascript:void(0)" onclick="navigateToEditSupportCategory(${notif.target.id})">${escapeHtml(notif.target.name)}</a>`;
        }
        if (targetLink) {
            msg = msg.replace(new RegExp(escapeRegex(notif.target.name || ''), 'g'), targetLink);
        }
    }

    return msg;
}

function escapeRegex(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function navigateToUserSearch(userName) {
    loadPage('users', true);
    setTimeout(() => {
        const input = document.getElementById('userSearchUsername');
        if (input) {
            input.value = userName;
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }
    }, 500);
}

function openChatByUniqId(uniqId) {
    if (currentPage !== 'chat') {
        loadPage('chat', true);
        setTimeout(() => {
            const chat = allAdminChats.find(c => c.chatUniqId === uniqId);
            if (chat) {
                openAdminChat(chat.chatId, chat.isChatLocked);
            }
        }, 800);
    } else {
        const chat = allAdminChats.find(c => c.chatUniqId === uniqId);
        if (chat) {
            openAdminChat(chat.chatId, chat.isChatLocked);
        }
    }
}

function getIconForAdminEventType(type) {
    const map = {
        "ChatUnlocked": "fa-unlock",
        "ChatLocked": "fa-lock",
        "ChatEnded": "fa-check-circle",
        "UserBanned": "fa-ban",
        "UserUnbanned": "fa-check-circle",
        "UserBanExpiryChanged": "fa-clock",
        "RoleCreated": "fa-plus-circle",
        "RoleEdited": "fa-edit",
        "RoleDeleted": "fa-trash-alt",
        "SupportCategoryCreated": "fa-plus-circle",
        "SupportCategoryEdited": "fa-edit"
    };
    return map[type] || "fa-bell";
}

function updateAdminNotificationBadge() {
    const badge = document.getElementById('notificationBadge');
    if (!badge) return;
    const count = adminNotifications.length;
    badge.textContent = count;
    badge.style.display = count > 0 ? 'inline-block' : 'none';
}

function updateMuteIconState() {
    const bellIcon = document.getElementById('adminNotifBell')?.querySelector('i');
    if (bellIcon) {
        bellIcon.className = isAdminNotificationMuted ? 'fas fa-bell-slash' : 'fas fa-bell';
    }
}

function formatTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diff = Math.floor((now - date) / 1000);
    if (diff < 60) return "همین الان";
    if (diff < 3600) return `${Math.floor(diff / 60)} دقیقه پیش`;
    if (diff < 86400) return `${Math.floor(diff / 3600)} ساعت پیش`;
    return date.toLocaleDateString("fa-IR");
}

// ========== UI UPDATE HELPERS ==========
function updateUserRow(user) {
    const row = document.querySelector(`#usersTableBody tr[data-user-id="${user.id}"]`);
    if (!row) return;
    row.cells[4].innerHTML = user.isBanned
        ? '<span class="badge badge-danger">بن شده</span>'
        : '<span class="badge badge-success">فعال</span>';
}

function addRoleToTable(role) {
    const tbody = document.getElementById('rolesTableBody');
    if (!tbody) return;
    const newRow = document.createElement('tr');
    newRow.innerHTML = `
        <td>${role.id}</td>
        <td>${role.name}</td>
        <td>${role.permissionsCount}</td>
        <td>${role.userHaveThisRoleCount}</td>
        <td>
            <button class="btn btn-sm btn-outline-info" onclick="navigateToEditRole(${role.id})"><i class="fas fa-edit"></i></button>
            <button class="btn btn-sm btn-outline-danger" onclick="navigateToDeleteRole(${role.id})"><i class="fas fa-trash"></i></button>
        </td>
    `;
    tbody.insertBefore(newRow, tbody.firstChild);
}

function updateRoleRow(role) {
    const targetRow = [...document.querySelectorAll('#rolesTableBody tr')]
        .find(r => r.cells[0].textContent == role.id);
    if (!targetRow) return;
    targetRow.cells[1].textContent = role.name;
    targetRow.cells[2].textContent = role.permissionsCount;
    targetRow.cells[3].textContent = role.userHaveThisRoleCount;
}

function removeRoleFromTable(roleId) {
    const row = [...document.querySelectorAll('#rolesTableBody tr')]
        .find(r => r.cells[0].textContent == roleId);
    if (row) row.remove();
}

function addSupportCategoryToTable(cat) {
    const tbody = document.getElementById('supportCategoriesTableBody');
    if (!tbody) return;
    const newRow = document.createElement('tr');
    newRow.innerHTML = `
        <td>${cat.id}</td>
        <td>${cat.name}</td>
        <td>${cat.isActive ? '<span class="badge badge-success">فعال</span>' : '<span class="badge badge-danger">غیرفعال</span>'}</td>
        <td>${cat.userCount}</td>
        <td>
            <button class="btn btn-sm btn-outline-info" onclick="navigateToEditSupportCategory(${cat.id})"><i class="fas fa-edit"></i></button>
        </td>
    `;
    tbody.insertBefore(newRow, tbody.firstChild);
}

function updateSupportCategoryRow(cat) {
    const row = [...document.querySelectorAll('#supportCategoriesTableBody tr')]
        .find(r => r.cells[0].textContent == cat.id);
    if (!row) return;
    row.cells[1].textContent = cat.name;
    row.cells[2].innerHTML = cat.isActive
        ? '<span class="badge badge-success">فعال</span>'
        : '<span class="badge badge-danger">غیرفعال</span>';
    row.cells[3].textContent = cat.userCount;
}

function updateChatInList(chat) {
    if (!allAdminChats) return;
    const idx = allAdminChats.findIndex(c => c.chatId === chat.chatId);
    if (idx !== -1) {
        allAdminChats[idx] = { ...allAdminChats[idx], ...chat };
    } else {
        allAdminChats.push(chat);
    }
    applyChatFilter();
}

function removeChatFromListById(chatId) {
    allAdminChats = allAdminChats.filter(c => c.chatId !== chatId);
    if (currentOpenChatId === chatId) closeAdminChatUI();
    applyChatFilter();
}

async function renewAndUpdateToken() {
    if (isRenewing) return;
    const newToken = await renewSignalRToken();
    if (!newToken) return;

    if (adminConnection && adminConnection.state !== signalR.HubConnectionState.Disconnected) {
        await adminConnection.stop().catch(() => { });
    }
    if (chatConnection && chatConnection.state !== signalR.HubConnectionState.Disconnected) {
        await chatConnection.stop().catch(() => { });
    }

    await initAdminHub(newToken);
    if (currentPage === 'chat') {
        await connectChatHub(newToken);
    }
}

// ========== ADMIN HUB ==========
async function initAdminHub(existingToken = null) {
    if (adminConnection && (adminConnection.state === signalR.HubConnectionState.Connected ||
        adminConnection.state === signalR.HubConnectionState.Connecting ||
        adminConnection.state === signalR.HubConnectionState.Reconnecting)) return;

    let token = existingToken || getAccessToken();
    if (!token) { token = await renewSignalRToken(); if (!token) return; }

    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        currentAdminUserId = parseInt(payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || payload.sub);
    } catch (e) { }

    adminConnection = new signalR.HubConnectionBuilder()
        .withUrl("/Hubs/Admin", { accessTokenFactory: () => getAccessToken() })
        .withAutomaticReconnect({ nextRetryDelayInMilliseconds: ctx => Math.min(10000, ctx.previousRetryCount * 2000) })
        .build();

    adminConnection.onreconnecting(() => window.showToast?.("در حال اتصال...", "info"));
    adminConnection.onreconnected(() => {
        window.showToast?.("اتصال برقرار شد.", "success");
        if (currentPage === 'roles') { adminConnection.invoke("GetRoles"); }
        else if (currentPage === 'roles-edit') { const roleId = parseInt(document.getElementById('editRoleId')?.value); if (roleId) safeLoadEditRoleData(roleId); }
        else if (currentPage === 'roles-delete') { const roleId = parseInt(document.getElementById('deleteRoleId')?.value); if (roleId) loadDeleteRoleData(roleId); }
        else if (currentPage === 'roles-add') { loadAddRoleData(); }
        else if (currentPage === 'users') { fetchUserPage(); }
        else if (currentPage === 'users-roles') { const userId = window.currentUserInfo?.userId || window.pendingUserRolesUserId; if (userId) { adminConnection.invoke("GetRolesForAssignToUser"); adminConnection.invoke("GetUserRolesAsync", userId); } }
        else if (currentPage === 'users-supportcategories') { const userId = window.currentUserSupportCategoryInfo?.userId || window.pendingUserSupportCategoryUserId; if (userId) { adminConnection.invoke("GetSupportCategoriesForAssignToUser"); adminConnection.invoke("GetUserSupportCategories", userId); } }
        else if (currentPage === 'categories') { adminConnection.invoke("GetSupportCategories"); }
        else if (currentPage === 'categories-edit') { const catId = window.pendingEditSupportCategoryId; if (catId) adminConnection.invoke("GetSupportCategoryForEdit", catId); }

        updateAdminNotificationBadge();
    });

    adminConnection.onclose(async (error) => {
        if (renewTimer) { clearTimeout(renewTimer); renewTimer = null; }
        await new Promise(r => setTimeout(r, 100));
        if (!isRenewing) {
            const newToken = await renewSignalRToken();
            if (newToken) await initAdminHub(newToken);
            else window.location.href = '/login';
        }
    });

    // ========== ADMIN HANDLERS ==========
    adminConnection.on("SystemMessage", alert => { if (alert) window.showToast?.(alert.message, alert.type, alert.title); });

    // ========== ADMIN EVENT NOTIFICATIONS ==========
    adminConnection.on("ReceiveNotification", (notification) => {
        showAdminNotification(notification);

        if (notification.type === "ChatAutoAssigned" && currentPage === 'chat') {
            chatConnection.invoke("GetAdminChatList").then(() => {
                if (currentOpenChatId) {
                    chatConnection.invoke("GetAdminChatData", currentOpenChatId).catch(() => { });
                }
            }).catch(() => { });
        }
    });

    // Roles
    adminConnection.on("ReceiveRoles", roles => renderRolesTable(roles));
    adminConnection.on("RoleSearchResult", roles => renderRolesTable(roles));
    adminConnection.on("ReceivePermissions", perms => {
        window.allPermissions = perms;
        if (waitingForPermissions) { waitingForPermissions = false; const checkboxContainer = document.getElementById('filterPermissionCheckboxes'); if (checkboxContainer) { populateFilterCheckboxes(); checkboxContainer.classList.remove('d-none'); } }
        if (currentPage === 'roles-add') { fillAddPermissions(); }
        else if (currentPage === 'roles-edit' && window.pendingEditRoleData) { fillEditForm(window.pendingEditRoleData); window.pendingEditRoleData = null; }
    });
    adminConnection.on("ReceiveRoleForEdit", role => { if (!role) return; if (window.allPermissions) fillEditForm(role); else window.pendingEditRoleData = role; });
    adminConnection.on("ReceiveRoleForDelete", role => { if (!role) return; populateDeleteConfirmation(role); });
    adminConnection.on("SuccessEditRole", alert => { window.showToast?.(alert.message, alert.type, alert.title); const roleId = parseInt(document.getElementById('editRoleId')?.value); if (roleId) safeLoadEditRoleData(roleId); });
    adminConnection.on("RoleSuccessfullyCreated", alert => { window.showToast?.(alert.message, alert.type, alert.title); loadPage('roles'); });
    adminConnection.on("RoleSuccessfullyDeleted", alert => { window.showToast?.(alert.message, alert.type, alert.title); loadPage('roles'); });

    // Users & Ban
    adminConnection.on("ReceiveUserList", pagedResult => {
        userState.items = pagedResult.items;
        userState.totalCount = pagedResult.totalCount;
        userState.paging = { PageNumber: pagedResult.pageNumber, PageSize: pagedResult.pageSize };
        applyUserSort();
        renderUsersTable(userState.items);
        renderPagination();
    });
    adminConnection.on("ReceiveUserBanHistories", data => { populateBanSlideover(data); });
    adminConnection.on("UserBanned", (payload) => {
        if (payload && payload.id !== undefined) {
            if (currentPage === 'users') updateUserRow(payload);
            if (banUserId === payload.id) {
                adminConnection.invoke("GetUserBanHistories", payload.id).catch(() => { });
                banUserIsBanned = true;
            }
            return;
        }
        window.showToast?.(payload.message, payload.type, payload.title);
        closeBanSlideover();
        fetchUserPage();
        if (currentPage === 'chat' && currentOpenChatId) {
            chatConnection.invoke("GetAdminChatData", currentOpenChatId).catch(() => { });
        }
    });

    adminConnection.on("UserUnBanned", (payload) => {
        if (payload && payload.id !== undefined) {
            if (currentPage === 'users') updateUserRow(payload);
            if (banUserId === payload.id) {
                adminConnection.invoke("GetUserBanHistories", payload.id).catch(() => { });
                banUserIsBanned = false;
            }
            return;
        }
        window.showToast?.(payload.message, payload.type, payload.title);
        closeBanSlideover();
        fetchUserPage();
        if (currentPage === 'chat' && currentOpenChatId) {
            chatConnection.invoke("GetAdminChatData", currentOpenChatId).catch(() => { });
        }
    });

    adminConnection.on("UserBanChanged", (payload) => {
        if (payload && payload.id !== undefined) {
            if (currentPage === 'users') updateUserRow(payload);
            if (banUserId === payload.id) {
                adminConnection.invoke("GetUserBanHistories", payload.id).catch(() => { });
            }
            return;
        }
        window.showToast?.(payload.message, payload.type, payload.title);
        closeBanSlideover();
        fetchUserPage();
        if (currentPage === 'chat' && currentOpenChatId) {
            chatConnection.invoke("GetAdminChatData", currentOpenChatId).catch(() => { });
        }
    });

    adminConnection.on("RoleCreated", (role) => {
        if (currentPage === 'roles') addRoleToTable(role);
    });
    adminConnection.on("RoleEdited", (role) => {
        if (currentPage === 'roles') updateRoleRow(role);
        if (currentPage === 'roles-edit') {
            const editingRoleId = parseInt(document.getElementById('editRoleId')?.value);
            if (editingRoleId && editingRoleId === role.id) {
                safeLoadEditRoleData(role.id);
            }
        }
    });
    adminConnection.on("RoleDeleted", (roleId) => {
        if (currentPage === 'roles') removeRoleFromTable(roleId);
        if (currentPage === 'roles-edit') {
            const editingRoleId = parseInt(document.getElementById('editRoleId')?.value);
            if (editingRoleId && editingRoleId === roleId) {
                window.showToast?.("این نقش توسط ادمین دیگری حذف شد.", "warning", "نقش حذف شد");
                loadPage('roles');
            }
        }
    });

    adminConnection.on("SupportCategoryCreated", (cat) => {
        if (currentPage === 'categories') addSupportCategoryToTable(cat);
    });
    adminConnection.on("SupportCategoryEdited", (cat) => {
        if (currentPage === 'categories') updateSupportCategoryRow(cat);
        if (currentPage === 'categories-edit') {
            const editingCatId = parseInt(document.getElementById('editSupportCategoryId')?.value);
            if (editingCatId && editingCatId === cat.id) {
                safeLoadEditSupportCategoryData(cat.id);
            }
        }
    });

    adminConnection.on("ChatLocked", (chat) => {
        if (currentPage === 'chat') {
            chatConnection.invoke("GetAdminChatList").then(() => {
                if (currentOpenChatId) {
                    chatConnection.invoke("GetAdminChatData", currentOpenChatId).catch(() => { });
                }
            }).catch(() => { });
        }
    });

    adminConnection.on("ChatUnlocked", (chat) => {
        if (currentPage === 'chat') {
            chatConnection.invoke("GetAdminChatList").then(() => {
                if (currentOpenChatId) {
                    chatConnection.invoke("GetAdminChatData", currentOpenChatId).catch(() => { });
                }
            }).catch(() => { });
        }
    });

    adminConnection.on("ChatEnded", (chatId) => {
        if (currentPage === 'chat') {
            if (currentOpenChatId === chatId) closeAdminChatUI();
            chatConnection.invoke("GetAdminChatList").catch(() => { });
        }
    });
    adminConnection.on("ChatEndedByUser", (chatId) => {
        if (currentPage === 'chat') {
            if (currentOpenChatId === chatId) closeAdminChatUI();
            chatConnection.invoke("GetAdminChatList").catch(() => { });
        }
    });
    adminConnection.on("ChatAutoAssigned", (chat) => {
        if (currentPage === 'chat') {
            chatConnection.invoke("GetAdminChatList").catch(() => { });
        }
    });

    // User Roles
    adminConnection.on("RoleListReceived", roles => {
        if (currentPage === 'users-roles') {
            window.allRoles = roles;
            if (window.currentUserInfo) { renderUserRoleCards(window.allRoles, window.currentUserInfo.userRolesIdList); setupUserRoleSearch(); }
        }
    });
    adminConnection.on("UserDataForChangeRoleReceived", userData => {
        if (currentPage === 'users-roles') {
            window.currentUserInfo = userData;
            document.getElementById('userRolesInfo').innerHTML = `<span class="fw-bold">${userData.fullName}</span> (${userData.userName}) <span class="badge bg-secondary ms-2">${userData.userRolesIdList.length} نقش</span>`;
            if (window.allRoles) { renderUserRoleCards(window.allRoles, userData.userRolesIdList); setupUserRoleSearch(); }
        }
    });

    adminConnection.on("UserRolesChanged", (payload) => {
        if (payload && payload.id !== undefined) {
            if (currentPage === 'users') updateUserRow(payload);
            if (currentPage === 'users-roles' && window.currentUserInfo?.userId === payload.id) {
                adminConnection.invoke("GetUserRolesAsync", payload.id).catch(() => { });
            }
            return;
        }
        window.showToast?.(payload.message, payload.type, payload.title);
        loadPage('users');
    });

    adminConnection.on("UserSupportCategoriesChanged", (payload) => {
        if (payload && payload.id !== undefined) {
            if (currentPage === 'users') updateUserRow(payload);
            if (currentPage === 'users-supportcategories' && window.currentUserSupportCategoryInfo?.userId === payload.id) {
                adminConnection.invoke("GetUserSupportCategories", payload.id).catch(() => { });
            }
            return;
        }
        window.showToast?.(payload.message, payload.type, payload.title);
        loadPage('users');
    });

    // Support Categories
    adminConnection.on("ReceiveSupportCategories", list => {
        if (currentPage === 'categories') {
            renderSupportCategoriesTable(list);
            setupSupportCategorySearch();
        }
    });
    adminConnection.on("SupportCategoryListReceived", list => {
        if (currentPage === 'users-supportcategories') {
            window.allSupportCategoriesForAssign = list;
            if (window.currentUserSupportCategoryInfo) {
                renderUserSupportCategoryCards(window.allSupportCategoriesForAssign, window.currentUserSupportCategoryInfo.supportCategoryIdList);
                setupUserSupportCategorySearch();
            }
        }
    });
    adminConnection.on("SupportCategorySuccessfullyCreated", alert => { window.showToast?.(alert.message, alert.type, alert.title); loadPage('categories'); });
    adminConnection.on("ReceiveSupportCategoryForEdit", dto => { if (currentPage === 'categories-edit') fillEditSupportCategoryForm(dto); });
    adminConnection.on("SuccessEditSupportCategory", alert => { window.showToast?.(alert.message, alert.type, alert.title); const id = parseInt(document.getElementById('editSupportCategoryId')?.value); if (id) adminConnection.invoke("GetSupportCategoryForEdit", id); });

    adminConnection.on("ReceiveIconMappings", icons => {
        window.iconMappings = icons;
        refreshOpenIconDropdowns();
    });

    adminConnection.on("UserDataForChangeSupportCategoryReceived", userData => {
        if (currentPage === 'users-supportcategories') {
            window.currentUserSupportCategoryInfo = userData;
            document.getElementById('userSupportCategoryInfo').innerHTML = `<span class="fw-bold">${userData.fullName}</span> (${userData.userName}) <span class="badge bg-secondary ms-2">${userData.supportCategoryIdList.length} واحد</span>`;
            if (window.allSupportCategoriesForAssign) {
                renderUserSupportCategoryCards(window.allSupportCategoriesForAssign, userData.supportCategoryIdList);
                setupUserSupportCategorySearch();
            }
        }
    });

    try {
        await adminConnection.start();
        console.log("✅ Admin Hub connected.");

        scheduleTokenRenewal(token);

        const pathInfo = getPageFromPath(window.location.pathname);
        if (typeof pathInfo === 'object') {
            loadPage(pathInfo.page, false, { roleId: pathInfo.roleId, userId: pathInfo.userId, id: pathInfo.id });
        } else {
            if (pathInfo === 'roles' && adminConnection.state === signalR.HubConnectionState.Connected) loadPage('roles', false);
            else if (pathInfo === 'users' && adminConnection.state === signalR.HubConnectionState.Connected) loadPage('users', false);
            else if (pathInfo === 'categories' && adminConnection.state === signalR.HubConnectionState.Connected) loadPage('categories', false);
            else if (pathInfo === 'categories-add' && adminConnection.state === signalR.HubConnectionState.Connected) loadPage('categories-add', false);
            else if (pathInfo === 'roles-add' && adminConnection.state === signalR.HubConnectionState.Connected) loadPage('roles-add', false);
            else if (pathInfo === 'chat' && adminConnection.state === signalR.HubConnectionState.Connected) loadPage('chat', false);
        }
    } catch (err) { console.error("❌ Admin Hub connection failed:", err); }
}// ========== CHAT HUB ==========
async function connectChatHub(existingToken = null) {
    if (chatConnection && chatConnection.state === signalR.HubConnectionState.Connected) {
        if (currentPage === 'chat') { chatConnection.invoke("GetAdminChatList").catch(() => { }); }
        return;
    }
    if (chatConnection && (chatConnection.state === signalR.HubConnectionState.Connecting ||
        chatConnection.state === signalR.HubConnectionState.Reconnecting)) return;

    let token = existingToken || getAccessToken();
    if (!token) { token = await renewSignalRToken(); if (!token) return; }

    chatConnection = new signalR.HubConnectionBuilder()
        .withUrl("/Chat", { accessTokenFactory: () => getAccessToken() })
        .withAutomaticReconnect()
        .build();

    // ========== CHAT HANDLERS ==========
    chatConnection.on("SystemMessage", alert => {
        if (alert) window.showToast?.(alert.message, alert.type, alert.title);
    });
    chatConnection.on("SetUserName", (userName) => {
        currentAdminUserName = userName.result || userName;
        if (currentChatData && currentChatMessages.length > 0) {
            renderAdminChatUI(currentChatData, currentChatMessages);
        }
    });
    chatConnection.on("AdminChatsReceived", (chats) => {
        if (currentPage !== 'chat') return;
        allAdminChats = chats;
        applyChatFilter();
    });
    chatConnection.on("PresenceUpdate", (chatUniqId, isOnline) => {
        if (currentPage !== 'chat') return;
        if (String(chatUniqId) === String(currentOpenChatUniqId)) {
            updateUserPresence(isOnline);
        }
    });
    chatConnection.on("ReceiveChatData", (data) => {
        if (currentPage !== 'chat') return;
        currentChatData = data;
        currentOpenChatId = data.chatId;
        currentOpenChatUniqId = String(data.chatUniqId);
        const chatInList = allAdminChats.find(c => c.chatId === data.chatId);
        currentChatIsMine = chatInList ? chatInList.isChatLocked : data.isChatLocked;
    });

    chatConnection.on("ReceiveChatMessages", (messages) => {
        if (currentPage !== 'chat' || !currentChatData) return;
        currentChatMessages = messages;
        renderAdminChatUI(currentChatData, currentChatMessages);
        markCurrentChatAsSeen();
    });

    chatConnection.on("ReceiveMessage", (message) => {
        if (currentPage !== 'chat') return;

        const chat = allAdminChats.find(c => c.chatUniqId === message.uniqChatId);
        if (chat) {
            chat.lastMessageText = message.messageData || (message.messageFiles && message.messageFiles.length > 0 ? '📎 فایل' : '');
            chat.lastMessageDateTime = message.sendTime;
            updateChatCardInList(chat);
        }

        const isAdminSender = !message.senderUserName || message.senderUserName.trim() === '' ||
            (message.adminFullName && message.adminFullName.trim() !== '');

        const isMyMessage = isAdminSender && currentAdminUserName && message.senderUserName === currentAdminUserName;

        if (String(message.uniqChatId) === String(currentOpenChatUniqId)) {
            const msg = {
                senderUserName: message.senderUserName,
                senderFullName: message.senderFullName || '',
                data: message.messageData || '',
                attachFiles: message.messageFiles || [],
                time: message.sendTime,
                isSeen: !!message.isSeen,
                isAdmin: isAdminSender
            };

            if (currentUploadingCardId) {
                const oldCard = document.getElementById(currentUploadingCardId);
                if (oldCard) {
                    const newMsgElement = document.createElement('div');
                    newMsgElement.innerHTML = renderMsgBubble(msg);
                    const newBubble = newMsgElement.firstChild;
                    oldCard.parentNode.replaceChild(newBubble, oldCard);
                    currentChatMessages.push(msg);
                    currentUploadingCardId = null;
                    markCurrentChatAsSeen();
                    return;
                }
            }

            currentChatMessages.push(msg);
            appendMessageToChat(msg);
            markCurrentChatAsSeen();
            return;
        }

        if (isMyMessage) {
            return;
        }

        if (!isAdminSender && chat && chat.isChatLocked) {
            const subject = chat.subject || 'چت جدید';
            const msgText = message.messageData || 'فایل دریافت شد';
            showChatNotification(chat.chatId, subject, msgText);
        }
    });

    chatConnection.on("ChatEndedByAdmin", (chatId) => {
        if (currentOpenChatId === chatId) {
            closeAdminChatUI();
        }
        chatConnection.invoke("GetAdminChatList").catch(() => { });
    });

    chatConnection.on("ChatUnlocked", (chatId) => {
        chatConnection.invoke("GetAdminChatList").catch(() => { });
        if (currentOpenChatId === chatId) {
            chatConnection.invoke("GetAdminChatData", chatId).catch(() => { });
            updateUserPresence(false);
        }
    });

    chatConnection.on("MessagesSeen", (chatUniqId, seenAt) => {
        if (currentOpenChatUniqId !== chatUniqId || !currentChatMessages) return;

        currentChatMessages.forEach(msg => {
            if (msg.senderUserName === currentAdminUserName) {
                msg.isSeen = true;
            }
        });

        document.querySelectorAll('.achat-msg.achat-out .msg-seen-status').forEach(el => {
            el.classList.add('seen');
        });
    });

    chatConnection.on("UserTyping", (chatUniqId, isTyping) => {
        if (currentPage !== 'chat') return;
        if (currentOpenChatUniqId === chatUniqId) {
            const typingEl = document.getElementById('adminTypingIndicator');
            if (!typingEl) return;
            if (isTyping) {
                typingEl.classList.add('active');
            } else {
                typingEl.classList.add('typing-exit');
                setTimeout(() => {
                    typingEl.classList.remove('active', 'typing-exit');
                }, 600);
            }
        }
    });

    chatConnection.onreconnecting(() => window.showToast?.("در حال اتصال به چت...", "info"));
    chatConnection.onreconnected(() => {
        window.showToast?.("اتصال چت برقرار شد.", "success");
        if (currentPage === 'chat') {
            chatConnection.invoke("GetAdminChatList").catch(() => { });
            if (currentOpenChatId) {
                chatConnection.invoke("GetAdminChatData", currentOpenChatId).catch(() => { });
            }
        }
    });

    chatConnection.onclose(async (error) => {
        if (currentPage !== 'chat') { chatConnection = null; return; }
        await new Promise(r => setTimeout(r, 2000));
        if (currentPage === 'chat') {
            const newToken = await renewSignalRToken();
            if (newToken) await connectChatHub(newToken);
        }
    });

    try {
        await chatConnection.start();
        console.log("✅ Chat Hub connected.");
        if (currentPage === 'chat') { chatConnection.invoke("GetAdminChatList").catch(() => { }); }
        loadCannedResponses();
    } catch (err) { console.error("❌ Chat Hub connection failed:", err); }
}

function disconnectChatHub() {
    if (chatConnection && chatConnection.state !== signalR.HubConnectionState.Disconnected) {
        chatConnection.stop().catch(() => { });
    }
    chatConnection = null;
}

// ========== CHAT FILTER (Centralized) ==========
function setChatFilter(filter) {
    if (currentChatFilter === filter) return;
    currentChatFilter = filter;

    const toggle = document.getElementById('chatFilterToggle');
    const indicator = document.getElementById('chatFilterIndicator');
    const buttons = toggle?.querySelectorAll('.chat-filter-btn');
    if (!toggle || !indicator || !buttons) return;

    buttons.forEach(b => b.classList.remove('active'));
    const activeBtn = toggle.querySelector(`.chat-filter-btn[data-filter="${filter}"]`);
    if (activeBtn) {
        activeBtn.classList.add('active');
        const left = activeBtn.offsetLeft;
        const width = activeBtn.offsetWidth;
        indicator.style.left = left + 'px';
        indicator.style.width = width + 'px';
    }

    applyChatFilter();
}

function applyChatFilter() {
    let filtered;
    if (currentChatFilter === 'yours') {
        filtered = allAdminChats.filter(c => c.isChatLocked === true);
    } else {
        filtered = allAdminChats.filter(c => c.isChatLocked === false);
    }
    renderFilteredChatList(filtered);
}

/**
 * Delegated click handler for chat cards (avoids inline onclick and "not defined" errors).
 */
function handleChatCardClick(e) {
    const card = e.target.closest('.chat-card');
    if (!card) return;
    const chatId = parseInt(card.dataset.chatId);
    const isLocked = card.dataset.isLocked === 'true';
    if (!isNaN(chatId)) {
        openAdminChat(chatId, isLocked);
    }
}

function renderFilteredChatList(chats) {
    const container = document.getElementById('adminChatListContainer');
    if (!container) return;

    const scrollTop = container.scrollTop;

    if (!chats || chats.length === 0) {
        container.innerHTML = `<div class="text-center py-5 text-secondary">
            <i class="fas fa-inbox fa-2x mb-2"></i>
            <p>${currentChatFilter === 'yours' ? 'چتی به شما اختصاص نیافته' : 'چت آزادی موجود نیست'}</p>
        </div>`;
        return;
    }

    let html = '<div class="admin-chat-list">';
    chats.forEach(chat => {
        const iconClass = chat.supportCategoryIconClass || 'fas fa-headset';
        const lastMsg = chat.lastMessageText || 'بدون پیام';
        const time = chat.lastMessageDateTime
            ? new Date(chat.lastMessageDateTime).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit' })
            : '';
        const creator = chat.creatorFullName || 'ناشناس';
        const category = chat.supportCategoryName;
        const isLocked = chat.isChatLocked;
        const lockIcon = isLocked ? '<i class="fas fa-lock chat-card-lock-icon"></i>' : '<i class="fas fa-unlock-alt chat-card-unlock-icon"></i>';
        const isActive = chat.chatId === currentOpenChatId;

        html += `
            <div class="chat-card ${isLocked ? 'locked' : ''} ${isActive ? 'active' : ''}"
                 data-chat-id="${chat.chatId}" data-is-locked="${isLocked}">
                <div class="chat-card-avatar">${lockIcon}<i class="${iconClass}"></i></div>
                <div class="chat-card-body">
                    <div class="chat-card-header">
                        <span class="chat-card-subject" title="${chat.subject}">${chat.subject}</span>
                        ${time ? `<span class="chat-card-time">${time}</span>` : ''}
                    </div>
                    <div class="chat-card-meta">
                        <span class="chat-card-creator"><i class="fas fa-user-circle"></i> ${creator}</span>
                        <span class="chat-card-category">${category}</span>
                    </div>
                    <div class="chat-card-lastmsg"><i class="fas fa-comment-dots"></i> ${lastMsg}</div>
                </div>
            </div>`;
    });
    html += '</div>';
    container.innerHTML = html;

    container.scrollTop = scrollTop;

    // Ensure delegated listener is active (re-attach because innerHTML replaced content)
    container.removeEventListener('click', handleChatCardClick);
    container.addEventListener('click', handleChatCardClick);
}

function updateChatCardInList(chat) {
    const card = document.querySelector(`.chat-card[data-chat-id="${chat.chatId}"]`);
    if (!card) return;

    const lastMsgEl = card.querySelector('.chat-card-lastmsg');
    if (lastMsgEl) {
        const lastMsg = chat.lastMessageText || 'بدون پیام';
        lastMsgEl.innerHTML = `<i class="fas fa-comment-dots"></i> ${lastMsg}`;
    }

    const timeEl = card.querySelector('.chat-card-time');
    if (timeEl) {
        const time = chat.lastMessageDateTime
            ? new Date(chat.lastMessageDateTime).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit' })
            : '';
        timeEl.textContent = time;
    }
}

function setupChatFilterToggle() {
    const container = document.getElementById('chatFilterContainer');
    if (!container) return;

    container.innerHTML = `
        <div class="chat-filter-toggle" id="chatFilterToggle">
            <div class="chat-filter-btn ${currentChatFilter === 'yours' ? 'active' : ''}" data-filter="yours">
                <i class="fas fa-lock"></i> چت‌های شما
            </div>
            <div class="chat-filter-btn ${currentChatFilter === 'free' ? 'active' : ''}" data-filter="free">
                <i class="fas fa-unlock-alt"></i> چت‌های آزاد
            </div>
            <span class="chat-filter-indicator" id="chatFilterIndicator"></span>
        </div>
    `;

    const toggle = document.getElementById('chatFilterToggle');
    const indicator = document.getElementById('chatFilterIndicator');
    const buttons = toggle.querySelectorAll('.chat-filter-btn');

    function updateIndicator() {
        const activeBtn = toggle.querySelector('.chat-filter-btn.active');
        if (!activeBtn) return;
        const left = activeBtn.offsetLeft;
        const width = activeBtn.offsetWidth;
        indicator.style.left = left + 'px';
        indicator.style.width = width + 'px';
    }

    updateIndicator();

    buttons.forEach(btn => {
        btn.addEventListener('click', function () {
            const filter = this.dataset.filter;
            if (filter === currentChatFilter) return;
            currentChatFilter = filter;

            buttons.forEach(b => b.classList.remove('active'));
            this.classList.add('active');

            updateIndicator();

            chatConnection.invoke("GetAdminChatList").catch(() => { });
        });
    });

    window.addEventListener('resize', updateIndicator);
}

// ========== SIDEBAR TOGGLE (Removed topbar button, replaced with minimize) ==========
function setupChatToggleInTopbar() {
    // Intentionally empty – chat sidebar toggle button removed from topbar.
}

function removeChatToggleFromTopbar() {
    // No-op, since we don't create the button.
}

function setupChatSidebar() {
    const sidebar = document.getElementById('chatSidebar');
    if (!sidebar) return;

   
    sidebar.classList.remove('minimized', 'closed');
    updateExpandArrow();

    const closeBtn = document.getElementById('chatSidebarClose');
    if (closeBtn) {
        const newBtn = closeBtn.cloneNode(true);
        closeBtn.parentNode.replaceChild(newBtn, closeBtn);
        newBtn.innerHTML = '<i class="fas fa-minus"></i>';
        newBtn.title = 'Minimize';
        newBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            sidebar.classList.toggle('minimized');
            updateExpandArrow();
        });
    }

   
    let arrow = document.getElementById('chatSidebarExpandArrow');
    if (!arrow) {
        arrow = document.createElement('div');
        arrow.id = 'chatSidebarExpandArrow';
        arrow.className = 'chat-sidebar-expand-arrow';
        arrow.innerHTML = '<i class="fas fa-chevron-left"></i>';
        arrow.addEventListener('click', (e) => {
            e.stopPropagation();
            sidebar.classList.remove('minimized');
            updateExpandArrow();
        });
        document.body.appendChild(arrow);
    }

    function updateExpandArrow() {
        const arrowEl = document.getElementById('chatSidebarExpandArrow');
        if (!arrowEl) return;
        arrowEl.classList.toggle('visible', sidebar.classList.contains('minimized'));
    }

    updateExpandArrow();

    
    const isMobile = window.innerWidth <= 768;
    if (isMobile) {
        sidebar.classList.add('closed');
        sidebar.classList.remove('minimized');
    } else {
        sidebar.classList.remove('closed');
    }
}

// ========== OPEN CHAT ==========
function openAdminChat(chatId, isLocked) {
    if (currentOpenChatId === chatId) return;
    closeAdminChatUI();
    currentOpenChatId = chatId;
    currentChatIsMine = isLocked;
    const mainArea = document.getElementById('chatMainArea');
    mainArea.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-primary"></div></div>`;
    chatConnection.invoke("GetAdminChatData", chatId).catch(err => {
        console.error(err);
        window.showToast?.("خطا در دریافت اطلاعات چت", "error");
    });
    applyChatFilter();
}

function closeAdminChatUI() {
    const mainArea = document.getElementById('chatMainArea');
    if (mainArea) {
        mainArea.innerHTML = `<div class="chat-placeholder">...</div>`;
    }
    currentOpenChatId = null;
    currentOpenChatUniqId = null;
    currentChatData = null;
    currentChatMessages = [];
    selectedFiles = [];
    applyChatFilter();
}

// ========== RENDER CHAT UI ==========
function renderAdminChatUI(chatData, messages) {
    const mainArea = document.getElementById('chatMainArea');
    if (!mainArea) return;

    const isLockedByMe = currentChatIsMine;
    const userFullName = `${chatData.creatorFirstName} ${chatData.creatorLastName}`;

    let html = `
    <div class="admin-chat-area">
        <div class="admin-chat-header">
            <div class="chat-header-left">
                <div class="chat-subject-title">${chatData.subject}</div>
                <div class="chat-header-details-row">
                    <a href="#" onclick="navigateToUserProfile(${chatData.creatorId}); return false;" class="chat-user-link">
                        <span class="chat-user-fullname">${userFullName}</span>
                        <span class="chat-user-username">@${chatData.creatorUserName}</span>
                    </a>
                    <span class="chat-unit-name">${chatData.supportCategoryName}</span>
                    <span class="chat-status-badge ${isLockedByMe ? 'locked' : 'unlocked'}">
                        ${isLockedByMe ? '<i class="fas fa-lock"></i> قفل شده' : '<i class="fas fa-unlock-alt"></i> آزاد'}
                    </span>
                    <span id="userPresenceIndicator" class="presence offline" title="آفلاین"></span>
                    <div id="adminTypingIndicator" class="typing-indicator">
                        <span class="typing-dot"></span>
                        <span class="typing-dot"></span>
                        <span class="typing-dot"></span>
                    </div>
                </div>
            </div>
            <div class="header-actions-group">
                ${isLockedByMe ? `
                    <button class="minimal-btn end-btn" onclick="requestEndChat(${chatData.chatId})">
                        <i class="fas fa-check-circle"></i> پایان
                    </button>
                    <button class="minimal-btn unlock-btn" onclick="unlockChat(${chatData.chatId})">
                        <i class="fas fa-lock-open"></i> آزادسازی
                    </button>
                ` : ''}
                <button class="minimal-btn ban-btn" onclick="banUserFromChat(${chatData.creatorId})">
                    <i class="fas fa-gavel"></i> مسدود کردن
                </button>
            </div>
        </div>

        <div class="admin-chat-messages" id="adminChatMessages">
            ${messages.map(msg => renderMsgBubble(msg)).join('')}
        </div>
        ${isLockedByMe ? `
        <div class="admin-chat-footer">
            <div class="message-form">
                <button class="btn-attach-file" onclick="document.getElementById('adminFileInput').click()">
                    <i class="fas fa-paperclip"></i>
                </button>
                <input type="file" id="adminFileInput" style="display:none" multiple>
                <button class="btn-attach-file" id="cannedTriggerBtn" onclick="openCannedPanel()">
                    <i class="fas fa-clipboard-list"></i>
                </button>
                <input type="text" class="form-control" id="adminMessageInput" placeholder="پیام خود را بنویسید..." autocomplete="off">
                <button class="btn-send-msg" onclick="sendAdminMessage()">
                    <i class="fas fa-paper-plane"></i>
                </button>
            </div>
            <div id="adminFilePreview" class="file-preview"></div>
        </div>
        ` : `
        <div class="admin-chat-footer">
            <button class="lock-chat-bottom" onclick="lockChat(${chatData.chatId})">
                <i class="fas fa-lock"></i> برای پاسخگویی روی چت قفل کنید
            </button>
        </div>
        `}
    </div>`;

    mainArea.innerHTML = html;
    
    if (currentUserIsOnline !== null) {
        updateUserPresence(currentUserIsOnline);
    }

    // File input management
    const fileInput = document.getElementById('adminFileInput');
    if (fileInput) {
        fileInput.addEventListener('change', () => {
            selectedFiles = Array.from(fileInput.files);
            renderFilePreview();
        });
    }

    // Scroll messages to bottom
    const msgDiv = document.getElementById('adminChatMessages');
    if (msgDiv) msgDiv.scrollTop = msgDiv.scrollHeight;

    // Admin typing management
    
    const msgInput = document.getElementById('adminMessageInput');
    if (msgInput && chatConnection) {
        msgInput.addEventListener('input', () => {
            const hasText = msgInput.value.trim().length > 0;
            if (hasText && !typingActive) {
                typingActive = true;
                chatConnection.invoke("StartTyping", currentOpenChatId).catch(() => { });
            } else if (!hasText && typingActive) {
                typingActive = false;
                chatConnection.invoke("StopTyping", currentOpenChatId).catch(() => { });
            }
        });
        msgInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                if (typingActive) {
                    typingActive = false;
                    chatConnection.invoke("StopTyping", currentOpenChatId).catch(() => { });
                }
                sendAdminMessage();
            }
        });
    }
}

// ========== RENDER MESSAGE BUBBLE ==========
function renderMsgBubble(msg) {
    const isAdminMsg = !msg.senderUserName || msg.senderUserName.trim() === '';
    const rowClass = isAdminMsg ? 'achat-out' : 'achat-in';

    let content = '';
    if (msg.data) {
        content = escapeHtml(msg.data);
    }

    if (msg.attachFiles && msg.attachFiles.length > 0) {
        content += '<div class="achat-files">';
        msg.attachFiles.forEach(f => {
            const name = f.origianlName || f.originalName || 'فایل';
            const downName = f.downloadName;
            const isImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(downName);
            if (isImg) {
                content += `
                <div class="achat-file image-attachment" data-download="${downName}" data-original="${name}">
                    <div class="image-preview-thumb">
                        <img src="/download/${encodeURIComponent(downName)}?originalName=${encodeURIComponent(name)}" alt="${name}" loading="lazy" />
                    </div>
                    <span>${name}</span>
                </div>`;
            } else {
                content += `
                <div class="achat-file" data-download="${downName}" data-original="${name}">
                    <i class="fas fa-file"></i>
                    <span>${name}</span>
                </div>`;
            }
        });
        content += '</div>';
    }

    let timeStr = '';
    if (msg.time) {
        if (/^\d{2}:\d{2}$/.test(msg.time)) {
            timeStr = msg.time;
        } else {
            const date = new Date(msg.time);
            if (!isNaN(date.getTime())) {
                timeStr = date.toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit' });
            }
        }
    }

    let seenHtml = '';
    if (isAdminMsg) {
        const seenClass = msg.isSeen ? 'seen' : '';
        seenHtml = `
        <span class="msg-seen-status ${seenClass}">
            <span class="glass-badge">
                <span class="unseen-icon">
                    <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="rgba(255,255,255,0.85)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M4.5 16.5c-1.5 1.26-2 5-2 5s3.74-.5 5-2c.71-.84.7-2.13-.09-2.91a2.18 2.18 0 0 0-2.91-.09z"/>
                        <path d="M12 15l-3-3a22 22 0 0 1 2-3.95A12.88 12.88 0 0 1 22 2c0 2.72-.78 7.5-6 11a22.35 22.35 0 0 1-4 2z"/>
                        <path d="M9 12H4s.55-3.03 2-4c1.62-1.08 5 0 5 0"/>
                        <path d="M12 15v5s3.03-.55 4-2c1.08-1.62 0-5 0-5"/>
                    </svg>
                </span>
                <span class="seen-icon">
                    <svg class="seen-check" viewBox="0 0 18 12" width="14" height="10" fill="none">
                        <polyline class="tick" points="3,6 6,9 12,4" />
                    </svg>
                </span>
            </span>
        </span>`;
    }

    return `
    <div class="achat-msg ${rowClass}">
        <div class="achat-msg-bubble">
            ${content}
            <div class="achat-msg-meta">
                <span class="achat-msg-time">${timeStr}</span>
                ${seenHtml}
            </div>
        </div>
    </div>`;
}

function appendMessageToChat(message) {
    const container = document.getElementById('adminChatMessages');
    if (!container) return;

    container.insertAdjacentHTML('beforeend', renderMsgBubble(message));

    const newMessages = container.querySelectorAll('.achat-msg:not(.message-enter)');
    const lastMsg = newMessages[newMessages.length - 1];
    if (lastMsg) {
        lastMsg.classList.add('message-enter');
        lastMsg.addEventListener('animationend', () => {
            lastMsg.classList.remove('message-enter');
        }, { once: true });
    }

    container.scrollTop = container.scrollHeight;
}

// ========== IMAGE PREVIEW & DOWNLOAD FUNCTIONS ==========
function openImagePreview(downloadName, originalName) {
    const oldModal = document.getElementById('imagePreviewModal');
    if (oldModal) oldModal.remove();

    const modalHtml = `
        <div class="modal fade" id="imagePreviewModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered modal-xl">
                <div class="modal-content glass-modal">
                    <div class="modal-header"><span class="modal-title">${originalName}</span><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
                    <div class="modal-body text-center p-0"><img src="/download/${encodeURIComponent(downloadName)}?originalName=${encodeURIComponent(originalName)}" class="img-fluid rounded" alt="${originalName}" style="max-height:80vh;width:auto;" /></div>
                    <div class="modal-footer"><a href="/download/${encodeURIComponent(downloadName)}?originalName=${encodeURIComponent(originalName)}" class="btn btn-primary" download="${originalName}"><i class="fas fa-download"></i> دانلود</a><button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">بستن</button></div>
                </div>
            </div>
        </div>`;

    document.body.insertAdjacentHTML('beforeend', modalHtml);
    const modal = new bootstrap.Modal(document.getElementById('imagePreviewModal'));
    modal.show();
    document.getElementById('imagePreviewModal').addEventListener('hidden.bs.modal', function () {
        this.remove();
    });
}

function downloadFile(downloadName, originalName) {
    const a = document.createElement('a');
    a.href = `/download/${encodeURIComponent(downloadName)}?originalName=${encodeURIComponent(originalName)}`;
    a.download = originalName;
    document.body.appendChild(a);
    a.click();
    a.remove();
}

// ========== FILE PREVIEW ==========
function renderFilePreview() {
    const preview = document.getElementById('adminFilePreview');
    if (!preview) return;

    if (selectedFiles.length === 0) {
        preview.innerHTML = '';
        preview.style.display = 'none';
        return;
    }

    preview.style.display = 'flex';
    preview.innerHTML = '';
    selectedFiles.forEach((file, index) => {
        const item = document.createElement('div');
        item.className = 'file-preview-item';
        if (file.type.startsWith('image/')) {
            item.innerHTML = `
                <img src="${URL.createObjectURL(file)}" alt="${file.name}" style="width:30px;height:30px;object-fit:cover;border-radius:4px;">
                <span>${file.name}</span>
                <span class="remove-file" data-index="${index}"><i class="fas fa-times"></i></span>
            `;
        } else {
            item.innerHTML = `
                <i class="fas fa-file"></i>
                <span>${file.name}</span>
                <span class="remove-file" data-index="${index}"><i class="fas fa-times"></i></span>
            `;
        }
        preview.appendChild(item);
    });

    preview.querySelectorAll('.remove-file').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const idx = parseInt(e.currentTarget.dataset.index);
            selectedFiles.splice(idx, 1);
            renderFilePreview();
        });
    });
}

// ========== DRAG & DROP OVERLAY ==========
function createDragOverlay() {
    if (dragOverlay) return;
    dragOverlay = document.createElement('div');
    dragOverlay.className = 'drag-overlay';
    dragOverlay.innerHTML = `
        <div class="drag-overlay-card">
            <div class="drag-overlay-icon"><i class="fas fa-cloud-upload-alt"></i></div>
            <div class="drag-overlay-title">فایل‌های خود را اینجا رها کنید</div>
            <div class="drag-overlay-subtitle">برای ارسال سریع فایل‌ها در این بخش بکشید و رها کنید</div>
            <div class="drag-overlay-hint"><span><i class="fas fa-image"></i> تصاویر</span><span><i class="fas fa-file-pdf"></i> اسناد</span><span><i class="fas fa-file-archive"></i> آرشیو</span></div>
        </div>`;
    document.body.appendChild(dragOverlay);
}

function setupDragAndDrop() {
    createDragOverlay();

    const handleDragOver = (e) => {
        e.preventDefault();
        dragOverlay.classList.add('active');
    };

    const handleDragLeave = (e) => {
        if (!e.relatedTarget || e.relatedTarget.nodeName === 'HTML') {
            dragOverlay.classList.remove('active');
        }
    };

    const handleDrop = (e) => {
        e.preventDefault();
        dragOverlay.classList.remove('active');
        const files = Array.from(e.dataTransfer.files);
        if (files.length) {
            selectedFiles = selectedFiles.concat(files).slice(0, 5);
            if (selectedFiles.length > 5) window.showToast?.('حداکثر ۵ فایل می‌توانید انتخاب کنید.', 'warning');
            renderFilePreview();
        }
    };

    document.addEventListener('dragover', handleDragOver);
    document.addEventListener('dragleave', handleDragLeave);
    document.addEventListener('drop', handleDrop);

    dragHandlers = { handleDragOver, handleDragLeave, handleDrop };
}

function removeDragAndDrop() {
    if (dragHandlers) {
        document.removeEventListener('dragover', dragHandlers.handleDragOver);
        document.removeEventListener('dragleave', dragHandlers.handleDragLeave);
        document.removeEventListener('drop', dragHandlers.handleDrop);
        dragHandlers = null;
    }
    if (dragOverlay) {
        dragOverlay.remove();
        dragOverlay = null;
    }
}

// ========== SEND FILE WITH PROGRESS  ==========
async function sendAdminFile() {
    const fileInput = document.getElementById('adminFileInput');
    const files = selectedFiles.length > 0 ? selectedFiles : (fileInput?.files ? Array.from(fileInput.files) : []);
    if (files.length === 0) return;
    if (files.length > 5) {
        window.showToast?.('حداکثر ۵ فایل می‌توانید ارسال کنید.', 'warning');
        return;
    }

    const formData = new FormData();
    formData.append('ChatId', currentOpenChatId);
    const msgText = document.getElementById('adminMessageInput')?.value.trim();
    if (msgText) formData.append('MessageData', msgText);
    files.forEach(file => formData.append('AttachFiles', file));

    const uploadCardId = 'upload-progress-' + Date.now();
    currentUploadingCardId = uploadCardId;

    const progressCard = document.createElement('div');
    progressCard.id = uploadCardId;
    progressCard.className = 'achat-msg achat-out message-enter';
    progressCard.innerHTML = `
        <div class="achat-msg-bubble">
            <div class="upload-card">
                <div class="upload-card-header">
                    <i class="fas fa-cloud-upload-alt"></i>
                    <span>در حال آپلود ${files.length} فایل...</span>
                </div>
                <div class="upload-file-list">
                    ${files.map(f => `<div class="upload-file-item">
                        <i class="fas fa-file-alt file-icon"></i>
                        <span class="file-name">${f.name}</span>
                        <span class="file-status"><i class="fas fa-spinner fa-pulse"></i></span>
                    </div>`).join('')}
                </div>
                <div class="upload-overall-progress">
                    <div class="progress-bar-glass">
                        <div class="progress-fill" style="width:0%"></div>
                    </div>
                    <span class="progress-percent">0%</span>
                </div>
            </div>
        </div>`;

    const msgContainer = document.getElementById('adminChatMessages');
    if (!msgContainer) return;
    msgContainer.appendChild(progressCard);
    msgContainer.scrollTop = msgContainer.scrollHeight;

    const progressFill = progressCard.querySelector('.progress-fill');
    const progressPercent = progressCard.querySelector('.progress-percent');
    const fileStatuses = progressCard.querySelectorAll('.file-status');

    const xhr = new XMLHttpRequest();
    xhr.upload.addEventListener('progress', e => {
        if (e.lengthComputable) {
            const percent = Math.round((e.loaded / e.total) * 100);
            progressFill.style.width = percent + '%';
            progressPercent.textContent = percent + '%';
        }
    });

    xhr.addEventListener('load', () => {
        if (xhr.status === 200) {
            document.getElementById('adminMessageInput').value = '';
            document.getElementById('adminFileInput').value = '';
            selectedFiles = [];
            renderFilePreview();

            const tempMsg = {
                senderUserName: currentAdminUserName,
                senderFullName: '',
                data: msgText || '',
                attachFiles: files.map(f => ({
                    origianlName: f.name,
                    originalName: f.name,
                    downloadName: '',
                    localUrl: f.type.startsWith('image/') ? URL.createObjectURL(f) : null
                })),
                time: new Date().toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit' }),
                isSeen: false,
                isAdmin: true
            };

            const tempHtml = renderMsgBubble(tempMsg);
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = tempHtml;
            const tempBubble = tempDiv.firstChild;
            tempBubble.id = uploadCardId;

            progressCard.parentNode.replaceChild(tempBubble, progressCard);
        } else {
            fileStatuses.forEach(s => s.innerHTML = '<i class="fas fa-times-circle" style="color:#ef4444;"></i>');
            progressCard.querySelector('.upload-card-header').innerHTML =
                '<i class="fas fa-times-circle" style="color:#ef4444;"></i><span>خطا در ارسال</span>';
            setTimeout(() => {
                if (progressCard.parentNode) progressCard.remove();
                currentUploadingCardId = null;
            }, 4000);
        }
    });

    xhr.addEventListener('error', () => {
        fileStatuses.forEach(s => s.innerHTML = '<i class="fas fa-times-circle" style="color:#ef4444;"></i>');
        progressCard.querySelector('.upload-card-header').innerHTML =
            '<i class="fas fa-times-circle" style="color:#ef4444;"></i><span>خطای شبکه</span>';
        setTimeout(() => {
            if (progressCard.parentNode) progressCard.remove();
            currentUploadingCardId = null;
        }, 4000);
    });

    xhr.open('POST', '/api/admin/chat/send-file');
    xhr.send(formData);
}

function renderMsgBubble(msg) {
    const isAdminMsg = !msg.senderUserName || msg.senderUserName.trim() === '';
    const rowClass = isAdminMsg ? 'achat-out' : 'achat-in';

    let content = '';
    if (msg.data) {
        content = escapeHtml(msg.data);
    }

    if (msg.attachFiles && msg.attachFiles.length > 0) {
        content += '<div class="achat-files">';
        msg.attachFiles.forEach(f => {
            const name = f.origianlName || f.originalName || 'فایل';
            const downName = f.downloadName;
            const isImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(downName) || f.localUrl;
            if (isImg) {
                const src = f.localUrl
                    ? f.localUrl
                    : `/download/${encodeURIComponent(downName)}?originalName=${encodeURIComponent(name)}`;
                content += `
                <div class="achat-file image-attachment" data-download="${downName}" data-original="${name}">
                    <div class="image-preview-thumb">
                        <img src="${src}" alt="${name}" loading="lazy" />
                    </div>
                    <span>${name}</span>
                </div>`;
            } else {
                content += `
                <div class="achat-file" data-download="${downName}" data-original="${name}">
                    <i class="fas fa-file"></i>
                    <span>${name}</span>
                </div>`;
            }
        });
        content += '</div>';
    }

    let timeStr = '';
    if (msg.time) {
        if (/^\d{2}:\d{2}$/.test(msg.time)) {
            timeStr = msg.time;
        } else {
            const date = new Date(msg.time);
            if (!isNaN(date.getTime())) {
                timeStr = date.toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit' });
            }
        }
    }

    let seenHtml = '';
    if (isAdminMsg) {
        const seenClass = msg.isSeen ? 'seen' : '';
        seenHtml = `
        <span class="msg-seen-status ${seenClass}">
            <span class="glass-badge">
                <span class="unseen-icon">
                    <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="rgba(255,255,255,0.85)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M4.5 16.5c-1.5 1.26-2 5-2 5s3.74-.5 5-2c.71-.84.7-2.13-.09-2.91a2.18 2.18 0 0 0-2.91-.09z"/>
                        <path d="M12 15l-3-3a22 22 0 0 1 2-3.95A12.88 12.88 0 0 1 22 2c0 2.72-.78 7.5-6 11a22.35 22.35 0 0 1-4 2z"/>
                        <path d="M9 12H4s.55-3.03 2-4c1.62-1.08 5 0 5 0"/>
                        <path d="M12 15v5s3.03-.55 4-2c1.08-1.62 0-5 0-5"/>
                    </svg>
                </span>
                <span class="seen-icon">
                    <svg class="seen-check" viewBox="0 0 18 12" width="14" height="10" fill="none">
                        <polyline class="tick" points="3,6 6,9 12,4" />
                    </svg>
                </span>
            </span>
        </span>`;
    }

    return `
    <div class="achat-msg ${rowClass}">
        <div class="achat-msg-bubble">
            ${content}
            <div class="achat-msg-meta">
                <span class="achat-msg-time">${timeStr}</span>
                ${seenHtml}
            </div>
        </div>
    </div>`;
}

// ========== USER PRESENCE & TYPING ==========
function updateUserPresence(isOnline) {
    currentUserIsOnline = isOnline;

    const presenceEl = document.getElementById('userPresenceIndicator');
    if (!presenceEl) return;

    presenceEl.className = 'presence ' + (isOnline ? 'online' : 'offline');
    presenceEl.title = isOnline ? 'آنلاین' : 'آفلاین';

    // حذف المان و تایمر قبلی
    const prevMsg = document.getElementById('presenceMessage');
    if (prevMsg) prevMsg.remove();
    if (presenceTypewriterTimerId) {
        clearTimeout(presenceTypewriterTimerId);
        presenceTypewriterTimerId = null;
    }

    const msgEl = document.createElement('span');
    msgEl.id = 'presenceMessage';
    msgEl.className = 'presence-message';
    presenceEl.parentNode.insertBefore(msgEl, presenceEl.nextSibling);

    const message = isOnline ? 'User On The Line...' : 'User left us...';

    typeWriterEffect(msgEl, message, () => {
        setTimeout(() => {
            if (msgEl.parentNode) msgEl.remove();
            presenceTypewriterTimerId = null;
        }, 2500);
    }, (id) => {
        presenceTypewriterTimerId = id;
    });
}
function typeWriterEffect(element, text, callback, setTimerId) {
    
    if (setTimerId && presenceTypewriterTimerId) {
        clearTimeout(presenceTypewriterTimerId);
        presenceTypewriterTimerId = null;
    }
    element.textContent = '';
    element.style.display = 'inline-block';
    let i = 0;
    const speed = 50;
    function type() {
        if (!element.parentNode) {
            presenceTypewriterTimerId = null;
            return;
        }
        if (i < text.length) {
            element.textContent += text.charAt(i);
            i++;
            presenceTypewriterTimerId = setTimeout(type, speed);
            if (setTimerId) setTimerId(presenceTypewriterTimerId);
        } else {
            presenceTypewriterTimerId = null;
            if (callback) callback();
        }
    }
    type();
}

function markCurrentChatAsSeen() {
    if (!currentOpenChatUniqId || !chatConnection ||
        chatConnection.state !== signalR.HubConnectionState.Connected) return;
    chatConnection.invoke("MarkMessagesAsSeen", currentOpenChatUniqId).catch(() => { });
}

// ========== NOTIFICATIONS & BAG ==========
function showChatNotification(chatId, subject, messageText) {
    if (globalChatGhost) {
        addToNotificationBag(chatId, subject, messageText);
        return;
    }

    let stack = document.getElementById('notificationStack');
    if (!stack) {
        stack = document.createElement('div');
        stack.id = 'notificationStack';
        stack.className = 'notification-stack';
        document.body.appendChild(stack);
    }

    if (chatTimers.has(chatId)) {
        clearTimeout(chatTimers.get(chatId));
        chatTimers.delete(chatId);

        const oldCard = stack.querySelector(`.chat-notification-card[data-chat-id="${chatId}"]`);
        if (oldCard) {
            const oldSubj = oldCard.dataset.subject || subject;
            const oldMsg = oldCard.dataset.message || messageText;
            addToNotificationBag(chatId, oldSubj, oldMsg);
            oldCard.remove();
        }
    }

    const card = document.createElement('div');
    card.className = 'chat-notification-card';
    card.dataset.chatId = chatId;
    card.dataset.subject = subject;
    card.dataset.message = messageText;

    const muteIcon = globalChatGhost ? 'fa-volume-up' : 'fa-volume-mute';
    const muteTitle = globalChatGhost ? 'فعال کردن صدا' : 'بی‌صدا کردن همه';

    card.innerHTML = `
        <div class="notif-content" data-chat-id="${chatId}">
            <div class="notif-title">${escapeHtml(subject)}</div>
            <div class="notif-message">${escapeHtml(messageText)}</div>
        </div>
        <div class="notif-actions">
            <button class="notif-btn mute-btn" title="${muteTitle}"><i class="fas ${muteIcon}"></i></button>
            <button class="notif-btn minimize-btn" title="کوچک کردن"><i class="fas fa-minus"></i></button>
            <button class="notif-btn close-btn" title="بستن"><i class="fas fa-times"></i></button>
        </div>
    `;

    card.querySelector('.notif-content').addEventListener('click', () => {
        if (chatTimers.has(chatId)) {
            clearTimeout(chatTimers.get(chatId));
            chatTimers.delete(chatId);
        }
        openAdminChat(chatId, true);
        card.remove();
    });

    card.querySelector('.close-btn').addEventListener('click', () => {
        if (chatTimers.has(chatId)) {
            clearTimeout(chatTimers.get(chatId));
            chatTimers.delete(chatId);
        }
        card.remove();
    });

    card.querySelector('.minimize-btn').addEventListener('click', () => {
        if (chatTimers.has(chatId)) {
            clearTimeout(chatTimers.get(chatId));
            chatTimers.delete(chatId);
        }
        card.classList.add('minimizing');
        setTimeout(() => {
            card.remove();
            addToNotificationBag(chatId, subject, messageText);
        }, 350);
    });

    card.querySelector('.mute-btn').addEventListener('click', () => {
        if (chatTimers.has(chatId)) {
            clearTimeout(chatTimers.get(chatId));
            chatTimers.delete(chatId);
        }

        globalChatGhost = !globalChatGhost;
        localStorage.setItem('globalChatGhost', globalChatGhost);

        card.classList.add('minimizing');
        setTimeout(() => {
            card.remove();
            addToNotificationBag(chatId, subject, messageText);
        }, 350);

        if (bagListElement && bagListElement.classList.contains('open')) {
            renderBagItems();
        }
    });

    stack.appendChild(card);

    const timerId = setTimeout(() => {
        chatTimers.delete(chatId);
        if (card.parentNode) {
            card.classList.add('minimizing');
            setTimeout(() => {
                if (card.parentNode) {
                    card.remove();
                    addToNotificationBag(chatId, subject, messageText);
                }
            }, 350);
        }
    }, 3000);

    chatTimers.set(chatId, timerId);
}

function addToNotificationBag(chatId, subject, messageText) {
    notificationBag.push({ chatId, subject, messageText });
    updateBagUI();
    if (bagListElement && bagListElement.classList.contains('open')) {
        renderBagItems();
    }
}

function updateBagUI() {
    if (bagElement && !document.body.contains(bagElement)) {
        bagElement = null;
        bagListElement = null;
    }

    if (!bagElement) {
        const actionsContainer = document.querySelector('.topbar-actions');
        if (!actionsContainer) return;

        bagElement = document.createElement('div');
        bagElement.className = 'notification-bag';
        bagElement.innerHTML = '<i class="fas fa-bell"></i><span class="badge-count">0</span>';
        actionsContainer.appendChild(bagElement);

        bagListElement = document.createElement('div');
        bagListElement.className = 'notif-bag-list';
        bagElement.appendChild(bagListElement);

        bagElement.addEventListener('click', (e) => {
            if (bagListElement.contains(e.target)) {
                return;
            }
            e.stopPropagation();
            bagListElement.classList.toggle('open');
            if (bagListElement.classList.contains('open')) {
                renderBagItems();
            }
        });

        document.addEventListener('click', (e) => {
            if (!bagElement.contains(e.target)) {
                bagListElement.classList.remove('open');
            }
        });
    }

    const count = notificationBag.length;
    bagElement.querySelector('.badge-count').textContent = count;
    bagElement.style.display = count === 0 ? 'none' : 'inline-flex';
}

function renderBagItems() {
    if (!bagListElement) return;

    const muteIcon = globalChatGhost ? 'fa-volume-up' : 'fa-volume-mute';
    const muteLabel = globalChatGhost ? 'بی‌صدا فعال' : 'بی‌صدا کردن همه';

    const muteRow = `
        <div class="bag-mute-row">
            <button class="global-ghost-btn ${globalChatGhost ? 'active' : ''}" id="globalGhostToggle">
                <i class="fas ${muteIcon}"></i>
                <span>${muteLabel}</span>
            </button>
        </div>
    `;

    const itemsHtml = notificationBag.map((item, index) => {
        return `
        <div class="bag-item" data-chat-id="${item.chatId}" data-index="${index}">
            <div class="bag-item-content">
                <div class="bag-item-title">${escapeHtml(item.subject)}</div>
                <div class="bag-item-msg">${escapeHtml(item.messageText)}</div>
            </div>
            <div class="bag-item-actions">
                <button class="notif-btn close-btn bag-remove-btn" title="حذف"><i class="fas fa-times"></i></button>
            </div>
        </div>`;
    }).join('');

    bagListElement.innerHTML = muteRow + itemsHtml;

    bagListElement.querySelectorAll('.bag-item-content').forEach(el => {
        el.addEventListener('click', () => {
            const chatId = parseInt(el.parentElement.dataset.chatId);
            openAdminChat(chatId, true);
            notificationBag = notificationBag.filter(n => n.chatId !== chatId);
            updateBagUI();
            renderBagItems();
            bagListElement.classList.remove('open');
        });
    });

    bagListElement.querySelectorAll('.bag-remove-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const index = parseInt(btn.parentElement.parentElement.dataset.index);
            notificationBag.splice(index, 1);
            updateBagUI();
            renderBagItems();
        });
    });

    const globalBtn = document.getElementById('globalGhostToggle');
    if (globalBtn) {
        globalBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            globalChatGhost = !globalChatGhost;
            localStorage.setItem('globalChatGhost', globalChatGhost);
            renderBagItems();
        });
    }
}

// ========== SEND MESSAGE (Check files first) ==========
async function sendAdminMessage() {
    const fileInput = document.getElementById('adminFileInput');
    if ((selectedFiles && selectedFiles.length > 0) || (fileInput && fileInput.files.length > 0)) {
        await sendAdminFile();
        return;
    }

    const input = document.getElementById('adminMessageInput');
    const message = input.value.trim();
    if (!message || !currentOpenChatId) return;

    if (typingActive) {
        typingActive = false;
        chatConnection.invoke("StopTyping", currentOpenChatId).catch(() => { });
    }

    input.value = '';
    try {
        await chatConnection.invoke("SendMessageToUser", {
            ChatId: currentOpenChatId,
            MessageData: message
        });
    } catch (err) {
        window.showToast?.("خطا در ارسال پیام", "error");
    }
}

function requestEndChat(chatId) {
    if (!chatId) return;

    const modalHtml = `
    <div class="modal fade" id="confirmEndChatModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content glass-modal">
                <div class="modal-header">
                    <h5 class="modal-title">پایان گفتگو</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body text-center">
                    <div class="mb-3" style="font-size:2.5rem;color:#10b981;">
                        <i class="fas fa-check-circle"></i>
                    </div>
                    <p class="fw-bold">آیا مطمئن هستید که می‌خواهید این گفتگو را پایان دهید؟</p>
                    <p class="text-secondary small">پس از پایان، امکان ارسال پیام جدید وجود ندارد و وضعیت چت به «تکمیل شده» تغییر می‌کند. این عملیات قابل بازگشت نیست.</p>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">انصراف</button>
                    <button type="button" class="btn-info-end" id="confirmEndChatBtn">پایان گفتگو</button>
                </div>
            </div>
        </div>
    </div>`;

    const existingModal = document.getElementById('confirmEndChatModal');
    if (existingModal) existingModal.remove();

    document.body.insertAdjacentHTML('beforeend', modalHtml);
    const modalEl = document.getElementById('confirmEndChatModal');
    const modal = new bootstrap.Modal(modalEl);
    modal.show();

    document.getElementById('confirmEndChatBtn').addEventListener('click', async () => {
        modal.hide();
        try {
            await chatConnection.invoke("EndChatByAdmin", chatId);
        } catch (err) {
            console.error(err);
            window.showToast?.("خطا در پایان گفتگو.", 'error');
        }
    });

    modalEl.addEventListener('hidden.bs.modal', () => modalEl.remove());
}

// ========== CHAT ACTIONS ==========
async function lockChat(chatId) {
    if (!chatConnection) return;
    try { await chatConnection.invoke("LockChat", chatId); } catch (e) { window.showToast?.("خطا", "error"); }
}

async function unlockChat(chatId) {
    if (!chatConnection) return;
    try { await chatConnection.invoke("UnLockChat", chatId); } catch (e) { window.showToast?.("خطا", "error"); }
}

async function endChat(chatUniqId) {
    if (!chatConnection) return;
    try { await chatConnection.invoke("EndChat", chatUniqId); } catch (e) { window.showToast?.("خطا", "error"); }
}

function banUserFromChat(userId) {
    const isBanned = currentChatData?.creatorIsBanned ?? false;
    openBanSlideover(userId, isBanned);
}

function navigateToUserProfile(userId) {
    loadPage('users-roles', true, { userId });
}

// ========== SUPPORT CATEGORIES ==========
function renderSupportCategoriesTable(list) {
    const tbody = document.getElementById('supportCategoriesTableBody');
    if (!tbody) return;
    tbody.innerHTML = list?.length ? list.map(c => `<tr>
        <td>${c.id}</td>
        <td>${c.name}</td>
        <td>${c.isActive ? '<span class="badge badge-success">فعال</span>' : '<span class="badge badge-danger">غیرفعال</span>'}</td>
        <td>${c.userCount}</td>
        <td>
            <button class="btn btn-sm btn-outline-info" onclick="navigateToEditSupportCategory(${c.id})"><i class="fas fa-edit"></i></button>
        </td>
    </tr>`).join('') : '<tr><td colspan="5" class="text-center">واحدی یافت نشد</td></tr>';
}

function setupSupportCategorySearch() {
    const searchInput = document.getElementById('supportCategorySearchInput');
    const tbody = document.getElementById('supportCategoriesTableBody');
    if (!searchInput || !tbody) return;
    searchInput.addEventListener('input', () => {
        const term = searchInput.value.trim().toLowerCase();
        const rows = tbody.querySelectorAll('tr');
        rows.forEach(row => {
            const name = row.cells[1]?.textContent.toLowerCase() || '';
            row.style.display = term === '' || name.includes(term) ? '' : 'none';
        });
    });
}

function loadAddSupportCategoryData() {
    const btn = document.getElementById('saveAddSupportCategoryBtn');
    if (!btn) {
        console.error('❌ saveAddSupportCategoryBtn not found');
        return;
    }

    enhanceIconInput('addSupportCategoryIcon');

    btn.addEventListener('click', () => {
        if (!adminConnection || adminConnection.state !== signalR.HubConnectionState.Connected) {
            window.showToast?.("ارتباط با سرور برقرار نیست. لطفاً منتظر بمانید...", "warning");
            return;
        }

        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-pulse"></i> در حال ذخیره...';

        const name = document.getElementById('addSupportCategoryName')?.value.trim();
        const details = document.getElementById('addSupportCategoryDetails')?.value.trim();
        const iconKey = document.getElementById('addSupportCategoryIcon')?.value.trim();

        if (!name || !details) {
            window.showToast?.("نام و توضیحات الزامی است.", "warning");
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-save"></i> ذخیره';
            return;
        }

        const dto = { Name: name, Details: details, IconKey: iconKey || null };
        adminConnection.invoke("AddSupportCategory", dto)
            .then(() => {
                // on success the page navigates away, no need to re-enable
            })
            .catch(err => {
                console.error(err);
                window.showToast?.("خطا در ذخیره", "error");
                btn.disabled = false;
                btn.innerHTML = '<i class="fas fa-save"></i> ذخیره';
            });
    });
}

function safeLoadEditSupportCategoryData(id) {
    if (!adminConnection || adminConnection.state !== signalR.HubConnectionState.Connected) {
        window.showToast?.("ارتباط با سرور برقرار نیست.", "warning");
        return;
    }
    window.pendingEditSupportCategoryId = id;
    adminConnection.invoke("GetSupportCategoryForEdit", id).catch(err => {
        console.error(err);
        window.showToast?.("خطا در دریافت اطلاعات", "error");
    });
}

function fillEditSupportCategoryForm(dto) {
    const template = document.getElementById('editSupportCategoryFormTemplate');
    const content = document.getElementById('editSupportCategoryContent');
    if (!template || !content) return;
    content.innerHTML = template.innerHTML;

    document.getElementById('editSupportCategoryId').value = dto.id;
    document.getElementById('editSupportCategoryName').value = dto.name;
    document.getElementById('editSupportCategoryDetails').value = dto.details;
    document.getElementById('editSupportCategoryIcon').value = dto.iconKey || '';
    document.getElementById('editSupportCategoryIsActive').checked = dto.isActive;

    const usersContainer = document.getElementById('editSupportCategoryUsersList');
    if (usersContainer && dto.users) {
        usersContainer.innerHTML = dto.users.length > 0
            ? dto.users.map(u => `<span class="user-badge" onclick="openUserSupportCategories(${u.userId})" title="ویرایش واحدهای پشتیبانی ${u.fullName}"><i class="fas fa-user-circle"></i> ${u.fullName} (${u.userName})</span>`).join('')
            : '<span class="text-secondary">این واحد هیچ کاربری ندارد.</span>';
    }

    enhanceIconInput('editSupportCategoryIcon');

    document.getElementById('saveEditSupportCategoryBtn')?.addEventListener('click', () => {
        const id = parseInt(document.getElementById('editSupportCategoryId').value);
        const name = document.getElementById('editSupportCategoryName').value.trim();
        const details = document.getElementById('editSupportCategoryDetails').value.trim();
        const iconKey = document.getElementById('editSupportCategoryIcon').value.trim();
        const isActive = document.getElementById('editSupportCategoryIsActive').checked;
        if (!name || !details) { window.showToast?.("نام و توضیحات الزامی است.", "warning"); return; }
        const editDto = { Id: id, Name: name, Details: details, IconKey: iconKey || null, IsActive: isActive };
        if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
            adminConnection.invoke("EditSupportCategory", editDto).catch(err => {
                console.error(err);
                window.showToast?.("خطا در ویرایش", "error");
            });
        }
    });
}

// ========== Icon Dropdown ==========
function enhanceIconInput(inputId) {
    const input = document.getElementById(inputId);
    if (!input) return;
    if (input.dataset.enhanced === 'true') return;
    input.dataset.enhanced = 'true';

    const wrapper = document.createElement('div');
    wrapper.className = 'icon-searchable';
    input.parentNode.insertBefore(wrapper, input);
    wrapper.appendChild(input);
    input.style.display = 'none';

    const currentIconClass = input.value || 'fas fa-tools';
    const selected = document.createElement('div');
    selected.className = 'selected-icon';
    selected.innerHTML = `<i class="${currentIconClass}"></i> <span>${currentIconClass}</span>`;
    wrapper.appendChild(selected);

    const dropdown = document.createElement('div');
    dropdown.className = 'dropdown-list';
    const searchInput = document.createElement('input');
    searchInput.type = 'text';
    searchInput.className = 'search-input';
    searchInput.placeholder = 'جستجوی آیکون...';
    dropdown.appendChild(searchInput);
    const listContainer = document.createElement('div');
    dropdown.appendChild(listContainer);
    wrapper.appendChild(dropdown);

    function loadIcons() {
        if (window.iconMappings) {
            renderIconList(listContainer, input, selected, dropdown);
        } else {
            adminConnection.invoke("GetAllIconMappings")
                .then(data => {
                    window.iconMappings = data;
                    renderIconList(listContainer, input, selected, dropdown);
                })
                .catch(err => {
                    console.warn("⚠️ Could not load icons:", err);
                });
        }
    }

    selected.addEventListener('click', () => {
        dropdown.classList.toggle('open');
        if (dropdown.classList.contains('open')) {
            loadIcons();
            searchInput.focus();
        }
    });

    searchInput.addEventListener('input', () => {
        const term = searchInput.value.trim().toLowerCase();
        const items = listContainer.querySelectorAll('.dropdown-item');
        items.forEach(item => {
            const text = item.textContent.toLowerCase();
            item.style.display = text.includes(term) ? '' : 'none';
        });
    });

    document.addEventListener('click', (e) => {
        if (!wrapper.contains(e.target)) dropdown.classList.remove('open');
    });
}

function renderIconList(container, input, selected, dropdown) {
    if (!window.iconMappings) return;
    container.innerHTML = window.iconMappings.map(icon => `
        <div class="dropdown-item" data-key="${icon.iconKey}">
            <i class="${icon.iconKey}"></i>
            <span>${icon.persianName}</span>
        </div>
    `).join('');
    container.querySelectorAll('.dropdown-item').forEach(item => {
        item.addEventListener('click', () => {
            const key = item.dataset.key;
            input.value = key;
            selected.innerHTML = `<i class="${key}"></i> <span>${key}</span>`;
            dropdown.classList.remove('open');
        });
    });
}

function refreshOpenIconDropdowns() {
    document.querySelectorAll('.icon-searchable .dropdown-list.open').forEach(dropdown => {
        const wrapper = dropdown.closest('.icon-searchable');
        if (!wrapper) return;
        const input = wrapper.querySelector('input[type="text"][style*="display: none"]') ||
            wrapper.querySelector('input.form-control');
        const selected = wrapper.querySelector('.selected-icon');
        const listContainer = dropdown.querySelector('div');
        if (input && selected && listContainer) {
            renderIconList(listContainer, input, selected, dropdown);
        }
    });
}

// ========== USER SUPPORT CATEGORIES (Assignment) ==========
function openUserSupportCategories(userId) {
    loadPage('users-supportcategories', true, { userId });
}

let oldSupportCardPositions = new Map();
function captureSupportCardPositions() {
    oldSupportCardPositions.clear();
    const cards = document.querySelectorAll('#userSupportCategoriesCardsContainer .support-card');
    cards.forEach(card => {
        const id = parseInt(card.dataset.id);
        if (!isNaN(id)) oldSupportCardPositions.set(id, card.getBoundingClientRect());
    });
}

function animateSupportCardsAfterFilter() {
    const container = document.getElementById('userSupportCategoriesCardsContainer');
    if (!container) return;
    const newCards = container.querySelectorAll('.support-card');
    newCards.forEach(card => {
        const id = parseInt(card.dataset.id);
        if (isNaN(id)) return;
        const oldRect = oldSupportCardPositions.get(id);
        if (oldRect) {
            const newRect = card.getBoundingClientRect();
            const deltaX = oldRect.left - newRect.left;
            const deltaY = oldRect.top - newRect.top;
            if (deltaX !== 0 || deltaY !== 0) {
                card.style.transition = 'none';
                card.style.transform = `translate(${deltaX}px, ${deltaY}px)`;
                requestAnimationFrame(() => {
                    card.style.transition = 'transform 0.4s cubic-bezier(0.25, 0.8, 0.25, 1.2)';
                    card.style.transform = '';
                });
            }
        } else {
            card.classList.add('fade-enter');
            requestAnimationFrame(() => {
                card.classList.add('fade-enter-active');
                card.addEventListener('transitionend', () => card.classList.remove('fade-enter', 'fade-enter-active'), { once: true });
            });
        }
    });
}

function renderUserSupportCategoryCards(categories, selectedIds) {
    const container = document.getElementById('userSupportCategoriesCardsContainer');
    if (!container) return;
    const searchInput = document.getElementById('supportCategorySearchInputForUser');
    let filtered = categories;
    if (searchInput) {
        const term = searchInput.value.trim().toLowerCase();
        if (term) filtered = categories.filter(c => c.name.toLowerCase().includes(term));
    }
    captureSupportCardPositions();

    container.innerHTML = filtered.map(c => {
        const isSelected = selectedIds.includes(c.id);
        const iconHtml = c.iconClassName ? `<i class="${c.iconClassName} me-2"></i>` : '';
        const inactiveBadge = (c.isActive !== undefined && !c.isActive)
            ? '<span class="inactive-badge">این واحد غیرفعال است</span>'
            : '';
        return `
            <div class="support-card ${isSelected ? 'selected' : ''}" data-id="${c.id}">
                <div class="support-card-header">
                    <div>
                        <div class="support-card-title">${iconHtml}${c.name}</div>
                        <div class="support-card-details">${c.details || ''}</div>
                        ${inactiveBadge}
                    </div>
                    <span class="status-icon unchecked"><i class="far fa-circle"></i></span>
                    <span class="status-icon checked"><i class="fas fa-check-circle"></i></span>
                </div>
            </div>
        `;
    }).join('');

    container.querySelectorAll('.support-card').forEach(card => {
        card.addEventListener('click', function () {
            const catId = parseInt(this.dataset.id);
            const idx = window.currentUserSupportCategoryInfo.supportCategoryIdList.indexOf(catId);
            if (idx > -1) {
                window.currentUserSupportCategoryInfo.supportCategoryIdList.splice(idx, 1);
                this.classList.remove('selected');
            } else {
                window.currentUserSupportCategoryInfo.supportCategoryIdList.push(catId);
                this.classList.add('selected');
            }
        });
    });

    animateSupportCardsAfterFilter();
}

let userSupportCategorySearchTimer = null;
function onUserSupportCategorySearch() {
    clearTimeout(userSupportCategorySearchTimer);
    userSupportCategorySearchTimer = setTimeout(() => {
        renderUserSupportCategoryCards(window.allSupportCategoriesForAssign, window.currentUserSupportCategoryInfo.supportCategoryIdList);
    }, 250);
}

function setupUserSupportCategorySearch() {
    const searchInput = document.getElementById('supportCategorySearchInputForUser');
    if (!searchInput) return;
    searchInput.removeEventListener('input', onUserSupportCategorySearch);
    searchInput.addEventListener('input', onUserSupportCategorySearch);
}

// ========== USERS ==========
function buildUserSearch() {
    const username = document.getElementById('userSearchUsername')?.value.trim();
    const firstName = document.getElementById('userSearchFirstName')?.value.trim();
    const lastName = document.getElementById('userSearchLastName')?.value.trim();
    const banSelect = document.getElementById('userBanFilter');
    const banVal = banSelect?.value;

    if (!username && !firstName && !lastName && banVal === '') return null;

    const search = {};
    if (username) search.UserName = username;
    if (firstName) search.FirstName = firstName;
    if (lastName) search.LastName = lastName;
    if (banVal !== '') search.IsBanned = banVal === 'true' ? true : (banVal === 'false' ? false : null);
    return search;
}

function fetchUserPage() {
    if (!adminConnection || adminConnection.state !== signalR.HubConnectionState.Connected) return;
    const search = buildUserSearch();
    userState.search = search;
    adminConnection.invoke("GetUsers", userState.paging, search);
}

function changeUserPage(pageNum) {
    userState.paging.PageNumber = pageNum;
    fetchUserPage();
}

function mapSortField(field) {
    const map = { 'Id': 'id', 'UserName': 'userName', 'FirstName': 'firstName', 'LastName': 'lastName' };
    return map[field] || field;
}

function applyUserSort() {
    if (!userState.sortField) return;
    const field = mapSortField(userState.sortField);
    const asc = userState.sortAsc;
    userState.items.sort((a, b) => {
        let valA = a[field];
        let valB = b[field];
        if (typeof valA === 'string') valA = valA.toLowerCase();
        if (typeof valB === 'string') valB = valB.toLowerCase();
        if (valA < valB) return asc ? -1 : 1;
        if (valA > valB) return asc ? 1 : -1;
        return 0;
    });
}

function renderUsersTable(users) {
    const tbody = document.getElementById('usersTableBody');
    if (!tbody) return;
    if (!users || users.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center">کاربری یافت نشد</td></tr>';
        return;
    }
    tbody.innerHTML = users.map(u => `<tr data-user-id="${u.id}">
        <td>${u.id}</td>
        <td>${u.userName}</td>
        <td>${u.firstName || '-'}</td>
        <td>${u.lastName || '-'}</td>
        <td>${u.isBanned ? '<span class="badge badge-danger">بن شده</span>' : '<span class="badge badge-success">فعال</span>'}</td>
        <td>${u.roleCount}</td>
        <td>
            <button class="btn btn-sm btn-outline-info" onclick="navigateToEditRole(${u.id})"><i class="fas fa-edit"></i></button>
            <button class="btn btn-sm btn-outline-warning" onclick="openBanSlideover(${u.id}, ${u.isBanned})" title="مدیریت مسدودیت"><i class="fas fa-gavel"></i></button>
            <button class="btn btn-sm btn-outline-primary" onclick="navigateToUserRoles(${u.id})" title="نقش‌های کاربر"><i class="fas fa-user-shield"></i></button>
            <button class="btn btn-sm btn-outline-secondary" onclick="openUserSupportCategories(${u.id})" title="واحدهای پشتیبانی"><i class="fas fa-headset"></i></button>
        </td>
    </tr>`).join('');
}

function renderPagination() {
    const container = document.getElementById('usersPagination');
    if (!container) return;

    const totalPages = Math.ceil(userState.totalCount / userState.paging.PageSize);
    if (totalPages <= 1 && userState.paging.PageSize >= userState.totalCount) {
        container.innerHTML = '';
        return;
    }

    const current = userState.paging.PageNumber;
    const maxVisible = 5;

    let html = '<div class="d-flex align-items-center gap-2 flex-wrap">';
    html += `<button class="page-btn" onclick="changeUserPage(${current - 1})" ${current === 1 ? 'disabled' : ''}>« قبلی</button>`;

    let startPage = Math.max(1, current - Math.floor(maxVisible / 2));
    let endPage = Math.min(totalPages, startPage + maxVisible - 1);
    if (endPage - startPage + 1 < maxVisible) {
        startPage = Math.max(1, endPage - maxVisible + 1);
    }

    if (startPage > 1) {
        html += `<button class="page-btn" onclick="changeUserPage(1)">1</button>`;
        if (startPage > 2) html += `<span class="page-ellipsis">…</span>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        html += `<button class="page-btn ${i === current ? 'active' : ''}" onclick="changeUserPage(${i})">${i}</button>`;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) html += `<span class="page-ellipsis">…</span>`;
        html += `<button class="page-btn" onclick="changeUserPage(${totalPages})">${totalPages}</button>`;
    }

    html += `<button class="page-btn" onclick="changeUserPage(${current + 1})" ${current === totalPages ? 'disabled' : ''}>بعدی »</button>`;

    html += `
        <span class="page-jump">
            <input type="number" id="pageJumpInput" class="page-jump-input" min="1" max="${totalPages}" placeholder="صفحه">
            <button class="page-btn page-jump-btn" onclick="jumpToPage()"><i class="fas fa-location-arrow"></i></button>
        </span>
    `;

    html += `
        <select id="pageSizeSelect" class="page-size-select" onchange="changePageSize(this.value)">
            <option value="5" ${userState.paging.PageSize === 5 ? 'selected' : ''}>۵</option>
            <option value="10" ${userState.paging.PageSize === 10 ? 'selected' : ''}>۱۰</option>
            <option value="20" ${userState.paging.PageSize === 20 ? 'selected' : ''}>۲۰</option>
            <option value="50" ${userState.paging.PageSize === 50 ? 'selected' : ''}>۵۰</option>
        </select>
    `;
    html += '</div>';
    container.innerHTML = html;
}

function changePageSize(size) {
    userState.paging.PageSize = parseInt(size);
    userState.paging.PageNumber = 1;
    fetchUserPage();
}

function jumpToPage() {
    const input = document.getElementById('pageJumpInput');
    if (!input) return;
    let page = parseInt(input.value);
    const totalPages = Math.ceil(userState.totalCount / userState.paging.PageSize);
    if (isNaN(page) || page < 1) page = 1;
    if (page > totalPages) page = totalPages;
    changeUserPage(page);
}

// ========== USERS EVENTS ==========
function setupUserEvents() {
    const usernameInput = document.getElementById('userSearchUsername');
    const firstNameInput = document.getElementById('userSearchFirstName');
    const lastNameInput = document.getElementById('userSearchLastName');
    const banFilter = document.getElementById('userBanFilter');
    const clearBtn = document.getElementById('clearUserSearchBtn');

    let debounceTimer;

    function onFilterChange() {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            userState.paging.PageNumber = 1;
            fetchUserPage();
        }, 350);
    }

    usernameInput?.addEventListener('input', onFilterChange);
    firstNameInput?.addEventListener('input', onFilterChange);
    lastNameInput?.addEventListener('input', onFilterChange);
    banFilter?.addEventListener('change', onFilterChange);

    clearBtn?.addEventListener('click', () => {
        if (usernameInput) usernameInput.value = '';
        if (firstNameInput) firstNameInput.value = '';
        if (lastNameInput) lastNameInput.value = '';
        if (banFilter) banFilter.value = '';
        userState.paging.PageNumber = 1;
        fetchUserPage();
    });
}

// ========== ROLES CRUD ==========
function safeLoadEditRoleData(roleId) {
    if (!adminConnection || adminConnection.state !== signalR.HubConnectionState.Connected) {
        window.showToast?.("ارتباط با سرور برقرار نیست.", "warning");
        return;
    }
    if (!window.allPermissions) {
        adminConnection.invoke("GetPermissions").catch(() => { });
    }
    adminConnection.invoke("GetRoleForEdit", roleId).catch(err => {
        console.error("Error invoking GetRoleForEdit:", err);
        window.showToast?.("خطا در دریافت اطلاعات نقش", "error");
    });
}

function loadDeleteRoleData(roleId) {
    const loading = document.getElementById('deleteRoleLoading');
    const confirmation = document.getElementById('deleteRoleConfirmation');
    if (loading) loading.style.display = 'block';
    if (confirmation) confirmation.style.display = 'none';
    document.getElementById('deleteRoleId').value = roleId;

    if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
        adminConnection.invoke("GetRoleForDelete", roleId).catch(err => {
            console.error(err);
            window.showToast?.("خطا در دریافت اطلاعات نقش", "error");
        });
    } else {
        window.showToast?.("ارتباط با سرور برقرار نیست.", "warning");
    }
}

function populateDeleteConfirmation(role) {
    const loading = document.getElementById('deleteRoleLoading');
    const confirmation = document.getElementById('deleteRoleConfirmation');
    if (loading) loading.style.display = 'none';
    if (confirmation) confirmation.style.display = 'block';

    document.getElementById('deleteRoleName').textContent = role.name;
    document.getElementById('deleteRoleUserCount').textContent = role.userHaveThisRoleCount ?? 0;

    const generalPerms = role.permissions?.filter(p => p.category === 'General') || [];
    const notificationPerms = role.permissions?.filter(p => p.category === 'Notification') || [];

    const generalContainer = document.getElementById('deleteGeneralPermissions');
    const generalEmpty = document.getElementById('deleteGeneralEmpty');
    const notifContainer = document.getElementById('deleteNotificationPermissions');
    const notifEmpty = document.getElementById('deleteNotificationEmpty');

    if (generalContainer) {
        if (generalPerms.length > 0) {
            generalContainer.innerHTML = generalPerms.map(p => `<span class="perm-badge">${p.name}</span>`).join('');
            generalContainer.style.display = 'flex';
        } else {
            generalContainer.innerHTML = '';
            generalContainer.style.display = 'none';
        }
    }
    if (generalEmpty) generalEmpty.style.display = generalPerms.length === 0 ? 'block' : 'none';

    if (notifContainer) {
        if (notificationPerms.length > 0) {
            notifContainer.innerHTML = notificationPerms.map(p => `<span class="perm-badge">${p.name}</span>`).join('');
            notifContainer.style.display = 'flex';
        } else {
            notifContainer.innerHTML = '';
            notifContainer.style.display = 'none';
        }
    }
    if (notifEmpty) notifEmpty.style.display = notificationPerms.length === 0 ? 'block' : 'none';

    const confirmBtn = document.getElementById('confirmDeleteRoleBtn');
    if (confirmBtn) {
        const newBtn = confirmBtn.cloneNode(true);
        confirmBtn.parentNode.replaceChild(newBtn, confirmBtn);
        newBtn.addEventListener('click', () => {
            const roleId = parseInt(document.getElementById('deleteRoleId').value);
            if (!roleId) return;
            if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
                adminConnection.invoke("DeleteRole", roleId)
                    .catch(err => {
                        console.error(err);
                        window.showToast?.("خطا در حذف نقش", "error");
                    });
            } else {
                window.showToast?.("ارتباط با سرور برقرار نیست.", "warning");
            }
        });
    }
}

function loadAddRoleData() {
    if (window.allPermissions) {
        fillAddPermissions();
    } else if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
        adminConnection.invoke("GetPermissions").catch(() => { });
    }
    document.getElementById('saveAddRoleBtn')?.addEventListener('click', () => {
        const name = document.getElementById('addRoleName').value.trim();
        if (!name) {
            window.showToast?.("نام نقش الزامی است.", "warning");
            return;
        }
        const selected = Array.from(document.querySelectorAll('#addGeneralPermissions .perm-check:checked, #addNotificationPermissions .perm-check:checked'))
            .map(cb => parseInt(cb.value));
        if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
            adminConnection.invoke("AddRole", { Name: name, PermissionIdList: selected })
                .catch(err => {
                    console.error(err);
                    window.showToast?.("خطا در ذخیره نقش", "error");
                });
        } else {
            window.showToast?.("ارتباط با سرور برقرار نیست.", "warning");
        }
    });
}

function fillAddPermissions() {
    const genContainer = document.getElementById('addGeneralPermissions');
    const notContainer = document.getElementById('addNotificationPermissions');
    if (!genContainer || !notContainer || !window.allPermissions) return;

    const general = window.allPermissions.filter(p => p.category === 'General');
    const notification = window.allPermissions.filter(p => p.category === 'Notification');

    genContainer.innerHTML = general.map(p => `
        <label class="permission-card">
            <input type="checkbox" value="${p.id}" class="perm-check">
            <span class="permission-card-content">
                <i class="fas fa-shield-alt"></i>
                <span class="perm-name">${p.name}</span>
            </span>
            <span class="status-icon unchecked"><i class="far fa-circle"></i></span>
            <span class="status-icon checked"><i class="fas fa-check-circle"></i></span>
        </label>
    `).join('');

    notContainer.innerHTML = notification.map(p => `
        <label class="permission-card">
            <input type="checkbox" value="${p.id}" class="perm-check">
            <span class="permission-card-content">
                <i class="fas fa-bell"></i>
                <span class="perm-name">${p.name}</span>
            </span>
            <span class="status-icon unchecked"><i class="far fa-circle"></i></span>
            <span class="status-icon checked"><i class="fas fa-check-circle"></i></span>
        </label>
    `).join('');
}

function fillEditForm(role) {
    const template = document.getElementById('roleEditFormTemplate');
    const content = document.getElementById('roleEditContent');
    if (!template || !content) return;

    content.innerHTML = template.innerHTML;
    document.getElementById('editRoleId').value = role.id;
    document.getElementById('editRoleName').value = role.name;

    const container = document.getElementById('editPermissionsCheckboxes');
    if (container && window.allPermissions) {
        const gen = window.allPermissions.filter(p => p.category === 'General');
        const not = window.allPermissions.filter(p => p.category === 'Notification');
        const selectedIds = role.permissions.map(p => p.id);
        container.innerHTML = `
            <div class="glass-card mb-3">
                <div class="permission-group-title">
                    <i class="fas fa-cogs"></i> مجوزهای مدیریتی
                </div>
                <div class="permission-grid">
                    ${gen.map(p => `
                        <label class="permission-card">
                            <input type="checkbox" value="${p.id}" class="perm-check" ${selectedIds.includes(p.id) ? 'checked' : ''}>
                            <span class="permission-card-content">
                                <i class="fas fa-shield-alt"></i>
                                <span class="perm-name">${p.name}</span>
                            </span>
                            <span class="status-icon unchecked"><i class="far fa-circle"></i></span>
                            <span class="status-icon checked"><i class="fas fa-check-circle"></i></span>
                        </label>
                    `).join('')}
                </div>
            </div>
            <div class="glass-card mb-3">
                <div class="permission-group-title">
                    <i class="fas fa-bell"></i> مجوزهای نوتیفیکیشن
                </div>
                <div class="permission-grid">
                    ${not.map(p => `
                        <label class="permission-card">
                            <input type="checkbox" value="${p.id}" class="perm-check" ${selectedIds.includes(p.id) ? 'checked' : ''}>
                            <span class="permission-card-content">
                                <i class="fas fa-bell"></i>
                                <span class="perm-name">${p.name}</span>
                            </span>
                            <span class="status-icon unchecked"><i class="far fa-circle"></i></span>
                            <span class="status-icon checked"><i class="fas fa-check-circle"></i></span>
                        </label>
                    `).join('')}
                </div>
            </div>
        `;

        document.getElementById('saveEditRoleBtn')?.addEventListener('click', () => {
            const id = parseInt(document.getElementById('editRoleId').value);
            const name = document.getElementById('editRoleName').value.trim();
            const permissions = Array.from(document.querySelectorAll('.perm-check:checked')).map(cb => parseInt(cb.value));
            if (!name) {
                window.showToast?.("نام نقش الزامی است.", "warning");
                return;
            }
            if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
                adminConnection.invoke("EditRole", { Id: id, Name: name, PermissionIdList: permissions })
                    .catch(err => {
                        console.error(err);
                        window.showToast?.("خطا در ویرایش نقش", "error");
                    });
            } else {
                window.showToast?.("ارتباط با سرور برقرار نیست.", "warning");
            }
        });
    }
}

// ========== RENDER ROLES TABLE ==========
function renderRolesTable(roles) {
    const tbody = document.getElementById('rolesTableBody');
    if (!tbody) return;
    tbody.innerHTML = roles?.length ? roles.map(r => `<tr>
        <td>${r.id}</td>
        <td>${r.name}</td>
        <td>${r.permissionsCount}</td>
        <td>${r.userHaveThisRoleCount}</td>
        <td>
            <button class="btn btn-sm btn-outline-info" onclick="navigateToEditRole(${r.id})"><i class="fas fa-edit"></i></button>
            <button class="btn btn-sm btn-outline-danger" onclick="navigateToDeleteRole(${r.id})"><i class="fas fa-trash"></i></button>
        </td>
    </tr>`).join('') : '<tr><td colspan="5" class="text-center">نقشی یافت نشد</td></tr>';
}

// ========== SPA PAGE LOAD ==========
async function loadPage(page, pushToHistory = true, params = {}) {
    const mainContent = document.getElementById('mainContent');
    if (!mainContent) return;

    if (currentPage === 'chat' && page !== 'chat') {
        document.body.classList.remove('admin-chat-active');
        removeChatToggleFromTopbar();
        const stack = document.getElementById('notificationStack');
        if (stack) stack.remove();
        disconnectChatHub();
        removeDragAndDrop();
        bagElement = null;
        bagListElement = null;
        // Remove expand arrow if exists
        document.getElementById('chatSidebarExpandArrow')?.remove();
    }

    currentPage = page;

    const pageTitles = {
        'roles-add': 'افزودن نقش',
        'roles-edit': 'ویرایش نقش',
        'roles-delete': 'حذف نقش',
        'users-roles': 'نقش‌های کاربر',
        'users-supportcategories': 'واحدهای پشتیبانی کاربر',
        'categories-add': 'افزودن واحد پشتیبانی',
        'categories-edit': 'ویرایش واحد پشتیبانی'
    };

    if (pageTitles[page]) {
        document.getElementById('pageTitle').textContent = pageTitles[page];
        document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
    } else if (page !== 'roles-edit' && page !== 'roles-add' && page !== 'roles-delete' && page !== 'users-roles' && page !== 'users-supportcategories') {
        document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
        const activeNav = document.querySelector(`.nav-item[data-page="${page}"]`);
        if (activeNav) {
            activeNav.classList.add('active');
            document.getElementById('pageTitle').textContent = activeNav.querySelector('span').textContent;
        }
    }

    mainContent.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-primary"></div></div>';

    const urls = {
        dashboard: '/Admin/Dashboard',
        users: '/Admin/Users/Partial',
        roles: '/Admin/Roles/Partial',
        'roles-add': '/Admin/Roles/Add/Partial',
        'roles-edit': (id) => `/Admin/Roles/Edit/${id}`,
        'roles-delete': (id) => `/Admin/Roles/Delete/${id}`,
        'users-roles': (userId) => `/Admin/Users/Roles/${userId}`,
        'users-supportcategories': (userId) => `/Admin/Users/SupportCategory/${userId}`,
        categories: '/Admin/SupportCategories/Partial',
        'categories-add': '/Admin/SupportCategories/Add/Partial',
        'categories-edit': (id) => `/Admin/SupportCategories/Edit/${id}`,
        chat: '/Admin/Chats/Partial',
        notifications: '/Admin/Notifications/Partial',
        settings: '/Admin/Settings/Partial'
    };

    const url = typeof urls[page] === 'function' ? urls[page](params.userId || params.roleId || params.id) : urls[page];

    try {
        const res = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const html = await res.text();

        const wrapper = document.createElement('div');
        wrapper.innerHTML = html;
        wrapper.classList.add('animate-fade-in-up');
        mainContent.innerHTML = '';
        mainContent.appendChild(wrapper);

        if (pushToHistory) {
            let newUrl;
            if (page === 'roles-edit') newUrl = `/Admin/Roles/Edit/${params.roleId}`;
            else if (page === 'roles-delete') newUrl = `/Admin/Roles/Delete/${params.roleId}`;
            else if (page === 'users-roles') newUrl = `/Admin/Users/Roles/${params.userId}`;
            else if (page === 'users-supportcategories') newUrl = `/Admin/Users/SupportCategory/${params.userId}`;
            else if (page === 'categories-edit') newUrl = `/Admin/SupportCategories/Edit/${params.id}`;
            else if (page === 'chat') newUrl = '/Admin/Chats';
            else newUrl = pageRoutes[page] || '/Admin';
            history.pushState({ page, params }, '', newUrl);
        }

        if (page === 'roles') {
            if (filterModalInstance) { filterModalInstance.hide(); filterModalInstance.dispose(); filterModalInstance = null; }
            document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
            if (adminConnection?.state === signalR.HubConnectionState.Connected) adminConnection.invoke("GetRoles");
            document.getElementById('addRoleBtn')?.addEventListener('click', () => loadPage('roles-add'));
            setupSearchAndFilter();
        } else if (page === 'roles-edit') safeLoadEditRoleData(params.roleId);
        else if (page === 'roles-add') loadAddRoleData();
        else if (page === 'roles-delete') loadDeleteRoleData(params.roleId);
        else if (page === 'users') {
            document.getElementById('userSearchUsername')?.value && (document.getElementById('userSearchUsername').value = '');
            document.getElementById('userSearchFirstName')?.value && (document.getElementById('userSearchFirstName').value = '');
            document.getElementById('userSearchLastName')?.value && (document.getElementById('userSearchLastName').value = '');
            document.getElementById('userBanFilter')?.value && (document.getElementById('userBanFilter').value = '');
            userState.paging.PageNumber = 1;
            setupUserEvents();
            fetchUserPage();
        } else if (page === 'users-roles') {
            window.allRoles = null;
            window.currentUserInfo = null;
            if (adminConnection?.state === signalR.HubConnectionState.Connected) {
                adminConnection.invoke("GetRolesForAssignToUser");
                adminConnection.invoke("GetUserRolesAsync", params.userId);
            }
            setupUserRoleSearch();
        } else if (page === 'users-supportcategories') {
            window.allSupportCategoriesForAssign = null;
            window.currentUserSupportCategoryInfo = null;
            if (adminConnection?.state === signalR.HubConnectionState.Connected) {
                adminConnection.invoke("GetSupportCategoriesForAssignToUser");
                adminConnection.invoke("GetUserSupportCategories", params.userId);
            }
            setupUserSupportCategorySearch();
        } else if (page === 'categories') {
            if (adminConnection?.state === signalR.HubConnectionState.Connected) adminConnection.invoke("GetSupportCategories");
            setupSupportCategorySearch();
        } else if (page === 'categories-add') {
            loadAddSupportCategoryData();
            if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
                adminConnection.invoke("GetAllIconMappings").catch(err => {
                    console.warn("⚠️ Preload icons on add page failed:", err);
                });
            }
        } else if (page === 'categories-edit') {
            safeLoadEditSupportCategoryData(params.id);
        } else if (page === 'chat') {
            document.body.classList.add('admin-chat-active');
            document.body.classList.remove('sidebar-open');
            setupChatFilterToggle();
            setupDragAndDrop();
            setupChatSidebar(); // directly set up sidebar (minimize & arrow)

            document.getElementById('notificationStack')?.remove();
            const notifStack = document.createElement('div');
            notifStack.id = 'notificationStack';
            notifStack.className = 'notification-stack';
            document.body.appendChild(notifStack);

            await connectChatHub();
        }
    } catch (err) {
        console.error(err);
        mainContent.innerHTML = '<p class="text-center text-danger">خطا در بارگذاری.</p>';
    }
}

// ========== ROLES SEARCH & FILTER ==========
function performSearch() {
    if (!adminConnection || adminConnection.state !== signalR.HubConnectionState.Connected) return;
    if (!currentSearch.roleName && currentSearch.permissionIds.length === 0) {
        adminConnection.invoke("GetRoles");
        return;
    }
    adminConnection.invoke("SearchInRoles", {
        RoleName: currentSearch.roleName || null,
        PermissionsIdList: currentSearch.permissionIds.length > 0 ? currentSearch.permissionIds : null
    });
}

function loadCannedResponses() {
    const stored = localStorage.getItem(CANNED_STORAGE_KEY);
    if (stored) {
        try {
            cannedResponses = JSON.parse(stored);
            if (!cannedResponses.length) setDefaultCanned();
        } catch (e) {
            setDefaultCanned();
        }
    } else {
        setDefaultCanned();
    }
}

function setDefaultCanned() {
    cannedResponses = [
        {
            id: 1,
            title: 'خوش‌آمدگویی',
            text: 'سلام $username عزیز، به پشتیبانی خوش آمدید.\nچطور می‌توانم کمک کنم؟'
        },
        {
            id: 2,
            title: 'پایان گفتگو',
            text: '$username گرامی، از اینکه با ما همراه بودید سپاسگزاریم.\nاگر سوال دیگری داشتید در خدمتیم.'
        },
        {
            id: 3,
            title: 'اطلاع‌رسانی ساعات کاری',
            text: '$username توجه داشته باشید که ساعات پشتیبانی از ۹ صبح تا ۶ عصر می‌باشد.\n$adminname'
        }
    ];
    saveCannedResponses();
}

function saveCannedResponses() {
    localStorage.setItem(CANNED_STORAGE_KEY, JSON.stringify(cannedResponses));
}

function showCannedForm(mode, id) {
    const list = document.getElementById('cannedList');
    const formContainer = document.getElementById('cannedFormContainer');
    if (!list || !formContainer) return;

    hideCannedForm();

    let title = '', text = '';
    if (mode === 'edit') {
        const resp = cannedResponses.find(r => r.id === id);
        if (resp) { title = resp.title; text = resp.text; }
    }

    const formDiv = document.createElement('div');
    formDiv.id = 'cannedEditForm';
    formDiv.className = 'canned-form';
    formDiv.innerHTML = `
        <input type="text" id="cannedTitle" placeholder="عنوان" value="${escapeHtml(title)}">
   <textarea id="cannedText" rows="3" placeholder="متن (می‌توانید از $username و $adminname استفاده کنید)">${escapeHtml(text)}</textarea>
        <div class="canned-form-buttons">
            <button class="btn btn-sm btn-outline-light" onclick="hideCannedForm()">انصراف</button>
            <button class="btn btn-sm btn-primary" onclick="saveCannedForm('${mode}', ${id})">ذخیره</button>
        </div>
    `;

    list.parentNode.insertBefore(formDiv, list);

    setTimeout(() => document.getElementById('cannedTitle')?.focus(), 50);
}

function hideCannedForm() {
    const form = document.getElementById('cannedEditForm');
    if (form) form.remove();
}

function saveCannedForm(mode, id) {
    const title = document.getElementById('cannedTitle').value.trim();
    const text = document.getElementById('cannedText').value.trim();
    if (!title || !text) {
        window.showToast?.("عنوان و متن الزامی است.", "warning");
        return;
    }

    if (mode === 'add') {
        const newId = cannedResponses.length > 0 ? Math.max(...cannedResponses.map(r => r.id)) + 1 : 1;
        cannedResponses.push({ id: newId, title, text });
    } else if (mode === 'edit') {
        const resp = cannedResponses.find(r => r.id === id);
        if (resp) { resp.title = title; resp.text = text; }
    }

    saveCannedResponses();
    hideCannedForm();
    refreshCannedList();
}

function refreshCannedList() {
    const list = document.getElementById('cannedList');
    if (!list) return;

    let filtered = cannedResponses;
    if (cannedSearchTerm) {
        filtered = cannedResponses.filter(r =>
            r.title.toLowerCase().includes(cannedSearchTerm) ||
            r.text.toLowerCase().includes(cannedSearchTerm)
        );
    }

    if (filtered.length === 0) {
        list.innerHTML = '<div class="text-center text-secondary py-3">پاسخی یافت نشد</div>';
        return;
    }

    list.innerHTML = filtered.map(r => `
        <div class="canned-item" data-id="${r.id}">
            <div class="canned-text">
                <strong>${escapeHtml(r.title)}</strong>
                <small>${escapeHtml(r.text.substring(0, 50))}...</small>
            </div>
            <div class="canned-actions">
                <button class="minimal-btn canned-send" onclick="useCannedResponse(${r.id})" title="ارسال">
                    <i class="fas fa-paper-plane"></i>
                </button>
                <button class="minimal-btn" onclick="showCannedForm('edit', ${r.id})" title="ویرایش">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="minimal-btn" onclick="deleteCannedResponse(${r.id})" title="حذف">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        </div>
    `).join('');
}

function deleteCannedResponse(id) {
    const panel = document.getElementById('cannedPanel');
    if (!panel) return;

    const existingOverlay = panel.querySelector('.canned-confirm-overlay');
    if (existingOverlay) existingOverlay.remove();

    const confirmDiv = document.createElement('div');
    confirmDiv.className = 'canned-confirm-overlay';
    confirmDiv.innerHTML = `
        <div class="canned-confirm-box">
            <p>آیا از حذف این پاسخ مطمئن هستید؟</p>
            <div class="canned-confirm-actions">
                <button class="btn btn-sm btn-outline-light" id="cancelDelete">انصراف</button>
                <button class="btn btn-sm btn-danger" id="confirmDelete">حذف</button>
            </div>
        </div>
    `;
    panel.appendChild(confirmDiv);

    document.getElementById('cancelDelete').addEventListener('click', () => confirmDiv.remove());
    document.getElementById('confirmDelete').addEventListener('click', () => {
        cannedResponses = cannedResponses.filter(r => r.id !== id);
        saveCannedResponses();
        refreshCannedList();
        confirmDiv.remove();
    });
}

function openCannedPanel() {
    const existing = document.getElementById('cannedPanel');
    if (existing) {
        existing.remove();
        return;
    }

    const panel = document.createElement('div');
    panel.id = 'cannedPanel';
    panel.className = 'canned-panel glass-card';
    panel.innerHTML = `
        <div class="canned-panel-header" id="cannedPanelHeader">
            <h5><i class="fas fa-clipboard-list"></i> پاسخ‌های آماده</h5>
            <button class="btn-icon" id="closeCannedPanel"><i class="fas fa-times"></i></button>
        </div>
        <div class="canned-search">
            <input type="text" id="cannedSearchInput" placeholder="جستجوی پاسخ...">
            <i class="fas fa-search"></i>
        </div>
        <div class="canned-list" id="cannedList"></div>
        <div id="cannedFormContainer"></div>
        <button class="btn-add-canned" id="addCannedBtn">
            <i class="fas fa-plus"></i> افزودن پاسخ جدید
        </button>
    `;

    document.body.appendChild(panel);

    if (cannedPanelPos) {
        panel.style.left = cannedPanelPos.left + 'px';
        panel.style.top = cannedPanelPos.top + 'px';
        panel.style.bottom = 'auto';
    } else {
        panel.style.left = '20px';
        panel.style.bottom = '80px';
    }

    document.getElementById('closeCannedPanel').addEventListener('click', () => panel.remove());

    refreshCannedList();

    document.getElementById('cannedSearchInput').addEventListener('input', (e) => {
        cannedSearchTerm = e.target.value.trim().toLowerCase();
        refreshCannedList();
    });

    document.getElementById('addCannedBtn').addEventListener('click', () => {
        showCannedForm('add');
    });

    // ---------- Draggable with persistence ----------
    const header = document.getElementById('cannedPanelHeader');
    header.addEventListener('mousedown', (e) => {
        cannedDragState = {
            startX: e.clientX,
            startY: e.clientY,
            startLeft: panel.offsetLeft,
            startTop: panel.offsetTop
        };
        document.addEventListener('mousemove', onCannedDrag);
        document.addEventListener('mouseup', onCannedDragEnd);
    });

    function onCannedDrag(e) {
        if (!cannedDragState) return;
        const dx = e.clientX - cannedDragState.startX;
        const dy = e.clientY - cannedDragState.startY;
        panel.style.left = (cannedDragState.startLeft + dx) + 'px';
        panel.style.top = (cannedDragState.startTop + dy) + 'px';
        panel.style.bottom = 'auto';
    }

    function onCannedDragEnd() {
        if (cannedDragState) {
            cannedPanelPos = {
                left: panel.offsetLeft,
                top: panel.offsetTop
            };
            localStorage.setItem('cannedPanelPos', JSON.stringify(cannedPanelPos));
        }
        cannedDragState = null;
        document.removeEventListener('mousemove', onCannedDrag);
        document.removeEventListener('mouseup', onCannedDragEnd);
    }
}

function useCannedResponse(id) {
    const resp = cannedResponses.find(r => r.id === id);
    if (!resp) return;

    let text = resp.text;

    if (currentChatData) {
        const fullName = `${currentChatData.creatorFirstName} ${currentChatData.creatorLastName}`;
        text = text.replace(/\$username/g, fullName);
    }

    if (currentAdminUserName) {
        text = text.replace(/\$adminname/g, currentAdminUserName);
    }

    const input = document.getElementById('adminMessageInput');
    if (input) {
        input.value = text;
        input.focus();
    }

    const panel = document.getElementById('cannedPanel');
    if (panel) panel.remove();
}

function addNewCannedResponse() {
    const title = prompt('عنوان پاسخ:');
    if (!title) return;
    const text = prompt('متن پاسخ (می‌توانید از $name استفاده کنید):');
    if (!text) return;

    const newId = cannedResponses.length > 0 ? Math.max(...cannedResponses.map(r => r.id)) + 1 : 1;
    cannedResponses.push({ id: newId, title, text });
    saveCannedResponses();
    openCannedPanel();
}

function editCannedResponse(id) {
    const resp = cannedResponses.find(r => r.id === id);
    if (!resp) return;

    const title = prompt('عنوان جدید:', resp.title);
    if (title === null) return;
    const text = prompt('متن جدید:', resp.text);
    if (text === null) return;

    resp.title = title;
    resp.text = text;
    saveCannedResponses();
    openCannedPanel();
}

function setupSearchAndFilter() {
    if (filterModalInstance) {
        try { filterModalInstance.hide(); filterModalInstance.dispose(); } catch (e) { }
        filterModalInstance = null;
    }
    document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());

    const searchInput = document.getElementById('roleSearchInput');
    const filterBtn = document.getElementById('roleFilterBtn');
    const clearFilterBtn = document.getElementById('clearFilterBtn');
    const applyBtn = document.getElementById('applyPermissionFilterBtn');
    const resetBtn = document.getElementById('resetPermissionFilterBtn');
    const permissionFilterModal = document.getElementById('permissionFilterModal');

    if (!permissionFilterModal) return;
    filterModalInstance = new bootstrap.Modal(permissionFilterModal);
    permissionFilterModal.addEventListener('hidden.bs.modal', () => {
        document.querySelector('.modal-backdrop')?.remove();
    });

    searchInput?.addEventListener('input', () => {
        currentSearch.roleName = searchInput.value.trim();
        clearTimeout(searchDebounceTimer);
        searchDebounceTimer = setTimeout(performSearch, 350);
    });

    filterBtn?.addEventListener('click', () => {
        document.querySelector('.modal-backdrop')?.remove();
        const checkboxContainer = document.getElementById('filterPermissionCheckboxes');
        if (!window.allPermissions) {
            waitingForPermissions = true;
            if (checkboxContainer) checkboxContainer.classList.add('d-none');
            filterModalInstance.show();
            adminConnection.invoke("GetPermissions").catch(() => {
                window.showToast?.("خطا در دریافت مجوزها", "error");
                filterModalInstance.hide();
                document.querySelector('.modal-backdrop')?.remove();
                waitingForPermissions = false;
            });
        } else {
            waitingForPermissions = false;
            populateFilterCheckboxes();
            if (checkboxContainer) checkboxContainer.classList.remove('d-none');
            filterModalInstance.show();
        }
    });

    applyBtn?.addEventListener('click', () => {
        const checked = Array.from(document.querySelectorAll('#filterPermissionCheckboxes .perm-check:checked'))
            .map(cb => parseInt(cb.value));
        currentSearch.permissionIds = checked;
        performSearch();
        if (checked.length > 0) clearFilterBtn?.classList.remove('d-none');
        else clearFilterBtn?.classList.add('d-none');
        filterModalInstance.hide();
        document.querySelector('.modal-backdrop')?.remove();
    });

    resetBtn?.addEventListener('click', () => {
        document.querySelectorAll('#filterPermissionCheckboxes .perm-check').forEach(cb => cb.checked = false);
    });

    clearFilterBtn?.addEventListener('click', () => {
        currentSearch.roleName = '';
        currentSearch.permissionIds = [];
        if (searchInput) searchInput.value = '';
        document.querySelectorAll('#filterPermissionCheckboxes .perm-check').forEach(cb => cb.checked = false);
        clearFilterBtn.classList.add('d-none');
        performSearch();
    });
}

function populateFilterCheckboxes() {
    const container = document.getElementById('filterPermissionCheckboxes');
    if (!container || !window.allPermissions) return;
    const gen = window.allPermissions.filter(p => p.category === 'General');
    const not = window.allPermissions.filter(p => p.category === 'Notification');
    const selectedIds = currentSearch.permissionIds;
    container.innerHTML = `
        <div class="permission-group-title mb-2"><i class="fas fa-cogs"></i> مجوزهای مدیریتی</div>
        <div class="filter-permission-row">
            ${gen.map(p => `
                <label class="permission-card">
                    <input type="checkbox" value="${p.id}" class="perm-check" ${selectedIds.includes(p.id) ? 'checked' : ''}>
                    <span class="permission-card-content"><i class="fas fa-shield-alt"></i><span class="perm-name">${p.name}</span></span>
                    <span class="status-icon unchecked"><i class="far fa-circle"></i></span>
                    <span class="status-icon checked"><i class="fas fa-check-circle"></i></span>
                </label>
            `).join('')}
        </div>
        <div class="permission-group-title mb-2 mt-3"><i class="fas fa-bell"></i> مجوزهای نوتیفیکیشن</div>
        <div class="filter-permission-row">
            ${not.map(p => `
                <label class="permission-card">
                    <input type="checkbox" value="${p.id}" class="perm-check" ${selectedIds.includes(p.id) ? 'checked' : ''}>
                    <span class="permission-card-content"><i class="fas fa-bell"></i><span class="perm-name">${p.name}</span></span>
                    <span class="status-icon unchecked"><i class="far fa-circle"></i></span>
                    <span class="status-icon checked"><i class="fas fa-check-circle"></i></span>
                </label>
            `).join('')}
        </div>
    `;
    container.classList.remove('d-none');
}

// ========== SORT DELEGATION ==========
document.getElementById('mainContent')?.addEventListener('click', (e) => {
    const th = e.target.closest('th[data-sort]');
    if (!th || currentPage !== 'users') return;

    const field = th.dataset.sort;
    if (userState.sortField === field) {
        userState.sortAsc = !userState.sortAsc;
    } else {
        userState.sortField = field;
        userState.sortAsc = true;
    }
    applyUserSort();
    renderUsersTable(userState.items);
    document.querySelectorAll('th[data-sort] i').forEach(icon => icon.className = 'fas fa-sort text-secondary ms-1');
    const icon = th.querySelector('i');
    if (icon) {
        icon.className = `fas fa-sort-${userState.sortAsc ? 'up' : 'down'} text-primary ms-1`;
    }
});

// ========== BAN SLIDE-OVER ==========
let banUserId = null;
let banUserIsBanned = false;

function openBanSlideover(userId, isBanned) {
    banUserId = userId;
    banUserIsBanned = isBanned;
    const slide = document.getElementById('banSlideover');
    const backdrop = document.getElementById('banSlideBackdrop');
    slide.classList.add('open');
    backdrop.classList.add('show');
    document.getElementById('banSlideBody').innerHTML =
        '<div class="text-center py-5"><div class="spinner-border text-primary"></div></div>';
    if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
        adminConnection.invoke("GetUserBanHistories", userId);
    }
}

function closeBanSlideover() {
    document.getElementById('banSlideover').classList.remove('open');
    document.getElementById('banSlideBackdrop').classList.remove('show');
    banUserId = null;
}

function getCurrentTimePlusMinutes(minutes = 5) {
    const d = new Date();
    d.setMinutes(d.getMinutes() + minutes);
    const hh = String(d.getHours()).padStart(2, '0');
    const mm = String(d.getMinutes()).padStart(2, '0');
    return `${hh}:${mm}`;
}

function populateBanSlideover(data) {
    const info = data.userInformation;
    const histories = data.banHistories || [];
    const body = document.getElementById('banSlideBody');
    const statusClass = banUserIsBanned ? 'banned' : 'active';
    const statusText = banUserIsBanned ? 'مسدود' : 'فعال';

    let html = `
        <div class="ban-user-card">
            <div class="user-details">
                <h4>${info.fullName}</h4>
                <p>${info.userName}</p>
            </div>
            <span class="ban-status-badge ${statusClass}">${statusText}</span>
        </div>
    `;

    if (!banUserIsBanned) {
        html += `
        <div class="ban-form-section">
            <h5 class="mb-3 fw-bold">اعمال مسدودیت</h5>
            <div class="radio-group">
                <label class="radio-card">
                    <input type="radio" name="banType" value="permanent" checked onchange="toggleBanDateSection()">
                    <span class="radio-card-content"><i class="fas fa-lock"></i> دائم</span>
                </label>
                <label class="radio-card">
                    <input type="radio" name="banType" value="temporary" onchange="toggleBanDateSection()">
                    <span class="radio-card-content"><i class="fas fa-clock"></i> موقت</span>
                </label>
            </div>
            <div id="tempBanDateSection" style="display:none;">
                <label class="form-label">تاریخ پایان</label>
                <input type="text" id="banEndDate" class="form-control" placeholder="انتخاب تاریخ">
                <div class="preset-dates">
                    <button type="button" class="preset-date-btn" data-days="1">+۱ روز</button>
                    <button type="button" class="preset-date-btn" data-days="5">+۵ روز</button>
                    <button type="button" class="preset-date-btn" data-days="7">+۷ روز</button>
                </div>
                <label class="form-label mt-2">ساعت پایان</label>
                <input type="time" id="banEndTime" class="time-input form-control" value="${getCurrentTimePlusMinutes(5)}">
            </div>
            <div class="mt-3">
                <label class="form-label">دلیل (اختیاری)</label>
                <textarea id="banReason" class="form-control" rows="2" placeholder="توضیح مختصر..."></textarea>
            </div>
            <button class="btn-ban mt-4" onclick="submitBan()">
                <i class="fas fa-gavel"></i> ثبت مسدودیت
            </button>
        </div>`;
    } else {
        html += `
        <div class="ban-form-section">
            <h5 class="mb-3 fw-bold">رفع مسدودیت</h5>
            <label class="form-label">دلیل (اختیاری)</label>
            <textarea id="unbanReason" class="form-control" rows="2" placeholder="توضیح..."></textarea>
            <button class="btn-unban mt-4" onclick="submitUnban()">
                <i class="fas fa-unlock"></i> رفع مسدودیت
            </button>
        </div>
        <div class="ban-form-section">
            <h5 class="mb-3 fw-bold">تغییر مدت مسدودیت</h5>
            <div class="radio-group">
                <label class="radio-card">
                    <input type="radio" name="changeBanType" value="permanent" checked onchange="toggleChangeDateSection()">
                    <span class="radio-card-content"><i class="fas fa-lock"></i> دائم</span>
                </label>
                <label class="radio-card">
                    <input type="radio" name="changeBanType" value="temporary" onchange="toggleChangeDateSection()">
                    <span class="radio-card-content"><i class="fas fa-clock"></i> موقت</span>
                </label>
            </div>
            <div id="changeDateSection" style="display:none;">
                <label class="form-label">تاریخ پایان جدید</label>
                <input type="text" id="changeEndDate" class="form-control" placeholder="انتخاب تاریخ">
                <div class="preset-dates">
                    <button type="button" class="preset-date-btn" data-days="1">+۱ روز</button>
                    <button type="button" class="preset-date-btn" data-days="5">+۵ روز</button>
                    <button type="button" class="preset-date-btn" data-days="7">+۷ روز</button>
                </div>
                <label class="form-label mt-2">ساعت پایان</label>
                <input type="time" id="changeEndTime" class="time-input form-control" value="${getCurrentTimePlusMinutes(5)}">
            </div>
            <div class="mt-3">
                <label class="form-label">دلیل (اختیاری)</label>
                <textarea id="changeReason" class="form-control" rows="2" placeholder="توضیح..."></textarea>
            </div>
            <button class="btn-change mt-4" onclick="submitChangeBanExpiry()">
                <i class="fas fa-pen"></i> اعمال تغییر
            </button>
        </div>`;
    }

    if (histories.length > 0) {
        html += `<div class="mt-4"><h5 class="fw-bold mb-3">تاریخچه</h5>`;
        histories.forEach(h => {
            let actionText = '', actionClass = '';
            switch (h.action) {
                case 'Ban': actionText = 'مسدود'; actionClass = 'ban'; break;
                case 'UnBan': actionText = 'رفع مسدودیت'; actionClass = 'unban'; break;
                case 'Change': actionText = 'تغییر مدت'; actionClass = 'change'; break;
            }
            let desc = '';
            if (h.action === 'Ban') {
                if (h.banExpiryDate) desc = `کاربر از ${h.actionDate} به‌صورت موقت تا ${h.banExpiryDate} مسدود شد`;
                else desc = `کاربر از ${h.actionDate} به‌صورت دائم مسدود شد`;
            } else if (h.action === 'UnBan') {
                desc = `کاربر در ${h.actionDate} از حالت مسدود خارج شد`;
            } else if (h.action === 'Change') {
                if (h.banExpiryDate) desc = `مدت مسدودی کاربر در ${h.actionDate} به‌صورت موقت تا ${h.banExpiryDate} تغییر کرد`;
                else desc = `مدت مسدودی کاربر در ${h.actionDate} به دائم تغییر کرد`;
            }
            if (h.reason) desc += ` – دلیل: ${h.reason}`;

            const fullAction = h.actionDateFull || h.actionDate;
            const fullExpiry = h.banExpiryDateFull ? ` – پایان: ${h.banExpiryDateFull}` : '';
            const tooltipText = `${h.bannedByAdminUserName} | ${fullAction}${fullExpiry}${h.reason ? ' | دلیل: ' + h.reason : ''}`;

            html += `
            <div class="ban-history-item" data-tooltip="${escapeHtml(tooltipText)}">
                <div class="history-header">
                    <span class="fw-bold">${h.bannedByAdminUserName}</span>
                    <span class="action-badge ${actionClass}">${actionText}</span>
                </div>
                <div class="history-date">${desc}</div>
            </div>`;
        });
        html += `</div>`;
    }

    body.innerHTML = html;

    const banDateInput = document.getElementById('banEndDate');
    if (banDateInput && typeof initShamsiDatePicker === 'function') initShamsiDatePicker(banDateInput);
    const changeDateInput = document.getElementById('changeEndDate');
    if (changeDateInput && typeof initShamsiDatePicker === 'function') initShamsiDatePicker(changeDateInput);
}

document.getElementById('banSlideover')?.addEventListener('click', function (e) {
    const btn = e.target.closest('.preset-date-btn');
    if (!btn) return;
    const days = parseInt(btn.dataset.days);
    const dateInput = document.getElementById('changeEndDate') || document.getElementById('banEndDate');
    if (!dateInput) return;
    applyPreset(days, dateInput);
});

function applyPreset(days, dateInput) {
    if (!dateInput) return;
    if (typeof gregorianToJalali !== 'function' || typeof jalaliToGregorian !== 'function') {
        window.showToast?.("تقویم شمسی بارگذاری نشده است.", "error");
        return;
    }
    let baseDateStr = dateInput.value.trim();
    let baseYear, baseMonth, baseDay;
    if (baseDateStr && /^\d{4}\/\d{2}\/\d{2}$/.test(baseDateStr)) {
        const parts = baseDateStr.split('/');
        baseYear = parseInt(parts[0]);
        baseMonth = parseInt(parts[1]);
        baseDay = parseInt(parts[2]);
    } else {
        const today = new Date();
        [baseYear, baseMonth, baseDay] = gregorianToJalali(today.getFullYear(), today.getMonth() + 1, today.getDate());
    }
    const greg = jalaliToGregorian(baseYear, baseMonth, baseDay);
    const newDate = new Date(greg[0], greg[1] - 1, greg[2] + days);
    const [fy, fm, fd] = gregorianToJalali(newDate.getFullYear(), newDate.getMonth() + 1, newDate.getDate());
    dateInput.value = `${fy}/${String(fm).padStart(2, '0')}/${String(fd).padStart(2, '0')}`;
}

function toggleBanDateSection() {
    const tempRadio = document.querySelector('input[name="banType"][value="temporary"]');
    const section = document.getElementById('tempBanDateSection');
    if (section) section.style.display = tempRadio.checked ? 'block' : 'none';
}

function toggleChangeDateSection() {
    const tempRadio = document.querySelector('input[name="changeBanType"][value="temporary"]');
    const section = document.getElementById('changeDateSection');
    if (section) section.style.display = tempRadio.checked ? 'block' : 'none';
}

function submitBan() {
    const userId = banUserId;
    const banTypeRadio = document.querySelector('input[name="banType"]:checked');
    const banType = banTypeRadio?.value;
    const reason = document.getElementById('banReason')?.value.trim();
    const dateInput = document.getElementById('banEndDate');
    const timeInput = document.getElementById('banEndTime');

    let expiryDate = null;
    if (banType === 'temporary') {
        const datePart = dateInput?.value.trim();
        const timePart = timeInput?.value || '00:00';
        if (!datePart) { window.showToast?.("لطفاً تاریخ و ساعت پایان را انتخاب کنید.", "warning"); return; }
        expiryDate = `${datePart} ${timePart}:00`;
    }

    const dto = { UserId: userId, BanExpiryDate: expiryDate, Reason: reason || null };
    if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
        adminConnection.invoke("BanUser", dto).catch(err => { console.error(err); window.showToast?.("خطا در ارسال درخواست", "error"); });
    }
}

function submitUnban() {
    const userId = banUserId;
    const reason = document.getElementById('unbanReason')?.value.trim();
    const dto = { UserId: userId, Reason: reason || null };
    if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
        adminConnection.invoke("UnBanUser", dto).catch(err => { console.error(err); window.showToast?.("خطا در ارسال درخواست", "error"); });
    }
}

function submitChangeBanExpiry() {
    const userId = banUserId;
    const changeTypeRadio = document.querySelector('input[name="changeBanType"]:checked');
    const changeType = changeTypeRadio?.value;
    const reason = document.getElementById('changeReason')?.value.trim();
    const dateInput = document.getElementById('changeEndDate');
    const timeInput = document.getElementById('changeEndTime');

    let newExpiry = null;
    if (changeType === 'temporary') {
        const datePart = dateInput?.value.trim();
        const timePart = timeInput?.value || '00:00';
        if (!datePart) { window.showToast?.("لطفاً تاریخ و ساعت پایان را انتخاب کنید.", "warning"); return; }
        newExpiry = `${datePart} ${timePart}:00`;
    }

    const dto = { UserId: userId, BanExpiryDate: newExpiry, Reason: reason || null };
    if (adminConnection && adminConnection.state === signalR.HubConnectionState.Connected) {
        adminConnection.invoke("ChangeUserBanExpiry", dto).catch(err => { console.error(err); window.showToast?.("خطا در ارسال درخواست", "error"); });
    }
}

function escapeHtml(text) {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#039;');
}

// ========== USER ROLES MANAGEMENT (FLIP) ==========
let oldCardPositions = new Map();

function captureCardPositions() {
    oldCardPositions.clear();
    const cards = document.querySelectorAll('#userRolesCardsContainer .role-card');
    cards.forEach(card => {
        const id = parseInt(card.dataset.roleId);
        if (!isNaN(id)) oldCardPositions.set(id, card.getBoundingClientRect());
    });
}

function animateRoleCardsAfterFilter() {
    const container = document.getElementById('userRolesCardsContainer');
    if (!container) return;
    const newCards = container.querySelectorAll('.role-card');
    newCards.forEach(card => {
        const id = parseInt(card.dataset.roleId);
        if (isNaN(id)) return;
        const oldRect = oldCardPositions.get(id);
        if (oldRect) {
            const newRect = card.getBoundingClientRect();
            const deltaX = oldRect.left - newRect.left;
            const deltaY = oldRect.top - newRect.top;
            if (deltaX !== 0 || deltaY !== 0) {
                card.style.transition = 'none';
                card.style.transform = `translate(${deltaX}px, ${deltaY}px)`;
                requestAnimationFrame(() => {
                    card.style.transition = 'transform 0.4s cubic-bezier(0.25, 0.8, 0.25, 1.2)';
                    card.style.transform = '';
                });
            }
        } else {
            card.classList.add('fade-enter');
            requestAnimationFrame(() => {
                card.classList.add('fade-enter-active');
                card.addEventListener('transitionend', () => card.classList.remove('fade-enter', 'fade-enter-active'), { once: true });
            });
        }
    });
}

function renderUserRoleCards(roles, selectedIds) {
    const container = document.getElementById('userRolesCardsContainer');
    if (!container) return;
    const searchInput = document.getElementById('roleSearchInputForUser');
    let filteredRoles = roles;
    if (searchInput) {
        const term = searchInput.value.trim().toLowerCase();
        if (term) filteredRoles = roles.filter(r => r.name.toLowerCase().includes(term));
    }
    captureCardPositions();

    container.innerHTML = filteredRoles.map(role => {
        const isSelected = selectedIds.includes(role.id);
        const genPerms = role.permissions?.filter(p => p.category === 'General') || [];
        const notifPerms = role.permissions?.filter(p => p.category === 'Notification') || [];
        const hasPermissions = (genPerms.length + notifPerms.length) > 0;
        return `
            <div class="role-card ${isSelected ? 'selected' : ''}" data-role-id="${role.id}">
                <div class="role-card-header">
                    <div style="flex:1; min-width:0;">
                        <div class="role-card-title">${role.name}</div>
                        <div class="role-card-perm-count">${role.permissions?.length || 0} مجوز</div>
                    </div>
                    <span class="status-icon unchecked"><i class="far fa-circle"></i></span>
                    <span class="status-icon checked"><i class="fas fa-check-circle"></i></span>
                </div>
            ${hasPermissions ? `
<button class="btn-show-perms" onclick="event.stopPropagation(); toggleRolePermissions('${role.id}')">
    <i class="fas fa-eye"></i> نمایش مجوزها
</button>
<div class="role-permissions-collapse" id="perms-${role.id}">
    <div class="perm-glass-box">
        ${genPerms.length ? `
            <div class="perm-category-title"><i class="fas fa-cogs"></i> مجوزهای مدیریتی</div>
            <div class="perm-list">${genPerms.map(p => `<span class="perm-badge-small">${p.name}</span>`).join('')}</div>
        ` : ''}
        ${notifPerms.length ? `
            <div class="perm-category-title"><i class="fas fa-bell"></i> مجوزهای نوتیفیکیشن</div>
            <div class="perm-list">${notifPerms.map(p => `<span class="perm-badge-small">${p.name}</span>`).join('')}</div>
        ` : ''}
    </div>
</div>
` : ''}
            </div>
        `;
    }).join('');

    container.querySelectorAll('.role-card').forEach(card => {
        card.addEventListener('click', function (e) {
            if (e.target.closest('.btn-show-perms')) return;
            const roleId = parseInt(this.dataset.roleId);
            const idx = window.currentUserInfo.userRolesIdList.indexOf(roleId);
            if (idx > -1) {
                window.currentUserInfo.userRolesIdList.splice(idx, 1);
                this.classList.remove('selected');
            } else {
                window.currentUserInfo.userRolesIdList.push(roleId);
                this.classList.add('selected');
            }
        });
    });

    animateRoleCardsAfterFilter();
}

function toggleRolePermissions(roleId) {
    const collapse = document.getElementById('perms-' + roleId);
    if (!collapse) return;
    collapse.classList.toggle('open');
}

let userRoleSearchTimer = null;
function onUserRoleSearch() {
    clearTimeout(userRoleSearchTimer);
    userRoleSearchTimer = setTimeout(() => {
        renderUserRoleCards(window.allRoles, window.currentUserInfo.userRolesIdList);
    }, 250);
}

function setupUserRoleSearch() {
    const searchInput = document.getElementById('roleSearchInputForUser');
    if (!searchInput) return;
    searchInput.removeEventListener('input', onUserRoleSearch);
    searchInput.addEventListener('input', onUserRoleSearch);
}

// ========== DOMContentLoaded Initialisation ==========
document.addEventListener('DOMContentLoaded', () => {
    function setInitialSidebarState() {
        document.body.classList[window.innerWidth > 768 ? 'add' : 'remove']('sidebar-open');
    }
    setInitialSidebarState();
    window.addEventListener('resize', setInitialSidebarState);

    document.querySelectorAll('.nav-item[data-page]').forEach(item => {
        item.addEventListener('click', e => {
            e.preventDefault();
            loadPage(item.dataset.page);
        });
    });

    document.getElementById('sidebarToggle')?.addEventListener('click', () => document.body.classList.toggle('sidebar-open'));
    document.getElementById('sidebarOverlay')?.addEventListener('click', () => document.body.classList.remove('sidebar-open'));

    window.addEventListener('popstate', (event) => {
        const state = event.state;
        if (state && state.page) {
            loadPage(state.page, false, state.params || {});
        } else {
            const pathInfo = getPageFromPath(window.location.pathname);
            if (typeof pathInfo === 'object') {
                loadPage(pathInfo.page, false, { roleId: pathInfo.roleId, userId: pathInfo.userId, id: pathInfo.id });
            } else {
                loadPage(pathInfo, false);
            }
        }
    });

    const bell = document.getElementById('adminNotifBell');
    if (bell) {
        bell.addEventListener('click', (e) => {
            e.stopPropagation();
            openAdminNotifPanel();
        });
    }

    // ===== Event delegation on mainContent =====
    document.getElementById('mainContent')?.addEventListener('click', (e) => {
        const fileEl = e.target.closest('.achat-file');
        if (fileEl) {
            const downloadName = fileEl.dataset.download;
            const originalName = fileEl.dataset.original;
            if (fileEl.classList.contains('image-attachment')) {
                openImagePreview(downloadName, originalName);
            } else {
                downloadFile(downloadName, originalName);
            }
            return;
        }

        if (e.target.id === 'saveUserRolesBtn') {
            if (!adminConnection || adminConnection.state !== signalR.HubConnectionState.Connected) {
                window.showToast?.("ارتباط با سرور برقرار نیست.", "warning");
                return;
            }
            const userId = window.currentUserInfo?.userId;
            const roleIds = window.currentUserInfo?.userRolesIdList || [];
            adminConnection.invoke("ChangeUserRolesAsync", {
                UserId: userId,
                UserName: window.currentUserInfo.userName,
                FullName: window.currentUserInfo.fullName,
                UserRolesIdList: roleIds
            }).catch(err => { console.error(err); window.showToast?.("خطا در ذخیره تغییرات", "error"); });
        }

        if (e.target.id === 'saveUserSupportCategoriesBtn') {
            if (!adminConnection || adminConnection.state !== signalR.HubConnectionState.Connected) {
                window.showToast?.("ارتباط با سرور برقرار نیست.", "warning");
                return;
            }
            const userId = window.currentUserSupportCategoryInfo?.userId;
            const ids = window.currentUserSupportCategoryInfo?.supportCategoryIdList || [];
            adminConnection.invoke("ChangeUserSupportCategories", {
                UserId: userId,
                UserName: window.currentUserSupportCategoryInfo.userName,
                FullName: window.currentUserSupportCategoryInfo.fullName,
                SupportCategoryIdList: ids
            }).catch(err => { console.error(err); window.showToast?.("خطا در ذخیره", "error"); });
        }

        if (e.target.id === 'addSupportCategoryBtn' || e.target.closest('#addSupportCategoryBtn')) {
            loadPage('categories-add');
        }
    });

    // ===== Initialise Hubs =====
    initAdminHub();
});

// ========== NAVIGATION HELPERS ==========
function navigateToEditRole(roleId) { loadPage('roles-edit', true, { roleId }); }
function navigateToDeleteRole(roleId) { loadPage('roles-delete', true, { roleId }); }
function navigateToUserRoles(userId) { loadPage('users-roles', true, { userId }); }
function navigateToEditSupportCategory(id) { loadPage('categories-edit', true, { id }); }