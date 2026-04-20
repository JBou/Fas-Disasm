using FasDisasm.Core.IO;
using FasDisasm.Core.Types;

namespace FasDisasm.Core.Disassembly;

/// <summary>
/// Disassembles FAS/FSL bytecode into readable instructions and decompiled Lisp code.
/// </summary>
public class FasDisassembler
{
    private readonly FasStack _stack = new();
    private readonly Dictionary<string, FasFunction> _functions = new();
    private readonly List<FasCommand> _commands = new();

    private object?[] _moduleVars0 = Array.Empty<object?>();
    private object?[] _moduleVars1 = Array.Empty<object?>();
    private int _currentModule;
    private FasFunction? _currentFunction;
    private int _indentLevel;

    /// <summary>
    /// Gets the disassembled commands.
    /// </summary>
    public IReadOnlyList<FasCommand> Commands => _commands;

    /// <summary>
    /// Gets the discovered functions.
    /// </summary>
    public IReadOnlyDictionary<string, FasFunction> Functions => _functions;

    /// <summary>
    /// Gets the decompiled Lisp code lines.
    /// </summary>
    public List<string> DecompiledLines { get; } = new();

    /// <summary>
    /// Event raised when a command is disassembled.
    /// </summary>
    public event EventHandler<FasCommand>? CommandDisassembled;

    /// <summary>
    /// Event raised during disassembly progress.
    /// </summary>
    public event EventHandler<ProgressEventArgs>? Progress;

    /// <summary>
    /// Disassembles the FAS file data.
    /// </summary>
    public void Disassemble(FasFileReader fileReader)
    {
        Console.WriteLine("[FasDisassembler] Starting disassembly...");
        Console.WriteLine($"[FasDisassembler] Module 0 vars: {fileReader.ModuleVars[0]?.Length ?? 0}");
        Console.WriteLine($"[FasDisassembler] Module 1 vars: {fileReader.ModuleVars[1]?.Length ?? 0}");
        Console.WriteLine($"[FasDisassembler] Resource data: {fileReader.ResourceData.Length} bytes");

        _moduleVars0 = fileReader.ModuleVars[0];
        _moduleVars1 = fileReader.ModuleVars[1];
        _currentModule = 0;
        _commands.Clear();
        _functions.Clear();
        DecompiledLines.Clear();
        _stack.Clear();

        // Disassemble resource stream (init/main)
        using var resourceStream = fileReader.CreateResourceStream();
        Console.WriteLine($"[FasDisassembler] Disassembling resource stream ({resourceStream.Length} bytes)...");
        DisassembleStream(resourceStream, resourceStream.Length);

        Console.WriteLine($"[FasDisassembler] Disassembly complete. {_commands.Count} commands, {_functions.Count} functions found.");
    }

    /// <summary>
    /// Disassembles a stream of bytecode.
    /// </summary>
    public void DisassembleStream(BinaryStreamReader stream, long stopOffset = long.MaxValue)
    {
        while (!stream.EndOfStream && stream.Position < stopOffset)
        {
            var command = DisassembleCommand(stream);
            if (command != null)
            {
                _commands.Add(command);

                // Add interpreted line to DecompiledLines if it has content
                if (!string.IsNullOrEmpty(command.Interpreted))
                {
                    DecompiledLines.Add(GetIndent() + command.Interpreted);
                }

                CommandDisassembled?.Invoke(this, command);
            }

            Progress?.Invoke(this, new ProgressEventArgs(stream.Position, stream.Length));
        }
    }

