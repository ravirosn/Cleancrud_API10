(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const grid = document.querySelector("[data-category-grid]");
    const modalElement = document.querySelector("[data-category-modal]");
    const form = modalElement?.querySelector("[data-category-form]");
    if (!grid || !modalElement || !form || !window.apcloudApi) return;
    if (modalElement.parentElement !== document.body) document.body.append(modalElement);
    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = ui?.Modal?.getOrCreateInstance(modalElement);
    const fields = {
      id: form.querySelector("[data-category-id]"), code: form.querySelector("[data-category-code]"),
      name: form.querySelector("[data-category-name]"), description: form.querySelector("[data-category-description]"),
      active: form.querySelector("[data-category-active]")
    };
    const title = form.querySelector("[data-category-title]");
    const save = form.querySelector("[data-category-save]");
    const saveLabel = form.querySelector("[data-category-save-label]");
    const saving = form.querySelector("[data-category-saving]");
    const property = (record, name) => Object.entries(record ?? {})
      .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.[1];
    const notify = (type, message, heading) => window.apcloudNotifications?.[type]?.(message, heading);
    const show = (record = null) => {
      form.reset(); form.classList.remove("was-validated");
      const id = property(record, "id");
      fields.id.value = id == null ? "" : String(id);
      fields.code.value = property(record, "code") ?? "";
      fields.name.value = property(record, "name") ?? "";
      fields.description.value = property(record, "description") ?? "";
      fields.active.checked = record ? property(record, "isActive") !== false : true;
      title.textContent = id == null ? "Add category" : "Edit category";
      saveLabel.textContent = id == null ? "Add category" : "Save changes";
      modal?.show();
      modalElement.addEventListener("shown.bs.modal", () => fields.code.focus(), { once: true });
    };
    document.querySelectorAll("[data-category-add]").forEach((button) => button.addEventListener("click", () => show()));
    fields.code.addEventListener("input", () => { fields.code.value = fields.code.value.toUpperCase(); });
    grid.addEventListener("server-grid:action", async (event) => {
      const { action, id, record } = event.detail ?? {};
      if (action === "edit") return show(record);
      if (action !== "delete" || !id) return;
      const name = property(record, "name") || "this category";
      if (!window.confirm(`Deactivate ${name}? Active list items must be deactivated first.`)) return;
      try {
        await window.apcloudApi.json(`list-items/categories/${encodeURIComponent(id)}`, { method: "DELETE" });
        notify("success", `${name} was deactivated.`, "Category deactivated");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) { notify("error", error.message || "The category could not be deactivated.", "Unable to deactivate category"); }
    });
    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      if (!form.checkValidity()) { form.classList.add("was-validated"); return; }
      const id = fields.id.value;
      save.disabled = true; saveLabel.classList.add("d-none"); saving.classList.remove("d-none");
      try {
        await window.apcloudApi.json(id ? `list-items/categories/${encodeURIComponent(id)}` : "list-items/categories", {
          method: id ? "PUT" : "POST",
          body: { code: fields.code.value.trim(), name: fields.name.value.trim(),
            description: fields.description.value.trim() || null, isActive: fields.active.checked }
        });
        modal?.hide(); notify("success", id ? "The category was updated." : "The category was created.", "Category saved");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) { notify("error", error.message || "The category could not be saved.", "Unable to save category"); }
      finally { save.disabled = false; saveLabel.classList.remove("d-none"); saving.classList.add("d-none"); }
    });
  });
})();
