(function () {
    'use strict';

    // ========================================================================
    // Local state
    // ========================================================================
    let chats = [];
    let activeChatId = window.initialActiveChatId || null;
    let selectedFiles = [];
    let userName = window.currentUserName || '';
    let typingActive = false;
    let presenceTypingTimer = null;       // timer for hiding the presence message
    let typingChainTimerId = null;        // timer for the typing effect chain (cancellable)

    // ========================================================================
    // DOM references
    // ========================================================================
    const chatListEl = document.getElementById('chatList');
    const activeChatNameEl = document.getElementById('activeChatName');
    const activeChatStatusBadge = document.getElementById('activeChatStatusBadge');
    const activeChatDateEl = document.getElementById('activeChatDate');
    const chatMessagesEl = document.getElementById('chatMessages');
    const messageForm = document.getElementById('messageForm');
    const messageInput = document.getElementById('messageInput');
    const createChatBtn = document.getElementById('createChatBtn');
    const newChatModalEl = document.getElementById('newChatModal');
    const newChatModal = newChatModalEl ? new bootstrap.Modal(newChatModalEl) : null;
    const attachBtn = document.getElementById('attachBtn');
    const fileInput = document.getElementById('fileInput');
    const filePreview = document.getElementById('filePreview');
    const notificationContainer = document.getElementById('notificationContainer');
    const endChatBtn = document.getElementById('endChatBtn');
    const welcomeGuide = document.getElementById('chatWelcomeGuide');

    // ========================================================================
    // SignalR connection
    // ========================================================================
    let connection = null;
    let renewTimer = null;
    let isRenewingToken = false;
    let renewalInProgress = false;

    // ========================================================================
    // Token helpers
    // ========================================================================

    /**
     * Extracts the expiry timestamp (ms) from a JWT access token.
     * @param {string} token - The raw JWT.
     * @returns {number|null} Expiry in milliseconds, or null if parsing fails.
     */
    function getTokenExpiry(token) {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            return (payload.exp || payload.Exp) * 1000;
        } catch (e) { return null; }
    }

    /**
     * Schedules an automatic token renewal shortly before the token expires.
     * @param {string} token - The current access token.
     */
    function scheduleTokenRenewal(token) {
        if (renewTimer) { clearTimeout(renewTimer); renewTimer = null; }
        const expiry = getTokenExpiry(token);
        if (!expiry) return;
        const timeLeft = expiry - Date.now();
        if (timeLeft <= 15000) { renewAndUpdateToken(); return; }
        const renewIn = timeLeft - 15000;
        renewTimer = setTimeout(() => { renewTimer = null; renewAndUpdateToken(); }, renewIn);
    }

    /**
     * Renews the access token and restarts the SignalR connection with the new token.
     */
    async function renewAndUpdateToken() {
        if (renewalInProgress) return;
        renewalInProgress = true;
        try {
            const newToken = await renewSignalRToken();
            if (newToken) {
                if (connection && connection.state === signalR.HubConnectionState.Connected) {
                    await connection.stop();
                }
                await startMainConnection(newToken);
            } else {
                window.location.href = '/login';
            }
        } finally {
            renewalInProgress = false;
        }
    }

    /**
     * Calls the server endpoint to obtain a new access token using the refresh token cookie.
     * @returns {Promise<string|null>} The new access token, or null on failure.
     */
    async function renewSignalRToken() {
        if (isRenewingToken) return null;
        isRenewingToken = true;
        try {
            const res = await fetch('/api/token/auto-renew', { credentials: 'include' });
            if (res.ok) {
                const data = await res.json();
                window.accessToken = data.accessToken;
                sessionStorage.setItem('accessToken', data.accessToken);
                return data.accessToken;
            }
        } catch (err) { console.error('Token renewal failed'); }
        finally { isRenewingToken = false; }
        return null;
    }

    // ========================================================================
    // Server event handlers
    // ========================================================================

    /**
     * Registers all SignalR hub event listeners for the chat connection.
     * @param {signalR.HubConnection} conn - The active hub connection.
     */
    function registerHubEvents(conn) {
        conn.on("SystemMessage", function (alert) {
            if (alert) {
                window.showToast?.(
                    alert.message || alert.Message,
                    alert.type || alert.Type || 'info',
                    alert.title || alert.Title || 'پیام سیستم'
                );
            }
        });

        conn.on("SetUserName", function (user) {
            userName = user.result;
        });

        conn.on("NewChatCreated", function (chat) {
            const newChat = {
                id: chat.uniqChatId,
                name: chat.subject,
                status: chat.chatStatus === "درحال پاسخگویی" ? "responding" : "completed",
                lastMessage: 'گفتگو آغاز شد',
                time: 'اکنون',
                unread: 0,
                createdAt: new Date().toLocaleDateString('fa-IR'),
                unitName: chat.supportCategoryName || '',
                unitIconClass: chat.supportCategoryClass || 'fas fa-tools',
                messages: []
            };
            chats.unshift(newChat);
            renderChatList();
            setActiveChat(newChat.id);
            newChatModal?.hide();
            document.getElementById('newChatForm')?.reset();
        });

        conn.on("ReceiveChatData", function (chatData) {
            const chat = chats.find(c => c.id === activeChatId);
            if (chat) {
                chat.name = chatData.subject;
                chat.createdAt = chatData.createdTime;
                chat.status = chatData.chatStatus === "درحال پاسخگویی" ? "responding" : "completed";
                chat.unitName = chatData.supportCategoryName || '';
                chat.unitIconClass = chatData.supportCategoryClass || 'fas fa-tools';
                updateChatHeader();
            }
        });

        conn.on("ReceiveChatMessages", function (chatMessages) {
            const chat = chats.find(c => c.id === activeChatId);
            if (!chat) return;
            chat.messages = chatMessages.map(m => ({
                sender: m.senderUserName === userName ? 'user' : 'admin',
                senderName: m.senderUserName === userName ? null : (m.senderName || m.senderUserName),
                text: m.data || '',
                files: (m.attachFiles || []).map(f => ({
                    originalName: f.originalName || f.origianlName || 'فایل',
                    downloadName: f.downloadName
                })),
                time: m.time,
                incoming: m.senderUserName !== userName,
                isSeen: !!m.isSeen
            }));
            renderChatMessages();
            markCurrentChatAsSeen();
        });

        conn.on("MessagesSeen", (chatUniqId, seenAt) => {
            const chat = chats.find(c => c.id === chatUniqId);
            if (!chat) return;
            chat.messages.forEach(m => {
                if (!m.incoming) m.isSeen = true;
            });
            document.querySelectorAll('.message.outgoing .msg-seen-status').forEach(el => {
                el.classList.add('seen');
            });
        });

        conn.on("ReceiveMessage", function (message) {
            if (!message || !message.uniqChatId) return;
            const chat = chats.find(c => c.id === message.uniqChatId);
            if (!chat) return;

            const isOwnMessage = message.senderUserName === userName;

            let displaySenderName = null;
            if (!isOwnMessage) {
                displaySenderName = message.adminFullName || message.senderName || message.senderFullName || message.senderUserName;
            }

            const msg = {
                sender: isOwnMessage ? 'user' : 'admin',
                senderName: displaySenderName,
                text: message.messageData || '',
                files: (message.messageFiles || []).map(f => ({
                    originalName: f.originalName || f.origianlName || 'فایل',
                    downloadName: f.downloadName
                })),
                time: message.sendTime ? new Date(message.sendTime).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit' }) : '',
                incoming: !isOwnMessage,
                isSeen: !!message.isSeen
            };

            chat.messages.push(msg);
            chat.lastMessage = msg.text || (msg.files && msg.files.length > 0 ? 'فایل ارسال شد' : '');
            chat.time = msg.time;

            if (activeChatId === message.uniqChatId) {
                const msgElement = document.createElement('div');
                msgElement.className = 'message ' + (msg.incoming ? 'incoming' : 'outgoing') + ' message-enter';

                let content = msg.text || '';
                if (msg.files && msg.files.length > 0) {
                    content += '<div class="message-files">';
                    msg.files.forEach(file => {
                        const isImage = /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(file.downloadName);
                        if (isImage) {
                            content += `<div class="file-attachment image-attachment" data-download="${file.downloadName}" data-original="${file.originalName}">
                        <div class="image-preview-thumb"><img src="/download/${encodeURIComponent(file.downloadName)}?originalName=${encodeURIComponent(file.originalName)}" alt="${file.originalName}" loading="lazy" /></div>
                        <span>${file.originalName}</span></div>`;
                        } else {
                            content += `<div class="file-attachment" data-download="${file.downloadName}" data-original="${file.originalName}">
                        <i class="fas fa-file"></i><span>${file.originalName}</span></div>`;
                        }
                    });
                    content += '</div>';
                }

                let seenHtml = '';
                if (!msg.incoming) {
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
                            <svg class="seen-check" viewBox="0 0 16 12" width="16" height="12" fill="none">
                                <polyline class="tick" points="4,6 7,9 12,4" />
                            </svg>
                        </span>
                    </span>
                </span>`;
                }

                msgElement.innerHTML = `<div class="message-bubble">${content}
            <div class="message-meta">
                ${msg.incoming ? '<span class="message-sender">' + (msg.senderName || 'پشتیبانی') + '</span>' : ''}
                <span class="message-time">${msg.time}</span>
                ${seenHtml}
            </div></div>`;

                chatMessagesEl.appendChild(msgElement);
                chatMessagesEl.scrollTop = chatMessagesEl.scrollHeight;

                msgElement.addEventListener('animationend', () => {
                    msgElement.classList.remove('message-enter');
                }, { once: true });

                if (!msg.incoming && welcomeGuide) {
                    welcomeGuide.style.display = 'none';
                }

                chat.unread = 0;

                if (msg.incoming) {
                    markCurrentChatAsSeen();
                }
            } else {
                chat.unread = (chat.unread || 0) + 1;
                showNotification(message.uniqChatId, msg);
            }
            renderChatList();
        });

        conn.on("ChatEnded", function (chatUniqId) {
            const chat = chats.find(c => c.id === chatUniqId);
            if (chat) {
                chat.status = 'completed';
                renderChatList();
                if (activeChatId === chatUniqId) updateChatHeader();
                window.showToast?.("گفتگو با موفقیت پایان یافت.", 'success');
            }
        });

        conn.on("ChatEndedByAdmin", function (chatUniqId) {
            const chat = chats.find(c => c.id === chatUniqId);
            if (chat) {
                chat.status = 'completed';
                renderChatList();
                if (activeChatId === chatUniqId) {
                    updateChatHeader();
                }
                window.showToast?.(
                    "گفتگوی شما با موفقیت توسط ادمین به پایان رسید. 🌟✨<br>هر زمان که سوال یا مشکل جدیدی داشتید، می‌توانید یک چت تازه ایجاد کنید. ما همیشه آماده کمک به شما هستیم! 😊💬",
                    "success",
                    "پایان گفتگو",
                    6000
                );
            }
        });

        // ====================================================================
        // Admin presence & typing indicators
        // ====================================================================
        conn.on("AdminPresenceUpdate", (chatUniqId, isOnline) => {
            if (activeChatId === chatUniqId) {
                updateAdminPresence(isOnline);
            }
        });

        conn.on("AdminTyping", (chatUniqId, isTyping) => {
            if (activeChatId !== chatUniqId) return;
            const indicator = document.getElementById('userTypingIndicator');
            if (!indicator) return;

            if (isTyping) {
                indicator.classList.remove('typing-exit');
                indicator.style.display = 'flex';
            } else {
                indicator.classList.add('typing-exit');
                setTimeout(() => {
                    indicator.style.display = 'none';
                    indicator.classList.remove('typing-exit');
                }, 600);
            }
        });
    }
    // ========================================================================
    // Connection management
    // ========================================================================

    /**
     * Starts (or restarts) the main SignalR connection to the Chat hub.
     * Exposed globally so that the token renewal system can call it.
     * @param {string} token - The JWT access token.
     */
    window.startMainConnection = async function (token) {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            await connection.stop();
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/Chat", { accessTokenFactory: () => window.accessToken })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: ctx => Math.min(10000, ctx.previousRetryCount * 2000)
            })
            .build();

        registerHubEvents(connection);

        connection.onreconnecting(() => window.showToast?.("در حال تلاش برای برقراری ارتباط...", "warning", "اتصال", 0));
        connection.onreconnected(id => {
            window.showToast?.("ارتباط با سرور برقرار شد.", "success", "آنلاین", 3000);
            if (activeChatId) {
                connection.invoke("GetChatData", activeChatId).catch(() => { });
                connection.invoke("GetUserChatMessages", activeChatId).catch(() => { });
            }
        });

        connection.onclose(async (error) => {
            if (renewTimer) { clearTimeout(renewTimer); renewTimer = null; }
            if (renewalInProgress) return;

            await new Promise(resolve => setTimeout(resolve, 100));
            if (!isRenewingToken && !renewalInProgress &&
                (!connection || connection.state === signalR.HubConnectionState.Disconnected)) {
                const newToken = await renewSignalRToken();
                if (newToken) {
                    await startMainConnection(newToken);
                } else {
                    window.location.href = '/login';
                }
            }
        });

        try {
            await connection.start();
            scheduleTokenRenewal(token);
            if (activeChatId) connection.invoke("GetChatData", activeChatId);
        } catch (err) {
            if (renewalInProgress) renewalInProgress = false;
        }
    };

    /**
     * Stops the SignalR connection gracefully.
     */
    window.stopMainConnection = function () {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.stop();
        }
    };

    // ========================================================================
    // Admin presence & typing UI
    // ========================================================================

    /**
     * Ensures the admin presence indicator element exists in the header.
     * @returns {HTMLElement} The indicator span.
     */
    function ensurePresenceIndicator() {
        let el = document.getElementById('adminPresenceIndicator');
        if (!el) {
            el = document.createElement('span');
            el.id = 'adminPresenceIndicator';
            el.className = 'presence offline';
            el.title = 'آفلاین';
            const detailsRow = document.querySelector('.chat-header-details-row');
            if (detailsRow) {
                detailsRow.appendChild(el);
            }
        }
        return el;
    }

    /**
     * Types out a message character by character, with support for cancellation.
     * @param {HTMLElement} element - The target element.
     * @param {string} text - The text to type.
     * @param {Function} callback - Called when typing is finished.
     * @param {object} cancelToken - An object with a `current` property that stores the active timeout ID.
     */
    function typeWriterEffect(element, text, callback, cancelToken) {
        // Cancel any ongoing typing on the same element
        if (cancelToken && cancelToken.current) {
            clearTimeout(cancelToken.current);
            cancelToken.current = null;
        }
        element.textContent = '';
        element.style.display = 'inline-block';
        let i = 0;
        const speed = 50;
        function type() {
            if (!element.parentNode) {
                if (cancelToken) cancelToken.current = null;
                return;
            }
            if (i < text.length) {
                element.textContent += text.charAt(i);
                i++;
                const id = setTimeout(type, speed);
                if (cancelToken) cancelToken.current = id;
            } else {
                if (cancelToken) cancelToken.current = null;
                if (callback) callback();
            }
        }
        type();
    }

    /**
     * Marks all unseen messages in the active chat as seen.
     */
    function markCurrentChatAsSeen() {
        if (!activeChatId || !connection ||
            connection.state !== signalR.HubConnectionState.Connected) return;
        connection.invoke("MarkMessagesAsSeen", activeChatId).catch(() => { });
    }

    /**
     * Updates the admin presence indicator and shows a temporary status message.
     * @param {boolean} isOnline - Whether the admin is online.
     */
    function updateAdminPresence(isOnline) {
        const el = document.getElementById('adminPresenceIndicator');
        if (!el) return;
        el.className = 'presence ' + (isOnline ? 'online' : 'offline');
        el.title = isOnline ? 'آنلاین' : 'آفلاین';

        // Cancel previous presence hide timer
        if (presenceTypingTimer) {
            clearTimeout(presenceTypingTimer);
            presenceTypingTimer = null;
        }
        // Cancel ongoing typing effect (using the chain timer ID)
        if (typingChainTimerId) {
            clearTimeout(typingChainTimerId);
            typingChainTimerId = null;
        }
        const prevMsg = document.getElementById('adminPresenceMessage');
        if (prevMsg) prevMsg.remove();

        const msgEl = document.createElement('span');
        msgEl.id = 'adminPresenceMessage';
        msgEl.className = 'presence-message';
        el.parentNode.insertBefore(msgEl, el.nextSibling);

        const message = isOnline ? 'ادمین آنلاینه!' : 'ادمین افلاینه ☹️';

        const cancelToken = { current: null };
        typeWriterEffect(msgEl, message, () => {
            // After typing finishes, schedule removal
            presenceTypingTimer = setTimeout(() => {
                if (msgEl.parentNode) msgEl.remove();
                presenceTypingTimer = null;
            }, 3500);
        }, cancelToken);
        typingChainTimerId = cancelToken.current;
    }

    /**
     * Ensures the user typing indicator element exists in the header.
     * @returns {HTMLElement} The indicator container.
     */
    function ensureTypingIndicator() {
        let el = document.getElementById('userTypingIndicator');
        if (!el) {
            el = document.createElement('span');
            el.id = 'userTypingIndicator';
            el.className = 'typing-indicator';
            el.innerHTML = '<span class="typing-dot"></span><span class="typing-dot"></span><span class="typing-dot"></span>';
            const header = document.querySelector('.chat-header-details-row');
            if (header) header.appendChild(el);
        }
        return el;
    }

    // ========================================================================
    // Chat list & header rendering
    // ========================================================================

    /**
     * Renders the sidebar chat list from the local `chats` array.
     */
    function renderChatList() {
        if (!chatListEl) return;
        if (!chats.length) { chatListEl.innerHTML = ''; return; }
        const fragment = document.createDocumentFragment();
        chats.forEach(chat => {
            const item = document.createElement('div');
            item.className = 'chat-item' + (chat.id === activeChatId ? ' active' : '');
            item.dataset.chatUniqId = chat.id;
            const statusText = chat.status === 'responding' ? 'در حال پاسخگویی' : 'تکمیل شده';
            const statusClass = chat.status === 'responding' ? 'responding' : 'completed';
            const iconClass = chat.unitIconClass || 'fas fa-tools';
            item.innerHTML = `
                <div class="chat-avatar"><i class="${iconClass}"></i></div>
                <div class="chat-item-details">
                    <div class="chat-title">${chat.name}</div>
                    <div class="chat-last-message">${chat.lastMessage || ''}</div>
                    <div class="chat-item-status">
                        <span class="status-dot ${statusClass}"></span><span>${statusText}</span>
                    </div>
                </div>
                <div class="chat-meta">
                    <span class="chat-time">${chat.time}</span>
                    ${chat.unread > 0 ? '<span class="unread-badge">' + chat.unread + '</span>' : ''}
                </div>`;
            item.addEventListener('click', () => setActiveChat(chat.id));
            fragment.appendChild(item);
        });
        chatListEl.innerHTML = '';
        chatListEl.appendChild(fragment);
    }

    /**
     * Activates the chat with the given ID and requests its data from the server.
     * @param {string} chatId - The chat's unique ID (ChatUniqId).
     */
    function setActiveChat(chatId) {
        activeChatId = chatId;
        renderChatList();
        if (chatId && connection?.state === signalR.HubConnectionState.Connected) {
            connection.invoke("GetChatData", chatId);
        }
        updateChatHeader();
        ensurePresenceIndicator();
        ensureTypingIndicator();
    }

    /**
     * Updates the chat header area (title, unit, status, date, buttons) based on the active chat.
     */
    function updateChatHeader() {
        const nameEl = document.getElementById('activeChatName');
        const unitEl = document.getElementById('activeChatUnit');
        const statusEl = document.getElementById('activeChatStatusBadge');
        const dateEl = document.getElementById('activeChatDate');
        const endBtn = document.getElementById('endChatBtn');
        const messageInputLocal = document.getElementById('messageInput');
        const attachBtnLocal = document.getElementById('attachBtn');
        const sendBtn = document.querySelector('.btn-send');
        const presenceInd = document.getElementById('adminPresenceIndicator');
        const typingInd = document.getElementById('userTypingIndicator');

        if (!activeChatId) {
            if (nameEl) nameEl.textContent = 'انتخاب گفتگو';
            if (unitEl) unitEl.style.display = 'none';
            if (statusEl) {
                statusEl.textContent = '';
                statusEl.className = 'chat-status-badge';
                statusEl.style.display = 'none';
            }
            if (dateEl) dateEl.textContent = '';
            if (endBtn) endBtn.style.display = 'none';
            if (messageInputLocal) messageInputLocal.disabled = true;
            if (attachBtnLocal) attachBtnLocal.disabled = true;
            if (sendBtn) sendBtn.disabled = true;
            if (presenceInd) {
                presenceInd.className = 'presence offline';
                presenceInd.style.display = 'none';
            }
            if (typingInd) typingInd.style.display = 'none';
            return;
        }

        const chat = chats.find(c => c.id === activeChatId);
        if (!chat) {
            if (nameEl) nameEl.textContent = 'انتخاب گفتگو';
            if (unitEl) unitEl.style.display = 'none';
            if (statusEl) {
                statusEl.textContent = '';
                statusEl.className = 'chat-status-badge';
                statusEl.style.display = 'none';
            }
            if (dateEl) dateEl.textContent = '';
            if (endBtn) endBtn.style.display = 'none';
            if (messageInputLocal) messageInputLocal.disabled = true;
            if (attachBtnLocal) attachBtnLocal.disabled = true;
            if (sendBtn) sendBtn.disabled = true;
            if (presenceInd) {
                presenceInd.className = 'presence offline';
                presenceInd.style.display = 'none';
            }
            if (typingInd) typingInd.style.display = 'none';
            return;
        }

        if (nameEl) nameEl.textContent = chat.name;

        if (unitEl) {
            if (chat.unitName) {
                unitEl.style.display = 'flex';
                unitEl.innerHTML = '<i class="' + (chat.unitIconClass || 'fas fa-tools') + '"></i><span>' + chat.unitName + '</span>';
            } else {
                unitEl.style.display = 'none';
            }
        }

        if (statusEl) {
            const statusText = chat.status === 'responding' ? 'در حال پاسخگویی' : 'تکمیل شده';
            const statusClass = chat.status === 'responding' ? 'responding' : 'completed';
            statusEl.textContent = statusText;
            statusEl.className = 'chat-status-badge ' + statusClass;
            statusEl.style.display = '';
        }

        if (dateEl) {
            dateEl.textContent = 'ایجاد: ' + (chat.createdAt || 'نامشخص');
        }

        if (endBtn) {
            endBtn.style.display = chat.status === 'completed' ? 'none' : 'flex';
        }

        const isCompleted = chat.status === 'completed';
        if (messageInputLocal) messageInputLocal.disabled = isCompleted;
        if (attachBtnLocal) attachBtnLocal.disabled = isCompleted;
        if (sendBtn) sendBtn.disabled = isCompleted;

        if (presenceInd) {
            presenceInd.style.display = '';
        }
        if (typingInd) {
        }
    }

    // ========================================================================
    // Message rendering
    // ========================================================================

    /**
     * Renders all messages of the active chat and sets up file click handlers.
     */
    function renderChatMessages() {
        const chat = chats.find(c => c.id === activeChatId);
        if (!chat) return;

        const hasUserMessage = chat.messages.some(m => !m.incoming);
        if (welcomeGuide) welcomeGuide.style.display = hasUserMessage ? 'none' : 'flex';

        // Clear existing messages
        chatMessagesEl.querySelectorAll('.message').forEach(m => m.remove());

        const fragment = document.createDocumentFragment();

        chat.messages.forEach(msg => {
            const div = document.createElement('div');
            div.className = 'message ' + (msg.incoming ? 'incoming' : 'outgoing');

            let content = msg.text || '';

            if (msg.files && msg.files.length > 0) {
                content += '<div class="message-files">';
                msg.files.forEach(file => {
                    const isImage = /\.(jpg|jpeg|png|gif|bmp|webp)$/i.test(file.downloadName);
                    if (isImage) {
                        content += `<div class="file-attachment image-attachment" data-download="${file.downloadName}" data-original="${file.originalName}">
                        <div class="image-preview-thumb"><img src="/download/${encodeURIComponent(file.downloadName)}?originalName=${encodeURIComponent(file.originalName)}" alt="${file.originalName}" loading="lazy" /></div>
                        <span>${file.originalName}</span></div>`;
                    } else {
                        content += `<div class="file-attachment" data-download="${file.downloadName}" data-original="${file.originalName}">
                        <i class="fas fa-file"></i><span>${file.originalName}</span></div>`;
                    }
                });
                content += '</div>';
            }

            let seenHtml = '';
            if (!msg.incoming) {
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
                    <svg class="seen-check" viewBox="0 0 16 12" width="16" height="12" fill="none">
                        <polyline class="tick" points="4,6 7,9 12,4" />
                    </svg>
                </span>
            </span>
        </span>`;
            }

            div.innerHTML = `<div class="message-bubble">${content}
    <div class="message-meta">
        ${msg.incoming ? '<span class="message-sender">' + (msg.senderName || 'پشتیبانی') + '</span>' : ''}
        <span class="message-time">${msg.time}</span>
        ${seenHtml}
    </div></div>`;

            fragment.appendChild(div);
        });

        chatMessagesEl.appendChild(fragment);

        chatMessagesEl.onclick = function (e) {
            const fileEl = e.target.closest('.file-attachment');
            if (!fileEl) return;
            const downloadName = fileEl.dataset.download;
            const originalName = fileEl.dataset.original;
            if (fileEl.classList.contains('image-attachment')) {
                openImagePreview(downloadName, originalName);
            } else {
                downloadFile(downloadName, originalName);
            }
        };

        chatMessagesEl.scrollTop = chatMessagesEl.scrollHeight;
    }

    /**
     * Opens a full‑screen modal for an image attachment.
     * @param {string} downloadName - The stored file name.
     * @param {string} originalName - The original file name.
     */
    function openImagePreview(downloadName, originalName) {
        const old = document.getElementById('imagePreviewModal');
        if (old) old.remove();
        const modalHtml = `
            <div class="modal fade" id="imagePreviewModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered modal-xl">
                    <div class="modal-content glass-modal">
                        <div class="modal-header"><span class="modal-title">${originalName}</span><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>
                        <div class="modal-body text-center p-0"><img src="/download/${encodeURIComponent(downloadName)}?originalName=${encodeURIComponent(originalName)}" class="img-fluid rounded" alt="${originalName}" style="max-height:80vh;width:auto;" /></div>
                        <div class="modal-footer"><a href="/download/${encodeURIComponent(downloadName)}?originalName=${encodeURIComponent(originalName)}" class="btn btn-primary" download="${originalName}"><i class="fas fa-download"></i> دانلود</a><button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">بستن</button></div>
                    </div>
                </div>
            </div>`;
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modal = new bootstrap.Modal(document.getElementById('imagePreviewModal'));
        modal.show();
        document.getElementById('imagePreviewModal').addEventListener('hidden.bs.modal', function () { this.remove(); });
    }

    /**
     * Triggers a file download in the browser.
     * @param {string} downloadName - The stored file name.
     * @param {string} originalName - The file name to show in the download dialog.
     */
    function downloadFile(downloadName, originalName) {
        const a = document.createElement('a');
        a.href = `/download/${encodeURIComponent(downloadName)}?originalName=${encodeURIComponent(originalName)}`;
        a.download = originalName;
        document.body.appendChild(a);
        a.click();
        a.remove();
    }

    // ========================================================================
    // Sending messages
    // ========================================================================

    /**
     * Sends a text message and/or files to the active chat.
     * @param {string} text - The plain text message.
     * @param {File[]} [files=[]] - File objects to upload.
     */
    function sendMessage(text, files = []) {
        if (!activeChatId) {
            window.showToast?.('هنوز گفتگویی را انتخاب نکرده‌اید.');
            return;
        }
        const chat = chats.find(c => c.id === activeChatId);
        if (!chat || chat.status !== 'responding') {
            window.showToast?.('امکان ارسال پیام وجود ندارد.', 'warning');
            return;
        }

        if (files.length === 0 && text.trim()) {
            connection.invoke("SendMessageToSupport", { ChatUniqId: activeChatId, MessageData: text })
                .catch(() => {
                    window.showToast?.('خطا در ارسال پیام.', 'error');
                });
            messageInput.value = '';
        } else if (files.length > 0) {
            uploadMultipleFilesWithProgress(files, activeChatId, text);
        }
    }

    /**
     * Uploads multiple files with an in‑chat progress card.
     * @param {File[]} files - The files to upload.
     * @param {string} chatUniqId - The target chat ID.
     * @param {string} [messageText] - Optional text to accompany the files.
     */
    function uploadMultipleFilesWithProgress(files, chatUniqId, messageText) {
        if (files.length > 5) {
            window.showToast?.('حداکثر ۵ فایل می‌توانید ارسال کنید.', 'warning');
            return;
        }

        const formData = new FormData();
        formData.append("ChatUniqId", chatUniqId);
        formData.append("MessageData", messageText || '');
        files.forEach(file => formData.append("AttachFiles", file));

        const progressCard = document.createElement('div');
        progressCard.className = 'message outgoing';
        progressCard.innerHTML = `
            <div class="message-bubble">
                <div class="upload-card">
                    <div class="upload-card-header"><i class="fas fa-cloud-upload-alt"></i><span>در حال آپلود ${files.length} فایل...</span></div>
                    <div class="upload-file-list">
                        ${files.map(file => `<div class="upload-file-item"><i class="fas fa-file-alt file-icon"></i><span class="file-name">${file.name}</span><span class="file-status"><i class="fas fa-spinner fa-pulse"></i></span></div>`).join('')}
                    </div>
                    <div class="upload-overall-progress">
                        <div class="progress-bar-glass"><div class="progress-fill" style="width:0%"></div></div>
                        <span class="progress-percent">0%</span>
                    </div>
                </div>
            </div>`;
        chatMessagesEl.appendChild(progressCard);
        chatMessagesEl.scrollTop = chatMessagesEl.scrollHeight;

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
                fileStatuses.forEach(s => s.innerHTML = '<i class="fas fa-check-circle" style="color:#10b981;"></i>');
                progressCard.querySelector('.upload-card-header').innerHTML = '<i class="fas fa-check-circle" style="color:#10b981;"></i><span>ارسال شد</span>';
                messageInput.value = '';
                clearFilePreview();
            } else {
                let errorMsg = 'خطا در ارسال فایل‌ها.';
                try { errorMsg = JSON.parse(xhr.responseText).error || errorMsg; } catch (e) { }
                fileStatuses.forEach(s => s.innerHTML = '<i class="fas fa-times-circle" style="color:#ef4444;"></i>');
                progressCard.querySelector('.upload-card-header').innerHTML = `<i class="fas fa-times-circle" style="color:#ef4444;"></i><span>${errorMsg}</span>`;
            }
            setTimeout(() => {
                progressCard.style.opacity = '0';
                progressCard.style.transition = 'opacity 0.3s';
                setTimeout(() => progressCard.remove(), 300);
            }, 3000);
        });

        xhr.addEventListener('error', () => {
            fileStatuses.forEach(s => s.innerHTML = '<i class="fas fa-times-circle" style="color:#ef4444;"></i>');
            progressCard.querySelector('.upload-card-header').innerHTML = '<i class="fas fa-times-circle" style="color:#ef4444;"></i><span>خطای شبکه</span>';
            setTimeout(() => progressCard.remove(), 3000);
        });

        xhr.open('POST', '/api/chat/send-file');
        xhr.send(formData);
    }

    /**
     * Creates a new support chat with the selected support unit and subject.
     */
    function createNewChat() {
        if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
            window.showToast?.("در حال برقراری ارتباط...", "info");
            return;
        }
        const unitRadio = document.querySelector('input[name="chatUnit"]:checked');
        const subjectInput = document.getElementById('chatName');
        const unitId = unitRadio?.value;
        const subject = subjectInput?.value.trim();

        if (!unitId || !subject) {
            window.showToast?.('لطفاً واحد و موضوع را وارد کنید.', 'warning');
            return;
        }
        if (subject.length < 5) {
            window.showToast?.('موضوع باید حداقل ۵ کاراکتر باشد.', 'error');
            return;
        }
        connection.invoke("CreateSupportChat", { Subject: subject, SupportUnitId: parseInt(unitId) });
    }

    // ========================================================================
    // File handling (preview & drag‑and‑drop)
    // ========================================================================

    /**
     * Handles file selection from the hidden file input.
     * @param {Event} e - The change event.
     */
    function handleFileSelect(e) {
        const files = Array.from(e.target.files || []);
        if (files.length) {
            selectedFiles = selectedFiles.concat(files).slice(0, 5);
            if (files.length > 5) window.showToast?.('حداکثر ۵ فایل می‌توانید انتخاب کنید.', 'warning');
            renderFilePreview();
        }
    }

    /**
     * Renders the selected file preview chips below the message input.
     */
    function renderFilePreview() {
        if (!filePreview) return;
        filePreview.innerHTML = '';
        selectedFiles.forEach((file, index) => {
            const item = document.createElement('div');
            item.className = 'file-preview-item';
            item.innerHTML = `<i class="fas fa-file"></i><span>${file.name}</span><span class="remove-file" data-index="${index}"><i class="fas fa-times"></i></span>`;
            filePreview.appendChild(item);
        });
    }

    /**
     * Clears the current file selection and preview area.
     */
    function clearFilePreview() {
        selectedFiles = [];
        if (filePreview) filePreview.innerHTML = '';
        if (fileInput) fileInput.value = '';
    }

    // ========================================================================
    // Notifications
    // ========================================================================

    /**
     * Displays a floating notification for a new message in a non‑active chat.
     * @param {string} chatId - The chat ID.
     * @param {Object} message - The message object.
     */
    function showNotification(chatId, message) {
        if (!notificationContainer) return;
        const chat = chats.find(c => c.id === chatId);
        if (!chat) return;

        const notification = document.createElement('div');
        notification.className = 'chat-notification';
        notification.dataset.chatId = chatId;
        const time = new Date().toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit' });

        notification.innerHTML = `
            <div class="notification-avatar">${chat.name.charAt(0)}</div>
            <div class="notification-content">
                <div class="notification-header"><span class="notification-title">${chat.name}</span><span class="notification-time">${time}</span></div>
                <div class="notification-message">${message.text}</div>
                <div class="notification-sender">${message.senderName}</div>
            </div>
            <button class="notification-close"><i class="fas fa-times"></i></button>`;

        notification.addEventListener('click', e => {
            if (!e.target.closest('.notification-close')) {
                setActiveChat(chatId);
                notification.remove();
            }
        });
        notification.querySelector('.notification-close').addEventListener('click', e => {
            e.stopPropagation();
            notification.remove();
        });

        notificationContainer.appendChild(notification);
        setTimeout(() => notification.remove(), 5000);
    }

    // ========================================================================
    // End chat
    // ========================================================================

    /**
     * Shows a confirmation modal and ends the specified chat if confirmed.
     * @param {string} chatId - The chat ID.
     */
    function requestEndChat(chatId) {
        const chat = chats.find(c => c.id === chatId);
        if (!chat || chat.status !== 'responding') return;

        const modalHtml = `
            <div class="modal fade" id="confirmEndChatModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content glass-modal">
                        <div class="modal-header"><h5 class="modal-title">پایان گفتگو</h5><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>
                        <div class="modal-body text-center">
                            <div class="mb-3" style="font-size:2.5rem;color:#ef4444;"><i class="fas fa-exclamation-triangle"></i></div>
                            <p class="fw-bold">آیا مطمئن هستید که می‌خواهید این گفتگو را پایان دهید؟</p>
                            <p class="text-secondary small">پس از پایان، امکان ارسال پیام جدید وجود ندارد و وضعیت به «تکمیل شده» تغییر می‌کند.</p>
                        </div>
                        <div class="modal-footer"><button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">انصراف</button><button type="button" class="btn btn-danger" id="confirmEndBtn">پایان گفتگو</button></div>
                    </div>
                </div>
            </div>`;

        const existingModal = document.getElementById('confirmEndChatModal');
        if (existingModal) existingModal.remove();

        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modalEl = document.getElementById('confirmEndChatModal');
        const modal = new bootstrap.Modal(modalEl);
        modal.show();

        document.getElementById('confirmEndBtn').addEventListener('click', async () => {
            modal.hide();
            try {
                await connection.invoke("EndChat", chatId);
            } catch (err) {
                window.showToast?.("خطا در پایان گفتگو.", 'error');
            }
        });

        modalEl.addEventListener('hidden.bs.modal', () => modalEl.remove());
    }

    // ========================================================================
    // Initial chat loading from server‑rendered DOM
    // ========================================================================

    /**
     * Reads the existing chat list from the DOM (pre‑rendered by Razor)
     * and populates the local `chats` array before the SignalR connection is established.
     */
    function loadInitialChatsFromDOM() {
        const items = chatListEl.querySelectorAll('.chat-item');
        if (items.length === 0) return;

        chats = [];
        items.forEach(item => {
            const uniqId = item.dataset.chatUniqId;
            if (!uniqId) return;
            const title = item.querySelector('.chat-title')?.textContent || '';
            const lastMessage = item.querySelector('.chat-last-message')?.textContent || '';
            const statusDot = item.querySelector('.status-dot');
            const statusText = item.querySelector('.chat-item-status span:last-child')?.textContent || '';
            const time = item.querySelector('.chat-time')?.textContent || '';
            const unitEl = document.getElementById('activeChatUnit');
            const unitName = unitEl?.querySelector('span')?.textContent || '';
            const unitIconClass = unitEl?.querySelector('i')?.className || 'fas fa-tools';
            const status = statusDot?.classList.contains('completed') ? 'completed' : 'responding';
            chats.push({
                id: uniqId,
                name: title,
                status,
                lastMessage,
                time,
                unread: 0,
                createdAt: '',
                unitName,
                unitIconClass,
                messages: []
            });
        });
        renderChatList();
        if (activeChatId && chats.some(c => c.id === activeChatId)) {
            setActiveChat(activeChatId);
        } else if (chats.length > 0) {
            setActiveChat(chats[0].id);
        }
    }

    // ========================================================================
    // Drag & drop overlay
    // ========================================================================
    let dragOverlay;

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
    createDragOverlay();

    document.addEventListener('dragover', e => {
        e.preventDefault();
        dragOverlay.classList.add('active');
    });

    document.addEventListener('dragleave', e => {
        if (!e.relatedTarget || e.relatedTarget.nodeName === 'HTML') {
            dragOverlay.classList.remove('active');
        }
    });

    document.addEventListener('drop', e => {
        e.preventDefault();
        dragOverlay.classList.remove('active');
        const files = Array.from(e.dataTransfer.files);
        if (files.length) {
            selectedFiles = selectedFiles.concat(files).slice(0, 5);
            if (files.length > 5) window.showToast?.('حداکثر ۵ فایل می‌توانید انتخاب کنید.', 'warning');
            renderFilePreview();
        }
    });

    // ========================================================================
    // Event listeners
    // ========================================================================
    messageForm?.addEventListener('submit', e => {
        e.preventDefault();
        if (typingActive) {
            typingActive = false;
            connection?.invoke("StopTypingUser", activeChatId).catch(() => { });
        }
        sendMessage(messageInput.value, selectedFiles);
    });

    const newChatForm = document.getElementById('newChatForm');
    if (newChatForm) {
        newChatForm.addEventListener('submit', function (e) {
            e.preventDefault();
            createNewChat();
        });
    }

    createChatBtn?.addEventListener('click', createNewChat);

    messageInput?.addEventListener('keydown', e => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            if (typingActive) {
                typingActive = false;
                connection?.invoke("StopTypingUser", activeChatId).catch(() => { });
            }
            sendMessage(messageInput.value, selectedFiles);
        }
    });

    messageInput?.addEventListener('input', () => {
        if (!activeChatId || !connection) return;
        const hasText = messageInput.value.trim().length > 0;
        if (hasText && !typingActive) {
            typingActive = true;
            connection.invoke("StartTypingUser", activeChatId).catch(() => { });
        } else if (!hasText && typingActive) {
            typingActive = false;
            connection.invoke("StopTypingUser", activeChatId).catch(() => { });
        }
    });

    attachBtn?.addEventListener('click', () => fileInput.click());
    fileInput?.addEventListener('change', handleFileSelect);

    filePreview?.addEventListener('click', e => {
        if (e.target.closest('.remove-file')) {
            const index = e.target.closest('.remove-file').dataset.index;
            selectedFiles.splice(index, 1);
            renderFilePreview();
        }
    });

    endChatBtn?.addEventListener('click', () => {
        if (activeChatId) requestEndChat(activeChatId);
    });

    // ========================================================================
    // Automatic connection on page load
    // ========================================================================
    (async function attemptConnection() {
        const token = window.accessToken || sessionStorage.getItem('accessToken');
        if (token) {
            window.accessToken = token;
            await startMainConnection(token);
        } else {
            const newToken = await renewSignalRToken();
            if (newToken) {
                await startMainConnection(newToken);
            } else {
                window.showToast?.("لطفاً وارد حساب کاربری خود شوید.", "warning");
            }
        }
    })();

    if (chatListEl?.children.length > 0) {
        loadInitialChatsFromDOM();
        updateChatHeader();
    } 
})();