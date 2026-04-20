using System.ComponentModel;
using FasDisasm.Core.Disassembly;
using FasDisasm.Core.IO;

namespace FasDisasm.UI;

public partial class MainForm : Form
{
    private FasFileReader? _fileReader;
    private FasDisassembler? _disassembler;
    private string? _pendingFile;
    private readonly BindingList<CommandListItem> _commands = new();

    public MainForm()
    {
        InitializeComponent();
        SetupUI();
    }

    private void SetupUI()
    {
        // Set form properties
        Text = "FAS Disassembler v2.0";
        Size = new Size(1200, 800);
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;

        // Create menu strip
        var menuStrip = new MenuStrip { Dock = DockStyle.Top };

        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Open...", null, OnOpenFile, Keys.Control | Keys.O));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Export Decompiled...", null, OnExportDecompiled));
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Export &Disassembly...", null, OnExportDisassembly));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (s, e) => Close(), Keys.Alt | Keys.F4));

        var viewMenu = new ToolStripMenuItem("&View");
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("&Functions", null, OnShowFunctions, Keys.F2));
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("&Strings", null, OnShowStrings, Keys.F3));
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("&Log", null, OnShowLog, Keys.F4));

        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add(new ToolStripMenuItem("&About", null, OnShowAbout));

        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add(viewMenu);
        menuStrip.Items.Add(helpMenu);

        // Create toolbar
        var toolStrip = new ToolStrip { Dock = DockStyle.Top };
        toolStrip.Items.Add(new ToolStripButton("Open", null, OnOpenFile) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(new ToolStripButton("Functions", null, OnShowFunctions));
        toolStrip.Items.Add(new ToolStripButton("Strings", null, OnShowStrings));

        // Create status bar
        var statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        var statusLabel = new ToolStripStatusLabel("Ready") { Name = "statusLabel", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        var progressBar = new ToolStripProgressBar { Name = "progressBar", Visible = false };
        statusStrip.Items.Add(statusLabel);
        statusStrip.Items.Add(progressBar);

        // Create split container
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 400,
            Panel1MinSize = 200,
            Panel2MinSize = 100
        };

        // Create data grid for commands
        var dataGrid = new DataGridView
        {
            Name = "dataGrid",
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9)
        };

        // Enable double buffering via reflection (DoubleBuffered is protected)
        typeof(DataGridView).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, dataGrid, new object[] { true });

        // Add columns with proper DataPropertyName binding
        dataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Offset", HeaderText = "Offset", DataPropertyName = "Offset", Width = 70 });
        dataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Opcode", HeaderText = "Cmd", DataPropertyName = "Opcode", Width = 40 });
        dataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Params", HeaderText = "Parameters", DataPropertyName = "Params", Width = 100 });
        dataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Stack", HeaderText = "SP", DataPropertyName = "Stack", Width = 40 });
        dataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Disasm", HeaderText = "Disassembly", DataPropertyName = "Disasm", Width = 300, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dataGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Decompiled", HeaderText = "Decompiled", DataPropertyName = "Decompiled", Width = 400, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

        dataGrid.DataSource = _commands;

        // Add row coloring based on opcode
        dataGrid.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _commands.Count) return;

            var item = _commands[e.RowIndex];
            var color = item.GetColor();

            if (color != Color.Black)
            {
                e.CellStyle.ForeColor = color;
            }
        };

        // Create decompiled output text box
        var decompiledTextBox = new TextBox
        {
            Name = "decompiledTextBox",
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 10),
            WordWrap = false,
            ReadOnly = true,
            BackColor = Color.White
        };

        // Add controls
        splitContainer.Panel1.Controls.Add(dataGrid);
        splitContainer.Panel2.Controls.Add(decompiledTextBox);

        Controls.Add(splitContainer);
        Controls.Add(toolStrip);
        Controls.Add(menuStrip);
        Controls.Add(statusStrip);

        MainMenuStrip = menuStrip;

        // Setup drag and drop
        DragEnter += (s, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };

        DragDrop += (s, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                LoadFile(files[0]);
            }
        };
    }

    public void LoadFileOnStart(string fileName)
    {
        _pendingFile = fileName;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (_pendingFile != null)
        {
            LoadFile(_pendingFile);
            _pendingFile = null;
        }
    }

    private void OnOpenFile(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open FAS/FSL File",
            Filter = "FAS Files (*.fas;*.fsl;*.vlx)|*.fas;*.fsl;*.vlx|All Files (*.*)|*.*",
            FilterIndex = 1
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            LoadFile(dialog.FileName);
        }
    }

    private async void LoadFile(string fileName)
    {
        // Find status strip items (not Controls, they're ToolStripItems)
        var statusStrip = Controls.OfType<StatusStrip>().FirstOrDefault();
        var statusLabel = statusStrip?.Items["statusLabel"] as ToolStripStatusLabel;
        var progressBar = statusStrip?.Items["progressBar"] as ToolStripProgressBar;
        var decompiledTextBox = Controls.Find("decompiledTextBox", true).FirstOrDefault() as TextBox;

        if (statusLabel == null || progressBar == null || decompiledTextBox == null) return;

        try
        {
            _commands.Clear();
            decompiledTextBox.Clear();

            statusLabel.Text = $"Loading {Path.GetFileName(fileName)}...";
            progressBar.Visible = true;
            progressBar.Value = 0;
            Cursor = Cursors.WaitCursor;

            // Check if VLX
            if (VlxSplitter.IsVlxFile(fileName))
            {
                MessageBox.Show(
                    "VLX files contain multiple FAS files.\nPlease extract them first using the VLX Splitter feature.",
                    "VLX File Detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            await Task.Run(() =>
            {
                _fileReader = new FasFileReader();
                _fileReader.LoadProgress += (s, e) =>
                {
                    BeginInvoke(() => progressBar.Value = (int)e.Percentage);
                };
                _fileReader.Load(fileName);

                _disassembler = new FasDisassembler();
                _disassembler.Progress += (s, e) =>
                {
                    BeginInvoke(() => progressBar.Value = (int)e.Percentage);
                };
                _disassembler.CommandDisassembled += (s, cmd) =>
                {
                    BeginInvoke(() =>
                    {
                        _commands.Add(new CommandListItem(cmd));
                    });
                };

                _disassembler.Disassemble(_fileReader);
            });

            // Update decompiled view
            decompiledTextBox.Text = string.Join(Environment.NewLine,
                _disassembler?.DecompiledLines ?? Enumerable.Empty<string>());

            // Automatically export decompiled and disassembly files
            AutoExportFiles(fileName);

            Text = $"FAS Disassembler - {Path.GetFileName(fileName)}";
            statusLabel.Text = $"Loaded: {_commands.Count} commands from {Path.GetFileName(fileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error loading file:\n\n{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            statusLabel.Text = "Error loading file";
        }
        finally
        {
            progressBar.Visible = false;
            Cursor = Cursors.Default;
        }
    }

    private void OnExportDecompiled(object? sender, EventArgs e)
    {
        var decompiledTextBox = Controls.Find("decompiledTextBox", true).FirstOrDefault() as TextBox;
        if (decompiledTextBox == null || string.IsNullOrEmpty(decompiledTextBox.Text))
        {
            MessageBox.Show("No decompiled code to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Export Decompiled Code",
            Filter = "Lisp Files (*.lsp)|*.lsp|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = "lsp"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, decompiledTextBox.Text);
            MessageBox.Show($"Exported to {dialog.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OnExportDisassembly(object? sender, EventArgs e)
    {
        if (_commands.Count == 0)
        {
            MessageBox.Show("No disassembly to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Export Disassembly",
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = "txt"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var lines = _commands.Select(c =>
                $"{c.Offset}\t{c.Opcode}\t{c.Params,-12}\t{c.Disasm}\t{c.Decompiled}");

            File.WriteAllLines(dialog.FileName, lines);
            MessageBox.Show($"Exported to {dialog.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void AutoExportFiles(string sourceFileName)
    {
        try
        {
            var baseName = Path.Combine(
                Path.GetDirectoryName(sourceFileName) ?? ".",
                Path.GetFileNameWithoutExtension(sourceFileName));

            // Export decompiled code (.lsp)
            if (_disassembler?.DecompiledLines.Count > 0)
            {
                var lspPath = baseName + "_.lsp";
                File.WriteAllLines(lspPath, _disassembler.DecompiledLines);
            }

            // Export disassembly (.disasm.txt)
            if (_commands.Count > 0)
            {
                var disasmPath = baseName + ".disasm.txt";
                var lines = _commands.Select(c =>
                    $"{c.Offset}\t{c.Opcode}\t{c.Params,-12}\t{c.Stack}\t{c.Disasm}\t{c.Decompiled}");
                File.WriteAllLines(disasmPath, lines);
            }
        }
        catch (Exception ex)
        {
            // Silently ignore auto-export errors - don't disrupt the user experience
            Console.WriteLine($"Auto-export error: {ex.Message}");
        }
    }

    private void OnShowFunctions(object? sender, EventArgs e)
    {
        if (_disassembler == null || _disassembler.Functions.Count == 0)
        {
            MessageBox.Show("No functions found.", "Functions", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new FunctionsForm(_disassembler.Functions.Values);
        dialog.ShowDialog(this);
    }

    private void OnShowStrings(object? sender, EventArgs e)
    {
        if (_fileReader == null)
        {
            MessageBox.Show("No file loaded.", "Strings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new StringsForm(_fileReader.ModuleVars);
        dialog.ShowDialog(this);
    }

    private void OnShowLog(object? sender, EventArgs e)
    {
        // TODO: Implement log window
    }

    private void OnShowAbout(object? sender, EventArgs e)
    {
        MessageBox.Show(
            "FAS Disassembler v2.0\n\n" +
            "A disassembler for Visual Lisp FAS/FSL files.\n\n" +
            "Originally written in VB6, converted to modern C#.\n\n" +
            "Supports FAS2, FAS3, FAS4, and FSL file formats.",
            "About FAS Disassembler",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}

/// <summary>
/// Wrapper class for displaying commands in the grid.
/// </summary>
public class CommandListItem
{
    private readonly FasCommand _command;

    public CommandListItem(FasCommand command)
    {
        _command = command;
    }

    public string Offset => _command.OffsetHex;
    public string Opcode => _command.OpcodeHex;
    public string Params => _command.ParametersHex;
    public string Stack => _command.StackPointerAfter.ToString();
    public string Disasm => _command.Disassembled;
    public string Decompiled => _command.Interpreted;

    /// <summary>
    /// Gets the display color for this command based on its opcode.
    /// </summary>
    public Color GetColor()
    {
        var fasOpcode = (FasOpcode)_command.Opcode;
        return fasOpcode.GetColor();
    }
}