    private FasCommand? DisassembleCommand(BinaryStreamReader stream)
    {
        var command = new FasCommand
        {
            Position = stream.Position,
            ModuleId = _currentModule,
            StackPointerBefore = _stack.StackPointer
        };

        command.Opcode = stream.ReadByte();
        var opcode = (FasOpcode)command.Opcode;

        try
        {
            switch (opcode)
            {
                // Constants
                case FasOpcode.LoadNil:
                    DisassembleLoadNil(command);
                    break;

                case FasOpcode.LoadT:
                    DisassembleLoadT(command);
                    break;

                // Variable access
                case FasOpcode.PushGlobalVar16:
                    DisassemblePushGlobalVar16(command, stream);
                    break;

                case FasOpcode.SetGlobalVar16:
                case FasOpcode.SetqFslFunc:
                case FasOpcode.SetqFslVar:
                    DisassembleSetq(command, stream);
                    break;

                case FasOpcode.PushItem16:
                    DisassemblePushItem16(command, stream);
                    break;

                case FasOpcode.PopDummy:
                    DisassemblePopDummy(command);
                    break;

                case FasOpcode.DuplicateTop:
                    DisassembleDuplicateTop(command);
                    break;

                case FasOpcode.GetLocalVar8:
                case FasOpcode.GetLocalVar16:
                    DisassembleGetLocalVar(command, stream);
                    break;

                case FasOpcode.SetLocalVar8:
                case FasOpcode.SetLocalVar16:
                    DisassembleSetLocalVar(command, stream);
                    break;

                case FasOpcode.ClearLocalVar8:
                case FasOpcode.ClearLocalVar16:
                    DisassembleClearLocalVar(command, stream);
                    break;

                case FasOpcode.CopyElement:
                    DisassembleCopyElement(command, stream);
                    break;

                case FasOpcode.PushGlobalVarFsl:
                    DisassemblePushGlobalVarFsl(command, stream);
                    break;

                case FasOpcode.PushStream:
                    DisassemblePushStream(command, stream);
                    break;

                // Function definitions
                case FasOpcode.Defun:
                case FasOpcode.DefunQ:
                    DisassembleDefun(command, stream, opcode == FasOpcode.Defun);
                    break;

                case FasOpcode.EndDefun:
                    DisassembleEndDefun(command);
                    break;

                // Branching
                case FasOpcode.BranchIfFalse16:
                case FasOpcode.BranchIfFalse16_2:
                    DisassembleBranchIfFalse16(command, stream);
                    break;

                case FasOpcode.BranchIfTrue16:
                case FasOpcode.BranchIfTrue16_2:
                    DisassembleBranchIfTrue16(command, stream);
                    break;

                case FasOpcode.BranchIfTrue32:
                    DisassembleBranchIfTrue32(command, stream);
                    break;

                case FasOpcode.Goto32:
                    DisassembleGoto32(command, stream);
                    break;

                case FasOpcode.CondOr:
                    DisassembleCondOr(command, stream);
                    break;

                case FasOpcode.And:
                    DisassembleAnd(command, stream);
                    break;

                // Function definition (register function)
                case FasOpcode.DefineUsubr:
                    DisassembleDefineUsubr(command);
                    break;

                // Function calls
                case FasOpcode.LoadUsubr:
                case FasOpcode.AcadFunc:
                case FasOpcode.Eval:
                    DisassembleLoadUsubr(command, stream, opcode);
                    break;

                // Literals
                case FasOpcode.LoadString:
                    DisassembleLoadString(command, stream);
                    break;

                case FasOpcode.LoadSymbol:
                case FasOpcode.LoadSymbolFsl:
                    DisassembleLoadSymbol(command, stream);
                    break;

                case FasOpcode.LoadList:
                    DisassembleLoadList(command, stream);
                    break;

                case FasOpcode.LoadInt8:
                    DisassembleLoadInt8(command, stream);
                    break;

                case FasOpcode.LoadInt32:
                    DisassembleLoadInt32(command, stream);
                    break;

                case FasOpcode.LoadReal:
                    DisassembleLoadReal(command, stream);
                    break;

                // Module initialization
                case FasOpcode.InitModuleVars:
                    DisassembleInitModuleVars(command, stream);
                    break;

                case FasOpcode.InitDone:
                    DisassembleInitDone(command);
                    break;

                // List operations
                case FasOpcode.GetFirst:
                    DisassembleGetFirst(command);
                    break;

                case FasOpcode.GetRest:
                    DisassembleGetRest(command);
                    break;

                case FasOpcode.Cons:
                    DisassembleCons(command);
                    break;

                // Stack/logical operations
                case FasOpcode.NullNot:
                    DisassembleNullNot(command);
                    break;

                case FasOpcode.Convert:
                    DisassembleConvert(command, stream);
                    break;

                case FasOpcode.ExitIfNotZero:
                    DisassembleExitIfNotZero(command);
                    break;

                case FasOpcode.ExitIfZero:
                    DisassembleExitIfZero(command);
                    break;

                case FasOpcode.ListStep:
                    DisassembleListStep(command, stream);
                    break;

                case FasOpcode.Branch32:
                    DisassembleBranch32(command, stream);
                    break;

                case FasOpcode.CopyStackToLocal:
                    DisassembleCopyStackToLocal(command, stream);
                    break;

                case FasOpcode.EndDefunCleanup:
                    DisassembleEndDefunCleanup(command);
                    break;

                // Arithmetic binary operations
                case FasOpcode.Add:
                case FasOpcode.Subtract:
                case FasOpcode.Multiply:
                case FasOpcode.Divide:
                case FasOpcode.Equal:
                case FasOpcode.NotEqual:
                case FasOpcode.LessThan:
                case FasOpcode.LessOrEqual:
                case FasOpcode.GreaterThan:
                    DisassembleArithmeticBinary(command, opcode);
                    break;

                // Arithmetic unary operations
                case FasOpcode.Minus:
                    DisassembleMinus(command);
                    break;

                case FasOpcode.OneAdd:
                    DisassembleOneAdd(command);
                    break;

                // NOP operations
                case FasOpcode.Nop20:
                case FasOpcode.Nop62:
                case FasOpcode.Nop63:
                    command.DisassembledShort = "NOP";
                    command.Disassembled = $"nop (0x{command.Opcode:X2})";
                    break;

                // FSL-specific
                case FasOpcode.JumpFsl:
                    DisassembleJumpFsl(command, stream);
                    break;

                default:
                    command.DisassembledShort = $"UNK_{command.Opcode:X2}";
                    command.Disassembled = $"Unknown opcode: 0x{command.Opcode:X2}";
                    break;
            }
        }
        catch (Exception ex)
        {
            command.Description = $"Error: {ex.Message}";
        }

        command.StackPointerAfter = _stack.StackPointer;
        command.StackAfter = _stack.GetStackString();

        return command;
    }

