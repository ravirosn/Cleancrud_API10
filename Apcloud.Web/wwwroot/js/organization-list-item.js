(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const grid = document.querySelector("[data-item-grid]");
    const modalElement = document.querySelector("[data-item-modal]");
    const form = modalElement?.querySelector("[data-item-form]");
    if (!grid || !modalElement || !form || !window.apcloudApi) return;
    if (modalElement.parentElement !== document.body) document.body.append(modalElement);
    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = ui?.Modal?.getOrCreateInstance(modalElement);
    const fields = {
      id: form.querySelector("[data-item-id]"), category: form.querySelector("[data-item-category]"),
      code: form.querySelector("[data-item-code]"), name: form.querySelector("[data-item-name]"),
      description: form.querySelector("[data-item-description]"), order: form.querySelector("[data-item-order]"),
      active: form.querySelector("[data-item-active]")
    };
    const title = form.querySelector("[data-item-title]");
    const save = form.querySelector("[data-item-save]");
    const saveLabel = form.querySelector("[data-item-save-label]");
    const saving = form.querySelector("[data-item-saving]");
    let categoriesLoaded = false;
    const property = (record, name) => Object.entries(record ?? {})
      .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.[1];
    const notify = (type, message, heading) => window.apcloudNotifications?.[type]?.(message, heading);
    const option = (value, text) => { const result = document.createElement("option"); result.value = String(value); result.textContent = text; return result; };
    const loadCategories = async () => {
      if (categoriesLoaded) return;
      const payload = await window.apcloudApi.json("list-items/categories/ddl");
      const categories = Array.isArray(payload) ? payload : (property(payload, "data") ?? []);
      fields.category.replaceChildren(option("", "Select category"));
      categories.forEach((category) => fields.category.append(option(
        property(category, "id"), `${property(category, "name")} (${property(category, "code")})`)));
      categoriesLoaded = true;
    };
    const show = async (record = null) => {
      try {
        await loadCategories(); form.reset(); form.classList.remove("was-validated");
        const id = property(record, "id");
        fields.id.value = id == null ? "" : String(id);
        fields.category.value = String(property(record, "listItemCategoryId") ?? "");
        fields.category.disabled = id != null;
        fields.code.value = property(record, "code") ?? "";
        fields.name.value = property(record, "name") ?? "";
        fields.description.value = property(record, "description") ?? "";
        fields.order.value = String(property(record, "displayOrder") ?? 0);
        fields.active.checked = record ? property(record, "isActive") !== false : true;
        title.textContent = id == null ? "Add list item" : "Edit list item";
        saveLabel.textContent = id == null ? "Add list item" : "Save changes";
        modal?.show(); modalElement.addEventListener("shown.bs.modal", () =>
          (id == null ? fields.category : fields.code).focus(), { once: true });
      } catch (error) { notify("error", error.message || "The editor could not be opened.", "Unable to load list-item form"); }
    };
    document.querySelectorAll("[data-item-add]").forEach((button) => button.addEventListener("click", () => show()));
    fields.code.addEventListener("input", () => { fields.code.value = fields.code.value.toUpperCase(); });
    grid.addEventListener("server-grid:action", async (event) => {
      const { action, id, record } = event.detail ?? {};
      if (action === "edit") return show(record);
      if (action !== "delete" || !id) return;
      const name = property(record, "name") || "this list item";
      if (!window.confirm(`Deactivate ${name}? It will no longer appear in active application selections.`)) return;
      try {
        await window.apcloudApi.json(`list-items/${encodeURIComponent(id)}`, { method: "DELETE" });
        notify("success", `${name} was deactivated.`, "List item deactivated");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) { notify("error", error.message || "The list item could not be deactivated.", "Unable to deactivate list item"); }
    });
    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      if (!form.checkValidity()) { form.classList.add("was-validated"); return; }
      const id = fields.id.value;
      save.disabled = true; saveLabel.classList.add("d-none"); saving.classList.remove("d-none");
      try {
        await window.apcloudApi.json(id ? `list-items/${encodeURIComponent(id)}` : "list-items", {
          method: id ? "PUT" : "POST",
          body: { listItemCategoryId: Number(fields.category.value), code: fields.code.value.trim(),
            name: fields.name.value.trim(), description: fields.description.value.trim() || null,
            displayOrder: Number(fields.order.value), isActive: fields.active.checked }
        });
        modal?.hide(); notify("success", id ? "The list item was updated." : "The list item was created.", "List item saved");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) { notify("error", error.message || "The list item could not be saved.", "Unable to save list item"); }
      finally { save.disabled = false; saveLabel.classList.remove("d-none"); saving.classList.add("d-none"); }
    });
  });
})();
