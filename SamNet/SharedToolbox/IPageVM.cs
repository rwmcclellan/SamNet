using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedToolbox
{
    public interface IPageVM
    {
        string Name { get; }
        void IsInFocus(int caller, object? payload);
    }
}