    #region Opcode Handlers

    private void DisassembleLoadNil(FasCommand command)
    {
        command.DisassembledShort = "ld NIL";
        command.Disassembled = "Push nil";
        command.Description = "Push NIL onto the stack";

        _stack.Push(FasNil.Instance);
    }

    private void DisassembleLoadT(FasCommand command)
    {
        command.DisassembledShort = "ld T";
        command.Disassembled = "Push T";
        command.Description = "Push T (true) onto the stack";

        _stack.Push(FasT.Instance);
    }

    private void DisassemblePushGlobalVar16(FasCommand command, BinaryStreamReader stream)
    {
        var index = stream.ReadUInt16();
        command.Parameters.Add((byte)(index & 0xFF));
        command.Parameters.Add((byte)(index >> 8));

        var value = GetModuleVar(index);
        command.DisassembledShort = "VALUE";
        command.Disassembled = $"Push value of [{value}]";
        command.Description = "Push global variable value onto stack";

        _stack.Push(value);
    }

    private void DisassembleSetq(FasCommand command, BinaryStreamReader stream)
    {
        var index = stream.ReadUInt16();
        command.Parameters.Add((byte)(index & 0xFF));
        command.Parameters.Add((byte)(index >> 8));

        var targetSymbol = GetModuleVar(index);
        var value = _stack.Pop();

        command.DisassembledShort = "setq";
        command.Disassembled = $"{targetSymbol} = {FormatValue(value)}";
        command.Description = "Pop and assign to global variable";

        var setq = new FasSetq(targetSymbol?.ToString() ?? "", value);
        command.Interpreted = setq.ToLispString();
    }

    private void DisassemblePushItem16(FasCommand command, BinaryStreamReader stream)
    {
        var index = stream.ReadUInt16();
        command.Parameters.Add((byte)(index & 0xFF));
        command.Parameters.Add((byte)(index >> 8));

        var value = GetModuleVar(index);
        command.DisassembledShort = "pu_Item";
        command.Disassembled = $"push {value}";
        command.Description = $"Push item #{index} from global vars onto stack";

        _stack.Push(value);
    }

    private void DisassemblePopDummy(FasCommand command)
    {
        var value = _stack.Pop();
        command.DisassembledShort = "Pop";
        command.Disassembled = "pop dummy (decrease stack)";
        command.Description = "Pop and discard top of stack";
        command.Interpreted = FormatValue(value);
    }

    private void DisassembleDuplicateTop(FasCommand command)
    {
        var value = _stack.Peek();
        _stack.Push(value);

        command.DisassembledShort = "Pu_Last";
        command.Disassembled = $"Push {FormatValue(value)} [Last element again]";
        command.Description = "Duplicate top of stack";
    }

    private void DisassembleGetLocalVar(FasCommand command, BinaryStreamReader stream)
    {
        var is16Bit = command.Opcode == (byte)FasOpcode.GetLocalVar16;
        int index;

        if (is16Bit)
        {
            index = stream.ReadUInt16();
            command.Parameters.Add((byte)(index & 0xFF));
            command.Parameters.Add((byte)(index >> 8));
        }
        else
        {
            index = stream.ReadByte();
            command.Parameters.Add((byte)index);
        }

        var lvar = GetLocalVar(index);
        command.DisassembledShort = is16Bit ? "F_getVAR" : "getVAR";
        command.Disassembled = $"L_{index} => {lvar}";
        command.Description = "Push local variable onto stack";

        _stack.Push(lvar);
    }

