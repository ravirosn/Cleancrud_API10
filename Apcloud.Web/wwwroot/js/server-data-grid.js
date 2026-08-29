(() => {
  "use strict";

  const itemContainers = ["items", "data", "result", "records", "applications"];
  const metadataContainers = ["data", "result", "pagination", "paging", "meta", "metadata"];
  const technicalFields = new Set(["rowversion", "concurrencystamp", "isdeleted", "deleted"]);
  const priorities = ["reference", "number", "title", "name", "permit", "applicant", "type", "category", "status", "date", "created"];

  const equalsIgnoreCase = (left, right) => left.localeCompare(right, undefined, { sensitivity: "accent" }) === 0;
  const findProperty = (object, names) => {
    if (!object || typeof object !== "object" || Array.isArray(object)) return undefined;
    const entry = Object.entries(object).find(([key]) => names.some((name) => equalsIgnoreCase(key, name)));
    return entry?.[1];
  };

  const findItems = (payload) => {
    if (Array.isArray(payload)) return payload;
    if (!payload || typeof payload !== "object") return [];

    for (const name of itemContainers) {
      const value = findProperty(payload, [name]);
      if (Array.isArray(value)) return value;
      if (value && typeof value === "object") {
        const nested = findItems(value);
        if (nested.length) return nested;
      }
    }

    return [];
  };

  const metadataCandidates = (payload) => {
    const candidates = [];
    const visit = (value, depth = 0) => {
      if (!value || typeof value !== "object" || Array.isArray(value) || depth > 2 || candidates.includes(value)) return;
      candidates.push(value);
      metadataContainers.forEach((name) => visit(findProperty(value, [name]), depth + 1));
    };
    visit(payload);
    return candidates;
  };

  const metadataNumber = (payload, names, fallback) => {
    for (const candidate of metadataCandidates(payload)) {
      const value = Number(findProperty(candidate, names));
      if (Number.isFinite(value) && value >= 0) return value;
    }
    return fallback;
  };

  const isIdentifier = (name) => /^id$/i.test(name) || /ids?$/i.test(name);
  const formatValue = (value) => {
    if (value === null || value === undefined || value === "") return null;
    if (typeof value === "boolean") return value ? "Yes" : "No";
    if (typeof value === "number") return new Intl.NumberFormat().format(value);
    if (typeof value === "string") {
      if (/^\d{4}-\d{2}-\d{2}(?:T|\s|$)/.test(value)) {
        const date = new Date(value);
        if (!Number.isNaN(date.valueOf())) return new Intl.DateTimeFormat(undefined, { day: "2-digit", month: "short", year: "numeric" }).format(date);
      }
      return value;
    }
    if (Array.isArray(value)) {
      const values = value.map(formatValue).filter(Boolean).slice(0, 4);
      return values.length ? values.join(", ") : null;
    }
    return null;
  };

  const flatten = (object, result = new Map(), prefix = "", depth = 0) => {
    if (!object || typeof object !== "object" || Array.isArray(object) || depth > 2) return result;
    Object.entries(object).forEach(([name, value]) => {
      if (isIdentifier(name) || technicalFields.has(name.toLowerCase())) return;
      const key = prefix ? `${prefix}.${name}` : name;
      const display = formatValue(value);
      if (display !== null) result.set(key, display);
      else if (value && typeof value === "object" && !Array.isArray(value)) flatten(value, result, key, depth + 1);
    });
    return result;
  };

  const getRecordId = (record) => {
    if (!record || typeof record !== "object") return null;
    const preferred = findProperty(record, ["permitApplicationId", "applicationId", "id"]);
    if (preferred !== undefined && preferred !== null) return String(preferred);
    const entry = Object.entries(record).find(([key, value]) => /id$/i.test(key) && value !== null && ["string", "number"].includes(typeof value));
    return entry ? String(entry[1]) : null;
  };

  const humanize = (key) => {
    const value = key.replaceAll(".", " ").replace(/([a-z0-9])([A-Z])/g, "$1 $2").replaceAll("_", " ");
    return value.charAt(0).toUpperCase() + value.slice(1);
  };

  const columnPriority = (key) => {
    const normalized = key.toLowerCase();
    const index = priorities.findIndex((value) => normalized.includes(value));
    return index < 0 ? priorities.length : index;
  };

  const createElement = (tag, className, text) => {
    const element = document.createElement(tag);
    if (className) element.className = className;
    if (text !== undefined) element.textContent = text;
    return element;
  };

  class ServerDataGrid {
    constructor(root) {
      this.root = root;
      this.url = root.dataset.url;
      this.editUrlTemplate = root.dataset.editUrlTemplate || "";
      this.rowActions = (root.dataset.rowActions || "").split(",").map((value) => value.trim()).filter(Boolean);
      this.hiddenColumns = (root.dataset.hiddenColumns || "")
        .split(",")
        .map((value) => value.trim().toLowerCase())
        .filter(Boolean);
      this.columnLabels = this.parseColumnLabels(root.dataset.columnLabels);
      this.configuredColumns = [...root.querySelectorAll("[data-grid-column]")]
        .map((element) => ({ key: element.dataset.gridColumn?.trim(), label: element.textContent.trim() }))
        .filter((column) => column.key);
      this.serverSorting = root.dataset.serverSort === "true";
      this.searchParameter = root.dataset.searchParam || "searchTerm";
      this.searchAlias = root.dataset.searchAlias || "";
      this.maxColumns = Number(root.dataset.maxColumns) || 10;
      this.state = {
        pageNumber: 1,
        pageSize: Number(root.dataset.pageSize) || 10,
        searchTerm: "",
        sortBy: root.dataset.defaultSort || "",
        sortDirection: root.dataset.defaultSortDirection === "asc" ? "asc" : "desc"
      };
      this.abortController = null;
      this.searchTimer = null;
      this.elements = {
        search: root.querySelector("[data-grid-search]"),
        searchButton: root.querySelector("[data-grid-search-button]"),
        clearButton: root.querySelector("[data-grid-clear]"),
        pageSize: root.querySelector("[data-grid-page-size]"),
        head: root.querySelector("[data-grid-head]"),
        body: root.querySelector("[data-grid-body]"),
        loading: root.querySelector("[data-grid-loading]"),
        error: root.querySelector("[data-grid-error]"),
        empty: root.querySelector("[data-grid-empty]"),
        content: root.querySelector("[data-grid-content]"),
        summary: root.querySelector("[data-grid-summary]"),
        pagination: root.querySelector("[data-grid-pagination]"),
        retry: root.querySelector("[data-grid-retry]")
      };
    }

    parseColumnLabels(value) {
      if (!value) return {};
      try {
        const labels = JSON.parse(value);
        return labels && typeof labels === "object" && !Array.isArray(labels) ? labels : {};
      } catch {
        console.warn("server-data-grid: data-column-labels must contain a valid JSON object.");
        return {};
      }
    }

    isColumnHidden(key) {
      const normalizedKey = key.toLowerCase();
      const propertyName = normalizedKey.split(".").at(-1);
      return this.hiddenColumns.includes(normalizedKey) || this.hiddenColumns.includes(propertyName);
    }

    columnLabel(key) {
      const entry = Object.entries(this.columnLabels)
        .find(([name]) => equalsIgnoreCase(name, key) || equalsIgnoreCase(name, key.split(".").at(-1)));
      return entry ? String(entry[1]) : humanize(key);
    }

    fieldValue(fields, key) {
      const entry = [...fields.entries()].find(([name]) => equalsIgnoreCase(name, key));
      return entry?.[1] || "—";
    }

    initialize() {
      if (!this.url || !window.apcloudApi) return;
      const search = () => {
        this.state.searchTerm = this.elements.search?.value.trim() || "";
        this.state.pageNumber = 1;
        this.load();
      };
      this.elements.searchButton?.addEventListener("click", search);
      this.elements.search?.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
          event.preventDefault();
          search();
        }
      });
      this.elements.clearButton?.addEventListener("click", () => {
        if (this.elements.search) this.elements.search.value = "";
        this.state.searchTerm = "";
        this.state.pageNumber = 1;
        this.load();
        this.elements.search?.focus();
      });
      this.elements.pageSize?.addEventListener("change", () => {
        this.state.pageSize = Number(this.elements.pageSize.value) || 10;
        this.state.pageNumber = 1;
        this.load();
      });
      this.elements.retry?.addEventListener("click", () => this.load());
      this.elements.head?.addEventListener("click", (event) => {
        const button = event.target.closest("[data-grid-sort]");
        if (!button || !this.elements.head.contains(button)) return;
        const sortBy = button.dataset.gridSort;
        this.state.sortDirection = this.state.sortBy === sortBy && this.state.sortDirection === "asc"
          ? "desc"
          : "asc";
        this.state.sortBy = sortBy;
        this.state.pageNumber = 1;
        this.load();
      });
      this.load();
    }

    async load() {
      this.abortController?.abort();
      this.abortController = new AbortController();
      this.showState("loading");
      this.root.setAttribute("aria-busy", "true");

      const separator = this.url.includes("?") ? "&" : "?";
      const query = new URLSearchParams({
        pageNumber: String(this.state.pageNumber),
        pageSize: String(this.state.pageSize)
      });
      if (this.state.searchTerm) {
        query.set(this.searchParameter, this.state.searchTerm);
        if (this.searchAlias && this.searchAlias !== this.searchParameter) {
          query.set(this.searchAlias, this.state.searchTerm);
        }
      }
      if (this.serverSorting && this.state.sortBy) {
        query.set("sortBy", this.state.sortBy);
        query.set("sortDirection", this.state.sortDirection);
      }

      try {
        const payload = await window.apcloudApi.json(`${this.url}${separator}${query}`, { signal: this.abortController.signal });
        const records = findItems(payload);
        const pageNumber = metadataNumber(payload, ["pageNumber", "currentPage", "page"], this.state.pageNumber);
        const pageSize = metadataNumber(payload, ["pageSize", "limit", "perPage"], this.state.pageSize);
        const totalCount = metadataNumber(payload, ["totalCount", "totalRecords", "totalItems", "recordCount", "total"], records.length);
        const totalPages = metadataNumber(payload, ["totalPages", "pageCount"], totalCount ? Math.ceil(totalCount / pageSize) : 0);

        this.state.pageNumber = Math.max(1, pageNumber);
        this.render(records, { pageNumber: this.state.pageNumber, pageSize, totalCount, totalPages });
        this.showState(records.length ? "content" : "empty");
      } catch (error) {
        if (error.name === "AbortError") return;
        const message = this.elements.error?.querySelector("[data-grid-error-message]");
        if (message) message.textContent = error.message || "The data could not be loaded.";
        this.showState("error");
      } finally {
        this.root.removeAttribute("aria-busy");
      }
    }

    render(records, paging) {
      const flattened = records.map((record) => ({ record, fields: flatten(record), id: getRecordId(record) }));
      const inferredColumns = [...new Set(flattened.flatMap((item) => [...item.fields.keys()]))]
        .filter((key) => !this.isColumnHidden(key))
        .map((key, index) => ({ key, index, priority: columnPriority(key) }))
        .sort((left, right) => left.priority - right.priority || left.index - right.index)
        .slice(0, this.maxColumns)
        .map((column) => ({ key: column.key, label: this.columnLabel(column.key) }));
      const columns = this.configuredColumns.length
        ? this.configuredColumns.filter((column) => !this.isColumnHidden(column.key))
        : inferredColumns;

      this.elements.head.replaceChildren();
      columns.forEach((column) => {
        const header = document.createElement("th");
        const isActiveSort = this.serverSorting && this.state.sortBy === column.key;
        if (this.serverSorting) {
          header.setAttribute("aria-sort", isActiveSort
            ? (this.state.sortDirection === "asc" ? "ascending" : "descending")
            : "none");
          const button = createElement("button", `server-grid-sort${isActiveSort ? " active" : ""}`);
          button.type = "button";
          button.dataset.gridSort = column.key;
          button.append(createElement("span", null, column.label));
          button.append(createElement("span", "server-grid-sort-icon", isActiveSort
            ? (this.state.sortDirection === "asc" ? "↑" : "↓")
            : "↕"));
          header.append(button);
        } else {
          header.textContent = column.label;
        }
        this.elements.head.append(header);
      });
      if (this.editUrlTemplate || this.rowActions.length) {
        const actionHeader = createElement("th", "w-1");
        actionHeader.append(createElement("span", "visually-hidden", "Actions"));
        this.elements.head.append(actionHeader);
      }

      this.elements.body.replaceChildren();
      flattened.forEach((item) => {
        const row = document.createElement("tr");
        columns.forEach((column) => {
          const cell = document.createElement("td");
          const value = this.fieldValue(item.fields, column.key);
          const normalized = column.key.toLowerCase();
          if (normalized.includes("status") || normalized.includes("level") || normalized.includes("rating")) {
            const badge = createElement("span", `badge ${this.badgeClass(value)}`, value);
            cell.append(badge);
          } else {
            const content = createElement("div", "server-grid-cell", value);
            content.title = value;
            cell.append(content);
          }
          row.append(cell);
        });

        if (this.editUrlTemplate || this.rowActions.length) {
          const cell = document.createElement("td");
          if (this.rowActions.length) {
            cell.append(this.createActionMenu(item));
          } else if (item.id) {
            const link = createElement("a", "btn btn-sm btn-ghost-primary", "Edit");
            link.href = this.editUrlTemplate.replace("{id}", encodeURIComponent(item.id));
            cell.append(link);
          }
          row.append(cell);
        }
        this.elements.body.append(row);
      });

      const first = paging.totalCount ? ((paging.pageNumber - 1) * paging.pageSize) + 1 : 0;
      const last = Math.min(paging.pageNumber * paging.pageSize, paging.totalCount);
      this.elements.summary.textContent = `Showing ${first} to ${last} of ${paging.totalCount}`;
      this.renderPagination(paging.totalPages);
    }

    createActionMenu(item) {
      const dropdown = createElement("div", "dropdown");
      const trigger = createElement("button", "btn btn-sm btn-icon btn-ghost-secondary server-grid-actions", "⋯");
      trigger.type = "button";
      trigger.dataset.bsToggle = "dropdown";
      trigger.setAttribute("aria-expanded", "false");
      trigger.setAttribute("aria-label", "Row actions");
      const menu = createElement("div", "dropdown-menu dropdown-menu-end");

      this.rowActions.forEach((action) => {
        if (action === "edit" && item.id && this.editUrlTemplate) {
          const link = createElement("a", "dropdown-item", this.actionLabel(action));
          link.href = this.editUrlTemplate.replace("{id}", encodeURIComponent(item.id));
          menu.append(link);
          return;
        }
        const button = createElement("button", "dropdown-item", this.actionLabel(action));
        button.type = "button";
        button.addEventListener("click", () => {
          this.root.dispatchEvent(new CustomEvent("server-grid:action", {
            bubbles: true,
            detail: { action, id: item.id, record: item.record }
          }));
        });
        menu.append(button);
      });

      dropdown.append(trigger, menu);
      return dropdown;
    }

    actionLabel(action) {
      const value = action.replaceAll("-", " ").replaceAll("_", " ");
      return value.charAt(0).toUpperCase() + value.slice(1);
    }

    renderPagination(totalPages) {
      this.elements.pagination.replaceChildren();
      if (totalPages <= 1) return;
      const first = Math.max(1, this.state.pageNumber - 2);
      const last = Math.min(totalPages, this.state.pageNumber + 2);
      this.elements.pagination.append(this.pageButton("Previous", this.state.pageNumber - 1, this.state.pageNumber === 1));
      for (let page = first; page <= last; page += 1) {
        this.elements.pagination.append(this.pageButton(String(page), page, false, page === this.state.pageNumber));
      }
      this.elements.pagination.append(this.pageButton("Next", this.state.pageNumber + 1, this.state.pageNumber === totalPages));
    }

    pageButton(label, page, disabled, active = false) {
      const item = createElement("li", `page-item${disabled ? " disabled" : ""}${active ? " active" : ""}`);
      const button = createElement("button", "page-link", label);
      button.type = "button";
      button.disabled = disabled;
      button.addEventListener("click", () => {
        this.state.pageNumber = page;
        this.load();
      });
      item.append(button);
      return item;
    }

    badgeClass(value) {
      switch (value.toLowerCase()) {
        case "approved": case "active": case "completed": case "low": return "bg-green-lt";
        case "pending": case "submitted": case "medium": case "moderate": return "bg-yellow-lt";
        case "rejected": case "inactive": case "critical": case "extreme": return "bg-red-lt";
        case "high": return "bg-orange-lt";
        default: return "bg-blue-lt";
      }
    }

    showState(state) {
      Object.entries({ loading: this.elements.loading, error: this.elements.error, empty: this.elements.empty, content: this.elements.content })
        .forEach(([name, element]) => element?.classList.toggle("d-none", name !== state));
    }
  }

  document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-server-grid]").forEach((root) => new ServerDataGrid(root).initialize());
  });
})();
