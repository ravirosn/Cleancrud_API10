document.addEventListener("DOMContentLoaded", () => {
  // Tabler 1.4 publishes Bootstrap under window.tabler.bootstrap. Keep the
  // standard namespace fallback for pages that load Bootstrap directly.
  const bootstrapUi = window.bootstrap ?? window.tabler?.bootstrap ?? window.tabler;
  const notificationContainer = document.createElement("div");
  notificationContainer.className = "toast-container apcloud-notification-container position-fixed top-0 end-0 p-3";
  notificationContainer.setAttribute("aria-live", "polite");
  notificationContainer.setAttribute("aria-atomic", "true");
  document.body.append(notificationContainer);

  const showNotification = (type, message, title) => {
    const kind = type === "success" ? "success" : "danger";
    const toast = document.createElement("div");
    toast.className = `toast apcloud-toast apcloud-toast-${kind}`;
    toast.setAttribute("role", "alert");
    toast.setAttribute("aria-live", "assertive");
    toast.setAttribute("aria-atomic", "true");

    const content = document.createElement("div");
    content.className = "toast-body d-flex align-items-start gap-3";

    const icon = document.createElement("span");
    icon.className = "apcloud-toast-icon";
    icon.setAttribute("aria-hidden", "true");
    icon.textContent = kind === "success" ? "✓" : "!";

    const copy = document.createElement("div");
    copy.className = "flex-fill";
    const heading = document.createElement("strong");
    heading.className = "d-block mb-1";
    heading.textContent = title || (kind === "success" ? "Success" : "Unable to complete request");
    const text = document.createElement("div");
    text.className = "text-secondary";
    text.textContent = message;
    copy.append(heading, text);

    const close = document.createElement("button");
    close.type = "button";
    close.className = "btn-close";
    close.dataset.bsDismiss = "toast";
    close.setAttribute("aria-label", "Close notification");

    content.append(icon, copy, close);
    toast.append(content);
    notificationContainer.append(toast);
    toast.addEventListener("hidden.bs.toast", () => toast.remove(), { once: true });
    if (bootstrapUi?.Toast) {
      bootstrapUi.Toast.getOrCreateInstance(toast, { autohide: true, delay: 4500 }).show();
    } else {
      toast.classList.add("show");
      window.setTimeout(() => toast.remove(), 4500);
    }
  };

  window.apcloudNotifications = Object.freeze({
    success: (message, title) => showNotification("success", message, title),
    error: (message, title) => showNotification("danger", message, title)
  });

  // The profile component is rendered inside the sticky topbar. A positioned
  // ancestor with a z-index creates a stacking context below Bootstrap's
  // document-level backdrop, so move the modal to the body before it opens.
  document.querySelectorAll("[data-profile-modal]").forEach((modal) => {
    if (modal.parentElement !== document.body) document.body.append(modal);
  });

  document.querySelectorAll("[data-login-form]").forEach((form) => {
    form.addEventListener("submit", (event) => {
      if (form.dataset.submitting === "true") {
        event.preventDefault();
        return;
      }

      // Unobtrusive validation cancels invalid submissions. Check it before
      // disabling the button so users can correct validation errors and retry.
      const jqueryForm = window.jQuery ? window.jQuery(form) : null;
      if (jqueryForm?.data("validator") && !jqueryForm.valid()) return;

      const button = form.querySelector("[data-login-submit]");
      if (!button) return;

      form.dataset.submitting = "true";
      form.setAttribute("aria-busy", "true");
      button.disabled = true;
      button.setAttribute("aria-disabled", "true");
      button.querySelector("[data-login-label]")?.classList.add("d-none");
      button.querySelector("[data-login-progress]")?.classList.remove("d-none");

      const status = form.querySelector("[data-login-status]");
      if (status) status.textContent = "Signing in. Please wait.";
    });
  });

  document.querySelectorAll("[data-password-toggle]").forEach((button) => {
    button.addEventListener("click", () => {
      const input = button.parentElement?.querySelector("input");
      if (!input) return;
      const reveal = input.type === "password";
      input.type = reveal ? "text" : "password";
      button.setAttribute("aria-label", reveal ? "Hide password" : "Show password");
      button.classList.toggle("is-visible", reveal);
    });
  });

  document.querySelectorAll("[data-profile-form]").forEach((form) => {
    const errorBox = form.querySelector("[data-profile-error]");
    const successBox = form.querySelector("[data-profile-success]");
    const saveButton = form.querySelector("[data-profile-save]");

    const showError = (message) => {
      if (!errorBox) return;
      errorBox.textContent = message;
      errorBox.classList.remove("d-none");
      successBox?.classList.add("d-none");
    };

    const unwrapReturnMessage = (response) => {
      if (!response || typeof response !== "object") {
        throw new Error("The server returned an empty response.");
      }

      if (!Object.hasOwn(response, "isSuccess")) {
        return { data: response, message: "" };
      }

      if (!response.isSuccess) {
        throw new Error(response.returnMessage || "The request could not be completed.");
      }

      return { data: response.data ?? null, message: response.returnMessage || "" };
    };

    const bffPictureUrl = (value) => {
      if (!value) return null;
      return value.toLowerCase().startsWith("/api/") ? `/bff/${value.slice(5)}` : value;
    };

    const initialsFor = (displayName) => displayName
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part.charAt(0).toUpperCase())
      .join("") || "U";

    const updateProfileHeader = (displayName, contactNumber, pictureUrl) => {
      document.querySelectorAll("[data-profile-display-name]").forEach((element) => {
        element.textContent = displayName;
      });

      document.querySelectorAll("[data-profile-contact-row]").forEach((row) => {
        row.classList.toggle("d-none", !contactNumber);
        const value = row.querySelector("[data-profile-contact-number]");
        if (value) value.textContent = contactNumber || "";
      });

      const initials = initialsFor(displayName);
      document.querySelectorAll("[data-profile-avatar]").forEach((avatar) => {
        if (pictureUrl) {
          let image = avatar.querySelector("[data-profile-image]");
          if (!image) {
            image = document.createElement("img");
            image.dataset.profileImage = "";
            image.alt = "";
            avatar.replaceChildren(image);
          }
          image.src = pictureUrl;
        } else {
          const initialsElement = avatar.querySelector("[data-profile-initials]");
          if (initialsElement) initialsElement.textContent = initials;
        }
      });
    };

    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      errorBox?.classList.add("d-none");
      successBox?.classList.add("d-none");

      if (!form.reportValidity()) return;
      const values = new FormData(form);
      const displayName = String(values.get("displayName") || "").trim();
      const contactNumber = String(values.get("contactNumber") || "").trim();
      const currentPassword = String(values.get("currentPassword") || "");
      const newPassword = String(values.get("newPassword") || "");
      const confirmPassword = String(values.get("confirmPassword") || "");
      const picture = values.get("picture");
      const changingPassword = Boolean(currentPassword || newPassword || confirmPassword);

      if (changingPassword && (!currentPassword || !newPassword || !confirmPassword)) {
        showError("Complete all three password fields to change your password.");
        return;
      }
      if (changingPassword && newPassword !== confirmPassword) {
        showError("The new password and confirmation do not match.");
        return;
      }
      if (picture instanceof File && picture.size > 0) {
        const imageTypes = new Set(["image/jpeg", "image/png", "image/webp"]);
        if (!imageTypes.has(picture.type)) {
          showError("Select a JPEG, PNG, or WebP profile picture.");
          return;
        }
        if (picture.size > 10 * 1024 * 1024) {
          showError("The selected profile picture must not exceed 10 MB.");
          return;
        }
      }

      saveButton.disabled = true;
      form.setAttribute("aria-busy", "true");
      try {
        const messages = [];
        if (changingPassword) {
          const passwordResponse = await window.apcloudApi.json("user-profile/password", {
            method: "PUT",
            body: { currentPassword, newPassword, confirmPassword }
          });
          const passwordResult = unwrapReturnMessage(passwordResponse);
          if (passwordResult.message) messages.push(passwordResult.message);
        }

        const profileResponse = await window.apcloudApi.json("user-profile", {
          method: "PUT",
          body: { displayName, contactNumber: contactNumber || null }
        });
        const profileResult = unwrapReturnMessage(profileResponse);
        const profile = profileResult.data;
        if (!profile) throw new Error("The server returned an empty profile response.");
        if (profileResult.message) messages.push(profileResult.message);

        let pictureUrl = bffPictureUrl(profile.profilePictureUrl);
        if (picture instanceof File && picture.size > 0) {
          const upload = new FormData();
          upload.append("picture", picture);
          const photoResponse = await window.apcloudApi.json("user-profile/photo", {
            method: "POST",
            body: upload
          });
          const photoResult = unwrapReturnMessage(photoResponse);
          if (!photoResult.data) throw new Error("The server returned an empty picture response.");
          if (photoResult.message) messages.push(photoResult.message);
          pictureUrl = bffPictureUrl(photoResult.data.profilePictureUrl);
        }

        updateProfileHeader(profile.displayName, profile.contactNumber, pictureUrl);
        form.querySelectorAll('input[type="password"]').forEach((input) => { input.value = ""; });
        const pictureInput = form.querySelector("[data-profile-picture]");
        if (pictureInput) pictureInput.value = "";
        successBox?.classList.add("d-none");
        const modal = form.closest(".modal");
        if (modal && bootstrapUi?.Modal) {
          bootstrapUi.Modal.getOrCreateInstance(modal).hide();
        }
        window.apcloudNotifications.success(
          messages.join(" ") || "Your profile was updated successfully.",
          "Profile updated"
        );
      } catch (error) {
        const message = error.message || "The profile could not be updated.";
        showError(message);
        window.apcloudNotifications.error(message, "Profile update failed");
      } finally {
        saveButton.disabled = false;
        form.removeAttribute("aria-busy");
      }
    });
  });

  const defaults = { mode: "system", color: "blue", radius: "6" };
  const systemTheme = window.matchMedia("(prefers-color-scheme: dark)");

  const loadSettings = () => {
    try {
      return { ...defaults, ...JSON.parse(localStorage.getItem("permit-management-settings") || "{}") };
    } catch {
      return { ...defaults };
    }
  };

  let settings = loadSettings();
  let themeSaveTimer;

  const persistThemeSettings = () => {
    themeSaveTimer = undefined;
    if (!window.apcloudApi) return;

    window.apcloudApi.json("user-theme-settings", {
      method: "PUT",
      keepalive: true,
      body: {
        mode: settings.mode,
        color: settings.color,
        radius: Number(settings.radius)
      }
    }).catch((error) => {
      console.error("Theme settings could not be saved.", error);
    });
  };

  const queueThemeSettingsSave = () => {
    if (themeSaveTimer) window.clearTimeout(themeSaveTimer);
    themeSaveTimer = window.setTimeout(persistThemeSettings, 200);
  };

  const resolvedMode = () => settings.mode === "system" ? (systemTheme.matches ? "dark" : "light") : settings.mode;

  const syncSettingsControls = () => {
    Object.entries({ "theme-mode": settings.mode, "theme-color": settings.color, "theme-radius": settings.radius }).forEach(([name, value]) => {
      const input = document.querySelector(`input[name="${name}"][value="${value}"]`);
      if (input) input.checked = true;
    });
  };

  const applySettings = ({ save = true, persist = false } = {}) => {
    const mode = resolvedMode();
    document.documentElement.setAttribute("data-bs-theme", mode);
    document.documentElement.dataset.themeMode = settings.mode;
    document.documentElement.dataset.themeColor = settings.color;
    document.documentElement.dataset.themeRadius = settings.radius;
    document.body.dataset.bsTheme = mode;
    document.body.dataset.themeColor = settings.color;
    document.body.dataset.themeRadius = settings.radius;

    const themeColor = getComputedStyle(document.documentElement).getPropertyValue("--tblr-primary").trim();
    document.querySelector('meta[name="theme-color"]')?.setAttribute("content", themeColor || "#206bc4");

    if (save) {
      localStorage.setItem("permit-management-settings", JSON.stringify(settings));
      localStorage.setItem("permit-management-theme", mode);
    }
    if (persist) queueThemeSettingsSave();
    syncSettingsControls();
  };

  applySettings({ save: false });

  document.querySelectorAll(".theme-toggle").forEach((toggle) => {
    toggle.addEventListener("click", (event) => {
      event.preventDefault();
      settings.mode = resolvedMode() === "dark" ? "light" : "dark";
      applySettings({ persist: true });
    });
  });

  document.querySelectorAll('input[name="theme-mode"]').forEach((input) => {
    input.addEventListener("change", () => {
      settings.mode = input.value;
      applySettings({ persist: true });
    });
  });

  document.querySelectorAll('input[name="theme-color"]').forEach((input) => {
    input.addEventListener("change", () => {
      settings.color = input.value;
      applySettings({ persist: true });
    });
  });

  document.querySelectorAll('input[name="theme-radius"]').forEach((input) => {
    input.addEventListener("change", () => {
      settings.radius = input.value;
      applySettings({ persist: true });
    });
  });

  document.getElementById("resetThemeSettings")?.addEventListener("click", () => {
    settings = { ...defaults };
    applySettings({ persist: true });
  });

  window.addEventListener("pagehide", () => {
    if (!themeSaveTimer) return;
    window.clearTimeout(themeSaveTimer);
    persistThemeSettings();
  });

  systemTheme.addEventListener("change", () => {
    if (settings.mode === "system") applySettings({ save: false });
  });

  document.querySelectorAll("[data-period]").forEach((button) => {
    button.addEventListener("click", () => {
      button.closest(".period-switcher")?.querySelectorAll(".btn").forEach((item) => item.classList.remove("active"));
      button.classList.add("active");
    });
  });

  const search = document.querySelector(".topbar-search input");
  document.addEventListener("keydown", (event) => {
    if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
      event.preventDefault();
      search?.focus();
    }
  });
});
