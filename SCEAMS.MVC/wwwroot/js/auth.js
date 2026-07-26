(() => {
  document.querySelectorAll("[data-password-toggle]").forEach((toggle) => {
    const targetId = toggle.getAttribute("data-password-target");
    const password = targetId ? document.getElementById(targetId) : null;

    if (!password) {
      return;
    }

    toggle.addEventListener("click", () => {
      const shouldShow = password.type === "password";

      password.type = shouldShow ? "text" : "password";
      toggle.setAttribute("aria-pressed", shouldShow.toString());
      toggle.setAttribute(
        "aria-label",
        shouldShow ? "Ẩn mật khẩu" : "Hiện mật khẩu");
    });
  });

  document.querySelectorAll("[data-auth-form]").forEach((form) => {
    form.addEventListener("submit", () => {
      const jquery = window.jQuery;

      if (jquery?.validator && !jquery(form).valid()) {
        return;
      }

      const submitButton = form.querySelector("[data-submit-button]");
      const submitLabel = submitButton?.querySelector("[data-submit-label]");
      const loadingLabel = form.getAttribute("data-loading-label");

      if (submitButton) {
        submitButton.disabled = true;
        submitButton.setAttribute("aria-busy", "true");
      }

      if (submitLabel && loadingLabel) {
        submitLabel.textContent = loadingLabel;
      }
    });
  });

  document.querySelector(".input-validation-error")?.focus();
})();
