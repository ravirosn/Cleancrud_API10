(() => {
  const unsafeMethods = new Set(["POST", "PUT", "PATCH", "DELETE"]);

  const request = async (path, options = {}) => {
    if (typeof path !== "string" || !path.trim() || /^https?:/i.test(path)) {
      throw new TypeError("API path must be a non-empty relative path.");
    }

    const method = (options.method || "GET").toUpperCase();
    const headers = new Headers(options.headers || {});

    if (unsafeMethods.has(method)) {
      const csrfToken = document.querySelector('meta[name="csrf-token"]')?.content;
      if (!csrfToken) throw new Error("The antiforgery token is unavailable.");
      headers.set("X-CSRF-TOKEN", csrfToken);
    }

    const response = await fetch(`/bff/${path.replace(/^\/+/, "")}`, {
      ...options,
      method,
      headers,
      credentials: "same-origin"
    });

    if (response.status === 401) {
      const returnUrl = `${window.location.pathname}${window.location.search}`;
      window.location.assign(`/Authentication/Account/Login?returnUrl=${encodeURIComponent(returnUrl)}`);
      throw new Error("The session has expired.");
    }

    return response;
  };

  const json = async (path, options = {}) => {
    const headers = new Headers(options.headers || {});
    let body = options.body;

    if (body !== undefined && !(body instanceof FormData) && typeof body !== "string") {
      headers.set("Content-Type", "application/json");
      body = JSON.stringify(body);
    }

    const response = await request(path, { ...options, headers, body });
    if (!response.ok) {
      const problem = await response.json().catch(() => null);
      const validationMessage = problem?.errors && typeof problem.errors === "object"
        ? Object.values(problem.errors).flat().find((message) => typeof message === "string" && message.trim())
        : null;
      throw new Error(
        problem?.returnMessage ||
        problem?.Message ||
        problem?.detail ||
        validationMessage ||
        problem?.title ||
        problem?.message ||
        `API request failed with status ${response.status}.`
      );
    }

    const result = response.status === 204 ? null : await response.json();
    if (result && Object.hasOwn(result, "isSuccess") && result.isSuccess === false) {
      throw new Error(result.returnMessage || "The API could not complete the request.");
    }

    return result;
  };

  window.apcloudApi = Object.freeze({ request, json });
})();
