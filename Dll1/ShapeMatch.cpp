#include "ShapeMatch.h"
#include "omp.h"
ShapeMatch::ShapeMatch()
{
	modelDefined = false;
	modelHeight=0;		
	modelWidth=0;			
}
ShapeMatch::~ShapeMatch()
{
}

int ShapeMatch::CreateMatchModel(IplImage *templateArr, double maxContrast, double minContrast, int pyramidnums, double anglestart, double angleend, double anglestep, double scalestart, double scaleend,double scalestep)
{	
	int scalenum = abs(scaleend - scalestart) / scalestep+1;
	int anglenum = abs(angleend - anglestart) / anglestep+1;
	scaleEdgePoints = (ScaleEdgePoints *)malloc(scalenum * sizeof(ScaleEdgePoints));
    ////求模板重心
	gravityPoint = extract_shape_info(templateArr, maxContrast, minContrast);
	////
	for (int i = 0; i < scalenum; i++)
	{
		scaleEdgePoints[i].angleEdgePoints= (AngleEdgePoints *)malloc(anglenum * sizeof(AngleEdgePoints));
		scaleEdgePoints[i].scaleVale = scalestart + i*scalestep;
		AngleEdgePoints *angleEdgePtr = scaleEdgePoints[i].angleEdgePoints;
		for (int j = 0; j < anglenum; j++)
		{
			angleEdgePtr[j].pyramidEdgePoints = (PyramidEdgePoints *)malloc((1+pyramidnums)* sizeof(PyramidEdgePoints));
			angleEdgePtr[j].templateAngle= anglestart + j*anglestep;
			PyramidEdgePoints *pyramidEdgePtr = angleEdgePtr[j].pyramidEdgePoints;
			IplImage * scaleAngleImage= cvCreateImage(cvSize(templateArr->width*(scalestart + i*scalestep), templateArr->height*(scalestart + i*scalestep)), IPL_DEPTH_8U, 1);
			cvResize(templateArr, scaleAngleImage);
			rotateImage(scaleAngleImage, scaleAngleImage, anglestart + j*anglestep);
			IplImage * tempDownImg=cvCreateImage(cvSize(round(scaleAngleImage->width), round(scaleAngleImage->height)), IPL_DEPTH_8U, 1);
			cvCopy(scaleAngleImage, tempDownImg);
			//CalEdgeCordinates(tempDownImg, maxContrast, minContrast, &(pyramidEdgePtr[0]));
			extract_shape_info(tempDownImg, &(pyramidEdgePtr[0]), maxContrast, minContrast );
			//IplImage* colorimg = cvLoadImage("1.BMP", -1);
			//DrawContours(scaleAngleImage, CvScalar(0, 0, 255), 1, pyramidEdgePtr[0].edgePoints, pyramidEdgePtr[0].centerOfGravity, pyramidEdgePtr[0].numOfCordinates);
			//cvNamedWindow("Search Image", 0);
			//cvShowImage("Search Image", scaleAngleImage);
			////cvSaveImage("yyy.bmp", colorimg);
			//cvWaitKey(0);
			for (int k = 1; k <= pyramidnums; k++)
			{
				pyramidEdgePtr[k].level = k;
				CvSize size;
				if (tempDownImg->height % 2 == 0)
					size.height = tempDownImg->height >> 1;
				else 
					size.height = floor(tempDownImg->height >> 1)+1;
				if (tempDownImg->width % 2 == 0)
					size.width = tempDownImg->width >> 1;
				else
					size.width = floor(tempDownImg->width >> 1) + 1;
				//CvSize size = cvSize(floor(tempDownImg->height>>1), floor(tempDownImg->width>>1));///
				IplImage* pyDownImg=cvCreateImage(size, IPL_DEPTH_8U, 1);
				cvPyrDown(tempDownImg, pyDownImg);
				//cvResize(tempDownImg, pyDownImg)
				tempDownImg = cvCreateImage(cvSize( pyDownImg->width, pyDownImg->height), IPL_DEPTH_8U, 1);
				cvCopy(pyDownImg, tempDownImg);
				//cvNamedWindow("Search Image", 0);
				//cvShowImage("Search Image", pyDownImg);
				//cvWaitKey(0);
				//CalEdgeCordinates(pyDownImg, maxContrast, minContrast, &(pyramidEdgePtr[k]));
				extract_shape_info(pyDownImg, &(pyramidEdgePtr[k]), maxContrast, minContrast);
				//DrawContours(pyDownImg, CvScalar(0, 0, 255), 1, pyramidEdgePtr[k].edgePoints, pyramidEdgePtr[k].centerOfGravity, pyramidEdgePtr[k].numOfCordinates);
				//cvNamedWindow("Search Image", 0);
				//cvShowImage("Search Image", pyDownImg);
				////cvSaveImage("yyy.bmp", colorimg);
				//cvWaitKey(0);
			}
		}
	}
	return 1;
}


