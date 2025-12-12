namespace FasDisasm.Core.Disassembly;

/// <summary>
/// FAS/FSL bytecode opcodes.
/// Based on original VB6 FasFile.cls implementation.
/// </summary>
public enum FasOpcode : byte
{
    // Constants (0x01-0x02)
    LoadNil = 0x01,              // Push NIL onto stack
    LoadT = 0x02,                // Push T onto stack

    // Variable access (0x03-0x0C)
    PushGlobalVar16 = 0x03,      // VALUE - Push global var value onto stack (16-bit index)
    PushStream = 0x04,           // Push stream reference (FSL)
    GetLocalVar8 = 0x05,         // Push local var onto stack (8-bit index)
    SetGlobalVar16 = 0x06,       // SETQ - Pop into global var (16-bit index)
    CopyElement = 0x07,          // Copy element from one index to another
    SetLocalVar8 = 0x08,         // FSL - Pop into local var (8-bit index)
    PushItem16 = 0x09,           // Push item from global vars onto stack
    PopDummy = 0x0A,             // Pop and discard
    DuplicateTop = 0x0B,         // Duplicate top of stack
    PushGlobalVarFsl = 0x0C,     // FSL - Push global var onto stack

    // Branching (0x0D-0x10)
    BranchIfFalse16 = 0x0D,      // Branch if false (16-bit offset)
    BranchIfTrue16 = 0x0E,       // Branch if true (16-bit offset)
    JumpFsl = 0x0F,              // FSL jump (16-bit offset)
    ListStep = 0x10,             // Step through list

    // Function definitions (0x14-0x1C)
    Defun = 0x14,                // Define function (FAS)
    DefunQ = 0x15,               // Define quoted function
    EndDefun = 0x16,             // End function definition
    DefunFas2 = 0x17,            // FAS2 defun
    CopyStackToLocal = 0x18,     // Copy stack to local var
    Unknown19 = 0x19,            // Unknown
    SetqFslFunc = 0x1A,          // FSL function assignment
    SetqFslVar = 0x1B,           // FSL variable assignment
    InitDone = 0x1C,             // Init section done
    Unknown1E = 0x1E,            // Unknown
    Unknown1F = 0x1F,            // Unknown

    // Stack operations (0x20-0x2F)
    Nop20 = 0x20,                // NOP
    EndDefunCleanup = 0x21,      // End defun with cleanup
    NullNot = 0x23,              // Null and Not
    Unknown24 = 0x24,            // Unknown
    Unknown25 = 0x25,            // Unknown
    Unknown26 = 0x26,            // Unknown
    GetFirst = 0x28,             // Get first element (car)
    GetRest = 0x29,              // Get rest elements (cdr)
    Cons = 0x2A,                 // Insert element at beginning (cons)
    Unknown2C = 0x2C,            // Unknown with int8 param
    Unknown2D = 0x2D,            // Unknown
    Unknown2E = 0x2E,            // Unknown
    Unknown2F = 0x2F,            // Unknown

    // Integers (0x32-0x33)
    LoadInt8 = 0x32,             // Load 8-bit signed integer
    LoadInt32 = 0x33,            // Load 32-bit signed integer

    // Function calls (0x34-0x3F)
    Eval = 0x34,                 // Evaluate expression
    LoadUsubr = 0x35,            // Load user subroutine
    Unknown37 = 0x37,            // Unknown
    Convert = 0x38,              // Convert last element on stack
    LoadList = 0x39,             // Load list / Choose array for strings
    DefineUsubr = 0x3A,          // Define user subroutine (register function)
    LoadReal = 0x3B,             // Load floating-point number
    BranchIfFalse16_2 = 0x3C,    // Branch if false (variant)
    BranchIfTrue16_2 = 0x3D,     // Branch if true (variant)
    ExitIfNotZero = 0x3E,        // Pop and exit func if not zero
    ExitIfZero = 0x3F,           // Pop and exit func if zero

    // More operations (0x40-0x5F)
    Unknown40 = 0x40,            // Unknown
    InitModuleVars = 0x43,       // Initialize module variables
    FuncInRam45 = 0x45,          // Function in RAM (Converted)

    // Arithmetic (0x46-0x50)
    Add = 0x46,                  // Addition
    Subtract = 0x47,             // Subtraction
    Multiply = 0x48,             // Multiplication
    Divide = 0x49,               // Division
    Equal = 0x4A,                // Equal comparison
    NotEqual = 0x4B,             // Not equal comparison
    LessThan = 0x4C,             // Less than comparison
    LessOrEqual = 0x4D,          // Less than or equal
    GreaterThan = 0x4E,          // Greater than comparison
    Minus = 0x4F,                // Unary minus
    OneAdd = 0x50,               // Add 1

