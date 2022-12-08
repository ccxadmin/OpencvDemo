using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParamDataLib;
using OpenCvSharp;

namespace FuncToolLib
{
   public interface IRunTool
    {
        T GetTool<T>() where T : class, IRunTool;

        Result Run<T>(Mat inputImg, T obj) where T :class, IParmaData;

    }
}
