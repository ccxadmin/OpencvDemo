using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using HalconDotNet;
using System.IO;

namespace FunctionLib.CodeReader
{
    public  class FindDataCode2d
    {
        //条码训练文件保存路径
        private static string trainPath = AppDomain.CurrentDomain.BaseDirectory + "CodeModle.dcm";
        public static string TrainPath
        {
            get { return trainPath; }
            set { trainPath = value; }
        }

        public static HTuple dataCodeHandle = null;
       // public static HTuple DataCodeHandle = null;
        /// <summary>
        /// 码文模型创建
        /// </summary>
        /// <param name="CodeType"></param>
        /// <returns></returns>
        public static HTuple create_data_code_2d_model(HTuple CodeType)
        {
            HTuple  GenParamNames = null, genParamValues = null;

            HOperatorSet.CreateDataCode2dModel(CodeType, new HTuple(), new HTuple(), out dataCodeHandle);
            //HOperatorSet.SetDataCode2dParam(DataCodeHandle, "polarity", Enum.GetName(typeof(Enumpolarity), polarity));
            //HOperatorSet.SetDataCode2dParam(DataCodeHandle, "timeout", readTimeOut);
            //HOperatorSet.SetDataCode2dParam(DataCodeHandle, "small_modules_robustness", "high");
           // HOperatorSet.SetDataCode2dParam(DataCodeHandle, "contrast_min", 10);

            HOperatorSet.QueryDataCode2dParams(dataCodeHandle, "get_model_params", out GenParamNames);

            HOperatorSet.GetDataCode2dParam(dataCodeHandle, GenParamNames, out genParamValues);
            return dataCodeHandle;
        }

        /// <summary>
        ///  码文模型创建
        /// </summary>
        /// <param name="CodeType"></param>
        /// <param name="DataMoudulParma"></param>
        /// <returns></returns>
        public static HTuple create_data_code_2d_model(HTuple CodeType, EumDataMoudulParma DataMoudulParma)
        {
            
            string parmaValue= Enum.GetName(typeof(EumDataMoudulParma), DataMoudulParma);
            if (parmaValue.Equals("None"))
                HOperatorSet.CreateDataCode2dModel(CodeType, new HTuple(), new HTuple(), out dataCodeHandle);
            else
                HOperatorSet.CreateDataCode2dModel(CodeType, "default_parameters", parmaValue, out dataCodeHandle);
            //HOperatorSet.SetDataCode2dParam(DataCodeHandle, "polarity", Enum.GetName(typeof(Enumpolarity), polarity));
            //HOperatorSet.SetDataCode2dParam(DataCodeHandle, "timeout", 5000);
            //HOperatorSet.SetDataCode2dParam(DataCodeHandle, "small_modules_robustness", "high");
            // HOperatorSet.SetDataCode2dParam(DataCodeHandle, "contrast_min", 10);

            //HOperatorSet.QueryDataCode2dParams(DataCodeHandle, "get_model_params", out GenParamNames);

            //HOperatorSet.GetDataCode2dParam(DataCodeHandle, GenParamNames, out genParamValues);
            return dataCodeHandle;
        }
        /// <summary>
        /// 条码训练
        /// </summary>
        /// <param name="img"></param>
        /// <param name="winHandle"></param>
        /// <param name="msg"></param>
        public static void Train_code_model(HObject img, HTuple winHandle,  int readTimeOut, bool IsTrain, ref string msg)
        {
            try
            {
                HTuple ResultHandles = null, DecodedDataStrings = null;
                HObject ho_SymbolXLDs;
                HOperatorSet.GenEmptyObj(out ho_SymbolXLDs);
                ho_SymbolXLDs.Dispose();
                if (IsTrain)   //启用训练
                {
                    HOperatorSet.FindDataCode2d(img, out ho_SymbolXLDs, dataCodeHandle, "train", "all", out  ResultHandles,
                                  out DecodedDataStrings);
                    HOperatorSet.SetDataCode2dParam(dataCodeHandle, "timeout", readTimeOut);
                }
                else
                {
                    HOperatorSet.SetDataCode2dParam(dataCodeHandle, "timeout", readTimeOut);
                    HOperatorSet.FindDataCode2d(img, out ho_SymbolXLDs, dataCodeHandle, new HTuple(), new HTuple(), out  ResultHandles,
                                 out DecodedDataStrings);
                }
                            
                HOperatorSet.DispXld(ho_SymbolXLDs, winHandle);

                HTuple TitleMessage = "Train on a image";
                display_found_data_codes(ho_SymbolXLDs, winHandle, DecodedDataStrings, TitleMessage, new HTuple(), "forest green", "black");
               
              //  HOperatorSet.WriteDataCode2dModel(DataCodeHandle, trainPath);
            }
            catch (HalconException HDevExpDefaultException)
            {
                msg = HDevExpDefaultException.Message;
            }
          
        }