int ShapeMatch::CalEdgeCordinates(IplImage *templateArr, double maxContrast, double minContrast, PyramidEdgePoints *PyramidEdgePtr)
{
	CvMat *gx = 0;		//Matrix to store X derivative
	CvMat *gy = 0;		//Matrix to store Y derivative
	CvMat *nmsEdges = 0;		//Matrix to store temp restult
	CvSize Ssize;

	// Convert IplImage to Matrix for integer operations
	CvMat srcstub, *src = (CvMat*)templateArr;
	src = cvGetMat(src, &srcstub);
	if (CV_MAT_TYPE(src->type) != CV_8UC1)
	{
		return 0;
	}

	// set width and height
	Ssize.width = src->width;
	Ssize.height = src->height;
	modelHeight = src->height;		//Save Template height
	modelWidth = src->width;			//Save Template width

	PyramidEdgePtr->numOfCordinates = 0;											//initialize	
	PyramidEdgePtr->edgePoints = new Point[modelWidth *modelHeight];		//Allocate memory for coorinates of selected points in template image

	PyramidEdgePtr->edgeMagnitude = new double[modelWidth *modelHeight];		//Allocate memory for edge magnitude for selected points
	PyramidEdgePtr->edgeDerivativeX = new double[modelWidth *modelHeight];			//Allocate memory for edge X derivative for selected points
	PyramidEdgePtr->edgeDerivativeY = new double[modelWidth *modelHeight];			////Allocate memory for edge Y derivative for selected points
																	// Calculate gradient of Template
	gx = cvCreateMat(Ssize.height, Ssize.width, CV_16SC1);		//create Matrix to store X derivative
	gy = cvCreateMat(Ssize.height, Ssize.width, CV_16SC1);		//create Matrix to store Y derivative
	cvSobel(src, gx, 1, 0, 3);		//gradient in X direction			
	cvSobel(src, gy, 0, 1, 3);	//gradient in Y direction

	nmsEdges = cvCreateMat(Ssize.height, Ssize.width, CV_32F);		//create Matrix to store Final nmsEdges
	const short* _sdx;
	const short* _sdy;
	double fdx, fdy;
	double MagG, DirG;
	double MaxGradient = -99999.99;
	double direction;
	int *orients = new int[Ssize.height *Ssize.width];
	int count = 0, i, j; // count variable;

	double **magMat;
	CreateDoubleMatrix(magMat, Ssize);

	for (i = 1; i < Ssize.height - 1; i++)
	{
		for (j = 1; j < Ssize.width - 1; j++)
		{
			_sdx = (short*)(gx->data.ptr + gx->step*i);
			_sdy = (short*)(gy->data.ptr + gy->step*i);
			fdx = _sdx[j]; fdy = _sdy[j];        // read x, y derivatives

			MagG = sqrt((float)(fdx*fdx) + (float)(fdy*fdy)); //Magnitude = Sqrt(gx^2 +gy^2)
			direction = cvFastArctan((float)fdy, (float)fdx);	 //Direction = invtan (Gy / Gx)
			magMat[i][j] = MagG;

			if (MagG>MaxGradient)
				MaxGradient = MagG; // get maximum gradient value for normalizing.


									// get closest angle from 0, 45, 90, 135 set
			if ((direction>0 && direction < 22.5) || (direction >157.5 && direction < 202.5) || (direction>337.5 && direction<360))
				direction = 0;
			else if ((direction>22.5 && direction < 67.5) || (direction >202.5 && direction <247.5))
				direction = 45;
			else if ((direction >67.5 && direction < 112.5) || (direction>247.5 && direction<292.5))
				direction = 90;
			else if ((direction >112.5 && direction < 157.5) || (direction>292.5 && direction<337.5))
				direction = 135;
			else
				direction = 0;

			orients[count] = (int)direction;
			count++;
		}
	}

	count = 0; // init count
			   // non maximum suppression非极大值抑制
	double leftPixel, rightPixel;

	for (i = 1; i < Ssize.height - 1; i++)
	{
		for (j = 1; j < Ssize.width - 1; j++)
		{
			switch (orients[count])
			{
			case 0:
				leftPixel = magMat[i][j - 1];
				rightPixel = magMat[i][j + 1];
				break;
			case 45:
				leftPixel = magMat[i - 1][j + 1];
				rightPixel = magMat[i + 1][j - 1];
				break;
			case 90:
				leftPixel = magMat[i - 1][j];
				rightPixel = magMat[i + 1][j];
				break;
			case 135:
				leftPixel = magMat[i - 1][j - 1];
				rightPixel = magMat[i + 1][j + 1];
				break;
			}
			// compare current pixels value with adjacent pixels
			if ((magMat[i][j] < leftPixel) || (magMat[i][j] < rightPixel))
				(nmsEdges->data.ptr + nmsEdges->step*i)[j] = 0;
			else
				(nmsEdges->data.ptr + nmsEdges->step*i)[j] = (uchar)(magMat[i][j] / MaxGradient * 255);

			count++;
		}
	}


	int RSum = 0, CSum = 0;
	int curX, curY;
	int flag = 1;

	//Hysterisis threshold滞后阈值
	for (i = 1; i < Ssize.height - 1; i++)
	{
		for (j = 1; j < Ssize.width; j++)
		{
			_sdx = (short*)(gx->data.ptr + gx->step*i);
			_sdy = (short*)(gy->data.ptr + gy->step*i);
			fdx = _sdx[j]; fdy = _sdy[j];

			MagG = sqrt(fdx*fdx + fdy*fdy); //Magnitude = Sqrt(gx^2 +gy^2)
			DirG = cvFastArctan((float)fdy, (float)fdx);	 //Direction = tan(y/x)

															 ////((uchar*)(imgGDir->imageData + imgGDir->widthStep*i))[j]= MagG;
			flag = 1;
			if (((double)((nmsEdges->data.ptr + nmsEdges->step*i))[j]) < maxContrast)
			{
				if (((double)((nmsEdges->data.ptr + nmsEdges->step*i))[j])< minContrast)
				{

					(nmsEdges->data.ptr + nmsEdges->step*i)[j] = 0;
					flag = 0; // remove from edge
							  ////((uchar*)(imgGDir->imageData + imgGDir->widthStep*i))[j]=0;
				}
				else
				{   // if any of 8 neighboring pixel is not greater than max contraxt remove from edge
					if ((((double)((nmsEdges->data.ptr + nmsEdges->step*(i - 1)))[j - 1]) < maxContrast) &&
						(((double)((nmsEdges->data.ptr + nmsEdges->step*(i - 1)))[j]) < maxContrast) &&
						(((double)((nmsEdges->data.ptr + nmsEdges->step*(i - 1)))[j + 1]) < maxContrast) &&
						(((double)((nmsEdges->data.ptr + nmsEdges->step*i))[j - 1]) < maxContrast) &&
						(((double)((nmsEdges->data.ptr + nmsEdges->step*i))[j + 1]) < maxContrast) &&
						(((double)((nmsEdges->data.ptr + nmsEdges->step*(i + 1)))[j - 1]) < maxContrast) &&
						(((double)((nmsEdges->data.ptr + nmsEdges->step*(i + 1)))[j]) < maxContrast) &&
						(((double)((nmsEdges->data.ptr + nmsEdges->step*(i + 1)))[j + 1]) < maxContrast))
					{
						(nmsEdges->data.ptr + nmsEdges->step*i)[j] = 0;
						flag = 0;
						////((uchar*)(imgGDir->imageData + imgGDir->widthStep*i))[j]=0;
					}
				}

			}

			// save selected edge information
			curX = i;	curY = j;
			if (flag != 0)
			{
				if (fdx != 0 || fdy != 0)
				{
					RSum = RSum + curX;	CSum = CSum + curY; // Row sum and column sum for center of gravity					
					PyramidEdgePtr->edgePoints[PyramidEdgePtr->numOfCordinates].x = curX;
					PyramidEdgePtr->edgePoints[PyramidEdgePtr->numOfCordinates].y = curY;
					PyramidEdgePtr->edgeDerivativeX[PyramidEdgePtr->numOfCordinates] = fdx;
					PyramidEdgePtr->edgeDerivativeY[PyramidEdgePtr->numOfCordinates] = fdy;

					//handle divide by zero
					if (MagG != 0)
						PyramidEdgePtr->edgeMagnitude[PyramidEdgePtr->numOfCordinates] = 1 / MagG;  // gradient magnitude 
					else
						PyramidEdgePtr->edgeMagnitude[PyramidEdgePtr->numOfCordinates] = 0;

					PyramidEdgePtr->numOfCordinates++;
				}
			}
		}
	}

	PyramidEdgePtr->centerOfGravity.x = RSum / PyramidEdgePtr->numOfCordinates; // center of gravity
	PyramidEdgePtr->centerOfGravity.y = CSum / PyramidEdgePtr->numOfCordinates;	// center of gravity

												// change coordinates to reflect center of gravity
	for (int m = 0; m<PyramidEdgePtr->numOfCordinates; m++)
	{
		int temp;

		temp = PyramidEdgePtr->edgePoints[m].x;
		PyramidEdgePtr->edgePoints[m].x = temp - PyramidEdgePtr->centerOfGravity.x;
		temp = PyramidEdgePtr->edgePoints[m].y;
		PyramidEdgePtr->edgePoints[m].y = temp - PyramidEdgePtr->centerOfGravity.y;
	}

	////cvSaveImage("Edges.bmp",imgGDir);

	// free alocated memories
	delete[] orients;
	////cvReleaseImage(&imgGDir);
	cvReleaseMat(&gx);
	cvReleaseMat(&gy);
	cvReleaseMat(&nmsEdges);
	ReleaseDoubleMatrix(magMat, Ssize.height);
	modelDefined = true;
	return 1;
}
//allocate memory for doubel matrix
void ShapeMatch::CreateDoubleMatrix(double **&matrix, Size size)
{
	matrix = new double*[size.height];
	for (int iInd = 0; iInd < size.height; iInd++)
		matrix[iInd] = new double[size.width];
}

