using FasDisasm.Core.Disassembly;

namespace FasDisasm.UI;

public partial class FunctionsForm : Form
{
    public FunctionsForm(IEnumerable<FasFunction> functions)
    {
        InitializeComponent();

        Text = "Functions";
        Size = new Size(500, 400);
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

        listView.Columns.Add("Name", 200);
        listView.Columns.Add("Offset", 80);
        listView.Columns.Add("Args", 50);
        listView.Columns.Add("Locals", 50);

        foreach (var func in functions.OrderBy(f => f.StartOffset))
        {
            var item = new ListViewItem(func.Name);
            item.SubItems.Add($"${func.StartOffset:X4}");
            item.SubItems.Add(func.ArgumentCount.ToString());
            item.SubItems.Add(func.LocalVariableCount.ToString());
            listView.Items.Add(item);
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
