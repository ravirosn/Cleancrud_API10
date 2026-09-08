(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const grid = document.querySelector("[data-permission-grid]");
    const modalElement = document.querySelector("[data-permission-modal]");
    const form = modalElement?.querySelector("[data-permission-form]");
    if (!grid || !modalElement || !form || !window.apcloudApi) return;

    if (modalElement.parentElement !== document.body) document.body.append(modalElement);
    const bootstrapUi = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = bootstrapUi?.Modal?.getOrCreateInstance(modalElement);
    const role = form.querySelector("[data-permission-role]");
    const selector = form.querySelector("[data-permission-selector]");
    const placeholder = form.querySelector("[data-permission-placeholder]");
    const loading = form.querySelector("[data-permission-loading]");
    const search = form.querySelector("[data-permission-search]");
    const list = form.querySelector("[data-permission-list]");
    const results = form.querySelector("[data-permission-results]");
    const count = form.querySelector("[data-permission-count]");
    const changeSummary = form.querySelector("[data-permission-change-summary]");
    const save = form.querySelector("[data-permission-save]");
    const prop = (record, name) => Object.entries(record ?? {})
      .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.[1];
    const notify = (type, message, title) => window.apcloudNotifications?.[type]?.(message, title);

    let rolesLoaded = false;
    let policies = [];
    let selected = new Set();
    let initiallySelected = new Set();
    let loadGeneration = 0;

    const createOption = (value, text) => {
      const option = document.createElement("option");
      option.value = String(value);
      option.textContent = text;
      return option;
    };

    const loadRoles = async () => {
      if (rolesLoaded) return;
      const rows = await window.apcloudApi.json("permission-assignments/roles");
      role.replaceChildren(createOption("", "Select role"));
      rows.forEach(item => role.append(createOption(prop(item, "id"), prop(item, "name"))));
      rolesLoaded = true;
    };

    const updateSummary = () => {
      const added = [...selected].filter(id => !initiallySelected.has(id)).length;
      const removed = [...initiallySelected].filter(id => !selected.has(id)).length;
      count.textContent = `${selected.size} selected`;
      changeSummary.textContent = added || removed ? `${added} to add · ${removed} to remove` : "No changes";
    };

    const matchesSearch = policy => {
      const term = search.value.trim().toLocaleLowerCase();
      return !term || [policy.name, policy.code, policy.moduleCode, policy.menuName]
        .some(value => value.toLocaleLowerCase().includes(term));
    };

    const renderPolicies = () => {
      const visible = policies.filter(matchesSearch);
      list.replaceChildren();
      results.textContent = `${visible.length} of ${policies.length} policies`;

      if (!visible.length) {
        const empty = document.createElement("div");
        empty.className = "p-4 text-center text-secondary";
        empty.textContent = "No policies match this search.";
        list.append(empty);
        updateSummary();
        return;
      }

      let currentGroup = "";
      visible.forEach(policy => {
        const group = `${policy.moduleCode} · ${policy.menuName}`;
        if (group !== currentGroup) {
          currentGroup = group;
          const heading = document.createElement("div");
          heading.className = "px-3 py-2 bg-body-tertiary border-bottom fw-semibold small text-uppercase";
          heading.textContent = group;
          list.append(heading);
        }

        const item = document.createElement("label");
        item.className = "d-flex gap-3 align-items-start px-3 py-2 border-bottom mb-0";
        item.dataset.permissionPolicyId = String(policy.id);
        const checkbox = document.createElement("input");
        checkbox.type = "checkbox";
        checkbox.className = "form-check-input mt-1 flex-shrink-0";
        checkbox.checked = selected.has(policy.id);
        checkbox.disabled = !policy.canAssign && !policy.isAssigned;
        checkbox.addEventListener("change", () => {
          if (checkbox.checked) selected.add(policy.id);
          else selected.delete(policy.id);
          updateSummary();
        });

        const detail = document.createElement("span");
        detail.className = "d-block flex-fill";
        const title = document.createElement("span");
        title.className = "d-flex flex-wrap align-items-center gap-2";
        const name = document.createElement("span");
        name.className = "fw-medium";
        name.textContent = policy.name;
        const code = document.createElement("code");
        code.className = "small";
        code.textContent = policy.code;
        title.append(name, code);
        if (!policy.canAssign) {
          const unavailable = document.createElement("span");
          unavailable.className = "badge bg-warning-lt";
          unavailable.textContent = policy.isAssigned ? "Menu unavailable · clear recommended" : "Menu required";
          title.append(unavailable);
        }
        detail.append(title);
        item.append(checkbox, detail);
        list.append(item);
      });
      updateSummary();
    };

    const setLoading = isLoading => {
      loading.classList.toggle("d-none", !isLoading);
      placeholder.classList.add("d-none");
      selector.classList.toggle("d-none", isLoading || !role.value);
      save.disabled = isLoading || !role.value;
    };

    const loadPolicies = async roleId => {
      const generation = ++loadGeneration;
      policies = [];
      selected = new Set();
      initiallySelected = new Set();
      search.value = "";
      if (!roleId) {
        selector.classList.add("d-none");
        loading.classList.add("d-none");
        placeholder.classList.remove("d-none");
        save.disabled = true;
        updateSummary();
        return;
      }

      setLoading(true);
      try {
        const rows = await window.apcloudApi.json(
          `permission-assignments/policies?roleId=${encodeURIComponent(roleId)}`);
        if (generation !== loadGeneration) return;
        policies = rows.map(item => ({
          id: Number(prop(item, "id")),
          code: String(prop(item, "code") ?? ""),
          name: String(prop(item, "name") ?? ""),
          moduleCode: String(prop(item, "moduleCode") ?? ""),
          menuName: String(prop(item, "menuName") ?? ""),
          isAssigned: prop(item, "isAssigned") === true,
          canAssign: prop(item, "canAssign") === true
        }));
        selected = new Set(policies.filter(item => item.isAssigned).map(item => item.id));
        initiallySelected = new Set(selected);
        setLoading(false);
        renderPolicies();
      } catch (error) {
        if (generation !== loadGeneration) return;
        selector.classList.add("d-none");
        loading.classList.add("d-none");
        placeholder.classList.remove("d-none");
        placeholder.textContent = "Unable to load policies for this role.";
        save.disabled = true;
        notify("error", error.message, "Unable to load policies");
      }
    };

    const show = async record => {
      try {
        form.reset();
        form.classList.remove("was-validated");
        placeholder.textContent = "Select a role to load its permission policies.";
        await loadRoles();
        const roleId = prop(record, "roleId");
        role.value = roleId ? String(roleId) : "";
        role.disabled = Boolean(record);
        await loadPolicies(role.value);
        modal?.show();
      } catch (error) {
        notify("error", error.message, "Unable to open permissions");
      }
    };

    const setVisible = shouldSelect => {
      policies.filter(matchesSearch).forEach(policy => {
        if (!policy.canAssign && !policy.isAssigned) return;
        if (shouldSelect) selected.add(policy.id);
        else selected.delete(policy.id);
      });
      renderPolicies();
    };

    document.querySelectorAll("[data-permission-add]").forEach(button =>
      button.addEventListener("click", () => show()));
    role.addEventListener("change", () => loadPolicies(role.value));
    search.addEventListener("input", renderPolicies);
    form.querySelector("[data-permission-select-visible]").addEventListener("click", () => setVisible(true));
    form.querySelector("[data-permission-clear-visible]").addEventListener("click", () => setVisible(false));
    modalElement.addEventListener("hidden.bs.modal", () => {
      role.disabled = false;
      ++loadGeneration;
    });

    grid.addEventListener("server-grid:action", async event => {
      const { action, record } = event.detail ?? {};
      if (action === "edit") return show(record);
      if (action !== "delete" || !record) return;
      if (!confirm(`Deactivate ${prop(record, "permissionCode")} for ${prop(record, "roleName")}?`)) return;
      try {
        await window.apcloudApi.json(
          `permission-assignments/${prop(record, "roleId")}/${prop(record, "permissionPolicyId")}`,
          { method: "DELETE" });
        notify("success", "Permission assignment deactivated.", "Assignment updated");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        notify("error", error.message, "Unable to deactivate assignment");
      }
    });

    form.addEventListener("submit", async event => {
      event.preventDefault();
      if (!form.checkValidity() || !role.value) {
        form.classList.add("was-validated");
        return;
      }

      save.disabled = true;
      form.querySelector("[data-permission-save-label]").classList.add("d-none");
      form.querySelector("[data-permission-saving]").classList.remove("d-none");
      try {
        const response = await window.apcloudApi.json(
          `permission-assignments/bulk/${encodeURIComponent(role.value)}`,
          { method: "PUT", body: { permissionPolicyIds: [...selected] } });
        modal?.hide();
        notify("success", `${prop(response, "assignedCount") ?? selected.size} active policies saved.`, "Role permissions updated");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        notify("error", error.message, "Unable to save role permissions");
      } finally {
        save.disabled = false;
        form.querySelector("[data-permission-save-label]").classList.remove("d-none");
        form.querySelector("[data-permission-saving]").classList.add("d-none");
      }
    });
  });
})();
