//
// File generated by HDevelop for HALCON/.NET (C#) Version 17.12
//
using System;
using OpenCvSharp;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;
using System.Collections.Generic;
using FuncToolLib.Filter;
using FuncToolLib.Location;
using VisionShowLib;

namespace FuncToolLib.GlueCheck
{
    public partial class RegionSet
    {
        public RegionSet()
        {
          
        }

        // Main procedure 
        public static ResultOfRegionExtra action(Mat srcImg, RegionParam regionParam)
        {
            ResultOfRegionExtra resultOfRegionExtra = new ResultOfRegionExtra();
            // Local iconic variables 
            resultOfRegionExtra.region = new List<CVPoint[]>();

            try
            {
                //区域提取
               
                Mat MeanMat= ImageFilter.MeanImage(srcImg,new Size(regionParam.MaskWidth, regionParam.MaskHeight));

                Blob3Result blob3Result= Blob3Tool.Run(MeanMat, regionParam.MinGrayOfThreshold,
                    regionParam.MinAreaOfGlue, regionParam.MaxAreaOfGlue, ParamDataLib.Location.EumWhiteOrBlack.White);

                resultOfRegionExtra.region = blob3Result.xlds;
                resultOfRegionExtra.runFlag = true;
             
            }
            catch (Exception er)
            {
               
                resultOfRegionExtra.runFlag = false;
                resultOfRegionExtra.errInfo = er.Message;
                resultOfRegionExtra.region = null;
                return resultOfRegionExtra;
            }
                            
            return resultOfRegionExtra;
        }
    }

    /// <summary>
    /// 胶水区域参数
    /// </summary>
    [Serializable]
    public class RegionParam
    {
        public RegionParam()
        {

        }
        /// <summary>
        ///图像均值掩膜宽度
        /// </summary>
        public int MaskWidth { get; set; } = 5;
        /// <summary>
        /// 图像均值掩膜高度
        /// </summary>
        public int MaskHeight { get; set; } = 5;

        /// <summary>
        /// 二值化阈值下限
        /// </summary>
        public byte MinGrayOfThreshold { get; set; } = 120;
        /// <summary>
        /// 二值化阈值上限
        /// </summary>
        public byte MaxGrayOfThreshold { get; set; } = 255;
        /// <summary>
        /// 胶水检测区域最小面积
        /// </summary>
        public int MinAreaOfGlue { get; set; } = 10000;
        /// <summary>
        /// 胶水检测区域最大面积
        /// </summary>
        public int MaxAreaOfGlue { get; set; } = 99999;
        /// <summary>
        /// 参数更新
        /// </summary>
        /// <param name="_MaskWidth"></param>
        /// <param name="_MaskHeight"></param>
        /// <param name="_MinGrayOfThreshold"></param>
        /// <param name="_MaxGrayOfThreshold"></param>
        /// <param name="_MinAreaOfGlue"></param>
        /// <param name="_MaxAreaOfGlue"></param>
        public void  updateParam(int _MaskWidth, int _MaskHeight,
                   byte _MinGrayOfThreshold, byte _MaxGrayOfThreshold,
                   int _MinAreaOfGlue, int _MaxAreaOfGlue)
        {
            MaskWidth = _MaskWidth;
            MaskHeight = _MaskHeight;
            MinGrayOfThreshold = _MinGrayOfThreshold;
            MaxGrayOfThreshold = _MaxGrayOfThreshold;
            MinAreaOfGlue = _MinAreaOfGlue;
            MaxAreaOfGlue = _MaxAreaOfGlue;
        }
        public RegionParam(int _MaskWidth, int _MaskHeight,
                   byte _MinGrayOfThreshold, byte _MaxGrayOfThreshold,
                   int _MinAreaOfGlue, int _MaxAreaOfGlue)
        {
            MaskWidth = _MaskWidth;
            MaskHeight = _MaskHeight;
            MinGrayOfThreshold = _MinGrayOfThreshold;
            MaxGrayOfThreshold = _MaxGrayOfThreshold;
            MinAreaOfGlue = _MinAreaOfGlue;
            MaxAreaOfGlue = _MaxAreaOfGlue;
        }
    }

    /// <summary>
    /// 区域参数集合
    /// </summary>
    [Serializable]
    public  class RegionParamArray
    {
        public RegionParamArray()
        {
            regionParam = new RegionParam();
            DetectionROI = new PolygonF();
            isUse = true;
        }
        /// <summary>
        /// 区域提取参数
        /// </summary>
        public RegionParam regionParam { get; set; }
        /// <summary>
        /// 当前区域名称
        /// </summary>
        public string currRegionName { get; set; }
        /// <summary>
        /// 是否启用偏移补正
        /// </summary>
        public bool isUsePosiCorrect { get; set; }
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool isUse { get; set; }
        /// <summary>
        /// 胶水检测区域
        /// </summary>
        public PolygonF DetectionROI;

        /// <summary>
        /// 区域设定基于自动提取
        /// </summary>
        public bool regionSetBaseOnAutoExtract { get; set; } = false;
    }

    public struct  ResultOfRegionExtra
    {
        public bool runFlag;
        public List<CVPoint[]> region;
        public string errInfo;
    }
}