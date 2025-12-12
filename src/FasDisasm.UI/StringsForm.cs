using FasDisasm.Core.Types;

namespace FasDisasm.UI;

public partial class StringsForm : Form
{
    public StringsForm(object?[][] moduleVars)
    {
        InitializeComponent();

        Text = "Module Variables";
        Size = new Size(600, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.SizableToolWindow;

        var listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Consolas", 9)
        };

        listView.Columns.Add("#", 50);
        listView.Columns.Add("Module", 60);
        listView.Columns.Add("Value", 300);
        listView.Columns.Add("Type", 100);

        for (int module = 0; module < moduleVars.Length; module++)
        {
            var vars = moduleVars[module];
            if (vars == null) continue;

            for (int i = 0; i < vars.Length; i++)
            {
                var value = vars[i];
                var valueStr = value switch
                {
                    null => "nil",
                    IFasType fasType => fasType.ToLispString(),
                    _ => value.ToString() ?? "nil"
                };

                var typeStr = value?.GetType().Name ?? "nil";

                var item = new ListViewItem(i.ToString());
                item.SubItems.Add(module.ToString());
                item.SubItems.Add(valueStr);
                item.SubItems.Add(typeStr);

                // Color code by type
                item.ForeColor = value switch
                {
                    FasString => Color.Brown,
                    FasSymbol => Color.DarkBlue,
                    FasInt or FasReal => Color.DarkGreen,
                    FasList => Color.Purple,
                    FasUsubr => Color.DarkRed,
                    _ => Color.Black
                };

                listView.Items.Add(item);
            }
        }

        Controls.Add(listView);
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Font;
        ResumeLayout(false);
    }
}
