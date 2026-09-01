(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    if (!window.apcloudApi) return;
    const modalElement = document.querySelector("[data-risk-modal]");
    const form = modalElement?.querySelector("[data-risk-form]");
    const grid = document.querySelector("[data-server-grid]");
    if (!modalElement || !form) return;
    if (modalElement.parentElement !== document.body) document.body.append(modalElement);

    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = ui?.Modal?.getOrCreateInstance(modalElement);
    const property = (record, name) => Object.entries(record ?? {})
      .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.[1];
    const notify = (type, message, title) => window.apcloudNotifications?.[type]?.(message, title);
    const panels = [...form.querySelectorAll("[data-risk-step-panel]")];
    const stepButtons = [...form.querySelectorAll("[data-risk-step-button]")];
    const previousButton = form.querySelector("[data-risk-previous]");
    const nextButton = form.querySelector("[data-risk-next]");
    const saveButton = form.querySelector("[data-risk-save]");
    const errorBox = form.querySelector("[data-risk-error]");
    const fields = {
      id: form.querySelector("[data-risk-id]"), number: form.querySelector("[data-risk-number]"),
      issueDate: form.querySelector("[data-risk-issue-date]"), location: form.querySelector("[data-risk-location]"),
      start: form.querySelector("[data-risk-start]"), end: form.querySelector("[data-risk-end]"),
      issuer: form.querySelector("[data-risk-issuer]"), receiver: form.querySelector("[data-risk-receiver]"),
      responsible: form.querySelector("[data-risk-responsible]"), description: form.querySelector("[data-risk-description]"),
      instructions: form.querySelector("[data-risk-instructions]"), otherPpe: form.querySelector("[data-risk-other-ppe]"),
      otherMeasures: form.querySelector("[data-risk-other-measures]")
    };
    const groupNames = ["hazardCategories", "specialPermits", "personalProtectiveEquipment", "additionalPpe"];
    const optionContainers = Object.fromEntries(groupNames.map(name => [name, form.querySelector(`[data-risk-options="${name}"]`)]));
    let currentStep = 0;
    let editingId = null;
    let lookupPromise = null;

    const normalizeCode = value => String(value ?? "").toUpperCase().replace(/[^A-Z0-9]/g, "");
    const categoryMatchers = {
      hazardCategories: code => code.includes("HAZARD"),
      personalProtectiveEquipment: code => (code === "PPE" || code.includes("PERSONALPROTECTIVE")) && !code.includes("ADDITIONAL"),
      additionalPpe: code => code.includes("ADDITIONAL") && (code.includes("PPE") || code.includes("PROTECT"))
    };

    const setStep = step => {
      currentStep = Math.max(0, Math.min(step, panels.length - 1));
      panels.forEach((panel, index) => panel.classList.toggle("d-none", index !== currentStep));
      stepButtons.forEach((button, index) => {
        button.classList.toggle("is-active", index === currentStep);
        button.classList.toggle("is-complete", index < currentStep);
        button.setAttribute("aria-current", index === currentStep ? "step" : "false");
      });
      previousButton.classList.toggle("d-none", currentStep === 0);
      nextButton.classList.toggle("d-none", currentStep === panels.length - 1);
      saveButton.classList.toggle("d-none", currentStep !== panels.length - 1);
      if (currentStep === panels.length - 1) renderReview();
      modalElement.querySelector(".modal-body")?.scrollTo({ top: 0, behavior: "smooth" });
    };

    const validateDates = () => {
      fields.end.setCustomValidity("");
      if (fields.start.value && fields.end.value && fields.end.value < fields.start.value)
        fields.end.setCustomValidity("Planned end must be on or after planned start.");
    };

    const validateStep = step => {
      validateDates();
      const controls = [...panels[step].querySelectorAll("input, textarea, select")];
      const valid = controls.every(control => control.checkValidity());
      if (!valid) {
        form.classList.add("was-validated");
        controls.find(control => !control.checkValidity())?.focus();
      }
      return valid;
    };

    const selectionPayload = name => [...optionContainers[name].querySelectorAll("input[type=checkbox]:checked")]
      .map(input => ({ listItemId: Number(input.value), isSelected: true }))
      .filter(item => item.listItemId > 0);

    const payload = () => ({
      preRiskAssessmentNumber: fields.number.value.trim(), issueDate: fields.issueDate.value,
      permitIssuerName: fields.issuer.value.trim(), permitReceiverName: fields.receiver.value.trim(),
      areaResponsibleName: fields.responsible.value.trim(), locationOfWork: fields.location.value.trim(),
      descriptionOfWork: fields.description.value.trim() || null,
      specialInstructions: fields.instructions.value.trim() || null,
      otherEquipmentsPPE: fields.otherPpe.value.trim() || null,
      otherProtectionMeasures: fields.otherMeasures.value.trim() || null,
      plannedStartDateTime: fields.start.value || null, plannedEndDateTime: fields.end.value || null,
      additionalPpe: selectionPayload("additionalPpe"),
      hazardCategories: selectionPayload("hazardCategories"),
      personalProtectiveEquipment: selectionPayload("personalProtectiveEquipment"),
      specialPermits: selectionPayload("specialPermits")
    });

    const createReviewItem = (term, value) => {
      const wrapper = document.createElement("div");
      const dt = document.createElement("dt"); dt.textContent = term;
      const dd = document.createElement("dd");
      if (Array.isArray(value)) {
        if (value.length) {
          const list = document.createElement("ul"); list.className = "risk-review-selection-list";
          value.forEach(item => {
            const listItem = document.createElement("li"); listItem.textContent = item;
            list.append(listItem);
          });
          dd.append(list);
        } else {
          dd.textContent = "None selected";
        }
      } else {
        dd.textContent = value || "Not provided";
      }
      wrapper.append(dt, dd); return wrapper;
    };

    const selectedNames = name => [...optionContainers[name].querySelectorAll("input:checked")]
      .map(input => input.closest("label")?.querySelector("span")?.textContent?.trim()).filter(Boolean);

    const createReviewSection = (title, entries, className = "") => {
      const section = document.createElement("section");
      section.className = `risk-review-section ${className}`.trim();
      const heading = document.createElement("h5"); heading.textContent = title;
      const list = document.createElement("dl"); list.className = "risk-review-grid";
      entries.forEach(([term, value]) => list.append(createReviewItem(term, value)));
      section.append(heading, list);
      return section;
    };

    const displayDateTime = value => value ? new Date(value).toLocaleString() : "Not provided";

    const renderReview = () => {
      const review = form.querySelector("[data-risk-review]");
      const heading = document.createElement("div"); heading.className = "risk-review-heading";
      const headingCopy = document.createElement("div");
      const eyebrow = document.createElement("span"); eyebrow.className = "risk-review-eyebrow"; eyebrow.textContent = "Final verification";
      const title = document.createElement("h4"); title.textContent = "Review before submitting";
      const hint = document.createElement("p"); hint.textContent = "Confirm every detail below. With the current API workflow, submission saves the assessment in Draft status.";
      const badge = document.createElement("span"); badge.className = "badge bg-yellow-lt risk-review-status"; badge.textContent = "Draft";
      headingCopy.append(eyebrow, title, hint); heading.append(headingCopy, badge);
      const body = document.createElement("div"); body.className = "risk-review-body";
      body.append(
        createReviewSection("Assessment", [
          ["Assessment number", fields.number.value], ["Issue date", fields.issueDate.value],
          ["Location of work", fields.location.value], ["Planned start", displayDateTime(fields.start.value)],
          ["Planned end", displayDateTime(fields.end.value)]
        ]),
        createReviewSection("People and work scope", [
          ["Permit issuer", fields.issuer.value], ["Permit receiver", fields.receiver.value],
          ["Area responsible", fields.responsible.value], ["Description of work", fields.description.value],
          ["Special instructions", fields.instructions.value]
        ]),
        createReviewSection("Hazards and permit requirements", [
          ["Hazard categories", selectedNames("hazardCategories")],
          ["Special permits / permit types", selectedNames("specialPermits")]
        ]),
        createReviewSection("Protection and additional controls", [
          ["Personal protective equipment", selectedNames("personalProtectiveEquipment")],
          ["Other equipment or PPE", fields.otherPpe.value],
          ["Additional protective measures", selectedNames("additionalPpe")],
          ["Other protection measures", fields.otherMeasures.value]
        ])
      );
      review.replaceChildren(heading, body);
    };

    const renderOptions = (name, items, emptyMessage = "No active list-item category or options are configured for this section.") => {
      const container = optionContainers[name];
      container.replaceChildren();
      if (!items.length) {
        const warning = document.createElement("div"); warning.className = "alert alert-warning py-2 mb-0";
        warning.textContent = emptyMessage;
        container.append(warning); return;
      }
      items.forEach(item => {
        const id = Number(property(item, "id"));
        const label = document.createElement("label"); label.className = "risk-option";
        const input = document.createElement("input"); input.type = "checkbox"; input.className = "form-check-input"; input.value = String(id);
        const text = document.createElement("span"); text.textContent = property(item, "name") || property(item, "code") || `Item ${id}`;
        label.append(input, text); container.append(label);
      });
    };

    const loadLookups = () => lookupPromise ??= (async () => {
      const categoriesPromise = window.apcloudApi.json("list-items/categories/ddl");
      await Promise.all(groupNames.map(async name => {
        try {
          let categoryName = "PermitType";
          if (name !== "specialPermits") {
            const categories = await categoriesPromise;
            categoryName = property(categories.find(item => categoryMatchers[name]?.(normalizeCode(
              `${property(item, "code") ?? ""} ${property(item, "name") ?? ""}`))), "name");
          }
          if (!categoryName) return renderOptions(name, []);
          const items = await window.apcloudApi.json(`list-items/category/${encodeURIComponent(categoryName)}`);
          renderOptions(name, items);
        } catch (error) {
          const label = name === "specialPermits" ? "PermitType options" : "options";
          renderOptions(name, [], `Unable to load ${label}. ${error.message || "Please try again."}`);
        }
      }));
    })().catch(error => {
      lookupPromise = null;
      groupNames.forEach(name => renderOptions(name, []));
      notify("error", error.message || "Risk assessment options could not be loaded.", "Unable to load form options");
    });

    const markSelections = (name, selections) => {
      const ids = new Set((selections ?? []).filter(item => property(item, "isSelected") !== false)
        .map(item => Number(property(item, "listItemId"))));
      ids.forEach(id => {
        let input = optionContainers[name].querySelector(`input[value="${id}"]`);
        if (!input) {
          const label = document.createElement("label"); label.className = "risk-option";
          input = document.createElement("input"); input.type = "checkbox"; input.className = "form-check-input"; input.value = String(id);
          const text = document.createElement("span"); text.textContent = `Configured item #${id}`;
          label.append(input, text); optionContainers[name].append(label);
        }
        input.checked = true;
      });
    };

    const localDateTime = value => value ? String(value).slice(0, 16) : "";
    const populate = record => {
      fields.id.value = property(record, "id") || "";
      fields.number.value = property(record, "preRiskAssessmentNumber") || "";
      fields.issueDate.value = String(property(record, "issueDate") || "").slice(0, 10);
      fields.location.value = property(record, "locationOfWork") || "";
      fields.start.value = localDateTime(property(record, "plannedStartDateTime"));
      fields.end.value = localDateTime(property(record, "plannedEndDateTime"));
      fields.issuer.value = property(record, "permitIssuerName") || "";
      fields.receiver.value = property(record, "permitReceiverName") || "";
      fields.responsible.value = property(record, "areaResponsibleName") || "";
      fields.description.value = property(record, "descriptionOfWork") || "";
      fields.instructions.value = property(record, "specialInstructions") || "";
      fields.otherPpe.value = property(record, "otherEquipmentsPPE") || "";
      fields.otherMeasures.value = property(record, "otherProtectionMeasures") || "";
      groupNames.forEach(name => markSelections(name, property(record, name)));
    };

    const openEditor = async id => {
      editingId = id ? Number(id) : null;
      form.reset(); form.classList.remove("was-validated"); errorBox.classList.add("d-none");
      groupNames.forEach(name => optionContainers[name].querySelectorAll("input:checked").forEach(input => input.checked = false));
      fields.issueDate.value = new Date().toISOString().slice(0, 10);
      modalElement.querySelector("[data-risk-mode]").textContent = editingId ? "Edit risk assessment" : "Add risk assessment";
      modalElement.querySelector("[data-risk-title]").textContent = editingId ? "Update draft risk assessment" : "New risk assessment";
      form.querySelector("[data-risk-save-label]").textContent = "Submit";
      setStep(0); modal?.show();
      try {
        await loadLookups();
        if (editingId) {
          const record = await window.apcloudApi.json(`risk-assessments/${editingId}`);
          if (String(property(record, "riskAssessmentStatus") || "").toLowerCase() !== "draft")
            throw new Error("Only Draft risk assessments can be edited.");
          populate(record);
        }
        fields.number.focus();
      } catch (error) {
        errorBox.textContent = error.message || "The risk assessment form could not be loaded.";
        errorBox.classList.remove("d-none"); saveButton.disabled = true;
      }
    };

    document.querySelectorAll("[data-risk-add]").forEach(button => button.addEventListener("click", () => openEditor()));
    grid?.addEventListener("server-grid:action", event => {
      if (event.detail?.action === "edit") openEditor(property(event.detail.record, "id"));
    });
    fields.start.addEventListener("change", validateDates); fields.end.addEventListener("change", validateDates);
    form.addEventListener("input", () => { if (currentStep === panels.length - 1) renderReview(); });
    form.addEventListener("change", () => { if (currentStep === panels.length - 1) renderReview(); });
    previousButton.addEventListener("click", () => setStep(currentStep - 1));
    nextButton.addEventListener("click", () => { if (validateStep(currentStep)) setStep(currentStep + 1); });
    stepButtons.forEach((button, index) => button.addEventListener("click", () => {
      if (index <= currentStep || validateStep(currentStep)) setStep(index);
    }));

    form.addEventListener("submit", async event => {
      event.preventDefault();
      if (!panels.every((_, index) => validateStep(index))) return;
      saveButton.disabled = true; errorBox.classList.add("d-none");
      form.querySelector("[data-risk-save-label]").classList.add("d-none");
      form.querySelector("[data-risk-saving]").classList.remove("d-none");
      try {
        await window.apcloudApi.json(editingId ? `risk-assessments/${editingId}` : "risk-assessments", {
          method: editingId ? "PUT" : "POST", body: payload()
        });
        modal?.hide();
        grid?.dispatchEvent(new CustomEvent("server-grid:reload"));
        notify("success", editingId ? "The draft risk assessment was updated." : "The risk assessment was created as a draft.", "Risk assessment saved");
      } catch (error) {
        errorBox.textContent = error.message || "The risk assessment could not be saved.";
        errorBox.classList.remove("d-none");
      } finally {
        saveButton.disabled = false;
        form.querySelector("[data-risk-save-label]").classList.remove("d-none");
        form.querySelector("[data-risk-saving]").classList.add("d-none");
      }
    });

    modalElement.addEventListener("hidden.bs.modal", () => { editingId = null; saveButton.disabled = false; });
    const query = new URLSearchParams(window.location.search);
    if (query.get("create") === "true") openEditor();
    else if (/^\d+$/.test(query.get("edit") || "")) openEditor(query.get("edit"));
  });
})();
