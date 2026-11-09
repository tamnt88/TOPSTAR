(function () {
    "use strict";
    const form = document.getElementById("brandDisputeReplyForm");
    if (!form) return;
    const body = document.getElementById("brandReplyBody");
    const evidence = document.getElementById("brandEvidenceUrl");
    function setError(field, message) {
        const error = form.querySelector(`[data-error-for="${field.name}"]`);
        field.classList.toggle("is-invalid", Boolean(message));
        field.closest(".brand-support-control")?.classList.toggle("is-invalid", Boolean(message));
        if (error) { error.classList.toggle("is-visible", Boolean(message)); error.querySelector("b").textContent = message || ""; }
    }
    function validateBody() { const value = body.value.trim(); const message = value.length < 10 ? "Vui lòng nhập nội dung phản hồi từ 10 đến 2.000 ký tự." : ""; setError(body, message); return !message; }
    function validateEvidence() {
        const value = evidence.value.trim(); let message = "";
        if (value) { try { const url = new URL(value); if (!["http:", "https:"].includes(url.protocol)) message = "Link bằng chứng phải bắt đầu bằng http:// hoặc https://."; } catch { message = "Link bằng chứng không đúng định dạng."; } }
        setError(evidence, message); return !message;
    }
    body.addEventListener("input", validateBody); evidence.addEventListener("input", validateEvidence);
    form.addEventListener("submit", function (event) { const valid = validateBody() & validateEvidence(); if (!valid) { event.preventDefault(); form.querySelector(".is-invalid")?.focus(); } });
})();