    AcadFunc = 0x51,             // ACAD function call
    Unknown53 = 0x53,            // Unknown
    Unknown54 = 0x54,            // Unknown
    LoadString = 0x55,           // Load string
    LoadSymbolFsl = 0x56,        // FSL - Load symbol
    Goto32 = 0x57,               // Goto (32-bit offset)
    Unknown59 = 0x59,            // Unknown
    FuncInRam5A = 0x5A,          // Function in RAM (Converted)
    LoadSymbol = 0x5B,           // Load symbol
    GetLocalVar16 = 0x5C,        // Push local var onto stack (16-bit index)
    SetLocalVar16 = 0x5D,        // Pop into local var (16-bit index)
    ClearLocalVar16 = 0x5E,      // Clear local var (16-bit)
    Unknown5F = 0x5F,            // Unknown

    // More operations (0x60-0x6F)
    Unknown60 = 0x60,            // Unknown
    Unknown61 = 0x61,            // Unknown
    Nop62 = 0x62,                // NOP 'b'
    Nop63 = 0x63,                // NOP 'c'
    ClearLocalVar8 = 0x64,       // FSL clear Local Var8
    Unknown65 = 0x65,            // Unknown 'e'
    Unknown66 = 0x66,            // Unknown 'f'
    BranchIfTrue32 = 0x67,       // Branch if true (32-bit offset)
    CondOr = 0x68,               // Conditional OR
    Branch32 = 0x69,             // Branch (32-bit offset)
    And = 0x6A,                  // AND operation
}

/// <summary>
/// Extension methods for FasOpcode.
/// </summary>
public static class FasOpcodeExtensions
{
    /// <summary>
    /// Gets a human-readable name for the opcode.
    /// </summary>
    public static string GetName(this FasOpcode opcode) => opcode switch
    {
        FasOpcode.LoadNil => "ld NIL",
        FasOpcode.LoadT => "ld T",
        FasOpcode.PushGlobalVar16 => "VALUE",
        FasOpcode.PushStream => "STREAM",
        FasOpcode.GetLocalVar8 => "getVAR8",
        FasOpcode.SetGlobalVar16 => "setq",
        FasOpcode.CopyElement => "COPY",
        FasOpcode.SetLocalVar8 => "setVAR8",
        FasOpcode.PushItem16 => "pu_Item",
        FasOpcode.PopDummy => "Pop",
        FasOpcode.DuplicateTop => "Pu_Last",
        FasOpcode.PushGlobalVarFsl => "FSL_Push",
        FasOpcode.BranchIfFalse16 => "BrIfF16",
        FasOpcode.BranchIfTrue16 => "BrIfT16",
        FasOpcode.JumpFsl => "JMP_FSL",
        FasOpcode.ListStep => "LIST_STEP",
        FasOpcode.Defun => "DEFUN",
        FasOpcode.DefunQ => "DEFUN-Q",
        FasOpcode.EndDefun => "END_DEFUN",
        FasOpcode.DefunFas2 => "DEFUN2",
        FasOpcode.CopyStackToLocal => "CPY_STK",
        FasOpcode.SetqFslFunc => "FSL_defun",
        FasOpcode.SetqFslVar => "FSL_setq",
        FasOpcode.InitDone => "I_DONE",
        FasOpcode.EndDefunCleanup => "CLEANUP",
        FasOpcode.NullNot => "NULL/NOT",
        FasOpcode.GetFirst => "car",
        FasOpcode.GetRest => "cdr",
        FasOpcode.Cons => "cons",
        FasOpcode.LoadInt8 => "Ld_INT8",
        FasOpcode.LoadInt32 => "Ld_INT32",
        FasOpcode.Eval => "EVAL",
        FasOpcode.LoadUsubr => "ld_USUBR",
        FasOpcode.Convert => "CONVERT",
        FasOpcode.LoadList => "Ld_LIST",
        FasOpcode.DefineUsubr => "ld_USUBR",
        FasOpcode.LoadReal => "Ld_REAL",
        FasOpcode.BranchIfFalse16_2 => "BrIfF16",
        FasOpcode.BranchIfTrue16_2 => "BrIfT16",
        FasOpcode.ExitIfNotZero => "EXIT_NZ",
        FasOpcode.ExitIfZero => "EXIT_Z",
        FasOpcode.InitModuleVars => "iVars",
        FasOpcode.Add => "+",
        FasOpcode.Subtract => "-",
        FasOpcode.Multiply => "*",
        FasOpcode.Divide => "/",
        FasOpcode.Equal => "=",
        FasOpcode.NotEqual => "/=",
        FasOpcode.LessThan => "<",
        FasOpcode.LessOrEqual => "<=",
        FasOpcode.GreaterThan => ">",
        FasOpcode.Minus => "MINUS",
        FasOpcode.OneAdd => "1+",
        FasOpcode.AcadFunc => "FUNC",
        FasOpcode.LoadString => "Ld_STR",
        FasOpcode.LoadSymbolFsl => "Ld_SYM",
        FasOpcode.Goto32 => "GOTO",
        FasOpcode.LoadSymbol => "Ld_SYM",
        FasOpcode.GetLocalVar16 => "getVAR16",
        FasOpcode.SetLocalVar16 => "setVAR16",
        FasOpcode.ClearLocalVar16 => "clrVAR16",
        FasOpcode.ClearLocalVar8 => "clrVAR8",
        FasOpcode.BranchIfTrue32 => "BrIfT32",
        FasOpcode.CondOr => "CND/OR",
        FasOpcode.Branch32 => "Br32",
        FasOpcode.And => "AND",
        FasOpcode.Nop20 or FasOpcode.Nop62 or FasOpcode.Nop63 => "NOP",
        _ => $"UNK_{(byte)opcode:X2}"
    };

