// Auto-dismiss toasts after 4s
document.querySelectorAll('.toast').forEach(function(t) {
    setTimeout(function() { t.remove(); }, 4000);
});