    private void DisassembleSetLocalVar(FasCommand command, BinaryStreamReader stream)
    {
        var is16Bit = command.Opcode == (byte)FasOpcode.SetLocalVar16;
        int index;

        if (is16Bit)
        {
            index = stream.ReadUInt16();
            command.Parameters.Add((byte)(index & 0xFF));
            command.Parameters.Add((byte)(index >> 8));
        }
        else
        {
            index = stream.ReadByte();
            command.Parameters.Add((byte)index);
        }

        var value = _stack.Pop();
        SetLocalVar(index, value);

        command.DisassembledShort = "setVAR";
        command.Disassembled = $"L_{index} <= {FormatValue(value)}";
        command.Description = "Pop into local variable";
    }

    private void DisassembleClearLocalVar(FasCommand command, BinaryStreamReader stream)
    {
        var is16Bit = command.Opcode == (byte)FasOpcode.ClearLocalVar16;
        int index;

        if (is16Bit)
        {
            index = stream.ReadUInt16();
            command.Parameters.Add((byte)(index & 0xFF));
            command.Parameters.Add((byte)(index >> 8));
        }
        else
        {
            index = stream.ReadByte();
            command.Parameters.Add((byte)index);
        }

        command.DisassembledShort = "clrVAR";
        command.Disassembled = $"clear L_{index}";
        command.Description = "Clear local variable";

        SetLocalVar(index, null);
    }

    private void DisassembleDefun(FasCommand command, BinaryStreamReader stream, bool isFas)
    {
        // Read 4 parameter bytes
        var p1 = stream.ReadByte();
        var p2Args = stream.ReadByte();
        var p2ArgsMax = stream.ReadByte();
        var p4 = stream.ReadByte();

        command.Parameters.AddRange(new[] { p1, p2Args, p2ArgsMax, p4 });

        // Calculate local variables count (15-bit value from ax:cx)
        var localVarsCount = ((p4 & 0xFE) * 0x80) | p1;
        var hasGc = (p4 & 0x01) != 0;

        // Create function object
        var func = new FasFunction
        {
            StartOffset = command.Position,
            ModuleId = _currentModule,
            ArgumentCount = p2Args,
            LocalVariableCount = localVarsCount
        };

        // Determine function name
        if (_currentModule == 0)
        {
            func.Name = p2Args == 1 ? "fs_init" : "main";
        }

        func.InitializeLocalVariables(localVarsCount, isFas ? "_FAS" : "_FSL");
        _currentFunction = func;

        var keyword = isFas ? "defun" : "defun-q";
        var argsStr = p2Args == 0 ? "()" : $"({string.Join(" ", func.Arguments)})";

        command.DisassembledShort = "DEFUN";
        command.Disassembled = $"({keyword} {func.Name} {argsStr}  LVrs:{localVarsCount} Args:{p2Args}..{p2ArgsMax} GC:{hasGc}";
        command.Description = "Define function";

        if (_currentModule != 0)
        {
            command.Interpreted = $"({keyword} {func.Name} {argsStr}";
            _indentLevel++;
        }
    }

    private void DisassembleEndDefun(FasCommand command)
    {
        command.DisassembledShort = "END_DEFUN";
        command.Disassembled = "end Defun";
        command.Description = "End function definition";

        if (!_stack.IsEmpty)
        {
            var returnValue = _stack.Pop();
            command.Interpreted = FormatValue(returnValue);
        }

        if (_currentModule != 0)
        {
            _indentLevel--;
            command.Interpreted += ")";
        }

        _currentFunction = null;
    }

    /// <summary>
    /// Disassembles opcode 0x3A - Define user subroutine (register function from stack).
    /// Pops: Name, StartOffset, Module from stack.
    /// </summary>
    private void DisassembleDefineUsubr(FasCommand command)
    {
        // Pop parameters from stack (Name, StartOffset, Module)
        var name = _stack.Pop();
        var startOffset = _stack.Pop();
        var module = _stack.Pop();

        var funcName = FormatValue(name);
        var offset = startOffset is FasInt fi ? (int)fi.Value : 0;
        var moduleId = module is FasNil ? 0 : 1;

        command.DisassembledShort = "ld_USUBR";
        command.Disassembled = $"def_Func {funcName} Modul:{moduleId}, Offs: ${offset:X4} [{offset}]";
        command.Description = "Load user subroutine from stream => GVar; 3x Stack";

        // Create and register the function
        var func = new FasFunction
        {
            Name = funcName,
            StartOffset = offset,
            ModuleId = moduleId
        };

        var key = offset.ToString();
        if (!_functions.ContainsKey(key))
        {
            _functions[key] = func;
            Console.WriteLine($"[FasDisassembler] Registered function: {funcName} at offset ${offset:X4}");
        }

        // Push result (USUBR) onto stack
        var usubr = new FasUsubr(funcName, offset);
        _stack.Push(usubr);
    }