// release memory
void ShapeMatch::ReleaseDoubleMatrix(double **&matrix, int size)
{
	for (int iInd = 0; iInd < size; iInd++)
		delete[] matrix[iInd];
}
void ShapeMatch::rotateImage(IplImage* srcImage, IplImage* dstImage, float Angle)
{
	float m[6];
	m[0] = (float)cos(Angle * CV_PI / 180.);
	m[1] = (float)sin(Angle * CV_PI / 180.);
	m[3] = -m[1];
	m[4] = m[0];
	m[2] = gravityPoint.x;
	m[5] = gravityPoint.y;
	//m[2] = srcImage->width * 0.5f;
	//m[5] = srcImage->width * 0.5f;
	CvMat M = cvMat(2, 3, CV_32F, m);
	cvGetQuadrangleSubPix(srcImage, dstImage, &M);
}

double ShapeMatch::FindGeoMatchModel(IplImage* srcarr, double minScore, double greediness, CvPoint *resultPoint, int pyramidnums, double anglestart, double angleend, double anglestep, double scalestart, double scaleend, double scalestep)
{
	if (srcarr == NULL)
		return -1;
	CvSize srcImgSize = cvSize(srcarr->width, srcarr->height);
	IplImage* grayImg = cvCreateImage(srcImgSize, IPL_DEPTH_8U, 1);

	// Convert color image to gray image.
	if (srcarr->nChannels == 3)
	{
		cvCvtColor(srcarr, grayImg, CV_RGB2GRAY);
	}
	else
	{
		cvCopy(srcarr, grayImg);
	}
	double resultScore = 0;
	double maxScore=0;
	int maxScoreId=0;
	PyramidEdgePoints *matchEdgePoints=new PyramidEdgePoints;///////////暂时注释
	double partialSum = 0;
	double sumOfCoords = 0;
	double partialScore;
	CvSize Ssize;
	CvPoint tempMatchPoint(0,0);
	AngleEdgePoints *angleEdgePtr;
	PyramidEdgePoints *pyramidEdgePtr;
	int scalenum = abs(scaleend - scalestart) / scalestep + 1;
	int anglenum = abs(angleend - anglestart) / anglestep + 1;
	ImgEdgeInfo *imgEdgeInfo= (ImgEdgeInfo *)malloc((pyramidnums + 1) * sizeof(ImgEdgeInfo));

	IplImageArr  *pyDownImgArr= (IplImageArr *)malloc((pyramidnums+1) * sizeof(IplImageArr));
	IplImage * tempDownImg = cvCreateImage(cvSize(grayImg->width, grayImg->height), IPL_DEPTH_8U, 1);
	cvCopy(grayImg, tempDownImg);
	pyDownImgArr[0].img = cvCreateImage(cvSize(grayImg->width, grayImg->height), IPL_DEPTH_8U, 1);
	cvCopy(grayImg, pyDownImgArr[0].img);
	CalSearchImgEdg(tempDownImg, &(imgEdgeInfo[0]));
	for (int i=1;i<=pyramidnums;i++)
	{
		CvSize size;
		if (tempDownImg->height % 2 == 0)
			size.height = tempDownImg->height >> 1;
		else
			size.height = floor(tempDownImg->height >> 1) + 1;
		if (tempDownImg->width % 2 == 0)
			size.width = tempDownImg->width >> 1;
		else
			size.width = floor(tempDownImg->width >> 1) + 1;
		//CvSize size = cvSize(floor(tempDownImg->height>>1), floor(tempDownImg->width>>1));///
		IplImage* pyDownImg = cvCreateImage(size, IPL_DEPTH_8U, 1);
		pyDownImgArr[i].img= cvCreateImage(size, IPL_DEPTH_8U, 1);
		cvPyrDown(tempDownImg, pyDownImg);
		cvReleaseImage(&tempDownImg);
		tempDownImg = cvCreateImage(cvSize(pyDownImg->width, pyDownImg->height), IPL_DEPTH_8U, 1);
		cvCopy(pyDownImg, tempDownImg);
		cvCopy(pyDownImg, pyDownImgArr[i].img);
		CalSearchImgEdg(tempDownImg, &(imgEdgeInfo[i]));
		cvReleaseImage(&pyDownImg);
		/*cvNamedWindow("Search Image", 0);
		cvShowImage("Search Image", tempDownImg);
		cvWaitKey(0);*/
		//cvSaveImage("tempimg.png", tempDownImg);
	}
   // #pragma omp parallel for
	MatchResult *ResultList = new MatchResult;
	MatchResult *ResultLists = new MatchResult[9999];
	int matcnnums = 0;
	search_region *SearchRegion = new search_region;
	for (int ii = 0; ii < scalenum; ii++)
	{
		angleEdgePtr = scaleEdgePoints[ii].angleEdgePoints;
		for (int jj = 0; jj < anglenum; jj++)
		{
			pyramidEdgePtr = angleEdgePtr[jj].pyramidEdgePoints;
			
			ResultList->CenterLocX = 0;
			ResultList->CenterLocY = 0;
			
			SearchRegion->EndX = pyDownImgArr[pyramidnums].img->width-1; SearchRegion->EndY = pyDownImgArr[pyramidnums].img->height - 1;
			SearchRegion->StartX = 1; SearchRegion->StartY = 1;
			for (int kk = pyramidnums; kk >= 0; kk--)
			{
				ResultList->CenterLocX = 0;
				ResultList->CenterLocY = 0;
				shape_match_accurate(pyDownImgArr[kk].img, &(pyramidEdgePtr[kk]),80, 20,/////80，20参数待修改
					minScore, greediness,SearchRegion,ResultList, &(imgEdgeInfo[kk]));
				if (ResultList->CenterLocX == 0 || ResultList->CenterLocY == 0)
				{
					break;
				}
				else
				{
					SearchRegion->StartX = ResultList->CenterLocX*2 - 6;
					SearchRegion->StartY = ResultList->CenterLocY *2 - 6;
					SearchRegion->EndX = ResultList->CenterLocX *2 +6;
					SearchRegion->EndY = ResultList->CenterLocY * 2 + 6;
					resultScore = ResultList->ResultScore;
				}
			}
			if (resultScore > minScore&&matcnnums<9999)
			{
				if (resultScore > maxScore)
				{
					maxScore = resultScore;
					maxScoreId = matcnnums;
					matchEdgePoints = &(pyramidEdgePtr[0]);//////////////////////暂时注释
				}
				ResultLists[matcnnums].ResultScore = resultScore;
				ResultLists[matcnnums].CenterLocX= ResultList->CenterLocX ;
				ResultLists[matcnnums].CenterLocY= ResultList->CenterLocY;
				ResultLists[matcnnums].scale = scaleEdgePoints[ii].scaleVale;
				ResultLists[matcnnums].Angel = angleEdgePtr[jj].templateAngle;				
				matcnnums++;
				ResultLists[matcnnums].nums = matcnnums;
			}
		}
	}
	if (matcnnums > 0)
	{
		resultPoint->x = ResultLists[maxScoreId].CenterLocX; resultPoint->y = ResultLists[maxScoreId].CenterLocY;
	}
	//if (matcnnums > 0)
	//{
	//	cout << "最匹配------------------------------------" << endl;
	//	cout << "分数:" << ResultLists[maxScoreId].ResultScore << endl;
	//	cout << "x:" << ResultLists[maxScoreId].CenterLocX << endl;
	//	cout << "y:" << ResultLists[maxScoreId].CenterLocY << endl;
	//	cout << "缩放系数：" << ResultLists[maxScoreId].scale << endl;
	//	cout << "角度：" << ResultLists[maxScoreId].Angel << endl;
	//	cout << endl;

	//}///暂时注释

	if (matcnnums > 0)
	{
		//DrawContours(srcarr, CvScalar(0, 0, 255), 1, matchEdgePoints->edgePoints, Point(ResultLists[maxScoreId].CenterLocX, ResultLists[maxScoreId].CenterLocY), matchEdgePoints->numOfCordinates);
	}
	/*cvNamedWindow("Search Image", 0);
	cvShowImage("Search Image", srcarr);
	cvWaitKey(600);*/
	//////
	//cvDestroyWindow("Search Image");
	//cvReleaseImage(&srcarr);
	delete ResultList; ResultList = NULL;
	delete []ResultLists; ResultLists = NULL;
	delete SearchRegion; SearchRegion = NULL;
	///////
	//delete matchEdgePoints;
	//////
	/////释放内存这里是pyramidnums=3,金字塔层数pyramidnums改变时自己稍微修改下吧
	free(imgEdgeInfo[0].pBufGradX); free(imgEdgeInfo[0].pBufGradY); free(imgEdgeInfo[0].pBufMag); imgEdgeInfo[0].pBufGradX = NULL; imgEdgeInfo[0].pBufGradY = NULL; imgEdgeInfo[0].pBufMag = NULL;
	free(imgEdgeInfo[1].pBufGradX); free(imgEdgeInfo[1].pBufGradY); free(imgEdgeInfo[1].pBufMag); imgEdgeInfo[1].pBufGradX = NULL; imgEdgeInfo[1].pBufGradY = NULL; imgEdgeInfo[1].pBufMag = NULL;
	free(imgEdgeInfo[2].pBufGradX); free(imgEdgeInfo[2].pBufGradY); free(imgEdgeInfo[2].pBufMag); imgEdgeInfo[2].pBufGradX = NULL; imgEdgeInfo[2].pBufGradY = NULL; imgEdgeInfo[2].pBufMag = NULL;
	free(imgEdgeInfo[3].pBufGradX); free(imgEdgeInfo[3].pBufGradY); free(imgEdgeInfo[3].pBufMag); imgEdgeInfo[3].pBufGradX = NULL; imgEdgeInfo[3].pBufGradY = NULL; imgEdgeInfo[3].pBufMag = NULL;
	/////
	free(imgEdgeInfo); imgEdgeInfo = NULL;
	///////////
	cvReleaseImage(&(pyDownImgArr[0].img)); cvReleaseImage(&(pyDownImgArr[1].img)); cvReleaseImage(&(pyDownImgArr[2].img));
	cvReleaseImage(&(pyDownImgArr[3].img));
	///////////
	free(pyDownImgArr) ; pyDownImgArr = NULL;
	cvReleaseImage(&grayImg); 
	cvReleaseImage(&tempDownImg);
	return resultScore;

}
void ShapeMatch::DrawContours(IplImage* source, CvScalar color, int lineWidth, Point   *cordinates, Point  centerOfGravity,int noOfCordinates)
{
	CvPoint point;
	for (int i = 0; i<noOfCordinates; i++)
	{
		point.x = cordinates[i].x + centerOfGravity.x;
		point.y = cordinates[i].y + centerOfGravity.y;
		cvLine(source, point, point, color, lineWidth);
	}
}



