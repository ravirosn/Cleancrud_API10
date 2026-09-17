(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const bootstrapUi = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    if (!bootstrapUi?.Modal) return;

    const modalElement = document.createElement("div");
    modalElement.className = "modal modal-blur fade apcloud-confirmation-modal";
    modalElement.tabIndex = -1;
    modalElement.setAttribute("aria-labelledby", "apcloudConfirmationTitle");
    modalElement.setAttribute("aria-describedby", "apcloudConfirmationMessage");
    modalElement.setAttribute("aria-hidden", "true");
    modalElement.innerHTML = `
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-body p-4 text-center">
            <div class="apcloud-confirmation-icon" aria-hidden="true">!</div>
            <h2 class="modal-title mt-3" id="apcloudConfirmationTitle" data-confirmation-title>Confirm action</h2>
            <p class="text-secondary mt-2 mb-0 apcloud-confirmation-message" id="apcloudConfirmationMessage" data-confirmation-message></p>
            <div class="alert alert-danger text-start mt-3 mb-0 d-none" role="alert" data-confirmation-error></div>
          </div>
          <div class="modal-footer justify-content-center gap-2">
            <button type="button" class="btn btn-outline-secondary" data-confirmation-cancel>Cancel</button>
            <button type="button" class="btn btn-primary" data-confirmation-confirm>
              <span data-confirmation-confirm-label>Confirm</span>
              <span class="d-none" data-confirmation-confirm-progress>
                <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Processing…
              </span>
            </button>
          </div>
        </div>
      </div>`;

    const pageLoader = document.createElement("div");
    pageLoader.className = "apcloud-page-loading-overlay d-none";
    pageLoader.setAttribute("role", "status");
    pageLoader.setAttribute("aria-live", "polite");
    pageLoader.innerHTML = `
      <div class="apcloud-page-loading-card">
        <span class="spinner-border text-primary" aria-hidden="true"></span>
        <strong data-page-loading-message>Please wait…</strong>
      </div>`;

    document.body.append(modalElement, pageLoader);

    const modal = bootstrapUi.Modal.getOrCreateInstance(modalElement, {
      backdrop: "static",
      keyboard: false
    });
    const elements = {
      title: modalElement.querySelector("[data-confirmation-title]"),
      message: modalElement.querySelector("[data-confirmation-message]"),
      error: modalElement.querySelector("[data-confirmation-error]"),
      cancel: modalElement.querySelector("[data-confirmation-cancel]"),
      confirm: modalElement.querySelector("[data-confirmation-confirm]"),
      confirmLabel: modalElement.querySelector("[data-confirmation-confirm-label]"),
      confirmProgress: modalElement.querySelector("[data-confirmation-confirm-progress]"),
      loadingMessage: pageLoader.querySelector("[data-page-loading-message]")
    };
    let activeRequest = null;

    const setBusy = (busy, loadingText) => {
      elements.confirm.disabled = busy;
      elements.cancel.disabled = busy;
      elements.confirmLabel.classList.toggle("d-none", busy);
      elements.confirmProgress.classList.toggle("d-none", !busy);
      elements.loadingMessage.textContent = loadingText || "Please wait…";
      pageLoader.classList.toggle("d-none", !busy);
      document.body.classList.toggle("apcloud-page-busy", busy);
      modalElement.setAttribute("aria-busy", busy ? "true" : "false");
    };

    const errorMessage = error => error?.message || "The action could not be completed.";

    elements.cancel.addEventListener("click", () => modal.hide());
    elements.confirm.addEventListener("click", async () => {
      const request = activeRequest;
      if (!request || request.busy) return;
      elements.error.classList.add("d-none");

      if (typeof request.options.onConfirm !== "function") {
        request.confirmed = true;
        modal.hide();
        return;
      }

      request.busy = true;
      setBusy(true, request.options.loadingText);
      try {
        request.result = await request.options.onConfirm();
        request.confirmed = true;
        request.successMessage = typeof request.options.successMessage === "function"
          ? request.options.successMessage(request.result)
          : request.options.successMessage;
        setBusy(false);
        modal.hide();
      } catch (error) {
        request.busy = false;
        setBusy(false);
        elements.error.textContent = errorMessage(error);
        elements.error.classList.remove("d-none");
        elements.confirm.focus();
      }
    });

    modalElement.addEventListener("shown.bs.modal", () => {
      [...document.querySelectorAll(".modal-backdrop")].at(-1)
        ?.classList.add("apcloud-confirmation-backdrop");
      elements.confirm.focus();
    });

    modalElement.addEventListener("hidden.bs.modal", () => {
      setBusy(false);
      const request = activeRequest;
      activeRequest = null;
      if (!request) return;
      if (request.confirmed && request.successMessage)
        window.apcloudNotifications?.success(
          request.successMessage,
          request.options.successTitle || "Action completed");
      request.resolve({ confirmed: request.confirmed, result: request.result });
    });

    const show = (options = {}) => {
      if (activeRequest)
        return Promise.reject(new Error("Another confirmation is already open."));
      if (!options.message)
        return Promise.reject(new TypeError("A confirmation message is required."));

      elements.title.textContent = options.title || "Confirm action";
      elements.message.textContent = options.message;
      elements.cancel.textContent = options.cancelText || "Cancel";
      elements.confirmLabel.textContent = options.confirmText || "Confirm";
      elements.confirm.className = `btn ${options.confirmButtonClass || "btn-primary"}`;
      elements.error.classList.add("d-none");
      elements.error.textContent = "";
      setBusy(false);

      return new Promise(resolve => {
        activeRequest = {
          options,
          resolve,
          busy: false,
          confirmed: false,
          result: undefined,
          successMessage: ""
        };
        modal.show();
      });
    };

    window.apcloudConfirmation = Object.freeze({ show });
  });
})();
