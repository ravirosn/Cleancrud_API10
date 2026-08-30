(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const grid = document.querySelector("[data-user-grid]");
    const editorElement = document.querySelector("[data-user-modal]");
    const roleElement = document.querySelector("[data-user-roles-modal]");
    const form = editorElement?.querySelector("[data-user-form]");
    const roleForm = roleElement?.querySelector("[data-user-roles-form]");
    if (!grid || !editorElement || !roleElement || !form || !roleForm || !window.apcloudApi) return;

    if (editorElement.parentElement !== document.body) document.body.append(editorElement);
    if (roleElement.parentElement !== document.body) document.body.append(roleElement);
    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const editor = ui?.Modal?.getOrCreateInstance(editorElement);
    const roleModal = ui?.Modal?.getOrCreateInstance(roleElement);
    const field = {
      id: form.querySelector("[data-user-id]"),
      userName: form.querySelector("[data-user-name]"),
      displayName: form.querySelector("[data-user-display-name]"),
      email: form.querySelector("[data-user-email]"),
      contact: form.querySelector("[data-user-contact]"),
      office: form.querySelector("[data-user-office]"),
      department: form.querySelector("[data-user-department]"),
      password: form.querySelector("[data-user-password]"),
      active: form.querySelector("[data-user-active]")
    };
    const title = form.querySelector("[data-user-modal-title]");
    const passwordLabel = form.querySelector("[data-user-password-label]");
    const passwordHint = form.querySelector("[data-user-password-hint]");
    const save = form.querySelector("[data-user-save]");
    const saveLabel = form.querySelector("[data-user-save-label]");
    const saveProgress = form.querySelector("[data-user-save-progress]");
    const roleId = roleForm.querySelector("[data-user-roles-id]");
    const roleSubtitle = roleForm.querySelector("[data-user-roles-subtitle]");
    const roleLoading = roleForm.querySelector("[data-user-roles-loading]");
    const roleList = roleForm.querySelector("[data-user-roles-list]");
    const roleSave = roleForm.querySelector("[data-user-roles-save]");
    let branchesLoaded = false;

    const property = (record, name) => {
      if (!record || typeof record !== "object") return undefined;
      return Object.entries(record)
        .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.[1];
    };
    const asItems = (payload) => Array.isArray(payload)
      ? payload
      : (property(payload, "data") ?? property(payload, "items") ?? []);
    const option = (value, text) => {
      const element = document.createElement("option");
      element.value = String(value);
      element.textContent = text;
      return element;
    };
    const notify = (type, message, titleText) =>
      window.apcloudNotifications?.[type]?.(message, titleText);

    const loadBranches = async () => {
      if (branchesLoaded) return;
      const branches = asItems(await window.apcloudApi.json("users/offices"));
      field.office.replaceChildren(option("", "No office"));
      branches.forEach((branch) => field.office.append(option(
        property(branch, "id"), `${property(branch, "name")} (${property(branch, "code")})`)));
      branchesLoaded = true;
    };

    const loadDepartments = async (officeId, selectedId = null) => {
      field.department.disabled = true;
      field.department.replaceChildren(option("", officeId ? "Loading departments..." : "Select an office first"));
      if (!officeId) return;
      const departments = asItems(await window.apcloudApi.json(
        `users/departments?officeBranchId=${encodeURIComponent(officeId)}`));
      field.department.replaceChildren(option("", "Select department"));
      departments.forEach((department) => field.department.append(option(
        property(department, "id"), `${property(department, "name")} (${property(department, "code")})`)));
      field.department.disabled = false;
      if (selectedId != null) field.department.value = String(selectedId);
    };

    const showEditor = async (record = null) => {
      try {
        await loadBranches();
        form.reset();
        form.classList.remove("was-validated");
        const id = property(record, "id");
        field.id.value = id == null ? "" : String(id);
        field.userName.value = property(record, "userName") ?? "";
        field.displayName.value = property(record, "displayName") ?? "";
        field.email.value = property(record, "email") ?? "";
        field.contact.value = property(record, "contactNumber") ?? "";
        field.office.value = String(property(record, "officeBranchId") ?? "");
        field.active.checked = record ? property(record, "isActive") !== false : true;
        field.password.value = "";
        field.password.required = id == null;
        passwordLabel.classList.toggle("required", id == null);
        passwordHint.textContent = id == null
          ? "At least 12 characters with uppercase, lowercase, number, and special characters."
          : "Leave blank to keep it; replacements need 12 characters with uppercase, lowercase, number, and special characters.";
        await loadDepartments(field.office.value, property(record, "departmentId"));
        title.textContent = id == null ? "Add user" : "Edit user";
        saveLabel.textContent = id == null ? "Add user" : "Save changes";
        editor?.show();
        editorElement.addEventListener("shown.bs.modal", () => field.userName.focus(), { once: true });
      } catch (error) {
        notify("error", error.message || "The editor could not be opened.", "Unable to load user form");
      }
    };

    const showRoles = async (id, record) => {
      roleId.value = String(id);
      roleSubtitle.textContent = property(record, "displayName") || property(record, "userName") || "";
      roleLoading.classList.remove("d-none");
      roleList.classList.add("d-none");
      roleList.replaceChildren();
      roleSave.disabled = true;
      roleModal?.show();
      try {
        const result = await window.apcloudApi.json(`users/${encodeURIComponent(id)}/roles`);
        roleSubtitle.textContent = property(result, "userName") ?? roleSubtitle.textContent;
        const roles = property(result, "roles") ?? [];
        if (!roles.length) {
          const empty = document.createElement("p");
          empty.className = "text-secondary mb-0";
          empty.textContent = "No active roles are available.";
          roleList.append(empty);
        }
        roles.forEach((role) => {
          const label = document.createElement("label");
          label.className = "form-check py-2 border-bottom";
          const checkbox = document.createElement("input");
          checkbox.type = "checkbox";
          checkbox.className = "form-check-input";
          checkbox.value = String(property(role, "roleId"));
          checkbox.checked = property(role, "isAssigned") === true;
          checkbox.dataset.userRole = "";
          const text = document.createElement("span");
          text.className = "form-check-label";
          text.textContent = property(role, "roleName") ?? "Role";
          label.append(checkbox, text);
          roleList.append(label);
        });
        roleLoading.classList.add("d-none");
        roleList.classList.remove("d-none");
        roleSave.disabled = false;
      } catch (error) {
        roleModal?.hide();
        notify("error", error.message || "Roles could not be loaded.", "Unable to load roles");
      }
    };

    document.querySelectorAll("[data-user-add]").forEach((button) =>
      button.addEventListener("click", () => showEditor()));
    field.office.addEventListener("change", () => loadDepartments(field.office.value).catch((error) =>
      notify("error", error.message || "Departments could not be loaded.", "Unable to load departments")));

    grid.addEventListener("server-grid:action", async (event) => {
      const { action, id, record } = event.detail ?? {};
      if (!id) return;
      if (action === "edit") return showEditor(record);
      if (action === "assign-roles") return showRoles(id, record);
      if (action !== "delete") return;
      const name = property(record, "displayName") || property(record, "userName") || "this user";
      if (!window.confirm(`Deactivate ${name}? The user will no longer be able to sign in.`)) return;
      try {
        await window.apcloudApi.json(`users/${encodeURIComponent(id)}`, { method: "DELETE" });
        notify("success", `${name} was deactivated.`, "User deactivated");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        notify("error", error.message || "The user could not be deactivated.", "Unable to deactivate user");
      }
    });

    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      if (!form.checkValidity()) {
        form.classList.add("was-validated");
        return;
      }
      const id = field.id.value;
      const request = {
        userName: field.userName.value.trim(),
        displayName: field.displayName.value.trim() || null,
        email: field.email.value.trim() || null,
        contactNumber: field.contact.value.trim() || null,
        officeBranchId: field.office.value ? Number(field.office.value) : null,
        departmentId: field.department.value ? Number(field.department.value) : null,
        isActive: field.active.checked
      };
      if (!id || field.password.value) request.password = field.password.value;
      save.disabled = true;
      saveLabel.classList.add("d-none");
      saveProgress.classList.remove("d-none");
      try {
        await window.apcloudApi.json(id ? `users/${encodeURIComponent(id)}` : "users", {
          method: id ? "PUT" : "POST", body: request
        });
        editor?.hide();
        notify("success", id ? "The user was updated." : "The user was created. You can now assign roles from the row menu.", "User saved");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        notify("error", error.message || "The user could not be saved.", "Unable to save user");
      } finally {
        save.disabled = false;
        saveLabel.classList.remove("d-none");
        saveProgress.classList.add("d-none");
      }
    });

    roleForm.addEventListener("submit", async (event) => {
      event.preventDefault();
      const id = roleId.value;
      const roleIds = [...roleList.querySelectorAll("[data-user-role]:checked")].map((item) => Number(item.value));
      roleSave.disabled = true;
      try {
        await window.apcloudApi.json(`users/${encodeURIComponent(id)}/roles`, {
          method: "PUT", body: { roleIds }
        });
        roleModal?.hide();
        notify("success", "Role assignments were updated.", "Roles saved");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        notify("error", error.message || "Role assignments could not be saved.", "Unable to save roles");
      } finally {
        roleSave.disabled = false;
      }
    });
  });
})();
