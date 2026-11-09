(function () {
            var $province = $("#koc-edit-province");
            var $ward = $("#koc-edit-ward");
            var $hasChildren = $("#koc-has-children");
            var $childrenBox = $("#koc-children-box");
            var $childrenList = $("#children-list");

            function toggleChildren() {
                var show = ($hasChildren.val() || "").toLowerCase() === "true";
                $childrenBox.toggle(show);
            }

            function childTemplate(index) {
                return '' +
                    '<div class="row g-2 mb-2 child-row">' +
                    '<div class="col-md-5"><input type="number" class="form-control child-age" name="Input.Children[' + index + '].Age" placeholder="Tuổi" /></div>' +
                    '<div class="col-md-5"><select class="form-select child-gender" name="Input.Children[' + index + '].Gender">' +
                    '<option value="">Giới tính</option><option value="Male">Nam</option><option value="Female">Nữ</option><option value="Other">Khác</option>' +
                    '</select></div>' +
                    '<div class="col-md-2"><button type="button" class="btn btn-outline-danger remove-child remove-child-icon" title="Xóa"><i class="fa-solid fa-trash-can"></i></button></div>' +
                    '</div>';
            }

            function reindexChildren() {
                $childrenList.find('.child-row').each(function (i) {
                    $(this).find('.child-age').attr('name', 'Input.Children[' + i + '].Age');
                    $(this).find('.child-gender').attr('name', 'Input.Children[' + i + '].Gender');
                });
            }

            $hasChildren.on('change', toggleChildren);
            toggleChildren();

            $('#add-child').on('click', function () {
                var index = $childrenList.find('.child-row').length;
                $childrenList.append(childTemplate(index));
            });

            $childrenList.on('click', '.remove-child', function () {
                $(this).closest('.child-row').remove();
                reindexChildren();
            });

            $province.on('change', function () {
                var provinceId = ($province.val() || "").trim();
                $ward.empty().append($('<option/>', { value: '', text: 'Chọn Phường/Xã' }));
                if (!provinceId) return;

                $.get('/Koc/EditProfile?handler=WardsByProvince', { provinceId: provinceId })
                    .done(function (res) {
                        $.each((res && res.data) ? res.data : [], function (_, ward) {
                            $ward.append($('<option/>', { value: ward.id, text: ward.name }));
                        });
                    });
            });

            function syncCategoryChipStyle($checkbox) {
                var $label = $checkbox.closest('label');
                if (!$label.length) return;
                $label.toggleClass('is-selected', $checkbox.is(':checked'));
            }

            $('input[type="checkbox"][name*="SelectedCategoryCodes"]').each(function () {
                syncCategoryChipStyle($(this));
            });

            $(document).on('change', 'input[type="checkbox"][name*="SelectedCategoryCodes"]', function () {
                syncCategoryChipStyle($(this));
            });

            function bindMediaList(listSelector, orderSelector, deleteSelector) {
                var $list = $(listSelector);
                var deleted = [];

                function serializeOrder() {
                    var ids = [];
                    $list.find('.media-item:visible').each(function () {
                        ids.push($(this).data('id'));
                    });
                    $(orderSelector).val(ids.join(','));
                    $(deleteSelector).val(deleted.join(','));
                }

                $list.on('click', '.media-delete-btn', function () {
                    var $item = $(this).closest('.media-item');
                    deleted.push($item.data('id'));
                    $item.remove();
                    serializeOrder();
                });

                $list.on('click', '[data-media-move]', function () {
                    var $item = $(this).closest('.media-item');
                    if ($(this).data('media-move') === 'up') $item.prev('.media-item').before($item);
                    else $item.next('.media-item').after($item);
                    serializeOrder();
                    $item.get(0)?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
                    $(this).trigger('focus');
                });

                serializeOrder();
            }

            bindMediaList('#gallery-list', '#gallery-order-csv', '#gallery-delete-csv');
            bindMediaList('#video-list', '#video-order-csv', '#video-delete-csv');

            var form = document.querySelector('form[enctype="multipart/form-data"]');
            // Each tab is saved independently; fields on other tabs must not
            // prevent this submission after Select2 hides its original select.
            var profileValidator = form && $(form).data('validator');
            if (form) form.dataset.sectionValidation = 'true';
            if (profileValidator) {
                profileValidator.settings.ignore = function (_, field) {
                    var pane = field.closest('.tab-pane');
                    if (pane && !pane.classList.contains('active')) return true;
                    return $(field).is(':hidden') && !field.matches('.select2-hidden-accessible,.ts-date-original');
                };
            }
            function restoreSaveControls() {
                form?.querySelectorAll('button[type="submit"]').forEach(function (button) { button.disabled = false; });
                document.getElementById('koc-upload-progress')?.classList.add('d-none');
            }
            $(form).on('invalid-form.validate', function () {
                restoreSaveControls();
                window.TopstarNotify?.warning('Thông tin chưa hợp lệ. Vui lòng kiểm tra các ô trong mục đang lưu.');
            });
            window.addEventListener('pageshow', restoreSaveControls);
            var warning = document.getElementById('koc-unsaved-warning');
            var servicePriceDisplay = document.getElementById('koc-service-price-display');
            var servicePriceValue = document.getElementById('Input_ServicePriceVnd');
            function syncServicePrice() {
                if (!servicePriceDisplay || !servicePriceValue) return;
                var digits = servicePriceDisplay.value.replace(/\D/g, '').slice(0, 15);
                servicePriceValue.value = digits;
                servicePriceDisplay.value = digits ? Number(digits).toLocaleString('vi-VN') : '';
            }
            servicePriceDisplay && servicePriceDisplay.addEventListener('input', syncServicePrice);
            syncServicePrice();
            var hasUnsavedChanges = false;
            function markUnsaved() {
                hasUnsavedChanges = true;
                warning && warning.classList.remove('d-none');
            }
            form && form.addEventListener('input', markUnsaved);
            form && form.addEventListener('change', markUnsaved);
            form && form.addEventListener('submit', function () {
                hasUnsavedChanges = false;
                warning && warning.classList.add('d-none');
            });
            window.addEventListener('beforeunload', function (event) {
                if (!hasUnsavedChanges) return;
                event.preventDefault();
                event.returnValue = '';
            });

            function formatBytes(bytes) {
                if (bytes < 1024 * 1024) return Math.max(1, Math.round(bytes / 1024)) + ' KB';
                return (bytes / 1024 / 1024).toFixed(1) + ' MB';
            }

            async function optimizeImage(file) {
                if (!file.type.startsWith('image/') || !window.createImageBitmap) return file;
                var bitmap = await createImageBitmap(file);
                var longestSide = Math.max(bitmap.width, bitmap.height);
                if (file.size <= 1.5 * 1024 * 1024 && longestSide <= 1920) {
                    bitmap.close();
                    return file;
                }
                var scale = Math.min(1, 1920 / longestSide);
                var canvas = document.createElement('canvas');
                canvas.width = Math.max(1, Math.round(bitmap.width * scale));
                canvas.height = Math.max(1, Math.round(bitmap.height * scale));
                canvas.getContext('2d').drawImage(bitmap, 0, 0, canvas.width, canvas.height);
                bitmap.close();
                var blob = await new Promise(function (resolve) { canvas.toBlob(resolve, 'image/webp', .86); });
                if (!blob || blob.size >= file.size) return file;
                var baseName = file.name.replace(/\.[^.]+$/, '');
                return new File([blob], baseName + '.webp', { type: 'image/webp', lastModified: Date.now() });
            }

            form && form.querySelectorAll('input[type="file"]').forEach(function (input) {
                input.addEventListener('change', async function () {
                    input.dataset.optimizing = 'true';
                    if ((input.name === 'AvatarFile' || input.name === 'GalleryFiles') && window.DataTransfer) {
                        var transfer = new DataTransfer();
                        for (var sourceFile of Array.from(input.files || [])) {
                            try { transfer.items.add(await optimizeImage(sourceFile)); }
                            catch (_) { transfer.items.add(sourceFile); }
                        }
                        input.files = transfer.files;
                    }
                    input.dataset.optimizing = 'false';
                    var preview = form.querySelector('[data-upload-preview-for="' + input.name + '"]');
                    if (!preview) return;
                    preview.replaceChildren();
                    Array.from(input.files || []).forEach(function (file) {
                        if (file.type.startsWith('image/') || file.type.startsWith('video/')) {
                            var figure = document.createElement('figure');
                            var media = document.createElement(file.type.startsWith('video/') ? 'video' : 'img');
                            var objectUrl = URL.createObjectURL(file);
                            media.src = objectUrl;
                            media.alt = file.name;
                            if (media.tagName === 'VIDEO') media.muted = true;
                            media.addEventListener('loadeddata', function () { URL.revokeObjectURL(objectUrl); }, { once: true });
                            media.addEventListener('load', function () { URL.revokeObjectURL(objectUrl); }, { once: true });
                            var caption = document.createElement('figcaption');
                            caption.textContent = file.name + ' · ' + formatBytes(file.size);
                            figure.append(media, caption);
                            preview.appendChild(figure);
                        } else {
                            var label = document.createElement('span');
                            label.textContent = file.name + ' · ' + formatBytes(file.size);
                            preview.appendChild(label);
                        }
                    });
                });
            });

            form && form.addEventListener('submit', function (event) {
                if (event.defaultPrevented) {
                    restoreSaveControls();
                    return;
                }
                syncServicePrice();
                if (form.querySelector('input[type="file"][data-optimizing="true"]')) {
                    event.preventDefault();
                    window.TopstarNotify?.warning('Ảnh đang được tối ưu. Vui lòng chờ vài giây rồi lưu lại.');
                    return;
                }
                var submitter = event.submitter;
                var section = submitter && submitter.value;
                if (section === 'gallery') {
                    var galleryInput = form.querySelector('input[name="GalleryFiles"]');
                    var hasGalleryFiles = galleryInput && galleryInput.files && galleryInput.files.length > 0;
                    var hasGalleryChanges = Boolean((document.getElementById('gallery-delete-csv')?.value || '').trim())
                        || Boolean(document.querySelector('#gallery-list .media-item'));
                    if (!hasGalleryFiles && !hasGalleryChanges) {
                        event.preventDefault();
                        window.TopstarNotify?.warning('Vui lòng chọn ít nhất một ảnh trước khi lưu thư viện.');
                        return;
                    }
                }
                if (section === 'video') {
                    var videoInput = form.querySelector('input[name="VideoFiles"]');
                    var hasVideoFiles = videoInput && videoInput.files && videoInput.files.length > 0;
                    var hasVideoChanges = Boolean((document.getElementById('video-delete-csv')?.value || '').trim())
                        || Boolean(document.querySelector('#video-list .media-item'));
                    if (!hasVideoFiles && !hasVideoChanges) {
                        event.preventDefault();
                        window.TopstarNotify?.warning('Vui lòng chọn ít nhất một video trước khi lưu.');
                        return;
                    }
                }
                var progressBox = document.getElementById('koc-upload-progress');
                var progressBar = document.getElementById('koc-upload-progressbar');
                var progressText = document.getElementById('koc-upload-percent');
                if (submitter && submitter.name) {
                    var sectionField = form.querySelector('input[data-koc-submit-section]');
                    if (!sectionField) {
                        sectionField = document.createElement('input');
                        sectionField.type = 'hidden';
                        sectionField.dataset.kocSubmitSection = 'true';
                        form.appendChild(sectionField);
                    }
                    sectionField.name = submitter.name;
                    sectionField.value = submitter.value;
                }
                progressBox && progressBox.classList.remove('d-none');
                if (progressBar) progressBar.removeAttribute('value');
                if (progressText) progressText.textContent = 'Đang lưu…';
                form.querySelectorAll('button[type="submit"]').forEach(function (button) { button.disabled = true; });
                // Other delegated validators may cancel the event after this
                // listener. Never leave a cancelled submission looking busy.
                setTimeout(function () {
                    if (event.defaultPrevented) restoreSaveControls();
                }, 0);
            });

        })();