/////////////////////提取轮廓
void ShapeMatch::extract_shape_info(IplImage *ImageData, PyramidEdgePoints *PyramidEdgePtr, int Contrast, int MinContrast)
{

		/* source image size */
	int width = ImageData->width;
	int height = ImageData->height;
	int widthstep = ImageData->widthStep;
	/* Compute buffer sizes */
	uint32_t  bufferSize = widthstep * height;
	PyramidEdgePtr->numOfCordinates = 0;											//initialize	
	PyramidEdgePtr->edgePoints = new Point[bufferSize];		//Allocate memory for coorinates of selected points in template image

	PyramidEdgePtr->edgeMagnitude = new double[bufferSize];		//Allocate memory for edge magnitude for selected points
	PyramidEdgePtr->edgeDerivativeX = new double[bufferSize];			//Allocate memory for edge X derivative for selected points
	PyramidEdgePtr->edgeDerivativeY = new double[bufferSize];			////Allocate memory for edge Y derivative for selected points

	/* Allocate buffers for each vector */
	uint8_t  *pInput = (uint8_t *)malloc(bufferSize * sizeof(uint8_t));
	uint8_t  *pBufOut = (uint8_t *)malloc(bufferSize * sizeof(uint8_t));
	int16_t  *pBufGradX = (int16_t *)malloc(bufferSize * sizeof(int16_t));
	int16_t  *pBufGradY = (int16_t *)malloc(bufferSize * sizeof(int16_t));
	int32_t	*pBufOrien = (int32_t *)malloc(bufferSize * sizeof(int32_t));
	float	    *pBufMag = (float *)malloc(bufferSize * sizeof(float));


	if (pInput && pBufGradX && pBufGradY && pBufMag && pBufOrien && pBufOut)
	{
		//gaussian_filter(ImageData, pInput, width, height);
		memcpy(pInput, ImageData->imageData, bufferSize * sizeof(uint8_t));
		memset(pBufGradX, 0, bufferSize * sizeof(int16_t));
		memset(pBufGradY, 0, bufferSize * sizeof(int16_t));
		memset(pBufOrien, 0, bufferSize * sizeof(int32_t));
		memset(pBufOut, 0, bufferSize * sizeof(uint8_t));
		memset(pBufMag, 0, bufferSize * sizeof(float));

		float MaxGradient = -9999.99f;
		int count = 0, i, j; // count variable;

		for (i = 1; i < width - 1; i++)
		{
			for (j = 1; j < height - 1; j++)
			{
				int16_t sdx = *(pInput + j*widthstep + i + 1) - *(pInput + j*widthstep + i - 1);
				int16_t sdy = *(pInput + (j + 1)*widthstep + i) - *(pInput + (j - 1)*widthstep + i);
				*(pBufGradX + j*widthstep + i) = sdx;
				*(pBufGradY + j*widthstep + i) = sdy;
				float MagG = sqrt((float)(sdx*sdx) + (float)(sdy*sdy));
				*(pBufMag + j*widthstep + i) = MagG;

				// get maximum gradient value for normalizing.
				if (MagG>MaxGradient)
					MaxGradient = MagG;
			}
		}

		for (i = 1; i < width - 1; i++)
		{
			for (j = 1; j < height - 1; j++)
			{
				int16_t fdx = *(pBufGradX + j*widthstep + i);
				int16_t fdy = *(pBufGradY + j*widthstep + i);

				float direction = cvFastArctan((float)fdy, (float)fdx);	 //Direction = invtan (Gy / Gx)

																		 // get closest angle from 0, 45, 90, 135 set
				if ((direction>0 && direction < 22.5) || (direction >157.5 && direction < 202.5) || (direction>337.5 && direction<360))
					direction = 0;
				else if ((direction>22.5 && direction < 67.5) || (direction >202.5 && direction <247.5))
					direction = 45;
				else if ((direction >67.5 && direction < 112.5) || (direction>247.5 && direction<292.5))
					direction = 90;
				else if ((direction >112.5 && direction < 157.5) || (direction>292.5 && direction<337.5))
					direction = 135;
				else
					direction = 0;

				pBufOrien[count] = (int32_t)direction;
				count++;
			}
		}

		count = 0; // init count
				   // non maximum suppression
		float leftPixel, rightPixel;

		for (i = 1; i < width - 1; i++)
		{
			for (j = 1; j < height - 1; j++)
			{
				switch (pBufOrien[count])
				{
				case 0:
					leftPixel = *(pBufMag + j*widthstep + i - 1);
					rightPixel = *(pBufMag + j*widthstep + i + 1);
					break;
				case 45:
					leftPixel = *(pBufMag + (j - 1)*widthstep + i - 1);
					rightPixel = *(pBufMag + (j + 1)*widthstep + i + 1);
					break;
				case 90:
					leftPixel = *(pBufMag + (j - 1)*widthstep + i);
					rightPixel = *(pBufMag + (j + 1)*widthstep + i);

					break;
				case 135:
					leftPixel = *(pBufMag + (j + 1)*widthstep + i - 1);
					rightPixel = *(pBufMag + (j - 1)*widthstep + i + 1);
					break;
				}
				// compare current pixels value with adjacent pixels
				if ((*(pBufMag + j*widthstep + i) < leftPixel) || (*(pBufMag + j*widthstep + i) < rightPixel))
				{
					*(pBufOut + j*widthstep + i) = 0;
				}
				else
					*(pBufOut + j*widthstep + i) = (uint8_t)(*(pBufMag + j*widthstep + i) / MaxGradient * 255);

				count++;
			}
		}
		int RSum = 0, CSum = 0;
		int curX, curY;
		int flag = 1;
		int n = 0;
		int iPr = 1;
		//Hysteresis threshold
		for (i = 1; i < width - 1; i += iPr)
		{
			for (j = 1; j < height - 1; j += iPr)
			{
				int16_t fdx = *(pBufGradX + j*widthstep + i);
				int16_t fdy = *(pBufGradY + j*widthstep + i);
				float MagG = *(pBufMag + j*widthstep + i);

				flag = 1;
				if ((float)*(pBufOut + j*widthstep + i) < Contrast)
				{
					if ((float)*(pBufOut + j*widthstep + i) < MinContrast)
					{
						*(pBufOut + j*widthstep + i) = 0;
						flag = 0; // remove from edge
					}
					else
					{   // if any of 8 neighboring pixel is not greater than max contract remove from edge
						if (((float)*(pBufOut + (j - 1)*widthstep + i - 1) < Contrast) &&
							((float)*(pBufOut + j     * widthstep + i - 1) < Contrast) &&
							((float)*(pBufOut + (j - 1) * widthstep + i - 1) < Contrast) &&
							((float)*(pBufOut + (j - 1) * widthstep + i) < Contrast) &&
							((float)*(pBufOut + (j + 1)* widthstep + i) < Contrast) &&
							((float)*(pBufOut + (j - 1) * widthstep + i + 1) < Contrast) &&
							((float)*(pBufOut + j     * widthstep + i + 1) < Contrast) &&
							((float)*(pBufOut + (j + 1)  * widthstep + i + 1) < Contrast))
						{
							*(pBufOut + j*widthstep + i) = 0;
							flag = 0;
						}
					}
				}

				// save selected edge information
				curX = i;	curY = j;
				if (flag != 0)
				{
					if (fdx != 0 || fdy != 0)
					{
						RSum = RSum + curX;
						CSum = CSum + curY; // Row sum and column sum for center of gravity

						PyramidEdgePtr->edgePoints[n].x = curX;
						PyramidEdgePtr->edgePoints[n].y = curY;
						PyramidEdgePtr->edgeDerivativeX[n] = fdx;
						PyramidEdgePtr->edgeDerivativeY[n] = fdy;

						//handle divide by zero
						if (MagG != 0)
							PyramidEdgePtr->edgeMagnitude[n] = 1 / MagG;  // gradient magnitude 
						else
							PyramidEdgePtr->edgeMagnitude[n] = 0;
						n++;
					}
				}
			}
		}
		if (n != 0)
		{
			PyramidEdgePtr->numOfCordinates = n;
			PyramidEdgePtr->centerOfGravity.x = RSum / n;			 // center of gravity
			PyramidEdgePtr->centerOfGravity.y = CSum / n;			 // center of gravity
			//PyramidEdgePtr->centerOfGravity.x = width / 2;			 // center of image
			//PyramidEdgePtr->centerOfGravity.y = height / 2;		     // center of image
		}
		// change coordinates to reflect center of reference
		int m, temp;
		for (m = 0; m < PyramidEdgePtr->numOfCordinates; m++)
		{
			temp = (PyramidEdgePtr->edgePoints + m)->x;
			(PyramidEdgePtr->edgePoints + m)->x = temp - PyramidEdgePtr->centerOfGravity.x;
			temp = (PyramidEdgePtr->edgePoints + m)->y;
			(PyramidEdgePtr->edgePoints + m)->y = temp - PyramidEdgePtr->centerOfGravity.y;
		}
	}

	free(pBufMag);
	free(pBufOrien);
	free(pBufGradY);
	free(pBufGradX);
	free(pBufOut);
	free(pInput);
}

