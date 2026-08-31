(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const grid = document.querySelector("[data-role-menu-grid]");
    const modalElement = document.querySelector("[data-role-menu-modal]");
    const form = modalElement?.querySelector("[data-role-menu-form]");
    if (!grid || !modalElement || !form || !window.apcloudApi) return;
    if (modalElement.parentElement !== document.body) document.body.append(modalElement);

    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = ui?.Modal?.getOrCreateInstance(modalElement);
    const fields = {
      role: form.querySelector("[data-role-menu-role]"),
      module: form.querySelector("[data-role-menu-module]"),
      menu: form.querySelector("[data-role-menu-menu]"),
      active: form.querySelector("[data-role-menu-active]")
    };
    const title = form.querySelector("[data-role-menu-title]");
    const save = form.querySelector("[data-role-menu-save]");
    const saveLabel = form.querySelector("[data-role-menu-save-label]");
    const saving = form.querySelector("[data-role-menu-saving]");
    let rolesLoaded = false;
    let editing = false;
    let originalKey = null;

    const property = (record, name) => Object.entries(record ?? {})
      .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.[1];
    const option = (value, text) => {
      const element = document.createElement("option");
      element.value = String(value);
      element.textContent = text;
      return element;
    };
    const payloadItems = (payload) => Array.isArray(payload) ? payload : (property(payload, "data") ?? []);
    const notify = (type, message, heading) => window.apcloudNotifications?.[type]?.(message, heading);

    const loadRoles = async () => {
      if (rolesLoaded) return;
      const roles = payloadItems(await window.apcloudApi.json("role-module-menus/roles"));
      fields.role.replaceChildren(option("", "Select role"));
      roles.forEach((role) => fields.role.append(option(property(role, "id"), property(role, "name"))));
      rolesLoaded = true;
    };

    const loadModules = async (roleId, selectedId = null) => {
      fields.module.replaceChildren(option("", roleId ? "Select module" : "Select role first"));
      fields.menu.replaceChildren(option("", "Select module first"));
      fields.module.disabled = !roleId;
      fields.menu.disabled = true;
      if (!roleId) return;
      const modules = payloadItems(await window.apcloudApi.json(
        `role-module-menus/modules?roleId=${encodeURIComponent(roleId)}`));
      modules.forEach((module) => fields.module.append(option(
        property(module, "id"), `${property(module, "name")} (${property(module, "code")})`)));
      fields.module.value = selectedId == null ? "" : String(selectedId);
      fields.module.disabled = false;
    };

    const loadMenus = async (roleId, moduleId, selectedId = null) => {
      fields.menu.replaceChildren(option("", moduleId ? "Select menu" : "Select module first"));
      fields.menu.disabled = !moduleId;
      if (!roleId || !moduleId) return;
      const menus = payloadItems(await window.apcloudApi.json(
        `role-module-menus/menus?roleId=${encodeURIComponent(roleId)}&moduleId=${encodeURIComponent(moduleId)}`));
      menus.forEach((menu) => {
        const item = option(property(menu, "id"),
          `${property(menu, "hierarchy")} · order ${property(menu, "displayOrder")}${property(menu, "canAssign") === false ? " · assign parent first" : ""}`);
        const selected = String(property(menu, "id")) === String(selectedId);
        item.disabled = (property(menu, "isAssigned") === true || property(menu, "canAssign") === false) && !selected;
        fields.menu.append(item);
      });
      fields.menu.value = selectedId == null ? "" : String(selectedId);
      fields.menu.disabled = false;
    };

    const showEditor = async (record = null) => {
      try {
        editing = record != null;
        form.reset();
        form.classList.remove("was-validated");
        await loadRoles();
        const roleId = property(record, "roleId");
        const moduleId = property(record, "applicationModuleId");
        const menuId = property(record, "moduleMenuId");
        originalKey = editing ? { roleId, moduleId, menuId } : null;
        fields.role.value = roleId == null ? "" : String(roleId);
        fields.role.disabled = false;
        await loadModules(roleId, moduleId);
        await loadMenus(roleId, moduleId, menuId);
        fields.active.checked = record ? property(record, "isActive") !== false : true;
        title.textContent = editing ? "Edit menu assignment" : "Assign menu";
        saveLabel.textContent = editing ? "Save changes" : "Assign menu";
        modal?.show();
        modalElement.addEventListener("shown.bs.modal", () =>
          fields.role.focus(), { once: true });
      } catch (error) {
        notify("error", error.message || "The assignment editor could not be opened.", "Unable to load assignment form");
      }
    };

    document.querySelectorAll("[data-role-menu-add]").forEach((button) =>
      button.addEventListener("click", () => showEditor()));

    fields.role.addEventListener("change", async () => {
      try { await loadModules(fields.role.value); }
      catch (error) { notify("error", error.message, "Unable to load modules"); }
    });
    fields.module.addEventListener("change", async () => {
      try { await loadMenus(fields.role.value, fields.module.value); }
      catch (error) { notify("error", error.message, "Unable to load menus"); }
    });

    grid.addEventListener("server-grid:action", async (event) => {
      const { action, record } = event.detail ?? {};
      if (action === "edit") return showEditor(record);
      if (action !== "delete" || !record) return;
      const roleId = property(record, "roleId");
      const moduleId = property(record, "applicationModuleId");
      const menuId = property(record, "moduleMenuId");
      const label = `${property(record, "menuName")} for ${property(record, "roleName")}`;
      if (!window.confirm(`Deactivate ${label}? The role will no longer see this menu.`)) return;
      try {
        await window.apcloudApi.json(`role-module-menus/${roleId}/${moduleId}/${menuId}`, { method: "DELETE" });
        notify("success", `${label} was deactivated.`, "Assignment deactivated");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        notify("error", error.message || "The assignment could not be deactivated.", "Unable to deactivate assignment");
      }
    });

    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      if (!form.checkValidity()) { form.classList.add("was-validated"); return; }
      const request = {
        roleId: Number(fields.role.value),
        applicationModuleId: Number(fields.module.value),
        moduleMenuId: Number(fields.menu.value),
        isActive: fields.active.checked
      };
      save.disabled = true;
      saveLabel.classList.add("d-none");
      saving.classList.remove("d-none");
      try {
        const path = editing
          ? `role-module-menus/${originalKey.roleId}/${originalKey.moduleId}/${originalKey.menuId}`
          : "role-module-menus";
        await window.apcloudApi.json(path, { method: editing ? "PUT" : "POST", body: request });
        modal?.hide();
        notify("success", editing ? "The assignment was updated." : "The menu was assigned.", "Assignment saved");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        notify("error", error.message || "The assignment could not be saved.", "Unable to save assignment");
      } finally {
        save.disabled = false;
        saveLabel.classList.remove("d-none");
        saving.classList.add("d-none");
      }
    });
  });
})();