        /// <summary>
        /// 码文模型文件加载
        /// </summary>
        /// <param name="readTimeOut"></param>
        /// <param name="polarity"></param>
        public static void LoadCodeFile()
        {

            HOperatorSet.ReadDataCode2dModel(trainPath, out dataCodeHandle);
         
        }

        /// <summary>
        /// 码文模型文件保存
        /// </summary>
        public static void SaveCodeFile()
        {
            HOperatorSet.WriteDataCode2dModel(dataCodeHandle, trainPath);
        
        }
        /// <summary>
        /// 条码读取
        /// </summary>
        /// <param name="img"></param>
        /// <param name="winHandle"></param>
        /// <returns></returns>
       public static string ReadCode(HObject img,HTuple winHandle)
       {
           if (dataCodeHandle == null)
           {
               return "码文模型句柄为空！";
           }
           HObject SymbolXLDs;
           HOperatorSet.GenEmptyObj(out SymbolXLDs);
           HTuple DecodedDataStrings=null;
           HTuple ResultHandles=null;
           HOperatorSet.FindDataCode2d(img, out SymbolXLDs, dataCodeHandle, new HTuple(), new HTuple(), out ResultHandles, out DecodedDataStrings);
           HOperatorSet.SetColor( winHandle, "green");
           HOperatorSet.DispObj(SymbolXLDs, winHandle);
           if (DecodedDataStrings.TupleLength() <= 0)
               return "TimeOut";
           return DecodedDataStrings.TupleSelect(0);
       }
        /// <summary>
        /// 句柄资源释放
        /// </summary>
        public static void clear_data_code_2d_model()
        {
            if (dataCodeHandle != null)
                HOperatorSet.ClearDataCode2dModel(dataCodeHandle);
        }