    private void DisassembleBranchIfFalse16(FasCommand command, BinaryStreamReader stream)
    {
        var offset = stream.ReadInt16();
        command.Parameters.Add((byte)(offset & 0xFF));
        command.Parameters.Add((byte)(offset >> 8));

        var condition = _stack.Pop();
        var target = stream.Position + offset;

        command.DisassembledShort = "BrIfF16";
        command.Disassembled = $"if ({FormatValue(condition)}==0) then jmp to ${target:X4}";
        command.Description = "Branch if condition is false";
    }

    private void DisassembleBranchIfTrue16(FasCommand command, BinaryStreamReader stream)
    {
        var offset = stream.ReadInt16();
        command.Parameters.Add((byte)(offset & 0xFF));
        command.Parameters.Add((byte)(offset >> 8));

        var condition = _stack.Peek();
        var target = stream.Position + offset;

        command.DisassembledShort = "BrIfT16";
        command.Disassembled = $"if ({FormatValue(condition)}) then pop else jmp to ${target:X4}";
        command.Description = "Branch if condition is true";
    }

    private void DisassembleBranchIfTrue32(FasCommand command, BinaryStreamReader stream)
    {
        var offset = stream.ReadInt32();
        command.Parameters.Add((byte)(offset & 0xFF));
        command.Parameters.Add((byte)((offset >> 8) & 0xFF));
        command.Parameters.Add((byte)((offset >> 16) & 0xFF));
        command.Parameters.Add((byte)((offset >> 24) & 0xFF));

        var condition = _stack.Pop();
        var target = stream.Position + offset;

        command.DisassembledShort = "BrIfT32";
        command.Disassembled = $"if ({FormatValue(condition)}) pop else goto ${target:X4}";
        command.Description = "Branch if condition is true (32-bit offset)";
    }

    private void DisassembleGoto32(FasCommand command, BinaryStreamReader stream)
    {
        var offset = stream.ReadInt32();
        command.Parameters.Add((byte)(offset & 0xFF));
        command.Parameters.Add((byte)((offset >> 8) & 0xFF));
        command.Parameters.Add((byte)((offset >> 16) & 0xFF));
        command.Parameters.Add((byte)((offset >> 24) & 0xFF));

        var target = stream.Position + offset;

        command.DisassembledShort = "GOTO";
        command.Disassembled = $"goto ${target:X4}";
        command.Description = "Unconditional jump";
    }

    private void DisassembleCondOr(FasCommand command, BinaryStreamReader stream)
    {
        var offset = stream.ReadInt32();
        command.Parameters.Add((byte)(offset & 0xFF));
        command.Parameters.Add((byte)((offset >> 8) & 0xFF));
        command.Parameters.Add((byte)((offset >> 16) & 0xFF));
        command.Parameters.Add((byte)((offset >> 24) & 0xFF));

        var condition = _stack.Peek();
        var target = stream.Position + offset;

        command.DisassembledShort = "CND/OR";
        command.Disassembled = $"cond/or If ({FormatValue(condition)}) Goto ${target:X4} Else .pop";
        command.Description = "Conditional OR";
    }

    private void DisassembleAnd(FasCommand command, BinaryStreamReader stream)
    {
        var offset = stream.ReadInt32();
        command.Parameters.Add((byte)(offset & 0xFF));
        command.Parameters.Add((byte)((offset >> 8) & 0xFF));
        command.Parameters.Add((byte)((offset >> 16) & 0xFF));
        command.Parameters.Add((byte)((offset >> 24) & 0xFF));

        var condition = _stack.Peek();
        var target = stream.Position + offset;

        command.DisassembledShort = "AND";
        command.Disassembled = $"and_If ({FormatValue(condition)}) .pop Else goto ${target:X4}";
        command.Description = "AND operation";
    }

