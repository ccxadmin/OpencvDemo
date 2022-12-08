#pragma once
#include <opencv2/opencv.hpp>
using namespace cv;
using namespace std;

struct IplImageArr
{
	IplImage * img;
};

struct ImgEdgeInfo////////////////用来存储目标图像多尺度的梯度信息
{
	int16_t  *pBufGradX ;
	int16_t  *pBufGradY ;
	float	    *pBufMag;
};
struct PyramidEdgePoints
{
	int     level;
	int	    numOfCordinates;	//坐标点个数
	Point   *edgePoints;        //坐标点
	double	*edgeMagnitude;		//梯度幅值数列
	double  *edgeDerivativeX;	//X方向梯度
	double  *edgeDerivativeY;	//Y方向梯度
	Point   centerOfGravity;	//模板重心坐标
};
struct AngleEdgePoints
{
	PyramidEdgePoints *pyramidEdgePoints;
	double  templateAngle;

};
struct ScaleEdgePoints
{
	AngleEdgePoints *angleEdgePoints;
	double scaleVale;
};
//匹配结果结构体
struct MatchResult
{
	int nums;
	double          scale;
	int             level;
	int 			Angel;						//匹配角度
	int 			CenterLocX;				//匹配参考点X坐标
	int			CenterLocY;				//匹配参考点Y坐标
	float 		ResultScore;				//匹配的分
};
//搜索区域
struct search_region
{
	int 	StartX;											//X方向起点
	int 	StartY;											//y方向起点
	int 	EndX;											//x方向终点
	int 	EndY;											//y方向终点
};
class ShapeMatch
{
private:
	ScaleEdgePoints* scaleEdgePoints;//坐标点数列
	int				modelHeight;		//模板图像高度
	int				modelWidth;			//模板图像宽度
	bool			modelDefined;
	Point           gravityPoint;
	void CreateDoubleMatrix(double **&matrix, Size size);
	void ReleaseDoubleMatrix(double **&matrix, int size);
	void ShapeMatch::rotateImage(IplImage* srcImage, IplImage* dstImage, float Angle);
public:
	ShapeMatch(void);
	float new_rsqrt(float f);
	//ShapeMatch(const void* templateArr);
	~ShapeMatch(void);
	int CreateMatchModel(IplImage *templateArr, double maxContrast, double minContrast, int pyramidnums,double anglestart, double angleend,double anglestep,double scalestart,double scaleend, double scalestep);
	int ShapeMatch::CalEdgeCordinates(IplImage *templateArr, double maxContrast, double minContrast, PyramidEdgePoints *PyramidEdgePtr);
	double FindGeoMatchModel(IplImage* srcarr, double minScore, double greediness, CvPoint *resultPoint, int pyramidnums, double anglestart, double angleend, double anglestep, double scalestart, double scaleend, double scalestep);
	//double FindGeoMatchModel(const void* srcarr, double minScore, double greediness, CvPoint *resultPoint);
	//void DrawContours(IplImage* pImage, CvPoint COG, CvScalar, int);
	//void DrawContours(IplImage* pImage, CvScalar, int);
	void DrawContours(IplImage* source, CvScalar color, int lineWidth,  Point   *cordinates, Point  centerOfGravity, int noOfCordinates);
	void extract_shape_info(IplImage *ImageData, PyramidEdgePoints *PyramidEdgePtr, int Contrast, int MinContrast);
	void shape_match_accurate(IplImage *SearchImage, PyramidEdgePoints *ShapeInfoVec, int Contrast, int MinContrast, float MinScore, float Greediness, search_region *SearchRegion, MatchResult *ResultList, ImgEdgeInfo *imgEdgeInfo);
	void CalSearchImgEdg(IplImage *SearchImage, ImgEdgeInfo *imgEdgeInfo);
	Point extract_shape_info(IplImage *ImageData, int Contrast, int MinContrast);
};