        public static void display_found_data_codes(HObject ho_SymbolXLDs, HTuple hv_WindowHandle, 
            HTuple hv_DecodedDataStrings, HTuple hv_TitleMessage, HTuple hv_ResultMessage, 
             HTuple hv_ColorDecodedStrings, HTuple hv_ColorResult)
  {




    // Local iconic variables 

    HObject ho_SymbolXLD=null;

    // Local control variables 

    HTuple hv_J = null, hv_Row = new HTuple();
    HTuple hv_Column = new HTuple(), hv_Row1 = new HTuple();
    HTuple hv_Column1 = new HTuple(), hv_Width = new HTuple();
    HTuple hv_Height = new HTuple(), hv_Ascent = new HTuple();
    HTuple hv_Descent = new HTuple(), hv_TWidth = new HTuple();
    HTuple hv_THeight = new HTuple(), hv_DecodedData = new HTuple();
    HTuple hv_DecodedDataSubstrings = new HTuple(), hv_TPosRow = new HTuple();
    HTuple hv_TPosColumn = new HTuple();
    HTuple   hv_DecodedDataStrings_COPY_INP_TMP = hv_DecodedDataStrings.Clone();

    // Initialize local and output iconic variables 
    HOperatorSet.GenEmptyObj(out ho_SymbolXLD);
    try
    {
      //This procedure displays the results of the search for
      //2d data codes. The data strings are displayed accordingly
      //to their length so that the whole string is visible.
      //If the data strings are too long only the first 50 chars
      //are displayed.
      //
      //Input parameters are the XLD contours of the decoded
      //data symbols, the decoded data strings, the windowhandle,
      //a title message, a result message, the color of the decoded
      //strings and the color of the result message.
      //
      //Display the result of the search for each found data code
      for (hv_J=0; (int)hv_J<=(int)((new HTuple(hv_DecodedDataStrings_COPY_INP_TMP.TupleLength()
          ))-1); hv_J = (int)hv_J + 1)
      {
        //
        //Display the XLD contour
        ho_SymbolXLD.Dispose();
        HOperatorSet.SelectObj(ho_SymbolXLDs, out ho_SymbolXLD, hv_J+1);
        HOperatorSet.GetContourXld(ho_SymbolXLD, out hv_Row, out hv_Column);
        if (HDevWindowStack.IsOpen())
        {
          HOperatorSet.DispObj(ho_SymbolXLD, HDevWindowStack.GetActive());
        }
        //
        //Display messages
        //------------------
        //Determine the length of the dislayed decoded data string
        HOperatorSet.GetWindowExtents(hv_WindowHandle, out hv_Row1, out hv_Column1, 
            out hv_Width, out hv_Height);
        HOperatorSet.GetStringExtents(hv_WindowHandle, hv_DecodedDataStrings_COPY_INP_TMP.TupleSelect(
            hv_J), out hv_Ascent, out hv_Descent, out hv_TWidth, out hv_THeight);
        if ((int)(new HTuple(hv_TWidth.TupleGreater(hv_Width))) != 0)
        {
          if (hv_DecodedDataStrings_COPY_INP_TMP == null)
            hv_DecodedDataStrings_COPY_INP_TMP = new HTuple();
          hv_DecodedDataStrings_COPY_INP_TMP[hv_J] = (((hv_DecodedDataStrings_COPY_INP_TMP.TupleSelect(
              hv_J))).TupleSubstr(0,50))+"...";
          HOperatorSet.GetStringExtents(hv_WindowHandle, hv_DecodedDataStrings_COPY_INP_TMP.TupleSelect(
              hv_J), out hv_Ascent, out hv_Descent, out hv_TWidth, out hv_THeight);
        }
        //
        //Split the decoded string in new lines for better readability
        HOperatorSet.TupleRegexpReplace(hv_DecodedDataStrings_COPY_INP_TMP.TupleSelect(
            hv_J), (new HTuple("[\\r\\f,^#;]")).TupleConcat("replace_all"), "\n", 
            out hv_DecodedData);
        HOperatorSet.TupleSplit(hv_DecodedData, "\n", out hv_DecodedDataSubstrings);
        //
        //Determine the position of the displayed decoded data string
        if ((int)((new HTuple(((hv_Row.TupleMax())).TupleGreater(420))).TupleAnd(
            new HTuple(((hv_Row.TupleMin())).TupleLess(40)))) != 0)
        {
          hv_TPosRow = (hv_Row.TupleMax())-30;
        }
        else if ((int)(new HTuple(((hv_Row.TupleMax())).TupleGreater(420))) != 0)
        {
          hv_TPosRow = (hv_Row.TupleMin())-20;
        }
        else if ((int)(new HTuple(((hv_Row.TupleMin())).TupleLess(100))) != 0)
        {
          hv_TPosRow = (hv_Row.TupleMax())-20;
        }
        else
        {
          hv_TPosRow = (hv_Row.TupleMax())-30;
        }
        hv_TPosColumn = (((((((((hv_Column.TupleMean())-(hv_TWidth/2))).TupleConcat(
            (hv_Width-32)-hv_TWidth))).TupleMin())).TupleConcat(12))).TupleMax();
        disp_message(hv_WindowHandle, hv_DecodedDataStrings_COPY_INP_TMP.TupleSelect(
            hv_J), "image", hv_TPosRow, hv_TPosColumn, hv_ColorDecodedStrings, "true");
      }
      //
      //Display the title message and result message
      disp_message(hv_WindowHandle, hv_TitleMessage, "window", 12, 12, "black", "true");
      disp_message(hv_WindowHandle, hv_ResultMessage, "window", 40, 12, hv_ColorResult, 
          "true");
      ho_SymbolXLD.Dispose();

      return;
    }
    catch (HalconException HDevExpDefaultException)
    {
      ho_SymbolXLD.Dispose();

      throw HDevExpDefaultException;
    }
  
    }

