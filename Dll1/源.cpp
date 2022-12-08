#include "ShapeMatch.h"
using namespace std;
using namespace cv;
void main()
{
	ShapeMatch shapematch;
	//Mat templateArr = imread("1.BMP");
	IplImage* templateArr = cvLoadImage("20.BMP",-1);
	CvSize templateSize = cvSize(templateArr->width, templateArr->height);
	IplImage* grayTemplateImg = cvCreateImage(templateSize, IPL_DEPTH_8U, 1);

	// Convert color image to gray image.
	if (templateArr->nChannels == 3)
	{
		cvCvtColor(templateArr, grayTemplateImg, CV_RGB2GRAY);
	}
	else
	{
		cvCopy(templateArr, grayTemplateImg);
	}
	double minScore = 0.95;
	double greediness = 0.8;
	double maxContrast=80;
	double minContrast = 20;
	int pyramidnums = 3;
	double anglestart =-4;
	double angleend = 4;
	double anglestep = 0.5;
	double scalestart = 0.98;
	double scaleend = 1.02;
	double scalestep = 0.005;
	shapematch.CreateMatchModel(grayTemplateImg, maxContrast, minContrast,pyramidnums, anglestart,angleend,anglestep,scalestart,scaleend,scalestep);;
	int t = 2772;
	while (1)
	{
		t++;
		char s[50];
		sprintf(s, "E:\\Êý¾Ý¼¯\\20190118\\1 (%d).jpg", t);
		Mat image = imread(s, 1);
		IplImage *searchImage = &IplImage(image);
	    CvPoint result= CvPoint(0,0);
	    double score = shapematch.FindGeoMatchModel(searchImage, minScore, greediness, &result, pyramidnums, anglestart, angleend, anglestep, scalestart, scaleend, scalestep);
	//cvReleaseImage(&searchImage);
	//cout << score << endl;
	//double time1 = clock();
	//double time2 = clock();
	//cout << "cost time:" << (time2 - time1) / 1000 << endl;
	//imwrite("result.bmp", image);
	//destroyWindow(s);
	}

}