    /// <summary>
    /// Gets a description for the opcode.
    /// </summary>
    public static string GetDescription(this FasOpcode opcode) => opcode switch
    {
        FasOpcode.LoadNil => "Push NIL onto the stack",
        FasOpcode.LoadT => "Push T (true) onto the stack",
        FasOpcode.PushGlobalVar16 => "Push value of global variable",
        FasOpcode.SetGlobalVar16 => "Pop and assign to global variable",
        FasOpcode.PushItem16 => "Push item from global vars onto stack",
        FasOpcode.PopDummy => "Pop and discard top of stack (decrease stack)",
        FasOpcode.DuplicateTop => "Duplicate top of stack",
        FasOpcode.Defun => "Define function",
        FasOpcode.EndDefun => "End function definition",
        FasOpcode.GetFirst => "Get first element of list (car)",
        FasOpcode.GetRest => "Get rest of list (cdr)",
        FasOpcode.Cons => "Pops two elements and pushes a list (cons)",
        FasOpcode.LoadInt8 => "Push signed 8-bit integer from stream => stack",
        FasOpcode.LoadInt32 => "Push signed 32-bit integer from stream => stack",
        FasOpcode.LoadUsubr => "Load user subroutine",
        FasOpcode.DefineUsubr => "Load user subroutine from stream => GVar; 3x Stack",
        FasOpcode.LoadString => "Load string literal",
        FasOpcode.LoadSymbol => "Load symbol",
        FasOpcode.LoadList => "Load list / Combines elements on stack to a LIST",
        FasOpcode.Goto32 => "Unconditional jump",
        FasOpcode.BranchIfTrue32 => "Branch if condition is true",
        FasOpcode.InitModuleVars => "St_init Stackitem => VarPos @nil",
        _ => string.Empty
    };

    /// <summary>
    /// Gets the color for the opcode (for UI display).
    /// </summary>
    public static System.Drawing.Color GetColor(this FasOpcode opcode) => opcode switch
    {
        FasOpcode.Defun or FasOpcode.DefunQ or FasOpcode.DefunFas2 => System.Drawing.Color.FromArgb(255, 128, 0), // Orange
        FasOpcode.EndDefun or FasOpcode.EndDefunCleanup => System.Drawing.Color.FromArgb(255, 128, 0), // Orange
        FasOpcode.LoadString => System.Drawing.Color.Red,
        FasOpcode.LoadSymbol or FasOpcode.LoadSymbolFsl => System.Drawing.Color.Magenta,
        FasOpcode.LoadInt8 or FasOpcode.LoadInt32 or FasOpcode.LoadReal => System.Drawing.Color.Blue,
        FasOpcode.LoadNil or FasOpcode.LoadT => System.Drawing.Color.Green,
        FasOpcode.Goto32 or FasOpcode.Branch32 or FasOpcode.BranchIfTrue32 => System.Drawing.Color.Teal,
        FasOpcode.BranchIfFalse16 or FasOpcode.BranchIfTrue16 or
        FasOpcode.BranchIfFalse16_2 or FasOpcode.BranchIfTrue16_2 => System.Drawing.Color.Teal,
        FasOpcode.SetGlobalVar16 or FasOpcode.SetLocalVar8 or FasOpcode.SetLocalVar16 => System.Drawing.Color.DarkCyan,
        FasOpcode.AcadFunc or FasOpcode.Eval => System.Drawing.Color.DarkBlue,
        FasOpcode.Cons or FasOpcode.GetFirst or FasOpcode.GetRest or FasOpcode.LoadList => System.Drawing.Color.Purple,
        FasOpcode.Add or FasOpcode.Subtract or FasOpcode.Multiply or FasOpcode.Divide => System.Drawing.Color.DarkGreen,
        FasOpcode.DefineUsubr or FasOpcode.LoadUsubr => System.Drawing.Color.DarkMagenta,
        FasOpcode.PopDummy => System.Drawing.Color.Gray,
        FasOpcode.InitModuleVars => System.Drawing.Color.DarkOrange,
        _ => System.Drawing.Color.Black
    };
}
