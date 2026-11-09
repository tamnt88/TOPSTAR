(function ($) {
    "use strict";
    const table = document.getElementById("brandShipmentTable");
    if (!table || !$.fn.DataTable) return;
    const dataTable = $(table).DataTable(TopstarLists.adapt($(table), {
        pageLength: 10,
        lengthMenu: [[10, 20, 50], ["10 dòng", "20 dòng", "50 dòng"]],
        ordering: true, autoWidth: false, order: [[0, "asc"]],
        columnDefs: [{ targets: [0, 5], orderable: false, searchable: false }, { targets: 0, width: "54px" }, { targets: 4, width: "120px" }, { targets: 5, width: "260px" }],
        dom: '<"brand-shipment-dt-toolbar"f l>t<"brand-shipment-dt-footer"ip>',
        language: { search: "", searchPlaceholder: "Tìm theo KOC, chiến dịch hoặc mã vận đơn", lengthMenu: "Hiển thị _MENU_", emptyTable: "Chưa có lượt giao hàng.", zeroRecords: "Không tìm thấy vận đơn phù hợp.", info: "Hiển thị _START_–_END_ trong _TOTAL_ lượt giao", infoEmpty: "Hiển thị 0 lượt giao", infoFiltered: "(lọc từ _MAX_ lượt giao)", paginate: { first: "Đầu", previous: "‹", next: "›", last: "Cuối" } },
        drawCallback: function () { const api=this.api(),start=api.page.info().start;api.column(0,{page:"current"}).nodes().each(function(cell,index){cell.innerHTML=start+index+1;}); }
    }));
    document.querySelectorAll("[data-shipment-filter]").forEach(function (button) {
        button.addEventListener("click", function () {
            document.querySelectorAll("[data-shipment-filter]").forEach(function (item) { item.classList.remove("is-active"); });
            button.classList.add("is-active");
            const value = button.dataset.shipmentFilter || "";
            const pattern = value === "preparation"
                ? "^(Chưa tạo vận đơn|Chờ xuất kho)$"
                : value === "exception"
                    ? "^(Có sự cố|Đã hoàn hàng)$"
                : (value ? "^" + $.fn.dataTable.util.escapeRegex(value) + "$" : "");
            dataTable.column(4).search(pattern, true, false).draw();
        });
    });

    document.querySelectorAll(".js-shipment-tracking-form").forEach(function (form) {
        const modal = form.closest(".brand-shipment-modal");
        const trackingPanel = modal && modal.querySelector('[id^="tracking-"]');
        if (!trackingPanel) return;
        trackingPanel.prepend(form);
        const processPanel = modal.querySelector('[id^="process-"]');
        if (processPanel && !processPanel.querySelector("form")) {
            processPanel.innerHTML = '<div class="brand-shipment-modal-guide"><i class="fa-solid fa-route"></i> Cập nhật checkpoint tại tab <strong>Hành trình</strong>.</div>';
        }
    });

    function clearForm(form) {
        form.reset();
        form.querySelectorAll(".is-invalid").forEach(function (node) { node.classList.remove("is-invalid"); });
        form.querySelectorAll("[data-error-for]").forEach(function (node) { node.classList.remove("is-visible"); node.querySelector("b").textContent = ""; });
    }
    function error(form, field, message) {
        const control = field.closest(".brand-shipment-modal-field").querySelector(":scope > div");
        const messageNode = form.querySelector(`[data-error-for="${field.name}"]`);
        field.classList.toggle("is-invalid", Boolean(message)); control.classList.toggle("is-invalid", Boolean(message));
        messageNode.classList.toggle("is-visible", Boolean(message)); messageNode.querySelector("b").textContent = message || "";
        return !message;
    }
    $(document).on("click", ".js-open-dispatch", function () {
        const form = document.getElementById("brandDispatchForm"); clearForm(form);
        document.getElementById("brandDispatchId").value = this.dataset.id || "";
        document.getElementById("brandDispatchKoc").textContent = "Vận đơn giao cho " + (this.dataset.koc || "KOC");
    });
    $(document).on("click", ".js-open-evidence", function () {
        const form = document.getElementById("brandEvidenceForm"); clearForm(form);
        document.getElementById("brandEvidenceId").value = this.dataset.id || "";
        document.getElementById("brandEvidenceKoc").textContent = "Bằng chứng giao nhận với " + (this.dataset.koc || "KOC");
    });
    $("#brandDispatchForm").on("submit", function (event) {
        const carrier = document.getElementById("brandCarrier"), tracking = document.getElementById("brandTrackingCode");
        const carrierOk = error(this, carrier, carrier.value.trim().length < 2 ? "Vui lòng nhập nhà vận chuyển từ 2 đến 100 ký tự." : "");
        const trackingOk = error(this, tracking, tracking.value.trim().length < 2 ? "Vui lòng nhập mã vận đơn từ 2 đến 100 ký tự." : "");
        if (!(carrierOk & trackingOk)) { event.preventDefault(); this.querySelector(".is-invalid")?.focus(); }
    });
    $("#brandEvidenceForm").on("submit", function (event) {
        const file = document.getElementById("brandEvidenceFile"), caption = document.getElementById("brandEvidenceCaption");
        const selected = file.files && file.files[0]; let fileMessage = "";
        if (!selected) fileMessage = "Vui lòng chọn ảnh bằng chứng.";
        else if (!["image/jpeg", "image/png", "image/webp"].includes(selected.type)) fileMessage = "Ảnh phải có định dạng JPG, PNG hoặc WEBP.";
        else if (selected.size > 5 * 1024 * 1024) fileMessage = "Dung lượng ảnh không được vượt quá 5 MB.";
        const fileOk = error(this, file, fileMessage);
        const captionOk = error(this, caption, caption.value.trim().length < 10 ? "Vui lòng nhập mô tả ảnh từ 10 đến 500 ký tự." : "");
        if (!(fileOk & captionOk)) { event.preventDefault(); this.querySelector(".is-invalid")?.focus(); }
    });
    $("#brandCarrier, #brandTrackingCode, #brandEvidenceCaption").on("input", function () { this.classList.remove("is-invalid"); this.closest(".brand-shipment-modal-field").querySelector(":scope > div").classList.remove("is-invalid"); });
    $("#brandEvidenceFile").on("change", function () { this.classList.remove("is-invalid"); this.closest(".brand-shipment-modal-field").querySelector(":scope > div").classList.remove("is-invalid"); });
    $(document).off("submit.workspaceShipment", ".js-shipment-dispatch-form").on("submit.workspaceShipment", ".js-shipment-dispatch-form", function (event) {
        const carrier=this.elements.carrier,tracking=this.elements.trackingCode;
        const okCarrier=error(this,carrier,carrier.value.trim().length<2?"Vui lòng nhập nhà vận chuyển từ 2 đến 100 ký tự.":"");
        const okTracking=error(this,tracking,tracking.value.trim().length<2?"Vui lòng nhập mã vận đơn từ 2 đến 100 ký tự.":"");
        if(!(okCarrier&okTracking)){event.preventDefault();this.querySelector(".is-invalid")?.focus();}
    });
    $(document).off("submit.workspaceShipment", ".js-shipment-evidence-form").on("submit.workspaceShipment", ".js-shipment-evidence-form", function (event) {
        const file=this.elements.evidenceFile,caption=this.elements.caption,selected=file.files&&file.files[0];let message="";
        if(!selected)message="Vui lòng chọn ảnh bằng chứng.";else if(!["image/jpeg","image/png","image/webp"].includes(selected.type))message="Ảnh phải có định dạng JPG, PNG hoặc WEBP.";else if(selected.size>5*1024*1024)message="Dung lượng ảnh không được vượt quá 5 MB.";
        const okFile=error(this,file,message),okCaption=error(this,caption,caption.value.trim().length<10?"Vui lòng nhập mô tả ảnh từ 10 đến 500 ký tự.":"");
        if(!(okFile&okCaption)){event.preventDefault();this.querySelector(".is-invalid")?.focus();}
    });
    $(document).off("submit.workspaceShipment", ".js-shipment-resolve-form").on("submit.workspaceShipment", ".js-shipment-resolve-form", function (event) {
        const decision=this.elements.decision,note=this.elements.note,value=note.value.trim();
        const okDecision=error(this,decision,decision.value?"":"Vui lòng chọn hướng xử lý sự cố.");
        const okNote=error(this,note,value.length<10||value.length>1000?"Vui lòng nhập ghi chú xử lý từ 10 đến 1.000 ký tự.":"");
        if(!(okDecision&okNote)){event.preventDefault();this.querySelector(".is-invalid")?.focus();}
    });
    $(document).off("submit.workspaceShipment", ".js-shipment-tracking-form").on("submit.workspaceShipment", ".js-shipment-tracking-form", function (event) {
        const code=this.elements.eventCode,description=this.elements.description,location=this.elements.location,occurred=this.elements.occurredAtLocal;
        const okCode=error(this,code,code.value?"":"Vui lòng chọn trạng thái hành trình.");
        const text=description.value.trim(),okDescription=error(this,description,text.length<2||text.length>1000?"Vui lòng nhập mô tả từ 2 đến 1.000 ký tự.":"");
        const okLocation=error(this,location,location.value.trim().length>300?"Địa điểm không được vượt quá 300 ký tự.":"");
        const okOccurred=error(this,occurred,occurred.value?"":"Vui lòng chọn thời điểm cập nhật.");
        if(!(okCode&okDescription&okLocation&okOccurred)){event.preventDefault();this.querySelector(".is-invalid")?.focus();}
    });
    function renderFile(picker) {
        const input=picker.querySelector(".brand-file-picker-input"),preview=picker.querySelector(".brand-file-picker-preview"),file=input.files&&input.files[0];
        if(!file){picker.classList.remove("has-file");preview.querySelector("img").removeAttribute("src");return;}
        picker.classList.add("has-file");preview.querySelector("strong").textContent=file.name;preview.querySelector("small").textContent=(file.size/1024/1024).toLocaleString("vi-VN",{maximumFractionDigits:2})+" MB";
        const reader=new FileReader();reader.onload=function(e){preview.querySelector("img").src=e.target.result;};reader.readAsDataURL(file);
        picker.classList.remove("is-invalid");picker.closest(".brand-shipment-modal-field").querySelector("[data-error-for]")?.classList.remove("is-visible");
    }
    $(document).on("change", ".brand-file-picker-input", function(){renderFile(this.closest(".brand-file-picker"));});
    $(document).on("click", ".brand-file-picker-drop", function(){this.closest(".brand-file-picker").querySelector("input").click();});
    $(document).on("click", ".brand-file-picker-preview button", function(){const picker=this.closest(".brand-file-picker");picker.querySelector("input").value="";renderFile(picker);});
    $(document).on("dragover dragenter", ".brand-file-picker-drop", function(event){event.preventDefault();this.closest(".brand-file-picker").classList.add("is-dragging");});
    $(document).on("dragleave drop", ".brand-file-picker-drop", function(event){event.preventDefault();const picker=this.closest(".brand-file-picker");picker.classList.remove("is-dragging");if(event.type==="drop"&&event.originalEvent.dataTransfer.files.length){const transfer=new DataTransfer();transfer.items.add(event.originalEvent.dataTransfer.files[0]);picker.querySelector("input").files=transfer.files;renderFile(picker);}});
})(jQuery);