        public static void disp_message(HTuple hv_WindowHandle, HTuple hv_String, HTuple hv_CoordSystem, 
               HTuple hv_Row, HTuple hv_Column, HTuple hv_Color, HTuple hv_Box)
  {



      // Local iconic variables 

      // Local control variables 

      HTuple hv_Red = null, hv_Green = null, hv_Blue = null;
      HTuple hv_Row1Part = null, hv_Column1Part = null, hv_Row2Part = null;
      HTuple hv_Column2Part = null, hv_RowWin = null, hv_ColumnWin = null;
      HTuple hv_WidthWin = null, hv_HeightWin = null, hv_MaxAscent = null;
      HTuple hv_MaxDescent = null, hv_MaxWidth = null, hv_MaxHeight = null;
      HTuple hv_R1 = new HTuple(), hv_C1 = new HTuple(), hv_FactorRow = new HTuple();
      HTuple hv_FactorColumn = new HTuple(), hv_UseShadow = null;
      HTuple hv_ShadowColor = null, hv_Exception = new HTuple();
      HTuple hv_Width = new HTuple(), hv_Index = new HTuple();
      HTuple hv_Ascent = new HTuple(), hv_Descent = new HTuple();
      HTuple hv_W = new HTuple(), hv_H = new HTuple(), hv_FrameHeight = new HTuple();
      HTuple hv_FrameWidth = new HTuple(), hv_R2 = new HTuple();
      HTuple hv_C2 = new HTuple(), hv_DrawMode = new HTuple();
      HTuple hv_CurrentColor = new HTuple();
      HTuple   hv_Box_COPY_INP_TMP = hv_Box.Clone();
      HTuple   hv_Color_COPY_INP_TMP = hv_Color.Clone();
      HTuple   hv_Column_COPY_INP_TMP = hv_Column.Clone();
      HTuple   hv_Row_COPY_INP_TMP = hv_Row.Clone();
      HTuple   hv_String_COPY_INP_TMP = hv_String.Clone();

      // Initialize local and output iconic variables 
    //This procedure displays text in a graphics window.
    //
    //Input parameters:
    //WindowHandle: The WindowHandle of the graphics window, where
    //   the message should be displayed
    //String: A tuple of strings containing the text message to be displayed
    //CoordSystem: If set to 'window', the text position is given
    //   with respect to the window coordinate system.
    //   If set to 'image', image coordinates are used.
    //   (This may be useful in zoomed images.)
    //Row: The row coordinate of the desired text position
    //   If set to -1, a default value of 12 is used.
    //Column: The column coordinate of the desired text position
    //   If set to -1, a default value of 12 is used.
    //Color: defines the color of the text as string.
    //   If set to [], '' or 'auto' the currently set color is used.
    //   If a tuple of strings is passed, the colors are used cyclically
    //   for each new textline.
    //Box: If Box[0] is set to 'true', the text is written within an orange box.
    //     If set to' false', no box is displayed.
    //     If set to a color string (e.g. 'white', '#FF00CC', etc.),
    //       the text is written in a box of that color.
    //     An optional second value for Box (Box[1]) controls if a shadow is displayed:
    //       'true' -> display a shadow in a default color
    //       'false' -> display no shadow (same as if no second value is given)
    //       otherwise -> use given string as color string for the shadow color
    //
    //Prepare window
    HOperatorSet.GetRgb(hv_WindowHandle, out hv_Red, out hv_Green, out hv_Blue);
    HOperatorSet.GetPart(hv_WindowHandle, out hv_Row1Part, out hv_Column1Part, out hv_Row2Part, 
        out hv_Column2Part);
    HOperatorSet.GetWindowExtents(hv_WindowHandle, out hv_RowWin, out hv_ColumnWin, 
        out hv_WidthWin, out hv_HeightWin);
    HOperatorSet.SetPart(hv_WindowHandle, 0, 0, hv_HeightWin-1, hv_WidthWin-1);
    //
    //default settings
    if ((int)(new HTuple(hv_Row_COPY_INP_TMP.TupleEqual(-1))) != 0)
    {
      hv_Row_COPY_INP_TMP = 12;
    }
    if ((int)(new HTuple(hv_Column_COPY_INP_TMP.TupleEqual(-1))) != 0)
    {
      hv_Column_COPY_INP_TMP = 12;
    }
    if ((int)(new HTuple(hv_Color_COPY_INP_TMP.TupleEqual(new HTuple()))) != 0)
    {
      hv_Color_COPY_INP_TMP = "";
    }
    //
    hv_String_COPY_INP_TMP = (((""+hv_String_COPY_INP_TMP)+"")).TupleSplit("\n");
    //
    //Estimate extentions of text depending on font size.
    HOperatorSet.GetFontExtents(hv_WindowHandle, out hv_MaxAscent, out hv_MaxDescent, 
        out hv_MaxWidth, out hv_MaxHeight);
    if ((int)(new HTuple(hv_CoordSystem.TupleEqual("window"))) != 0)
    {
      hv_R1 = hv_Row_COPY_INP_TMP.Clone();
      hv_C1 = hv_Column_COPY_INP_TMP.Clone();
    }
    else
    {
      //Transform image to window coordinates
      hv_FactorRow = (1.0*hv_HeightWin)/((hv_Row2Part-hv_Row1Part)+1);
      hv_FactorColumn = (1.0*hv_WidthWin)/((hv_Column2Part-hv_Column1Part)+1);
      hv_R1 = ((hv_Row_COPY_INP_TMP-hv_Row1Part)+0.5)*hv_FactorRow;
      hv_C1 = ((hv_Column_COPY_INP_TMP-hv_Column1Part)+0.5)*hv_FactorColumn;
    }
    //
    //Display text box depending on text size
    hv_UseShadow = 1;
    hv_ShadowColor = "gray";
    if ((int)(new HTuple(((hv_Box_COPY_INP_TMP.TupleSelect(0))).TupleEqual("true"))) != 0)
    {
      if (hv_Box_COPY_INP_TMP == null)
        hv_Box_COPY_INP_TMP = new HTuple();
      hv_Box_COPY_INP_TMP[0] = "#fce9d4";
      hv_ShadowColor = "#f28d26";
    }
    if ((int)(new HTuple((new HTuple(hv_Box_COPY_INP_TMP.TupleLength())).TupleGreater(
        1))) != 0)
    {
      if ((int)(new HTuple(((hv_Box_COPY_INP_TMP.TupleSelect(1))).TupleEqual("true"))) != 0)
      {
        //Use default ShadowColor set above
      }
      else if ((int)(new HTuple(((hv_Box_COPY_INP_TMP.TupleSelect(1))).TupleEqual(
          "false"))) != 0)
      {
        hv_UseShadow = 0;
      }
      else
      {
        hv_ShadowColor = hv_Box_COPY_INP_TMP[1];
        //Valid color?
        try
        {
          HOperatorSet.SetColor(hv_WindowHandle, hv_Box_COPY_INP_TMP.TupleSelect(
              1));
        }
        // catch (Exception) 
        catch (HalconException HDevExpDefaultException1)
        {
          HDevExpDefaultException1.ToHTuple(out hv_Exception);
          hv_Exception = "Wrong value of control parameter Box[1] (must be a 'true', 'false', or a valid color string)";
          throw new HalconException(hv_Exception);
        }
      }
    }
    if ((int)(new HTuple(((hv_Box_COPY_INP_TMP.TupleSelect(0))).TupleNotEqual("false"))) != 0)
    {
      //Valid color?
      try
      {
        HOperatorSet.SetColor(hv_WindowHandle, hv_Box_COPY_INP_TMP.TupleSelect(0));
      }
      // catch (Exception) 
      catch (HalconException HDevExpDefaultException1)
      {
        HDevExpDefaultException1.ToHTuple(out hv_Exception);
        hv_Exception = "Wrong value of control parameter Box[0] (must be a 'true', 'false', or a valid color string)";
        throw new HalconException(hv_Exception);
      }
      //Calculate box extents
      hv_String_COPY_INP_TMP = (" "+hv_String_COPY_INP_TMP)+" ";
      hv_Width = new HTuple();
      for (hv_Index=0; (int)hv_Index<=(int)((new HTuple(hv_String_COPY_INP_TMP.TupleLength()
          ))-1); hv_Index = (int)hv_Index + 1)
      {
        HOperatorSet.GetStringExtents(hv_WindowHandle, hv_String_COPY_INP_TMP.TupleSelect(
            hv_Index), out hv_Ascent, out hv_Descent, out hv_W, out hv_H);
        hv_Width = hv_Width.TupleConcat(hv_W);
      }
      hv_FrameHeight = hv_MaxHeight*(new HTuple(hv_String_COPY_INP_TMP.TupleLength()
          ));
      hv_FrameWidth = (((new HTuple(0)).TupleConcat(hv_Width))).TupleMax();
      hv_R2 = hv_R1+hv_FrameHeight;
      hv_C2 = hv_C1+hv_FrameWidth;
      //Display rectangles
      HOperatorSet.GetDraw(hv_WindowHandle, out hv_DrawMode);
      HOperatorSet.SetDraw(hv_WindowHandle, "fill");
      //Set shadow color
      HOperatorSet.SetColor(hv_WindowHandle, hv_ShadowColor);
      if ((int)(hv_UseShadow) != 0)
      {
        HOperatorSet.DispRectangle1(hv_WindowHandle, hv_R1+1, hv_C1+1, hv_R2+1, hv_C2+1);
      }
      //Set box color
      HOperatorSet.SetColor(hv_WindowHandle, hv_Box_COPY_INP_TMP.TupleSelect(0));
      HOperatorSet.DispRectangle1(hv_WindowHandle, hv_R1, hv_C1, hv_R2, hv_C2);
      HOperatorSet.SetDraw(hv_WindowHandle, hv_DrawMode);
    }
    //Write text.
    for (hv_Index=0; (int)hv_Index<=(int)((new HTuple(hv_String_COPY_INP_TMP.TupleLength()
        ))-1); hv_Index = (int)hv_Index + 1)
    {
      hv_CurrentColor = hv_Color_COPY_INP_TMP.TupleSelect(hv_Index%(new HTuple(hv_Color_COPY_INP_TMP.TupleLength()
          )));
      if ((int)((new HTuple(hv_CurrentColor.TupleNotEqual(""))).TupleAnd(new HTuple(hv_CurrentColor.TupleNotEqual(
          "auto")))) != 0)
      {
        HOperatorSet.SetColor(hv_WindowHandle, hv_CurrentColor);
      }
      else
      {
        HOperatorSet.SetRgb(hv_WindowHandle, hv_Red, hv_Green, hv_Blue);
      }
      hv_Row_COPY_INP_TMP = hv_R1+(hv_MaxHeight*hv_Index);
      HOperatorSet.SetTposition(hv_WindowHandle, hv_Row_COPY_INP_TMP, hv_C1);
      HOperatorSet.WriteString(hv_WindowHandle, hv_String_COPY_INP_TMP.TupleSelect(
          hv_Index));
    }
    //Reset changed window settings
    HOperatorSet.SetRgb(hv_WindowHandle, hv_Red, hv_Green, hv_Blue);
    HOperatorSet.SetPart(hv_WindowHandle, hv_Row1Part, hv_Column1Part, hv_Row2Part, 
        hv_Column2Part);

    return;
  }
    }

