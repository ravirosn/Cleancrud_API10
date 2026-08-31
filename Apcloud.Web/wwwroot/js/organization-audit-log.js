(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const root = document.querySelector("[data-audit-grid]");
    const modalElement = document.querySelector("[data-audit-modal]");
    if (!root || !modalElement || !window.apcloudApi) return;
    if (modalElement.parentElement !== document.body) document.body.append(modalElement);

    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = ui?.Modal?.getOrCreateInstance(modalElement);
    const elements = {
      form: root.querySelector("[data-audit-filter-form]"),
      search: root.querySelector("[data-audit-search]"),
      entity: root.querySelector("[data-audit-entity]"),
      action: root.querySelector("[data-audit-action]"),
      changedBy: root.querySelector("[data-audit-changed-by]"),
      from: root.querySelector("[data-audit-from]"),
      to: root.querySelector("[data-audit-to]"),
      pageSize: root.querySelector("[data-audit-page-size]"),
      clear: root.querySelector("[data-audit-clear]"),
      retry: root.querySelector("[data-audit-retry]"),
      body: root.querySelector("[data-audit-body]"),
      summary: root.querySelector("[data-audit-summary]"),
      pagination: root.querySelector("[data-audit-pagination]"),
      loading: root.querySelector("[data-audit-loading]"),
      error: root.querySelector("[data-audit-error]"),
      empty: root.querySelector("[data-audit-empty]"),
      content: root.querySelector("[data-audit-content]"),
      exportButton: document.querySelector("[data-audit-export]"),
      exportLabel: document.querySelector("[data-audit-export-label]"),
      exportProgress: document.querySelector("[data-audit-export-progress]")
    };
    const state = { pageNumber: 1, pageSize: 25, sortBy: "changedAtUtc", sortDirection: "desc" };
    let abortController = null;

    const property = (record, name) => Object.entries(record ?? {})
      .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.[1];
    const create = (tag, className, text) => {
      const element = document.createElement(tag);
      if (className) element.className = className;
      if (text !== undefined) element.textContent = text;
      return element;
    };
    const notify = (type, message, title) => window.apcloudNotifications?.[type]?.(message, title);
    const parseJson = (value, fallback) => {
      if (!value) return fallback;
      if (typeof value === "object") return value;
      try { return JSON.parse(value); } catch { return fallback; }
    };
    const humanize = (value) => {
      const text = String(value ?? "").replace(/([a-z0-9])([A-Z])/g, "$1 $2").replaceAll("_", " ");
      return text ? text.charAt(0).toUpperCase() + text.slice(1) : "—";
    };
    const formatDate = (value) => {
      const date = new Date(value);
      return Number.isNaN(date.valueOf()) ? "—" : new Intl.DateTimeFormat(undefined, {
        year: "numeric", month: "short", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit"
      }).format(date);
    };
    const displayValue = (value, relatedName) => {
      if (relatedName) return String(relatedName);
      if (value === null || value === undefined || value === "") return "—";
      if (typeof value === "boolean") return value ? "Yes" : "No";
      if (typeof value === "object") return JSON.stringify(value, null, 2);
      return String(value);
    };
    const actionClass = (action) => {
      const normalized = String(action ?? "").toLowerCase();
      if (["added", "insert", "created"].includes(normalized)) return "bg-green-lt";
      if (["deleted", "delete", "removed"].includes(normalized)) return "bg-red-lt";
      return "bg-blue-lt";
    };

    const dateBoundary = (value, endOfDay) => {
      if (!value) return null;
      const date = new Date(`${value}T${endOfDay ? "23:59:59.999" : "00:00:00.000"}`);
      return Number.isNaN(date.valueOf()) ? null : date.toISOString();
    };

    const buildQuery = (includePaging = true) => {
      const query = new URLSearchParams();
      if (includePaging) {
        query.set("pageNumber", String(state.pageNumber));
        query.set("pageSize", String(state.pageSize));
      }
      const values = [
        ["search", elements.search.value.trim()],
        ["entityName", elements.entity.value.trim()],
        ["action", elements.action.value],
        ["changedBy", elements.changedBy.value.trim()],
        ["fromUtc", dateBoundary(elements.from.value, false)],
        ["toUtc", dateBoundary(elements.to.value, true)],
        ["sortBy", state.sortBy],
        ["sortDirection", state.sortDirection]
      ];
      values.forEach(([name, value]) => { if (value) query.set(name, value); });
      return query;
    };

    const validateDates = () => {
      if (!elements.from.value || !elements.to.value || elements.from.value <= elements.to.value) return true;
      notify("error", "The from date cannot be later than the to date.", "Invalid date range");
      elements.from.focus();
      return false;
    };

    const loadFilterOptions = async () => {
      try {
        const options = await window.apcloudApi.json("audit-logs/filter-options");
        const appendOptions = (select, values, placeholder) => {
          const selected = select.value;
          select.replaceChildren(create("option", null, placeholder));
          select.firstElementChild.value = "";
          (values ?? []).forEach((value) => {
            const option = create("option", null, value);
            option.value = value;
            select.append(option);
          });
          select.value = selected;
        };
        appendOptions(elements.entity, property(options, "entityNames"), "All entities");
        appendOptions(elements.action, property(options, "actions"), "All actions");
      } catch (error) {
        notify("error", error.message || "Filter options could not be loaded.", "Audit filters unavailable");
      }
    };

    const showState = (name) => {
      Object.entries({
        loading: elements.loading,
        error: elements.error,
        empty: elements.empty,
        content: elements.content
      }).forEach(([key, element]) => element?.classList.toggle("d-none", key !== name));
    };

    const updateSortHeaders = () => {
      root.querySelectorAll("[data-audit-sort]").forEach((button) => {
        const active = button.dataset.auditSort === state.sortBy;
        button.classList.toggle("active", active);
        button.closest("th")?.setAttribute("aria-sort", active
          ? (state.sortDirection === "asc" ? "ascending" : "descending") : "none");
        const icon = button.querySelector("[data-audit-sort-icon]");
        if (icon) icon.textContent = active ? (state.sortDirection === "asc" ? "↑" : "↓") : "⇅";
      });
    };

    const pageButton = (label, page, disabled, active = false) => {
      const item = create("li", `page-item${disabled ? " disabled" : ""}${active ? " active" : ""}`);
      const button = create("button", "page-link", label);
      button.type = "button";
      button.disabled = disabled;
      button.addEventListener("click", () => { state.pageNumber = page; load(); });
      item.append(button);
      return item;
    };

    const renderPagination = (totalPages) => {
      elements.pagination.replaceChildren();
      if (totalPages <= 1) return;
      const first = Math.max(1, state.pageNumber - 2);
      const last = Math.min(totalPages, state.pageNumber + 2);
      elements.pagination.append(pageButton("Previous", state.pageNumber - 1, state.pageNumber === 1));
      for (let page = first; page <= last; page++)
        elements.pagination.append(pageButton(String(page), page, false, page === state.pageNumber));
      elements.pagination.append(pageButton("Next", state.pageNumber + 1, state.pageNumber === totalPages));
    };

    const openDetails = (record) => {
      const oldValues = parseJson(property(record, "oldValues"), {});
      const newValues = parseJson(property(record, "newValues"), {});
      const relatedNames = parseJson(property(record, "relatedNames"), {});
      const changedColumns = parseJson(property(record, "changedColumns"), []);
      const keys = [...new Set([
        ...(Array.isArray(changedColumns) ? changedColumns : []),
        ...Object.keys(oldValues ?? {}), ...Object.keys(newValues ?? {})
      ])];

      modalElement.querySelector("[data-audit-modal-entity]").textContent = property(record, "entityName") || "Audit record";
      modalElement.querySelector("[data-audit-modal-title]").textContent = property(record, "entityDisplayName") || "Change details";
      const meta = modalElement.querySelector("[data-audit-detail-meta]");
      meta.replaceChildren();
      [
        ["Action", property(record, "action")],
        ["Changed by", property(record, "changedByName") || "System"],
        ["Changed at", formatDate(property(record, "changedAtUtc"))],
        ["IP address", property(record, "ipAddress") || "Not recorded"]
      ].forEach(([label, value]) => {
        const item = create("div", "audit-detail-meta-item");
        item.append(create("span", null, label), create("strong", null, value));
        meta.append(item);
      });

      const body = modalElement.querySelector("[data-audit-diff-body]");
      body.replaceChildren();
      if (!keys.length) {
        const row = document.createElement("tr");
        const cell = create("td", "text-secondary text-center py-4", "No field-level values were recorded for this event.");
        cell.colSpan = 3;
        row.append(cell);
        body.append(row);
      } else {
        keys.forEach((key) => {
          const row = document.createElement("tr");
          const oldValue = displayValue(oldValues?.[key], relatedNames?.[key]);
          const newValue = displayValue(newValues?.[key], relatedNames?.[key]);
          row.append(create("td", "audit-diff-field", humanize(key)));
          row.append(create("td", "audit-diff-old", oldValue));
          row.append(create("td", "audit-diff-new", newValue));
          body.append(row);
        });
      }

      const technical = modalElement.querySelector("[data-audit-technical]");
      technical.replaceChildren();
      [
        ["Audit ID", property(record, "id")],
        ["Entity key", property(record, "entityKey")],
        ["Trace ID", property(record, "traceId")],
        ["Changed-by user ID", property(record, "changedByUserId")]
      ].forEach(([label, value]) => {
        technical.append(create("dt", "col-sm-3", label), create("dd", "col-sm-9 text-break", displayValue(value)));
      });
      modal?.show();
    };

    const render = (records, payload) => {
      elements.body.replaceChildren();
      records.forEach((record) => {
        const row = document.createElement("tr");
        const changedAt = create("td", "text-nowrap", formatDate(property(record, "changedAtUtc")));

        const entity = document.createElement("td");
        entity.append(create("strong", "d-block", property(record, "entityDisplayName") || property(record, "entityName") || "—"));
        entity.append(create("span", "text-secondary small", property(record, "entityName") || "—"));

        const action = document.createElement("td");
        action.append(create("span", `badge ${actionClass(property(record, "action"))}`, property(record, "action") || "Unknown"));

        const columns = parseJson(property(record, "changedColumns"), []);
        const fields = document.createElement("td");
        const fieldNames = Array.isArray(columns) ? columns.map(humanize) : [];
        const fieldText = fieldNames.length ? fieldNames.join(", ") : "No field list";
        const fieldContent = create("div", "audit-changed-fields", fieldText);
        fieldContent.title = fieldText;
        fields.append(fieldContent);

        const actor = document.createElement("td");
        actor.append(create("strong", "d-block", property(record, "changedByName") || "System"));
        actor.append(create("span", "text-secondary small", property(record, "ipAddress") || "IP not recorded"));

        const details = document.createElement("td");
        const button = create("button", "btn btn-sm btn-outline-primary", "View changes");
        button.type = "button";
        button.addEventListener("click", () => openDetails(record));
        details.append(button);
        row.append(changedAt, entity, action, fields, actor, details);
        elements.body.append(row);
      });

      const total = Number(property(payload, "totalRecords")) || 0;
      const totalPages = Number(property(payload, "totalPages")) || 0;
      const first = total ? ((state.pageNumber - 1) * state.pageSize) + 1 : 0;
      const last = Math.min(state.pageNumber * state.pageSize, total);
      elements.summary.textContent = `Showing ${first} to ${last} of ${new Intl.NumberFormat().format(total)} audit records`;
      renderPagination(totalPages);
    };

    const load = async () => {
      if (!validateDates()) return;
      abortController?.abort();
      abortController = new AbortController();
      showState("loading");
      root.setAttribute("aria-busy", "true");
      try {
        const payload = await window.apcloudApi.json(`audit-logs?${buildQuery()}`, { signal: abortController.signal });
        const records = property(payload, "data") ?? [];
        state.pageNumber = Number(property(payload, "pageNumber")) || state.pageNumber;
        render(records, payload);
        showState(records.length ? "content" : "empty");
      } catch (error) {
        if (error.name === "AbortError") return;
        root.querySelector("[data-audit-error-message]").textContent = error.message || "The audit data could not be loaded.";
        showState("error");
      } finally {
        root.removeAttribute("aria-busy");
      }
    };

    const exportExcel = async () => {
      if (!validateDates()) return;
      elements.exportButton.disabled = true;
      elements.exportLabel.classList.add("d-none");
      elements.exportProgress.classList.remove("d-none");
      try {
        const response = await window.apcloudApi.request(`audit-logs/export?${buildQuery(false)}`);
        if (!response.ok) {
          const problem = await response.json().catch(() => null);
          throw new Error(problem?.detail || problem?.title || `Export failed with status ${response.status}.`);
        }
        const blob = await response.blob();
        const disposition = response.headers.get("Content-Disposition") || "";
        const match = disposition.match(/filename\*?=(?:UTF-8''|\")?([^\";]+)/i);
        const fileName = match ? decodeURIComponent(match[1].replaceAll("\"", "")) : "AuditLogs.xlsx";
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        document.body.append(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
        notify("success", "The filtered audit logs were exported successfully.", "Excel export ready");
      } catch (error) {
        notify("error", error.message || "The Excel export could not be created.", "Unable to export audit logs");
      } finally {
        elements.exportButton.disabled = false;
        elements.exportLabel.classList.remove("d-none");
        elements.exportProgress.classList.add("d-none");
      }
    };

    elements.form.addEventListener("submit", (event) => { event.preventDefault(); state.pageNumber = 1; load(); });
    elements.clear.addEventListener("click", () => {
      elements.form.reset();
      elements.pageSize.value = "25";
      state.pageNumber = 1;
      state.pageSize = 25;
      state.sortBy = "changedAtUtc";
      state.sortDirection = "desc";
      updateSortHeaders();
      load();
    });
    elements.pageSize.addEventListener("change", () => { state.pageSize = Number(elements.pageSize.value) || 25; state.pageNumber = 1; load(); });
    elements.retry.addEventListener("click", load);
    elements.exportButton.addEventListener("click", exportExcel);
    root.querySelectorAll("[data-audit-sort]").forEach((button) => button.addEventListener("click", () => {
      const sortBy = button.dataset.auditSort;
      state.sortDirection = state.sortBy === sortBy && state.sortDirection === "asc" ? "desc" : "asc";
      state.sortBy = sortBy;
      state.pageNumber = 1;
      updateSortHeaders();
      load();
    }));

    updateSortHeaders();
    loadFilterOptions().finally(load);
  });
})();
