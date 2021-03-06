﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Eir.DevTools
{
    public abstract class CodeBlock
    {
        public CodeEditor owner { get; }
        public abstract bool Empty { get; }

        public CodeBlock(CodeEditor owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Clear all content.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Return array of all lines
        /// </summary>
        public abstract String[] Lines();

        /// <summary>
        /// Return array of all lines including header and footer lines
        /// </summary>
        public abstract String[] AllLines();

        /// <summary>
        /// Return string of all lines
        /// </summary>
        public abstract String Text();

        /// <summary>
        /// Return string of all lines including header and footer lines
        /// </summary>
        public abstract String AllText();
    }
}
