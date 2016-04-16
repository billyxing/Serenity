﻿using System;
using System.Runtime.CompilerServices;

namespace Serenity
{
    public class ReadOnlyAttribute : Attribute
    {
        public ReadOnlyAttribute(bool readOnly = true)
        {
            this.Value = readOnly;
        }

        [IntrinsicProperty]
        public bool Value
        {
            get;
            private set;
        }
    }
}