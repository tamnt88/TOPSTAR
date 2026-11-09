(function () {
    "use strict";

    const form = document.getElementById("kocDisputeReplyForm");
    if (!form) return;

    const body = document.getElementById("disputeReplyBody");
    const evidence = document.getElementById("disputeEvidenceUrl");
    const evidenceFile = document.getElementById("disputeEvidenceFile");

    function showError(field, message) {
        const error = form.querySelector(`[data-error-for="${field.name}"]`);
        field.classList.toggle("is-invalid", Boolean(message));
        if (error) {
            error.textContent = message || "";
            error.classList.toggle("is-visible", Boolean(message));
        }
    }

    function validateBody() {
        const value = body.value.trim();
        const message = value.length < 2
            ? "Vui lòng nhập nội dung phản hồi từ 2 ký tự."
            : "";
        showError(body, message);
        return !message;
    }

    function validateEvidence() {
        const value = evidence.value.trim();
        let message = "";
        if (value) {
            try {
                const url = new URL(value);
                if (url.protocol !== "http:" && url.protocol !== "https:") {
                    message = "Link bằng chứng phải bắt đầu bằng http:// hoặc https://.";
                }
            } catch {
                message = "Link bằng chứng không đúng định dạng.";
            }
        }
        showError(evidence, message);
        return !message;
    }

    function validateEvidenceFile() {
        const file = evidenceFile?.files?.[0];
        let message = "";
        if (file && evidence.value.trim()) message = "Chỉ chọn tệp hoặc nhập liên kết bằng chứng.";
        else if (file && file.size > 5 * 1024 * 1024) message = "Tệp bằng chứng không được vượt quá 5 MB.";
        else if (file && !/\.(jpe?g|png|webp|pdf)$/i.test(file.name)) message = "Chỉ hỗ trợ JPG, PNG, WebP hoặc PDF.";
        if (evidenceFile) showError(evidenceFile, message);
        return !message;
    }

    body.addEventListener("input", validateBody);
    evidence.addEventListener("input", validateEvidence);
    evidenceFile?.addEventListener("change", validateEvidenceFile);
    form.addEventListener("submit", function (event) {
        const valid = validateBody() & validateEvidence() & validateEvidenceFile();
        if (!valid) {
            event.preventDefault();
            form.querySelector(".is-invalid")?.focus();
        }
    });
})();
