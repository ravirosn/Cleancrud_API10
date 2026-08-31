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
    const modulesContainer = form.querySelector("[data-role-modules]");
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

    const loadModules = async (roleId) => {
      modulesContainer.innerHTML = '<div class="d-flex align-items-center gap-2 text-secondary"><span class="spinner-border spinner-border-sm" aria-hidden="true"></span><span>Loading modules…</span></div>';
      const suffix = roleId == null ? "" : `?roleId=${encodeURIComponent(roleId)}`;
      const payload = await window.apcloudApi.json(`roles/module-options${suffix}`);
      const modules = Array.isArray(payload) ? payload : (property(payload, "data") ?? []);
      modulesContainer.replaceChildren();
      if (!modules.length) {
        const empty = document.createElement("p");
        empty.className = "text-secondary mb-0";
        empty.textContent = "No active application modules are available.";
        modulesContainer.append(empty);
        return;
      }

      const row = document.createElement("div");
      row.className = "row g-2";
      modules.forEach((module) => {
        const moduleId = property(module, "id");
        const column = document.createElement("div");
        column.className = "col-md-6";
        const label = document.createElement("label");
        label.className = "form-check border rounded p-2 h-100 m-0";
        const checkbox = document.createElement("input");
        checkbox.type = "checkbox";
        checkbox.className = "form-check-input";
        checkbox.value = String(moduleId);
        checkbox.checked = property(module, "isAssigned") === true;
        checkbox.dataset.roleModule = "";
        const text = document.createElement("span");
        text.className = "form-check-label";
        text.textContent = `${property(module, "name")} (${property(module, "code")})`;
        label.append(checkbox, text);
        column.append(label);
        row.append(column);
      });
      modulesContainer.append(row);
    };

    const showEditor = async (record = null) => {
      try {
        form.reset();
        form.classList.remove("was-validated");
        const id = property(record, "id");
        idInput.value = id == null ? "" : String(id);
        nameInput.value = property(record, "name") ?? "";
        activeInput.checked = record ? property(record, "isActive") !== false : true;
        title.textContent = id == null ? "Add role" : "Edit role";
        saveLabel.textContent = id == null ? "Add role" : "Save changes";
        await loadModules(id);
        modal?.show();
        modalElement.addEventListener("shown.bs.modal", () => nameInput.focus(), { once: true });
      } catch (error) {
        window.apcloudNotifications?.error(
          error.message || "The role editor could not be loaded.",
          "Unable to load role form");
      }
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
        isActive: activeInput.checked,
        moduleIds: [...modulesContainer.querySelectorAll("[data-role-module]:checked")]
          .map((checkbox) => Number(checkbox.value))
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
