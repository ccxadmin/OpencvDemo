using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace visionForm
{
    /*
     * 自动标定流程及格式
     * 
     */

    public  class AutoCalibrationFormat
    {
        public  static  class CalibrateOfNightPoint
        {
            /* Motion to vision */
            //九点标定准备启动
            public const string Ready_To_CalibrateOfNightPoint = "NP,S";
            //标定第一点，发送机械手当前坐标xy
            public const string First_Step_CalibrateOfNightPoint = "NP,1";
            //标定第二点，发送机械手当前坐标xy
            public const string Second_Step_CalibrateOfNightPoint = "NP,2";
            //标定第三点，发送机械手当前坐标xy
            public const string Third_Step_CalibrateOfNightPoint = "NP,3";
            //标定第四点，发送机械手当前坐标xy
            public const string Four_Step_CalibrateOfNightPoint = "NP,4";
            //标定第五点，发送机械手当前坐标xy
            public const string Five_Step_CalibrateOfNightPoint = "NP,5";
            //标定第六点，发送机械手当前坐标xy
            public const string Six_Step_CalibrateOfNightPoint = "NP,6";
            //标定第七点，发送机械手当前坐标xy
            public const string Seven_Step_CalibrateOfNightPoint = "NP,7";
            //标定第八点，发送机械手当前坐标xy
            public const string Eight_Step_CalibrateOfNightPoint = "NP,8";
            //标定第九点，发送机械手当前坐标xy
            public const string Night_Step_CalibrateOfNightPoint = "NP,9";
            //九点标定结束
            public const string Finish_CalibrateOfNightPoint = "NP,E";

            /* Vision to motion*/
            //九点标定准备启动,反馈结果信号
            public const string Feedback_Ready_To_CalibrateOfNightPoint = "NP,S,OK";
            //标定第一点，模板匹配OK
            public const string FeedbackOK_First_Step_CalibrateOfNightPoint = "NP,1,OK";
            //标定第一点，模板匹配NG
            public const string FeedbackNG_First_Step_CalibrateOfNightPoint = "NP,1,NG";
            //标定第二点，模板匹配OK
            public const string FeedbackOK_Second_Step_CalibrateOfNightPoint = "NP,2,OK";
            //标定第二点，模板匹配NG
            public const string FeedbackNG_Second_Step_CalibrateOfNightPoint = "NP,2,NG";
            //标定第三点，模板匹配OK
            public const string FeedbackOK_Third_Step_CalibrateOfNightPoint = "NP,3,OK";
            //标定第三点，模板匹配NG
            public const string FeedbackNG_Third_Step_CalibrateOfNightPoint = "NP,3,NG";
            //标定第四点，模板匹配OK
            public const string FeedbackOK_Four_Step_CalibrateOfNightPoint = "NP,4,OK";
            //标定第四点，模板匹配NG
            public const string FeedbackNG_Four_Step_CalibrateOfNightPoint = "NP,4,NG";
            //标定第五点，模板匹配OK
            public const string FeedbackOK_Five_Step_CalibrateOfNightPoint = "NP,5,OK";
            //标定第五点，模板匹配NG
            public const string FeedbackNG_Five_Step_CalibrateOfNightPoint = "NP,5,NG";
            //标定第六点，模板匹配OK
            public const string FeedbackOK_Six_Step_CalibrateOfNightPoint = "NP,6,OK";
            //标定第六点，模板匹配NG
            public const string FeedbackNG_Six_Step_CalibrateOfNightPoint = "NP,6,NG";
            //标定第七点，模板匹配OK
            public const string FeedbackOK_Seven_Step_CalibrateOfNightPoint = "NP,7,OK";
            //标定第七点，模板匹配NG
            public const string FeedbackNG_Seven_Step_CalibrateOfNightPoint = "NP,7,NG";
            //标定第八点，模板匹配OK
            public const string FeedbackOK_Eight_Step_CalibrateOfNightPoint = "NP,8,OK";
            //标定第八点，模板匹配NG
            public const string FeedbackNG_Eight_Step_CalibrateOfNightPoint = "NP,8,NG";
            //标定第九点，模板匹配OK
            public const string FeedbackOK_Night_Step_CalibrateOfNightPoint = "NP,9,OK";
            //标定第九点，模板匹配NG
            public const string FeedbackNG_Night_Step_CalibrateOfNightPoint = "NP,9,NG";
            //九点标定流程结束，标定OK
            public const string FeedbackOK_Finish_CalibrateOfNightPoint = "NP,E,OK";
            //九点标定流程结束，标定NG
            public const string FeedbackNG_Finish_CalibrateOfNightPoint = "NP,E,NG";

        }

        public static class CalibrateOfRorationPoint
        { 
            /* Motion to vision */
            //旋转中心标定准备好
            public const string Read_To_CalibrateOfRorationPoint = "C,S";
            //标定第一点，发送机械手当前坐标xy
            public const string First_Step_CalibrateOfRorationPoint = "C,1";
            //标定第二点，发送机械手当前坐标xy
            public const string Second_Step_CalibrateOfRorationPoint = "C,2";
            //标定第三点，发送机械手当前坐标xy
            public const string Third_Step_CalibrateOfRorationPoint = "C,3";
            //标定第四点，发送机械手当前坐标xy
            public const string Four_Step_CalibrateOfRorationPoint = "C,4";
            //标定第五点，发送机械手当前坐标xy
            public const string Five_Step_CalibrateOfRorationPoint = "C,5";
            //旋转标定流程结束
            public const string Finish_CalibrateOfRorationPoint = "C,E";


            /* Vision to motion*/
            //旋转标定准备启动,反馈结果信号
            public const string Feedback_Ready_To_CalibrateOfRorationPoint = "C,S,OK";
            //标定第一点，模板匹配OK
            public const string FeedbackOK_First_Step_CalibrateOfRorationPoint = "C,1,OK";
            //标定第一点，模板匹配NG
            public const string FeedbackNG_First_Step_CalibrateOfRorationPoint = "C,1,NG";
            //标定第二点，模板匹配OK
            public const string FeedbackOK_Second_Step_CalibrateOfRorationPoint = "C,2,OK";
            //标定第二点，模板匹配NG
            public const string FeedbackNG_Second_Step_CalibrateOfRorationPoint = "C,2,NG";
            //标定第三点，模板匹配OK
            public const string FeedbackOK_Third_Step_CalibrateOfRorationPoint = "C,3,OK";
            //标定第三点，模板匹配NG
            public const string FeedbackNG_Third_Step_CalibrateOfRorationPoint = "C,3,NG";
            //标定第四点，模板匹配OK
            public const string FeedbackOK_Four_Step_CalibrateOfRorationPoint = "C,4,OK";
            //标定第四点，模板匹配NG
            public const string FeedbackNG_Four_Step_CalibrateOfRorationPoint = "C,4,NG";
            //标定第五点，模板匹配OK
            public const string FeedbackOK_Five_Step_CalibrateOfRorationPoint = "C,5,OK";
            //标定第五点，模板匹配NG
            public const string FeedbackNG_Five_Step_CalibrateOfRorationPoint = "C,5,NG";
            //旋转标定流程结束，标定OK
            public const string FeedbackOK_Finish_CalibrateOfRorationPoint = "C,E,OK";
            //旋转标定流程结束，标定NG
            public const string FeedbackNG_Finish_CalibrateOfRorationPoint = "C,E,NG";
        }
    }
}
