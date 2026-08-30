(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const grid = document.querySelector("[data-module-grid]");
    const modalElement = document.querySelector("[data-module-modal]");
    const form = modalElement?.querySelector("[data-module-form]");
    if (!grid || !modalElement || !form || !window.apcloudApi) return;

    if (modalElement.parentElement !== document.body) document.body.append(modalElement);

    const bootstrapUi = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = bootstrapUi?.Modal?.getOrCreateInstance(modalElement);
    const fields = {
      id: form.querySelector("[data-module-id]"),
      code: form.querySelector("[data-module-code]"),
      name: form.querySelector("[data-module-name]"),
      description: form.querySelector("[data-module-description]"),
      icon: form.querySelector("[data-module-icon]"),
      displayOrder: form.querySelector("[data-module-display-order]"),
      isActive: form.querySelector("[data-module-active]")
    };
    const title = form.querySelector("[data-module-modal-title]");
    const saveButton = form.querySelector("[data-module-save]");
    const saveLabel = form.querySelector("[data-module-save-label]");
    const saveProgress = form.querySelector("[data-module-save-progress]");

    const property = (record, name) => {
      if (!record || typeof record !== "object") return undefined;
      const entry = Object.entries(record)
        .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0);
      return entry?.[1];
    };

    const showEditor = (record = null) => {
      form.reset();
      form.classList.remove("was-validated");
      const id = property(record, "id");
      fields.id.value = id == null ? "" : String(id);
      fields.code.value = property(record, "code") ?? "";
      fields.name.value = property(record, "name") ?? "";
      fields.description.value = property(record, "description") ?? "";
      fields.icon.value = property(record, "icon") ?? "";
      fields.displayOrder.value = String(property(record, "displayOrder") ?? 0);
      fields.isActive.checked = record ? property(record, "isActive") !== false : true;
      title.textContent = id == null ? "Add module" : "Edit module";
      saveLabel.textContent = id == null ? "Add module" : "Save changes";
      modal?.show();
      modalElement.addEventListener("shown.bs.modal", () => fields.code.focus(), { once: true });
    };

    document.querySelectorAll("[data-module-add]").forEach((button) => {
      button.addEventListener("click", () => showEditor());
    });

    fields.code.addEventListener("input", () => {
      const position = fields.code.selectionStart;
      fields.code.value = fields.code.value.toUpperCase();
      fields.code.setSelectionRange(position, position);
    });

    grid.addEventListener("server-grid:action", async (event) => {
      const { action, id, record } = event.detail ?? {};
      if (action === "edit") {
        showEditor(record);
        return;
      }

      if (action !== "delete" || !id) return;
      const moduleName = property(record, "name") || "this module";
      if (!window.confirm(`Deactivate ${moduleName}? It will no longer be available to assigned roles.`)) {
        return;
      }

      try {
        await window.apcloudApi.json(`modules/${encodeURIComponent(id)}`, { method: "DELETE" });
        window.apcloudNotifications?.success(`${moduleName} was deactivated.`, "Module deleted");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        window.apcloudNotifications?.error(
          error.message || "The module could not be deleted.",
          "Unable to delete module");
      }
    });

    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      if (!form.checkValidity()) {
        form.classList.add("was-validated");
        return;
      }

      const id = fields.id.value;
      const request = {
        code: fields.code.value.trim(),
        name: fields.name.value.trim(),
        description: fields.description.value.trim() || null,
        icon: fields.icon.value.trim() || null,
        displayOrder: Number(fields.displayOrder.value),
        isActive: fields.isActive.checked
      };

      saveButton.disabled = true;
      saveLabel.classList.add("d-none");
      saveProgress.classList.remove("d-none");

      try {
        await window.apcloudApi.json(id ? `modules/${encodeURIComponent(id)}` : "modules", {
          method: id ? "PUT" : "POST",
          body: request
        });
        modal?.hide();
        window.apcloudNotifications?.success(
          id ? "The module was updated." : "The module was added.",
          "Module saved");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        window.apcloudNotifications?.error(
          error.message || "The module could not be saved.",
          "Unable to save module");
      } finally {
        saveButton.disabled = false;
        saveLabel.classList.remove("d-none");
        saveProgress.classList.add("d-none");
      }
    });
  });
})();

