#pragma once
#include <opencv2/opencv.hpp>
using namespace cv;
using namespace std;

struct IplImageArr
{
	IplImage * img;
};

struct ImgEdgeInfo////////////////�����洢Ŀ��ͼ���߶ȵ��ݶ���Ϣ
{
	int16_t  *pBufGradX ;
	int16_t  *pBufGradY ;
	float	    *pBufMag;
};
struct PyramidEdgePoints
{
	int     level;
	int	    numOfCordinates;	//��������
	Point   *edgePoints;        //�����
	double	*edgeMagnitude;		//�ݶȷ�ֵ����
	double  *edgeDerivativeX;	//X�����ݶ�
	double  *edgeDerivativeY;	//Y�����ݶ�
	Point   centerOfGravity;	//ģ����������
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
//ƥ�����ṹ��
struct MatchResult
{
	int nums;
	double          scale;
	int             level;
	int 			Angel;						//ƥ��Ƕ�
	int 			CenterLocX;				//ƥ��ο���X����
	int			CenterLocY;				//ƥ��ο���Y����
	float 		ResultScore;				//ƥ��ķ�
};
//��������
struct search_region
{
	int 	StartX;											//X�������
	int 	StartY;											//y�������
	int 	EndX;											//x�����յ�
	int 	EndY;											//y�����յ�
};
class ShapeMatch
{
private:
	ScaleEdgePoints* scaleEdgePoints;//���������
	int				modelHeight;		//ģ��ͼ��߶�
	int				modelWidth;			//ģ��ͼ����
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
