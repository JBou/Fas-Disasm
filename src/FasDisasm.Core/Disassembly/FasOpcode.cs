namespace FasDisasm.Core.Disassembly;

/// <summary>
/// FAS/FSL bytecode opcodes.
/// </summary>
public enum FasOpcode : byte
{
    // Constants
    LoadNil = 0x01,          // Push NIL onto stack
    LoadT = 0x02,            // Push T onto stack

    // Variable access
    PushGlobalVar16 = 0x03,  // VALUE - Push global var value onto stack (16-bit index)
    PushStream = 0x04,       // Push stream reference (FSL)
    GetLocalVar8 = 0x05,     // Push local var onto stack (8-bit index)
    SetGlobalVar16 = 0x06,   // SETQ - Pop into global var (16-bit index)
    CopyElement = 0x07,      // Copy element from one index to another
    SetLocalVar8 = 0x08,     // FSL - Pop into local var (8-bit index)
    PushItem16 = 0x09,       // Push item from global vars onto stack
    PopDummy = 0x0A,         // Pop and discard
    DuplicateTop = 0x0B,     // Duplicate top of stack
    PushGlobalVarFsl = 0x0C, // FSL - Push global var onto stack

    // Branching
    BranchIfFalse16 = 0x0D,  // Branch if false (16-bit offset)
    BranchIfTrue16 = 0x0E,   // Branch if true (16-bit offset)
    JumpFsl = 0x0F,          // FSL jump (16-bit offset)
    ListStep = 0x10,         // Step through list

    // Function definitions
    Defun = 0x14,            // Define function (FAS)
    DefunQ = 0x15,           // Define quoted function
    EndDefun = 0x16,         // End function definition
    DefunFas2 = 0x17,        // FAS2 defun
    SetqFslFunc = 0x1A,      // FSL function assignment
    SetqFslVar = 0x1B,       // FSL variable assignment
    InitDone = 0x1C,         // Init section done

    // Stack cleanup
    EndDefunCleanup = 0x21,  // End defun with cleanup

    // Integers
    LoadInt8 = 0x32,         // Load 8-bit signed integer
    LoadInt32 = 0x33,        // Load 32-bit signed integer

    // Function calls
    Eval = 0x34,             // Evaluate expression
    LoadUsubr = 0x35,        // Load user subroutine
    DefineUsubr = 0x3A,      // Define user subroutine (register function from stack)
    LoadReal = 0x3B,         // Load floating-point number
    BranchIfFalse16_2 = 0x3C, // Branch if false (variant)
    BranchIfTrue16_2 = 0x3D, // Branch if true (variant)
    ExitIfNotZero = 0x3E,    // Exit function if not zero
    ExitIfZero = 0x3F,       // Exit function if zero

    // Module variables
    InitModuleVars = 0x43,   // Initialize module variables

    // Function calls
    AcadFunc = 0x51,         // ACAD function call
    LoadString = 0x55,       // Load string
    LoadSymbolFsl = 0x56,    // FSL - Load symbol
    Goto32 = 0x57,           // Goto (32-bit offset)
    LoadSymbol = 0x5B,       // Load symbol
    GetLocalVar16 = 0x5C,    // Push local var onto stack (16-bit index)
    SetLocalVar16 = 0x5D,    // Pop into local var (16-bit index)
    ClearLocalVar16 = 0x5E,  // Clear local var (16-bit)

    // List operations
    LoadList = 0x39,         // Load list

    // Local var (8-bit) clear
    ClearLocalVar8 = 0x64,   // Clear local var (8-bit)

    // Branching (32-bit)
    BranchIfTrue32 = 0x67,   // Branch if true (32-bit offset)
    CondOr = 0x68,           // Conditional OR
    Branch32 = 0x69,         // Branch (32-bit offset)
    And = 0x6A,              // AND operation
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
        FasOpcode.SetqFslFunc => "FSL_defun",
        FasOpcode.SetqFslVar => "FSL_setq",
        FasOpcode.InitDone => "I_DONE",
        FasOpcode.EndDefunCleanup => "CLEANUP",
        FasOpcode.Eval => "EVAL",
        FasOpcode.LoadUsubr => "ld_USUBR",
        FasOpcode.LoadInt8 => "ld_INT",
        FasOpcode.BranchIfFalse16_2 => "BrIfF16",
        FasOpcode.BranchIfTrue16_2 => "BrIfT16",
        FasOpcode.ExitIfNotZero => "EXIT_NZ",
        FasOpcode.ExitIfZero => "EXIT_Z",
        FasOpcode.InitModuleVars => "INIT_VARS",
        FasOpcode.AcadFunc => "FUNC",
        FasOpcode.LoadString => "ld_STR",
        FasOpcode.LoadSymbolFsl => "ld_SYM",
        FasOpcode.Goto32 => "GOTO",
        FasOpcode.LoadSymbol => "ld_SYM",
        FasOpcode.GetLocalVar16 => "F_getVAR",
        FasOpcode.SetLocalVar16 => "setVAR",
        FasOpcode.ClearLocalVar16 => "clrVAR",
        FasOpcode.LoadList => "ld_LIST",
        FasOpcode.ClearLocalVar8 => "clrVAR8",
        FasOpcode.BranchIfTrue32 => "BrIfT32",
        FasOpcode.CondOr => "CND/OR",
        FasOpcode.Branch32 => "Br32",
        FasOpcode.And => "AND",
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
        FasOpcode.PopDummy => "Pop and discard top of stack",
        FasOpcode.DuplicateTop => "Duplicate top of stack",
        FasOpcode.Defun => "Define function",
        FasOpcode.EndDefun => "End function definition",
        FasOpcode.LoadUsubr => "Load user subroutine",
        FasOpcode.LoadString => "Load string literal",
        FasOpcode.LoadSymbol => "Load symbol",
        FasOpcode.LoadList => "Load list",
        FasOpcode.Goto32 => "Unconditional jump",
        FasOpcode.BranchIfTrue32 => "Branch if condition is true",
        _ => string.Empty
    };
}
