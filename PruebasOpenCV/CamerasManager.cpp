#include "stdafx.h"
#include "CamerasManager.h"

#include <stdlib.h>     /* srand, rand */
#include <iostream>
#include <time.h>       /* time */

#include "opencv2/highgui/highgui.hpp"

int cameraCount;
int precCamera;

CamerasManager::CamerasManager(void)
{
	cameraCount = CountCameras();
	precCamera = -1;
}

CamerasManager::~CamerasManager(void)
{
}

/**
 * Get the number of camera available
 */
int CamerasManager::CountCameras()
{
	cv::VideoCapture temp_camera;
	int maxTested = 10;
	for (int i = 0; i < maxTested; i++)
	{
		cv::VideoCapture temp_camera(i);
		bool res = (!temp_camera.isOpened());
		temp_camera.release();
		if (res)
		{
			return i;
		}
	}
	return maxTested;
}

int CamerasManager::ChangeCamera()
{
    int nextCamera = 0;
	time_t t;

	/* Intializes random number generator */
	srand((unsigned) time(&t));

    // Return a random camera
    do
    {
        nextCamera = rand() % cameraCount;
	} while (nextCamera == precCamera);

	precCamera = nextCamera;

    return precCamera;
}

