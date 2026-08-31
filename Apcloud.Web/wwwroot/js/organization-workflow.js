(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const grid = document.querySelector("[data-workflow-grid]");
    const modalElement = document.querySelector("[data-workflow-modal]");
    const form = modalElement?.querySelector("[data-workflow-form]");
    if (!grid || !modalElement || !form || !window.apcloudApi) return;
    if (modalElement.parentElement !== document.body) document.body.append(modalElement);
    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = ui?.Modal?.getOrCreateInstance(modalElement);
    const fields = {
      id: form.querySelector("[data-workflow-id]"), module: form.querySelector("[data-workflow-module]"),
      category: form.querySelector("[data-workflow-category]"), subject: form.querySelector("[data-workflow-subject]"),
      name: form.querySelector("[data-workflow-name]"), code: form.querySelector("[data-workflow-code]"),
      active: form.querySelector("[data-workflow-active]"), levels: form.querySelector("[data-workflow-levels]")
    };
    const title = form.querySelector("[data-workflow-title]");
    const save = form.querySelector("[data-workflow-save]");
    const saveLabel = form.querySelector("[data-save-label]");
    const saving = form.querySelector("[data-saving]");
    const templates = Object.fromEntries([...form.querySelectorAll("[data-template-group]")].map((group) =>
      [group.dataset.templateGroup, { title: group.querySelector("[data-template-title]"), message: group.querySelector("[data-template-message]") }]));
    let roles = [];
    let optionsLoaded = false;
    let editing = false;

    const property = (record, name) => Object.entries(record ?? {})
      .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.[1];
    const items = (payload) => Array.isArray(payload) ? payload : (property(payload, "data") ?? []);
    const option = (value, text, code = null) => {
      const result = document.createElement("option"); result.value = String(value); result.textContent = text;
      if (code) result.dataset.code = code; return result;
    };
    const notify = (type, message, heading) => window.apcloudNotifications?.[type]?.(message, heading);

    const loadOptions = async () => {
      if (optionsLoaded) return;
      const [modulesPayload, rolesPayload, categoriesPayload] = await Promise.all([
        window.apcloudApi.json("workflow-setup/options/modules"),
        window.apcloudApi.json("workflow-setup/options/roles"),
        window.apcloudApi.json("workflow-setup/options/subject-categories")
      ]);
      fields.module.replaceChildren(option("", "Select module"));
      items(modulesPayload).forEach((item) => fields.module.append(option(
        property(item, "id"), property(item, "name"), property(item, "code"))));
      fields.category.replaceChildren(option("", "Select category"));
      items(categoriesPayload).forEach((item) => fields.category.append(option(
        property(item, "code"), `${property(item, "name")} (${property(item, "code")})`)));
      roles = items(rolesPayload);
      optionsLoaded = true;
    };

    const loadSubjects = async (categoryCode, selectedId = null) => {
      fields.subject.replaceChildren(option("", "Module-wide default", "DEFAULT"));
      fields.subject.disabled = !categoryCode;
      if (!categoryCode) return;
      const payload = await window.apcloudApi.json(
        `workflow-setup/options/subjects?categoryCode=${encodeURIComponent(categoryCode)}`);
      items(payload).forEach((item) => fields.subject.append(option(
        property(item, "id"), `${property(item, "name")} (${property(item, "code")})`, property(item, "code"))));
      fields.subject.value = selectedId == null ? "" : String(selectedId);
      fields.subject.disabled = false;
    };

    const roleSelect = (selected, alternate = false) => {
      const select = document.createElement("select"); select.className = "form-select";
      select.required = !alternate;
      select.append(option("", alternate ? "No alternate role" : "Select primary role"));
      roles.forEach((role) => select.append(option(property(role, "id"), property(role, "name"))));
      select.value = selected == null ? "" : String(selected);
      return select;
    };

    const reindexLevels = () => {
      [...fields.levels.children].forEach((row, index) => {
        row.dataset.levelNumber = String(index + 1);
        row.querySelector("[data-level-number]").textContent = `Level ${index + 1}`;
      });
      form.querySelector("[data-level-add]").disabled = fields.levels.children.length >= 5;
      fields.levels.querySelectorAll("[data-level-remove]").forEach((button) =>
        button.disabled = fields.levels.children.length <= 1);
    };

    const addLevel = (level = null) => {
      if (fields.levels.children.length >= 5) return;
      const row = document.createElement("div"); row.className = "border rounded p-3";
      const layout = document.createElement("div"); layout.className = "row g-2 align-items-end";
      const number = document.createElement("div"); number.className = "col-md-2 fw-semibold"; number.dataset.levelNumber = "";
      const primaryWrap = document.createElement("div"); primaryWrap.className = "col-md-4";
      primaryWrap.innerHTML = '<label class="form-label required">Primary approver role</label>';
      const primary = roleSelect(property(level, "primaryApproverRoleId")); primary.dataset.levelPrimary = ""; primaryWrap.append(primary);
      const alternateWrap = document.createElement("div"); alternateWrap.className = "col-md-4";
      alternateWrap.innerHTML = '<label class="form-label">Alternate approver role</label>';
      const alternate = roleSelect(property(level, "alternateApproverRoleId"), true); alternate.dataset.levelAlternate = ""; alternateWrap.append(alternate);
      const action = document.createElement("div"); action.className = "col-md-2 text-end";
      const remove = document.createElement("button"); remove.type = "button"; remove.className = "btn btn-outline-danger";
      remove.textContent = "Remove"; remove.dataset.levelRemove = "";
      remove.addEventListener("click", () => { row.remove(); reindexLevels(); }); action.append(remove);
      layout.append(number, primaryWrap, alternateWrap, action); row.append(layout); fields.levels.append(row); reindexLevels();
    };

    const selectedCode = (select) => select.selectedOptions[0]?.dataset.code || "";
    const generateCode = () => {
      const parts = [selectedCode(fields.module), fields.category.value, selectedCode(fields.subject) || "DEFAULT"].filter(Boolean);
      fields.code.value = parts.join(".").toUpperCase();
    };

    const setTemplates = (record = null) => {
      templates.pending.title.value = property(record, "pendingNotificationTitle") ?? "{Reference} requires approval";
      templates.pending.message.value = property(record, "pendingNotificationMessage") ?? "{Reference} is waiting for level {Level} approval.";
      templates.approved.title.value = property(record, "approvedNotificationTitle") ?? "{Reference} was approved";
      templates.approved.message.value = property(record, "approvedNotificationMessage") ?? "{Reference} completed its approval workflow.";
      templates.rejected.title.value = property(record, "rejectedNotificationTitle") ?? "{Reference} was rejected";
      templates.rejected.message.value = property(record, "rejectedNotificationMessage") ?? "{Reference} was rejected at level {Level}.";
    };

    const showEditor = async (id = null) => {
      try {
        await loadOptions();
        const record = id == null ? null : await window.apcloudApi.json(`workflow-setup/${encodeURIComponent(id)}`);
        editing = record != null; form.reset(); form.classList.remove("was-validated"); fields.levels.replaceChildren();
        fields.id.value = property(record, "id") ?? "";
        fields.module.value = property(record, "applicationModuleId") ?? "";
        fields.category.value = property(record, "subjectType") ?? "";
        await loadSubjects(fields.category.value, property(record, "subjectTypeListItemId"));
        fields.name.value = property(record, "name") ?? "";
        fields.code.value = property(record, "workflowCode") ?? "";
        fields.active.checked = record ? property(record, "isActive") !== false : true;
        fields.module.disabled = false; fields.category.disabled = false;
        fields.subject.disabled = !fields.category.value; fields.code.disabled = false;
        const levels = property(record, "levels") ?? [];
        (levels.length ? levels : [null]).forEach(addLevel);
        setTemplates(record); title.textContent = editing ? "Edit approval workflow" : "Add approval workflow";
        saveLabel.textContent = editing ? "Save changes" : "Create workflow";
        if (!editing) generateCode();
        modal?.show();
      } catch (error) { notify("error", error.message || "The workflow form could not be loaded.", "Unable to load workflow"); }
    };

    fields.module.addEventListener("change", generateCode);
    fields.category.addEventListener("change", async () => {
      try { await loadSubjects(fields.category.value); generateCode(); }
      catch (error) { notify("error", error.message, "Unable to load workflow types"); }
    });
    fields.subject.addEventListener("change", generateCode);
    fields.code.addEventListener("input", () => { fields.code.value = fields.code.value.toUpperCase(); });
    form.querySelector("[data-level-add]").addEventListener("click", () => addLevel());
    document.querySelectorAll("[data-workflow-add]").forEach((button) => button.addEventListener("click", () => showEditor()));

    grid.addEventListener("server-grid:action", async (event) => {
      const { action, id, record } = event.detail ?? {};
      if (action === "edit") return showEditor(id);
      if (action !== "delete" || !id) return;
      if (!window.confirm(`Deactivate ${property(record, "name") || "this workflow"}? New submissions will no longer use it.`)) return;
      try {
        await window.apcloudApi.json(`workflow-setup/${encodeURIComponent(id)}`, { method: "DELETE" });
        notify("success", "The workflow was deactivated.", "Workflow deactivated");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) { notify("error", error.message, "Unable to deactivate workflow"); }
    });

    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      if (!form.checkValidity()) { form.classList.add("was-validated"); return; }
      const levels = [...fields.levels.children].map((row, index) => ({
        levelNumber: index + 1,
        primaryApproverRoleId: Number(row.querySelector("[data-level-primary]").value),
        alternateApproverRoleId: Number(row.querySelector("[data-level-alternate]").value) || null
      }));
      if (levels.some((level) => level.primaryApproverRoleId === level.alternateApproverRoleId)) {
        notify("warning", "Primary and alternate roles must differ at every level.", "Check approval levels"); return;
      }
      const request = {
        applicationModuleId: Number(fields.module.value), workflowCode: fields.code.value.trim(),
        subjectType: fields.category.value, subjectTypeListItemId: Number(fields.subject.value) || null,
        name: fields.name.value.trim(), isActive: fields.active.checked, levels,
        pendingNotificationTitle: templates.pending.title.value.trim(), pendingNotificationMessage: templates.pending.message.value.trim(),
        approvedNotificationTitle: templates.approved.title.value.trim(), approvedNotificationMessage: templates.approved.message.value.trim(),
        rejectedNotificationTitle: templates.rejected.title.value.trim(), rejectedNotificationMessage: templates.rejected.message.value.trim()
      };
      save.disabled = true; saveLabel.classList.add("d-none"); saving.classList.remove("d-none");
      try {
        const id = fields.id.value;
        await window.apcloudApi.json(id ? `workflow-setup/${encodeURIComponent(id)}` : "workflow-setup",
          { method: id ? "PUT" : "POST", body: request });
        modal?.hide(); notify("success", id ? "The workflow was updated." : "The workflow was created.", "Workflow saved");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) { notify("error", error.message || "The workflow could not be saved.", "Unable to save workflow"); }
      finally { save.disabled = false; saveLabel.classList.remove("d-none"); saving.classList.add("d-none"); }
    });
  });
})();
