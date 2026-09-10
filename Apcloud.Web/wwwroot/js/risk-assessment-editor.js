(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    if (!window.apcloudApi) return;
    const modalElement = document.querySelector("[data-risk-modal]");
    const form = modalElement?.querySelector("[data-risk-form]");
    const permitModalElement = document.querySelector("[data-permit-editor-modal]");
    const permitForm = permitModalElement?.querySelector("[data-permit-editor-form]");
    const grid = document.querySelector("[data-server-grid]");
    if (!modalElement || !form) return;
    if (modalElement.parentElement !== document.body) document.body.append(modalElement);
    if (permitModalElement && permitModalElement.parentElement !== document.body) document.body.append(permitModalElement);

    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = ui?.Modal?.getOrCreateInstance(modalElement);
    const permitModal = permitModalElement ? ui?.Modal?.getOrCreateInstance(permitModalElement) : null;
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
    let creatingWorkflow = false;
    let lookupPromise = null;
    let activePermitRiskId = null;
    let activePermitContainer = null;
    let permitPreviewMode = false;
    let permitUsersById = new Map();

    const permitFields = permitForm ? {
      id: permitForm.querySelector("[data-permit-id]"), number: permitForm.querySelector("[data-permit-number]"),
      type: permitForm.querySelector("[data-permit-type]"), status: permitForm.querySelector("[data-permit-status]"),
      issueDate: permitForm.querySelector("[data-permit-issue-date]"), issuer: permitForm.querySelector("[data-permit-issuer]"),
      plannedStart: permitForm.querySelector("[data-permit-planned-start]"), plannedEnd: permitForm.querySelector("[data-permit-planned-end]"),
      issuerContact: permitForm.querySelector("[data-permit-issuer-contact]"), receiver: permitForm.querySelector("[data-permit-receiver]"),
      receiverContact: permitForm.querySelector("[data-permit-receiver-contact]"), riskNumber: permitForm.querySelector("[data-permit-risk-number]"),
      workLocation: permitForm.querySelector("[data-permit-work-location]"), workDescription: permitForm.querySelector("[data-permit-work-description]"),
      specialInstructions: permitForm.querySelector("[data-permit-special-instructions]"), workHeight: permitForm.querySelector("[data-permit-work-height]"),
      completion: permitForm.querySelector("[data-permit-completion]")
    } : null;
    const permitOptionNames = ["inspectionPriorToCommencement", "worksOnWall", "workingInConfinedSpace"];
    const permitOptionContainers = permitForm ? Object.fromEntries(permitOptionNames.map(name =>
      [name, permitForm.querySelector(`[data-permit-options="${name}"]`)])) : {};

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
      issueDate: fields.issueDate.value,
      permitIssuerUserId: Number(fields.issuer.value), permitReceiverUserId: Number(fields.receiver.value),
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

    const selectedUserName = select => select.selectedOptions[0]?.textContent?.trim() || "Not selected";

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

    const displayDate = value => {
      const match = String(value ?? "").match(/^(\d{4})-(\d{2})-(\d{2})/);
      if (!match) return value || "—";
      return new Intl.DateTimeFormat(undefined, { day: "2-digit", month: "short", year: "numeric" })
        .format(new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3])));
    };

    const permitStatusClass = status => {
      switch (String(status ?? "").toLowerCase()) {
        case "approved": case "active": case "completed": return "bg-green-lt";
        case "pending": case "submitted": return "bg-yellow-lt";
        case "rejected": case "inactive": case "cancelled": return "bg-red-lt";
        default: return "bg-blue-lt";
      }
    };

    const createPermitCell = (value, className = "") => {
      const cell = document.createElement("td");
      if (className) cell.className = className;
      cell.textContent = value ?? "—";
      return cell;
    };

    const populatePermitUsers = (users, selectedIssuerId, selectedReceiverId, issuerName, receiverName) => {
      permitUsersById = new Map(users.map(user => [String(property(user, "id")), user]));
      [[permitFields.issuer, selectedIssuerId, issuerName], [permitFields.receiver, selectedReceiverId, receiverName]].forEach(([select, selectedId, selectedName]) => {
        select.replaceChildren(new Option("Select a user", ""));
        users.forEach(user => select.add(new Option(property(user, "name"), String(property(user, "id")))));
        if (selectedId && ![...select.options].some(option => option.value === String(selectedId)))
          select.add(new Option(selectedName || `User ${selectedId}`, String(selectedId)));
      });
      permitFields.issuer.value = String(selectedIssuerId ?? "");
      permitFields.receiver.value = String(selectedReceiverId ?? "");
      permitFields.issuerContact.value = property(permitUsersById.get(permitFields.issuer.value), "contactNumber") ?? "";
      permitFields.receiverContact.value = property(permitUsersById.get(permitFields.receiver.value), "contactNumber") ?? "";
    };

    const renderPermitOptions = (name, items, preview) => {
      const container = permitOptionContainers[name];
      container.replaceChildren();
      if (!items?.length) {
        const empty = document.createElement("p"); empty.className = "text-secondary small mb-0";
        empty.textContent = "No list items are configured for this category."; container.append(empty); return;
      }
      items.forEach(item => {
        const label = document.createElement("label"); label.className = "permit-extension-option";
        const checkbox = document.createElement("input"); checkbox.type = "checkbox"; checkbox.className = "form-check-input";
        checkbox.value = property(item, "listItemId"); checkbox.checked = Boolean(property(item, "isSelected"));
        checkbox.disabled = preview;
        const copy = document.createElement("span");
        const title = document.createElement("strong"); title.textContent = property(item, "name") || "List item";
        copy.append(title);
        label.classList.toggle("is-selected", checkbox.checked); checkbox.addEventListener("change", () =>
          label.classList.toggle("is-selected", checkbox.checked));
        label.append(checkbox, copy); container.append(label);
      });
    };

    permitFields?.issuer.addEventListener("change", () => {
      permitFields.issuerContact.value = property(permitUsersById.get(permitFields.issuer.value), "contactNumber") ?? "";
    });
    permitFields?.receiver.addEventListener("change", () => {
      permitFields.receiverContact.value = property(permitUsersById.get(permitFields.receiver.value), "contactNumber") ?? "";
    });

    const permitSelections = name => [...permitOptionContainers[name].querySelectorAll('input[type="checkbox"]')]
      .map(input => ({ listItemId: Number(input.value), isSelected: input.checked }));

    const setPermitPreviewMode = preview => {
      permitPreviewMode = preview;
      permitForm.querySelectorAll("input:not([type=hidden]), select, textarea").forEach(control => {
        if (!control.hasAttribute("readonly")) control.disabled = preview;
      });
      permitForm.querySelector("[data-permit-save]").classList.toggle("d-none", preview);
      permitForm.querySelector("[data-permit-finalize]").classList.toggle("d-none", preview);
      permitForm.querySelector("[data-permit-print]").classList.toggle("d-none", !preview);
      permitForm.querySelector("[data-permit-editor-mode]").textContent = preview ? "Preview permit application" : "Edit permit application";
    };

    const printPermitPreview = () => {
      document.body.classList.add("permit-preview-printing");
      window.addEventListener("afterprint", () => document.body.classList.remove("permit-preview-printing"), { once: true });
      window.requestAnimationFrame(() => window.requestAnimationFrame(() => window.print()));
    };

    const openPermitApplication = async (permitId, preview = false, printAfterOpen = false) => {
      if (!permitForm || !permitFields || !permitModal) return;
      permitForm.reset(); permitForm.querySelector("[data-permit-editor-error]").classList.add("d-none");
      setPermitPreviewMode(preview);
      permitModal.show();
      try {
        const [details, users] = await Promise.all([
          window.apcloudApi.json(`permit/applications/${encodeURIComponent(permitId)}${printAfterOpen ? "/print-preview" : preview ? "/preview" : ""}`),
          window.apcloudApi.json("risk-assessments/options/users")
        ]);
        const typeCode = String(property(details, "permitTypeSystemName") ?? "").toUpperCase();
        if (!preview && typeCode !== "HOT_WORK") throw new Error(`Editing permit type '${typeCode || "Unknown"}' is not supported yet.`);
        permitFields.id.value = property(details, "id");
        permitFields.number.value = property(details, "permitNumber") ?? "";
        permitFields.type.value = property(details, "permitTypeName") ?? typeCode;
        permitFields.status.value = property(details, "permitStatusName") ?? "";
        permitFields.issueDate.value = String(property(details, "issueDate") ?? "").slice(0, 10);
        permitFields.plannedStart.value = String(property(details, "plannedStartDateTime") ?? "").slice(0, 16);
        permitFields.plannedEnd.value = String(property(details, "plannedEndDateTime") ?? "").slice(0, 16);
        populatePermitUsers(users, property(details, "permitIssuerId"), property(details, "permitReceiverId"),
          property(details, "permitIssuerName"), property(details, "permitReceiverName"));
        permitFields.riskNumber.value = property(details, "riskAssessmentNumber") ?? "";
        permitFields.workLocation.value = property(details, "workLocation") ?? "";
        permitFields.workDescription.value = property(details, "workDescription") ?? "";
        permitFields.specialInstructions.value = property(details, "specialInstructions") ?? "";
        permitFields.workHeight.value = property(details, "workHeightBelowSurface") ?? "";
        permitFields.completion.value = property(details, "completionOfWorks") ?? "";
        permitForm.querySelector("[data-permit-editor-title]").textContent = property(details, "permitNumber") || "Permit application";
        permitForm.querySelector("[data-permit-print-title]").textContent = `Permit application ${property(details, "permitNumber") || ""}`;
        const isHotWork = typeCode === "HOT_WORK";
        permitForm.querySelector("[data-permit-hot-work]").classList.toggle("d-none", !isHotWork);
        permitForm.querySelector("[data-permit-unsupported-preview]").classList.toggle("d-none", isHotWork);
        if (isHotWork) permitOptionNames.forEach(name => renderPermitOptions(name, property(details, name) ?? [], preview));
        setPermitPreviewMode(preview);
        if (printAfterOpen) window.setTimeout(printPermitPreview, 350);
      } catch (error) {
        const box = permitForm.querySelector("[data-permit-editor-error]");
        box.textContent = error.message || "The permit application could not be loaded."; box.classList.remove("d-none");
        permitForm.querySelector("[data-permit-save]").classList.add("d-none");
        permitForm.querySelector("[data-permit-finalize]").classList.add("d-none");
      }
    };

    const savePermitApplication = async finalize => {
      permitFields.plannedEnd.setCustomValidity(permitFields.plannedStart.value && permitFields.plannedEnd.value
        && permitFields.plannedEnd.value < permitFields.plannedStart.value
        ? "Planned end date/time must be on or after planned start date/time." : "");
      if (permitPreviewMode || !permitForm.reportValidity()) return;
      const saveButton = permitForm.querySelector("[data-permit-save]");
      const finalizeButton = permitForm.querySelector("[data-permit-finalize]");
      const activeButton = finalize ? finalizeButton : saveButton;
      const errorBox = permitForm.querySelector("[data-permit-editor-error]");
      saveButton.disabled = true; finalizeButton.disabled = true; errorBox.classList.add("d-none");
      activeButton.querySelector(finalize ? "[data-permit-finalize-label]" : "[data-permit-save-label]").classList.add("d-none");
      activeButton.querySelector(finalize ? "[data-permit-finalizing]" : "[data-permit-saving]").classList.remove("d-none");
      try {
        await window.apcloudApi.json(`permit/applications/${encodeURIComponent(permitFields.id.value)}${finalize ? "/finalize" : ""}`, {
          method: "PUT",
          body: {
            issueDate: permitFields.issueDate.value, permitIssuerId: Number(permitFields.issuer.value),
            plannedStartDateTime: permitFields.plannedStart.value || null,
            plannedEndDateTime: permitFields.plannedEnd.value || null,
            permitIssuerContactNumber: permitFields.issuerContact.value || null,
            permitReceiverId: Number(permitFields.receiver.value), permitReceiverContactNumber: permitFields.receiverContact.value || null,
            workLocation: permitFields.workLocation.value, workDescription: permitFields.workDescription.value,
            specialInstructions: permitFields.specialInstructions.value || null,
            workHeightBelowSurface: permitFields.workHeight.value || null, completionOfWorks: permitFields.completion.value || null,
            inspectionPriorToCommencement: permitSelections("inspectionPriorToCommencement"),
            worksOnWall: permitSelections("worksOnWall"), workingInConfinedSpace: permitSelections("workingInConfinedSpace")
          }
        });
        permitModal.hide();
        if (activePermitRiskId && activePermitContainer) loadPermitApplications(activePermitRiskId, activePermitContainer);
        notify("success", finalize
          ? "The permit application was updated and finalized for approval."
          : "The permit application details and HOT_WORK controls were saved without changing its status.",
          finalize ? "Permit finalized" : "Draft saved");
      } catch (error) {
        errorBox.textContent = error.message || "The permit application could not be saved."; errorBox.classList.remove("d-none");
      } finally {
        saveButton.disabled = false; finalizeButton.disabled = false;
        activeButton.querySelector(finalize ? "[data-permit-finalize-label]" : "[data-permit-save-label]").classList.remove("d-none");
        activeButton.querySelector(finalize ? "[data-permit-finalizing]" : "[data-permit-saving]").classList.add("d-none");
      }
    };
    permitForm?.addEventListener("submit", event => {
      event.preventDefault();
      savePermitApplication(false);
    });
    permitForm?.querySelector("[data-permit-finalize]")?.addEventListener("click", () => savePermitApplication(true));
    permitForm?.querySelector("[data-permit-print]")?.addEventListener("click", printPermitPreview);

    const renderPermitApplications = (container, permits) => {
      container.replaceChildren();
      const panel = document.createElement("div"); panel.className = "risk-related-permits";
      const header = document.createElement("div"); header.className = "risk-related-permits-header";
      const headingCopy = document.createElement("div");
      const title = document.createElement("h4"); title.textContent = "Related permit applications";
      const hint = document.createElement("p"); hint.textContent = "Permit applications created from this risk assessment.";
      const count = document.createElement("span"); count.className = "badge bg-blue-lt";
      count.textContent = `${permits.length} ${permits.length === 1 ? "permit" : "permits"}`;
      headingCopy.append(title, hint); header.append(headingCopy, count); panel.append(header);

      if (!permits.length) {
        const empty = document.createElement("div"); empty.className = "risk-related-permits-empty";
        empty.textContent = "No permit applications have been created for this risk assessment.";
        panel.append(empty); container.append(panel); return;
      }

      const scroll = document.createElement("div"); scroll.className = "table-responsive";
      const table = document.createElement("table"); table.className = "table table-vcenter risk-related-permits-table mb-0";
      const head = document.createElement("thead");
      const headRow = document.createElement("tr");
      ["Permit number", "Issue date", "Permit type", "Issuer", "Receiver", "Status", "Action"].forEach(label => {
        const cell = document.createElement("th"); cell.textContent = label; headRow.append(cell);
      });
      head.append(headRow); table.append(head);
      const body = document.createElement("tbody");
      permits.forEach(permit => {
        const row = document.createElement("tr");
        const permitNumber = property(permit, "permitNumber") || "—";
        row.append(
          createPermitCell(permitNumber, "fw-semibold"),
          createPermitCell(displayDate(property(permit, "issueDate"))),
          createPermitCell(property(permit, "permitTypeName")),
          createPermitCell(property(permit, "permitIssuerName")),
          createPermitCell(property(permit, "permitReceiverName"))
        );
        const statusCell = document.createElement("td");
        const status = property(permit, "permitStatusName") || "Unknown";
        const badge = document.createElement("span"); badge.className = `badge ${permitStatusClass(status)}`; badge.textContent = status;
        statusCell.append(badge); row.append(statusCell);
        const actionCell = document.createElement("td");
        const dropdown = document.createElement("div"); dropdown.className = "dropdown dropstart risk-permit-action-dropdown";
        const trigger = document.createElement("button");
        trigger.type = "button"; trigger.className = "btn btn-sm btn-icon btn-ghost-secondary risk-permit-actions";
        trigger.dataset.bsToggle = "dropdown"; trigger.dataset.bsBoundary = "viewport"; trigger.setAttribute("aria-expanded", "false");
        trigger.setAttribute("aria-label", `Actions for ${permitNumber}`); trigger.textContent = "⋯";
        const menu = document.createElement("div"); menu.className = "dropdown-menu";
        ["Preview", "Edit", "Finalize", "Print"].forEach(action => {
          const item = document.createElement("button"); item.type = "button"; item.className = "dropdown-item";
          item.textContent = action;
          const permitId = property(permit, "id");
          const isHotWork = String(property(permit, "permitTypeSystemName") ?? "").toUpperCase() === "HOT_WORK";
          const statusCode = String(property(permit, "permitStatusSystemName") ?? "").toUpperCase();
          const isEditable = statusCode === "PERMIT_DRAFT" || statusCode === "PERMIT_REJECTED" || statusCode === "FINALIZED_FOR_APPROVAL";
          if (action === "Preview") item.addEventListener("click", () => openPermitApplication(permitId, true));
          else if (action === "Print") item.addEventListener("click", () => openPermitApplication(permitId, true, true));
          else if (action === "Edit") {
            item.disabled = !isHotWork || !isEditable;
            item.title = !isHotWork ? "A type-specific form will be added later."
              : isEditable ? "Edit this HOT_WORK permit application." : "This permit status cannot be edited.";
            if (isHotWork && isEditable) item.addEventListener("click", () => openPermitApplication(permitId));
          } else {
            item.disabled = true;
            item.title = "Finalize remains unchanged and will be enabled in its workflow step.";
          }
          menu.append(item);
        });
        dropdown.append(trigger, menu); actionCell.append(dropdown); row.append(actionCell); body.append(row);
      });
      table.append(body); scroll.append(table); panel.append(scroll); container.append(panel);
    };

    const loadPermitApplications = async (riskAssessmentId, container) => {
      activePermitRiskId = riskAssessmentId;
      activePermitContainer = container;
      container.replaceChildren();
      const loading = document.createElement("div"); loading.className = "risk-related-permits-loading";
      const spinner = document.createElement("span"); spinner.className = "spinner-border spinner-border-sm text-primary";
      const text = document.createElement("span"); text.textContent = "Loading related permit applications…";
      loading.append(spinner, text); container.append(loading);
      try {
        const result = await window.apcloudApi.json(`risk-assessments/${encodeURIComponent(riskAssessmentId)}/permit-applications`);
        const permits = Array.isArray(result) ? result : (property(result, "data") ?? []);
        renderPermitApplications(container, permits);
      } catch (error) {
        container.replaceChildren();
        const alert = document.createElement("div"); alert.className = "alert alert-danger m-3 d-flex align-items-center gap-3";
        const message = document.createElement("span"); message.textContent = error.message || "Related permit applications could not be loaded.";
        const retry = document.createElement("button"); retry.type = "button"; retry.className = "btn btn-sm btn-outline-danger ms-auto"; retry.textContent = "Retry";
        retry.addEventListener("click", () => loadPermitApplications(riskAssessmentId, container));
        alert.append(message, retry); container.append(alert);
      }
    };

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
          ["Permit issuer", selectedUserName(fields.issuer)], ["Permit receiver", selectedUserName(fields.receiver)],
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
      const categoriesPromise = window.apcloudApi.json("risk-assessments/options/categories");
      const usersPromise = window.apcloudApi.json("risk-assessments/options/users");
      const users = await usersPromise;
      [fields.issuer, fields.receiver].forEach(select => {
        select.replaceChildren();
        const prompt = document.createElement("option"); prompt.value = ""; prompt.textContent = "Select a user";
        select.append(prompt);
        users.forEach(user => {
          const option = document.createElement("option");
          option.value = String(property(user, "id"));
          option.textContent = property(user, "name") || `User ${option.value}`;
          option.dataset.currentUser = property(user, "isCurrentUser") === true ? "true" : "false";
          select.append(option);
        });
      });
      await Promise.all(groupNames.map(async name => {
        try {
          let categoryName = "PermitType";
          if (name !== "specialPermits") {
            const categories = await categoriesPromise;
            categoryName = property(categories.find(item => categoryMatchers[name]?.(normalizeCode(
              `${property(item, "code") ?? ""} ${property(item, "name") ?? ""}`))), "name");
          }
          if (!categoryName) return renderOptions(name, []);
          const items = await window.apcloudApi.json(`risk-assessments/options/category/${encodeURIComponent(categoryName)}`);
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
      fields.number.value = property(record, "riskAssessmentNumber") || "";
      fields.issueDate.value = String(property(record, "issueDate") || "").slice(0, 10);
      fields.location.value = property(record, "locationOfWork") || "";
      fields.start.value = localDateTime(property(record, "plannedStartDateTime"));
      fields.end.value = localDateTime(property(record, "plannedEndDateTime"));
      fields.issuer.value = String(property(record, "permitIssuerUserId") || "");
      fields.receiver.value = String(property(record, "permitReceiverUserId") || "");
      fields.responsible.value = property(record, "areaResponsibleName") || "";
      fields.description.value = property(record, "descriptionOfWork") || "";
      fields.instructions.value = property(record, "specialInstructions") || "";
      fields.otherPpe.value = property(record, "otherEquipmentsPPE") || "";
      fields.otherMeasures.value = property(record, "otherProtectionMeasures") || "";
      groupNames.forEach(name => markSelections(name, property(record, name)));
    };

    const openEditor = async id => {
      editingId = id ? Number(id) : null;
      creatingWorkflow = !editingId;
      form.reset(); form.classList.remove("was-validated"); errorBox.classList.add("d-none");
      groupNames.forEach(name => optionContainers[name].querySelectorAll("input:checked").forEach(input => input.checked = false));
      fields.issueDate.value = new Date().toISOString().slice(0, 10);
      modalElement.querySelector("[data-risk-mode]").textContent = editingId ? "Edit risk assessment" : "Add risk assessment";
      modalElement.querySelector("[data-risk-title]").textContent = editingId ? "Update draft risk assessment" : "New risk assessment";
      form.querySelector("[data-risk-save-label]").textContent = "Save & close";
      setStep(0); modal?.show();
      try {
        await loadLookups();
        if (editingId) {
          const record = await window.apcloudApi.json(`risk-assessments/${editingId}`);
          const status = String(property(record, "riskAssessmentStatus") || "").toLowerCase();
          if (status !== "draft" && status !== "rejected")
            throw new Error("Only Draft or Rejected risk assessments can be edited.");
          populate(record);
        } else {
          const currentIssuer = [...fields.issuer.options]
            .find(option => option.dataset.currentUser === "true");
          if (currentIssuer) fields.issuer.value = currentIssuer.value;
        }
        fields.issueDate.focus();
      } catch (error) {
        errorBox.textContent = error.message || "The risk assessment form could not be loaded.";
        errorBox.classList.remove("d-none"); saveButton.disabled = true;
      }
    };

    document.querySelectorAll("[data-risk-add]").forEach(button => button.addEventListener("click", () => openEditor()));
    grid?.addEventListener("server-grid:action", event => {
      if (event.detail?.action === "edit") openEditor(property(event.detail.record, "id"));
    });
    grid?.addEventListener("server-grid:expand", event => {
      const { id, row, button, columnCount } = event.detail ?? {};
      if (!id || !row || !button) return;
      const existing = row.nextElementSibling?.dataset.riskPermitChildFor === String(id)
        ? row.nextElementSibling
        : null;
      if (existing) {
        existing.remove();
        row.classList.remove("is-expanded");
        button.classList.remove("is-expanded");
        button.setAttribute("aria-expanded", "false");
        button.removeAttribute("aria-controls");
        return;
      }

      grid.querySelectorAll(".risk-permit-child-row").forEach(child => child.remove());
      grid.querySelectorAll("[data-grid-record-id].is-expanded").forEach(parent => parent.classList.remove("is-expanded"));
      grid.querySelectorAll(".server-grid-expand.is-expanded").forEach(expandButton => {
        expandButton.classList.remove("is-expanded");
        expandButton.setAttribute("aria-expanded", "false");
        expandButton.removeAttribute("aria-controls");
      });

      const childRow = document.createElement("tr");
      childRow.className = "risk-permit-child-row";
      childRow.dataset.riskPermitChildFor = String(id);
      childRow.id = `risk-permits-${id}`;
      const childCell = document.createElement("td"); childCell.colSpan = Number(columnCount) || row.children.length;
      const content = document.createElement("div"); content.className = "risk-permit-child-content";
      childCell.append(content); childRow.append(childCell); row.after(childRow);
      row.classList.add("is-expanded");
      button.classList.add("is-expanded");
      button.setAttribute("aria-expanded", "true");
      button.setAttribute("aria-controls", childRow.id);
      loadPermitApplications(id, content);
    });
    fields.start.addEventListener("change", validateDates); fields.end.addEventListener("change", validateDates);
    form.addEventListener("input", () => { if (currentStep === panels.length - 1) renderReview(); });
    form.addEventListener("change", () => { if (currentStep === panels.length - 1) renderReview(); });
    const saveDraft = async () => {
      const isCreate = !editingId;
      const url = isCreate
        ? "risk-assessments"
        : (creatingWorkflow ? `risk-assessments/${editingId}/creation-progress` : `risk-assessments/${editingId}`);
      const result = await window.apcloudApi.json(url, {
        method: isCreate ? "POST" : "PUT",
        body: payload()
      });
      editingId = Number(property(result, "riskAssessmentId")) || editingId;
      fields.id.value = String(editingId || "");
      fields.number.value = property(result, "riskAssessmentNumber") || fields.number.value;
      return result;
    };

    previousButton.addEventListener("click", () => setStep(currentStep - 1));
    nextButton.addEventListener("click", async () => {
      if (!validateStep(currentStep)) return;
      nextButton.disabled = true;
      errorBox.classList.add("d-none");
      nextButton.querySelector("[data-risk-next-label]").classList.add("d-none");
      nextButton.querySelector("[data-risk-next-saving]").classList.remove("d-none");
      try {
        await saveDraft();
        grid?.dispatchEvent(new CustomEvent("server-grid:reload"));
        setStep(currentStep + 1);
      } catch (error) {
        errorBox.textContent = error.message || "The risk assessment could not be saved.";
        errorBox.classList.remove("d-none");
      } finally {
        nextButton.disabled = false;
        nextButton.querySelector("[data-risk-next-label]").classList.remove("d-none");
        nextButton.querySelector("[data-risk-next-saving]").classList.add("d-none");
      }
    });
    stepButtons.forEach((button, index) => button.addEventListener("click", () => {
      if (index <= currentStep) setStep(index);
    }));

    form.addEventListener("submit", async event => {
      event.preventDefault();
      if (!panels.every((_, index) => validateStep(index))) return;
      saveButton.disabled = true; errorBox.classList.add("d-none");
      form.querySelector("[data-risk-save-label]").classList.add("d-none");
      form.querySelector("[data-risk-saving]").classList.remove("d-none");
      const wasCreating = creatingWorkflow;
      try {
        await saveDraft();
        modal?.hide();
        grid?.dispatchEvent(new CustomEvent("server-grid:reload"));
        notify("success", wasCreating ? "The risk assessment was created as a draft." : "The draft risk assessment was updated.", "Risk assessment saved");
      } catch (error) {
        errorBox.textContent = error.message || "The risk assessment could not be saved.";
        errorBox.classList.remove("d-none");
      } finally {
        saveButton.disabled = false;
        form.querySelector("[data-risk-save-label]").classList.remove("d-none");
        form.querySelector("[data-risk-saving]").classList.add("d-none");
      }
    });

    modalElement.addEventListener("hidden.bs.modal", () => { editingId = null; creatingWorkflow = false; saveButton.disabled = false; });
    const query = new URLSearchParams(window.location.search);
    if (query.get("create") === "true") openEditor();
    else if (/^\d+$/.test(query.get("edit") || "")) openEditor(query.get("edit"));
  });
})();