    private void DisassembleLoadUsubr(FasCommand command, BinaryStreamReader stream, FasOpcode opcode)
    {
        var paramCount = stream.ReadByte();
        command.Parameters.Add(paramCount);

        ushort? funcIndex = null;
        if (opcode == FasOpcode.LoadUsubr || opcode == FasOpcode.AcadFunc)
        {
            funcIndex = stream.ReadUInt16();
            command.Parameters.Add((byte)(funcIndex.Value & 0xFF));
            command.Parameters.Add((byte)(funcIndex.Value >> 8));
        }

        var flags = stream.ReadByte();
        command.Parameters.Add(flags);

        if (opcode == FasOpcode.AcadFunc)
        {
            var extra = stream.ReadByte();
            command.Parameters.Add(extra);
        }

        var funcName = funcIndex.HasValue ? GetModuleVar(funcIndex.Value) : null;

        // Pop parameters from stack
        var args = _stack.PopArray(paramCount);

        var shortName = opcode switch
        {
            FasOpcode.Eval => "EVAL",
            FasOpcode.LoadUsubr => "ld_USUBR",
            FasOpcode.AcadFunc => "FUNC",
            _ => "CALL"
        };

        command.DisassembledShort = shortName;
        command.Disassembled = $"{funcName} {paramCount} Params are above...";
        command.Description = "Call user subroutine";

        // Format the function call
        var argsStr = string.Join(" ", args.Select(FormatValue));
        command.Interpreted = $"({funcName} {argsStr})";

        // Push result placeholder
        _stack.Push($"<result of {funcName}>");
    }

    private void DisassembleLoadString(FasCommand command, BinaryStreamReader stream)
    {
        var length = stream.ReadByte();
        command.Parameters.Add(length);

        var str = stream.ReadFixedString(length);
        var fasStr = new FasString(str);

        command.DisassembledShort = "ld_STR";
        command.Disassembled = $"Push {fasStr.ToLispString()}";
        command.Description = "Load string literal";

        _stack.Push(fasStr);
    }

    private void DisassembleLoadSymbol(FasCommand command, BinaryStreamReader stream)
    {
        var length = stream.ReadByte();
        command.Parameters.Add(length);

        var name = stream.ReadFixedString(length);
        var symbol = new FasSymbol(name);

        command.DisassembledShort = "ld_SYM";
        command.Disassembled = $"Push symbol '{name}'";
        command.Description = "Load symbol";

        _stack.Push(symbol);
    }

    private void DisassembleLoadList(FasCommand command, BinaryStreamReader stream)
    {
        var count = stream.ReadByte();
        command.Parameters.Add(count);

        // Pop items from stack to form the list
        var items = _stack.PopArray(count);
        var list = new FasList(items);

        command.DisassembledShort = "ld_LIST";
        command.Disassembled = $"Create list with {count} items";
        command.Description = "Load list";

        _stack.Push(list);
    }

    /// <summary>
    /// Disassembles opcode 0x32 - Load 8-bit signed integer.
    /// </summary>
    private void DisassembleLoadInt8(FasCommand command, BinaryStreamReader stream)
    {
        var value = stream.ReadSByte();
        command.Parameters.Add((byte)value);

        var fasInt = new FasInt(value, 1);

        command.DisassembledShort = "Ld_INT8";
        command.Disassembled = $"push {value:00}";
        command.Description = "Push signed 8-bit integer from stream => stack";

        _stack.Push(fasInt);
    }

    /// <summary>
    /// Disassembles opcode 0x33 - Load 32-bit signed integer.
    /// </summary>
    private void DisassembleLoadInt32(FasCommand command, BinaryStreamReader stream)
    {
        var value = stream.ReadInt32();
        command.Parameters.Add((byte)(value & 0xFF));
        command.Parameters.Add((byte)((value >> 8) & 0xFF));
        command.Parameters.Add((byte)((value >> 16) & 0xFF));
        command.Parameters.Add((byte)((value >> 24) & 0xFF));

        var fasInt = new FasInt(value, 4);

        command.DisassembledShort = "Ld_INT32";
        command.Disassembled = $"push {value}";
        command.Description = "Push signed 32-bit integer from stream => stack";

        _stack.Push(fasInt);
    }

    /// <summary>
    /// Disassembles opcode 0x3B - Load floating-point (real) number.
    /// The number is stored as a zero-terminated string.
    /// </summary>
    private void DisassembleLoadReal(FasCommand command, BinaryStreamReader stream)
    {
        // Read null-terminated string representing the real number
        var sb = new System.Text.StringBuilder();
        byte b;
        while ((b = stream.ReadByte()) != 0 && !stream.EndOfStream)
        {
            sb.Append((char)b);
        }

        var realStr = sb.ToString();
        double.TryParse(realStr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var realValue);

        var fasReal = new FasReal(realValue);

        command.DisassembledShort = "Ld_REAL";
        command.Disassembled = $"push {realStr}";
        command.Description = "Push floating-point number string from stream => stack";

        _stack.Push(fasReal);
    }

