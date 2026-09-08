(() => {
  "use strict";

  const property = (record, name) => Object.entries(record || {})
    .find(([key]) => key.toLowerCase() === name.toLowerCase())?.[1];

  document.addEventListener("DOMContentLoaded", async () => {
    const scope = document.querySelector("[data-permission-scope]");
    if (!scope || !window.apcloudApi) return;

    const guarded = [...document.querySelectorAll("[data-requires-permission]")];
    guarded.forEach((element) => element.classList.add("d-none"));

    const grid = document.querySelector("[data-server-grid][data-permission-actions]");

    try {
      const query = new URLSearchParams({
        moduleCode: scope.dataset.permissionModule,
        menuController: scope.dataset.permissionController,
        menuAction: scope.dataset.permissionAction
      });
      const rows = await window.apcloudApi.json(`permissions/me?${query}`);
      const permissions = new Set(rows.map((row) => property(row, "code")));

      guarded.forEach((element) => {
        const required = element.dataset.requiresPermission
          .split(",").map((code) => code.trim()).filter(Boolean);
        element.classList.toggle("d-none", !required.some((code) => permissions.has(code)));
      });

      if (grid) {
        const actions = grid.dataset.permissionActions.split(",")
          .map((entry) => entry.split(":", 2).map((value) => value.trim()))
          .filter(([action, code]) => action && permissions.has(code))
          .map(([action]) => action);
        grid.dispatchEvent(new CustomEvent("server-grid:set-actions", {
          detail: { actions, reload: false }
        }));
      }
    } catch {
      // Fail closed. API policies remain the final authorization boundary.
    }
  });
})();
