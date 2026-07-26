(() => {
  const setupDeleteDialog = () => {
    const dialog = document.querySelector("[data-delete-category-dialog]");
    const form = dialog?.querySelector("[data-delete-category-form]");
    const nameLabel = dialog?.querySelector("[data-delete-category-name]");
    const cancelButton = dialog?.querySelector("[data-delete-category-cancel]");
    const actionBase = dialog?.getAttribute("data-action-base");

    if (!dialog || !form || !actionBase) {
      return;
    }

    cancelButton?.addEventListener("click", () => {
      dialog.close();
    });

    dialog.addEventListener("click", (event) => {
      if (event.target === dialog) {
        dialog.close();
      }
    });

    document.querySelectorAll("[data-category-delete]").forEach((button) => {
      button.addEventListener("click", () => {
        const categoryId = button.getAttribute("data-category-id");
        const categoryName = button.getAttribute("data-category-name") || "Danh mục";

        if (!categoryId) {
          return;
        }

        form.action = `${actionBase.replace(/\/$/, "")}/${encodeURIComponent(categoryId)}/Delete`;

        if (nameLabel) {
          nameLabel.textContent = categoryName;
        }

        if (typeof dialog.showModal === "function") {
          dialog.showModal();
        } else if (window.confirm(`Bạn có chắc chắn muốn xóa danh mục "${categoryName}" không?`)) {
          form.requestSubmit();
        }
      });
    });
  };

  document.addEventListener("DOMContentLoaded", () => {
    setupDeleteDialog();
  });
})();
