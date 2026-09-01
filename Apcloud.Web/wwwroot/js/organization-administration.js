(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    if (!window.apcloudApi) return;
    const property = (record, name) => Object.entries(record ?? {})
      .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.[1];
    const notify = (type, message, title) => window.apcloudNotifications?.[type]?.(message, title);
    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const profile = document.querySelector("[data-organization-profile]");
    const branchGrid = document.querySelector("[data-branch-grid]");
    const departmentGrid = document.querySelector("[data-department-grid]");
    let organization = null;
    let branches = [];

    const formatDate = (value) => {
      if (!value) return "Not updated";
      const date = new Date(value);
      return Number.isNaN(date.valueOf()) ? "—" : new Intl.DateTimeFormat(undefined, {
        year: "numeric", month: "short", day: "2-digit", hour: "2-digit", minute: "2-digit"
      }).format(date);
    };

    const profileElements = profile ? {
      loading: profile.querySelector("[data-organization-loading]"),
      error: profile.querySelector("[data-organization-error]"),
      display: profile.querySelector("[data-organization-display]"),
      actions: profile.querySelector("[data-organization-actions]"),
      form: profile.querySelector("[data-organization-form]"),
      edit: profile.querySelector("[data-organization-edit]"),
      cancel: profile.querySelector("[data-organization-cancel]"),
      retry: profile.querySelector("[data-organization-retry]"),
      save: profile.querySelector("[data-organization-save]"),
      saveLabel: profile.querySelector("[data-organization-save-label]"),
      saving: profile.querySelector("[data-organization-saving]")
    } : null;

    const profileFields = profileElements ? {
      code: profile.querySelector("[data-organization-form-code]"),
      name: profile.querySelector("[data-organization-form-name]"),
      address: profile.querySelector("[data-organization-form-address]"),
      phone: profile.querySelector("[data-organization-form-phone]"),
      email: profile.querySelector("[data-organization-form-email]"),
      website: profile.querySelector("[data-organization-form-website]")
    } : null;

    const showProfileState = (state) => {
      profileElements.loading.classList.toggle("d-none", state !== "loading");
      profileElements.error.classList.toggle("d-none", state !== "error");
      profileElements.display.classList.toggle("d-none", state !== "display");
      profileElements.form.classList.toggle("d-none", state !== "edit");
      profileElements.actions.classList.toggle("d-none", state !== "display");
    };

    const setText = (selector, value) => {
      const element = profile.querySelector(selector);
      if (element) element.textContent = value || "—";
    };

    const renderOrganization = () => {
      setText("[data-organization-name]", property(organization, "name"));
      setText("[data-organization-code]", property(organization, "code"));
      setText("[data-organization-address]", property(organization, "address"));
      setText("[data-organization-phone]", property(organization, "phoneNumber"));
      setText("[data-organization-email]", property(organization, "email"));
      setText("[data-organization-website]", property(organization, "website"));
      setText("[data-organization-created]", formatDate(property(organization, "createdAtUtc")));
      setText("[data-organization-updated]", formatDate(property(organization, "updatedAtUtc")));
      const status = profile.querySelector("[data-organization-status]");
      const active = property(organization, "isActive") === true;
      status.textContent = active ? "Active" : "Inactive";
      status.className = `badge ${active ? "bg-green-lt" : "bg-red-lt"}`;
    };

    const populateOrganizationForm = () => {
      profileElements.form.reset();
      profileElements.form.classList.remove("was-validated");
      profileFields.code.value = property(organization, "code") || "";
      profileFields.name.value = property(organization, "name") || "";
      profileFields.address.value = property(organization, "address") || "";
      profileFields.phone.value = property(organization, "phoneNumber") || "";
      profileFields.email.value = property(organization, "email") || "";
      profileFields.website.value = property(organization, "website") || "";
    };

    const loadOrganization = async () => {
      if (profileElements) showProfileState("loading");
      try {
        organization = await window.apcloudApi.json("organization/current");
        if (profileElements) {
          renderOrganization();
          showProfileState("display");
        }
      } catch (error) {
        if (profileElements) {
          profile.querySelector("[data-organization-error-message]").textContent = error.message || "Organization details could not be loaded.";
          showProfileState("error");
        } else if (branchGrid) {
          notify("error", error.message || "Organization details could not be loaded.", "Unable to load organization");
        }
      }
    };

    profileElements?.edit.addEventListener("click", () => {
      populateOrganizationForm();
      showProfileState("edit");
      profileFields.name.focus();
    });
    profileElements?.cancel.addEventListener("click", () => showProfileState("display"));
    profileElements?.retry.addEventListener("click", loadOrganization);
    profileElements?.form.addEventListener("submit", async (event) => {
      event.preventDefault();
      if (!profileElements.form.checkValidity()) {
        profileElements.form.classList.add("was-validated");
        return;
      }
      profileElements.save.disabled = true;
      profileElements.saveLabel.classList.add("d-none");
      profileElements.saving.classList.remove("d-none");
      try {
        organization = await window.apcloudApi.json(`organization/${property(organization, "id")}`, {
          method: "PUT",
          body: {
            code: profileFields.code.value.trim(), name: profileFields.name.value.trim(),
            address: profileFields.address.value.trim(), phoneNumber: profileFields.phone.value.trim() || null,
            email: profileFields.email.value.trim() || null, website: profileFields.website.value.trim() || null
          }
        });
        renderOrganization();
        showProfileState("display");
        branchGrid?.dispatchEvent(new CustomEvent("server-grid:reload"));
        notify("success", "Organization details were updated.", "Organization saved");
      } catch (error) {
        notify("error", error.message || "Organization details could not be saved.", "Unable to save organization");
      } finally {
        profileElements.save.disabled = false;
        profileElements.saveLabel.classList.remove("d-none");
        profileElements.saving.classList.add("d-none");
      }
    });

    const branchModalElement = document.querySelector("[data-branch-modal]");
    const departmentModalElement = document.querySelector("[data-department-modal]");
    [branchModalElement, departmentModalElement].forEach((element) => {
      if (element && element.parentElement !== document.body) document.body.append(element);
    });
    const branchModal = branchModalElement && ui?.Modal?.getOrCreateInstance(branchModalElement);
    const departmentModal = departmentModalElement && ui?.Modal?.getOrCreateInstance(departmentModalElement);
    const branchForm = branchModalElement?.querySelector("[data-branch-form]");
    const departmentForm = departmentModalElement?.querySelector("[data-department-form]");
    let branchEditingId = null;
    let departmentEditingId = null;

    const branchFields = branchForm ? {
      code: branchForm.querySelector("[data-branch-code]"), name: branchForm.querySelector("[data-branch-name]"),
      address: branchForm.querySelector("[data-branch-address]"), head: branchForm.querySelector("[data-branch-head]"),
      active: branchForm.querySelector("[data-branch-active]")
    } : null;
    const departmentFields = departmentForm ? {
      branch: departmentForm.querySelector("[data-department-branch]"), code: departmentForm.querySelector("[data-department-code]"),
      name: departmentForm.querySelector("[data-department-name]"), active: departmentForm.querySelector("[data-department-active]")
    } : null;

    const loadBranches = async () => {
      if (!departmentFields) return;
      branches = await window.apcloudApi.json("organization/branches/ddl");
      const selected = departmentFields.branch.value;
      departmentFields.branch.replaceChildren(new Option("Select an office branch", ""));
      branches.forEach((branch) => departmentFields.branch.append(new Option(
        `${property(branch, "name")} (${property(branch, "code")})`, String(property(branch, "id")))));
      if (selected) departmentFields.branch.value = selected;
    };

    const showBranch = (record = null) => {
      if (!organization) return notify("error", "Organization details are not available yet.", "Unable to open branch form");
      branchEditingId = record ? property(record, "id") : null;
      branchForm.reset();
      branchForm.classList.remove("was-validated");
      branchFields.code.value = property(record, "code") || "";
      branchFields.name.value = property(record, "name") || "";
      branchFields.address.value = property(record, "address") || "";
      branchFields.head.checked = property(record, "isHeadOffice") === true;
      branchFields.active.checked = record ? property(record, "isActive") === true : true;
      branchModalElement.querySelector("[data-branch-title]").textContent = record ? "Edit office branch" : "Add office branch";
      branchModalElement.querySelector("[data-branch-save-label]").textContent = record ? "Save changes" : "Add office branch";
      branchModal?.show();
    };

    const showDepartment = async (record = null) => {
      departmentEditingId = record ? property(record, "id") : null;
      departmentForm.reset();
      departmentForm.classList.remove("was-validated");
      try { await loadBranches(); } catch (error) { return notify("error", error.message, "Unable to load office branches"); }
      const branchId = property(record, "officeBranchId");
      if (record && branchId && ![...departmentFields.branch.options].some((option) => option.value === String(branchId)))
        departmentFields.branch.append(new Option(`${property(record, "branchName")} (inactive)`, String(branchId)));
      departmentFields.branch.value = branchId ? String(branchId) : "";
      departmentFields.code.value = property(record, "code") || "";
      departmentFields.name.value = property(record, "name") || "";
      departmentFields.active.checked = record ? property(record, "isActive") === true : true;
      departmentModalElement.querySelector("[data-department-title]").textContent = record ? "Edit department" : "Add department";
      departmentModalElement.querySelector("[data-department-save-label]").textContent = record ? "Save changes" : "Add department";
      departmentModal?.show();
    };

    document.querySelector("[data-branch-add]")?.addEventListener("click", () => showBranch());
    document.querySelector("[data-department-add]")?.addEventListener("click", () => showDepartment());
    branchFields?.head.addEventListener("change", () => { if (branchFields.head.checked) branchFields.active.checked = true; });
    branchFields?.active.addEventListener("change", () => { if (!branchFields.active.checked) branchFields.head.checked = false; });

    branchGrid?.addEventListener("server-grid:action", async (event) => {
      const { action, record } = event.detail ?? {};
      if (action === "edit") return showBranch(record);
      if (action !== "delete" || !record) return;
      if (!window.confirm(`Deactivate office branch "${property(record, "name")}"?`)) return;
      try {
        await window.apcloudApi.json(`organization/branches/${property(record, "id")}`, { method: "DELETE" });
        notify("success", "The office branch was deactivated.", "Branch updated");
        branchGrid.dispatchEvent(new CustomEvent("server-grid:reload"));
        departmentGrid?.dispatchEvent(new CustomEvent("server-grid:reload"));
        if (departmentFields) await loadBranches();
      } catch (error) { notify("error", error.message || "The office branch could not be deactivated.", "Unable to deactivate branch"); }
    });

    departmentGrid?.addEventListener("server-grid:action", async (event) => {
      const { action, record } = event.detail ?? {};
      if (action === "edit") return showDepartment(record);
      if (action !== "delete" || !record) return;
      if (!window.confirm(`Deactivate department "${property(record, "name")}"?`)) return;
      try {
        await window.apcloudApi.json(`organization/departments/${property(record, "id")}`, { method: "DELETE" });
        notify("success", "The department was deactivated.", "Department updated");
        departmentGrid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) { notify("error", error.message || "The department could not be deactivated.", "Unable to deactivate department"); }
    });

    branchForm?.addEventListener("submit", async (event) => {
      event.preventDefault();
      if (!branchForm.checkValidity()) { branchForm.classList.add("was-validated"); return; }
      const save = branchForm.querySelector("[data-branch-save]");
      save.disabled = true;
      branchForm.querySelector("[data-branch-save-label]").classList.add("d-none");
      branchForm.querySelector("[data-branch-saving]").classList.remove("d-none");
      try {
        await window.apcloudApi.json(branchEditingId ? `organization/branches/${branchEditingId}` : "organization/branches", {
          method: branchEditingId ? "PUT" : "POST",
          body: { organizationId: Number(property(organization, "id")), code: branchFields.code.value.trim(), name: branchFields.name.value.trim(), address: branchFields.address.value.trim() || null, isHeadOffice: branchFields.head.checked, isActive: branchFields.active.checked }
        });
        branchModal?.hide();
        notify("success", branchEditingId ? "The office branch was updated." : "The office branch was created.", "Branch saved");
        branchGrid?.dispatchEvent(new CustomEvent("server-grid:reload"));
        if (departmentFields) await loadBranches();
      } catch (error) { notify("error", error.message || "The office branch could not be saved.", "Unable to save branch"); }
      finally {
        save.disabled = false;
        branchForm.querySelector("[data-branch-save-label]").classList.remove("d-none");
        branchForm.querySelector("[data-branch-saving]").classList.add("d-none");
      }
    });

    departmentForm?.addEventListener("submit", async (event) => {
      event.preventDefault();
      if (!departmentForm.checkValidity()) { departmentForm.classList.add("was-validated"); return; }
      const save = departmentForm.querySelector("[data-department-save]");
      save.disabled = true;
      departmentForm.querySelector("[data-department-save-label]").classList.add("d-none");
      departmentForm.querySelector("[data-department-saving]").classList.remove("d-none");
      try {
        await window.apcloudApi.json(departmentEditingId ? `organization/departments/${departmentEditingId}` : "organization/departments", {
          method: departmentEditingId ? "PUT" : "POST",
          body: { officeBranchId: Number(departmentFields.branch.value), code: departmentFields.code.value.trim(), name: departmentFields.name.value.trim(), isActive: departmentFields.active.checked }
        });
        departmentModal?.hide();
        notify("success", departmentEditingId ? "The department was updated." : "The department was created.", "Department saved");
        departmentGrid?.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) { notify("error", error.message || "The department could not be saved.", "Unable to save department"); }
      finally {
        save.disabled = false;
        departmentForm.querySelector("[data-department-save-label]").classList.remove("d-none");
        departmentForm.querySelector("[data-department-saving]").classList.add("d-none");
      }
    });

    loadOrganization();
    if (departmentFields) loadBranches().catch(() => {});
  });
})();
