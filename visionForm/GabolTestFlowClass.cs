using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
namespace visionForm
{
   public  class GabolTestFlowClass
    {
        //绝对工具计算流程
        public static Dictionary<string, UserToolControl> gabolTestFlowDir =
                   new Dictionary<string, UserToolControl>();

        //相对工具计算流程
        public static Dictionary<string, UserToolControl2> gabolTestFlowDir2 =
                   new Dictionary<string, UserToolControl2>();


        //绝对工具计算流程
        public static void RemoveControl(UserToolControl uc)
        {
            if (!gabolTestFlowDir.ContainsKey(uc.TitleName))
                return;
            else
                gabolTestFlowDir.Remove(uc.TitleName);
        }

        public static void NewAddControl(UserToolControl uc)
        {
            if (gabolTestFlowDir.ContainsKey(uc.TitleName))
            {
                gabolTestFlowDir.Remove(uc.TitleName);
            }         
             gabolTestFlowDir.Add(uc.TitleName, uc);
        }

        public static UserToolControl GetControl(string name)
        {
            UserToolControl temcontrol = null;
            if (!gabolTestFlowDir.ContainsKey(name))
                return null;
            else
                gabolTestFlowDir.TryGetValue(name,out temcontrol);
            return temcontrol;


        }


        //相对工具计算流程
        public static void RemoveControl(UserToolControl2 uc)
        {
            if (!gabolTestFlowDir2.ContainsKey(uc.TitleName))
                return;
            else
                gabolTestFlowDir2.Remove(uc.TitleName);
        }

        public static void NewAddControl(UserToolControl2 uc)
        {
            if (gabolTestFlowDir2.ContainsKey(uc.TitleName))
            {
                gabolTestFlowDir2.Remove(uc.TitleName);
            }
            gabolTestFlowDir2.Add(uc.TitleName, uc);
        }

        public static UserToolControl2 GetControl3(string name)
        {
            UserToolControl2 temcontrol = null;
            if (!gabolTestFlowDir2.ContainsKey(name))
                return null;
            else
                gabolTestFlowDir2.TryGetValue(name, out temcontrol);
            return temcontrol;


        }
    

    }
}
