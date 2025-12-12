using System.Reflection;

namespace FasDisasm.UI.Extensions;

/// <summary>
/// Extension methods for DataGridView.
/// </summary>
public static class DataGridViewExtensions
{
    /// <summary>
    /// Enables double buffering on a DataGridView for smoother rendering.
    /// </summary>
    public static void SetDoubleBuffered(this DataGridView dgv, bool enabled = true)
    {
        var dgvType = dgv.GetType();
        var pi = dgvType.GetProperty("DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);
        pi?.SetValue(dgv, enabled, null);
    }
}

/// <summary>
/// Extension method attribute for DataGridView.
/// </summary>
public static class ControlExtensions
{
    /// <summary>
    /// Sets the DoubleBuffered property via reflection.
    /// </summary>
    public static void DoubleBuffered(this Control control, bool enabled)
    {
        var prop = control.GetType().GetProperty("DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);
        prop?.SetValue(control, enabled);
    }
}