/////////////////////提取重心
Point ShapeMatch::extract_shape_info(IplImage *ImageData, int Contrast, int MinContrast)
{
	Point gravity = Point(0, 0);
	PyramidEdgePoints *PyramidEdgePtr = new PyramidEdgePoints;
	/* source image size */
	int width = ImageData->width;
	int height = ImageData->height;
	int widthstep = ImageData->widthStep;
	/* Compute buffer sizes */
	uint32_t  bufferSize = widthstep * height;
	PyramidEdgePtr->numOfCordinates = 0;											//initialize	
	PyramidEdgePtr->edgePoints = new Point[bufferSize];		//Allocate memory for coorinates of selected points in template image

	PyramidEdgePtr->edgeMagnitude = new double[bufferSize];		//Allocate memory for edge magnitude for selected points
	PyramidEdgePtr->edgeDerivativeX = new double[bufferSize];			//Allocate memory for edge X derivative for selected points
	PyramidEdgePtr->edgeDerivativeY = new double[bufferSize];			////Allocate memory for edge Y derivative for selected points

																		/* Allocate buffers for each vector */
	uint8_t  *pInput = (uint8_t *)malloc(bufferSize * sizeof(uint8_t));
	uint8_t  *pBufOut = (uint8_t *)malloc(bufferSize * sizeof(uint8_t));
	int16_t  *pBufGradX = (int16_t *)malloc(bufferSize * sizeof(int16_t));
	int16_t  *pBufGradY = (int16_t *)malloc(bufferSize * sizeof(int16_t));
	int32_t	*pBufOrien = (int32_t *)malloc(bufferSize * sizeof(int32_t));
	float	    *pBufMag = (float *)malloc(bufferSize * sizeof(float));


	if (pInput && pBufGradX && pBufGradY && pBufMag && pBufOrien && pBufOut)
	{
		//gaussian_filter(ImageData, pInput, width, height);
		memcpy(pInput, ImageData->imageData, bufferSize * sizeof(uint8_t));
		memset(pBufGradX, 0, bufferSize * sizeof(int16_t));
		memset(pBufGradY, 0, bufferSize * sizeof(int16_t));
		memset(pBufOrien, 0, bufferSize * sizeof(int32_t));
		memset(pBufOut, 0, bufferSize * sizeof(uint8_t));
		memset(pBufMag, 0, bufferSize * sizeof(float));

		float MaxGradient = -9999.99f;
		int count = 0, i, j; // count variable;

		for (i = 1; i < width - 1; i++)
		{
			for (j = 1; j < height - 1; j++)
			{
				int16_t sdx = *(pInput + j*widthstep + i + 1) - *(pInput + j*widthstep + i - 1);
				int16_t sdy = *(pInput + (j + 1)*widthstep + i) - *(pInput + (j - 1)*widthstep + i);
				*(pBufGradX + j*widthstep + i) = sdx;
				*(pBufGradY + j*widthstep + i) = sdy;
				float MagG = sqrt((float)(sdx*sdx) + (float)(sdy*sdy));
				*(pBufMag + j*widthstep + i) = MagG;

				// get maximum gradient value for normalizing.
				if (MagG>MaxGradient)
					MaxGradient = MagG;
			}
		}

		for (i = 1; i < width - 1; i++)
		{
			for (j = 1; j < height - 1; j++)
			{
				int16_t fdx = *(pBufGradX + j*widthstep + i);
				int16_t fdy = *(pBufGradY + j*widthstep + i);

				float direction = cvFastArctan((float)fdy, (float)fdx);	 //Direction = invtan (Gy / Gx)

																		 // get closest angle from 0, 45, 90, 135 set
				if ((direction>0 && direction < 22.5) || (direction >157.5 && direction < 202.5) || (direction>337.5 && direction<360))
					direction = 0;
				else if ((direction>22.5 && direction < 67.5) || (direction >202.5 && direction <247.5))
					direction = 45;
				else if ((direction >67.5 && direction < 112.5) || (direction>247.5 && direction<292.5))
					direction = 90;
				else if ((direction >112.5 && direction < 157.5) || (direction>292.5 && direction<337.5))
					direction = 135;
				else
					direction = 0;

				pBufOrien[count] = (int32_t)direction;
				count++;
			}
		}

		count = 0; // init count
				   // non maximum suppression
		float leftPixel, rightPixel;

		for (i = 1; i < width - 1; i++)
		{
			for (j = 1; j < height - 1; j++)
			{
				switch (pBufOrien[count])
				{
				case 0:
					leftPixel = *(pBufMag + j*widthstep + i - 1);
					rightPixel = *(pBufMag + j*widthstep + i + 1);
					break;
				case 45:
					leftPixel = *(pBufMag + (j - 1)*widthstep + i - 1);
					rightPixel = *(pBufMag + (j + 1)*widthstep + i + 1);
					break;
				case 90:
					leftPixel = *(pBufMag + (j - 1)*widthstep + i);
					rightPixel = *(pBufMag + (j + 1)*widthstep + i);

					break;
				case 135:
					leftPixel = *(pBufMag + (j + 1)*widthstep + i - 1);
					rightPixel = *(pBufMag + (j - 1)*widthstep + i + 1);
					break;
				}
				// compare current pixels value with adjacent pixels
				if ((*(pBufMag + j*widthstep + i) < leftPixel) || (*(pBufMag + j*widthstep + i) < rightPixel))
				{
					*(pBufOut + j*widthstep + i) = 0;
				}
				else
					*(pBufOut + j*widthstep + i) = (uint8_t)(*(pBufMag + j*widthstep + i) / MaxGradient * 255);

				count++;
			}
		}
		int RSum = 0, CSum = 0;
		int curX, curY;
		int flag = 1;
		int n = 0;
		int iPr = 1;
		//Hysteresis threshold
		for (i = 1; i < width - 1; i += iPr)
		{
			for (j = 1; j < height - 1; j += iPr)
			{
				int16_t fdx = *(pBufGradX + j*widthstep + i);
				int16_t fdy = *(pBufGradY + j*widthstep + i);
				float MagG = *(pBufMag + j*widthstep + i);

				flag = 1;
				if ((float)*(pBufOut + j*widthstep + i) < Contrast)
				{
					if ((float)*(pBufOut + j*widthstep + i) < MinContrast)
					{
						*(pBufOut + j*widthstep + i) = 0;
						flag = 0; // remove from edge
					}
					else
					{   // if any of 8 neighboring pixel is not greater than max contract remove from edge
						if (((float)*(pBufOut + (j - 1)*widthstep + i - 1) < Contrast) &&
							((float)*(pBufOut + j     * widthstep + i - 1) < Contrast) &&
							((float)*(pBufOut + (j - 1) * widthstep + i - 1) < Contrast) &&
							((float)*(pBufOut + (j - 1) * widthstep + i) < Contrast) &&
							((float)*(pBufOut + (j + 1)* widthstep + i) < Contrast) &&
							((float)*(pBufOut + (j - 1) * widthstep + i + 1) < Contrast) &&
							((float)*(pBufOut + j     * widthstep + i + 1) < Contrast) &&
							((float)*(pBufOut + (j + 1)  * widthstep + i + 1) < Contrast))
						{
							*(pBufOut + j*widthstep + i) = 0;
							flag = 0;
						}
					}
				}

				// save selected edge information
				curX = i;	curY = j;
				if (flag != 0)
				{
					if (fdx != 0 || fdy != 0)
					{
						RSum = RSum + curX;
						CSum = CSum + curY; // Row sum and column sum for center of gravity

						PyramidEdgePtr->edgePoints[n].x = curX;
						PyramidEdgePtr->edgePoints[n].y = curY;
						PyramidEdgePtr->edgeDerivativeX[n] = fdx;
						PyramidEdgePtr->edgeDerivativeY[n] = fdy;

						//handle divide by zero
						if (MagG != 0)
							PyramidEdgePtr->edgeMagnitude[n] = 1 / MagG;  // gradient magnitude 
						else
							PyramidEdgePtr->edgeMagnitude[n] = 0;
						n++;
					}
				}
			}
		}
		if (n != 0)
		{
			PyramidEdgePtr->numOfCordinates = n;
			gravity.x = RSum / n;			 // center of gravity
			gravity.y = CSum / n;			 // center of gravity
																	 //PyramidEdgePtr->centerOfGravity.x = width / 2;			 // center of image
																	 //PyramidEdgePtr->centerOfGravity.y = height / 2;		     // center of image
		}
	}
	free(pBufMag);
	free(pBufOrien);
	free(pBufGradY);
	free(pBufGradX);
	free(pBufOut);
	free(pInput);
	delete []PyramidEdgePtr->edgePoints;		
	delete []PyramidEdgePtr->edgeMagnitude ;		
	delete []PyramidEdgePtr->edgeDerivativeX ;
	delete []PyramidEdgePtr->edgeDerivativeY;
	delete PyramidEdgePtr;
	return gravity;
}


