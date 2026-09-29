/**
 * Apartment Details Modal — Phase 24.3
 */

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
                        <h5 class="modal-title">📋 تفاصيل الشقة</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body" id="apartmentDetailsContent">
                        <div class="text-center py-5">
                            <div class="spinner-border text-primary"></div>
                            <p class="text-muted mt-2">جاري التحميل...</p>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">إغلاق</button>
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
            <p class="text-muted mt-2">جاري التحميل...</p>
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
                ❌ فشل تحميل البيانات: ${err.message}
            </div>
        `;
    }
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
    if (!confirm('⚠️ هل تريد الدخول كساكن هذه الشقة؟\n\nملاحظات:\n• سيتم تسجيل خروجك من حساب الأدمن\n• يمكنك الرجوع لحسابك في أي وقت من الشريط الأحمر أعلى الصفحة\n• سيتم تسجيل هذا الإجراء في السجل')) {
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
    if (!confirm(`🔓 هل تريد فتح شقة ${aptNumber}؟\n\nملاحظة:\n• الشقة هتضاف للمصروفات الشهرية\n• هتقدر تصدر فواتير ليها\n• لو فيها دفعات قديمة، هتتحسب`)) {
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
        `🔒 إغلاق شقة ${aptNumber}\n\nاكتب سبب الإغلاق (اختياري):\n\nمثال: الساكن مسافر - شقة فاضية - صيانة`,
        ''
    );

    if (reason === null) return;  // المستخدم ضغط Cancel

    if (!confirm(`⚠️ تأكيد إغلاق شقة ${aptNumber}؟\n\n• مش هتتضمن في توزيع المصروفات\n• مش هيتم إصدار فواتير ليها`)) {
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
// Initialize tooltips
// ============================================================
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
        new bootstrap.Tooltip(el);
    });
});