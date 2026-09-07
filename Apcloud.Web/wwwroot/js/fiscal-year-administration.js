(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const grid = document.querySelector("[data-fiscal-year-grid]");
    const modalElement = document.querySelector("[data-fiscal-year-modal]");
    const form = modalElement?.querySelector("[data-fiscal-year-form]");
    if (!grid || !modalElement || !form || !window.apcloudApi) return;
    if (modalElement.parentElement !== document.body) document.body.append(modalElement);

    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = ui?.Modal?.getOrCreateInstance(modalElement);
    const property = (record, name) => Object.entries(record || {}).find(([key]) => key.toLowerCase() === name.toLowerCase())?.[1];
    const notify = (type, message, title) => window.apcloudNotifications?.[type]?.(message, title);
    const fields = {
      name: form.querySelector("[data-fiscal-year-name]"), start: form.querySelector("[data-fiscal-year-start]"),
      end: form.querySelector("[data-fiscal-year-end]"), raPrefix: form.querySelector("[data-fiscal-year-ra-prefix]"),
      paPrefix: form.querySelector("[data-fiscal-year-pa-prefix]"), nextRa: form.querySelector("[data-fiscal-year-next-ra]"),
      nextPa: form.querySelector("[data-fiscal-year-next-pa]"), active: form.querySelector("[data-fiscal-year-active]"),
      closed: form.querySelector("[data-fiscal-year-closed]")
    };
    const closeContainer = form.querySelector("[data-fiscal-year-close-container]");
    const closeWarning = form.querySelector("[data-fiscal-year-close-warning]");
    let editingId = null;
    const permissions = new Set();
    const permissionCode = {
      create: "FiscalYear.Create", edit: "FiscalYear.Edit", close: "FiscalYear.Close", delete: "FiscalYear.Delete"
    };

    const loadPermissions = async () => {
      const rows = await window.apcloudApi.json("permissions/me?moduleCode=ORGANIZATION&menuController=Organization&menuAction=FiscalYears");
      rows.forEach((row) => permissions.add(property(row, "code")));
      document.querySelectorAll("[data-fiscal-year-add]").forEach((button) =>
        button.classList.toggle("d-none", !permissions.has(permissionCode.create)));
      const actions = [];
      if (permissions.has(permissionCode.edit) || permissions.has(permissionCode.close)) actions.push("edit");
      if (permissions.has(permissionCode.delete)) actions.push("delete");
      grid.dispatchEvent(new CustomEvent("server-grid:set-actions", { detail: { actions } }));
    };

    const showEditor = (record = null) => {
      if (property(record, "isClosed") === true) {
        notify("warning", "Closed fiscal years are read-only.", "Fiscal year is closed");
        return;
      }
      editingId = record ? property(record, "id") : null;
      if (!editingId && !permissions.has(permissionCode.create)) return;
      if (editingId && !permissions.has(permissionCode.edit) && !permissions.has(permissionCode.close)) return;
      form.reset(); form.classList.remove("was-validated");
      fields.end.setCustomValidity("");
      fields.name.value = property(record, "displayName") || "";
      fields.start.value = property(record, "startDate") || "";
      fields.end.value = property(record, "endDate") || "";
      fields.end.min = fields.start.value;
      fields.raPrefix.value = property(record, "raPrefix") || "";
      fields.paPrefix.value = property(record, "paPrefix") || "";
      fields.nextRa.value = property(record, "nextRaNumber") || "";
      fields.nextPa.value = property(record, "nextPaNumber") || "";
      fields.active.checked = property(record, "isActive") === true;
      fields.closed.checked = false;
      const canEdit = permissions.has(permissionCode.edit);
      const canClose = editingId && permissions.has(permissionCode.close);
      [fields.name, fields.start, fields.end, fields.raPrefix, fields.paPrefix, fields.nextRa, fields.nextPa, fields.active]
        .forEach((field) => { field.disabled = !!editingId && !canEdit; });
      closeContainer.classList.toggle("d-none", !canClose);
      closeWarning.classList.add("d-none");
      modalElement.querySelector("[data-fiscal-year-title]").textContent = record ? "Edit fiscal year" : "Add fiscal year";
      modalElement.querySelector("[data-fiscal-year-save-label]").textContent = record ? "Save changes" : "Add fiscal year";
      modal?.show();
    };

    document.querySelectorAll("[data-fiscal-year-add]").forEach((button) => button.addEventListener("click", () => showEditor()));
    fields.active.addEventListener("change", () => { if (fields.active.checked) fields.closed.checked = false; closeWarning.classList.add("d-none"); });
    fields.closed.addEventListener("change", () => {
      if (fields.closed.checked) fields.active.checked = false;
      closeWarning.classList.toggle("d-none", !fields.closed.checked);
    });
    fields.start.addEventListener("change", () => { fields.end.min = fields.start.value; });

    grid.addEventListener("server-grid:action", (event) => {
      const { action, record } = event.detail ?? {};
      if (!record) return;
      if (action === "edit") return showEditor(record);
      if (action !== "delete") return;
      if (property(record, "isActive") === true || property(record, "isClosed") === true) {
        notify("warning", "Only a draft fiscal year can be deleted.", "Fiscal year cannot be deleted");
        return;
      }
      if (!window.confirm(`Delete draft fiscal year "${property(record, "displayName")}"?`)) return;
      window.apcloudApi.json(`organization/fiscal-years/${property(record, "id")}`, { method: "DELETE" })
        .then(() => { notify("success", "The draft fiscal year was deleted.", "Fiscal year deleted"); grid.dispatchEvent(new CustomEvent("server-grid:reload")); })
        .catch((error) => notify("error", error.message || "The fiscal year could not be deleted.", "Unable to delete fiscal year"));
    });

    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      fields.end.setCustomValidity(fields.start.value && fields.end.value && fields.end.value < fields.start.value ? "End date must not precede start date." : "");
      if (!form.checkValidity()) { form.classList.add("was-validated"); return; }
      if (fields.closed.checked && !window.confirm("Close this fiscal year permanently? This cannot be undone.")) return;
      const save = form.querySelector("[data-fiscal-year-save]");
      save.disabled = true; form.querySelector("[data-fiscal-year-save-label]").classList.add("d-none"); form.querySelector("[data-fiscal-year-saving]").classList.remove("d-none");
      try {
        const closing = editingId && fields.closed.checked;
        await window.apcloudApi.json(closing ? `organization/fiscal-years/${editingId}/close` : editingId ? `organization/fiscal-years/${editingId}` : "organization/fiscal-years", {
          method: closing ? "POST" : editingId ? "PUT" : "POST",
          body: { displayName: fields.name.value.trim(), startDate: fields.start.value, endDate: fields.end.value,
            raPrefix: fields.raPrefix.value.trim(), paPrefix: fields.paPrefix.value.trim(), nextRaNumber: fields.nextRa.value.trim(),
            nextPaNumber: fields.nextPa.value.trim(), isActive: fields.active.checked, isClosed: fields.closed.checked }
        });
        modal?.hide(); notify("success", fields.closed.checked ? "The fiscal year was closed." : editingId ? "The fiscal year was updated." : "The fiscal year was created.", "Fiscal year saved");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) { notify("error", error.message || "The fiscal year could not be saved.", "Unable to save fiscal year"); }
      finally { save.disabled = false; form.querySelector("[data-fiscal-year-save-label]").classList.remove("d-none"); form.querySelector("[data-fiscal-year-saving]").classList.add("d-none"); }
    });

    loadPermissions().catch(() => {
      grid.dispatchEvent(new CustomEvent("server-grid:set-actions", { detail: { actions: [] } }));
    });
  });
})();