    public enum Enumpolarity
    {
       light_on_dark,
        dark_on_light,
        any
    }

    public enum EumDataMoudulParma
    {
        None,
        standard_recognition,
        enhanced_recognition,
        maximum_recognition
    
    }
    public  class codeEnumInfo
    { 
      public static string DataMatrixECC200 = "Data Matrix ECC 200";
      public static string AztecCode = "Aztec Code";
      public static string GS1AztecCode = "GS1 Aztec Code";
      public static string GS1DataMatrix = "GS1 DataMatrix";
      public static string GS1QRCode = "GS1 QR Code";
      public static string MicroQRCode = "Micro QR Code";
      public static string PDF417 = "PDF417";
      public static string QRCode = "QR Code";

      //public static Dictionary<string,string> codeEnumDirc
      //{
      //    get 
      //    {
      //        Dictionary<string, string> temdirc = new Dictionary<string, string>();
      //        temdirc.Add(DataMatrixECC200, "Data Matrix ECC 200");
      //        temdirc.Add(AztecCode, "Aztec Code");
      //        temdirc.Add(GS1AztecCode, "GS1 Aztec Code");
      //        temdirc.Add(GS1DataMatrix, "GS1 DataMatrix");
      //        temdirc.Add(GS1QRCode, "GS1 QR Code");
      //        temdirc.Add(MicroQRCode, "Micro QR Code");
      //        temdirc.Add(PDF417, "PDF417");
      //        temdirc.Add(QRCode, "QR Code");
      //        return temdirc;
      //    }
      //}

      public static List<string> codeEnumLst
      {
          get
          {
              List<string> temList = new List<string>();
              temList.Add(DataMatrixECC200);
              temList.Add(AztecCode);
              temList.Add(GS1AztecCode);
              temList.Add(GS1DataMatrix);
              temList.Add(GS1QRCode);
              temList.Add(MicroQRCode);
              temList.Add(PDF417);
              temList.Add(QRCode);
              return temList;
          }
      }
    }
}