    private void DisassembleInitModuleVars(FasCommand command, BinaryStreamReader stream)
    {
        var varPos = stream.ReadUInt16();
        var count = stream.ReadUInt16();

        command.Parameters.Add((byte)(varPos & 0xFF));
        command.Parameters.Add((byte)(varPos >> 8));
        command.Parameters.Add((byte)(count & 0xFF));
        command.Parameters.Add((byte)(count >> 8));

        // Get module from stack
        var moduleObj = _stack.Pop();
        var moduleId = moduleObj is FasNil ? 0 : 1;

        command.DisassembledShort = "iVars";
        command.Disassembled = $"Init {count} vars at position {varPos} in module {moduleId}";
        command.Description = "Initialize module variables from stack";

        // Pop items and store in module vars
        for (int i = varPos + count - 1; i >= varPos; i--)
        {
            var value = _stack.Pop();
            SetModuleVar(i, moduleId, value);
        }
    }

    private void DisassembleInitDone(FasCommand command)
    {
        if (!_stack.IsEmpty)
        {
            _stack.Pop();
        }

        command.DisassembledShort = "I_DONE";
        command.Disassembled = "Init done";
        command.Description = "Initialization section complete";

        _stack.Clear();
    }

    private void DisassembleArithmeticBinary(FasCommand command, FasOpcode opcode)
    {
        var op = opcode switch
        {
            FasOpcode.Add => "+",
            FasOpcode.Subtract => "-",
            FasOpcode.Multiply => "*",
            FasOpcode.Divide => "/",
            FasOpcode.Equal => "=",
            FasOpcode.NotEqual => "/=",
            FasOpcode.LessThan => "<",
            FasOpcode.LessOrEqual => "<=",
            FasOpcode.GreaterThan => ">",
            _ => "?"
        };

        var right = _stack.Pop();
        var left = _stack.Pop();
        var result = $"({op} {FormatValue(left)} {FormatValue(right)})";

        command.DisassembledShort = op;
        command.Disassembled = result;
        command.Description = $"Binary operation: {op}";
        command.Interpreted = result;

        _stack.Push(result);
    }

    private void DisassembleMinus(FasCommand command)
    {
        var value = _stack.Pop();
        var result = $"(- {FormatValue(value)})";

        command.DisassembledShort = "MINUS";
        command.Disassembled = result;
        command.Description = "Unary minus";
        command.Interpreted = result;

        _stack.Push(result);
    }

    private void DisassembleOneAdd(FasCommand command)
    {
        var value = _stack.Pop();
        var result = $"(1+ {FormatValue(value)})";

        command.DisassembledShort = "1+";
        command.Disassembled = result;
        command.Description = "Add 1";
        command.Interpreted = result;

        _stack.Push(result);
    }

    private void DisassembleGetFirst(FasCommand command)
    {
        var list = _stack.Pop();
        var result = $"(car {FormatValue(list)})";

        command.DisassembledShort = "car";
        command.Disassembled = result;
        command.Description = "Get first element of list (car)";
        command.Interpreted = result;

        _stack.Push(result);
    }

    private void DisassembleGetRest(FasCommand command)
    {
        var list = _stack.Pop();
        var result = $"(cdr {FormatValue(list)})";

        command.DisassembledShort = "cdr";
        command.Disassembled = result;
        command.Description = "Get rest of list (cdr)";
        command.Interpreted = result;

        _stack.Push(result);
    }

    private void DisassembleCons(FasCommand command)
    {
        var rest = _stack.Pop();
        var first = _stack.Pop();
        var result = $"(cons {FormatValue(first)} {FormatValue(rest)})";

        command.DisassembledShort = "cons";
        command.Disassembled = result;
        command.Description = "Pops two elements and pushes a list (cons)";
        command.Interpreted = result;

        _stack.Push(result);
    }

    private void DisassembleNullNot(FasCommand command)
    {
        var value = _stack.Pop();
        var result = $"(null {FormatValue(value)})";

        command.DisassembledShort = "NULL/NOT";
        command.Disassembled = $"null/not {FormatValue(value)}";
        command.Description = "Null and Not";
        command.Interpreted = result;

        _stack.Push(result);
    }

    private void DisassembleConvert(FasCommand command, BinaryStreamReader stream)
    {
        var param = stream.ReadByte();
        command.Parameters.Add(param);

        var value = _stack.Pop();

        command.DisassembledShort = "CONVERT";
        command.Disassembled = $"convert {FormatValue(value)} (param={param})";
        command.Description = "Convert last element on stack";

        _stack.Push(value);
    }

    private void DisassembleExitIfNotZero(FasCommand command)
    {
        var value = _stack.Pop();

        command.DisassembledShort = "EXIT_NZ";
        command.Disassembled = $"exit if not zero: {FormatValue(value)}";
        command.Description = "Pop and exit func if not zero";
    }

