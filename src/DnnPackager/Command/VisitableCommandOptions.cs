using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DnnPackager.Command
{
    public abstract class VisitableCommandOptions : IVisitableOptions
    {
        public abstract void Accept(ICommandVisitor visitor);
    }
}
