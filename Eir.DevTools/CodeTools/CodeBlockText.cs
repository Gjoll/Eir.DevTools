﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Eir.DevTools
{
    public class CodeBlockText : CodeBlock
    {
        /// <summary>
        /// Strings that make up code file.
        /// </summary>
        public List<String> Code {get; }  = new List<String>();

        /// <summary>
        /// Return true if empty.
        /// </summary>
        public override bool Empty => Code.Count == 0 ? true : false;

        public CodeBlockText(CodeEditor owner) : base(owner)
        {
        }

        /// <summary>
        /// Clear all content.
        /// </summary>
        public override void Clear() => this.Code.Clear();

        /// <summary>
        /// Return array of all lines
        /// </summary>
        public override String[] Lines() => this.Code.ToArray();

        /// <summary>
        /// Return array of all lines
        /// </summary>
        public override String[] AllLines() => this.Code.ToArray();

        /// <summary>
        /// Return array of all lines
        /// </summary>
        public override String Text()
        {
            StringBuilder sb = new StringBuilder();
            foreach (String line in this.Code)
                sb.AppendLine(line);
            return sb.ToString();
        }

        /// <summary>
        /// Return array of all lines
        /// </summary>
        public override String AllText()
        {
            StringBuilder sb = new StringBuilder();
            foreach (String line in this.Code)
                sb.AppendLine(line);
            return sb.ToString();
        }

        public void Set(String[] lines)
        {
            this.Clear();
            this.Code.AddRange(lines);
        }
    }
}
