(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const grid = document.querySelector("[data-guideline-grid]");
    const modalElement = document.querySelector("[data-guideline-modal]");
    const form = modalElement?.querySelector("[data-guideline-form]");
    const editorElement = form?.querySelector("[data-guideline-editor]");
    if (!grid || !modalElement || !form || !editorElement || !window.apcloudApi || !window.Quill) return;
    if (modalElement.parentElement !== document.body) document.body.append(modalElement);

    const ui = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
    const modal = ui?.Modal?.getOrCreateInstance(modalElement);
    const property = (record, name) => Object.entries(record ?? {})
      .find(([key]) => key.localeCompare(name, undefined, { sensitivity: "accent" }) === 0)?.[1];
    const fields = {
      id: form.querySelector("[data-guideline-id]"),
      permitType: form.querySelector("[data-guideline-permit-type]"),
      isActive: form.querySelector("[data-guideline-active]")
    };
    const mode = form.querySelector("[data-guideline-mode]");
    const errorBox = form.querySelector("[data-guideline-error]");
    const contentError = form.querySelector("[data-guideline-content-error]");
    const saveButton = form.querySelector("[data-guideline-save]");
    const loadingOverlay = form.querySelector("[data-guideline-loading]");
    let permitTypesPromise;

    const FontStyle = window.Quill.import("attributors/style/font");
    FontStyle.whitelist = ["Arial", "Georgia", "Tahoma", "Verdana", "Times New Roman", "Courier New"];
    window.Quill.register(FontStyle, true);

    const SizeStyle = window.Quill.import("attributors/style/size");
    SizeStyle.whitelist = ["8px", "9px", "10px", "11px", "12px", "14px", "16px", "18px", "20px", "24px", "28px", "32px", "36px", "48px"];
    window.Quill.register(SizeStyle, true);

    const showInvalidImageMessage = () => {
      const message = "Enter a complete http:// or https:// image URL.";
      if (window.apcloudNotifications) window.apcloudNotifications.error(message, "Invalid image URL");
      else window.alert(message);
    };

    const quill = new window.Quill(editorElement, {
      theme: "snow",
      placeholder: "Enter the permit instructions, restrictions, precautions, and other guidelines…",
      formats: [
        "font", "size", "header", "bold", "italic", "underline", "strike",
        "color", "background", "script", "blockquote", "code-block", "list",
        "indent", "direction", "align", "link", "image"
      ],
      modules: {
        history: { delay: 750, maxStack: 100, userOnly: true },
        toolbar: {
          container: [
            [{ font: [false, ...FontStyle.whitelist] }, { size: [false, ...SizeStyle.whitelist] }],
            [{ header: [1, 2, 3, 4, 5, 6, false] }],
            ["bold", "italic", "underline", "strike"],
            [{ color: [] }, { background: [] }],
            [{ script: "sub" }, { script: "super" }],
            ["blockquote", "code-block"],
            [{ list: "ordered" }, { list: "bullet" }, { list: "check" }],
            [{ indent: "-1" }, { indent: "+1" }],
            [{ direction: "rtl" }, { align: [] }],
            ["link", "image"],
            ["undo", "redo", "clean"]
          ],
          handlers: {
            undo() { this.quill.history.undo(); },
            redo() { this.quill.history.redo(); },
            image() {
              const value = window.prompt("Enter the image URL (http:// or https://):");
              if (!value) return;
              let url;
              try { url = new URL(value); } catch { showInvalidImageMessage(); return; }
              if (url.protocol !== "http:" && url.protocol !== "https:") {
                showInvalidImageMessage(); return;
              }
              const range = this.quill.getSelection(true);
              this.quill.insertEmbed(range.index, "image", url.href, "user");
              this.quill.setSelection(range.index + 1, 0, "silent");
            }
          }
        }
      }
    });

    const toolbarElement = quill.getModule("toolbar")?.container;
    const configurePickerLabels = (format, defaultLabel) => {
      const picker = toolbarElement?.querySelector(`.ql-picker.ql-${format}`);
      picker?.querySelector(".ql-picker-label")?.setAttribute("data-label", defaultLabel);
      picker?.querySelectorAll(".ql-picker-item").forEach(item => {
        const value = item.getAttribute("data-value");
        item.setAttribute("data-label", value || defaultLabel);
        if (format === "font" && value) item.style.fontFamily = value;
      });
    };
    configurePickerLabels("font", "Default font");
    configurePickerLabels("size", "Normal size");

    const toolbarTitles = {
      font: "Font family", size: "Font size", header: "Heading", bold: "Bold",
      italic: "Italic", underline: "Underline", strike: "Strikethrough", color: "Text color",
      background: "Background color", blockquote: "Block quote", "code-block": "Code block",
      script: "Subscript or superscript", list: "List", indent: "Indent",
      direction: "Right-to-left text", align: "Alignment", link: "Insert link", image: "Insert image from URL",
      undo: "Undo", redo: "Redo", clean: "Clear formatting"
    };
    Object.entries(toolbarTitles).forEach(([format, title]) => {
      toolbarElement?.querySelectorAll(`.ql-${format}`).forEach(control => {
        control.setAttribute("title", title);
        control.setAttribute("aria-label", title);
      });
    });
    const undoButton = toolbarElement?.querySelector("button.ql-undo");
    const redoButton = toolbarElement?.querySelector("button.ql-redo");
    if (undoButton) undoButton.innerHTML = "&#8630;";
    if (redoButton) redoButton.innerHTML = "&#8631;";

    const setLoading = loading => {
      if (!loadingOverlay) return;
      loadingOverlay.classList.toggle("d-none", !loading);
      const body = loadingOverlay.closest(".modal-body");
      body?.classList.toggle("is-loading", loading);
      if (loading) body?.setAttribute("aria-busy", "true"); else body?.removeAttribute("aria-busy");
    };

    const loadPermitTypes = () => permitTypesPromise ??= window.apcloudApi
      .json("permit-application-guidelines/permit-types")
      .then(items => {
        fields.permitType.replaceChildren(new Option("Select a permit type", ""));
        items.forEach(item => fields.permitType.add(new Option(
          `${property(item, "name")} (${property(item, "code")})`, String(property(item, "id")))));
        return items;
      })
      .catch(error => { permitTypesPromise = null; throw error; });

    const openEditor = async id => {
      form.reset(); form.classList.remove("was-validated"); errorBox.classList.add("d-none");
      contentError.classList.add("d-none"); fields.id.value = id || ""; fields.isActive.checked = true;
      quill.setContents([]); mode.textContent = id ? "Edit guideline" : "Add guideline";
      modal?.show(); setLoading(true);
      try {
        await loadPermitTypes();
        if (id) {
          const record = await window.apcloudApi.json(`permit-application-guidelines/${encodeURIComponent(id)}`);
          fields.permitType.value = String(property(record, "permitTypeListItemId") ?? "");
          if (!fields.permitType.value) {
            fields.permitType.add(new Option(property(record, "permitTypeName") || `Permit type ${property(record, "permitTypeListItemId")}`,
              String(property(record, "permitTypeListItemId"))));
            fields.permitType.value = String(property(record, "permitTypeListItemId"));
          }
          fields.isActive.checked = property(record, "isActive") !== false;
          quill.clipboard.dangerouslyPasteHTML(property(record, "guidelines") || "");
        }
      } catch (error) {
        errorBox.textContent = error.message || "The guideline could not be loaded.";
        errorBox.classList.remove("d-none"); saveButton.disabled = true;
      } finally { setLoading(false); }
    };

    document.querySelectorAll("[data-guideline-add]").forEach(button =>
      button.addEventListener("click", () => openEditor()));

    grid.addEventListener("server-grid:action", async event => {
      const { action, id, record } = event.detail ?? {};
      if (action === "edit" && id) return openEditor(id);
      if (action !== "delete" || !id) return;
      const permitType = property(record, "permitTypeName") || "this permit type";
      if (!window.confirm(`Deactivate the guideline for ${permitType}?`)) return;
      try {
        await window.apcloudApi.json(`permit-application-guidelines/${encodeURIComponent(id)}`, { method: "DELETE" });
        window.apcloudNotifications?.success(`The guideline for ${permitType} was deactivated.`, "Guideline deactivated");
        grid.dispatchEvent(new CustomEvent("server-grid:reload"));
      } catch (error) {
        window.apcloudNotifications?.error(error.message || "The guideline could not be deactivated.", "Unable to deactivate guideline");
      }
    });

    form.addEventListener("submit", async event => {
      event.preventDefault();
      const hasContent = quill.getText().trim().length > 0;
      contentError.classList.toggle("d-none", hasContent);
      editorElement.closest(".ql-container")?.classList.toggle("is-invalid", !hasContent);
      if (!form.checkValidity() || !hasContent) { form.classList.add("was-validated"); return; }
      const id = fields.id.value;
      saveButton.disabled = true;
      form.querySelector("[data-guideline-save-label]").classList.add("d-none");
      form.querySelector("[data-guideline-saving]").classList.remove("d-none");
      try {
        await window.apcloudApi.json(id ? `permit-application-guidelines/${encodeURIComponent(id)}` : "permit-application-guidelines", {
          method: id ? "PUT" : "POST",
          body: {
            permitTypeListItemId: Number(fields.permitType.value),
            guidelines: quill.getSemanticHTML(),
            isActive: fields.isActive.checked
          }
        });
        modal?.hide(); grid.dispatchEvent(new CustomEvent("server-grid:reload"));
        window.apcloudNotifications?.success(id ? "The guideline was updated." : "The guideline was created.", "Guideline saved");
      } catch (error) {
        errorBox.textContent = error.message || "The guideline could not be saved."; errorBox.classList.remove("d-none");
      } finally {
        saveButton.disabled = false;
        form.querySelector("[data-guideline-save-label]").classList.remove("d-none");
        form.querySelector("[data-guideline-saving]").classList.add("d-none");
      }
    });

    quill.on("text-change", () => {
      const hasContent = quill.getText().trim().length > 0;
      contentError.classList.toggle("d-none", hasContent);
      editorElement.closest(".ql-container")?.classList.toggle("is-invalid", !hasContent);
    });
    modalElement.addEventListener("hidden.bs.modal", () => { saveButton.disabled = false; setLoading(false); });
  });
})();
