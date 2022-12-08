using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ParamDataLib
{
   public interface IParmaData
    {
        T GetData<T>() where T : class, IParmaData;

      
      
    }
}
