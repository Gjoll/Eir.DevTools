﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Eir.DevTools
{
    [DebuggerDisplay("{Name}")]
    public class CodeBlockNested : CodeBlock
    {
        const String IndentOneLevel = "    ";

        public const Int32 CommentCol = 140;
        public delegate void CodeCallback(CodeBlockNested code);

        /// <summary>
        /// Return true if empty.
        /// </summary>
        public override bool Empty
        {
            get
            {
                foreach (CodeBlock block in this.Children)
                    if (block.Empty == false)
                        return false;
                return true;
            }
        }

        public String Name { get; }
        public IEnumerable<CodeBlockNested> AllNamedBlocks => this.NamedBlocks.Values;
        public bool HasNamedBlocks => this.NamedBlocks.Count > 0;
        public bool HasTextBlocks => this.Children.Count > this.NamedBlocks.Count;

        readonly Dictionary<String, CodeBlockNested> NamedBlocks = new Dictionary<String, CodeBlockNested>();
        Int32 macroInhibit = 0;

        /// <summary>
        /// Clear all content.
        /// </summary>
        public override void Clear()
        {
            this.NamedBlocks.Clear();
            this.Children.Clear();
        }

        /// <summary>
        /// return true if name matches regular expression one time
        /// </summary>
        public bool MatchName(String regEx)
        {
            Regex exp = new Regex(regEx);
            var matches = exp.Matches(this.Name);
            switch (matches.Count)
            {
                case 0: return false;
                case 1: return true;
                default:
                    throw new Exception($"MatchName failed because name '{this.Name}' matched expression '{regEx}' multiple times");
            }
        }

        /// <summary>
        /// Merge this block the blocks in mergeFrom.
        /// Names child blocks are only merged if they match the indicated regular expression.
        /// </summary>
        public void Merge(String regEx, CodeBlockNested mergeFrom)
        {
            if (!MatchName(regEx))
            {
                MergeNamedChildren(regEx, mergeFrom);
                return;
            }

            if (this.HasNamedBlocks && this.HasTextBlocks)
                throw new Exception($"Block '{this.Name}' can not be a merge target when it contain both named blocks and unnamed/text blocs");

            if (mergeFrom.HasNamedBlocks && mergeFrom.HasTextBlocks)
                throw new Exception($"Block '{mergeFrom.Name}' can not be a merge source when it contain both named blocks and unnamed/text blocs");

            if (this.HasTextBlocks)
                this.Clear();

            foreach (CodeBlock mergeBlock in mergeFrom.Children)
            {
                CodeBlockNested? mergeBlockNested = mergeBlock as CodeBlockNested;
                if (mergeBlockNested != null)
                {
                    CodeBlockNested? localBlock = this.Find(mergeBlockNested.Name);
                    if (localBlock != null)
                        localBlock.Merge(regEx, mergeBlockNested);
                }
                else
                    this.Children.Add(mergeBlock);
            }
        }

        /// <summary>
        /// Merge this block children with the children of mergeFrom.
        /// </summary>
        public void MergeNamedChildren(String regEx, CodeBlockNested mergeFrom)
        {
            foreach (CodeBlock mergeBlock in mergeFrom.Children)
            {
                CodeBlockNested? mergeBlockNested = mergeBlock as CodeBlockNested;
                if (mergeBlockNested != null)
                {
                    CodeBlockNested? localBlock = this.Find(mergeBlockNested.Name);
                    if (localBlock != null)
                        localBlock.Merge(regEx, mergeBlockNested);
                }
            }
        }

        /// <summary>
        /// Clear all content in named blocks with names that don't match the indicated suffix regular expression
        /// </summary>
        public void Clear(String regEx)
        {
            if (!MatchName(regEx))
                return;

            foreach (CodeBlock block in this.Children)
            {
                if (!(block is CodeBlockNested))
                    block.Clear();
            }
            foreach (CodeBlockNested block in this.NamedBlocks.Values)
                block.Clear(regEx);
        }

        /// <summary>
        /// Purge all children that are not in list.
        /// </summary>
        /// <param name="usedBlocks"></param>
        public void PurgeUnusedChildren(List<String> usedBlocks)
        {
            if (usedBlocks is null)
                throw new ArgumentNullException(nameof(usedBlocks));

            Int32 index = 0;
            while (index < this.Children.Count)
            {
                CodeBlockNested? child = this.Children[index] as CodeBlockNested;
                if (
                    (child != null) &&
                    (usedBlocks.Contains(child.Name) == false)
                    )
                    this.Children.RemoveAt(index);
                else
                    index += 1;
            }
        }
        /// <summary>
        /// Return access to lines. This block must contains a single text block. otherwsie throw error.
        /// </summary>
        public List<String> Code
        {
            get
            {
                switch (this.Children.Count)
                {
                    case 0:
                        CodeBlockText temp = new CodeBlockText(this.owner);
                        this.Children.Add(temp);
                        return temp.Code;

                    case 1:
                        if (this.Children[0] is CodeBlockText textBlock)
                            return textBlock.Code;
                        break;
                }

                throw new Exception("Invalid block access.");
            }
        }

        /// <summary>
        /// String that starts block ('//+...')
        /// </summary>
        public String StartLine
        {
            get { return this.startLine; }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                this.startLine = value;
                this.baseMargin = value.Substring(0, value.IndexOf(this.owner.BlockStart));
            }
        }
        String startLine = String.Empty;

        public String MarginString => this.baseMargin + this.indentMargin;
        public Int32 MarginLength => this.baseMargin.Length + this.indentMargin.Length;
        String baseMargin = "";
        String indentMargin = "";

        /// <summary>
        /// String that ends block ('//-...')
        /// </summary>
        public String EndLine { get; set; } = String.Empty;

        /// <summary>
        /// Strings that make up code file.
        /// </summary>
        public List<CodeBlock> Children { get; } = new List<CodeBlock>();

        public CodeBlockNested(CodeEditor owner, String name) : base(owner)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            this.Name = name.Trim();
        }

        public CodeBlockNested IndentMarkerLines(Int32 indent)
        {
            StringBuilder sb = new StringBuilder();
            while (indent-- > 0)
                sb.Append(IndentOneLevel);
            this.StartLine = $"{sb.ToString()}{this.StartLine.Trim()}";
            this.EndLine = $"{sb.ToString()}{this.EndLine.Trim()}";
            return this;
        }

        public CodeBlockNested SetBaseMargin(Int32 indentLevel)
        {
            this.baseMargin = String.Empty;
            for (Int32 i = 0; i < indentLevel; i++)
                this.baseMargin = this.baseMargin + IndentOneLevel;
            return this;
        }

        public CodeBlockNested SetIndent(Int32 indentLevel)
        {
            this.indentMargin = String.Empty;
            for (Int32 i = 0; i < indentLevel; i++)
                this.indentMargin = this.indentMargin + IndentOneLevel;
            return this;
        }

        public CodeBlockNested Indent()
        {
            this.indentMargin = this.indentMargin + IndentOneLevel;
            return this;
        }

        public CodeBlockNested Unindent()
        {
            this.indentMargin = this.indentMargin.Substring(0, this.indentMargin.Length - 4);
            return this;
        }

        /// <summary>
        /// Replace existing block with passed code.
        /// </summary>
        /// <param name="blockName"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public Boolean Replace(String blockName, String code, bool addMargin) => this.Replace(blockName, code.ToLines(), addMargin);

        /// <summary>
        /// Replace existing block with passed code.
        /// </summary>
        /// <param name="blockName"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public Boolean Replace(String blockName, String[] code, bool addMargin)
        {
            if (this.NamedBlocks.TryGetValue(blockName, out CodeBlockNested? block) == false)
                return false;
            block.Children.Clear();
            block.Load(code, addMargin);
            return true;
        }

        /// <summary>
        /// Find names block.
        /// throw exception if not found.
        /// </summary>
        /// <param name="blockName"></param>
        /// <returns></returns>
        public CodeBlockNested FindRequired(String blockName)
        {
            if (this.NamedBlocks.TryGetValue(blockName, out CodeBlockNested? block) == false)
                throw new Exception($"Required block {blockName} not found");
            return block;
        }

        /// <summary>
        /// Find names block.
        /// </summary>
        /// <param name="blockName"></param>
        /// <returns></returns>
        public CodeBlockNested? Find(String blockName, Boolean createFlag = false)
        {
            if (
                (this.NamedBlocks.TryGetValue(blockName, out CodeBlockNested? block) == false) &&
                createFlag
            )
            {
                block = new CodeBlockNested(this.owner, blockName)
                {
                    StartLine = $"{this.MarginString}{this.owner.BlockStart} {blockName}{this.owner.BlockStartTerm}",
                    EndLine = $"{this.MarginString}{this.owner.BlockEnd} {blockName}{this.owner.BlockEndTerm}"
                };
                this.NamedBlocks.Add(blockName, block);
                this.Children.Add(block);
            }

            return block;
        }

        /// <summary>
        /// Return array of all lines
        /// </summary>
        public override String[] Lines()
        {
            List<String> retVal = new List<String>();
            foreach (CodeBlock block in this.Children)
                retVal.AddRange(block.AllLines());
            return retVal.ToArray();
        }

        /// <summary>
        /// Return array of all lines
        /// </summary>
        public override String Text()
        {
            StringBuilder sb = new StringBuilder();
            foreach (String line in this.Lines())
                sb.AppendLine(line);
            return sb.ToString();
        }

        /// <summary>
        /// Return array of all lines
        /// </summary>
        public override String[] AllLines()
        {
            List<String> retVal = new List<String>();
            if ((string.IsNullOrWhiteSpace(this.Name) == false) && (this.Name[0] != '*'))
                retVal.Add(this.StartLine);
            foreach (CodeBlock block in this.Children)
                retVal.AddRange(block.AllLines());
            if ((string.IsNullOrWhiteSpace(this.Name) == false) && (this.Name[0] != '*'))
                retVal.Add(this.EndLine);
            return retVal.ToArray();
        }

        /// <summary>
        /// Return array of all lines
        /// </summary>
        public override String AllText()
        {
            StringBuilder sb = new StringBuilder();
            foreach (String line in this.AllLines())
                sb.AppendLine(line);
            return sb.ToString();
        }

        /// <summary>
        /// Load code from 
        /// </summary>
        /// <param name="path"></param>
        public void Load(String[] lines, bool addMargin)
        {
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            this.Children.Clear();
            Int32 index = 0;
            this.DoLoad(lines, addMargin, ref index);
        }

        /// <summary>
        /// Reload block.
        /// This will expand macros that may have been previously unexpanded.
        /// </summary>
        public void Reload()
        {
            foreach (CodeBlock child in this.Children)
            {
                switch (child)
                {
                    case CodeBlockText text:
                        String[] lines = text.AllLines();
                        text.Clear();
                        foreach (String line in lines)
                            text.Code.Add(this.ProcessLine(line));
                        break;

                    case CodeBlockNested nested:
                        nested.Reload();
                        break;

                    default:
                        throw new NotImplementedException("Unknown code block type");
                }
            }
        }

        /// <summary>
        /// Load code from 
        /// </summary>
        /// <param name="path"></param>
        void DoLoad(String[] lines, bool addMargin, ref Int32 index)
        {
            CodeBlockText? currentBlock = null;

            while (index < lines.Length)
            {
                String line = lines[index++];
                String s = line.Trim();

                if (s.StartsWith(this.owner.BlockStart))
                {
                    // All blocks start with //+
                    String blockName = s.Substring(this.owner.BlockStart.Length).Trim();
                    if (String.IsNullOrEmpty(this.owner.BlockStartTerm) == false)
                    {
                        if (blockName.EndsWith(this.owner.BlockStartTerm) == false)
                            throw new Exception($"Invalid termination for block start '{s}'");
                        blockName = blockName.Substring(0, blockName.Length - this.owner.BlockStartTerm.Length);
                    }

                    blockName = blockName.Trim();

                    CodeBlockNested block = new CodeBlockNested(this.owner, blockName);
                    block.StartLine = line;
                    this.Children.Add(block);
                    if (block.Name.Length > 0)
                        this.NamedBlocks.Add(block.Name, block);
                    block.DoLoad(lines, addMargin, ref index);
                    currentBlock = null;
                }
                else if (s.StartsWith(this.owner.BlockEnd))
                {
                    // All blocks end  with //-
                    this.EndLine = line;
                    return;
                }
                else
                {
                    if (currentBlock == null)
                    {
                        currentBlock = new CodeBlockText(this.owner);
                        this.Children.Add(currentBlock);
                    }

                    line = this.ProcessLine(line);
                    if (addMargin)
                        currentBlock.Code.Add($"{this.MarginString}{line}");
                    else
                        currentBlock.Code.Add(line);
                }
            }
        }

        /// <summary>
        /// Add block of indicated name
        /// </summary>
        /// <param name="path"></param>
        public CodeBlockNested AppendBlock(String blockName = "")
        {
            CodeBlockNested block = new CodeBlockNested(this.owner, blockName)
            {
                StartLine = $"{this.MarginString}{this.owner.BlockStart} {blockName}{this.owner.BlockStartTerm}",
                EndLine = $"{this.MarginString}{this.owner.BlockEnd} {blockName}{this.owner.BlockEndTerm}"
            };
            return AppendBlock(block);
        }

        public CodeBlock AppendBlock(CodeBlock block)
        {
            this.Children.Add(block);
            CodeBlockNested? blockNested = block as CodeBlockNested;
            if (blockNested != null)
                this.NamedBlocks.Add(blockNested.Name, blockNested);
            return block;
        }

        public CodeBlockText AppendBlock(CodeBlockText block)
        {
            this.Children.Add(block);
            return block;
        }

        public CodeBlockNested AppendBlock(CodeBlockNested block)
        {
            this.Children.Add(block);
            if (String.IsNullOrEmpty(block.Name) == false)
                this.NamedBlocks.Add(block.Name, block);
            return block;
        }

        /// <summary>
        /// Load code from 
        /// </summary>
        /// <param name="path"></param>
        public CodeBlockNested AppendRaw(String line)
        {
            CodeBlockText? currentBlock = null;
            if (this.Children.Count > 0)
                currentBlock = this.Children[this.Children.Count - 1] as CodeBlockText;

            if (currentBlock == null)
            {
                currentBlock = new CodeBlockText(this.owner);
                this.Children.Add(currentBlock);
            }

            String[] lines = line.ToLines();
            foreach (String l in lines)
                currentBlock.Code.Add(l);
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="path"></param>
        public CodeBlockNested? AppendUniqueLine(String line, String? margin = null)
        {
            if (margin == null)
                margin = this.MarginString;
            String formattedLine = this.ProcessLine($"{margin}{line}");
            String trimmedLine = formattedLine.Trim();
            foreach (String existingLine in this.Lines())
            {
                String existingLineTrimmed = existingLine.Trim();
                if (String.Compare(existingLineTrimmed, trimmedLine) == 0)
                    return this;
            }
            this.AppendRaw(formattedLine);
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="path"></param>
        public CodeBlockNested AppendLine(String line, String? margin = null)
        {
            if (margin == null)
                margin = this.MarginString;
            this.AppendRaw(this.ProcessLine($"{margin}{line}"));
            return this;
        }

        /// <summary>
        /// Append comment lines. Split long lines. 
        /// </summary>
        public CodeBlockNested AppendComment(params String[] lines)
        {
            if (lines == null)
                return this;

            this.macroInhibit += 1;
            foreach (String line in lines)
            {
                this.AppendLine($"// {line}");
            }
            this.macroInhibit -= 1;
            return this;
        }

        /// <summary>
        /// Append comment lines. Split long lines. 
        /// </summary>
        public CodeBlockNested AppendComment(String line)
        {
            const Int32 maxLength = 60;

            if (line == null)
                return this;

            this.macroInhibit += 1;
            while (line.Length > 0)
            {
                Int32 length = 0;
                StringBuilder sb = new StringBuilder();

                bool done = false;
                while ((length < line.Length) && (done == false))
                {
                    Char c = line[length++];
                    switch (c)
                    {
                        case '\r':
                            break;
                        case '\n':
                            done = true;
                            break;
                        case ' ':
                            if (length > maxLength)
                                done = true;
                            sb.Append(c);
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }
                this.AppendLine($"// {line.Substring(0, length)}");
                line = line.Substring(length);
            }

            this.macroInhibit -= 1;
            return this;
        }


        public CodeBlockNested SummaryOpen([CallerFilePath] String filePath = "",
            [CallerLineNumber] Int32 lineNumber = 0)
        {
            this.AppendLine($"// {Path.GetFileName(filePath)}:{lineNumber}");
            this.AppendLine("/// <summary>");
            return this;
        }

        public CodeBlockNested Summary(String line)
        {
            this.AppendLine($"/// {line}");
            return this;
        }

        public CodeBlockNested Summary(IEnumerable<String> lines)
        {
            this.AppendLines("/// ", lines);
            return this;
        }

        public CodeBlockNested SummaryClose()
        {
            this.AppendLine("/// </summary>");
            return this;
        }

        public CodeBlockNested SummaryLines(String lines)
        {
            String[] lineArr = lines.ToLines();
            foreach (String line in lineArr)
                this.AppendLine($"/// {line}");
            return this;
        }

        public CodeBlockNested AppendLines(String prefix, IEnumerable<String> lines)
        {
            foreach (String line in lines)
                this.AppendLine($"{prefix}{line}");
            return this;
        }

        public CodeBlockNested Example(params String[] text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            this.AppendLine("/// <example>");
            foreach (String line in text)
                this.AppendLine($"/// {line}");
            this.AppendLine("/// </example>");
            return this;
        }

        public CodeBlockNested OpenBrace([CallerFilePath] String filePath = "",
            [CallerLineNumber] Int32 lineNumber = 0)
        {
            this.AppendCode("{", filePath, lineNumber);
            this.Indent();
            return this;
        }

        public CodeBlockNested CloseBrace(String term = "",
            [CallerFilePath] String filePath = "",
            [CallerLineNumber] Int32 lineNumber = 0)
        {
            this.Unindent();
            this.AppendCode($"}}{term}", filePath, lineNumber);
            return this;
        }

        public CodeBlockNested InsertBlock(CodeBlockNested block,
            String blockName = "")
        {
            if (String.IsNullOrEmpty(blockName))
            {
                block.StartLine = "";
                block.EndLine = "";
            }
            else
            {
                block.StartLine = $"{this.MarginString}{this.owner.BlockStart} {blockName}{this.owner.BlockStartTerm}";
                block.EndLine = $"{this.MarginString}{this.owner.BlockEnd} {blockName}{this.owner.BlockEndTerm}";
            }
            this.AppendBlock(block);
            return this;
        }

        /// <summary>
        /// Create a new nested code block and return it.
        /// Calling EndBlock will return the current block.
        /// </summary>
        public CodeBlockNested StartBlock(String blockName = "")
        {
            this.owner.BlockStack.Push(this);
            return this.AppendBlock(blockName);
        }

        /// <summary>
        /// Returns the codeblock pushed in CodeBlockStart
        /// </summary>
        public CodeBlockNested EndBlock()
        {
            return this.owner.BlockStack.Pop();
        }

        public CodeBlockNested DefineBlock(out CodeBlockNested block,
            String blockName = "")
        {
            block = this.AppendBlock(blockName);
            return this;
        }

        public CodeBlockNested BlankLine([CallerFilePath] String filePath = "",
            [CallerLineNumber] Int32 lineNumber = 0)
        {
            this.AppendCode(string.Empty, filePath, lineNumber);
            return this;
        }

        public CodeBlockNested Call(CodeCallback code,
            [CallerFilePath] String filePath = "",
            [CallerLineNumber] Int32 lineNumber = 0)
        {
            //if (CodeEditor.DebugFlag)
            //    this.AppendCode($"//If: {filePath} {lineNumber}");
            code(this);
            return this;
        }

        public CodeBlockNested If(bool value,
            CodeCallback ifCode,
            [CallerFilePath] String filePath = "",
            [CallerLineNumber] Int32 lineNumber = 0)
        {
            if (value)
            {
                //if (CodeEditor.DebugFlag)
                //    this.AppendCode($"//If: {filePath} {lineNumber}");
                ifCode(this);
            }
            return this;
        }

        public CodeBlockNested IfElse(bool value,
            CodeCallback ifCode,
            CodeCallback elseCode,
            [CallerFilePath] String filePath = "",
            [CallerLineNumber] Int32 lineNumber = 0)
        {
            if (value)
            {
                //if (CodeEditor.DebugFlag)
                //    this.AppendCode($"//If: {filePath} {lineNumber}");
                ifCode(this);
            }
            else
            {
                //if (CodeEditor.DebugFlag)
                //    this.AppendCode($"//Else: {filePath} {lineNumber}");
                elseCode(this);
            }
            return this;
        }

        public CodeBlockNested AppendCode(String codeLine,
            [CallerFilePath] String filePath = "",
            [CallerLineNumber] Int32 lineNumber = 0)
        {
            String line;
            String fileName = Path.GetFileName(filePath);
            if (CodeEditor.DebugFlag)
                line = this.ProcessLine($"{this.MarginString}{codeLine}%col:{CommentCol}%// {fileName}:{lineNumber}");
            else
                line = this.ProcessLine($"{this.MarginString}{codeLine}");

            this.AppendRaw(line);
            return this;
        }

        public CodeBlockNested AppendXml(XmlElement element, String commentChars = "//")
        {
            if (element is null)
                throw new ArgumentNullException(nameof(element));

            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace,
                ConformanceLevel = ConformanceLevel.Auto
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                element.WriteContentTo(writer);
            }
            foreach (String line in sb.ToString().ToLines())
                this.AppendRaw($"{commentChars} {line}");

            return this;
        }

        void ProcessUserMacro(StringBuilder sb,
            String name,
            List<String> values)
        {
            if (this.owner.TryGetUserMacro(name, out Object? macroValue) == false)
            {
                OutputRawMacro(sb, name, values);
                return;
            }

            String prefix = "";
            String suffix = "";
            bool lbFlag = false;

            bool expandFlag = true;
            foreach (String value in values)
            {
                String[] valueArr = value.Split('=');
                switch (valueArr[0])
                {
                    case null:
                    case "":
                        break;

                    case "lb":
                        lbFlag = true;
                        break;

                    case "prefix":
                        prefix = valueArr[1];
                        break;

                    case "suffix":
                        suffix = valueArr[1];
                        break;

                    case "noexpand":
                        expandFlag = false;
                        break;

                    case "expand":
                        expandFlag = true;
                        break;

                    default:
                        throw new Exception($"Invalid user macro value {value}");
                }
            }

            void Append(String s)
            {
                String fullValue = $"{prefix}{s}{suffix}";

                if (lbFlag)
                    sb.AppendLine("");
                if (expandFlag)
                    fullValue = this.ProcessLine(fullValue);
                sb.Append(fullValue);
            }

            switch (macroValue)
            {
                case String macroStringValue:
                    Append(macroStringValue);
                    break;

                case IEnumerable<String> macroStringValues:
                    foreach (String macroStringValue in macroStringValues)
                        Append(macroStringValue);
                    break;

                default:
                    throw new Exception($"Invalid macro type {macroValue?.GetType().Name}");
            }
        }

        void OutputRawMacro(StringBuilder sb,
            String name,
            List<String> values)
        {
            sb.Append($"%{name}");
            foreach (String value in values)
                sb.Append($":{value}");
            sb.Append($"%");
        }

        void ProcessSystemMacro(StringBuilder sb,
        String name,
        List<String> values)
        {
            switch (name.ToLower().Trim())
            {
                case "col":
                    if (values.Count != 1)
                        throw new Exception($"Invalid col value count (expected one, got {values.Count})");
                    if (int.TryParse(values[0], out Int32 col) == false)
                        throw new Exception($"Invalid col macro value {values[0]}");
                    while (sb.Length < col)
                        sb.Append(" ");
                    return;

                default:
                    OutputRawMacro(sb, name, values);
                    return;
            }
        }

        void ProcessMacro(StringBuilder sb,
            String line,
            ref Int32 index)
        {
            StringBuilder current = new StringBuilder();
            String name = String.Empty;
            List<String> values = new List<string>();

            Boolean valueFlag = false;
            bool done = false;

            void AddToken()
            {
                if (valueFlag == false)
                {
                    name = current.ToString();
                    valueFlag = true;
                }
                else
                    values.Add(current.ToString());
                current.Clear();
            }

            while (done == false)
            {
                Char c = CodeBlockNested.GetChar(line, ref index);
                switch (c)
                {
                    case '\\':
                        c = CodeBlockNested.GetChar(line, ref index);
                        if (c != '%')
                            sb.Append('\\');
                        sb.Append(c);
                        break;

                    case '%':
                        AddToken();
                        if (Char.IsLower(name[0]))
                            ProcessSystemMacro(sb, name, values);
                        else
                            ProcessUserMacro(sb, name, values);
                        done = true;
                        break;

                    case ':':
                        AddToken();
                        break;

                    default:
                        current.Append(c);
                        break;
                }
            }
        }

        static Char GetChar(String line, ref Int32 index)
        {
            if (line is null)
                throw new ArgumentNullException(nameof(line));

            if (index >= line.Length)
                throw new Exception("Unexpected end of string!");
            return line[index++];
        }

        String ProcessLine(String line)
        {
            StringBuilder sb = new StringBuilder();
            Int32 index = 0;
            Boolean quoteFlag = false;

            while (index < line.Length)
            {
                Char c = line[index++];
                switch (c)
                {
                    case '\"':
                        if (this.owner.IgnoreMacrosInQuotedStrings == true)
                            quoteFlag = !quoteFlag;
                        sb.Append(c);
                        break;

                    case '\\':
                        c = CodeBlockNested.GetChar(line, ref index);
                        if (c != '%')
                            sb.Append('\\');
                        sb.Append(c);
                        break;

                    case '%':
                        if ((this.macroInhibit > 0) || (quoteFlag == true))
                            sb.Append(c);
                        else
                            this.ProcessMacro(sb, line, ref index);
                        break;

                    default:
                        sb.Append(c);
                        break;

                }
            }

            return sb.ToString();
        }
    }
}
