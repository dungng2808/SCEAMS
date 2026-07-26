(() => {
  const getInitials = (fullName) => {
    const parts = fullName.trim().split(/\s+/).filter(Boolean);

    if (parts.length === 0) {
      return "SC";
    }

    return parts.length === 1
      ? parts[0].slice(0, 1).toUpperCase()
      : `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
  };

  const openDialog = (dialog, fallbackMessage, form) => {
    if (typeof dialog.showModal === "function") {
      dialog.showModal();
    } else if (window.confirm(fallbackMessage)) {
      form.requestSubmit();
    }
  };

  const bindDismiss = (dialog, cancelButton) => {
    cancelButton?.addEventListener("click", () => {
      dialog.close();
    });

    dialog.addEventListener("click", (event) => {
      if (event.target === dialog) {
        dialog.close();
      }
    });
  };

  const setupStatusDialog = () => {
    const dialog = document.querySelector("[data-status-dialog]");
    const form = dialog?.querySelector("[data-status-form]");
    const statusInput = form?.querySelector("[data-status-value]");
    const title = dialog?.querySelector("[data-status-title]");
    const description =
      dialog?.querySelector("[data-status-description]");
    const name = dialog?.querySelector("[data-status-name]");
    const email = dialog?.querySelector("[data-status-email]");
    const avatar = dialog?.querySelector("[data-status-avatar]");
    const confirmButton =
      dialog?.querySelector("[data-status-confirm]");
    const confirmLabel =
      dialog?.querySelector("[data-status-confirm-label]");
    const cancelButton =
      dialog?.querySelector("[data-status-cancel]");
    const actionBase = dialog?.getAttribute("data-action-base");

    if (!dialog || !form || !statusInput || !actionBase) {
      return;
    }

    document.querySelectorAll("[data-status-action]").forEach((button) => {
      button.addEventListener("click", () => {
        const userId = button.getAttribute("data-user-id");
        const fullName =
          button.getAttribute("data-user-name") || "Tài khoản";
        const userEmail =
          button.getAttribute("data-user-email") || "";
        const isCurrentlyActive =
          button.getAttribute("data-current-active") === "true";
        const shouldActivate = !isCurrentlyActive;
        const mode = shouldActivate ? "unlock" : "lock";

        if (!userId) {
          return;
        }

        form.action =
          `${actionBase.replace(/\/$/, "")}/${encodeURIComponent(userId)}/ActiveStatus`;
        statusInput.value = shouldActivate.toString();
        dialog.setAttribute("data-mode", mode);

        if (title) {
          title.textContent = shouldActivate
            ? "Mở khóa tài khoản?"
            : "Khóa tài khoản?";
        }

        if (description) {
          description.textContent = shouldActivate
            ? "Người dùng sẽ có thể đăng nhập và sử dụng hệ thống trở lại."
            : "Người dùng sẽ không thể đăng nhập; refresh token hiện tại cũng bị thu hồi.";
        }

        if (name) {
          name.textContent = fullName;
        }

        if (email) {
          email.textContent = userEmail;
        }

        if (avatar) {
          avatar.textContent = getInitials(fullName);
        }

        if (confirmLabel) {
          confirmLabel.textContent = shouldActivate
            ? "Mở khóa tài khoản"
            : "Khóa tài khoản";
        }

        openDialog(
          dialog,
          description?.textContent || "Xác nhận thao tác?",
          form
        );
      });
    });

    bindDismiss(dialog, cancelButton);

    form.addEventListener("submit", () => {
      if (confirmButton) {
        confirmButton.disabled = true;
        confirmButton.setAttribute("aria-busy", "true");
      }

      if (confirmLabel) {
        confirmLabel.textContent =
          statusInput.value === "true"
            ? "Đang mở khóa..."
            : "Đang khóa...";
      }
    });
  };

  const setupRoleDialog = () => {
    const dialog = document.querySelector("[data-role-dialog]");
    const form = dialog?.querySelector("[data-role-form]");
    const select = dialog?.querySelector("[data-role-select]");
    const name = dialog?.querySelector("[data-role-name]");
    const email = dialog?.querySelector("[data-role-email]");
    const avatar = dialog?.querySelector("[data-role-avatar]");
    const currentLabel =
      dialog?.querySelector("[data-role-current-label]");
    const confirmButton =
      dialog?.querySelector("[data-role-confirm]");
    const confirmLabel =
      dialog?.querySelector("[data-role-confirm-label]");
    const cancelButton =
      dialog?.querySelector("[data-role-cancel]");
    const actionBase = dialog?.getAttribute("data-action-base");
    let currentRole = "";

    if (!dialog || !form || !select || !actionBase) {
      return;
    }

    const updateConfirmationState = () => {
      const selectedOption = select.options[select.selectedIndex];
      const hasChanged =
        Boolean(select.value) && select.value !== currentRole;

      if (confirmButton) {
        confirmButton.disabled = !hasChanged;
      }

      if (confirmLabel) {
        confirmLabel.textContent = hasChanged
          ? `Đổi thành ${selectedOption.textContent.trim()}`
          : "Chọn vai trò mới";
      }
    };

    document.querySelectorAll("[data-role-action]").forEach((button) => {
      button.addEventListener("click", () => {
        const userId = button.getAttribute("data-user-id");
        const fullName =
          button.getAttribute("data-user-name") || "Tài khoản";
        const userEmail =
          button.getAttribute("data-user-email") || "";

        currentRole =
          button.getAttribute("data-current-role") || "";

        if (!userId) {
          return;
        }

        form.action =
          `${actionBase.replace(/\/$/, "")}/${encodeURIComponent(userId)}/Role`;
        select.value = "";

        Array.from(select.options).forEach((option) => {
          option.disabled =
            Boolean(option.value) && option.value === currentRole;
        });

        if (name) {
          name.textContent = fullName;
        }

        if (email) {
          email.textContent = userEmail;
        }

        if (avatar) {
          avatar.textContent = getInitials(fullName);
        }

        if (currentLabel) {
          currentLabel.textContent =
            button.getAttribute("data-current-role-label") ||
            currentRole;
        }

        updateConfirmationState();

        if (typeof dialog.showModal === "function") {
          dialog.showModal();
        } else {
          const requestedRole = window.prompt(
            "Nhập vai trò mới: Admin, Staff, Organizer hoặc Student."
          );
          const matchingOption = Array.from(select.options).find(
            (option) =>
              option.value &&
              option.value.toLowerCase() ===
                requestedRole?.trim().toLowerCase()
          );

          if (matchingOption && matchingOption.value !== currentRole) {
            select.value = matchingOption.value;
            form.requestSubmit();
          }
        }
      });
    });

    select.addEventListener("change", updateConfirmationState);
    bindDismiss(dialog, cancelButton);

    form.addEventListener("submit", () => {
      if (confirmButton) {
        confirmButton.disabled = true;
        confirmButton.setAttribute("aria-busy", "true");
      }

      if (confirmLabel) {
        confirmLabel.textContent = "Đang cập nhật...";
      }
    });
  };

  setupStatusDialog();
  setupRoleDialog();
})();
