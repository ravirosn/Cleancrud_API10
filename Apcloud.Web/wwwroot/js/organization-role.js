(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const grid = document.querySelector("[data-role-grid]");
    const modalElement = document.querySelector("[data-role-modal]");
    const form = modalElement?.querySelector("[data-role-form]");
    if (!grid || !modalElement || !form || !window.apcloudApi) return;

    if (modalElement.parentElement !== document.body) document.body.append(modalElement);

    const bootstrapUi = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = bootstrapUi?.Modal?.getOrCreateInstance(modalElement);
    const idInput = form.querySelector("[data-role-id]");
    const nameInput = form.querySelector("[data-role-name]");
    const activeInput = form.querySelector("[data-role-active]");
    const title = form.querySelector("[data-role-modal-title]");
    const saveButton = form.querySelector("[data-role-save]");
    const saveLabel = form.querySelector("[data-role-save-label]");
    const saveProgress = form.querySelector("[data-role-save-progress]");

    const property = (record, name) => {
      if (!record || typeof record !== "object") return undefined;
      const entry = Object.entries(record)
        .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0);
      return entry?.[1];
    };

    const notifySuccess = (message) =>
      window.apcloudNotifications?.success(message, "Role saved");
    const notifyError = (message) =>
      window.apcloudNotifications?.error(message, "Unable to save role");

    const showEditor = (record = null) => {
      form.reset();
      form.classList.remove("was-validated");
      const id = property(record, "id");
      idInput.value = id == null ? "" : String(id);
      nameInput.value = property(record, "name") ?? "";
      activeInput.checked = record ? property(record, "isActive") !== false : true;
      title.textContent = id == null ? "Add role" : "Edit role";
      saveLabel.textContent = id == null ? "Add role" : "Save changes";
      modal?.show();
      modalElement.addEventListener("shown.bs.modal", () => nameInput.focus(), { once: true });
    };

    document.querySelectorAll("[data-role-add]").forEach((button) => {
      button.addEventListener("click", () => showEditor());
    });

    grid.addEventListener("server-grid:action", async (event) => {
      const { action, id, record } = event.detail ?? {};
      if (action === "edit") {
        showEditor(record);
        return;
      }

      if (action !== "delete" || !id) return;
      const roleName = property(record, "name") || "this role";
      if (!window.confirm(`Deactivate ${roleName}? Users assigned only to this role will lose its module access.`)) {
        return;
      }

      try {
        await window.apcloudApi.json(`roles/${encodeURIComponent(id)}`, { method: "DELETE" });
        window.apcloudNotifications?.success(`${roleName} was deactivated.`, "Role deleted");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        window.apcloudNotifications?.error(error.message || "The role could not be deleted.", "Unable to delete role");
      }
    });

    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      if (!form.checkValidity()) {
        form.classList.add("was-validated");
        return;
      }

      const id = idInput.value;
      const request = {
        name: nameInput.value.trim(),
        isActive: activeInput.checked
      };

      saveButton.disabled = true;
      saveLabel.classList.add("d-none");
      saveProgress.classList.remove("d-none");

      try {
        await window.apcloudApi.json(id ? `roles/${encodeURIComponent(id)}` : "roles", {
          method: id ? "PUT" : "POST",
          body: request
        });
        modal?.hide();
        notifySuccess(id ? "The role was updated." : "The role was added.");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        notifyError(error.message || "The role could not be saved.");
      } finally {
        saveButton.disabled = false;
        saveLabel.classList.remove("d-none");
        saveProgress.classList.add("d-none");
      }
    });
  });
})();