/////////////////////

///////轮廓匹配
void ShapeMatch::shape_match_accurate(IplImage *SearchImage, PyramidEdgePoints *ShapeInfoVec, int Contrast, int MinContrast, float MinScore, float Greediness, search_region *SearchRegion, MatchResult *ResultList, ImgEdgeInfo *imgEdgeInfo)
{
	/* source image size */
	int Width = SearchImage->width;
	int Height = SearchImage->height;
	int widthstep = SearchImage->widthStep;
	/* Compute buffer sizes */
	uint32_t  bufferSize = widthstep * Height;
	int16_t  *pBufGradX = imgEdgeInfo->pBufGradX; //(int16_t *)malloc(bufferSize * sizeof(int16_t));
	int16_t  *pBufGradY = imgEdgeInfo->pBufGradY;//(int16_t *)malloc(bufferSize * sizeof(int16_t));
	float	    *pBufMag = imgEdgeInfo->pBufMag; //(float *)malloc(bufferSize * sizeof(float));

	if ( pBufGradX && pBufGradY && pBufMag)
	{
		int i, j, m; // count variable;
		int curX = 0;
		int curY = 0;

		int16_t iTx = 0;
		int16_t iTy = 0;
		int16_t iSx = 0;
		int16_t iSy = 0;
		float   iSm = 0;
		float   iTm = 0;

		int startX = SearchRegion->StartX;
		int startY = SearchRegion->StartY;
		int endX = SearchRegion->EndX;
		int endY = SearchRegion->EndY;
		int   SumOfCoords = 0;
		int   TempPiontX = 0;
		int   TempPiontY = 0;
		float PartialSum = 0;
		float PartialScore = 0;
		float ResultScore = 0;
		float TempScore = 0;
		float anMinScore = 1 - MinScore;
		float NormMinScore = 0;
		float NormGreediness = Greediness;
		/*for (int k = 0; k < ShapeInfoVec[0].AngleNum; k++)
		{
			if (ShapeInfoVec[k].Angel < AngleStart || ShapeInfoVec[k].Angel > AngleStop)
				continue;
*/
			ResultScore = 0;
			NormMinScore = MinScore / ShapeInfoVec->numOfCordinates;
			NormGreediness = ((1 - Greediness * MinScore) / (1 - Greediness)) / ShapeInfoVec->numOfCordinates;
           // #pragma omp parallel for
			for (i = startX; i < endX; i++)
			{
				for (j = startY; j < endY; j++)
				{
					PartialSum = 0;
					for (m = 0; m < ShapeInfoVec->numOfCordinates; m++)
					{
						curX = i + (ShapeInfoVec->edgePoints + m)->x;		// template X coordinate
						curY = j + (ShapeInfoVec->edgePoints + m)->y; 		// template Y coordinate
						iTx = *(ShapeInfoVec->edgeDerivativeX + m);		    // template X derivative
						iTy = *(ShapeInfoVec->edgeDerivativeY + m);    		// template Y derivative
						iTm = *(ShapeInfoVec->edgeMagnitude + m);			// template gradients magnitude

						if (curX < 0 || curY < 0 || curX > Width - 1 || curY > Height - 1)
							continue;

						iSx = *(pBufGradX + curY*widthstep + curX);			// get corresponding  X derivative from source image
						iSy = *(pBufGradY + curY*widthstep + curX);			// get corresponding  Y derivative from source image
						iSm = *(pBufMag + curY*widthstep + curX);			// get gradients magnitude from source image

						if ((iSx != 0 || iSy != 0) && (iTx != 0 || iTy != 0))
						{
							PartialSum = PartialSum + ((iSx * iTx) + (iSy * iTy)) * (iTm * iSm);// calculate similarity
						}
						SumOfCoords = m + 1;
						PartialScore = PartialSum / SumOfCoords;															// Normalized
						if (PartialScore < (MIN(anMinScore + NormGreediness * SumOfCoords, NormMinScore * SumOfCoords)))
							break;
					}

					if (PartialScore > ResultScore)
					{
						ResultScore = PartialScore;		// Match score
						TempPiontX = i;						// result coordinate X
						TempPiontY = j;						// result coordinate Y
					/*}
					if (ResultScore > TempScore)
					{*/
						TempScore = ResultScore;
						ResultList->ResultScore = TempScore;
						//ResultList->Angel = ShapeInfoVec->Angel;
						ResultList->CenterLocX = TempPiontX;
						ResultList->CenterLocY = TempPiontY;
					}
				}
			}
	}
}

