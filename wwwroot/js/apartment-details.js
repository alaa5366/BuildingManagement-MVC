// ============================================================
// i18n Helper
// ============================================================
const i18nT = (k, ...a) =>
    String((window.I18N && window.I18N[k]) ?? k)
        .replace(/\{(\d+)\}/g, (m, i) => (a[i] ?? m));

// ============================================================
// Apartment Details Modal — Phase 24.3
// ============================================================

let apartmentDetailsModal = null;

function getOrCreateModal() {
    if (apartmentDetailsModal) return apartmentDetailsModal;

    // إنشاء Modal container
    let modalEl = document.getElementById('apartmentDetailsModal');
    if (!modalEl) {
        modalEl = document.createElement('div');
        modalEl.id = 'apartmentDetailsModal';
        modalEl.className = 'modal fade';
        modalEl.tabIndex = -1;
        modalEl.innerHTML = `
            <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
                <div class="modal-content">
                    <div class="modal-header bg-dark text-white">
                        <h5 class="modal-title">${i18nT('Js_AptDetails_Title')}</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body" id="apartmentDetailsContent">
                        <div class="text-center py-5">
                            <div class="spinner-border text-primary"></div>
                            <p class="text-muted mt-2">${i18nT('Js_Loading')}</p>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">${i18nT('Js_Close')}</button>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(modalEl);
    }

    apartmentDetailsModal = new bootstrap.Modal(modalEl);
    return apartmentDetailsModal;
}

async function openApartmentDetails(aptId) {
    const modal = getOrCreateModal();
    const content = document.getElementById('apartmentDetailsContent');

    // Reset + show loading
    content.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary"></div>
            <p class="text-muted mt-2">${i18nT('Js_Loading')}</p>
        </div>
    `;

    modal.show();

    try {
        const res = await fetch(`/AdminHome/ApartmentDetails?aptId=${encodeURIComponent(aptId)}`, {
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const html = await res.text();
        content.innerHTML = html;
    } catch (err) {
        console.error('[Details] Failed:', err);
        content.innerHTML = `
            <div class="alert alert-danger">
                ${i18nT('Js_LoadFailed', err.message)}
            </div>
        `;
    }
}
    function enterResidentWallet(aptId) {
    if (!confirm('⚠️ هل تريد الدخول على محفظة الساكن؟\n\nملاحظات:\n• هتتصفح كأنك الساكن\n• تقدر ترجع لحسابك في أي وقت من الشريط الأحمر\n• الحركة دي هتتسجل في السجل')) {
        return;
    }

    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Impersonation/EnterResidentWallet';
    form.innerHTML = `
        <input type="hidden" name="aptId" value="${aptId}" />
        <input type="hidden" name="__RequestVerificationToken" value="${getAntiForgeryToken()}" />
    `;
    document.body.appendChild(form);
    form.submit();
}

// ============================================================
// Quick Actions
// ============================================================

function sendWelcomeFromDetails(aptId) {
    // Redirect لـ POST /AdminApts/GenerateWelcome
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/AdminApts/GenerateWelcome';
    form.innerHTML = `
        <input type="hidden" name="aptId" value="${aptId}" />
        <input type="hidden" name="validityHours" value="24" />
        <input type="hidden" name="__RequestVerificationToken" value="${getAntiForgeryToken()}" />
    `;
    document.body.appendChild(form);
    form.submit();
}

function quickQrFromDetails(aptId) {
    // Redirect لـ /AdminApts
    window.location.href = '/AdminApts#quickQr_' + aptId;
}

function impersonateResident(aptId) {
    if (!confirm(i18nT('Js_ConfirmImpersonate'))) {
        return;
    }

    // أنشئ form POST
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Impersonation/Enter';

    const aptInput = document.createElement('input');
    aptInput.type = 'hidden';
    aptInput.name = 'aptId';
    aptInput.value = aptId;
    form.appendChild(aptInput);

    const tokenInput = document.createElement('input');
    tokenInput.type = 'hidden';
    tokenInput.name = '__RequestVerificationToken';
    tokenInput.value = getAntiForgeryToken();
    form.appendChild(tokenInput);

    document.body.appendChild(form);
    form.submit();
}
function getAntiForgeryToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}
// ============================================================
// ✅ فتح شقة
// ============================================================
function openApartment(aptId, aptNumber) {
    if (!confirm(i18nT('Js_ConfirmOpenApt', aptNumber))) {
        return;
    }

    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/AdminApts/OpenApartment';
    form.innerHTML = `
        <input type="hidden" name="aptId" value="${aptId}" />
        <input type="hidden" name="__RequestVerificationToken" value="${getAntiForgeryToken()}" />
    `;
    document.body.appendChild(form);
    form.submit();
}

// ============================================================
// ✅ إغلاق شقة
// ============================================================
function closeApartment(aptId, aptNumber) {
    const reason = prompt(
        i18nT('Js_PromptCloseApt', aptNumber),
        ''
    );

    if (reason === null) return;  // المستخدم ضغط Cancel

    if (!confirm(i18nT('Js_ConfirmCloseApt', aptNumber))) {
        return;
    }

    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/AdminApts/CloseApartment';
    form.innerHTML = `
        <input type="hidden" name="aptId" value="${aptId}" />
        <input type="hidden" name="reason" value="${reason || ''}" />
        <input type="hidden" name="__RequestVerificationToken" value="${getAntiForgeryToken()}" />
    `;
    document.body.appendChild(form);
    form.submit();
}

// ============================================================
// ✅ تعطيل شقة
// ============================================================
function disableApartment(aptId, aptNumber) {
    const reason = prompt(
        i18nT('Js_PromptDisableApt', aptNumber),
        ''
    );

    if (reason === null) return;  // المستخدم ضغط Cancel

    if (!confirm(i18nT('Js_ConfirmDisableApt', aptNumber))) {
        return;
    }

    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/AdminApts/DisableApartment';
    form.innerHTML = `
        <input type="hidden" name="aptId" value="${aptId}" />
        <input type="hidden" name="reason" value="${reason || ''}" />
        <input type="hidden" name="__RequestVerificationToken" value="${getAntiForgeryToken()}" />
    `;
    document.body.appendChild(form);
    form.submit();
}

// ============================================================
// ✅ تفعيل شقة
// ============================================================
function enableApartment(aptId, aptNumber) {
    const reason = prompt(
        i18nT('Js_PromptEnableApt', aptNumber),
        ''
    );

    if (reason === null) return;

    if (!confirm(i18nT('Js_ConfirmEnableApt', aptNumber))) {
        return;
    }

    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/AdminApts/EnableApartment';
    form.innerHTML = `
        <input type="hidden" name="aptId" value="${aptId}" />
        <input type="hidden" name="reason" value="${reason || ''}" />
        <input type="hidden" name="__RequestVerificationToken" value="${getAntiForgeryToken()}" />
    `;
    document.body.appendChild(form);
    form.submit();
}
// ============================================================
// Initialize tooltips
// ============================================================
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
        new bootstrap.Tooltip(el);
    });
});