    private void DisassembleExitIfZero(FasCommand command)
    {
        var value = _stack.Pop();

        command.DisassembledShort = "EXIT_Z";
        command.Disassembled = $"exit if zero: {FormatValue(value)}";
        command.Description = "Pop and exit func if zero";
    }

    private void DisassembleListStep(FasCommand command, BinaryStreamReader stream)
    {
        var offset = stream.ReadInt16();
        command.Parameters.Add((byte)(offset & 0xFF));
        command.Parameters.Add((byte)(offset >> 8));

        var target = stream.Position + offset;

        command.DisassembledShort = "LIST_STEP";
        command.Disassembled = $"list step -> ${target:X4}";
        command.Description = "Step through list";
    }

    private void DisassembleBranch32(FasCommand command, BinaryStreamReader stream)
    {
        var offset = stream.ReadInt32();
        command.Parameters.Add((byte)(offset & 0xFF));
        command.Parameters.Add((byte)((offset >> 8) & 0xFF));
        command.Parameters.Add((byte)((offset >> 16) & 0xFF));
        command.Parameters.Add((byte)((offset >> 24) & 0xFF));

        var target = stream.Position + offset;

        command.DisassembledShort = "Br32";
        command.Disassembled = $"branch ${target:X4}";
        command.Description = "Branch (32-bit offset)";
    }

    private void DisassembleCopyStackToLocal(FasCommand command, BinaryStreamReader stream)
    {
        var index = stream.ReadByte();
        command.Parameters.Add(index);

        command.DisassembledShort = "CPY_STK";
        command.Disassembled = $"copy stack to L_{index}";
        command.Description = "Copy stack to local var";
    }

    private void DisassembleEndDefunCleanup(FasCommand command)
    {
        command.DisassembledShort = "CLEANUP";
        command.Disassembled = "end defun with cleanup";
        command.Description = "End defun with cleanup";

        _currentFunction = null;
    }

    private void DisassembleCopyElement(FasCommand command, BinaryStreamReader stream)
    {
        var from = stream.ReadByte();
        var to = stream.ReadByte();
        command.Parameters.Add(from);
        command.Parameters.Add(to);

        command.DisassembledShort = "COPY";
        command.Disassembled = $"copy element from {from} to {to}";
        command.Description = "Copy element from one index to another";
    }

    private void DisassemblePushGlobalVarFsl(FasCommand command, BinaryStreamReader stream)
    {
        var index = stream.ReadByte();
        command.Parameters.Add(index);

        var value = GetModuleVar(index);
        command.DisassembledShort = "FSL_Push";
        command.Disassembled = $"Push [{value}]";
        command.Description = "FSL - Push global var onto stack";

        _stack.Push(value);
    }

    private void DisassemblePushStream(FasCommand command, BinaryStreamReader stream)
    {
        var index = stream.ReadUInt16();
        command.Parameters.Add((byte)(index & 0xFF));
        command.Parameters.Add((byte)(index >> 8));

        command.DisassembledShort = "STREAM";
        command.Disassembled = $"Push stream reference {index}";
        command.Description = "Push stream reference (FSL)";

        _stack.Push($"<stream#{index}>");
    }

    private void DisassembleJumpFsl(FasCommand command, BinaryStreamReader stream)
    {
        var offset = stream.ReadInt16();
        command.Parameters.Add((byte)(offset & 0xFF));
        command.Parameters.Add((byte)(offset >> 8));

        var target = stream.Position + offset;

        command.DisassembledShort = "JMP_FSL";
        command.Disassembled = $"jmp ${target:X4}";
        command.Description = "FSL jump (16-bit offset)";
    }

    #endregion

    #region Helper Methods

    private object? GetModuleVar(int index)
    {
        var vars = _currentModule == 0 ? _moduleVars0 : _moduleVars1;
        if (index >= 0 && index < vars.Length)
            return vars[index];
        return $"MVar#{index}";
    }

    private void SetModuleVar(int index, int module, object? value)
    {
        var vars = module == 0 ? _moduleVars0 : _moduleVars1;
        if (index >= 0 && index < vars.Length)
            vars[index] = value;
    }

    private FasLocalVar GetLocalVar(int index)
    {
        if (_currentFunction != null)
            return _currentFunction.GetLocalVariable(index);
        return new FasLocalVar(index);
    }

    private void SetLocalVar(int index, object? value)
    {
        if (_currentFunction != null)
        {
            var name = value?.ToString() ?? $"_fas{index}";
            _currentFunction.SetLocalVariable(index, name);
        }
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "nil",
            IFasType fasType => fasType.ToLispString(),
            _ => value.ToString() ?? "nil"
        };
    }

    private string GetIndent()
    {
        return new string(' ', _indentLevel * 2);
    }

    #endregion
}
