﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Eir.DevTools
{
    /// <summary>
    /// Xml extensions
    /// </summary>
    public static class XmlExtensions
    {
        /// <summary>
        /// Access named attribute.
        /// </summary>
        public static XmlAttribute? Attribute(this XmlElement element, String name)
        {
            if (element is null)
                throw new ArgumentNullException(nameof(element));
            return element.Attributes[name];
        }

        /// <summary>
        /// Get all xml child nodes with the indicated name.
        /// </summary>
        public static IEnumerable<XmlElement> Children(this XmlElement element, String name)
        {
            if (element is null)
                throw new ArgumentNullException(nameof(element));

            foreach (XmlElement e in element.ChildNodes)
                if (e.Name == name)
                    yield return e;
        }

        /// <summary>
        /// Access required attribute. Throw exception if attribute missing.
        /// </summary>
        public static XmlAttribute RequiredAttribute(this XmlElement element, String name)
        {
            if (element is null)
                throw new ArgumentNullException(nameof(element));

            XmlAttribute? retVal = element.Attributes[name];
            if (retVal == null)
                throw new Exception($"Missing req   uired attribute '{name}'");
            return retVal;
        }
    }
}