float ShapeMatch::new_rsqrt(float f)
{	
	return 1 / sqrtf(f);
}


void ShapeMatch::CalSearchImgEdg(IplImage *SearchImage,ImgEdgeInfo *imgEdgeInfo)
{
	int Width = SearchImage->width;
	int Height = SearchImage->height;
	int widthstep = SearchImage->widthStep;
	/* Compute buffer sizes */
	uint32_t  bufferSize = widthstep * Height;
	/* Allocate buffers for each vector */
	uint8_t  *pInput = (uint8_t *)malloc(bufferSize * sizeof(uint8_t));
	imgEdgeInfo->pBufGradX = (int16_t *)malloc(bufferSize * sizeof(int16_t));
	imgEdgeInfo->pBufGradY = (int16_t *)malloc(bufferSize * sizeof(int16_t));
	imgEdgeInfo->pBufMag = (float *)malloc(bufferSize * sizeof(float));

	if (pInput &&imgEdgeInfo->pBufGradX && imgEdgeInfo->pBufGradY &&imgEdgeInfo->pBufMag)
	{
		//gaussian_filter(SearchImage, pInput, width, height);
		memcpy(pInput, SearchImage->imageData, bufferSize * sizeof(uint8_t));
		memset(imgEdgeInfo->pBufGradX, 0, bufferSize * sizeof(int16_t));
		memset(imgEdgeInfo->pBufGradY, 0, bufferSize * sizeof(int16_t));
		memset(imgEdgeInfo->pBufMag, 0, bufferSize * sizeof(float));

		int i, j, m; // count variable;
        #pragma omp parallel for
		for (i = 1; i < Width - 1; i++)
		{
			for (j = 1; j < Height - 1; j++)
			{
				int16_t sdx = *(pInput + j*widthstep + i + 1) - *(pInput + j*widthstep + i - 1);
				int16_t sdy = *(pInput + (j + 1)*widthstep + i) - *(pInput + (j - 1)*widthstep + i);
				*(imgEdgeInfo->pBufGradX + j*widthstep + i) = sdx;
				*(imgEdgeInfo->pBufGradY + j*widthstep + i) = sdy;
				*(imgEdgeInfo->pBufMag + j*widthstep + i) = new_rsqrt((float)(sdx*sdx) + (float)(sdy*sdy));
			}
		}
	}
	free(pInput);
}
