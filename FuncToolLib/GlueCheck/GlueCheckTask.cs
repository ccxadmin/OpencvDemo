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
using FuncToolLib;
using FuncToolLib.Morphology;
using FuncToolLib.Filter;
using FuncToolLib.TwoImagesOperate;
using FuncToolLib.Enhancement;
using FuncToolLib.Location;

namespace FuncToolLib.GlueCheck
{
    public partial class GlueCheckTask
    {
        public GlueCheckTask()
        {

        }
      
        /// <summary>
        /// ��ˮ���
        /// </summary>
        /// <param name="srcImg"></param>
        /// <param name="glueCheckParam"></param>
        /// <returns></returns>
        public static ResultOfGlueCheck action(Mat srcImg, GlueCheckParam glueCheckParam)
        {
            ResultOfGlueCheck resultOfGlueCheck = new ResultOfGlueCheck();
            resultOfGlueCheck.region = new List<CVPoint[]>();
            if (glueCheckParam.detectROI.Count <= 0|| srcImg.Empty()|| srcImg.Width<=0)
            {
                resultOfGlueCheck.runFlag = false;
                resultOfGlueCheck.errInfo += "�������Ϊ�գ�";
                return resultOfGlueCheck;
            }
            Mat RoiMat=  MatExtension.Reducue_Mask_Mat(srcImg,glueCheckParam.detectROI.ToArray());
      
          
            try
            {
                //��ʴ
                Mat ErodeMat = Morphological_Proces.MorphologyEx(RoiMat, MorphShapes.Rect, new Size(10, 10),
                        MorphTypes.Erode);
                //��ֵ
                Mat MeanMat = ImageFilter.MeanImage(ErodeMat, new Size(5, 5));
                //ͼ�����
                Mat mulMat = ImagesArithmetic.MulOfTwoImages(MeanMat, MeanMat);
                //��������
                Mat scaleMat = ImageEmphize.scale_image_range(mulMat, glueCheckParam.scaleGrayMin, glueCheckParam.scaleGrayMax);

                //��·��������Ͻ�����ˮ�����ȵ�
                Blob3Result blob3Result= Blob3Tool.Run(scaleMat, glueCheckParam.MinGrayOfThreshold,
                    glueCheckParam.MinAreaOfGlue, glueCheckParam.MaxAreaOfGlue,
                     ParamDataLib.Location.EumWhiteOrBlack.Black);

                resultOfGlueCheck.n_count = blob3Result.positionData.Count;
                resultOfGlueCheck.region = blob3Result.xlds;
                resultOfGlueCheck.runFlag = true;

            }
            catch (Exception er)
            {
               
                resultOfGlueCheck.runFlag = false;
                resultOfGlueCheck.errInfo = er.Message;
                return resultOfGlueCheck;
            }

         return resultOfGlueCheck;

        }

    }

    /// <summary>
    /// ��ˮ������
    /// </summary>
    [Serializable]
    public class GlueCheckParam
    {
        /// <summary>
        /// �������
        /// </summary>
        public List<CVPoint> detectROI;
        /// <summary>
        /// ��ֵ����ֵ����
        /// </summary>
        public byte MinGrayOfThreshold { get; set; } = 120;
        /// <summary>
        /// ��ֵ����ֵ����
        /// </summary>
        public byte MaxGrayOfThreshold { get; set; } = 255;
        //�Ҷ���������
        public byte scaleGrayMin { get; set; } = 80;
        /// <summary>
        /// �Ҷ���������
        /// </summary>
        public byte scaleGrayMax { get; set; } = 180;
        /// <summary>
        /// ��ˮȱ�ݲ�λ��С���
        /// </summary>
        public int MinAreaOfGlue { get; set; } = 10000;
        /// <summary>
        /// ��ˮȱ�ݲ�λ������
        /// </summary>
        public int MaxAreaOfGlue { get; set; } = 99999;
    }

    public struct ResultOfGlueCheck
    {
        public bool runFlag;
        public List<CVPoint[]> region;
        public string errInfo;
        public int n_count;
    }

}

