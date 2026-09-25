// ============================================================
// Secret Access — Super Admin
// ============================================================

const SECRET_CLICK_COUNT = 5;
const SECRET_CLICK_TIMEOUT = 2000;
const SUPER_ADMIN_SECRET = 'P@ssw0rd'; // ← غيّرها هنا

// ✅ in-memory فقط — يختفي بعد Refresh
let hasAccessInMemory = false;
let clickCount = 0;
let lastClickTime = 0;

// ============================================================
// Access Check
// ============================================================
export function hasSuperAdminAccess() {
    return hasAccessInMemory;
}

export function grantSuperAdminAccess() {
    hasAccessInMemory = true;
}

export function revokeSuperAdminAccess() {
    hasAccessInMemory = false;
}

// ============================================================
// Secret Modal
// ============================================================
export function showSecretModal() {
    const existing = document.getElementById('secretModal');
    if (existing) existing.remove();

    const modal = document.createElement('div');
    modal.id = 'secretModal';
    modal.style.cssText = `
        position: fixed;
        inset: 0;
        background: rgba(18, 16, 13, 0.6);
        z-index: 9999;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 16px;
    `;

    modal.innerHTML = `
        <div style="
            background: #fff;
            border-radius: 14px;
            padding: 24px;
            width: 100%;
            max-width: 380px;
            box-shadow: 0 20px 50px rgba(0, 0, 0, 0.4);
            text-align: center;
        ">
            <div style="font-size: 42px; margin-bottom: 12px;">🔒</div>
            <h3 style="font-size: 18px; font-weight: 800; color: #12433C; margin: 0 0 6px;">وصول خاص</h3>
            <p style="font-size: 12.5px; color: #666; margin: 0 0 18px;">أدخل كلمة السر للوصول للوحة Super Admin</p>

            <form id="secretForm" autocomplete="off">
                <input type="text" id="secretUsername" value="admin" autocomplete="username" style="display:none;" readonly />
                <input type="password" id="secretInput" placeholder="كلمة السر" autocomplete="current-password"
                       style="width: 100%; padding: 12px 14px; border: 1.5px solid #ddd; border-radius: 8px;
                              font-family: inherit; font-size: 15px; text-align: center; letter-spacing: 2px;
                              box-sizing: border-box;" />
            </form>

            <div id="secretError" style="color: #A0432A; font-size: 12px; margin-top: 8px; min-height: 16px; font-weight: 700;"></div>

            <div style="display: flex; gap: 8px; margin-top: 16px;">
                <button type="button" id="secretCancelBtn"
                        style="flex: 1; padding: 10px 16px; border: 1.3px solid #12433C; background: transparent;
                               color: #12433C; border-radius: 7px; font-family: inherit; font-size: 13.5px;
                               font-weight: 600; cursor: pointer;">إلغاء</button>
                <button type="button" id="secretConfirmBtn"
                        style="flex: 1; padding: 10px 16px; border: none; background: #12433C; color: #fff;
                               border-radius: 7px; font-family: inherit; font-size: 13.5px; font-weight: 600;
                               cursor: pointer;">تأكيد</button>
            </div>
        </div>
    `;

    document.body.appendChild(modal);

    const input = document.getElementById('secretInput');
    const confirmBtn = document.getElementById('secretConfirmBtn');
    const cancelBtn = document.getElementById('secretCancelBtn');
    const errorEl = document.getElementById('secretError');

    setTimeout(() => input.focus(), 100);

    const checkSecret = () => {
        const entered = input.value.trim();
        if (entered === SUPER_ADMIN_SECRET) {
            grantSuperAdminAccess();
            modal.remove();

            // ✅ أظهر الزرار مباشرة (من غير reload)
            const googleSection = document.getElementById('googleSignInSection');
            if (googleSection) {
                googleSection.style.display = 'block';
                googleSection.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        } else {
            errorEl.textContent = '❌ كلمة السر غير صحيحة';
            input.value = '';
            input.focus();
        }
    };

    document.getElementById('secretForm').onsubmit = (e) => {
        e.preventDefault();
        checkSecret();
    };

    confirmBtn.onclick = checkSecret;
    cancelBtn.onclick = () => modal.remove();
    modal.onclick = (e) => { if (e.target === modal) modal.remove(); };
}

// ============================================================
// Bind Clicks on Title
// ============================================================
export function bindTitleClicks() {
    const title = document.getElementById('appTitle');
    if (!title) return;

    title.onclick = () => {
        const now = Date.now();

        if (now - lastClickTime > SECRET_CLICK_TIMEOUT) {
            clickCount = 1;
        } else {
            clickCount++;
        }

        lastClickTime = now;

        title.style.transform = 'scale(0.95)';
        setTimeout(() => title.style.transform = 'scale(1)', 100);

        if (clickCount >= SECRET_CLICK_COUNT) {
            clickCount = 0;
            showSecretModal();
        }
    };
}